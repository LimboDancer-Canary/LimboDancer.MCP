namespace LimboDancer.MCP.McpServer.Configuration;

/// <summary>
/// Configuration options for vector search services.
/// </summary>
public class VectorOptions
{
    /// <summary>
    /// Azure AI Search service endpoint.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure AI Search API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Search index name.
    /// </summary>
    public string IndexName { get; set; } = "ldm-memory";

    /// <summary>
    /// Vector dimensions for embeddings.
    /// </summary>
    public int VectorDimensions { get; set; } = 1536;
}