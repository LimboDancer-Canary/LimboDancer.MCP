using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gremlin.Net.Driver;
using Microsoft.Extensions.Logging;

namespace LimboDancer.MCP.Graph.CosmosGremlin
{
    public sealed class Preconditions
    {
        private readonly IGremlinClient _client;
        private readonly Func<string> _getTenantId;
        private readonly ILogger<Preconditions> _logger;

        public Preconditions(IGremlinClient client, Func<string> getTenantId, ILogger<Preconditions> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _getTenantId = getTenantId ?? throw new ArgumentNullException(nameof(getTenantId));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> VertexExistsAsync(string localId, CancellationToken ct = default)
        {
            var tenantId = _getTenantId();
            var vid = GraphWriteHelpers.ToVertexId(tenantId, localId);

            var query = "g.V(vid).has(tprop, tid).limit(1).count()";
            var bindings = new Dictionary<string, object>
            {
                ["vid"] = vid,
                ["tid"] = tenantId,
                ["tprop"] = GraphWriteHelpers.TenantPropertyName
            };

            var result = await _client.SubmitAsync<long>(query, bindings, cancellationToken: ct).ConfigureAwait(false);
            var count = FirstOrDefault(result);
            return count > 0;
        }

        public async Task<bool> EdgeExistsAsync(string label, string outLocalId, string inLocalId, CancellationToken ct = default)
        {
            GraphWriteHelpers.ValidateLabel(label);
            var tenantId = _getTenantId();
            var outId = GraphWriteHelpers.ToVertexId(tenantId, outLocalId);
            var inId = GraphWriteHelpers.ToVertexId(tenantId, inLocalId);

            var query = "g.V(outId).has(tprop, tid).outE(lbl)." +
                        "filter(inV().hasId(inId)).has(tprop, tid).limit(1).count()";

            var bindings = new Dictionary<string, object>
            {
                ["outId"] = outId,
                ["inId"] = inId,
                ["lbl"] = label,
                ["tid"] = tenantId,
                ["tprop"] = GraphWriteHelpers.TenantPropertyName
            };

            var result = await _client.SubmitAsync<long>(query, bindings, cancellationToken: ct).ConfigureAwait(false);
            var count = FirstOrDefault(result);
            return count > 0;
        }

        public async Task<bool> HasPropertyAsync(string localId, string key, object? value, CancellationToken ct = default)
        {
            GraphWriteHelpers.ValidatePropertyKey(key);
            var tenantId = _getTenantId();
            var vid = GraphWriteHelpers.ToVertexId(tenantId, localId);

            string query;
            var bindings = new Dictionary<string, object>
            {
                ["vid"] = vid,
                ["tid"] = tenantId,
                ["tprop"] = GraphWriteHelpers.TenantPropertyName,
                ["k"] = key
            };

            if (value is null)
            {
                // Property exists regardless of value
                query = "g.V(vid).has(tprop, tid).properties(k).limit(1).count()";
            }
            else
            {
                query = "g.V(vid).has(tprop, tid).has(k, v).limit(1).count()";
                bindings["v"] = value;
            }

            var result = await _client.SubmitAsync<long>(query, bindings, cancellationToken: ct).ConfigureAwait(false);
            var count = FirstOrDefault(result);
            return count > 0;
        }

        private static T FirstOrDefault<T>(IReadOnlyCollection<T> results)
        {
            foreach (var r in results) return r;
            return default!;
        }
    }
}