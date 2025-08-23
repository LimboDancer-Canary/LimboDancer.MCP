using System.Diagnostics;

namespace LimboDancer.MCP.McpServer.Telemetry;

/// <summary>
/// Activity source for distributed tracing.
/// </summary>
public static class McpActivitySource
{
    public static readonly string Name = "LimboDancer.MCP";
    public static readonly ActivitySource Instance = new(Name, "1.0.0");
}