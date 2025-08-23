using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace LimboDancer.MCP.McpServer.Telemetry;

/// <summary>
/// Custom resource detector for environment information.
/// </summary>
public class EnvironmentResourceDetector : IResourceDetector
{
    public Resource Detect()
    {
        var attributes = new List<KeyValuePair<string, object>>
        {
            new("deployment.environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"),
            new("host.name", Environment.MachineName),
            new("os.description", System.Runtime.InteropServices.RuntimeInformation.OSDescription),
            new("process.runtime.name", ".NET"),
            new("process.runtime.version", Environment.Version.ToString())
        };

        return new Resource(attributes);
    }
}