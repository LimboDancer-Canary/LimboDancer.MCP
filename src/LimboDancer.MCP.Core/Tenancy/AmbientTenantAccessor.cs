using System.Threading;

namespace LimboDancer.MCP.Core.Tenancy;

/// <summary>
/// Ambient tenant accessor that uses AsyncLocal for thread-safe tenant context.
/// Falls back to defaultTenantId when no ambient context is set.
/// </summary>
public sealed class AmbientTenantAccessor : ITenantAccessor
{
    private static readonly AsyncLocal<Guid?> _ambient = new();
    private readonly Guid _defaultTenantId;

    // Direct constructor instead of using IOptions to avoid external dependencies
    public AmbientTenantAccessor(Guid defaultTenantId)
    {
        _defaultTenantId = defaultTenantId;
    }

    // Alternative constructor for convenience
    public AmbientTenantAccessor() : this(Guid.Empty)
    {
    }

    public static void Set(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));

        _ambient.Value = tenantId;
    }

    public static void Set(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId cannot be null or empty.", nameof(tenantId));

        if (!Guid.TryParse(tenantId, out var guid))
            throw new ArgumentException("TenantId must be a valid GUID.", nameof(tenantId));

        _ambient.Value = guid;
    }

    public static void Clear() => _ambient.Value = null;

    public Guid TenantId => _ambient.Value ?? _defaultTenantId;

    public bool IsDevelopment => false; // AmbientTenantAccessor is typically used in CLI/non-HTTP contexts
}

/// <summary>
/// Configuration options for tenancy.
/// Moved here as a simple POCO to avoid Options dependency.
/// </summary>
public sealed class TenancyOptions
{
    public Guid DefaultTenantId { get; set; } = Guid.Empty;
    public string DefaultPackage { get; set; } = "default";
    public string DefaultChannel { get; set; } = "dev";
}