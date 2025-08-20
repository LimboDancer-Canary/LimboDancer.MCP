// File: /src/LimboDancer.MCP.Vector.AzureSearch/SearchIndexBuilder.cs
// (UPDATED) Adds an internal adapter interface so unit tests can verify the built index.

using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace LimboDancer.MCP.Vector.AzureSearch
{
    public static class SearchIndexBuilder
    {
        public const string DefaultIndexName = "ldm-memory";
        public const string DefaultVectorProfile = "ldm-vector-profile";
        public const string DefaultSemanticConfig = "ldm-semantic";

        // ---- Public API remains the same ----
        public static Task EnsureIndexAsync(
            SearchIndexClient client,
            string? indexName = null,
            int vectorDimensions = 1536,
            string? semanticConfigName = null,
            CancellationToken ct = default)
        {
            var adapter = new AdminClientAdapter(client);
            return EnsureIndexCoreAsync(adapter, indexName, vectorDimensions, semanticConfigName, ct);
        }

        // ---- Internal for tests ----
        internal interface IIndexAdminClient
        {
            Task<Response<SearchIndex>> GetIndexAsync(string name, CancellationToken ct);
            Task<Response<SearchIndex>> CreateOrUpdateIndexAsync(SearchIndex index, CancellationToken ct);
        }

        internal sealed class AdminClientAdapter : IIndexAdminClient
        {
            private readonly SearchIndexClient _client;
            public AdminClientAdapter(SearchIndexClient client) => _client = client;
            public Task<Response<SearchIndex>> GetIndexAsync(string name, CancellationToken ct) => _client.GetIndexAsync(name, ct);
            public Task<Response<SearchIndex>> CreateOrUpdateIndexAsync(SearchIndex index, CancellationToken ct) => _client.CreateOrUpdateIndexAsync(index, ct: ct);
        }

        internal static async Task EnsureIndexCoreAsync(
            IIndexAdminClient admin,
            string? indexName,
            int vectorDimensions,
            string? semanticConfigName,
            CancellationToken ct)
        {
            indexName ??= DefaultIndexName;
            semanticConfigName ??= DefaultSemanticConfig;

            SearchIndex index;
            try
            {
                var existing = await admin.GetIndexAsync(indexName, ct);
                index = existing.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                index = new SearchIndex(indexName);
            }

            ApplySchema(index, vectorDimensions, semanticConfigName);
            await admin.CreateOrUpdateIndexAsync(index, ct);
        }

        private static void ApplySchema(SearchIndex index, int vectorDimensions, string semanticConfigName)
        {
            index.Fields = new FieldBuilder().Build(typeof(MemoryIndexDocument));

            var algoName = "hnsw-default";
            index.VectorSearch ??= new VectorSearch();
            index.VectorSearch.Algorithms.Clear();
            index.VectorSearch.Profiles.Clear();
            index.VectorSearch.Algorithms.Add(new HnswAlgorithmConfiguration(algoName));
            index.VectorSearch.Profiles.Add(new VectorSearchProfile(DefaultVectorProfile, algoName));

            var vectorField = index.GetField(nameof(MemoryIndexDocument.ContentVector)) as SearchField;
            if (vectorField is not null)
            {
                vectorField.VectorSearchProfileName = DefaultVectorProfile;
                vectorField.VectorSearchDimensions = vectorDimensions;
            }

            index.SemanticSettings ??= new SemanticSettings();
            index.SemanticSettings.Configurations.Clear();
            index.SemanticSettings.Configurations.Add(new SemanticConfiguration(
                semanticConfigName,
                new PrioritizedFields
                {
                    TitleField = new SemanticField(nameof(MemoryIndexDocument.Label)),
                    ContentFields =
                    {
                        new SemanticField(nameof(MemoryIndexDocument.Content)),
                        new SemanticField(nameof(MemoryIndexDocument.Kind)),
                        new SemanticField(nameof(MemoryIndexDocument.Status))
                    },
                    KeywordsFields =
                    {
                        new SemanticField(nameof(MemoryIndexDocument.Tags))
                    }
                }));
        }

        private sealed class MemoryIndexDocument
        {
            [SimpleField(IsKey = true, IsFilterable = true)] public string Id { get; set; } = default!;
            [SimpleField(IsFilterable = true, IsFacetable = true)] public string TenantId { get; set; } = default!;
            [SearchableField(IsFilterable = true, IsSortable = true)] public string Label { get; set; } = default!;
            [SimpleField(IsFilterable = true, IsFacetable = true)] public string Kind { get; set; } = default!;
            [SimpleField(IsFilterable = true, IsFacetable = true)] public string Status { get; set; } = default!;
            [SearchableField(IsFilterable = true, IsFacetable = true)] public string? Tags { get; set; }
            [SimpleField(IsFilterable = true)] public string? SourceId { get; set; }
            [SimpleField(IsFilterable = true)] public string? SourceType { get; set; }
            [SearchableField] public string Content { get; set; } = default!;
            [SearchField(VectorSearchDimensions = 1536, VectorSearchProfileName = DefaultVectorProfile, DataType = SearchFieldDataType.Collection(SearchFieldDataType.Single))]
            public float[] ContentVector { get; set; } = Array.Empty<float>();
            [SimpleField(IsFilterable = true, IsSortable = true)] public DateTimeOffset? CreatedUtc { get; set; }
            [SimpleField(IsFilterable = true, IsSortable = true)] public DateTimeOffset? UpdatedUtc { get; set; }
        }
    }
}
