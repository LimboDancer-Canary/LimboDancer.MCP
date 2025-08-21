using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gremlin.Net.Driver;
using Microsoft.Extensions.Logging;
using LimboDancer.MCP.Core.Tenancy;

namespace LimboDancer.MCP.Graph.CosmosGremlin
{
    public sealed class GraphStore
    {
        private readonly IGremlinClient _client;
        private readonly Preconditions _preconditions;
        private readonly Func<string> _getTenantId;
        private readonly ILogger<GraphStore> _logger;

        /// <summary>
        /// Legacy constructor for backward compatibility.
        /// </summary>
        [Obsolete("Use constructor with ILoggerFactory or ITenantAccessor instead. This constructor will be removed in a future version.")]
        public GraphStore(
            IGremlinClient client,
            Func<string> getTenantId,
            ILogger<GraphStore> logger,
            Preconditions? preconditions = null,
            ILogger<Preconditions>? preconditionsLogger = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _getTenantId = getTenantId ?? throw new ArgumentNullException(nameof(getTenantId));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _preconditions = preconditions ?? new Preconditions(client, getTenantId, preconditionsLogger ?? CreateFallbackLogger<Preconditions>(logger));
        }

        /// <summary>
        /// Constructor with ILoggerFactory for better DI integration.
        /// </summary>
        public GraphStore(
            IGremlinClient client,
            Func<string> getTenantId,
            ILoggerFactory loggerFactory,
            Preconditions? preconditions = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _getTenantId = getTenantId ?? throw new ArgumentNullException(nameof(getTenantId));
            _logger = loggerFactory?.CreateLogger<GraphStore>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            _preconditions = preconditions ?? new Preconditions(client, getTenantId, loggerFactory.CreateLogger<Preconditions>());
        }

        /// <summary>
        /// Constructor with ITenantAccessor for streamlined DI integration.
        /// </summary>
        public GraphStore(
            IGremlinClient client,
            ITenantAccessor tenantAccessor,
            ILoggerFactory loggerFactory,
            Preconditions? preconditions = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            var accessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
            _getTenantId = () => accessor.TenantId;
            _logger = loggerFactory?.CreateLogger<GraphStore>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            _preconditions = preconditions ?? new Preconditions(client, _getTenantId, loggerFactory.CreateLogger<Preconditions>());
        }

        private static ILogger<T> CreateFallbackLogger<T>(ILogger baseLogger)
        {
            // Attempt to cast, but provide a better fallback if cast fails
            return baseLogger as ILogger<T> ?? 
                   throw new ArgumentException($"Logger cannot be cast to ILogger<{typeof(T).Name}>. Use constructor with ILoggerFactory instead.");
        }

        // Upsert a vertex with tenant-scoped id and properties
        public async Task UpsertVertexAsync(string label, string localId, IDictionary<string, object>? properties = null, CancellationToken ct = default)
        {
            GraphWriteHelpers.ValidateLabel(label);

            var tenantId = _getTenantId();
            var vertexId = GraphWriteHelpers.ToVertexId(tenantId, localId);

            // Ensure vertex exists (create if not)
            var upsertQuery =
                "g.V(vid).fold().coalesce(" +
                "unfold()," +
                "addV(lbl).property('id', vid).property(tprop, tid)" +
                ")";

            var upsertBindings = new Dictionary<string, object>
            {
                ["vid"] = vertexId,
                ["lbl"] = label,
                ["tid"] = tenantId,
                ["tprop"] = GraphWriteHelpers.TenantPropertyName
            };

            await _client.SubmitAsync<dynamic>(upsertQuery, upsertBindings, cancellationToken: ct).ConfigureAwait(false);

            // Set/update properties, excluding reserved ones
            var props = GraphWriteHelpers.WithTenantProperty(properties, tenantId);
            foreach (var kv in props)
            {
                if (string.Equals(kv.Key, GraphWriteHelpers.TenantPropertyName, StringComparison.Ordinal))
                {
                    // Ensure tenant property is always enforced/update in case mismatch
                    var pQuery = "g.V(vid).property(tprop, tid)";
                    var pBindings = new Dictionary<string, object>
                    {
                        ["vid"] = vertexId,
                        ["tprop"] = GraphWriteHelpers.TenantPropertyName,
                        ["tid"] = tenantId
                    };
                    await _client.SubmitAsync<dynamic>(pQuery, pBindings, cancellationToken: ct).ConfigureAwait(false);
                }
                else
                {
                    var pQuery = "g.V(vid).has(tprop, tid).property(k, v)";
                    var pBindings = new Dictionary<string, object>
                    {
                        ["vid"] = vertexId,
                        ["tprop"] = GraphWriteHelpers.TenantPropertyName,
                        ["tid"] = tenantId,
                        ["k"] = kv.Key,
                        ["v"] = kv.Value
                    };
                    await _client.SubmitAsync<dynamic>(pQuery, pBindings, cancellationToken: ct).ConfigureAwait(false);
                }
            }
        }

        // Upsert an edge between two tenant-scoped vertices, forbidding cross-tenant edges
        public async Task UpsertEdgeAsync(string label, string outLocalId, string inLocalId, IDictionary<string, object>? properties = null, CancellationToken ct = default)
        {
            GraphWriteHelpers.ValidateLabel(label);

            var tenantId = _getTenantId();
            var outId = GraphWriteHelpers.ToVertexId(tenantId, outLocalId);
            var inId = GraphWriteHelpers.ToVertexId(tenantId, inLocalId);

            // Explicit guards against cross-tenant mistakes (defensive, since we constructed the ids)
            GraphWriteHelpers.EnsureTenantMatches(tenantId, outId);
            GraphWriteHelpers.EnsureTenantMatches(tenantId, inId);

            // Ensure both vertices exist and belong to tenant
            if (!await _preconditions.VertexExistsAsync(outLocalId, ct).ConfigureAwait(false))
            {
                throw new InvalidOperationException($"Out-vertex does not exist for tenant '{tenantId}': '{outLocalId}'.");
            }

            if (!await _preconditions.VertexExistsAsync(inLocalId, ct).ConfigureAwait(false))
            {
                throw new InvalidOperationException($"In-vertex does not exist for tenant '{tenantId}': '{inLocalId}'.");
            }

            // Upsert the edge and enforce tenant property
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
                ["tid"] = tenantId,
                ["tprop"] = GraphWriteHelpers.TenantPropertyName
            };

            await _client.SubmitAsync<dynamic>(upsertEdgeQuery, bindings, cancellationToken: ct).ConfigureAwait(false);

            // Apply/update additional properties
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
                        ["tid"] = tenantId
                    };
                    await _client.SubmitAsync<dynamic>(q, b, cancellationToken: ct).ConfigureAwait(false);
                }
                else
                {
                    var q = "g.V(outId).has(tprop, tid).outE(lbl).filter(inV().hasId(inId)).has(tprop, tid).property(k, v)";
                    var b = new Dictionary<string, object>
                    {
                        ["outId"] = outId,
                        ["inId"] = inId,
                        ["lbl"] = label,
                        ["tid"] = tenantId,
                        ["tprop"] = GraphWriteHelpers.TenantPropertyName,
                        ["k"] = kv.Key,
                        ["v"] = kv.Value
                    };
                    await _client.SubmitAsync<dynamic>(q, b, cancellationToken: ct).ConfigureAwait(false);
                }
            }
        }

        public async Task<dynamic?> GetVertexAsync(string localId, CancellationToken ct = default)
        {
            var tenantId = _getTenantId();
            var vid = GraphWriteHelpers.ToVertexId(tenantId, localId);

            var query = "g.V(vid).has(tprop, tid).limit(1)";
            var bindings = new Dictionary<string, object>
            {
                ["vid"] = vid,
                ["tid"] = tenantId,
                ["tprop"] = GraphWriteHelpers.TenantPropertyName
            };

            var result = await _client.SubmitAsync<dynamic>(query, bindings, cancellationToken: ct).ConfigureAwait(false);
            foreach (var r in result) return r;
            return null;
        }

        public async Task<IReadOnlyCollection<dynamic>> QueryVerticesByLabelAsync(string label, CancellationToken ct = default)
        {
            GraphWriteHelpers.ValidateLabel(label);
            var tenantId = _getTenantId();

            var query = "g.V().hasLabel(lbl).has(tprop, tid)";
            var bindings = new Dictionary<string, object>
            {
                ["lbl"] = label,
                ["tid"] = tenantId,
                ["tprop"] = GraphWriteHelpers.TenantPropertyName
            };

            return await _client.SubmitAsync<dynamic>(query, bindings, cancellationToken: ct).ConfigureAwait(false);
        }

        public async Task DeleteVertexAsync(string localId, CancellationToken ct = default)
        {
            var tenantId = _getTenantId();
            var vid = GraphWriteHelpers.ToVertexId(tenantId, localId);

            var query = "g.V(vid).has(tprop, tid).drop()";
            var bindings = new Dictionary<string, object>
            {
                ["vid"] = vid,
                ["tid"] = tenantId,
                ["tprop"] = GraphWriteHelpers.TenantPropertyName
            };

            await _client.SubmitAsync<dynamic>(query, bindings, cancellationToken: ct).ConfigureAwait(false);
        }

        public async Task DeleteEdgeAsync(string label, string outLocalId, string inLocalId, CancellationToken ct = default)
        {
            GraphWriteHelpers.ValidateLabel(label);

            var tenantId = _getTenantId();
            var outId = GraphWriteHelpers.ToVertexId(tenantId, outLocalId);
            var inId = GraphWriteHelpers.ToVertexId(tenantId, inLocalId);

            var query =
                "g.E().hasLabel(lbl).has(tprop, tid)" +
                ".filter(outV().hasId(outId).and(inV().hasId(inId))).drop()";

            var bindings = new Dictionary<string, object>
            {
                ["outId"] = outId,
                ["inId"] = inId,
                ["lbl"] = label,
                ["tid"] = tenantId,
                ["tprop"] = GraphWriteHelpers.TenantPropertyName
            };

            await _client.SubmitAsync<dynamic>(query, bindings, cancellationToken: ct).ConfigureAwait(false);
        }
    }
}