namespace LimboDancer.MCP.McpServer.Telemetry;

/// <summary>
/// Telemetry configuration for OpenTelemetry.
/// </summary>
public class TelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public string ServiceName { get; set; } = "LimboDancer.MCP";
    public string ServiceVersion { get; set; } = "1.0.0";
    public string? OtlpEndpoint { get; set; }
    public bool EnableTracing { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableLogging { get; set; } = false;
    public Dictionary<string, string> ResourceAttributes { get; set; } = new();
}