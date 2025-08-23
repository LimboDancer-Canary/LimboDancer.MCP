using System;
using OpenTelemetry.Resources;

namespace LimboDancer.MCP.McpServer.Telemetry;

/// <summary>
/// Detects environment-specific resources for telemetry.
/// </summary>
public class EnvironmentResourceDetector : IResourceDetector
{
    public Resource Detect()
    {
        var attributes = new Dictionary<string, object>
        {
            ["service.name"] = "LimboDancer.MCP",
            ["service.version"] = GetType().Assembly.GetName().Version?.ToString() ?? "unknown",
            ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "production",
            ["host.name"] = Environment.MachineName
        };

        // Add Azure-specific attributes if running in Azure
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))
        {
            attributes["cloud.provider"] = "azure";
            attributes["cloud.platform"] = "azure_app_service";
            attributes["cloud.resource_id"] = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
            attributes["cloud.region"] = Environment.GetEnvironmentVariable("REGION_NAME");
        }

        return new Resource(attributes);
    }
}