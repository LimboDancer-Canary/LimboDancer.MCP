using System.Threading;

namespace LimboDancer.MCP.Core.Tenancy;

/// <summary>
/// Ambient tenant accessor that uses AsyncLocal for thread-safe tenant context.
/// </summary>
public sealed class AmbientTenantAccessor : ITenantAccessor
{
    private static readonly AsyncLocal<string?> _ambient = new();

    public static void Set(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId cannot be null or empty.", nameof(tenantId));
        
        _ambient.Value = tenantId;
    }

    public static void Clear() => _ambient.Value = null;

    public string TenantId => _ambient.Value ?? throw new InvalidOperationException("No tenant context available.");
}

/*
 Register this accessor in your DI container:

 // Before running a command that touches stores:
   AmbientTenantAccessor.Set(tenantString);
   
   // DI (CLI host):
   services.AddSingleton<ITenantAccessor, AmbientTenantAccessor>();
   
 */