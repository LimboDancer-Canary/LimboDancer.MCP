using System.Threading;

namespace LimboDancer.MCP.Core.Tenancy;

public sealed class AmbientTenantAccessor : ITenantAccessor
{
    private static readonly AsyncLocal<Guid> _ambient = new();

    public static void Set(Guid tenantId) => _ambient.Value = tenantId;

    public Guid TenantId => _ambient.Value;
}

/*
 ToDo: Register this accessor in your DI container.

 // Before running a command that touches stores:
   AmbientTenantAccessor.Set(tenantGuid);
   
   // DI (CLI host):
   services.AddScoped<ITenantAccessor>(_ => new AmbientTenantAccessor());
   
 */