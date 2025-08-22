using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Gremlin.Net.Driver;
using LimboDancer.MCP.Core;
using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.Graph.CosmosGremlin;
using LimboDancer.MCP.Storage;
using LimboDancer.MCP.Vector.AzureSearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.MCP.Cli.Commands;

internal static class ServicesBootstrap
{
    public static void Configure(HostApplicationBuilder b)
    {
        var cfg = b.Configuration;

        // Tenancy
        b.Services.AddAmbientTenantAccessor(cfg);

        // EF Core (Chat) — Audit can be added later
        b.Services.AddDbContext<ChatDbContext>(opt =>
        {
            var cs = cfg["Persistence:ConnectionString"] ?? throw new InvalidOperationException("Missing Persistence:ConnectionString");
            opt.UseNpgsql(cs);
        });

        b.Services.AddScoped<IChatHistoryStore, ChatHistoryStore>();

        // Azure AI Search clients
        b.Services.AddSingleton<SearchIndexClient>(_ =>
        {
            var endpoint = new Uri(cfg["Search:Endpoint"]!);
            var key = new AzureKeyCredential(cfg["Search:ApiKey"]!);
            return new SearchIndexClient(endpoint, key);
        });

        b.Services.AddSingleton<SearchClient>(_ =>
        {
            var endpoint = new Uri(cfg["Search:Endpoint"]!);
            var key = new AzureKeyCredential(cfg["Search:ApiKey"]!);
            var index = cfg["Search:Index"] ?? "ldm-memory";
            return new SearchClient(endpoint, index, key);
        });

        // Vector store
        b.Services.AddSingleton<VectorStore>();

        // Gremlin + GraphStore
        b.Services.AddCosmosGremlinGraph(cfg);
    }
}