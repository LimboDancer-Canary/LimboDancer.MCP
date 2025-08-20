using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.Vector.AzureSearch;
using ModelContextProtocol.Protocol;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LimboDancer.MCP.McpServer.Tools;

public sealed class MemorySearchTool : IMcpTool
{
    public string Name => "memory.search";
    public Tool ToolDescriptor { get; }

    private readonly VectorStore _vector;
    private readonly ITenantAccessor _tenant;

    public MemorySearchTool(VectorStore vector, ITenantAccessor tenant)
    {
        _vector = vector;
        _tenant = tenant;

        ToolDescriptor = new Tool
        {
            Name = Name,
            Description = "Hybrid search over memory index (BM25 + vector). Tenancy is enforced by server context.",
            InputSchema = ToolSchema.Build((schema, props, req) =>
            {
                ToolSchema.Prop(props, "queryText", "string", "BM25/semantic text (optional if vector is supplied)");
                ToolSchema.Prop(props, "vectorBase64", "string", "Base64(float32[]) optional");
                ToolSchema.Prop(props, "k", "integer", "Top K (default 8)");
                ToolSchema.Prop(props, "ontologyClass", "string", "Filter: ontology class (optional)");
                ToolSchema.Prop(props, "uriEquals", "string", "Filter: exact URI (optional)");

                // array<string> tagsAny
                var tagsAny = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject { ["type"] = "string" },
                    ["title"] = "Filter: any tag matches (optional)"
                };
                ((JsonObject)schema["properties"]!)["tagsAny"] = tagsAny;
            })
        };
    }

    public async Task<CallToolResult> CallAsync(Dictionary<string, object?> args, CancellationToken ct)
    {
        args.TryGetValue("queryText", out var qObj);
        var queryText = qObj?.ToString();

        float[]? vector = null;
        if (args.TryGetValue("vectorBase64", out var vecObj) && vecObj is not null && !string.IsNullOrWhiteSpace(vecObj.ToString()))
        {
            try
            {
                var bytes = Convert.FromBase64String(vecObj!.ToString()!);
                var floats = new float[bytes.Length / 4];
                Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
                vector = floats;
            }
            catch
            {
                throw new McpException("Invalid 'vectorBase64' (must be base64-encoded float32 array).");
            }
        }

        if (vector is null && string.IsNullOrWhiteSpace(queryText))
            throw new McpException("Provide either 'queryText' or 'vectorBase64'.");

        var k = 8;
        if (args.TryGetValue("k", out var kObj) && int.TryParse(kObj?.ToString(), out var kParsed) && kParsed > 0)
            k = kParsed;

        var filters = new VectorStore.SearchFilters();
        if (args.TryGetValue("ontologyClass", out var ocObj) && ocObj is not null) filters.OntologyClass = ocObj.ToString();
        if (args.TryGetValue("uriEquals", out var uriObj) && uriObj is not null) filters.UriEquals = uriObj.ToString();

        if (args.TryGetValue("tagsAny", out var tagsObj) && tagsObj is IEnumerable<object?> en)
            filters.TagsAny = en.Where(x => x is not null).Select(x => x!.ToString()!).ToArray();

        var results = await _vector.SearchHybridAsync(queryText, vector, k, filters, ct);

        var payload = new
        {
            tenantId = _tenant.TenantId,
            count = results.Count,
            items = results.Select(r => new
            {
                r.Id,
                r.Title,
                r.Source,
                r.Chunk,
                r.OntologyClass,
                r.Uri,
                r.Tags,
                r.Score,
                preview = r.Content is { Length: > 240 } ? r.Content[..240] + "…" : r.Content
            })
        };

        return new CallToolResult
        {
            Content = [new TextContentBlock { Type = "text", Text = JsonSerializer.Serialize(payload) }]
        };
    }
}
