// File: /src/LimboDancer.MCP.Vector.AzureSearch/VectorStore.cs
// Purpose:
//   Unified Azure AI Search client wrapper for memory docs with hybrid (text + vector) search.
//   Now aligned with MemoryDoc as the single source of truth for field names and schema.
//
// Highlights:
//   - Constructors: (a) SearchClient, (b) endpoint+key+(indexName)
//   - Hybrid search: lex/semantic + vector query combined in one call
//   - Tenant scoping: mandatory filter (when provided)
//   - Field names derived directly from MemoryDoc properties via nameof()
//   - Removed duplicate MemoryIndexDocument class
//
// Dependencies: Azure.Search.Documents (v11+)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using MemoryDoc = LimboDancer.MCP.Vector.AzureSearch.Models.MemoryDoc;

namespace LimboDancer.MCP.Vector.AzureSearch
{
    public sealed class VectorStore
    {
        public string IndexName { get; }
        public SearchClient Client { get; }

        // Keep parity with SearchIndexBuilder
        private const string DefaultIndexName = SearchIndexBuilder.DefaultIndexName;
        private const string DefaultVectorProfile = SearchIndexBuilder.DefaultVectorProfile;
        private const string DefaultSemanticConfig = SearchIndexBuilder.DefaultSemanticConfig;

        // Index field names (single source of truth; derived from MemoryDoc properties)
        private static class F
        {
            public const string Id = nameof(MemoryDoc.Id);
            public const string TenantId = nameof(MemoryDoc.TenantId);
            public const string Label = nameof(MemoryDoc.Label);
            public const string Kind = nameof(MemoryDoc.Kind);
            public const string Status = nameof(MemoryDoc.Status);
            public const string Tags = nameof(MemoryDoc.Tags);
            public const string SourceId = nameof(MemoryDoc.SourceId);
            public const string SourceType = nameof(MemoryDoc.SourceType);
            public const string Content = nameof(MemoryDoc.Content);
            public const string ContentVector = nameof(MemoryDoc.ContentVector);
            public const string CreatedUtc = nameof(MemoryDoc.CreatedUtc);
            public const string UpdatedUtc = nameof(MemoryDoc.UpdatedUtc);
        }

        // -------------------------
        // Constructors
        // -------------------------

        /// <summary>
        /// Server/CLI can pass an already-constructed SearchClient (recommended for DI).
        /// </summary>
        public VectorStore(SearchClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            IndexName = client.IndexName;
        }

        /// <summary>
        /// Convenience for CLI/server: build from endpoint+apiKey; optional explicit index name.
        /// </summary>
        public VectorStore(Uri endpoint, string apiKey, string? indexName = null)
        {
            if (endpoint is null) throw new ArgumentNullException(nameof(endpoint));
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentNullException(nameof(apiKey));

            IndexName = string.IsNullOrWhiteSpace(indexName) ? DefaultIndexName : indexName!;
            Client = new SearchClient(endpoint, IndexName, new AzureKeyCredential(apiKey));
        }

        // -------------------------
        // Public API
        // -------------------------

        /// <summary>
        /// Upsert (upload or merge) a batch of documents. All docs should carry TenantId.
        /// </summary>
        public async Task UploadAsync(IEnumerable<MemoryDoc> docs, CancellationToken ct = default)
        {
            var batch = IndexDocumentsBatch.Upload(docs.Select(ToIndexModel));
            await Client.IndexDocumentsAsync(batch, ct);
        }

        /// <summary>
        /// Delete a batch of documents by Id.
        /// </summary>
        public async Task DeleteAsync(IEnumerable<string> ids, CancellationToken ct = default)
        {
            var keys = ids.Select(id => new { Id = id }).ToArray();
            var batch = IndexDocumentsBatch.Delete(keys);
            await Client.IndexDocumentsAsync(batch, ct);
        }

        /// <summary>
        /// Hybrid search: combines textual (semantic) query and vector KNN on Content/ContentVector.
        /// Pass either/both of (queryText, vector). If both are null/empty, throws.
        /// </summary>
        /// <param name="tenantId">Optional tenant scope. If provided, enforced as a filter.</param>
        /// <param name="queryText">Lexical/semantic query text (optional).</param>
        /// <param name="vector">Embedding for vector similarity (optional).</param>
        /// <param name="k">KNN neighbors to consider for vector scoring (default 50).</param>
        /// <param name="top">How many results to return (default 20, max 1000).</param>
        /// <param name="filter">Additional OData filter, ANDed with tenant filter.</param>
        public async Task<IReadOnlyList<SearchResultItem>> HybridSearchAsync(
            string? tenantId,
            string? queryText,
            float[]? vector,
            int k = 50,
            int top = 20,
            string? filter = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(queryText) && (vector is null || vector.Length == 0))
                throw new ArgumentException("Provide queryText and/or vector.");

            var options = BuildHybridOptions(
                tenantId: tenantId,
                queryTextProvided: !string.IsNullOrWhiteSpace(queryText),
                k: k,
                top: top,
                filter: filter,
                vector: vector
            );

            // Azure AI Search will blend scores when both Search and VectorSearch are provided.
            // We select a compact projection; adjust if you need more fields.
            var response = await Client.SearchAsync<SearchDocument>(queryText ?? "*", options, ct);

            var items = new List<SearchResultItem>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                items.Add(ToItem(result));
            }
            return items;
        }

        /// <summary>
        /// Simple text-only semantic search (no vector).
        /// </summary>
        public Task<IReadOnlyList<SearchResultItem>> TextSearchAsync(
            string? tenantId,
            string queryText,
            int top = 20,
            string? filter = null,
            CancellationToken ct = default)
            => HybridSearchAsync(tenantId, queryText, vector: null, k: 0, top: top, filter: filter, ct);

        /// <summary>
        /// Vector-only KNN search (no text).
        /// </summary>
        public Task<IReadOnlyList<SearchResultItem>> VectorSearchAsync(
            string? tenantId,
            float[] vector,
            int k = 50,
            int top = 20,
            string? filter = null,
            CancellationToken ct = default)
            => HybridSearchAsync(tenantId, queryText: null, vector: vector, k: k, top: top, filter: filter, ct);

        // -------------------------
        // Internal helpers
        // -------------------------

        private static IndexDocumentsAction<SearchDocument> ToIndexModel(MemoryDoc d)
        {
            // Ensure required fields exist; Azure Search ignores nulls for some attributes but key is mandatory
            var doc = new SearchDocument
            {
                [F.Id] = d.Id,
                [F.TenantId] = d.TenantId ?? string.Empty,
                [F.Label] = d.Label ?? string.Empty,
                [F.Kind] = d.Kind ?? string.Empty,
                [F.Status] = d.Status ?? string.Empty,
                [F.Tags] = d.Tags ?? string.Empty,
                [F.SourceId] = d.SourceId,
                [F.SourceType] = d.SourceType,
                [F.Content] = d.Content ?? string.Empty,
                [F.ContentVector] = d.ContentVector ?? Array.Empty<float>(),
                [F.CreatedUtc] = d.CreatedUtc,
                [F.UpdatedUtc] = d.UpdatedUtc
            };

            return IndexDocumentsAction.Upload(doc);
        }

        private static SearchOptions BuildHybridOptions(
            string? tenantId,
            bool queryTextProvided,
            int k,
            int top,
            string? filter,
            float[]? vector)
        {
            var options = new SearchOptions
            {
                Size = Math.Clamp(top, 1, 1000),
                IncludeTotalCount = false
            };

            // Projection
            options.Select.Add(F.Id);
            options.Select.Add(F.TenantId);
            options.Select.Add(F.Label);
            options.Select.Add(F.Kind);
            options.Select.Add(F.Status);
            options.Select.Add(F.Tags);
            options.Select.Add(F.CreatedUtc);
            options.Select.Add(F.UpdatedUtc);

            // Semantic when text query exists
            if (queryTextProvided)
            {
                options.QueryType = SearchQueryType.Semantic;
                options.SemanticSearch = new SemanticSearchOptions
                {
                    ConfigurationName = DefaultSemanticConfig
                };
            }

            // Vector query (hybrid when combined with text)
            if (vector is not null && vector.Length > 0)
            {
                options.VectorSearch = new()
                {
                    Queries =
                    {
                        new VectorQuery
                        {
                            Vector = vector,
                            KNearestNeighborsCount = k > 0 ? k : 50,
                            Fields = F.ContentVector,
                            // Ensure this matches SearchIndexBuilder profile
                            Profile = DefaultVectorProfile
                        }
                    }
                };
            }

            // Tenant filter + additional filter
            var filters = new List<string>();
            if (!string.IsNullOrWhiteSpace(tenantId))
                filters.Add($"{F.TenantId} eq '{EscapeODataString(tenantId)}'");

            if (!string.IsNullOrWhiteSpace(filter))
                filters.Add($"({filter})");

            if (filters.Count > 0)
                options.Filter = string.Join(" and ", filters);

            return options;
        }

        private static string EscapeODataString(string value)
            => value.Replace("'", "''");

        private static SearchResultItem ToItem(SearchResult<SearchDocument> r)
        {
            var d = r.Document;

            string? GetString(string key) => d.TryGetValue(key, out var val) ? val as string : null;
            DateTimeOffset? GetDto(string key) => d.TryGetValue(key, out var val) ? val as DateTimeOffset? : null;

            return new SearchResultItem
            {
                Id = GetString(F.Id)!,
                TenantId = GetString(F.TenantId),
                Label = GetString(F.Label),
                Kind = GetString(F.Kind),
                Status = GetString(F.Status),
                Tags = GetString(F.Tags),
                CreatedUtc = GetDto(F.CreatedUtc),
                UpdatedUtc = GetDto(F.UpdatedUtc),
                Score = r.Score
            };
        }

        // -------------------------
        // Public models
        // -------------------------

        public sealed class SearchResultItem
        {
            public string Id { get; set; } = default!;
            public string? TenantId { get; set; }
            public string? Label { get; set; }
            public string? Kind { get; set; }
            public string? Status { get; set; }
            public string? Tags { get; set; }
            public DateTimeOffset? CreatedUtc { get; set; }
            public DateTimeOffset? UpdatedUtc { get; set; }
            public double? Score { get; set; }
        }
    }
}
