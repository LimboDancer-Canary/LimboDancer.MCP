namespace LimboDancer.MCP.McpServer.Http.Chat;

public interface IChatOrchestrator
{
    Task<string> CreateSessionAsync(string tenantId, string? systemPrompt, CancellationToken ct);
    Task<object> GetHistoryAsync(string tenantId, string sessionId, CancellationToken ct);
    Task<string> EnqueueUserMessageAsync(string tenantId, string sessionId, string role, string content, CancellationToken ct);
    IAsyncEnumerable<ChatEvent> SubscribeAsync(string tenantId, string sessionId, CancellationToken ct);
}