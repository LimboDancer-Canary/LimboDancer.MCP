namespace LimboDancer.MCP.McpServer.Graph;

/// <summary>
/// Graph store contract matching GraphStore implementation.
/// </summary>
public interface IGraphStore
{
    /// <summary>
    /// Upserts a vertex property, scoped to tenant.
    /// </summary>
    Task UpsertVertexPropertyAsync(string localId, string propertyKey, object? value, Guid? tenantIdOverride, CancellationToken ct);

    /// <summary>
    /// Adds or upserts a directed edge from source to target with the given concrete label, scoped to tenant.
    /// </summary>
    Task UpsertEdgeAsync(string sourceVertexId, string targetVertexId, string edgeLabel, IDictionary<string, object?>? edgeProperties, Guid? tenantIdOverride, CancellationToken ct);

    /// <summary>
    /// Get a property value from a vertex.
    /// </summary>
    Task<string?> GetVertexPropertyAsync(string localId, string propertyKey, CancellationToken ct);
}