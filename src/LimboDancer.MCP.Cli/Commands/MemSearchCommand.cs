using LimboDancer.MCP.Vector.AzureSearch;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using LimboDancer.MCP.Core.Tenancy;

namespace LimboDancer.MCP.Cli.Commands;

internal static class MemSearchCommand
{
    public static Command Build()
    {
        var cmd = new Command("mem", "Vector memory ops");
        var search = new Command("search", "Hybrid search (BM25 + vector)");

        var query = new Option<string?>("--query", "BM25/Semantic query text");
        var k = new Option<int>("--k", () => 8, "Top K");
        var oc = new Option<string?>("--class", "Filter: ontologyClass");
        var uri = new Option<string?>("--uri", "Filter: URI equals");
        var tags = new Option<string[]>("--tag", parseArgument: r => r.Tokens.Select(t => t.Value).ToArray(), description: "Filter: any tag");
        var tenant = new Option<string?>("--tenant", "Tenant Id (GUID). Defaults in Development from config.");
        var package = new Option<string?>("--package", "Optional package identifier.");
        var channel = new Option<string?>("--channel", "Optional channel identifier.");

        search.AddOption(query); search.AddOption(k); search.AddOption(oc); search.AddOption(uri); search.AddOption(tags);
        search.AddOption(tenant); search.AddOption(package); search.AddOption(channel);

        search.SetHandler(async (string? q, int topK, string? klass, string? uriEq, string[] tagArr, string? tenantOpt, string? _pkg, string? _chan) =>
        {
            using var host = Bootstrap.BuildHost();
            ApplyTenant(host, tenantOpt);

            var store = host.Services.GetRequiredService<VectorStore>();

            var filters = new VectorStore.SearchFilters
            {
                OntologyClass = klass,
                UriEquals = uriEq,
                TagsAny = tagArr?.Length > 0 ? tagArr : null
            };

            var res = await store.SearchHybridAsync(q, vector: null, k: topK, filters: filters);
            foreach (var r in res)
            {
                Console.WriteLine($"[{r.Score:F3}] {r.Title}  {r.Source}#{r.Chunk}  class={r.OntologyClass}  tags=[{string.Join(",", r.Tags ?? Array.Empty<string>())}]");
                var prev = r.Content?.Length > 200 ? r.Content[..200] + "…" : r.Content;
                Console.WriteLine(prev + "\n");
            }
        }, query, k, oc, uri, tags, tenant, package, channel);

        cmd.AddCommand(search);
        return cmd;
    }

    private static void ApplyTenant(IHost host, string? tenantOpt)
    {
        var env = host.Services.GetRequiredService<IHostEnvironment>();
        var cfg = host.Services.GetRequiredService<IConfiguration>();
        var accessor = host.Services.GetRequiredService<ITenantAccessor>();

        if (!string.IsNullOrWhiteSpace(tenantOpt) && Guid.TryParse(tenantOpt, out var tidFromOpt))
        {
            accessor.TenantId = tidFromOpt;
            return;
        }

        if (env.IsDevelopment())
        {
            var cfgTenant = cfg["Tenant"];
            if (Guid.TryParse(cfgTenant, out var tidFromCfg))
            {
                accessor.TenantId = tidFromCfg;
            }
        }
    }
}