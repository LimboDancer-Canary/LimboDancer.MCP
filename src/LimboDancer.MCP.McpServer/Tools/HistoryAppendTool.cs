using LimboDancer.MCP.Core;
using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.McpServer.Storage;
using LimboDancer.MCP.Storage;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LimboDancer.MCP.McpServer.Tools;

public sealed class HistoryAppendTool : IMcpTool
{
    public string Name => "history.append";
    public Tool ToolDescriptor { get; }

    private readonly IHistoryService _history;
    private readonly ITenantAccessor _tenant;

    public HistoryAppendTool(IHistoryService history, ITenantAccessor tenant)
    {
        _history = history;
        _tenant = tenant;

        ToolDescriptor = new Tool
        {
            Name = Name,
            Description = "Append a message (user/assistant/tool) to a session. Tenancy is enforced by server context.",
            InputSchema = ToolSchema.Build(
                (schema, props, req) =>
                {
                    // Bind to ontology URIs via @id
                    ToolSchema.Prop(props, "sessionId", "string", "Session id (GUID)", "uuid", ontologyId: "ldm:Session");
                    ToolSchema.Prop(props, "role", "string", "user | assistant | tool", ontologyId: "ldm:Message.role");
                    ToolSchema.Prop(props, "content", "string", "Message text", ontologyId: "ldm:Message.content");
                    ToolSchema.Prop(props, "toolCallsJson", "string", "JSON (optional)", ontologyId: "ldm:ToolCall");
                    req.Add("sessionId"); req.Add("role"); req.Add("content");
                },
                customize: schema =>
                {
                    // Attach ontology preconditions/effects (using ontology predicates) as vendor extension
                    var pre = new JsonArray
                    {
                        new JsonObject
                        {
                            // A simple existence check for the session (predicate omitted => existence)
                            ["subject"] = "ldm:Session",
                            ["predicate"] = "",
                            ["equals"] = ""
                        }
                    };

                    var eff = new JsonArray
                    {
                        new JsonObject
                        {
                            // Effect: session hasMessage -> message (predicate uses ontology relation)
                            ["predicate"] = "ldm:hasMessage",
                            ["value"] = "created" // hint; servers may ignore
                        }
                    };

                    ToolSchema.AddOntologyExtensions(schema, preconditions: pre, effects: eff);
                })
        };
    }

    public async Task<CallToolResult> CallAsync(Dictionary<string, object?> args, CancellationToken ct)
    {
        if (!args.TryGetValue("sessionId", out var sidObj) || sidObj is null)
            throw new McpException("Missing required argument 'sessionId'.");
        if (!Guid.TryParse(sidObj.ToString(), out var sessionId))
            throw new McpException("Invalid 'sessionId' (expected GUID).");

        if (!args.TryGetValue("role", out var roleObj) || roleObj is null)
            throw new McpException("Missing required argument 'role'.");
        var roleStr = roleObj.ToString()!.Trim().ToLowerInvariant();

        MessageRole role = roleStr switch
        {
            "user" => MessageRole.User,
            "assistant" => MessageRole.Assistant,
            "tool" => MessageRole.Tool,
            _ => throw new McpException("Invalid 'role' (expected: user|assistant|tool).")
        };

        if (!args.TryGetValue("content", out var contentObj) || contentObj is null)
            throw new McpException("Missing required argument 'content'.");
        var content = contentObj.ToString() ?? "";

        JsonDocument? toolCalls = null;
        if (args.TryGetValue("toolCallsJson", out var tcObj) && tcObj is not null && !string.IsNullOrWhiteSpace(tcObj.ToString()))
        {
            try { toolCalls = JsonDocument.Parse(tcObj!.ToString()!); }
            catch { throw new McpException("Invalid 'toolCallsJson' (must be valid JSON)."); }
        }

        var msg = await _history.AppendMessageAsync(sessionId, role, content, toolCalls, ct);

        var payload = new
        {
            tenantId = _tenant.TenantId,
            id = msg.Id,
            sessionId,
            role = role.ToString().ToLowerInvariant(),
            createdAt = msg.CreatedAt
        };

        return new CallToolResult
        {
            Content = [new TextContentBlock { Type = "text", Text = System.Text.Json.JsonSerializer.Serialize(payload) }]
        };
    }
}