using System.Text.Json;
using LimboDancer.MCP.Storage;

namespace LimboDancer.MCP.Core;

public interface IChatHistoryStore
{
    Task<Session> CreateSessionAsync(string userId, JsonDocument? tagsJson = null, CancellationToken ct = default);
    Task<Message> AppendMessageAsync(Guid sessionId, MessageRole role, string content, JsonDocument? toolCallsJson = null, CancellationToken ct = default);
    Task<IReadOnlyList<Message>> GetMessagesAsync(Guid sessionId, int take = 100, int skip = 0, CancellationToken ct = default);
}