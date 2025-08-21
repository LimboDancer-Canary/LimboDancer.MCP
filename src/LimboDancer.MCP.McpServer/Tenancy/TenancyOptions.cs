using System;

namespace LimboDancer.MCP.McpServer.Tenancy;

public sealed class TenancyOptions
{
    public Guid DefaultTenantId { get; set; } = Guid.Empty;
    public string DefaultPackage { get; set; } = "default";
    public string DefaultChannel { get; set; } = "dev";
}