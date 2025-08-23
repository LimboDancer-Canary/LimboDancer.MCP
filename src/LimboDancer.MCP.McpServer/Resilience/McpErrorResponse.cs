namespace LimboDancer.MCP.McpServer.Resilience;

/// <summary>
/// Enhanced error response with detailed information.
/// </summary>
public class McpErrorResponse
{
    public string ErrorCode { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string? Details { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string? TraceId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? HelpLink { get; set; }
    public RetryInfo? RetryInfo { get; set; }
}