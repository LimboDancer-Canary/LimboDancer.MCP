using System;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace LimboDancer.MCP.Vector.AzureSearch;

/// <summary>
/// Index schema type used by SearchIndexBuilder. Field names must remain stable.
/// </summary>
public sealed class MemoryIndexDocument
{
    [SimpleField(IsKey = true, IsFilterable = true)]
    public string Id { get; set; } = string.Empty;

    [SimpleField(IsFilterable = true)]
    public string TenantId { get; set; } = string.Empty;

    [SearchableField(IsFilterable = false, IsSortable = false)]
    public string? Label { get; set; }

    [SimpleField(IsFilterable = true)]
    public string? Kind { get; set; }

    [SimpleField(IsFilterable = true)]
    public string? Status { get; set; }

    [SimpleField(IsFilterable = true)]
    public string? Tags { get; set; }

    [SearchableField]
    public string Content { get; set; } = string.Empty;

    // Vector field; dimensions/profile are applied by SearchIndexBuilder.ApplySchema
    public float[] ContentVector { get; set; } = Array.Empty<float>();

    [SimpleField(IsFilterable = true, IsSortable = true)]
    public DateTimeOffset? CreatedUtc { get; set; }

    [SimpleField(IsFilterable = true, IsSortable = true)]
    public DateTimeOffset? UpdatedUtc { get; set; }
}