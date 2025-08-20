using LimboDancer.MCP.McpServer.Tenancy;
using LimboDancer.MCP.Ontology.Constants;
using LimboDancer.MCP.Ontology.Runtime;
using LimboDancer.MCP.Graph.CosmosGremlin;

namespace LimboDancer.MCP.McpServer.Graph;

/// <summary>
/// Evaluates ontology-bound preconditions against the tenant-scoped graph.
/// This mirrors the design doc evaluator and ensures property mapping from ontology URIs to graph keys.
/// </summary>
public sealed class GraphPreconditionsService
{
    private readonly GraphStore _graph;
    private readonly ITenantScopeAccessor _scope;

    public GraphPreconditionsService(GraphStore graph, ITenantScopeAccessor scope)
    {
        _graph = graph;
        _scope = scope;
    }

    public async Task<PreconditionCheckResult> EvaluateAsync(
        string subjectLabel,
        string subjectId,
        ToolPrecondition precondition,
        CancellationToken ct = default)
    {
        // The GraphStore is expected to enforce tenant guards internally; we only pass local ids here.
        if (string.IsNullOrWhiteSpace(precondition.Predicate) && string.IsNullOrWhiteSpace(precondition.Equals))
        {
            // Existence check using a cheap probe
            var label = await _graph.GetVertexPropertyAsync(subjectId, Kg.Fields.Label, ct);
            return string.IsNullOrEmpty(label)
                ? PreconditionCheckResult.Fail($"Subject {subjectLabel}/{subjectId} not found.")
                : PreconditionCheckResult.Pass();
        }

        var key = MapPropertyKey(precondition.Predicate);
        if (!string.IsNullOrWhiteSpace(precondition.Equals))
        {
            var value = await _graph.GetVertexPropertyAsync(subjectId, key, ct);
            if (!string.Equals(value, precondition.Equals, StringComparison.OrdinalIgnoreCase))
                return PreconditionCheckResult.Fail($"Precondition failed: {key} != {precondition.Equals} (actual: {value ?? "<null>"})");
        }

        return PreconditionCheckResult.Pass();
    }

    public async Task<PreconditionCheckResult> EvaluateAllAsync(
        string subjectLabel,
        string subjectId,
        IEnumerable<ToolPrecondition> preconditions,
        CancellationToken ct = default)
    {
        foreach (var p in preconditions)
        {
            var r = await EvaluateAsync(subjectLabel, subjectId, p, ct);
            if (!r.Ok) return r;
        }
        return PreconditionCheckResult.Pass();
    }

    private static string MapPropertyKey(string? ontologyPredicate)
    {
        if (string.IsNullOrWhiteSpace(ontologyPredicate))
            return Kg.Fields.Label;

        // Known mappings to graph field names
        if (ontologyPredicate == Ldm.Properties.Status) return Kg.Fields.Status;
        if (ontologyPredicate == Ldm.Properties.Label) return Kg.Fields.Label;
        if (ontologyPredicate == Ldm.Properties.Kind) return Kg.Fields.Kind;

        // Fallback: last segment after ':' or '/'
        var s = ontologyPredicate!;
        var idx = Math.Max(s.LastIndexOf(':'), s.LastIndexOf('/'));
        return idx >= 0 && idx < s.Length - 1 ? s[(idx + 1)..] : s;
    }
}

public sealed record PreconditionCheckResult(bool Ok, string? Reason)
{
    public static PreconditionCheckResult Pass() => new(true, null);
    public static PreconditionCheckResult Fail(string reason) => new(false, reason);
}