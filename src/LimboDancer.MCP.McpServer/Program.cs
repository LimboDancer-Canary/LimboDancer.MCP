//file: /src/LimboDancer.MCP.McpServer/Program.cs
using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.Graph.CosmosGremlin;
using LimboDancer.MCP.McpServer;
using LimboDancer.MCP.McpServer.Configuration;
using LimboDancer.MCP.McpServer.DependencyInjection;
using LimboDancer.MCP.McpServer.Graph;
using LimboDancer.MCP.McpServer.Storage;
using LimboDancer.MCP.McpServer.Tenancy;
using LimboDancer.MCP.McpServer.Tools;
using LimboDancer.MCP.McpServer.Transport;
using LimboDancer.MCP.McpServer.Vector;
using LimboDancer.MCP.Ontology.Export;
using LimboDancer.MCP.Ontology.Mapping;
using LimboDancer.MCP.Ontology.Repositories;
using LimboDancer.MCP.Ontology.Runtime;
using LimboDancer.MCP.Ontology.Store;
using LimboDancer.MCP.Ontology.Validation;
using LimboDancer.MCP.Storage;
using LimboDancer.MCP.Vector.AzureSearch;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using IGraphPreconditionsService = LimboDancer.MCP.McpServer.Graph.IGraphPreconditionsService;
using ITenantScopeAccessor = LimboDancer.MCP.McpServer.Tenancy.ITenantScopeAccessor;
using TenancyOptions = LimboDancer.MCP.McpServer.Tenancy.TenancyOptions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;

// Options
services.Configure<TenancyOptions>(config.GetSection("Tenancy"));
services.Configure<VectorOptions>(config.GetSection("Vector"));
services.Configure<GremlinOptions>(config.GetSection("Graph"));

// Tenancy
services.AddHttpContextAccessor();
services.AddScoped<ITenantAccessor, HttpTenantAccessor>();
services.AddScoped<ITenantScopeAccessor, TenantScopeAccessor>();

// Ontology runtime
services.AddOntologyRuntime(config);

// Property key mapper (required by GraphPreconditionsService)
services.AddSingleton<IPropertyKeyMapper, DefaultPropertyKeyMapper>();

// Storage
services.AddStorage(config);

// Graph - Fixed to use the extension method properly
services.AddCosmosGremlinGraph(config);

// Register IGraphStore implementation
services.AddScoped<IGraphStore, TenantScopedGraphStore>();
services.AddScoped<GraphPreconditionsService>();
services.AddScoped<GraphEffectsService>();
services.AddScoped<IGraphEffectsService>(sp => sp.GetRequiredService<GraphEffectsService>());
services.AddScoped<IGraphPreconditionsService>(sp => sp.GetRequiredService<GraphPreconditionsService>());
services.AddScoped<IGraphQueryStore, GraphQueryStore>();

// Vector store
services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<VectorOptions>>().Value;
    return new VectorStore(new Uri(opts.Endpoint), new Azure.AzureKeyCredential(opts.ApiKey), opts.IndexName);
});
services.AddScoped<VectorSearchService>();

// History - Updated to use unified HistoryService
services.AddScoped<HistoryService>();
services.AddScoped<IHistoryService>(sp => sp.GetRequiredService<HistoryService>());
services.AddScoped<IHistoryReader>(sp => sp.GetRequiredService<HistoryService>());
services.AddScoped<IHistoryStore>(sp => sp.GetRequiredService<HistoryService>());

// MCP tools - All 4 tools registered
services.AddScoped<HistoryGetTool>();
services.AddScoped<HistoryAppendTool>();
services.AddScoped<GraphQueryTool>();
services.AddScoped<MemorySearchTool>();

// MCP Server - NEW!
services.AddMcpServer();

// Authentication (if needed for HTTP mode)
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Authority = config["Authentication:Authority"];
        options.Audience = config["Authentication:Audience"];
    });

services.AddAuthorization();

// Add API controllers for HTTP mode
services.AddControllers();

// Build the app
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseAuthentication();
app.UseAuthorization();

// Map MCP endpoints
app.MapMcpEndpoints();

// Add other endpoints (health check, etc.)
app.MapGet("/health", () => "OK");

app.Run();