using Azure.Search.Documents;
using LimboDancer.MCP.Vector.AzureSearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.Security.Cryptography;
using System.Text;
using static LimboDancer.MCP.Vector.AzureSearch.VectorStore;
using Microsoft.Extensions.Hosting;
using LimboDancer.MCP.Core.Tenancy;

namespace LimboDancer.MCP.Cli.Commands;

internal static class MemAddCommand
{
    public static Command Build()
    {
        var cmd = new Command("mem", "Vector memory ops");
        var add = new Command("add", "Ingest text/file -> chunk -> embed -> upsert");

        var file = new Option<string?>("--file", "Path to a text/markdown file");
        var text = new Option<string?>("--text", "Raw text to ingest");
        var title = new Option<string?>("--title", () => "CLI Ingest", "Title");
        var source = new Option<string?>("--source", () => "cli", "Source");
        var klass = new Option<string?>("--class", () => "ldm:MemoryItem", "Ontology class");
        var tagsCsv = new Option<string?>("--tags", () => "kind:vector,source:cli", "CSV tags");
        var maxChars = new Option<int>("--max-chars", () => 1200, "Chunk size (characters)");
        var dim = new Option<int>("--dim", () => 1536, "Embedding dimensions (for random embedding)");
        var tenant = new Option<string?>("--tenant", "Tenant Id (GUID). Defaults in Development from config.");
        var package = new Option<string?>("--package", "Optional package identifier.");
        var channel = new Option<string?>("--channel", "Optional channel identifier.");

        add.AddOption(file); add.AddOption(text); add.AddOption(title); add.AddOption(source);
        add.AddOption(klass); add.AddOption(tagsCsv); add.AddOption(maxChars); add.AddOption(dim);
        add.AddOption(tenant); add.AddOption(package); add.AddOption(channel);

        add.SetHandler(async (string? file, string? text, string? title, string? source, string? klass, string? tagsCsv, int maxChars, int dim, string? tenantOpt, string? _pkg, string? _chan) =>
        {
            using var host = Bootstrap.BuildHost();
            ApplyTenant(host, tenantOpt);

            var cfg = host.Services.GetRequiredService<IConfiguration>();
            var client = host.Services.GetRequiredService<SearchClient>();

            // Deterministic random embedder (no external services needed)
            Task<float[]> EmbedAsync(string s)
            {
                var vec = new float[dim];
                var seed = SeedFrom(s);
                var rng = new Random(seed);
                for (int i = 0; i < dim; i++) vec[i] = (float)(rng.NextDouble() - 0.5); // [-0.5, 0.5)
                return Task.FromResult(vec);
            }

            var store = new VectorStore(client, EmbedAsync);

            var raw = string.IsNullOrWhiteSpace(file)
                ? text
                : await File.ReadAllTextAsync(file!);

            if (string.IsNullOrWhiteSpace(raw))
            {
                Console.Error.WriteLine("Provide --file or --text");
                return;
            }

            var chunks = ChunkBySentences(raw!, maxChars);
            Console.WriteLine($"Chunks: {chunks.Count}");

            var now = DateTimeOffset.UtcNow;
            var docs = new List<MemoryDoc>();
            for (int i = 0; i < chunks.Count; i++)
            {
                var id = Guid.NewGuid().ToString("N");
                docs.Add(new MemoryDoc
                {
                    Id = id,
                    Title = title,
                    Content = chunks[i],
                    Source = source,
                    Chunk = i,
                    CreatedAt = now,
                    OntologyClass = klass,
                    Uri = $"ldm:MemoryItem#{id}",
                    Tags = (tagsCsv ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                });
            }

            await store.UpsertAsync(docs, embedIfMissing: true);
            Console.WriteLine($"Upserted {docs.Count} chunks.");
        }, file, text, title, source, klass, tagsCsv, maxChars, dim, tenant, package, channel);

        cmd.AddCommand(add);
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

    private static int SeedFrom(string s)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        // Convert first 4 bytes into an int
        return BitConverter.ToInt32(bytes, 0);
    }
}