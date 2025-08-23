namespace LimboDancer.MCP.McpServer.Resilience;

public class RetryInfo
{
    public bool CanRetry { get; set; }
    public TimeSpan? RetryAfter { get; set; }
    public int? RemainingRetries { get; set; }
}