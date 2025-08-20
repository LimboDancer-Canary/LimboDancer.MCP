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
            new SimpleField("tenantId", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
            new SearchField(ContentFieldName, SearchFieldDataType.String) { IsSearchable = true },
            new SearchField(TagsFieldName, SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsFilterable = true, IsFacetable = true },
            new SearchField(ExternalIdFieldName, SearchFieldDataType.String) { IsFilterable = true }
        };

        // Vector field: Collection(Single), with profile & dimensions
        fields.Add(new SearchField(VectorFieldName, SearchFieldDataType.Collection(SearchFieldDataType.Single))
        {
            // Vector-specific settings (GA API)
            VectorSearchDimensions = vectorDimensions,
            VectorSearchProfileName = VectorProfileName,
            // vectors are not searchable text; do not mark IsSearchable
            // IsStored defaults true for vectors (retrievable); leave as default
        });

        var index = new SearchIndex(indexName)
        {
            Fields = fields,
            Similarity = new BM25Similarity(), // BM25 for keyword portion
            VectorSearch = new VectorSearch
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(HnswAlgoName)
                    {
                        Parameters = new HnswParameters
                        {
                            // Sensible defaults; tune later as needed
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
