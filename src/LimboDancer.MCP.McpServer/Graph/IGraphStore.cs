namespace LimboDancer.MCP.McpServer.Graph;

/// <summary>
/// Minimal graph store contract used by this service.
/// NOTE: If your project already exposes a richer IGraphStore, adapt these calls accordingly.
/// </summary>
public interface IGraphStore
{
    /// <summary>
    /// Upserts a vertex property, scoped to tenant (if provided).
    /// </summary>
    Task UpsertVertexPropertyAsync(string vertexId, string propertyKey, object? value, string? tenantId, CancellationToken ct);

    /// <summary>
    /// Adds or upserts a directed edge from source to target with the given concrete label, scoped to tenant (if provided).
    /// </summary>
    Task UpsertEdgeAsync(string sourceVertexId, string targetVertexId, string edgeLabel, IDictionary<string, object?>? edgeProperties, string? tenantId, CancellationToken ct);

}