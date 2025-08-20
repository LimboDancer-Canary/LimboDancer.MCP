using System.CommandLine;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.MCP.Cli.Commands;

internal static class OntologyExportCommand
{
    public static Command Build()
    {
        var cmd = new Command("ontology", "Ontology utilities");
        var export = new Command("export", "Export ontology bundle (JSON-LD or Turtle)");

        var format = new Option<string>("--format", () => "jsonld", "jsonld | turtle");
        var outPath = new Option<string>("--out", description: "Output file path") { IsRequired = true };
        var tenant = new Option<string?>("--tenant", "Tenant Id (GUID)");
        var package = new Option<string?>("--package", () => "default", "Package (default)");
        var channel = new Option<string?>("--channel", () => "dev", "Channel (dev)");

        export.AddOption(format);
        export.AddOption(outPath);
        export.AddOption(tenant);
        export.AddOption(package);
        export.AddOption(channel);

        export.SetHandler(async (string fmt, string outFile, string? tenantOpt, string? pkg, string? chan) =>
        {
            using var host = Bootstrap.BuildHost();
            var cfg = host.Services.GetRequiredService<IConfiguration>();
            var baseUrl = cfg["Server:BaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                Console.Error.WriteLine("Server:BaseUrl not configured.");
                return;
            }

            if (!Guid.TryParse(tenantOpt ?? cfg["Tenant"], out var tenantId))
            {
                Console.Error.WriteLine("Provide --tenant or set Tenant in config.");
                return;
            }

            var url = $"{baseUrl}/api/ontology/export?tenant={tenantId:D}&package={Uri.EscapeDataString(pkg ?? "default")}&channel={Uri.EscapeDataString(chan ?? "dev")}&format={Uri.EscapeDataString(fmt)}";
            using var http = new HttpClient();
            var resp = await http.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"Export failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");
                return;
            }

            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var bytes = await resp.Content.ReadAsByteArrayAsync();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outFile))!);
            await File.WriteAllBytesAsync(outFile, bytes);
            Console.WriteLine($"Exported ontology bundle ({contentType}) to {outFile}");
        }, format, outPath, tenant, package, channel);

        cmd.AddCommand(export);
        return cmd;
    }
}