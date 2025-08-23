using Microsoft.Extensions.Options;
using LimboDancer.MCP.Core.Tenancy;

namespace LimboDancer.MCP.McpServer.Extensions;

/// <summary>
/// Extension methods for registering tenant accessor services.
/// This belongs in a host project, not in Core.
/// </summary>
public static class TenancyServiceCollectionExtensions
{
    /// <summary>
    /// Registers ITenantAccessor as singleton using AmbientTenantAccessor with TenancyOptions.
    /// </summary>
    public static IServiceCollection AddAmbientTenantAccessor(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TenancyOptions>(configuration.GetSection("Tenancy"));

        services.AddSingleton<ITenantAccessor>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<TenancyOptions>>();
            return TenantAccessorFactory.CreateAmbient(options.Value);
        });

        return services;
    }
}