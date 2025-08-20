using LimboDancer.MCP.Ontology.Export;
using LimboDancer.MCP.Ontology.JsonLd;
using LimboDancer.MCP.Ontology.Repositories;
using LimboDancer.MCP.Ontology.Repositories.Cosmos;
using LimboDancer.MCP.Ontology.Store;
using LimboDancer.MCP.Ontology.Validation;
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

        // Validation and governance
        services.AddSingleton<OntologyValidators>();
        services.AddSingleton<PublishGates>();

        // Context + export
        services.AddScoped<JsonLdContextBuilder>();
        services.AddScoped<JsonLdExportService>();
        services.AddScoped<RdfExportService>();

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