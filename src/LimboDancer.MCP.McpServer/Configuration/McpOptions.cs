namespace LimboDancer.MCP.McpServer.Configuration;

/// <summary>
/// Configuration options for MCP server.
/// </summary>
public class McpOptions
{
    /// <summary>
    /// Maximum number of concurrent tool executions.
    /// </summary>
    public int MaxConcurrentToolExecutions { get; set; } = 10;

    /// <summary>
    /// Default timeout for tool execution in seconds.
    /// </summary>
    public int ToolExecutionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum tool execution time as TimeSpan.
    /// </summary>
    public TimeSpan MaxToolExecutionTime => TimeSpan.FromSeconds(ToolExecutionTimeoutSeconds);

    /// <summary>
    /// Tool-specific timeout configurations.
    /// </summary>
    public Dictionary<string, TimeSpan> ToolTimeouts { get; set; } = new();

    /// <summary>
    /// Retry policy configuration.
    /// </summary>
    public RetryPolicyOptions RetryPolicy { get; set; } = new();

    /// <summary>
    /// Circuit breaker configuration.
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Enable detailed logging.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Enable telemetry collection.
    /// </summary>
    public bool EnableTelemetry { get; set; } = true;
}