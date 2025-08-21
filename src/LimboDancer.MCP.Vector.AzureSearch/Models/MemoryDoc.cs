// File: /src/LimboDancer.MCP.Vector.AzureSearch/Models/MemoryDoc.cs
// Purpose:
//   Unified model for vector index ingestion/search with strict multi‑tenancy and Azure Search integration.
//   TenantId is REQUIRED. All ingestion paths must provide it.
//   This model now serves as the single source of truth for both runtime operations and index schema generation.
//
// Notes:
//   - Uses System.Text.Json (not Newtonsoft).
//   - Provides a JsonConstructor that enforces Id + TenantId.
//   - Includes Azure.Search.Documents attributes for direct FieldBuilder usage.
//   - Added SourceId/SourceType for provenance tracking (nullable, filterable).
//   - Includes Validate() helper for early guardrails in ingestion pipelines.
//   - Field names and attributes align with SearchIndexBuilder requirements.

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace LimboDancer.MCP.Vector.AzureSearch.Models
{
    /// <summary>
    /// Ingest/search document for the Azure AI Search index.
    /// </summary>
    public sealed class MemoryDoc
    {
        // Constants for Azure Search integration
        internal const string DefaultVectorProfile = "ldm-vector-profile";

        /// <summary>
        /// Globally unique identifier for the document (index key).
        /// </summary>
        [JsonPropertyName("id")]
        [Required]
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; init; }

        /// <summary>
        /// REQUIRED tenant discriminator. All queries/ingestion enforce/assume this is set.
        /// </summary>
        [JsonPropertyName("tenantId")]
        [Required]
        [SimpleField(IsFilterable = true, IsFacetable = true)]
        public string TenantId { get; init; }

        /// <summary>
        /// Human-friendly title/label for the content.
        /// </summary>
        [JsonPropertyName("label")]
        [SearchableField(IsFilterable = true, IsSortable = true)]
        public string? Label { get; set; }

        /// <summary>
        /// Content kind/category (aligns with filters/facets).
        /// </summary>
        [JsonPropertyName("kind")]
        [SimpleField(IsFilterable = true, IsFacetable = true)]
        public string? Kind { get; set; }

        /// <summary>
        /// Status (e.g., active, draft).
        /// </summary>
        [JsonPropertyName("status")]
        [SimpleField(IsFilterable = true, IsFacetable = true)]
        public string? Status { get; set; }

        /// <summary>
        /// Optional tags (string; join small lists if desired).
        /// </summary>
        [JsonPropertyName("tags")]
        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? Tags { get; set; }

        /// <summary>
        /// Optional source identifier for provenance tracking.
        /// </summary>
        [JsonPropertyName("sourceId")]
        [SimpleField(IsFilterable = true)]
        public string? SourceId { get; set; }

        /// <summary>
        /// Optional source type for provenance tracking.
        /// </summary>
        [JsonPropertyName("sourceType")]
        [SimpleField(IsFilterable = true)]
        public string? SourceType { get; set; }

        /// <summary>
        /// Primary textual content (used by lexical/semantic search).
        /// </summary>
        [JsonPropertyName("content")]
        [Required]
        [SearchableField]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Embedding vector for the content. Dimension must match the index profile.
        /// </summary>
        [JsonPropertyName("contentVector")]
        [SearchField(VectorSearchDimensions = 1536, VectorSearchProfileName = DefaultVectorProfile, DataType = SearchFieldDataType.Collection(SearchFieldDataType.Single))]
        public float[] ContentVector { get; set; } = Array.Empty<float>();

        /// <summary>
        /// Optional creation timestamp (UTC).
        /// </summary>
        [JsonPropertyName("createdUtc")]
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTimeOffset? CreatedUtc { get; set; }

        /// <summary>
        /// Optional update timestamp (UTC).
        /// </summary>
        [JsonPropertyName("updatedUtc")]
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTimeOffset? UpdatedUtc { get; set; }

        /// <summary>
        /// Strict JSON constructor: enforces non-empty Id and TenantId at deserialization time.
        /// </summary>
        [JsonConstructor]
        public MemoryDoc(
            string id,
            string tenantId,
            string? label = null,
            string? kind = null,
            string? status = null,
            string? tags = null,
            string? sourceId = null,
            string? sourceType = null,
            string? content = null,
            float[]? contentVector = null,
            DateTimeOffset? createdUtc = null,
            DateTimeOffset? updatedUtc = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("TenantId is required.", nameof(tenantId));

            Id = id;
            TenantId = tenantId;
            Label = label;
            Kind = kind;
            Status = status;
            Tags = tags;
            SourceId = sourceId;
            SourceType = sourceType;
            Content = content ?? string.Empty;
            ContentVector = contentVector ?? Array.Empty<float>();
            CreatedUtc = createdUtc;
            UpdatedUtc = updatedUtc;
        }

        /// <summary>
        /// Convenience factory for code paths that construct docs programmatically.
        /// Enforces TenantId and sets sensible defaults.
        /// </summary>
        public static MemoryDoc Create(
            string id,
            string tenantId,
            string content,
            float[]? contentVector = null,
            string? label = null,
            string? kind = null,
            string? status = null,
            string? tags = null,
            string? sourceId = null,
            string? sourceType = null,
            DateTimeOffset? createdUtc = null,
            DateTimeOffset? updatedUtc = null)
            => new(
                id: id,
                tenantId: tenantId,
                label: label,
                kind: kind,
                status: status,
                tags: tags,
                sourceId: sourceId,
                sourceType: sourceType,
                content: content ?? string.Empty,
                contentVector: contentVector ?? Array.Empty<float>(),
                createdUtc: createdUtc,
                updatedUtc: updatedUtc
            );

        /// <summary>
        /// Validate required fields and common invariants.
        /// Throw ArgumentException with a concise reason when invalid.
        /// Call this in all ingestion paths before indexing.
        /// </summary>
        public void Validate(int? expectedVectorDimensions = null)
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("MemoryDoc.Id is required.");
            if (string.IsNullOrWhiteSpace(TenantId))
                throw new ArgumentException("MemoryDoc.TenantId is required.");
            if (Content is null)
                throw new ArgumentException("MemoryDoc.Content must not be null (use empty string for none).");

            if (expectedVectorDimensions.HasValue && ContentVector.Length > 0 && ContentVector.Length != expectedVectorDimensions.Value)
            {
                throw new ArgumentException(
                    $"MemoryDoc.ContentVector length {ContentVector.Length} does not match expected {expectedVectorDimensions.Value}.");
            }
        }
    }

    /// <summary>
    /// Guard helpers for ingestion code paths to ensure TenantId is present.
    /// </summary>
    public static class MemoryDocGuards
    {
        /// <summary>
        /// Ensure the document has a non-empty TenantId; throw if missing.
        /// </summary>
        public static MemoryDoc EnsureTenant(this MemoryDoc doc)
        {
            if (doc is null) throw new ArgumentNullException(nameof(doc));
            if (string.IsNullOrWhiteSpace(doc.TenantId))
                throw new ArgumentException("TenantId is required on MemoryDoc before ingestion.");
            return doc;
        }

        /// <summary>
        /// Validate a batch before ingestion. Optionally enforce embedding dimension.
        /// </summary>
        public static void ValidateBatch(this ReadOnlySpan<MemoryDoc> docs, int? expectedVectorDimensions = null)
        {
            for (var i = 0; i < docs.Length; i++)
            {
                docs[i].Validate(expectedVectorDimensions);
            }
        }
    }
}
