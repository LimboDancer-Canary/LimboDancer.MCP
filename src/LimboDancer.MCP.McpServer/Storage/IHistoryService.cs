using System.Text.Json;
using LimboDancer.MCP.Core;
using LimboDancer.MCP.Storage;

namespace LimboDancer.MCP.McpServer.Storage;

public interface IHistoryService
{
    Task<Message> AppendMessageAsync(Guid sessionId, MessageRole role, string content, JsonDocument? toolCalls, CancellationToken ct);
}