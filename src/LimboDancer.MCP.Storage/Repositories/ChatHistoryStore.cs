using LimboDancer.MCP.Core;
using LimboDancer.MCP.Core.Primitives;
using LimboDancer.MCP.Core.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LimboDancer.MCP.Storage.Repositories;

public sealed class ChatHistoryStore : IChatHistoryStore
{
    private readonly ChatDbContext _db;
    private readonly ITenantAccessor _tenant;
    private readonly ILogger<ChatHistoryStore> _logger;

    public ChatHistoryStore(ChatDbContext db, ITenantAccessor tenant, ILogger<ChatHistoryStore> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SessionInfo> CreateSessionAsync(string userId, JsonDocument? tagsJson = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be null or empty.", nameof(userId));

        var tenantId = _tenant.TenantId;
        if (tenantId == Guid.Empty)
            throw new InvalidOperationException("TenantId cannot be empty.");

        // Use transaction for consistency
        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var session = new Session
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                TagsJson = tagsJson,
                CreatedAt = DateTime.UtcNow
            };

            _db.Sessions.Add(session);
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Created session {SessionId} for user {UserId} in tenant {TenantId}",
                session.Id, userId, tenantId);

            return new SessionInfo(
                session.Id,
                session.TenantId,
                session.UserId,
                session.TagsJson,
                session.CreatedAt);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to create session for user {UserId} in tenant {TenantId}",
                userId, tenantId);
            throw;
        }
    }

    public async Task<MessageInfo> AppendMessageAsync(Guid sessionId, MessageRole role, string content, JsonDocument? toolCallsJson = null, CancellationToken ct = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("SessionId cannot be empty.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or empty.", nameof(content));

        var tenantId = _tenant.TenantId;
        if (tenantId == Guid.Empty)
            throw new InvalidOperationException("TenantId cannot be empty.");

        // Use transaction to ensure session exists and message is appended atomically
        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Verify session exists and belongs to tenant (with lock for consistency)
            var sessionExists = await _db.Sessions
                .Where(x => x.Id == sessionId && x.TenantId == tenantId)
                .AnyAsync(ct);

            if (!sessionExists)
            {
                _logger.LogWarning("Session {SessionId} not found for tenant {TenantId}", sessionId, tenantId);
                throw new InvalidOperationException($"Session {sessionId} not found for tenant.");
            }

            var message = new Message
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SessionId = sessionId,
                Role = role,
                Content = content,
                ToolCallsJson = toolCallsJson,
                CreatedAt = DateTime.UtcNow
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogDebug("Appended message {MessageId} to session {SessionId}", message.Id, sessionId);

            return new MessageInfo(
                message.Id,
                message.SessionId,
                message.TenantId,
                message.Role,
                message.Content,
                message.ToolCallsJson,
                message.CreatedAt);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to append message to session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<IReadOnlyList<MessageInfo>> GetMessagesAsync(Guid sessionId, int take = 100, int skip = 0, CancellationToken ct = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("SessionId cannot be empty.", nameof(sessionId));

        take = Math.Clamp(take, 1, 1000);
        skip = Math.Max(0, skip);

        try
        {
            var messages = await _db.Messages
                .AsNoTracking()
                .Where(x => x.SessionId == sessionId)
                .OrderBy(x => x.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(m => new MessageInfo(
                    m.Id,
                    m.SessionId,
                    m.TenantId,
                    m.Role,
                    m.Content,
                    m.ToolCallsJson,
                    m.CreatedAt))
                .ToListAsync(ct);

            _logger.LogDebug("Retrieved {Count} messages for session {SessionId}", messages.Count, sessionId);

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve messages for session {SessionId}", sessionId);
            throw;
        }
    }
}