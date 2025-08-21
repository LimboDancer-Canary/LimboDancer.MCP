namespace LimboDancer.MCP.Core.Tenancy;

public interface ITenantAccessor
{
    string TenantId { get; }
    bool IsDevelopment { get; }
}
