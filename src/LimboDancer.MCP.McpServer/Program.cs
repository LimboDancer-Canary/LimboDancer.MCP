//file: /src/LimboDancer.MCP.McpServer/Program.cs
using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.Graph.CosmosGremlin;
using LimboDancer.MCP.McpServer.DependencyInjection;
using LimboDancer.MCP.McpServer.Graph;
using LimboDancer.MCP.McpServer.Storage;
using LimboDancer.MCP.McpServer.Tenancy;
using LimboDancer.MCP.McpServer.Tools;
using LimboDancer.MCP.McpServer.Vector;
using LimboDancer.MCP.Ontology.Export;
using LimboDancer.MCP.Ontology.Mapping;
using LimboDancer.MCP.Ontology.Repositories;
using LimboDancer.MCP.Ontology.Runtime;
using LimboDancer.MCP.Ontology.Store;
using LimboDancer.MCP.Storage;
using LimboDancer.MCP.Vector.AzureSearch;
using Microsoft.EntityFrameworkCore;
using LimboDancer.MCP.Ontology.Validation;
using IGraphPreconditionsService = LimboDancer.MCP.McpServer.Graph.IGraphPreconditionsService;
using ITenantScopeAccessor = LimboDancer.MCP.McpServer.Tenancy.ITenantScopeAccessor;

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
    return new VectorStore(new Uri(opts.Endpoint), opts.ApiKey, opts.IndexName, opts.VectorDimensions);
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

var app = builder.Build();

// Health endpoints
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/ready", async (IServiceProvider sp) =>
{
    try
    {
        // Check database connectivity
        using var scope = sp.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        var canConnect = await dbContext.Database.CanConnectAsync();

        if (!canConnect)
            return Results.Json(new { status = "not ready", database = "disconnected" }, statusCode: 503);

        // Initialize persistence if needed
        if (config.GetValue<bool>("Storage:ApplyMigrationsAtStartup"))
        {
            await dbContext.Database.MigrateAsync();
        }

        return Results.Ok(new { status = "ready", database = "connected" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "not ready", error = ex.Message }, statusCode: 503);
    }
});

// Ontology endpoints
app.MapGet("/api/ontology/validate", async (HttpContext http, IOntologyRepository repo, CancellationToken ct) =>
{
    string tenant = http.Request.Query["tenant"];
    string package = http.Request.Query["package"];
    string channel = http.Request.Query["channel"];

    if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(package) || string.IsNullOrWhiteSpace(channel))
        return Results.Json(new { error = "Missing required query params: tenant, package, channel." }, statusCode: 400);

    var scope = new TenantScope(tenant, package, channel);

    try
    {
        var store = new OntologyStore(repo);
        await store.LoadAsync(scope, ct);

        var validation = OntologyValidator.Validate(store, scope);

        return Results.Ok(new
        {
            scope = scope.ToString(),
            isValid = validation.Errors.Count == 0,
            errors = validation.Errors
        });
    }
    catch (TenantScopeException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 400);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ontology validation error");
        return Results.Json(new { error = "An error occurred during validation." }, statusCode: 500);
    }
});

app.MapPost("/api/ontology/validate", async (HttpContext http, IOntologyRepository repo, OntologyValidationRequest request, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.TenantId) || string.IsNullOrWhiteSpace(request.PackageId) || string.IsNullOrWhiteSpace(request.ChannelId))
        return Results.Json(new { error = "Missing required fields: tenantId, packageId, channelId." }, statusCode: 400);

    var scope = new TenantScope(request.TenantId, request.PackageId, request.ChannelId);

    try
    {
        var store = new OntologyStore(repo);
        await store.LoadAsync(scope, ct);

        var validation = OntologyValidator.Validate(store, scope);
        return Results.Ok(new
        {
            scope = scope.ToString(),
            isValid = validation.Errors.Count == 0,
            errors = validation.Errors
        });
    }
    catch (TenantScopeException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 400);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ontology validation error");
        return Results.Json(new { error = "An error occurred during validation." }, statusCode: 500);
    }
});

app.MapGet("/api/ontology/export", async (
    HttpContext http,
    IOntologyRepository repo,
    JsonLdExportService jsonLdExport,
    RdfExportService rdfExport,
    CancellationToken ct) =>
{
    string tenant = http.Request.Query["tenant"];
    string package = http.Request.Query["package"];
    string channel = http.Request.Query["channel"];
    string format = http.Request.Query["format"].ToString().ToLowerInvariant();

    if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(package) || string.IsNullOrWhiteSpace(channel))
        return Results.Json(new { error = "Missing required query params: tenant, package, channel." }, statusCode: 400);

    if (string.IsNullOrWhiteSpace(format))
        format = "jsonld";

    var scope = new TenantScope(tenant, package, channel);
    var baseNamespace = $"https://ontology.limbodancer.mcp/{tenant}/{package}/{channel}#";

    try
    {
        switch (format)
        {
            case "jsonld":
            case "json-ld":
                var jsonLd = await jsonLdExport.ExportStringAsync(scope, baseNamespace, indented: true, ct);
                return Results.Text(jsonLd, "application/ld+json");

            case "turtle":
            case "ttl":
                var ttl = await rdfExport.ExportTurtleAsync(scope, baseNamespace, ct);
                return Results.Text(ttl, "text/turtle");

            default:
                return Results.Json(new { error = $"Unsupported format: {format}. Use jsonld or turtle." }, statusCode: 400);
        }
    }
    catch (TenantScopeException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 400);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ontology export error");
        return Results.Json(new { error = "An error occurred during export." }, statusCode: 500);
    }
});

app.Run();

// Request DTOs
public record OntologyValidationRequest(string TenantId, string PackageId, string ChannelId);

// Options classes
public sealed class VectorOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string IndexName { get; set; } = "ldm-memory";
    public int VectorDimensions { get; set; } = 1536;
}

public sealed class GraphOptions
{
    // Any graph-specific options can go here
}