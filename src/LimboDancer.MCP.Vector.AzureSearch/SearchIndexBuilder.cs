using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

namespace LimboDancer.MCP.Vector.AzureSearch;

public sealed class SearchIndexBuilder
{
    public const string VectorFieldName = "vector";
    public const string ContentFieldName = "content";
    public const string TagsFieldName = "tags";
    public const string ExternalIdFieldName = "externalId";
    public const string KeyFieldName = "id";

    // Also used by MemoryDoc's JsonPropertyName on TenantId
    public const string TenantFieldName = "tenantId";

    // Names for vector configuration
    private const string HnswAlgoName = "hnsw-default";
    private const string VectorProfileName = "vector-profile";

    private readonly SearchIndexClient _indexClient;

    public SearchIndexBuilder(Uri endpoint, string apiKey)
    {
        _indexClient = new SearchIndexClient(endpoint, new AzureKeyCredential(apiKey));
    }

    public SearchIndexBuilder(SearchIndexClient indexClient) => _indexClient = indexClient;

    /// <summary>Create or update an index with BM25 + vector field.</summary>
    public async Task EnsureIndexAsync(string indexName, int vectorDimensions, CancellationToken ct = default)
    {
        var fields = new List<SearchField>
        {
            // Required key field
            new SimpleField(KeyFieldName, SearchFieldDataType.String)
            {
                IsKey = true,
                IsFilterable = true
            },

            // Tenant scoping field: filterable/facetable
            new SimpleField(TenantFieldName, SearchFieldDataType.String)
            {
                IsFilterable = true,
                IsFacetable = true
            },

            // Content fields
            new SearchField(ContentFieldName, SearchFieldDataType.String) { IsSearchable = true },
            new SearchField(TagsFieldName, SearchFieldDataType.Collection(SearchFieldDataType.String))
            {
                IsFilterable = true,
                IsFacetable = true
            },
            new SearchField(ExternalIdFieldName, SearchFieldDataType.String) { IsFilterable = true }
        };

        // Vector field: Collection(Single), with profile & dimensions
        fields.Add(new SearchField(VectorFieldName, SearchFieldDataType.Collection(SearchFieldDataType.Single))
        {
            VectorSearchDimensions = vectorDimensions,
            VectorSearchProfileName = VectorProfileName
        });

        var index = new SearchIndex(indexName)
        {
            Fields = fields,
            Similarity = new BM25Similarity(),
            VectorSearch = new VectorSearch
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(HnswAlgoName)
                    {
                        Parameters = new HnswParameters
                        {
                            M = 16,
                            EfConstruction = 400,
                            EfSearch = 100
                        }
                    }
                },
                Profiles =
                {
                    new VectorSearchProfile(VectorProfileName, HnswAlgoName)
                }
            }
        };

        // Create or update
        await _indexClient.CreateOrUpdateIndexAsync(index, ct);
    }
}