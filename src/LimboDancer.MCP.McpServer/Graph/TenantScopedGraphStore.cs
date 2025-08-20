// File: /src/LimboDancer.MCP.McpServer/Graph/TenantScopedGraphStore.cs
// Purpose: Enforce TenantId on *all* graph mutations, regardless of caller behavior.
//DI: register your concrete GraphStore and then .AddScoped<IGraphStore, TenantScopedGraphStore>() wrapping it.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LimboDancer.MCP.McpServer.Graph;

namespace LimboDancer.MCP.McpServer.Graph
{

    /// <summary>
    /// Decorator that guarantees a TenantId flows into the inner store.
    /// </summary>
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

        private string? RequireTenant(string? provided)
        {
            var effective = provided ?? _scope.CurrentTenantId;
            if (string.IsNullOrWhiteSpace(effective))
            {
                _log.LogError("TenantScopedGraphStore: missing TenantId for graph mutation.");
                throw new InvalidOperationException("TenantId is required for graph mutations.");
            }
            return effective;
        }
    }
}
