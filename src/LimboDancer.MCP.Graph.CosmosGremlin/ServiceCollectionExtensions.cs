using Gremlin.Net.Driver;
using LimboDancer.MCP.Core.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LimboDancer.MCP.Graph.CosmosGremlin;

/// <summary>
/// Extension methods for dependency injection configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// DI helper registration for Cosmos Gremlin client factory.
    /// </summary>
    public static IServiceCollection AddCosmosGremlin(
        this IServiceCollection services,
        IConfiguration config,
        string sectionName = "CosmosGremlin")
    {
        services.Configure<GremlinOptions>(config.GetSection(sectionName));
        services.AddSingleton<IGremlinClientFactory, GremlinClientFactory>();
        return services;
    }

    /// <summary>
    /// Complete DI registration for Cosmos Gremlin including GraphStore.
    /// Registers: Options, Factory, Client, GraphStore, Preconditions
    /// </summary>
    public static IServiceCollection AddCosmosGremlinGraph(
        this IServiceCollection services,
        IConfiguration config,
        string sectionName = "CosmosGremlin")
    {
        // Register options
        services.Configure<GremlinOptions>(config.GetSection(sectionName));

        // Register factory and client
        services.AddSingleton<IGremlinClientFactory, GremlinClientFactory>();
        services.AddSingleton<IGremlinClient>(sp =>
            sp.GetRequiredService<IGremlinClientFactory>().Create());

        // Register GraphStore (scoped to respect tenant context)
        services.AddScoped<GraphStore>(sp =>
        {
            var client = sp.GetRequiredService<IGremlinClient>();
            var tenant = sp.GetRequiredService<ITenantAccessor>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new GraphStore(client, tenant, loggerFactory);
        });

        // Register Preconditions
        services.AddScoped<Preconditions>(sp =>
        {
            var client = sp.GetRequiredService<IGremlinClient>();
            var tenant = sp.GetRequiredService<ITenantAccessor>();
            var logger = sp.GetRequiredService<ILogger<Preconditions>>();
            return new Preconditions(client, () => tenant.TenantId, logger);
        });

        return services;
    }
}