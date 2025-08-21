namespace LimboDancer.MCP.McpServer.Graph
{
    public sealed class TenantScopedGraphStore : IGraphStore
    {
        private readonly IGraphStore _inner;
        private readonly ITenantScopeAccessor _scope;
        private readonly ILogger<TenantScopedGraphStore> _log;

        public TenantScopedGraphStore(IGraphStore inner, ITenantScopeAccessor scope, ILogger<TenantScopedGraphStore> log)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public Task UpsertVertexPropertyAsync(string vertexId, string propertyKey, object? value, string? tenantId, CancellationToken ct)
            => _inner.UpsertVertexPropertyAsync(vertexId, propertyKey, value, RequireTenant(tenantId), ct);

        public Task UpsertEdgeAsync(string sourceVertexId, string targetVertexId, string edgeLabel, IDictionary<string, object?>? edgeProperties, string? tenantId, CancellationToken ct)
            => _inner.UpsertEdgeAsync(sourceVertexId, targetVertexId, edgeLabel, edgeProperties, RequireTenant(tenantId), ct);

        private string RequireTenant(string? provided)
        {
            var effective = string.IsNullOrWhiteSpace(provided) ? _scope.GetCurrentScope().TenantId : provided!;
            if (string.IsNullOrWhiteSpace(effective))
            {
                _log.LogError("TenantScopedGraphStore: missing TenantId for graph mutation.");
                throw new InvalidOperationException("TenantId is required for graph mutations.");
            }
            return effective;
        }
    }
}