using LimboDancer.MCP.Ontology.Export;
using LimboDancer.MCP.Ontology.Repositories;
using LimboDancer.MCP.Ontology.Repositories.Cosmos;
using LimboDancer.MCP.Ontology.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LimboDancer.MCP.McpServer.DependencyInjection;

internal static class OntologyServiceCollectionExtensions
{
    public static IServiceCollection AddOntologyRuntime(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<OntologyCosmosOptions>(config.GetSection("Ontology:Cosmos"));

        // Repository (authoritative store)
        services.AddSingleton<IOntologyRepository>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OntologyCosmosOptions>>().Value;
            return new CosmosOntologyRepository(opts);
        });

        // In-memory read store built over the repository
        services.AddScoped<OntologyStore>();

        // Export services
        services.AddScoped<JsonLdExportService>();
        services.AddScoped<RdfExportService>();

        // Notes:
        // - OntologyValidators and PublishGates are static utility classes; they are not registered in DI.
        // - JsonLdContextBuilder is also static and used internally by JsonLdExportService.

        return services;
    }
}