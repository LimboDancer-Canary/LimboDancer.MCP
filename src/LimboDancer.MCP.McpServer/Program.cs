using Gremlin.Net.Driver;
using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.Graph.CosmosGremlin;
using LimboDancer.MCP.McpServer.DependencyInjection;
using LimboDancer.MCP.McpServer.Graph;
using LimboDancer.MCP.McpServer.Storage;
using LimboDancer.MCP.McpServer.Tenancy;
using LimboDancer.MCP.McpServer.Tools;
using LimboDancer.MCP.McpServer.Vector;
using LimboDancer.MCP.Ontology.Export;
using LimboDancer.MCP.Ontology.Repositories;
using LimboDancer.MCP.Ontology.Runtime;
using LimboDancer.MCP.Ontology.Store;
using LimboDancer.MCP.Ontology.Validation;
using LimboDancer.MCP.Storage;
using LimboDancer.MCP.Vector.AzureSearch;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;

// Options
services.Configure<TenancyOptions>(config.GetSection("Tenancy"));
services.Configure<VectorOptions>(config.GetSection("Vector"));
services.Configure<GraphOptions>(config.GetSection("Graph"));

// Tenancy
services.AddHttpContextAccessor();
services.AddScoped<ITenantAccessor, HttpTenantAccessor>();
services.AddScoped<ITenantScopeAccessor, TenantScopeAccessor>();

// Ontology runtime
services.AddOntologyRuntime(config);

// Storage
services.AddStorage(config);

// Graph
// (Assumes a Gremlin client factory or similar already registered elsewhere; adjust as needed)
services.AddSingleton(sp =>
{
    // Acquire or build the Gremlin client (placeholder; replace with actual retrieval)
    var gremlinClient = sp.GetRequiredService<IGremlinClient>();
    var tenantAccessor = sp.GetRequiredService<ITenantAccessor>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return new GraphStore(gremlinClient, tenantAccessor, loggerFactory);
});
services.AddScoped<GraphPreconditionsService>();
services.AddScoped<GraphEffectsService>();

// Vector store
services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<VectorOptions>>().Value;
    return new VectorStore(new Uri(opts.Endpoint), opts.ApiKey, opts.IndexName, opts.VectorDimensions);
});
services.AddScoped<VectorSearchService>();

// History
services.AddScoped<IHistoryService, HistoryService>();

// MCP tools
services.AddScoped<IMcpTool, HistoryGetTool>();
services.AddScoped<IMcpTool, HistoryAppendTool>();
services.AddScoped<IMcpTool, GraphQueryTool>();

var app = builder.Build();

// Endpoints (unchanged)
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/ontology/validate", async (HttpContext http, IOntologyRepository repo, CancellationToken ct) =>
{
    string tenant = http.Request.Query["tenant"];
    string package = http.Request.Query["package"];
    string channel = http.Request.Query["channel"];

    if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(package) || string.IsNullOrWhiteSpace(channel))
        return Results.BadRequest(new { error = "Missing required query params: tenant, package, channel." });

    var scope = new TenantScope(tenant, package, channel);

    var store = new OntologyStore(repo);
    try
    {
        await store.LoadAsync(scope, ct).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }

    var validation = OntologyValidator.Validate(store, scope);
    return Results.Ok(new
    {
        scope,
        validation.Errors
    });
});

app.Run();