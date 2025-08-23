using System.ComponentModel.DataAnnotations;

namespace LimboDancer.MCP.McpServer.Configuration;

public class CircuitBreakerOptions
{
    /// <summary>
    /// Number of consecutive failures before opening circuit.
    /// </summary>
    [Range(1, 100)]
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Duration the circuit stays open before attempting recovery.
    /// </summary>
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Sampling duration for failure rate calculation.
    /// </summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Minimum throughput required for circuit breaker activation.
    /// </summary>
    [Range(1, 1000)]
    public int MinimumThroughput { get; set; } = 10;
}