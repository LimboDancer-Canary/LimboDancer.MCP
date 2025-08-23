using System.Text.Json;
using LimboDancer.MCP.Core.Primitives;

namespace LimboDancer.MCP.Core;

public interface IChatHistoryStore
{
    Task<SessionInfo> CreateSessionAsync(string userId, JsonDocument? tagsJson = null, CancellationToken ct = default);
    Task<MessageInfo> AppendMessageAsync(Guid sessionId, MessageRole role, string content, JsonDocument? toolCallsJson = null, CancellationToken ct = default);
    Task<IReadOnlyList<MessageInfo>> GetMessagesAsync(Guid sessionId, int take = 100, int skip = 0, CancellationToken ct = default);
}