using System.Text.Json;

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

public enum MessageRole { User, Assistant, Tool }

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

public enum MemoryKind { Vector, Graph, Reasoning }

public sealed class MemoryItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public MemoryKind Kind { get; set; }
    public string ExternalId { get; set; } = "";
    public JsonDocument? MetaJson { get; set; }
    public DateTime CreatedAt { get; set; }
}