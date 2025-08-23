using LimboDancer.MCP.Core.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.MCP.Cli.Commands;

/// <summary>
/// Extension methods for configuring tenancy services in CLI.
/// </summary>
public static class TenancyExtensions
{
    /// <summary>
    /// Add ambient tenant accessor for CLI scenarios.
    /// </summary>
    public static IServiceCollection AddAmbientTenantAccessor(this IServiceCollection services, IConfiguration configuration)
    {
        // For CLI scenarios, we use AmbientTenantAccessor with a default tenant
        services.AddSingleton<ITenantAccessor>(sp =>
        {
            var accessor = new AmbientTenantAccessor();

            // Try to get tenant from configuration
            var tenantId = configuration["Tenancy:DefaultTenantId"];
            if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var guid))
            {
                AmbientTenantAccessor.Set(guid);
            }
            else
            {
                // Use a default tenant for development
                AmbientTenantAccessor.Set(new Guid("00000000-0000-0000-0000-000000000001"));
            }

            return accessor;
        });

        return services;
    }
}