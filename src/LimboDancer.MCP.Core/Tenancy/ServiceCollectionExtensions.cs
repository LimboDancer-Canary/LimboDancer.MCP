using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace LimboDancer.MCP.Core.Tenancy;

/// <summary>
/// Extension methods for registering tenant accessor services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ITenantAccessor as singleton using AmbientTenantAccessor with TenancyOptions.
    /// </summary>
    public static IServiceCollection AddAmbientTenantAccessor(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TenancyOptions>(configuration.GetSection("Tenancy"));
        services.AddSingleton<ITenantAccessor, AmbientTenantAccessor>();
        return services;
    }
}