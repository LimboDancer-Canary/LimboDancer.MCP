namespace LimboDancer.MCP.Core.Tenancy;

/// <summary>
/// Factory for creating tenant accessors without DI dependencies.
/// The actual DI registration should be done in the host projects.
/// </summary>
public static class TenantAccessorFactory
{
    /// <summary>
    /// Creates an ambient tenant accessor with the specified default tenant ID.
    /// </summary>
    public static ITenantAccessor CreateAmbient(Guid defaultTenantId = default)
    {
        return new AmbientTenantAccessor(defaultTenantId);
    }

    /// <summary>
    /// Creates an ambient tenant accessor with the specified default tenant ID string.
    /// </summary>
    public static ITenantAccessor CreateAmbient(string? defaultTenantId)
    {
        if (string.IsNullOrWhiteSpace(defaultTenantId) || !Guid.TryParse(defaultTenantId, out var guid))
        {
            return new AmbientTenantAccessor(Guid.Empty);
        }
        return new AmbientTenantAccessor(guid);
    }

    /// <summary>
    /// Creates an ambient tenant accessor from tenancy options.
    /// </summary>
    public static ITenantAccessor CreateAmbient(TenancyOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        return new AmbientTenantAccessor(options.DefaultTenantId);
    }
}

/*
 Usage in host projects that have DI:

 // In your Startup.cs or Program.cs:
 services.Configure<TenancyOptions>(configuration.GetSection("Tenancy"));
 services.AddSingleton<ITenantAccessor>(sp =>
 {
     var options = sp.GetRequiredService<IOptions<TenancyOptions>>();
     return TenantAccessorFactory.CreateAmbient(options.Value);
 });

 // Or simpler:
 var tenantId = configuration["Tenancy:DefaultTenantId"];
 services.AddSingleton<ITenantAccessor>(TenantAccessorFactory.CreateAmbient(tenantId));
 */