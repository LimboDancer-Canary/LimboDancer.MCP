using System.Text.Json;
using LimboDancer.MCP.Core;
using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.McpServer.Tools;
using LimboDancer.MCP.Storage;
using Microsoft.EntityFrameworkCore;

namespace LimboDancer.MCP.McpServer.Storage;

public class HistoryService : IHistoryService, IHistoryReader, IHistoryStore
{
    private readonly ChatDbContext _db;
    private readonly ITenantAccessor _tenant;
    private readonly ILogger<HistoryService> _logger;

    public HistoryService(ChatDbContext db, ITenantAccessor tenant, ILogger<HistoryService> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    // IHistoryService implementation (existing interface)
    public async Task<Message> AppendMessageAsync(Guid sessionId, MessageRole role, string content, JsonDocument? toolCalls, CancellationToken ct)
    {
        var tenantId = Guid.Parse(_tenant.TenantId);
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
            return Array.Empty<HistoryItemDto>();

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
                Metadata = null // Could deserialize ToolCallsJson if needed
            })
            .ToListAsync(ct);

        messages.Reverse(); // Return in chronological order
        return messages;
    }

    // IHistoryStore implementation
    public async Task<HistoryAppendResult> AppendAsync(HistoryAppendRecord record, CancellationToken ct = default)
    {
        if (!Guid.TryParse(record.SessionId, out var sessionGuid))
            throw new ArgumentException("Invalid session ID format");

        var role = Enum.TryParse<MessageRole>(record.Sender, true, out var r) ? r : MessageRole.User;

        var message = await AppendMessageAsync(sessionGuid, role, record.Text,
            record.Metadata != null ? JsonSerializer.SerializeToDocument(record.Metadata) : null, ct);

        return new HistoryAppendResult(
            message.Id.ToString(),
            message.SessionId.ToString(),
            new DateTimeOffset(message.CreatedAt, TimeSpan.Zero)
        );
    }
}