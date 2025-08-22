namespace LimboDancer.MCP.Core.Tenancy;

/// <summary>
/// Canonical header names for multi-tenant scoping. 
/// Use these constants everywhere to avoid casing drift.
/// </summary>
public static class TenantHeaders
{
    public const string TenantId = "X-Tenant-Id";
    public const string Package = "X-Tenant-Package";
    public const string Channel = "X-Tenant-Channel";
}