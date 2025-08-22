using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.Vector.AzureSearch;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LimboDancer.MCP.McpServer.Tools;

public sealed class MemorySearchInput
{
    [JsonPropertyName("queryText")] public string? QueryText { get; set; }
    [JsonPropertyName("vectorBase64")] public string? VectorBase64 { get; set; }
    [JsonPropertyName("k")] public int K { get; set; } = 8;
    [JsonPropertyName("ontologyClass")] public string? OntologyClass { get; set; }
    [JsonPropertyName("uriEquals")] public string? UriEquals { get; set; }
    [JsonPropertyName("tagsAny")] public List<string>? TagsAny { get; set; }
}

public sealed class MemorySearchOutput
{
    [JsonPropertyName("tenantId")] public string TenantId { get; set; } = default!;
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("items")] public List<MemorySearchItem> Items { get; set; } = new();
}

public sealed class MemorySearchItem
{
    [JsonPropertyName("id")] public string Id { get; set; } = default!;
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("source")] public string? Source { get; set; }
    [JsonPropertyName("chunk")] public int? Chunk { get; set; }
    [JsonPropertyName("ontologyClass")] public string? OntologyClass { get; set; }
    [JsonPropertyName("uri")] public string? Uri { get; set; }
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    [JsonPropertyName("score")] public double Score { get; set; }
    [JsonPropertyName("preview")] public string? Preview { get; set; }
}

public sealed class MemorySearchTool
{
    private readonly VectorStore _vector;
    private readonly ITenantAccessor _tenant;
    private readonly ILogger<MemorySearchTool> _logger;

    public MemorySearchTool(VectorStore vector, ITenantAccessor tenant, ILogger<MemorySearchTool> logger)
    {
        _vector = vector ?? throw new ArgumentNullException(nameof(vector));
        _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public static string ToolSchema =>
        "{\n" +
        $"  \"@context\": {ToolSchemas.JsonLdContext},\n" +
        "  \"@id\": \"ldm:tool/MemorySearch\",\n" +
        "  \"title\": \"Search memory index\",\n" +
        "  \"description\": \"Hybrid search over memory index (BM25 + vector). Tenancy enforced by server.\",\n" +
        "  \"input\": {\n" +
        "    \"type\": \"object\",\n" +
        "    \"properties\": {\n" +
        "      \"queryText\": { \"type\": \"string\" },\n" +
        "      \"vectorBase64\": { \"type\": \"string\" },\n" +
        "      \"k\": { \"type\": \"integer\", \"minimum\": 1, \"maximum\": 100, \"default\": 8 },\n" +
        "      \"ontologyClass\": { \"type\": \"string\" },\n" +
        "      \"uriEquals\": { \"type\": \"string\" },\n" +
        "      \"tagsAny\": { \"type\": \"array\", \"items\": { \"type\": \"string\" } }\n" +
        "    }\n" +
        "  },\n" +
        "  \"output\": {\n" +
        "    \"type\": \"object\",\n" +
        "    \"required\": [\"tenantId\",\"count\",\"items\"],\n" +
        "    \"properties\": {\n" +
        "      \"tenantId\": { \"type\": \"string\" },\n" +
        "      \"count\": { \"type\": \"integer\" },\n" +
        "      \"items\": { \"type\": \"array\" }\n" +
        "    }\n" +
        "  }\n" +
        "}";

    public async Task<MemorySearchOutput> ExecuteAsync(MemorySearchInput input, CancellationToken ct = default)
    {
        float[]? vector = null;
        if (!string.IsNullOrWhiteSpace(input.VectorBase64))
        {
            try
            {
                var bytes = Convert.FromBase64String(input.VectorBase64);
                vector = new float[bytes.Length / 4];
                Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse vectorBase64");
                throw new ArgumentException("Invalid vectorBase64 (must be base64-encoded float32 array).");
            }
        }

        if (vector is null && string.IsNullOrWhiteSpace(input.QueryText))
        {
            _logger.LogWarning("MemorySearch called without query or vector");
            throw new ArgumentException("Provide either queryText or vectorBase64.");
        }

        var filters = new VectorStore.SearchFilters
        {
            OntologyClass = input.OntologyClass,
            UriEquals = input.UriEquals,
            TagsAny = input.TagsAny?.ToArray()
        };

        _logger.LogInformation("Executing memory search for tenant {TenantId}, query: {Query}, k: {K}",
            _tenant.TenantId, input.QueryText ?? "<vector>", input.K);

        var results = await _vector.SearchHybridAsync(input.QueryText, vector, input.K, filters, ct);

        return new MemorySearchOutput
        {
            TenantId = _tenant.TenantId,
            Count = results.Count,
            Items = results.Select(r => new MemorySearchItem
            {
                Id = r.Id,
                Title = r.Title,
                Source = r.Source,
                Chunk = r.Chunk,
                OntologyClass = r.OntologyClass,
                Uri = r.Uri,
                Tags = r.Tags?.ToList(),
                Score = r.Score,
                Preview = r.Content is { Length: > 240 } ? r.Content[..240] + "…" : r.Content
            }).ToList()
        };
    }

    public Task<MemorySearchOutput> ExecuteAsync(JsonElement json, CancellationToken ct = default)
    {
        var input = JsonSerializer.Deserialize<MemorySearchInput>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new ArgumentException("Invalid MemorySearch input payload.");
        return ExecuteAsync(input, ct);
    }
}