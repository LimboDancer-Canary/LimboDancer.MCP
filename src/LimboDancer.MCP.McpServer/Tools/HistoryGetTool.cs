// UPDATED: emits shared @context; minor cleanup. If you already had a different signature,
// keep the IHistoryStore usage and the schema changes below.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace LimboDancer.MCP.McpServer.Tools
{
    public sealed class HistoryGetInput
    {
        [JsonPropertyName("sessionId")] public string SessionId { get; set; } = default!;
        [JsonPropertyName("limit")] public int Limit { get; set; } = 50;
        [JsonPropertyName("before")] public DateTimeOffset? Before { get; set; }
    }

    public sealed class HistoryGetOutput
    {
        [JsonPropertyName("sessionId")] public string SessionId { get; set; } = default!;
        [JsonPropertyName("messages")] public List<HistoryItemDto> Messages { get; set; } = new();
    }

    public sealed class HistoryItemDto
    {
        [JsonPropertyName("id")] public string Id { get; set; } = default!;
        [JsonPropertyName("sender")] public string Sender { get; set; } = default!;
        [JsonPropertyName("text")] public string Text { get; set; } = default!;
        [JsonPropertyName("timestamp")] public DateTimeOffset Timestamp { get; set; }
        [JsonPropertyName("metadata")] public Dictionary<string, object?>? Metadata { get; set; }
    }

    public interface IHistoryReader
    {
        Task<IReadOnlyList<HistoryItemDto>> ListAsync(string sessionId, int limit, DateTimeOffset? before, CancellationToken ct = default);
    }

    public sealed class HistoryGetTool
    {
        private readonly IHistoryReader _reader;

        public HistoryGetTool(IHistoryReader reader) => _reader = reader ?? throw new ArgumentNullException(nameof(reader));

        public static string ToolSchema =>
            "{\n" +
            $"  \"@context\": {ToolSchemas.JsonLdContext},\n" +
            "  \"@id\": \"ldm:tool/HistoryGet\",\n" +
            "  \"title\": \"Get session history\",\n" +
            "  \"description\": \"Returns recent messages for a session.\",\n" +
            "  \"input\": {\n" +
            "    \"type\": \"object\",\n" +
            "    \"required\": [\"sessionId\"],\n" +
            "    \"properties\": {\n" +
            "      \"sessionId\": { \"type\": \"string\" },\n" +
            "      \"limit\": { \"type\": \"integer\", \"minimum\": 1, \"maximum\": 200, \"default\": 50 },\n" +
            "      \"before\": { \"type\": \"string\", \"format\": \"date-time\" }\n" +
            "    }\n" +
            "  },\n" +
            "  \"output\": {\n" +
            "    \"type\": \"object\",\n" +
            "    \"required\": [\"sessionId\",\"messages\"],\n" +
            "    \"properties\": {\n" +
            "      \"sessionId\": { \"type\": \"string\" },\n" +
            "      \"messages\": { \"type\": \"array\" }\n" +
            "    }\n" +
            "  }\n" +
            "}";

        public async Task<HistoryGetOutput> ExecuteAsync(HistoryGetInput input, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(input.SessionId)) throw new ArgumentException("sessionId is required.");
            var items = await _reader.ListAsync(input.SessionId, Math.Clamp(input.Limit, 1, 200), input.Before, ct);
            return new HistoryGetOutput { SessionId = input.SessionId, Messages = new List<HistoryItemDto>(items) };
        }

        public Task<HistoryGetOutput> ExecuteAsync(JsonElement json, CancellationToken ct = default)
        {
            var input = JsonSerializer.Deserialize<HistoryGetInput>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? throw new ArgumentException("Invalid HistoryGet input payload.");
            return ExecuteAsync(input, ct);
        }
    }
}
