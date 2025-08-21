using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gremlin.Net.Driver;
using Microsoft.Extensions.Logging;
using LimboDancer.MCP.Core.Tenancy;

namespace LimboDancer.MCP.Graph.CosmosGremlin
{
    /// <summary>
    /// GraphStore encapsulates Gremlin (Cosmos DB) vertex/edge upsert operations with tenant scoping.
    ///
    /// Migration Note (C5):
    /// The delegate-based constructor GraphStore(IGremlinClient, Func&lt;string&gt;, ILoggerFactory, ...) has been removed.
    /// GraphStore now depends directly on ITenantAccessor for tenant resolution.
    /// Usage: new GraphStore(gremlinClient, tenantAccessor, loggerFactory)
    /// Preconditions always acquire their logger via ILoggerFactory; no casting occurs now.
    ///
    /// TODO (G7): Introduce DI extension AddCosmosGremlinGraph(...) to centralize registration & options binding.
    /// </summary>
    public sealed class GraphStore
    {
        private readonly IGremlinClient _client;
        private readonly Preconditions _preconditions;
        private readonly ITenantAccessor _tenantAccessor;
        private readonly ILogger<GraphStore> _logger;

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
            _preconditions = preconditions ?? new Preconditions(client, () => _tenantAccessor.TenantId, loggerFactory.CreateLogger<Preconditions>());
        }

        public async Task UpsertVertexAsync(string label, string localId, IDictionary<string, object>? properties = null, CancellationToken ct = default)
        {
            GraphWriteHelpers.ValidateLabel(label);

            var tenantId = _tenantAccessor.TenantId;
            var vertexId = GraphWriteHelpers.ToVertexId(tenantId, localId);

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

        public async Task UpsertEdgeAsync(string label, string outLocalId, string inLocalId, IDictionary<string, object>? properties = null, CancellationToken ct = default)
        {
            GraphWriteHelpers.ValidateLabel(label);

            var tenantId = _tenantAccessor.TenantId;
            var outId = GraphWriteHelpers.ToVertexId(tenantId, outLocalId);
            var inId = GraphWriteHelpers.ToVertexId(tenantId, inLocalId);

            GraphWriteHelpers.EnsureTenantMatches(tenantId, outId);
            GraphWriteHelpers.EnsureTenantMatches(tenantId, inId);

            if (!await _preconditions.VertexExistsAsync(outLocalId, ct).ConfigureAwait(false))
                throw new InvalidOperationException($"Out-vertex does not exist for tenant '{tenantId}': '{outLocalId}'.");

            if (!await _preconditions.VertexExistsAsync(inLocalId, ct).ConfigureAwait(false))
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
                ["tid"] = tenantId,
                ["tprop"] = GraphWriteHelpers.TenantPropertyName
            };

            await _client.SubmitAsync<dynamic>(upsertEdgeQuery, bindings, cancellationToken: ct).ConfigureAwait(false);

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
            var tenantId = _tenantAccessor.TenantId;
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
            var tenantId = _tenantAccessor.TenantId;

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
            var tenantId = _tenantAccessor.TenantId;
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

            var tenantId = _tenantAccessor.TenantId;
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