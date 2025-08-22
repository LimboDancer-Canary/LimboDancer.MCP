using LimboDancer.MCP.Ontology.Mapping;
using LimboDancer.MCP.Core.Tenancy;
using Microsoft.Extensions.Logging;

namespace LimboDancer.MCP.McpServer.Graph
{
    public sealed record GraphEffect
    {
        public string? Predicate { get; init; }
        public object? Value { get; init; }
        public string? EdgeTargetId { get; init; }
        public string? EdgeLabel { get; init; }
        public string Mode { get; init; } = "replace";
    }

    public sealed record ApplyGraphEffectsRequest(string SubjectVertexId, IReadOnlyList<GraphEffect> Effects);

    public interface IGraphEffectsService
    {
        Task ApplyAsync(ApplyGraphEffectsRequest request, CancellationToken ct = default);
    }

    public sealed class GraphEffectsService : IGraphEffectsService
    {
        private readonly IGraphStore _graph;
        private readonly IPropertyKeyMapper _keys;
        private readonly ITenantAccessor _tenant;
        private readonly ILogger<GraphEffectsService> _log;

        public GraphEffectsService(IGraphStore graph, IPropertyKeyMapper keys, ITenantAccessor tenant, ILogger<GraphEffectsService> log)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task ApplyAsync(ApplyGraphEffectsRequest request, CancellationToken ct = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.SubjectVertexId)) throw new ArgumentException("SubjectVertexId is required.", nameof(request));

            var tenantId = _tenant.TenantId;
            if (tenantId == Guid.Empty)
            {
                _log.LogWarning("Applying GraphEffects without a resolved TenantId. Subject={Subject}", request.SubjectVertexId);
            }

            if (request.Effects == null || request.Effects.Count == 0)
            {
                _log.LogDebug("No effects to apply. Subject={Subject}", request.SubjectVertexId);
                return;
            }

            foreach (var effect in request.Effects)
            {
                ct.ThrowIfCancellationRequested();
                var isEdgeEffect = !string.IsNullOrWhiteSpace(effect.EdgeTargetId);
                if (isEdgeEffect)
                    await ApplyEdgeEffectAsync(request.SubjectVertexId, effect, tenantId, ct);
                else
                    await ApplyPropertyEffectAsync(request.SubjectVertexId, effect, tenantId, ct);
            }
        }

        private async Task ApplyPropertyEffectAsync(string subjectId, GraphEffect effect, Guid tenantId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(effect.Predicate))
            {
                _log.LogDebug("Skipping property effect with no predicate. Subject={Subject}", subjectId);
                return;
            }

            if (!_keys.TryMapPropertyKey(effect.Predicate, out var propertyKey))
            {
                _log.LogWarning("Unknown property predicate '{Predicate}'. Subject={Subject} — effect skipped.", effect.Predicate, subjectId);
                return;
            }

            await _graph.UpsertVertexPropertyAsync(subjectId, propertyKey, effect.Value, tenantId, ct);
        }

        private async Task ApplyEdgeEffectAsync(string subjectId, GraphEffect effect, Guid tenantId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(effect.EdgeTargetId) || string.IsNullOrWhiteSpace(effect.EdgeLabel))
            {
                _log.LogWarning("Edge effect missing target or label. Subject={Subject}", subjectId);
                return;
            }

            await _graph.UpsertEdgeAsync(subjectId, effect.EdgeTargetId, effect.EdgeLabel, null, tenantId, ct);
        }
    }
}