using LimboDancer.MCP.Vector.AzureSearch;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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

        search.AddOption(query); search.AddOption(k); search.AddOption(oc); search.AddOption(uri); search.AddOption(tags);

        search.SetHandler(async (string? q, int topK, string? klass, string? uriEq, string[] tagArr) =>
        {
            using var host = Bootstrap.BuildHost();
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
        }, query, k, oc, uri, tags);

        cmd.AddCommand(search);
        return cmd;
    }
}
