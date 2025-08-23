namespace LimboDancer.MCP.Core.Tenancy;

public interface ITenantAccessor
{
    Guid TenantId { get; }
    bool IsDevelopment { get; }
}