using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.MCP.Core.Tenancy;

/// <summary>
/// Extension methods for registering tenant accessor services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ITenantAccessor as singleton using AmbientTenantAccessor.
    /// </summary>
    public static IServiceCollection AddAmbientTenantAccessor(this IServiceCollection services)
    {
        services.AddSingleton<ITenantAccessor, AmbientTenantAccessor>();
        return services;
    }
}