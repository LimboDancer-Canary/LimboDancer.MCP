using System.Text.Json;
using LimboDancer.MCP.Core.Primitives;

namespace LimboDancer.MCP.Core;

public record MessageInfo(
    Guid Id,
    Guid SessionId,
    Guid TenantId,
    MessageRole Role,
    string Content,
    JsonDocument? ToolCallsJson,
    DateTime CreatedAt);