using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Exceptions;
using Microsoft.Extensions.Logging;
using LimboDancer.MCP.Core.Tenancy;
using Polly;
using Polly.Retry;

namespace LimboDancer.MCP.Graph.CosmosGremlin
{
    /// <summary>
    /// GraphStore encapsulates Gremlin (Cosmos DB) vertex/edge upsert operations with tenant scoping.
    /// Includes retry logic for transient failures.
    /// </summary>
    public sealed class GraphStore
    {
        private readonly IGremlinClient _client;
        private readonly Preconditions _preconditions;
        private readonly ITenantAccessor _tenantAccessor;
        private readonly ILogger<GraphStore> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        /// <summary>
        /// Constructor that uses ITenantAccessor for tenant resolution.
        /// </summary>
        public GraphStore(
            IGremlinClient client,
            ITenantAccessor tenantAccessor,
            ILoggerFactory loggerFactory,
            Preconditions? preconditions = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
            _logger = loggerFactory?.CreateLogger<GraphStore>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            _preconditions = preconditions ?? new Preconditions(client, () => Guid.Parse(_tenantAccessor.TenantId), loggerFactory.CreateLogger<Preconditions>());

            // Configure retry policy for transient failures
            _retryPolicy = Policy
                .Handle<ResponseException>(IsTransientException)
                .Or<ServerUnavailableException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception,
                            "Retry {RetryCount} after {Delay}ms for operation {Operation}",
                            retryCount, timeSpan.TotalMilliseconds, context.Values.GetValueOrDefault("operation"));
                    });
        }

        private static bool IsTransientException(ResponseException ex)
        {
            // Cosmos DB specific transient error codes
            if (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                ex.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                ex.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)
            {
                return true;
            }

            // Check for specific Cosmos DB error subcodes
            if (ex.Message?.Contains("Request rate is large", StringComparison.OrdinalIgnoreCase) == true ||
                ex.Message?.Contains("Timeout", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            return false;
        }

        public async Task UpsertVertexAsync(string label, string localId, IDictionary<string, object>? properties = null, CancellationToken ct = default)
        {
            GraphWriteHelpers.ValidateLabel(label);

            var tenantId = Guid.Parse(_tenantAccessor.TenantId);
            var vertexId = GraphWriteHelpers.ToVertexId(tenantId, localId);

            await _retryPolicy.ExecuteAsync(async (context, cancellationToken) =>
            {
                var upsertQuery =
                    "g.V(vid).fold().coalesce(" +
                    "unfold()," +
                    "addV(lbl).property('id', vid).property(tprop, tid)" +
                    ")";

                var upsertBindings = new Dictionary<string, object>
                {
                    ["vid"] = vertexId,
                    ["lbl"] = label,
                    ["tid"] = tenantId.ToString("D"),
                    ["tprop"] = GraphWriteHelpers.TenantPropertyName
                };

                await _client.SubmitAsync<dynamic>(upsertQuery, upsertBindings, cancellationToken: cancellationToken).ConfigureAwait(false);

                var props = GraphWriteHelpers.WithTenantProperty(properties, tenantId);
                foreach (var kv in props)
                {
                    if (string.Equals(kv.Key, GraphWriteHelpers.TenantPropertyName, StringComparison.Ordinal))
                    {
                        var pQuery = "g.V(vid).property(tprop, tid)";
                        var pBindings = new Dictionary<string, object>
                        {
                            ["vid"] = vertexId,
                            ["tprop"] = GraphWriteHelpers.TenantPropertyName,
                            ["tid"] = tenantId.ToString("D")
                        };
                        await _client.SubmitAsync<dynamic>(pQuery, pBindings, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var pQuery = "g.V(vid).has(tprop, tid).property(k, v)";
                        var pBindings = new Dictionary<string, object>
                        {
                            ["vid"] = vertexId,
                            ["tprop"] = GraphWriteHelpers.TenantPropertyName,
                            ["tid"] = tenantId.ToString("D"),
                            ["k"] = kv.Key,
                            ["v"] = kv.Value
                        };
                        await _client.SubmitAsync<dynamic>(pQuery, pBindings, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                }
            }, new Context { ["operation"] = "UpsertVertex" }, ct);
        }

        /// <summary>
        /// Upsert a single property on a vertex with optional explicit tenant override.
        /// Used by GraphEffectsService for property effects.
        /// </summary>
        public async Task UpsertVertexPropertyAsync(string localId, string propertyKey, object? value, Guid? tenantIdOverride = null, CancellationToken ct = default)
        {
            GraphWriteHelpers.ValidatePropertyKey(propertyKey);

            var tenantId = tenantIdOverride ?? Guid.Parse(_tenantAccessor.TenantId);
            var vertexId = GraphWriteHelpers.ToVertexId(tenantId, localId);

            await _retryPolicy.ExecuteAsync(async (context, cancellationToken) =>
            {
                // Ensure vertex exists first
                if (!await _preconditions.VertexExistsAsync(localId, cancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException($"Vertex '{localId}' does not exist for tenant '{tenantId}'.");
                }

                var query = "g.V(vid).has(tprop, tid).property(k, v)";
                var bindings = new Dictionary<string, object>
                {
                    ["vid"] = vertexId,
                    ["tprop"] = GraphWriteHelpers.TenantPropertyName,
                    ["tid"] = tenantId.ToString("D"),
                    ["k"] = propertyKey,
                    ["v"] = value ?? ""
                };

                await _client.SubmitAsync<dynamic>(query, bindings, cancellationToken: cancellationToken).ConfigureAwait(false);
            }, new Context { ["operation"] = "UpsertVertexProperty" }, ct);
        }

        /// <summary>
        /// Get a property value from a vertex.
        /// Used by GraphPreconditionsService.
        /// </summary>
        public async Task<string?> GetVertexPropertyAsync(string localId, string propertyKey, CancellationToken ct = default)
        {
            GraphWriteHelpers.ValidatePropertyKey(propertyKey);

            var tenantId = Guid.Parse(_tenantAccessor.TenantId);
            var vertexId = GraphWriteHelpers.ToVertexId(tenantId, localId);

            return await _retryPolicy.ExecuteAsync(async (context, cancellationToken) =>
            {
                var query = "g.V(vid).has(tprop, tid).values(k).limit(1)";
                var bindings = new Dictionary<string, object>
                {
                    ["vid"] = vertexId,
                    ["tprop"] = GraphWriteHelpers.TenantPropertyName,
                    ["tid"] = tenantId.ToString("D"),
                    ["k"] = propertyKey
                };

                var result = await _client.SubmitAsync<dynamic>(query, bindings, cancellationToken: cancellationToken).ConfigureAwait(false);
                foreach (var r in result)
                {
                    return r?.ToString();
                }
                return null;
            }, new Context { ["operation"] = "GetVertexProperty" }, ct);
        }

        public async Task UpsertEdgeAsync(string label, string outLocalId, string inLocalId, IDictionary<string, object>? properties = null, CancellationToken ct = default)
        {
            GraphWriteHelpers.ValidateLabel(label);

            var tenantId = Guid.Parse(_tenantAccessor.TenantId);
            var outId = GraphWriteHelpers.ToVertexId(tenantId, outLocalId);
            var inId = GraphWriteHelpers.ToVertexId(tenantId, inLocalId);

            GraphWriteHelpers.EnsureTenantMatches(tenantId, outId);
            GraphWriteHelpers.EnsureTenantMatches(tenantId, inId);

            await _retryPolicy.ExecuteAsync(async (context, cancellationToken) =>
            {
                if (!await _preconditions.VertexExistsAsync(outLocalId, cancellationToken).ConfigureAwait(false))
                    throw new InvalidOperationException($"Out-vertex does not exist for tenant '{tenantId}': '{outLocalId}'.");

                if (!await _preconditions.VertexExistsAsync(inLocalId, cancellationToken).ConfigureAwait(false))
                    throw new InvalidOperationException($"In-vertex does not exist for tenant '{tenantId}': '{inLocalId}'.");

                var upsertEdgeQuery =
                    "g.V(outId).has(tprop, tid).as('a')" +
                    ".V(inId).has(tprop, tid).as('b')" +
                    ".coalesce(" +
                    "select('a').outE(lbl).filter(inV().hasId(inId)).has(tprop, tid)," +
                    "addE(lbl).from('a').to('b').property(tprop, tid)" +
                    ")";

                var bindings = new Dictionary<string, object>
                {
                    ["outId"] = outId,
                    ["inId"] = inId,
                    ["lbl"] = label,
                    ["tid"] = tenantId.ToString("D"),
                    ["tprop"] = GraphWriteHelpers.TenantPropertyName
                };

                await _client.SubmitAsync<dynamic>(upsertEdgeQuery, bindings, cancellationToken: cancellationToken).ConfigureAwait(false);

                var props = GraphWriteHelpers.WithTenantProperty(properties, tenantId);
                foreach (var kv in props)
                {
                    if (string.Equals(kv.Key, GraphWriteHelpers.TenantPropertyName, StringComparison.Ordinal))
                    {
                        var q = "g.V(outId).outE(lbl).filter(inV().hasId(inId)).property(tprop, tid)";
                        var b = new Dictionary<string, object>
                        {
                            ["outId"] = outId,
                            ["inId"] = inId,
                            ["lbl"] = label,
                            ["tprop"] = GraphWriteHelpers.TenantPropertyName,
                            ["tid"] = tenantId.ToString("D")
                        };
                        await _client.SubmitAsync<dynamic>(q, b, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var q = "g.V(outId).has(tprop, tid).outE(lbl).filter(inV().hasId(inId)).has(tprop, tid).property(k, v)";
                        var b = new Dictionary<string, object>
                        {
                            ["outId"] = outId,
                            ["inId"] = inId,
                            ["lbl"] = label,
                            ["tid"] = tenantId.ToString("D"),
                            ["tprop"] = GraphWriteHelpers.TenantPropertyName,
                            ["k"] = kv.Key,
                            ["v"] = kv.Value
                        };
                        await _client.SubmitAsync<dynamic>(q, b, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                }
            }, new Context { ["operation"] = "UpsertEdge" }, ct);
        }

        /// <summary>
        /// Upsert edge with optional explicit tenant override.
        /// Used by GraphEffectsService for edge effects.
        /// </summary>
        public async Task UpsertEdgeAsync(string outLocalId, string inLocalId, string label, IDictionary<string, object>? properties, Guid? tenantIdOverride, CancellationToken ct = default)
        {
            // Set tenant override if provided
            var originalTenantId = _tenantAccessor.TenantId;
            if (tenantIdOverride.HasValue && tenantIdOverride.Value.ToString() != originalTenantId)
            {
                _logger.LogWarning("UpsertEdge called with tenant override from {Original} to {Override}", originalTenantId, tenantIdOverride.Value);
            }

            await UpsertEdgeAsync(label, outLocalId, inLocalId, properties, ct).ConfigureAwait(false);
        }

        public async Task<dynamic?> GetVertexAsync(string localId, CancellationToken ct = default)
        {
            var tenantId = Guid.Parse(_tenantAccessor.TenantId);
            var vid = GraphWriteHelpers.ToVertexId(tenantId, localId);

            return await _retryPolicy.ExecuteAsync(async (context, cancellationToken) =>
            {
                var query = "g.V(vid).has(tprop, tid).limit(1)";
                var bindings = new Dictionary<string, object>
                {
                    ["vid"] = vid,
                    ["tid"] = tenantId.ToString("D"),
                    ["tprop"] = GraphWriteHelpers.TenantPropertyName
                };

                var result = await _client.SubmitAsync<dynamic>(query, bindings, cancellationToken: cancellationToken).ConfigureAwait(false);
                foreach (var r in result) return r;
                return null;
            }, new Context { ["operation"] = "GetVertex" }, ct);
        }

        public async Task<IReadOnlyCollection<dynamic>> QueryVerticesByLabelAsync(string label, CancellationToken ct = default)
        {
            GraphWriteHelpers.ValidateLabel(label);
            var tenantId = Guid.Parse(_tenantAccessor.TenantId);

            return await _retryPolicy.ExecuteAsync(async (context, cancellationToken) =>
            {
                var query = "g.V().hasLabel(lbl).has(tprop, tid)";
                var bindings = new Dictionary<string, object>
                {
                    ["lbl"] = label,
                    ["tid"] = tenantId.ToString("D"),
                    ["tprop"] = GraphWriteHelpers.TenantPropertyName
                };

                return await _client.SubmitAsync<dynamic>(query, bindings, cancellationToken: cancellationToken).ConfigureAwait(false);
            }, new Context { ["operation"] = "QueryVerticesByLabel" }, ct);
        }

        public async Task DeleteVertexAsync(string localId, CancellationToken ct = default)
        {
            var tenantId = Guid.Parse(_tenantAccessor.TenantId);
            var vid = GraphWriteHelpers.ToVertexId(tenantId, localId);

            await _retryPolicy.ExecuteAsync(async (context, cancellationToken) =>
            {
                var query = "g.V(vid).has(tprop, tid).drop()";
                var bindings = new Dictionary<string, object>
                {
                    ["vid"] = vid,
                    ["tid"] = tenantId.ToString("D"),
                    ["tprop"] = GraphWriteHelpers.TenantPropertyName
                };

                await _client.SubmitAsync<dynamic>(query, bindings, cancellationToken: cancellationToken).ConfigureAwait(false);
            }, new Context { ["operation"] = "DeleteVertex" }, ct);
        }

        public async Task DeleteEdgeAsync(string label, string outLocalId, string inLocalId, CancellationToken ct = default)
        {
            GraphWriteHelpers.ValidateLabel(label);

            var tenantId = Guid.Parse(_tenantAccessor.TenantId);
            var outId = GraphWriteHelpers.ToVertexId(tenantId, outLocalId);
            var inId = GraphWriteHelpers.ToVertexId(tenantId, inLocalId);

            await _retryPolicy.ExecuteAsync(async (context, cancellationToken) =>
            {
                var query =
                    "g.E().hasLabel(lbl).has(tprop, tid)" +
                    ".filter(outV().hasId(outId).and(inV().hasId(inId))).drop()";

                var bindings = new Dictionary<string, object>
                {
                    ["outId"] = outId,
                    ["inId"] = inId,
                    ["lbl"] = label,
                    ["tid"] = tenantId.ToString("D"),
                    ["tprop"] = GraphWriteHelpers.TenantPropertyName
                };

                await _client.SubmitAsync<dynamic>(query, bindings, cancellationToken: cancellationToken).ConfigureAwait(false);
            }, new Context { ["operation"] = "DeleteEdge" }, ct);
        }
    }
}