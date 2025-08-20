using LimboDancer.MCP.McpServer.Tenancy;
using LimboDancer.MCP.Ontology.Runtime;
using LimboDancer.MCP.Graph.CosmosGremlin;

namespace LimboDancer.MCP.McpServer.Graph;

/// <summary>
/// Commits ontology-bound tool effects to the graph within the current TenantScope.
/// This is intentionally simple and can be extended to support batched multi-property updates and edges.
/// </summary>
public sealed class GraphEffectsService
{
    private readonly GraphStore _graph;
    private readonly ITenantScopeAccessor _scope;

    public GraphEffectsService(GraphStore graph, ITenantScopeAccessor scope)
    {
        _graph = graph;
        _scope = scope;
    }

    public async Task CommitAsync(string subjectId, IEnumerable<ToolEffect> effects, CancellationToken ct = default)
    {
        // For this initial pass we support simple property updates (set).
        foreach (var e in effects)
        {
            if (string.IsNullOrWhiteSpace(e.Predicate)) continue;
            await _graph.SetVertexPropertyAsync(subjectId, e.Predicate!, e.Value ?? string.Empty, ct);
        }
    }
}