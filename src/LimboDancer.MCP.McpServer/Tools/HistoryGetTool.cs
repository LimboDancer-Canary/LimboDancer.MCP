using LimboDancer.MCP.Core;
using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.Storage;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LimboDancer.MCP.McpServer.Tools;

/// <summary>Common MCP tool contract used by the host to list and invoke tools.</summary>
internal interface IMcpTool
{
    string Name { get; }
    Tool ToolDescriptor { get; }
    Task<CallToolResult> CallAsync(Dictionary<string, object?> args, CancellationToken ct);
}

/// <summary>Small helper to build a JSON Schema object for MCP Tool.InputSchema.</summary>
internal static class ToolSchema
{
    public static JsonElement Build(Action<JsonObject, JsonObject, JsonArray> build, Action<JsonObject>? customize = null)
    {
        var schema = new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = new JsonObject(),
            ["required"] = new JsonArray()
        };

        var props = (JsonObject)schema["properties"]!;
        var req = (JsonArray)schema["required"]!;
        build(schema, props, req);

        customize?.Invoke(schema);

        return JsonSerializer.Deserialize<JsonElement>(schema.ToJsonString())!;
    }

    public static void Prop(
        JsonObject props,
        string name,
        string type,
        string? title = null,
        string? format = null,
        string? ontologyId = null)
    {
        var o = new JsonObject { ["type"] = type };
        if (!string.IsNullOrWhiteSpace(title)) o["title"] = title;
        if (!string.IsNullOrWhiteSpace(format)) o["format"] = format;
        if (!string.IsNullOrWhiteSpace(ontologyId)) o["@id"] = ontologyId; // JSON-LD binding
        props[name] = o;
    }

    public static void AddOntologyExtensions(JsonObject schema, JsonArray? preconditions = null, JsonArray? effects = null)
    {
        var ext = new JsonObject();
        if (preconditions is not null) ext["preconditions"] = preconditions;
        if (effects is not null) ext["effects"] = effects;
        if (ext.Count > 0)
        {
            schema["x-ontology"] = ext; // vendor extension to carry ontology preconditions/effects (using ontology predicates)
        }
    }
}

public sealed class HistoryGetTool : IMcpTool
{
    public string Name => "history.get";
    public Tool ToolDescriptor { get; }

    private readonly IChatHistoryStore _history;
    private readonly ITenantAccessor _tenant;

    public HistoryGetTool(IChatHistoryStore history, ITenantAccessor tenant)
    {
        _history = history;
        _tenant = tenant;

        ToolDescriptor = new Tool
        {
            Name = Name,
            Description = "Get chat history messages for a session (paged). Tenancy is enforced by server context.",
            InputSchema = ToolSchema.Build((schema, props, req) =>
            {
                // Bind to ontology URIs via @id
                ToolSchema.Prop(props, "sessionId", "string", "Session id (GUID)", "uuid", ontologyId: "ldm:Session");
                ToolSchema.Prop(props, "take", "integer", "Page size (default 100)");
                ToolSchema.Prop(props, "skip", "integer", "Offset (default 0)");
                req.Add("sessionId");
            })
        };
    }

    public async Task<CallToolResult> CallAsync(Dictionary<string, object?> args, CancellationToken ct)
    {
        if (!args.TryGetValue("sessionId", out var sidObj) || sidObj is null)
            throw new McpException("Missing required argument 'sessionId'.");
        if (!Guid.TryParse(sidObj.ToString(), out var sessionId))
            throw new McpException("Invalid 'sessionId' (expected GUID).");

        var take = args.TryGetValue("take", out var tObj) && int.TryParse(tObj?.ToString(), out var tVal) && tVal > 0 ? tVal : 100;
        var skip = args.TryGetValue("skip", out var sObj) && int.TryParse(sObj?.ToString(), out var sVal) && sVal >= 0 ? sVal : 0;

        var msgs = await _history.GetMessagesAsync(sessionId, take: take, skip: skip, ct: ct);

        var payload = new
        {
            tenantId = _tenant.TenantId,
            sessionId,
            count = msgs.Count,
            items = msgs.Select(m => new
            {
                id = m.Id,
                role = m.Role.ToString().ToLowerInvariant(),
                content = m.Content,
                createdAt = m.CreatedAt
            })
        };

        return new CallToolResult
        {
            Content = [new TextContentBlock { Type = "text", Text = JsonSerializer.Serialize(payload) }]
        };
    }
}