// File: /src/LimboDancer.MCP.Vector.AzureSearch/SearchIndexBuilder.cs
// Purpose: 
//   Creates and manages Azure AI Search index schema using MemoryDoc as the unified model.
//   Updated for current Azure.Search.Documents SDK API.

using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using MemoryDoc = LimboDancer.MCP.Vector.AzureSearch.Models.MemoryDoc;

namespace LimboDancer.MCP.Vector.AzureSearch
{
    public static class SearchIndexBuilder
    {
        public const string DefaultIndexName = "ldm-memory";
        public const string DefaultVectorProfile = "ldm-vector-profile";
        public const string DefaultSemanticConfig = "ldm-semantic";
        public const int DefaultVectorDimensions = 1536;

        public static Task EnsureIndexAsync(
            SearchIndexClient client,
            string? indexName = null,
            int vectorDimensions = DefaultVectorDimensions,
            string? semanticConfigName = null,
            CancellationToken ct = default)
        {
            var adapter = new AdminClientAdapter(client);
            return EnsureIndexCoreAsync(adapter, indexName, vectorDimensions, semanticConfigName, ct);
        }

        // Internal for tests
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
            public Task<Response<SearchIndex>> CreateOrUpdateIndexAsync(SearchIndex index, CancellationToken ct) => _client.CreateOrUpdateIndexAsync(index, cancellationToken: ct);
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
            index.Fields = new FieldBuilder().Build(typeof(MemoryDoc));

            // Configure vector search
            var algoName = "hnsw-default";
            index.VectorSearch ??= new VectorSearch();
            index.VectorSearch.Algorithms.Clear();
            index.VectorSearch.Profiles.Clear();
            index.VectorSearch.Algorithms.Add(new HnswAlgorithmConfiguration(algoName));
            index.VectorSearch.Profiles.Add(new VectorSearchProfile(DefaultVectorProfile, algoName));

            // Find and configure the vector field
            var vectorField = index.Fields.FirstOrDefault(f => f.Name == nameof(MemoryDoc.ContentVector)) as SearchField;
            if (vectorField != null)
            {
                vectorField.IsSearchable = false; // Vector fields should not be searchable
                vectorField.VectorSearchDimensions = vectorDimensions;
                // VectorSearchProfileName is set via the VectorSearchProfile configuration above
            }

            // Configure semantic search
            index.SemanticSearch ??= new SemanticSearch();
            index.SemanticSearch.Configurations.Clear();

            var semanticConfig = new SemanticConfiguration(
                semanticConfigName,
                new SemanticPrioritizedFields()
                {
                    TitleField = new SemanticField(nameof(MemoryDoc.Label)),
                    ContentFields =
                    {
                        new SemanticField(nameof(MemoryDoc.Content))
                    },
                    KeywordsFields =
                    {
                        new SemanticField(nameof(MemoryDoc.Tags))
                    }
                });

            index.SemanticSearch.Configurations.Add(semanticConfig);
        }
    }
}