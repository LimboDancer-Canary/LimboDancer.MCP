using LimboDancer.MCP.Core.Tenancy;

namespace LimboDancer.MCP.Graph.CosmosGremlin;

/// <summary>
/// Read-only helpers used by planner/guard code to validate graph state before performing actions.
/// All checks are tenant-scoped.
/// </summary>
public sealed class Preconditions
{
    private readonly GraphStore _graph;
    private readonly ITenantAccessor _tenant;

    public Preconditions(GraphStore graph, ITenantAccessor tenant)
    {
        _graph = graph;
        _tenant = tenant;
    }

    /// <summary>Ensures a vertex has a non-empty property value.</summary>
    public async Task<bool> HasPropertyAsync(string subjectLocalId, string property, CancellationToken ct = default)
    {
        var val = await _graph.GetVertexPropertyAsync(subjectLocalId, property, ct);
        return !string.IsNullOrWhiteSpace(val);
    }

    /// <summary>Ensures there is an edge between two vertices.</summary>
    public Task<bool> EdgeExistsAsync(string outLocalId, string edgeLabel, string inLocalId, CancellationToken ct = default)
        => _graph.EdgeExistsAsync(outLocalId, edgeLabel, inLocalId, ct);

    /// <summary>Ensures there is NO edge between two vertices.</summary>
    public async Task<bool> EdgeNotExistsAsync(string outLocalId, string edgeLabel, string inLocalId, CancellationToken ct = default)
        => !await _graph.EdgeExistsAsync(outLocalId, edgeLabel, inLocalId, ct);

    /// <summary>Utility for diagnostics.</summary>
    public Guid CurrentTenant() => _tenant.TenantId;
}