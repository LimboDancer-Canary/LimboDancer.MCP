namespace LimboDancer.MCP.McpServer.Http.Chat;

public sealed record CreateSessionRequest(string? SystemPrompt);
public sealed record CreateSessionResponse(string SessionId);

public sealed record PostMessageRequest(string Role, string Content);
public sealed record PostMessageResponse(string CorrelationId);

public sealed record ChatEvent(
    string Type,           // "token" | "message.completed" | "error" | "ping"
    string SessionId,
    string? Content = null,
    string? CorrelationId = null,
    string? ErrorCode = null,
    string? ErrorMessage = null
);