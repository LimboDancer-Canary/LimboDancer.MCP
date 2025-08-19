using System.CommandLine;
using Azure.Search.Documents.Indexes;
using LimboDancer.MCP.Vector.AzureSearch;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.MCP.Cli.Commands;

internal static class VectorInitCommand
{
    public static Command Build()
    {
        var cmd = new Command("vector", "Vector index ops");
        var init = new Command("init", "Ensure Azure AI Search index exists");

        var dim = new Option<int>("--dim", () => 1536, "Embedding dimensions");
        var profile = new Option<string>("--profile", () => SearchIndexBuilder.DefaultVectorProfile, "Vector profile name");
        var index = new Option<string?>("--index", "Override index name");

        init.AddOption(dim); init.AddOption(profile); init.AddOption(index);

        init.SetHandler(async (int d, string prof, string? idxName) =>
        {
            using var host = Bootstrap.BuildHost();
            var ic = host.Services.GetRequiredService<SearchIndexClient>();

            var cfg = host.Services.GetRequiredService<IConfiguration>();
            var indexName = idxName ?? cfg["Search:Index"] ?? SearchIndexBuilder.DefaultIndexName;

            await SearchIndexBuilder.EnsureIndexAsync(ic, indexName, embeddingDimensions: d, vectorProfileName: prof);
            Console.WriteLine($"Index '{indexName}' ensured (dim={d}, profile={prof}).");
        }, dim, profile, index);

        cmd.AddCommand(init);
        return cmd;
    }
}