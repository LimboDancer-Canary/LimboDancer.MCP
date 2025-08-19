using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using LimboDancer.MCP.Graph.CosmosGremlin;
using LimboDancer.MCP.Storage;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Config
var cfg = builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// EF Core — use factory for Blazor Server safety
builder.Services.AddDbContextFactory<ChatDbContext>(opt =>
{
    var cs = cfg["Persistence:ConnectionString"]
             ?? "Host=localhost;Port=5432;Database=limbodancer_dev;Username=postgres;Password=postgres";
    opt.UseNpgsql(cs);
});

// Azure AI Search clients
builder.Services.AddSingleton<SearchIndexClient>(_ =>
{
    var endpoint = new Uri(cfg["Search:Endpoint"] ?? "https://example.search.windows.net");
    var key = new AzureKeyCredential(cfg["Search:ApiKey"] ?? "replace-me");
    return new SearchIndexClient(endpoint, key);
});
builder.Services.AddSingleton<SearchClient>(_ =>
{
    var endpoint = new Uri(cfg["Search:Endpoint"] ?? "https://example.search.windows.net");
    var key = new AzureKeyCredential(cfg["Search:ApiKey"] ?? "replace-me");
    var index = cfg["Search:Index"] ?? "ldm-memory";
    return new SearchClient(endpoint, index, key);
});

// Cosmos Gremlin
builder.Services.AddCosmosGremlin(builder.Configuration, sectionName: "CosmosGremlin");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();