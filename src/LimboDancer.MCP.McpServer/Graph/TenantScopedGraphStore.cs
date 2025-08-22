using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.Graph.CosmosGremlin;
using Microsoft.Extensions.Logging;

namespace LimboDancer.MCP.McpServer.Graph
{
    public sealed class TenantScopedGraphStore : IGraphStore
    {
        private readonly GraphStore _inner;
        private readonly ITenantAccessor _tenant;
        private readonly ILogger<TenantScopedGraphStore> _log;

        public TenantScopedGraphStore(GraphStore inner, ITenantAccessor tenant, ILogger<TenantScopedGraphStore> log)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public Task UpsertVertexPropertyAsync(string localId, string propertyKey, object? value, Guid? tenantIdOverride, CancellationToken ct)
            => _inner.UpsertVertexPropertyAsync(localId, propertyKey, value, RequireTenant(tenantIdOverride), ct);

        public Task UpsertEdgeAsync(string sourceVertexId, string targetVertexId, string edgeLabel, IDictionary<string, object?>? edgeProperties, Guid? tenantIdOverride, CancellationToken ct)
            => _inner.UpsertEdgeAsync(sourceVertexId, targetVertexId, edgeLabel, edgeProperties, RequireTenant(tenantIdOverride), ct);

        public Task<string?> GetVertexPropertyAsync(string localId, string propertyKey, CancellationToken ct)
            => _inner.GetVertexPropertyAsync(localId, propertyKey, ct);

        private Guid RequireTenant(Guid? provided)
        {
            var effective = provided ?? _tenant.TenantId;
            if (effective == Guid.Empty)
            {
                _log.LogError("TenantScopedGraphStore: missing TenantId for graph mutation.");
                throw new InvalidOperationException("TenantId is required for graph mutations.");
            }
            return effective;
        }
    }
}