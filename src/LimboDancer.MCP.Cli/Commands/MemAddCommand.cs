using Azure.Search.Documents;
using LimboDancer.MCP.Vector.AzureSearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.Security.Cryptography;
using System.Text;
using static LimboDancer.MCP.Vector.AzureSearch.VectorStore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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

        add.AddOption(file); add.AddOption(text); add.AddOption(title); add.AddOption(source);
        add.AddOption(klass); add.AddOption(tagsCsv); add.AddOption(maxChars); add.AddOption(dim);

        add.SetHandler(async (string? file, string? text, string? title, string? source, string? klass, string? tagsCsv, int maxChars, int dim) =>
        {
            using var host = Bootstrap.BuildHost();
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
            Console.WriteLine("Upsert complete.");
        }, file, text, title, source, klass, tagsCsv, maxChars, dim);

        cmd.AddCommand(add);
        return cmd;
    }

    private static int SeedFrom(string s)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(s));
        return Math.Abs(BitConverter.ToInt32(hash, 0));
    }

    private static List<string> ChunkBySentences(string text, int maxChars)
    {
        var parts = SplitSentences(text);
        var chunks = new List<string>();
        var sb = new StringBuilder(maxChars + 64);

        foreach (var s in parts)
        {
            if (sb.Length + s.Length + 1 > maxChars && sb.Length > 0)
            {
                chunks.Add(sb.ToString().Trim());
                sb.Clear();
            }
            sb.AppendLine(s);
        }
        if (sb.Length > 0) chunks.Add(sb.ToString().Trim());
        return chunks;
    }

    private static IEnumerable<string> SplitSentences(string text)
    {
        foreach (var para in text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var p = para.Replace("\r\n", " ").Replace('\n', ' ').Trim();
            if (p.Length == 0) continue;

            var acc = new StringBuilder();
            foreach (var token in p.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                acc.Append(token).Append(' ');
                if (token.EndsWith('.') || token.EndsWith('!') || token.EndsWith('?'))
                {
                    yield return acc.ToString().Trim();
                    acc.Clear();
                }
            }
            if (acc.Length > 0) yield return acc.ToString().Trim();
        }
    }
}
