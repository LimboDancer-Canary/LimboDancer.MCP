using System.Diagnostics;

namespace LimboDancer.MCP.McpServer.Telemetry;

/// <summary>
/// Activity source for MCP server tracing.
/// </summary>
public static class McpActivitySource
{
    public const string ActivitySourceName = "LimboDancer.MCP.McpServer";

    public static readonly ActivitySource Instance = new(ActivitySourceName, "1.0.0");
}