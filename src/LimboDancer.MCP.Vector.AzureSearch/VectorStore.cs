// File: /src/LimboDancer.MCP.Vector.AzureSearch/VectorStore.cs
// Purpose:
//   Unified Azure AI Search client wrapper for memory docs with hybrid (text + vector) search.
//   Updated for current Azure.Search.Documents SDK API.

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
        /// Convenience ctor for quick testing/demos.
        /// </summary>
        public VectorStore(Uri endpoint, AzureKeyCredential key, string? indexName = null)
        {
            IndexName = indexName ?? DefaultIndexName;
            Client = new SearchClient(endpoint, IndexName, key);
        }

        // -------------------------
        // Upsert/Delete
        // -------------------------

        public async Task<int> UpsertDocsAsync(IEnumerable<MemoryDoc> docs, CancellationToken ct = default)
        {
            if (docs is null) return 0;

            var batch = IndexDocumentsBatch.Create<SearchDocument>();
            var count = 0;

            foreach (var d in docs)
            {
                d.Validate();
                batch.Actions.Add(DocToIndexAction(d));
                count++;
            }

            if (count == 0) return 0;

            var result = await Client.IndexDocumentsAsync(batch, cancellationToken: ct);
            return result.Value.Results.Count(r => r.Succeeded);
        }

        public async Task<bool> DeleteDocAsync(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;

            var batch = IndexDocumentsBatch.Delete(F.Id, new[] { id });
            var result = await Client.IndexDocumentsAsync(batch, cancellationToken: ct);
            return result.Value.Results.FirstOrDefault()?.Succeeded ?? false;
        }

        // -------------------------
        // Hybrid Search
        // -------------------------

        /// <summary>
        /// Combined text (BM25/semantic) + vector search.
        /// </summary>
        public async Task<IReadOnlyList<SearchHit>> SearchHybridAsync(
            string? queryText,
            float[]? queryVector,
            int k = 10,
            string? filterOData = null,
            CancellationToken ct = default)
        {
            // For Azure Search, we need at least one of text or vector
            if (string.IsNullOrWhiteSpace(queryText) && queryVector is null)
                return Array.Empty<SearchHit>();

            var searchText = string.IsNullOrWhiteSpace(queryText) ? "*" : queryText;
            var options = BuildHybridOptions(!string.IsNullOrWhiteSpace(queryText), k, k * 2, filterOData, queryVector);

            var response = await Client.SearchAsync<SearchDocument>(searchText, options, ct);
            var hits = new List<SearchHit>();

            await foreach (var result in response.Value.GetResultsAsync())
            {
                hits.Add(ToSearchHit(result));
            }

            return hits;
        }

        /// <summary>
        /// Convenience overload with structured filters.
        /// </summary>
        public Task<IReadOnlyList<SearchHit>> SearchHybridAsync(
            string? queryText,
            float[]? queryVector,
            int k,
            SearchFilters filters,
            CancellationToken ct = default)
        {
            var filter = BuildFilterString(filters);
            return SearchHybridAsync(queryText, queryVector, k, filter, ct);
        }

        // -------------------------
        // Filter builder
        // -------------------------

        public static string BuildFilterString(SearchFilters filters)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(filters.TenantId))
                parts.Add($"{F.TenantId} eq '{EscapeODataString(filters.TenantId)}'");

            if (!string.IsNullOrWhiteSpace(filters.OntologyClass))
                parts.Add($"{F.Kind} eq '{EscapeODataString(filters.OntologyClass)}'");

            if (!string.IsNullOrWhiteSpace(filters.UriEquals))
                parts.Add($"{F.SourceId} eq '{EscapeODataString(filters.UriEquals)}'");

            if (filters.TagsAny?.Length > 0)
            {
                var tagFilters = filters.TagsAny.Select(t => $"search.ismatch('{EscapeODataString(t)}', '{F.Tags}')");
                parts.Add($"({string.Join(" or ", tagFilters)})");
            }

            return parts.Count == 0 ? string.Empty : string.Join(" and ", parts);
        }

        // -------------------------
        // DTOs
        // -------------------------

        public sealed record SearchHit(
            string Id,
            string? Title,
            string? Source,
            int? Chunk,
            string? OntologyClass,
            string? Uri,
            string[]? Tags,
            string? Content,
            double Score
        );

        public sealed class SearchFilters
        {
            public string? TenantId { get; init; }
            public string? OntologyClass { get; init; }
            public string? UriEquals { get; init; }
            public string[]? TagsAny { get; init; }
        }

        // -------------------------
        // Helpers
        // -------------------------

        private static IndexDocumentsAction<SearchDocument> DocToIndexAction(MemoryDoc d)
        {
            var doc = new SearchDocument
            {
                [F.Id] = d.Id ?? string.Empty,
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
                    SemanticConfigurationName = DefaultSemanticConfig
                };
            }

            // Vector query (hybrid when combined with text)
            if (vector != null && vector.Length > 0)
            {
                options.VectorSearch = new VectorSearchOptions();

                var vectorQuery = new VectorizedQuery(vector)
                {
                    KNearestNeighborsCount = k > 0 ? k : 50,
                    // Fields must be set in constructor or via separate method
                };
                vectorQuery.Fields.Add(F.ContentVector);

                options.VectorSearch.Queries.Add(vectorQuery);
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

            string? GetString(string key) => d.TryGetValue(key, out var val) ? val?.ToString() : null;
            int? GetInt(string key) => d.TryGetValue(key, out var val) && val is int i ? i : null;
            string[]? GetTags(string key)
            {
                var str = GetString(key);
                return string.IsNullOrWhiteSpace(str) ? null : str.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }

            return new SearchHit(
                Id: GetString(F.Id) ?? "?",
                Title: GetString(F.Label),
                Source: GetString(F.SourceId),
                Chunk: GetInt("chunk"),
                OntologyClass: GetString(F.Kind),
                Uri: GetString(F.SourceId),
                Tags: GetTags(F.Tags),
                Content: GetString(F.Content),
                Score: r.Score ?? 0.0
            );
        }
    }
}