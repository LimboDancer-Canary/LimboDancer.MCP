using System.ComponentModel.DataAnnotations;

namespace LimboDancer.MCP.McpServer.Configuration;

public class RetryPolicyOptions
{
    /// <summary>
    /// Number of retry attempts for transient failures.
    /// </summary>
    [Range(0, 10)]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between retries (exponential backoff).
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay between retries.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Jitter factor for retry delays (0.0 to 1.0).
    /// </summary>
    [Range(0.0, 1.0)]
    public double JitterFactor { get; set; } = 0.2;
}