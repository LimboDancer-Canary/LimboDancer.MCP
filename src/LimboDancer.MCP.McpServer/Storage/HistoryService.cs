using System.Text.Json;
using LimboDancer.MCP.Core;
using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.McpServer.Tools;
using LimboDancer.MCP.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LimboDancer.MCP.McpServer.Storage;

public class HistoryService : IHistoryService, IHistoryReader, IHistoryStore
{
    private readonly ChatDbContext _db;
    private readonly ITenantAccessor _tenant;
    private readonly ILogger<HistoryService> _logger;

    public HistoryService(ChatDbContext db, ITenantAccessor tenant, ILogger<HistoryService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // IHistoryService implementation
    public async Task<Message> AppendMessageAsync(Guid sessionId, MessageRole role, string content, JsonDocument? toolCalls, CancellationToken ct)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("SessionId cannot be empty.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or empty.", nameof(content));

        Guid tenantId;
        try
        {
            tenantId = Guid.Parse(_tenant.TenantId);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid tenant ID format: {TenantId}", _tenant.TenantId);
            throw new InvalidOperationException($"Invalid tenant ID format: {_tenant.TenantId}", ex);
        }

        if (tenantId == Guid.Empty)
            throw new InvalidOperationException("TenantId cannot be empty.");

        var message = new Message
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            TenantId = tenantId,
            Role = role,
            Content = content,
            ToolCallsJson = toolCalls,
            CreatedAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Appended message {MessageId} to session {SessionId}", message.Id, sessionId);
        return message;
    }

    // IHistoryReader implementation
    public async Task<IReadOnlyList<HistoryItemDto>> ListAsync(string sessionId, int limit, DateTimeOffset? before, CancellationToken ct = default)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            _logger.LogWarning("Invalid session ID format: {SessionId}", sessionId);
            return Array.Empty<HistoryItemDto>();
        }

        var query = _db.Messages
            .AsNoTracking()
            .Where(m => m.SessionId == sessionGuid);

        if (before.HasValue)
            query = query.Where(m => m.CreatedAt < before.Value.UtcDateTime);

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .Select(m => new HistoryItemDto
            {
                Id = m.Id.ToString(),
                Sender = m.Role.ToString().ToLowerInvariant(),
                Text = m.Content,
                Timestamp = new DateTimeOffset(m.CreatedAt, TimeSpan.Zero),
                Metadata = ConvertToolCallsToMetadata(m.ToolCallsJson)
            })
            .ToListAsync(ct);

        messages.Reverse(); // Return in chronological order
        return messages;
    }

    // IHistoryStore implementation
    public async Task<HistoryAppendResult> AppendAsync(HistoryAppendRecord record, CancellationToken ct = default)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));

        if (!Guid.TryParse(record.SessionId, out var sessionGuid))
            throw new ArgumentException("Invalid session ID format", nameof(record));

        var role = Enum.TryParse<MessageRole>(record.Sender, true, out var r) ? r : MessageRole.User;

        // Convert metadata to JsonDocument if needed
        JsonDocument? toolCallsJson = null;
        if (record.Metadata != null && record.Metadata.Count > 0)
        {
            toolCallsJson = JsonSerializer.SerializeToDocument(record.Metadata);
        }

        var message = await AppendMessageAsync(sessionGuid, role, record.Text, toolCallsJson, ct);

        return new HistoryAppendResult(
            message.Id.ToString(),
            message.SessionId.ToString(),
            new DateTimeOffset(message.CreatedAt, TimeSpan.Zero)
        );
    }

    private static Dictionary<string, object?>? ConvertToolCallsToMetadata(JsonDocument? toolCallsJson)
    {
        if (toolCallsJson == null) return null;

        try
        {
            var metadata = new Dictionary<string, object?>();
            foreach (var property in toolCallsJson.RootElement.EnumerateObject())
            {
                metadata[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => property.Value.GetRawText()
                };
            }
            return metadata;
        }
        catch (Exception)
        {
            return null;
        }
    }
}