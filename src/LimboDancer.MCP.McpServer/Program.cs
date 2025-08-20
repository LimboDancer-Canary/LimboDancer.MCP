using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.Graph.CosmosGremlin;
using LimboDancer.MCP.McpServer.DependencyInjection;
using LimboDancer.MCP.McpServer.Storage;
using LimboDancer.MCP.McpServer.Tenancy;
using LimboDancer.MCP.McpServer.Tools;
using LimboDancer.MCP.McpServer.Vector;
using LimboDancer.MCP.Storage;
using LimboDancer.MCP.Vector.AzureSearch;
using Microsoft.Extensions.DependencyInjection;

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

// Ontology runtime (validators, store, json-ld, repo)
services.AddOntologyRuntime(config);

// Storage (EF Core + repositories)
services.AddStorage(config);

// Graph
services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GraphOptions>>().Value;
    return new GraphStore(opts.Endpoint, opts.Database, opts.Graph, opts.AuthKey);
});
services.AddScoped<GraphPreconditionsService>();   // flows TenantScope into preconditions
services.AddScoped<GraphEffectsService>();         // flows TenantScope into effects

// Vector store + tenant-scoped search facade
services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<VectorOptions>>().Value;
    return new VectorStore(new Uri(opts.Endpoint), opts.ApiKey, opts.IndexName, opts.VectorDimensions);
});
services.AddScoped<VectorSearchService>();

// History service wrapper to ensure tenant tagging and future ontology annotations
services.AddScoped<IHistoryService, HistoryService>();

// MCP tools
services.AddScoped<IMcpTool, HistoryGetTool>();
services.AddScoped<IMcpTool, HistoryAppendTool>();
services.AddScoped<IMcpTool, GraphQueryTool>();

var app = builder.Build();

// The MCP Server host wiring would go here (endpoints/stdio). This file focuses on DI.

// Basic health for container app readiness
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// Options records
internal sealed record VectorOptions(string Endpoint = "", string ApiKey = "", string IndexName = "mcp-memory", int VectorDimensions = 1536);
internal sealed record GraphOptions(string Endpoint = "", string Database = "", string Graph = "", string AuthKey = "");