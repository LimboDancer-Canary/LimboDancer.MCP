using System.Text.Json;
using LimboDancer.MCP.Core.Primitives;

namespace LimboDancer.MCP.Storage;

public sealed class Session
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = null!;
    public JsonDocument? TagsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public sealed class Message
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid TenantId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public JsonDocument? ToolCallsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public Session Session { get; set; } = null!;
}

public sealed class MemoryItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public MemoryKind Kind { get; set; }
    public string ExternalId { get; set; } = "";
    public JsonDocument? MetaJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Tenant-scoped chat entities

public sealed class ChatThread
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Required tenant scope for all entities
    public string TenantId { get; set; } = default!;

    public string? Title { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public sealed class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Required tenant scope for all entities
    public string TenantId { get; set; } = default!;

    public Guid ThreadId { get; set; }

    // Simple string for role ("user", "assistant", etc.) to avoid coupling here
    public string Role { get; set; } = "user";

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ChatThread? Thread { get; set; }
}