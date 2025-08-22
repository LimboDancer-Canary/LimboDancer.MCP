using LimboDancer.MCP.Core;
using LimboDancer.MCP.Core.Tenancy;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LimboDancer.MCP.Storage;

public sealed class ChatHistoryStore : IChatHistoryStore
{
    private readonly ChatDbContext _db;
    private readonly ITenantAccessor _tenant;

    public ChatHistoryStore(ChatDbContext db, ITenantAccessor tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Session> CreateSessionAsync(string userId, JsonDocument? tagsJson = null, CancellationToken ct = default)
    {
        var tenantId = _tenant.TenantId;
        if (tenantId == Guid.Empty)
            throw new InvalidOperationException("TenantId cannot be empty.");

        var s = new Session
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            TagsJson = tagsJson,
            CreatedAt = DateTime.UtcNow
        };
        _db.Sessions.Add(s);
        await _db.SaveChangesAsync(ct);
        return s;
    }

    public async Task<Message> AppendMessageAsync(Guid sessionId, MessageRole role, string content, JsonDocument? toolCallsJson = null, CancellationToken ct = default)
    {
        var tenantId = _tenant.TenantId;
        if (tenantId == Guid.Empty)
            throw new InvalidOperationException("TenantId cannot be empty.");

        // Query filter ensures tenant scoping; still do an exists check
        var exists = await _db.Sessions.AsNoTracking().AnyAsync(x => x.Id == sessionId && x.TenantId == tenantId, ct);
        if (!exists)
            throw new InvalidOperationException($"Session {sessionId} not found for tenant.");

        var m = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SessionId = sessionId,
            Role = role,
            Content = content,
            ToolCallsJson = toolCallsJson,
            CreatedAt = DateTime.UtcNow
        };
        _db.Messages.Add(m);
        await _db.SaveChangesAsync(ct);
        return m;
    }

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(Guid sessionId, int take = 100, int skip = 0, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 1000);
        skip = Math.Max(0, skip);

        var items = await _db.Messages.AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return items;
    }
}