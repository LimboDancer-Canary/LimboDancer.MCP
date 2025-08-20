namespace LimboDancer.MCP.McpServer.Tenancy;

public sealed class TenancyOptions
{
    // Optional defaults used if the inbound request does not specify them
    public Guid DefaultTenantId { get; set; } = Guid.Empty; // set via configuration in non-dev
    public string DefaultPackage { get; set; } = "default";
    public string DefaultChannel { get; set; } = "dev";
}