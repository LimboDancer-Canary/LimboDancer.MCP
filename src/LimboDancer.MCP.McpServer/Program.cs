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
using Microsoft.Extensions.Logging;
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

// Tenancy
services.AddHttpContextAccessor();
services.AddScoped<ITenantAccessor, HttpTenantAccessor>();
services.AddScoped<ITenantScopeAccessor, TenantScopeAccessor>();

// Ontology runtime (validators, store, json-ld, repo)
services.AddOntologyRuntime(config);

// Storage (EF Core + repositories)
services.AddStorage(config);

// Graph
services.AddCosmosGremlin(config, "Graph");
services.AddScoped<GraphStore>(sp =>
{
    var clientFactory = sp.GetRequiredService<IGremlinClientFactory>();
    var tenantAccessor = sp.GetRequiredService<ITenantAccessor>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    
    var client = clientFactory.Create();
    return new GraphStore(client, tenantAccessor, loggerFactory);
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

// -----------------------------------------------------------------------------
// HTTP endpoints
// -----------------------------------------------------------------------------

// Basic health for container app readiness
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Ontology: validate the currently-scoped channel
// GET /api/ontology/validate?tenant=...&package=...&channel=...
app.MapGet("/api/ontology/validate", async (HttpContext http, IOntologyRepository repo, CancellationToken ct) =>
{
    string tenant = http.Request.Query["tenant"];
    string package = http.Request.Query["package"];
    string channel = http.Request.Query["channel"];

    if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(package) || string.IsNullOrWhiteSpace(channel))
        return Results.BadRequest(new { error = "Missing required query params: tenant, package, channel." });

    var scope = new TenantScope(tenant, package, channel);

    // Load ontology into in-memory store
    var store = new OntologyStore(repo);
    try
    {
        await store.LoadAsync(scope, ct).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        // If repository backend is not implemented, surface a clear error
        return Results.Problem(title: "Ontology load failed", detail: ex.Message, statusCode: 500);
    }

    // Aggregate validation results
    var errors = new List<string>();

    // Entity referentials are checked during LoadAsync -> ValidateReferentials (throws). If we reached here, parents exist.

    foreach (var p in store.Properties())
        errors.AddRange(OntologyValidators.ValidateProperty(scope, p, store));
    foreach (var r in store.Relations())
        errors.AddRange(OntologyValidators.ValidateRelation(scope, r, store));
    foreach (var e in store.Enums())
        errors.AddRange(OntologyValidators.ValidateEnum(scope, e));
    foreach (var s in store.Shapes())
        errors.AddRange(OntologyValidators.ValidateShape(scope, s, store));

    var result = new
    {
        ok = errors.Count == 0,
        scope = scope.ToString(),
        counts = new
        {
            entities = store.Entities().Count(),
            properties = store.Properties().Count(),
            relations = store.Relations().Count(),
            enums = store.Enums().Count(),
            aliases = store.Aliases().Count(),
            shapes = store.Shapes().Count()
        },
        errors
    };

    return Results.Ok(result);
});

// Ontology: export using services (JSON-LD or TTL)
// GET /api/ontology/export?tenant=...&package=...&channel=...&format=jsonld|ttl[&base=...]
app.MapGet("/api/ontology/export", async (
    HttpContext http,
    JsonLdExportService jsonld,
    RdfExportService rdf,
    CancellationToken ct) =>
{
    string tenant = http.Request.Query["tenant"];
    string package = http.Request.Query["package"];
    string channel = http.Request.Query["channel"];
    string format = http.Request.Query["format"];
    string baseNs = http.Request.Query["base"];

    if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(package) || string.IsNullOrWhiteSpace(channel))
        return Results.BadRequest(new { error = "Missing required query params: tenant, package, channel." });

    var scope = new TenantScope(tenant, package, channel);
    if (string.IsNullOrWhiteSpace(baseNs))
    {
        // Reasonable default base namespace per tenant/package/channel
        baseNs = $"https://{tenant}.limbodancer.ai/ontology/{package}/{channel}#";
    }
    else if (!baseNs.EndsWith("#") && !baseNs.EndsWith("/"))
    {
        baseNs += "#";
    }

    format = string.IsNullOrWhiteSpace(format) ? "jsonld" : format.Trim().ToLowerInvariant();

    try
    {
        if (format is "jsonld")
        {
            var doc = await jsonld.ExportAsync(scope, baseNs, ct).ConfigureAwait(false);
            return Results.Json(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        else if (format is "ttl" or "turtle")
        {
            var ttl = await rdf.ExportTurtleAsync(scope, baseNs, ct).ConfigureAwait(false);
            return Results.Text(ttl, "text/turtle");
        }
        else
        {
            return Results.BadRequest(new { error = "Unsupported format. Use 'jsonld' or 'ttl'." });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(title: "Ontology export failed", detail: ex.Message, statusCode: 500);
    }
});

// MCP: List tools
app.MapGet("/mcp/tools", (IEnumerable<IMcpTool> tools) =>
{
    var list = tools.Select(t => new
    {
        name = t.Name,
        descriptor = t.ToolDescriptor
    });
    return Results.Ok(list);
});

// MCP: Call tool by name
// POST /mcp/tools/{name}/call
// Body: { "arguments": { ... } } OR a plain object of arguments { ... }
app.MapPost("/mcp/tools/{name}/call", async (string name, HttpRequest req, IEnumerable<IMcpTool> tools, CancellationToken ct) =>
{
    var tool = tools.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
    if (tool is null) return Results.NotFound(new { error = $"Tool '{name}' not found." });

    using var doc = await JsonDocument.ParseAsync(req.Body, cancellationToken: ct);
    Dictionary<string, object?> args;
    try
    {
        if (doc.RootElement.ValueKind == JsonValueKind.Object &&
            doc.RootElement.TryGetProperty("arguments", out var argsProp) &&
            argsProp.ValueKind == JsonValueKind.Object)
        {
            args = argsProp.EnumerateObject().ToDictionary(p => p.Name, p => (object?)JsonElementToDotNet(p.Value));
        }
        else
        {
            args = doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => (object?)JsonElementToDotNet(p.Value));
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Invalid JSON body: {ex.Message}" });
    }

    try
    {
        var result = await tool.CallAsync(args, ct);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Helpers
static object? JsonElementToDotNet(JsonElement el)
{
    return el.ValueKind switch
    {
        JsonValueKind.Undefined => null,
        JsonValueKind.Null => null,
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.TryGetDouble(out var d) ? d : el.ToString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Object => el.EnumerateObject().ToDictionary(p => p.Name, p => JsonElementToDotNet(p.Value)),
        JsonValueKind.Array => el.EnumerateArray().Select(JsonElementToDotNet).ToArray(),
        _ => el.ToString()
    };
}

app.Run();

// Options records
internal sealed record VectorOptions(string Endpoint = "", string ApiKey = "", string IndexName = "mcp-memory", int VectorDimensions = 1536);