using System.Text.Json;

namespace LimboDancer.MCP.Core;

public record SessionInfo(
    Guid Id,
    Guid TenantId,
    string UserId,
    JsonDocument? TagsJson,
    DateTime CreatedAt);