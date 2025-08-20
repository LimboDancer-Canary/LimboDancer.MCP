using LimboDancer.MCP.McpServer.Tenancy;
using LimboDancer.MCP.Ontology.Constants;
using LimboDancer.MCP.Ontology.Runtime;
using LimboDancer.MCP.Graph.CosmosGremlin;

namespace LimboDancer.MCP.McpServer.Graph;

/// <summary>
/// Evaluates ontology-bound preconditions against the tenant-scoped graph.
/// Maps ontology predicates (CURIE/IRI/local) to graph property keys.
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
        // Resolve current scope (used for diagnostics; GraphStore may enforce guards internally).
        var scope = _scope.GetCurrentScope();

        // If no predicate/value provided, treat as a simple existence probe by reading the "name"/label field.
        if (string.IsNullOrWhiteSpace(precondition.Predicate) && string.IsNullOrWhiteSpace(precondition.Equals))
        {
            var labelValue = await _graph.GetVertexPropertyAsync(subjectId, Kg.Name, ct);
            return string.IsNullOrEmpty(labelValue)
                ? PreconditionCheckResult.Fail($"[{scope}] Subject {subjectLabel}/{subjectId} not found.")
                : PreconditionCheckResult.Pass();
        }

        var key = MapPropertyKey(precondition.Predicate);

        if (!string.IsNullOrWhiteSpace(precondition.Equals))
        {
            var value = await _graph.GetVertexPropertyAsync(subjectId, key, ct);
            if (!string.Equals(value, precondition.Equals, StringComparison.OrdinalIgnoreCase))
            {
                var actual = value is null ? "<null>" : value;
                return PreconditionCheckResult.Fail($"[{scope}] Precondition failed: {key} != {precondition.Equals} (actual: {actual})");
            }
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
        // Default to a human-friendly label/name field when the predicate is empty.
        if (string.IsNullOrWhiteSpace(ontologyPredicate))
            return Kg.Name;

        var s = ontologyPredicate.Trim();

        // Derive a local name from CURIE/IRI: prefer last ':' segment, else last '/' segment.
        var lastColon = s.LastIndexOf(':');
        var lastSlash = s.LastIndexOf('/');
        var idx = Math.Max(lastColon, lastSlash);
        var local = idx >= 0 && idx < s.Length - 1 ? s[(idx + 1)..] : s;

        switch (local.ToLowerInvariant())
        {
            case "label":
            case "name":
                return Kg.Name;

            case "type":
            case "kind":
                // Prefer standardized "type" field if available.
                return Kg.Type;

            case "status":
                // Use a conventional 'status' field when present in the graph.
                return "status";

            default:
                // Unknown predicate -> use its local name as-is.
                return local;
        }
    }
}

public sealed record PreconditionCheckResult(bool Ok, string? Reason)
{
    public static PreconditionCheckResult Pass() => new(true, null);
    public static PreconditionCheckResult Fail(string reason) => new(false, reason);
}