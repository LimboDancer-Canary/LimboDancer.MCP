using LimboDancer.MCP.BlazorConsole.Services;
using LimboDancer.MCP.Ontology.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.MCP.BlazorConsole;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all BlazorConsole-specific services.
    /// </summary>
    public static IServiceCollection AddBlazorConsoleServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Tenant UI state (scoped per user connection)
        services.AddScoped<TenantUiState>();

        // HTTP handler for tenant headers
        services.AddTransient<TenantHeaderHandler>();

        // Ontology validation service with HTTP client configuration
        services.AddOntologyValidationService(configuration);

        // Property key mapper for ontology predicate to graph key mapping
        services.AddSingleton<IPropertyKeyMapper, DefaultPropertyKeyMapper>();

        return services;
    }
}