using System.Text.Json.Serialization;

namespace LimboDancer.MCP.Vector.AzureSearch;

/// <summary>
/// Minimal document shape for vector indexing & search.
/// </summary>
public sealed class MemoryDoc
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    public Guid TenantId { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public string[]? Tags { get; set; }

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; set; }

    /// <summary>Optional precomputed embedding. If null and an embedder is provided, it will be filled on upsert.</summary>
    [JsonPropertyName("vector")]
    public float[]? Vector { get; set; }
}
