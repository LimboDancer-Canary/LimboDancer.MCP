using LimboDancer.MCP.Core;
using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.Storage;
using ModelContextProtocol.Protocol;
using System.Net.Mail;
using System.Text.Json;

namespace LimboDancer.MCP.McpServer.Tools;

public sealed class HistoryAppendTool : IMcpTool
{
    public string Name => "history.append";
    public Tool ToolDescriptor { get; }

    private readonly IChatHistoryStore _history;
    private readonly ITenantAccessor _tenant;

    public HistoryAppendTool(IChatHistoryStore history, ITenantAccessor tenant)
    {
        _history = history;
        _tenant = tenant;

        ToolDescriptor = new Tool
        {
            Name = Name,
            Description = "Append a message (user/assistant/tool) to a session. Tenancy is enforced by server context.",
            InputSchema = ToolSchema.Build((schema, props, req) =>
            {
                ToolSchema.Prop(props, "sessionId", "string", "Session id (GUID)", "uuid");
                ToolSchema.Prop(props, "role", "string", "user | assistant | tool");
                ToolSchema.Prop(props, "content", "string", "Message text");
                ToolSchema.Prop(props, "toolCallsJson", "string", "JSON (optional)");
                req.Add("sessionId"); req.Add("role"); req.Add("content");
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
            Content = [new TextContentBlock { Type = "text", Text = JsonSerializer.Serialize(payload) }]
        };
    }
}
