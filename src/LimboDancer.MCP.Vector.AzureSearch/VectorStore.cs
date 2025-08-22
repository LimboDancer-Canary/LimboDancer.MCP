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
        public VectorStore(Uri endpoint, string apiKey, string? indexName = null, int vectorDimensions = 1536)
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
        public async Task<IReadOnlyList<SearchHit>> SearchHybridAsync(
            string? queryText,
            float[]? vector,
            int k = 50,
            string? filter = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(queryText) && (vector is null || vector.Length == 0))
                throw new ArgumentException("Provide queryText and/or vector.");

            var options = BuildHybridOptions(
                queryTextProvided: !string.IsNullOrWhiteSpace(queryText),
                k: k,
                top: 20,
                filter: filter,
                vector: vector
            );

            // Azure AI Search will blend scores when both Search and VectorSearch are provided.
            // We select a compact projection; adjust if you need more fields.
            var response = await Client.SearchAsync<SearchDocument>(queryText ?? "*", options, ct);

            var items = new List<SearchHit>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                items.Add(ToSearchHit(result));
            }
            return items;
        }

        /// <summary>
        /// Hybrid search with structured filters.
        /// </summary>
        public async Task<IReadOnlyList<SearchHit>> SearchHybridAsync(
            string? queryText,
            float[]? vector,
            int k,
            SearchFilters filters,
            CancellationToken ct = default)
        {
            var filterClauses = new List<string>();

            if (!string.IsNullOrWhiteSpace(filters.OntologyClass))
                filterClauses.Add($"{F.Kind} eq '{EscapeODataString(filters.OntologyClass)}'");

            if (!string.IsNullOrWhiteSpace(filters.UriEquals))
                filterClauses.Add($"{F.SourceId} eq '{EscapeODataString(filters.UriEquals)}'");

            if (filters.TagsAny?.Length > 0)
            {
                var tagFilters = filters.TagsAny.Select(t => $"{F.Tags} eq '{EscapeODataString(t)}'");
                filterClauses.Add($"({string.Join(" or ", tagFilters)})");
            }

            var filter = filterClauses.Count > 0 ? string.Join(" and ", filterClauses) : null;
            return await SearchHybridAsync(queryText, vector, k, filter, ct);
        }

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
            options.Select.Add(F.Content);
            options.Select.Add(F.SourceId);
            options.Select.Add(F.SourceType);
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

            if (!string.IsNullOrWhiteSpace(filter))
                options.Filter = filter;

            return options;
        }

        private static string EscapeODataString(string value)
            => value.Replace("'", "''");

        private static SearchHit ToSearchHit(SearchResult<SearchDocument> r)
        {
            var d = r.Document;

            string? GetString(string key) => d.TryGetValue(key, out var val) ? val as string : null;
            DateTimeOffset? GetDto(string key) => d.TryGetValue(key, out var val) ? val as DateTimeOffset? : null;
            int? GetInt(string key) => d.TryGetValue(key, out var val) && val is int i ? i : null;

            var sourceId = GetString(F.SourceId);
            string? source = null;
            int? chunk = null;

            // Parse source and chunk from sourceId pattern "source#chunk"
            if (!string.IsNullOrEmpty(sourceId))
            {
                var parts = sourceId.Split('#');
                source = parts[0];
                if (parts.Length > 1 && int.TryParse(parts[1], out var c))
                    chunk = c;
            }

            return new SearchHit
            {
                Id = GetString(F.Id)!,
                Title = GetString(F.Label),
                Source = source,
                Chunk = chunk,
                OntologyClass = GetString(F.Kind),
                Uri = GetString(F.SourceId),
                Tags = GetString(F.Tags)?.Split(',', StringSplitOptions.RemoveEmptyEntries),
                Content = GetString(F.Content),
                Score = r.Score ?? 0
            };
        }

        // -------------------------
        // Public models
        // -------------------------

        public sealed class SearchHit
        {
            public string Id { get; set; } = default!;
            public string? Title { get; set; }
            public string? Source { get; set; }
            public int? Chunk { get; set; }
            public string? OntologyClass { get; set; }
            public string? Uri { get; set; }
            public string[]? Tags { get; set; }
            public string? Content { get; set; }
            public double Score { get; set; }
        }

        public sealed class SearchFilters
        {
            public string? OntologyClass { get; set; }
            public string? UriEquals { get; set; }
            public string[]? TagsAny { get; set; }
        }
    }
}