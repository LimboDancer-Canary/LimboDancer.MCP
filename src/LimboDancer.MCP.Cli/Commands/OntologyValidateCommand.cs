using System.CommandLine;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.MCP.Cli.Commands;

internal static class OntologyValidateCommand
{
    public static Command Build()
    {
        var cmd = new Command("ontology", "Ontology utilities");
        var validate = new Command("validate", "Run ontology validators and print outcomes");

        var tenant = new Option<string?>("--tenant", "Tenant Id (GUID)");
        var package = new Option<string?>("--package", () => "default", "Package (default)");
        var channel = new Option<string?>("--channel", () => "dev", "Channel (dev)");

        validate.AddOption(tenant);
        validate.AddOption(package);
        validate.AddOption(channel);

        validate.SetHandler(async (string? tenantOpt, string? pkg, string? chan) =>
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

            var url = $"{baseUrl}/api/ontology/validate?tenant={tenantId:D}&package={Uri.EscapeDataString(pkg ?? "default")}&channel={Uri.EscapeDataString(chan ?? "dev")}";
            using var http = new HttpClient();
            var outcomes = await http.GetFromJsonAsync<List<ValidatorRow>>(url) ?? new();

            if (outcomes.Count == 0)
            {
                Console.WriteLine("No validator outcomes.");
                return;
            }

            Console.WriteLine($"Validator outcomes ({outcomes.Count}):");
            foreach (var o in outcomes.OrderByDescending(x => x.At))
            {
                Console.WriteLine($"{o.At:u} [{o.Severity}] {o.Kind} {o.Id} - {o.Message}");
            }
        }, tenant, package, channel);

        cmd.AddCommand(validate);
        return cmd;
    }

    private sealed record ValidatorRow(string Kind, string Id, string Severity, string Message, DateTimeOffset At);
}