using System.Threading;
using Microsoft.Extensions.Options;

namespace LimboDancer.MCP.Core.Tenancy;

/// <summary>
/// Ambient tenant accessor that uses AsyncLocal for thread-safe tenant context.
/// Falls back to TenancyOptions.DefaultTenantId when no ambient context is set.
/// </summary>
public sealed class AmbientTenantAccessor : ITenantAccessor
{
    private static readonly AsyncLocal<string?> _ambient = new();
    private readonly string _defaultTenantId;

    public AmbientTenantAccessor(IOptions<TenancyOptions> options)
    {
        _defaultTenantId = options?.Value?.DefaultTenantId.ToString() ?? Guid.Empty.ToString();
    }

    public static void Set(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId cannot be null or empty.", nameof(tenantId));

        _ambient.Value = tenantId;
    }

    public static void Clear() => _ambient.Value = null;

    public string TenantId => _ambient.Value ?? _defaultTenantId;

    public bool IsDevelopment => false; // AmbientTenantAccessor is typically used in CLI/non-HTTP contexts
}

/// <summary>
/// Configuration options for tenancy.
/// </summary>
public sealed class TenancyOptions
{
    public Guid DefaultTenantId { get; set; } = Guid.Empty;
    public string DefaultPackage { get; set; } = "default";
    public string DefaultChannel { get; set; } = "dev";
}

/*
 Register this accessor in your DI container:

 // Before running a command that touches stores:
   AmbientTenantAccessor.Set(tenantString);

   // DI (CLI host):
   services.Configure<TenancyOptions>(configuration.GetSection("Tenancy"));
   services.AddSingleton<ITenantAccessor, AmbientTenantAccessor>();

 */