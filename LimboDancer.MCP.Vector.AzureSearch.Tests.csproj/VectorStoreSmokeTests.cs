using FluentAssertions;
using LimboDancer.MCP.Vector.AzureSearch;
using System.Security.Cryptography;
using Xunit;

public class VectorStoreSmokeTests
{
    // Set these in your environment to run against a live service.
    private static readonly string? Endpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_ENDPOINT");
    private static readonly string? ApiKey = Environment.GetEnvironmentVariable("AZURE_SEARCH_API_KEY");

    // Keep this small for a fast/randomized smoke run
    private const int Dims = 32;

    private static bool Configured => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ApiKey);

    private static float[] DeterministicRandomVector(string seed, int dims)
    {
        // Deterministic pseudo-random per seed for stability across runs
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(seed));
        var r = new Random(BitConverter.ToInt32(hash, 0));
        var v = new float[dims];
        for (int i = 0; i < dims; i++)
            v[i] = (float)(r.NextDouble() * 2.0 - 1.0); // [-1, 1]
        return v;
    }

    [Fact(Skip = "Set AZURE_SEARCH_ENDPOINT and AZURE_SEARCH_API_KEY to run this smoke test against your service.")]
    public async Task HybridSearch_Returns_UpsertedDocs()
    {
        if (!Configured)
            return;

        var endpoint = new Uri(Endpoint!);
        var indexName = "ldmcp-smoke-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        var store = new VectorStore(endpoint, ApiKey!, indexName, Dims);

        await store.EnsureIndexAsync();

        var docs = new[]
        {
            new MemoryDoc { Id = "1", Content = "The quick brown fox jumps over the lazy dog", Tags = new[] {"animal","pangram"}, ExternalId = "fox-1", Vector = DeterministicRandomVector("1", Dims) },
            new MemoryDoc { Id = "2", Content = "Azure AI Search supports hybrid search with BM25 and vector similarity", Tags = new[] {"azure","search"}, ExternalId = "doc-2", Vector = DeterministicRandomVector("2", Dims) }
        };

        // Upsert with existing vectors (delegate not needed for this smoke)
        await store.UpsertAsync(docs, embedIfMissing: false);

        // Small retry loop to allow near‑real‑time commit
        IReadOnlyList<VectorStore.SearchHit>? hits = null;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            hits = await store.SearchHybridAsync(
                queryText: "hybrid search vector BM25 brown fox", // text will match both docs
                vector: DeterministicRandomVector("query", Dims),  // random vector part (still hybrid)
                k: 5);

            if (hits.Count >= 2) break;
            await Task.Delay(500);
        }

        hits.Should().NotBeNull();
        hits!.Select(h => h.Doc.Id).Should().Contain(new[] { "1", "2" });
    }
}
