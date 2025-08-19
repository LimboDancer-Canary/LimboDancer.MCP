using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;

namespace LimboDancer.MCP.Vector.AzureSearch;

/// <summary>
/// Thin vector store over Azure AI Search with hybrid search.
/// </summary>
public sealed class VectorStore
{
    private readonly SearchClient _search;
    private readonly SearchIndexBuilder _builder;
    private readonly string _indexName;
    private readonly int _dims;

    public VectorStore(Uri endpoint, string apiKey, string indexName, int vectorDimensions)
    {
        _indexName = indexName;
        _dims = vectorDimensions;

        var indexClient = new SearchIndexClient(endpoint, new AzureKeyCredential(apiKey));
        _builder = new SearchIndexBuilder(indexClient);

        _search = new SearchClient(endpoint, indexName, new AzureKeyCredential(apiKey));
    }

    /// <summary>
    /// Ensures the index exists with the right schema.
    /// </summary>
    public Task EnsureIndexAsync(CancellationToken ct = default) =>
        _builder.EnsureIndexAsync(_indexName, _dims, ct);

    /// <summary>
    /// Upsert documents. If a document has no <see cref="MemoryDoc.Vector"/> and an embedder is provided,
    /// the embedder will be invoked to populate it.
    /// </summary>
    /// <param name="docs">Documents to upsert.</param>
    /// <param name="embedIfMissing">If true and embedder provided, fill missing vectors.</param>
    /// <param name="embedder">Delegate that returns a vector for the input text (usually doc.Content).</param>
    public async Task UpsertAsync(IEnumerable<MemoryDoc> docs,
                                  bool embedIfMissing = true,
                                  Func<MemoryDoc, Task<float[]>>? embedder = null,
                                  CancellationToken ct = default)
    {
        var list = new List<MemoryDoc>();
        foreach (var d in docs)
        {
            if (d.Vector is null && embedIfMissing && embedder is not null)
            {
                d.Vector = await embedder(d);
            }
            if (d.Vector is null)
                throw new InvalidOperationException($"Doc {d.Id} has no vector and no embedder was provided.");

            if (d.Vector.Length != _dims)
                throw new InvalidOperationException($"Doc {d.Id} vector has length {d.Vector.Length}, expected {_dims}.");

            list.Add(d);
        }

        // Merge or upload
        await _search.MergeOrUploadDocumentsAsync(list, cancellationToken: ct);
    }

    public sealed record SearchHit(MemoryDoc Doc, double Score);

    /// <summary>
    /// Hybrid search (text + optional vector). If vector is null, it's standard keyword search.
    /// </summary>
    public async Task<IReadOnlyList<SearchHit>> SearchHybridAsync(
        string? queryText,
        float[]? vector,
        int k = 5,
        string? filterOData = null,
        CancellationToken ct = default)
    {
        var options = new SearchOptions
        {
            Size = k,
            IncludeTotalCount = false
        };

        // Return the core fields; vectors usually aren't useful to retrieve
        options.Select.AddRange(new[] { "id", "content", "tags", "externalId" });

        // Scope the full-text search
        options.SearchFields.AddRange(new[] { "content", "tags", "externalId" });

        if (!string.IsNullOrWhiteSpace(filterOData))
            options.Filter = filterOData;

        // Attach vector part if provided
        if (vector is not null)
        {
            if (vector.Length != _dims)
                throw new InvalidOperationException($"Query vector length {vector.Length} != {_dims}.");

            var vq = new VectorizedQuery(vector)
            {
                KNearestNeighborsCount = k
            };
            vq.Fields.Add(SearchIndexBuilder.VectorFieldName);

            options.VectorSearch = new VectorSearchOptions();
            options.VectorSearch.Queries.Add(vq);
        }

        // For pure vector search you can pass empty string; for hybrid, pass the query text.
        var text = queryText ?? string.Empty;
        var resp = await _search.SearchAsync<SearchDocument>(text, options, ct);

        var results = new List<SearchHit>();
        await foreach (var r in resp.Value.GetResultsAsync())
        {
            var doc = new MemoryDoc
            {
                Id = (string)r.Document["id"],
                Content = r.Document.TryGetValue("content", out var c) ? c?.ToString() ?? "" : "",
                ExternalId = r.Document.TryGetValue("externalId", out var e) ? e?.ToString() : null,
                Tags = r.Document.TryGetValue("tags", out var t) ? (t as IEnumerable<object>)?.Select(o => o.ToString()!).ToArray() : null
            };
            results.Add(new SearchHit(doc, r.Score ?? 0));
        }
        return results;
    }
}
