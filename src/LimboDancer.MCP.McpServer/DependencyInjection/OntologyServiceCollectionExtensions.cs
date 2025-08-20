using LimboDancer.MCP.Ontology.Export;
using LimboDancer.MCP.Ontology.Repositories;
using LimboDancer.MCP.Ontology.Repositories.Cosmos;
using LimboDancer.MCP.Ontology.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.MCP.McpServer.DependencyInjection;

internal static class OntologyServiceCollectionExtensions
{
    public static IServiceCollection AddOntologyRuntime(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<OntologyCosmosOptions>(config.GetSection("Ontology:Cosmos"));

        // Repository (authoritative store)
        services.AddSingleton<IOntologyRepository>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OntologyCosmosOptions>>().Value;
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

// Options for the Cosmos repository
internal sealed class OntologyCosmosOptions
{
    public string AccountEndpoint { get; set; } = string.Empty;
    public string AccountKey { get; set; } = string.Empty;
    public string Database { get; set; } = "ontology";
    public string Container { get; set; } = "catalog";
    public string LeasesContainer { get; set; } = "leases";
}