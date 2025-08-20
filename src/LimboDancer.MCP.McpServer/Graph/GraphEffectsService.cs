// UPDATED: now depends on central LimboDancer.MCP.Ontology.Mapping.IPropertyKeyMapper.
// No other behavior changes; still enforces tenant and maps predicates.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LimboDancer.MCP.Ontology.Mapping;

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

    public interface ITenantScopeAccessor
    {
        string? CurrentTenantId { get; }
    }

    public interface IGraphStore
    {
        Task UpsertVertexPropertyAsync(string vertexId, string propertyKey, object? value, string? tenantId, CancellationToken ct);
        Task UpsertEdgeAsync(string sourceVertexId, string targetVertexId, string edgeLabel, IDictionary<string, object?>? edgeProperties, string? tenantId, CancellationToken ct);
    }

    public sealed class GraphEffectsService : IGraphEffectsService
    {
        private readonly IGraphStore _graph;
        private readonly IPropertyKeyMapper _keys; // ⬅ central mapper
        private readonly ITenantScopeAccessor _scope;
        private readonly ILogger<GraphEffectsService> _log;

        public GraphEffectsService(IGraphStore graph, IPropertyKeyMapper keys, ITenantScopeAccessor scope, ILogger<GraphEffectsService> log)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task ApplyAsync(ApplyGraphEffectsRequest request, CancellationToken ct = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.SubjectVertexId)) throw new ArgumentException("SubjectVertexId is required.", nameof(request));

            var tenantId = _scope.CurrentTenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
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

        private async Task ApplyPropertyEffectAsync(string subjectId, GraphEffect effect, string? tenantId, CancellationToken ct)
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
            _log.LogDebug("Property upserted: {Subject}.{Key} = {Value} (Tenant={Tenant})",
                subjectId, propertyKey, TruncateForLog(effect.Value), tenantId);
        }

        private async Task ApplyEdgeEffectAsync(string subjectId, GraphEffect effect, string? tenantId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(effect.EdgeTargetId))
            {
                _log.LogDebug("Skipping edge effect with no target. Subject={Subject}", subjectId);
                return;
            }

            var relationPredicate = effect.EdgeLabel ?? effect.Predicate;
            if (string.IsNullOrWhiteSpace(relationPredicate))
            {
                _log.LogWarning("Edge effect missing both EdgeLabel and Predicate. Subject={Subject} -> Target={Target} — skipped.", subjectId, effect.EdgeTargetId);
                return;
            }

            if (!_keys.TryMapEdgeLabel(relationPredicate!, out var edgeLabel))
            {
                _log.LogWarning("Unknown edge predicate '{Predicate}'. Subject={Subject} -> Target={Target} — effect skipped.", relationPredicate, subjectId, effect.EdgeTargetId);
                return;
            }

            IDictionary<string, object?>? edgeProps = null;
            if (effect.Value is not null)
            {
                edgeProps = new Dictionary<string, object?> { ["value"] = effect.Value };
            }

            await _graph.UpsertEdgeAsync(subjectId, effect.EdgeTargetId!, edgeLabel, edgeProps, tenantId, ct);
            _log.LogDebug("Edge upserted: {Subject}-[{Label}]->{Target} (Tenant={Tenant})", subjectId, edgeLabel, effect.EdgeTargetId, tenantId);
        }

        private static object? TruncateForLog(object? value)
        {
            if (value is null) return null;
            var s = value.ToString();
            if (s is null) return value;
            return s.Length <= 512 ? s : s.Substring(0, 512) + "…";
        }
    }
}
