using LimboDancer.MCP.McpServer.Tenancy;
using LimboDancer.MCP.McpServer.Tools;
using LimboDancer.MCP.Ontology.Constants;
using LimboDancer.MCP.Ontology.Mapping;
using LimboDancer.MCP.Ontology.Runtime;
using LimboDancer.MCP.Graph.CosmosGremlin;
using Microsoft.Extensions.Logging;

namespace LimboDancer.MCP.McpServer.Graph;

/// <summary>
/// Evaluates ontology-bound preconditions against the tenant-scoped graph.
/// Maps ontology predicates (CURIE/IRI/local) to graph property keys.
/// </summary>
public sealed class GraphPreconditionsService : IGraphPreconditionsService
{
    private readonly GraphStore _graph;
    private readonly ITenantScopeAccessor _scope;
    private readonly IPropertyKeyMapper _mapper;
    private readonly ILogger<GraphPreconditionsService> _logger;

    public GraphPreconditionsService(
        GraphStore graph,
        ITenantScopeAccessor scope,
        IPropertyKeyMapper mapper,
        ILogger<GraphPreconditionsService> logger)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Implement the interface method
    public async Task<PreconditionsResult> CheckAsync(CheckGraphPreconditionsRequest request, CancellationToken ct = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.SubjectVertexId))
            throw new ArgumentException("SubjectVertexId is required.", nameof(request));

        var violations = new List<PreconditionViolation>();

        foreach (var precondition in request.Preconditions ?? Enumerable.Empty<GraphPrecondition>())
        {
            var result = await EvaluatePreconditionAsync(request.SubjectVertexId, precondition, ct);
            if (!result.Ok && !string.IsNullOrEmpty(result.Reason))
            {
                violations.Add(new PreconditionViolation(
                    precondition.Predicate ?? "unknown",
                    result.Reason
                ));
            }
        }

        return new PreconditionsResult(
            IsSatisfied: violations.Count == 0,
            Violations: violations
        );
    }

    private async Task<PreconditionCheckResult> EvaluatePreconditionAsync(
        string subjectId,
        GraphPrecondition precondition,
        CancellationToken ct)
    {
        var scope = _scope.GetCurrentScope();

        // Simple existence check if no predicate
        if (string.IsNullOrWhiteSpace(precondition.Predicate))
        {
            var exists = await _graph.GetVertexPropertyAsync(subjectId, Kg.Name, ct);
            return string.IsNullOrEmpty(exists)
                ? PreconditionCheckResult.Fail($"[{scope}] Subject {subjectId} not found.")
                : PreconditionCheckResult.Pass();
        }

        // Map the predicate to a graph property key
        string propertyKey;
        if (!_mapper.TryMapPropertyKey(precondition.Predicate, out propertyKey!))
        {
            _logger.LogWarning("Unknown property predicate '{Predicate}' for precondition check", precondition.Predicate);
            propertyKey = precondition.Predicate; // Use as-is if not mapped
        }

        // Check the condition based on the operation
        switch (precondition.Op?.ToLowerInvariant() ?? "eq")
        {
            case "eq":
            case "equals":
                var value = await _graph.GetVertexPropertyAsync(subjectId, propertyKey, ct);
                var expected = precondition.Expected?.ToString() ?? "";
                if (!string.Equals(value, expected, StringComparison.OrdinalIgnoreCase))
                {
                    var actual = value ?? "<null>";
                    return PreconditionCheckResult.Fail(
                        $"[{scope}] Precondition failed: {propertyKey} != {expected} (actual: {actual})");
                }
                break;

            case "exists":
                var existsValue = await _graph.GetVertexPropertyAsync(subjectId, propertyKey, ct);
                if (string.IsNullOrEmpty(existsValue))
                {
                    return PreconditionCheckResult.Fail(
                        $"[{scope}] Precondition failed: property {propertyKey} does not exist");
                }
                break;

            default:
                _logger.LogWarning("Unsupported precondition operation: {Op}", precondition.Op);
                return PreconditionCheckResult.Fail($"Unsupported operation: {precondition.Op}");
        }

        return PreconditionCheckResult.Pass();
    }

    // Legacy methods preserved for backward compatibility
    public async Task<PreconditionCheckResult> EvaluateAsync(
        string subjectLabel,
        string subjectId,
        ToolPrecondition precondition,
        CancellationToken ct = default)
    {
        var graphPrecondition = new GraphPrecondition(
            precondition.Predicate ?? "",
            "eq",
            precondition.Equals
        );

        return await EvaluatePreconditionAsync(subjectId, graphPrecondition, ct);
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
}

public sealed record PreconditionCheckResult(bool Ok, string? Reason)
{
    public static PreconditionCheckResult Pass() => new(true, null);
    public static PreconditionCheckResult Fail(string reason) => new(false, reason);
}

// Legacy type for backward compatibility
public sealed record ToolPrecondition(string? Predicate, string? Equals);