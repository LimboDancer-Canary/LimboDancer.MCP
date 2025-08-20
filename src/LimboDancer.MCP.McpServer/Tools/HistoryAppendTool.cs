// UPDATED: now uses ToolSchemas.JsonLdContext for "@context" (no hard-coded context)

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LimboDancer.MCP.McpServer.Graph;

namespace LimboDancer.MCP.McpServer.Tools
{
    public interface IHistoryStore
    {
        Task<HistoryAppendResult> AppendAsync(HistoryAppendRecord record, CancellationToken ct = default);
    }

    public sealed record HistoryAppendRecord(
        string SessionId,
        string Sender,
        string Text,
        DateTimeOffset Timestamp,
        IDictionary<string, object?>? Metadata
    );

    public sealed record HistoryAppendResult(
        string MessageId,
        string SessionId,
        DateTimeOffset Timestamp
    );

    public interface IGraphPreconditionsService
    {
        Task<PreconditionsResult> CheckAsync(CheckGraphPreconditionsRequest request, CancellationToken ct = default);
    }

    public sealed record CheckGraphPreconditionsRequest(
        string SubjectVertexId,
        IReadOnlyList<GraphPrecondition> Preconditions
    );

    public sealed record PreconditionsResult(
        bool IsSatisfied,
        IReadOnlyList<PreconditionViolation> Violations
    );

    public sealed record PreconditionViolation(
        string Predicate,
        string Reason
    );

    public sealed record GraphPrecondition(
        string Predicate,
        string Op,
        object? Expected
    );

    public sealed class HistoryAppendInput
    {
        [JsonPropertyName("sessionId")] public string SessionId { get; set; } = default!;
        [JsonPropertyName("sender")] public string Sender { get; set; } = default!;
        [JsonPropertyName("text")] public string Text { get; set; } = default!;
        [JsonPropertyName("timestamp")] public DateTimeOffset? Timestamp { get; set; }
        [JsonPropertyName("metadata")] public Dictionary<string, object?>? Metadata { get; set; }
        [JsonPropertyName("subjectVertexId")] public string SubjectVertexId { get; set; } = default!;
        [JsonPropertyName("preconditions")] public List<GraphPrecondition>? Preconditions { get; set; }
        [JsonPropertyName("effects")] public List<GraphEffect>? Effects { get; set; }
    }

    public sealed class HistoryAppendOutput
    {
        [JsonPropertyName("messageId")] public string MessageId { get; set; } = default!;
        [JsonPropertyName("sessionId")] public string SessionId { get; set; } = default!;
        [JsonPropertyName("timestamp")] public DateTimeOffset Timestamp { get; set; }
        [JsonPropertyName("preconditionViolations")] public List<PreconditionViolation>? PreconditionViolations { get; set; }
    }

    public sealed class HistoryAppendTool
    {
        private readonly IHistoryStore _historyStore;
        private readonly IGraphPreconditionsService _preconditions;
        private readonly IGraphEffectsService _effects;
        private readonly ILogger<HistoryAppendTool> _log;

        public HistoryAppendTool(
            IHistoryStore historyStore,
            IGraphPreconditionsService preconditions,
            IGraphEffectsService effects,
            ILogger<HistoryAppendTool> log)
        {
            _historyStore = historyStore ?? throw new ArgumentNullException(nameof(historyStore));
            _preconditions = preconditions ?? throw new ArgumentNullException(nameof(preconditions));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public static string ToolSchema =>
            // NOTE: Using shared @context so all tools stay in sync
            "{\n" +
            $"  \"@context\": {ToolSchemas.JsonLdContext},\n" +
            "  \"@id\": \"ldm:tool/HistoryAppend\",\n" +
            "  \"title\": \"Append message to session history and apply graph effects\",\n" +
            "  \"description\": \"Appends a message; validates ontology preconditions; applies effects via mapped predicates.\",\n" +
            "  \"input\": {\n" +
            "    \"type\": \"object\",\n" +
            "    \"required\": [\"sessionId\",\"sender\",\"text\",\"subjectVertexId\"],\n" +
            "    \"properties\": {\n" +
            "      \"sessionId\": { \"type\": \"string\" },\n" +
            "      \"sender\": { \"type\": \"string\" },\n" +
            "      \"text\": { \"type\": \"string\" },\n" +
            "      \"timestamp\": { \"type\": \"string\", \"format\": \"date-time\" },\n" +
            "      \"metadata\": { \"type\": \"object\", \"additionalProperties\": true },\n" +
            "      \"subjectVertexId\": { \"type\": \"string\" },\n" +
            "      \"preconditions\": { \"type\": \"array\" },\n" +
            "      \"effects\": { \"type\": \"array\" }\n" +
            "    }\n" +
            "  },\n" +
            "  \"output\": {\n" +
            "    \"type\": \"object\",\n" +
            "    \"required\": [\"messageId\",\"sessionId\",\"timestamp\"],\n" +
            "    \"properties\": {\n" +
            "      \"messageId\": { \"type\": \"string\" },\n" +
            "      \"sessionId\": { \"type\": \"string\" },\n" +
            "      \"timestamp\": { \"type\": \"string\", \"format\": \"date-time\" },\n" +
            "      \"preconditionViolations\": { \"type\": \"array\" }\n" +
            "    }\n" +
            "  }\n" +
            "}";

        public async Task<HistoryAppendOutput> ExecuteAsync(HistoryAppendInput input, CancellationToken ct = default)
        {
            ValidateBasicInput(input);

            var preconditions = input.Preconditions ?? new List<GraphPrecondition>();
            if (preconditions.Count > 0)
            {
                var check = await _preconditions.CheckAsync(
                    new CheckGraphPreconditionsRequest(input.SubjectVertexId, preconditions), ct);

                if (!check.IsSatisfied)
                {
                    _log.LogInformation("Preconditions failed for Subject={Subject} on session={Session}.", input.SubjectVertexId, input.SessionId);
                    return new HistoryAppendOutput
                    {
                        MessageId = string.Empty,
                        SessionId = input.SessionId,
                        Timestamp = DateTimeOffset.UtcNow,
                        PreconditionViolations = new List<PreconditionViolation>(check.Violations)
                    };
                }
            }

            var ts = input.Timestamp ?? DateTimeOffset.UtcNow;
            var appendResult = await _historyStore.AppendAsync(
                new HistoryAppendRecord(input.SessionId, input.Sender, input.Text, ts, input.Metadata), ct);

            var effects = input.Effects ?? new List<GraphEffect>();
            if (effects.Count > 0)
            {
                await _effects.ApplyAsync(new ApplyGraphEffectsRequest(input.SubjectVertexId, effects), ct);
            }

            return new HistoryAppendOutput
            {
                MessageId = appendResult.MessageId,
                SessionId = appendResult.SessionId,
                Timestamp = appendResult.Timestamp
            };
        }

        public Task<HistoryAppendOutput> ExecuteAsync(JsonElement json, CancellationToken ct = default)
        {
            var input = JsonSerializer.Deserialize<HistoryAppendInput>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? throw new ArgumentException("Invalid HistoryAppend input payload.");
            return ExecuteAsync(input, ct);
        }

        private static void ValidateBasicInput(HistoryAppendInput input)
        {
            if (string.IsNullOrWhiteSpace(input.SessionId)) throw new ArgumentException("sessionId is required.");
            if (string.IsNullOrWhiteSpace(input.Sender)) throw new ArgumentException("sender is required.");
            if (string.IsNullOrWhiteSpace(input.Text)) throw new ArgumentException("text is required.");
            if (string.IsNullOrWhiteSpace(input.SubjectVertexId)) throw new ArgumentException("subjectVertexId is required.");
        }
    }
}
