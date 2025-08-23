namespace LimboDancer.MCP.McpServer.Tenancy;

/// <summary>
/// Standard tenant-related HTTP headers.
/// </summary>
public static class TenantHeaders
{
    public const string TenantId = "X-Tenant-Id";
    public const string Package = "X-Tenant-Package";
    public const string Channel = "X-Tenant-Channel";
}