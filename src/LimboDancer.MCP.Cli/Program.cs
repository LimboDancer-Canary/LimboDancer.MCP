// File: /src/LimboDancer.MCP.Cli/Program.cs
// Purpose:
//   CLI entrypoint for LimboDancer.MCP. Adds robust server/endpoint checks for
//   ontology-related commands so failures are graceful and informative.
//
// Commands (examples):
//   ldm vector init --endpoint https://svc.search.windows.net --api-key XXX
//   ldm mem add     --endpoint https://svc.search.windows.net --api-key XXX --tenant t1 --content "hello"
//   ldm ontology validate --server http://localhost:5179 --input ./ontology.json
//   ldm ontology export   --server http://localhost:5179 --out ./export.json
//
// Exit codes:
//   0   success
//   1   generic error
//   2   Azure RequestFailedException (vector ops)
//   3   server unavailable (network/connectivity/timeout)
//   4   required endpoint missing (404)
//   130 canceled (SIGINT)

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using LimboDancer.MCP.Cli.Commands; // VectorInitCommand, MemAddCommand
using Azure;

var root = new RootCommand("LimboDancer MCP CLI");

// -----------------------------
// Vector: init
// -----------------------------
var vectorInit = new Command("vector", "Vector index commands");
var vectorInitCmd = new Command("init", "Ensure Azure AI Search index exists (create/update)");

// Define options as variables first
var viEndpointOpt = new Option<string>("--endpoint", "Azure Search endpoint (https://<service>.search.windows.net)") { IsRequired = true };
var viApiKeyOpt = new Option<string>("--api-key", "Admin API key") { IsRequired = true };
var viIndexNameOpt = new Option<string>("--index-name", () => LimboDancer.MCP.Vector.AzureSearch.SearchIndexBuilder.DefaultIndexName, "Index name");
var viVectorDimsOpt = new Option<int>("--vector-dimensions", () => 1536, "Embedding dimensions");
var viSemanticOpt = new Option<string>("--semantic-config", () => LimboDancer.MCP.Vector.AzureSearch.SearchIndexBuilder.DefaultSemanticConfig, "Semantic config name");
var viQuietOpt = new Option<bool>("--quiet", "Suppress non-error output");

// Add options to command
vectorInitCmd.AddOption(viEndpointOpt);
vectorInitCmd.AddOption(viApiKeyOpt);
vectorInitCmd.AddOption(viIndexNameOpt);
vectorInitCmd.AddOption(viVectorDimsOpt);
vectorInitCmd.AddOption(viSemanticOpt);
vectorInitCmd.AddOption(viQuietOpt);

vectorInitCmd.SetHandler(async (string endpoint, string apiKey, string indexName, int dims, string sem, bool quiet) =>
{
    var opts = new VectorInitCommand.Options
    {
        Endpoint = endpoint,
        ApiKey = apiKey,
        IndexName = indexName,
        VectorDimensions = dims,
        SemanticConfig = sem,
        Quiet = quiet
    };
    Environment.ExitCode = await VectorInitCommand.RunAsync(opts);
},
viEndpointOpt,
viApiKeyOpt,
viIndexNameOpt,
viVectorDimsOpt,
viSemanticOpt,
viQuietOpt);

vectorInit.AddCommand(vectorInitCmd);
root.AddCommand(vectorInit);

// -----------------------------
// Vector: mem add
// -----------------------------
var mem = new Command("mem", "Memory/vector document commands");
var memAdd = new Command("add", "Ingest documents into the vector index");

// Define all options as variables
var maEndpointOpt = new Option<string>("--endpoint", "Azure Search endpoint (https://<service>.search.windows.net)") { IsRequired = true };
var maApiKeyOpt = new Option<string>("--api-key", "Admin API key") { IsRequired = true };
var maIndexNameOpt = new Option<string>("--index-name", () => LimboDancer.MCP.Vector.AzureSearch.SearchIndexBuilder.DefaultIndexName, "Index name");
var maTenantOpt = new Option<string>("--tenant", "Tenant identifier (required for ingestion)") { IsRequired = true };
var maFileOpt = new Option<string?>("--file", "JSON file: MemoryDoc or MemoryDoc[]");
var maContentOpt = new Option<string?>("--content", "Inline content for a single doc");
var maIdOpt = new Option<string?>("--id", "Optional explicit id (defaults to GUID)");
var maLabelOpt = new Option<string?>("--label", "Label");
var maKindOpt = new Option<string?>("--kind", "Kind");
var maStatusOpt = new Option<string?>("--status", "Status");
var maTagsOpt = new Option<string?>("--tags", "Tags");
var maVectorDimsOpt = new Option<int?>("--vector-dimensions", description: "Optional expected embedding dimensions for validation");
var maQuietOpt = new Option<bool>("--quiet", "Suppress non-error output");

// Add options to command
memAdd.AddOption(maEndpointOpt);
memAdd.AddOption(maApiKeyOpt);
memAdd.AddOption(maIndexNameOpt);
memAdd.AddOption(maTenantOpt);
memAdd.AddOption(maFileOpt);
memAdd.AddOption(maContentOpt);
memAdd.AddOption(maIdOpt);
memAdd.AddOption(maLabelOpt);
memAdd.AddOption(maKindOpt);
memAdd.AddOption(maStatusOpt);
memAdd.AddOption(maTagsOpt);
memAdd.AddOption(maVectorDimsOpt);
memAdd.AddOption(maQuietOpt);

// Use InvocationContext to avoid SetHandler arity limits
memAdd.SetHandler(async (InvocationContext ctx) =>
{
    var pr = ctx.ParseResult;

    var opts = new MemAddCommand.Options
    {
        Endpoint = pr.GetValueForOption(maEndpointOpt),
        ApiKey = pr.GetValueForOption(maApiKeyOpt),
        IndexName = pr.GetValueForOption(maIndexNameOpt) ?? LimboDancer.MCP.Vector.AzureSearch.SearchIndexBuilder.DefaultIndexName,
        Tenant = pr.GetValueForOption(maTenantOpt),
        File = pr.GetValueForOption(maFileOpt),
        Content = pr.GetValueForOption(maContentOpt),
        Id = pr.GetValueForOption(maIdOpt),
        Label = pr.GetValueForOption(maLabelOpt),
        Kind = pr.GetValueForOption(maKindOpt),
        Status = pr.GetValueForOption(maStatusOpt),
        Tags = pr.GetValueForOption(maTagsOpt),
        VectorDims = pr.GetValueForOption(maVectorDimsOpt),
        Quiet = pr.GetValueForOption(maQuietOpt)
    };

    Environment.ExitCode = await MemAddCommand.RunAsync(opts);
});

mem.AddCommand(memAdd);
root.AddCommand(mem);

// -----------------------------
// Ontology: validate
// -----------------------------
var ontology = new Command("ontology", "Ontology runtime commands");
var ontologyValidate = new Command("validate", "Validate an ontology JSON file against the McpServer endpoint");

// Define options as variables
var ovServerOpt = new Option<string>("--server", "MCP server URL (e.g., http://localhost:5179)") { IsRequired = true };
var ovFileOpt = new Option<string>("--input", "Local ontology JSON file") { IsRequired = true };

ontologyValidate.AddOption(ovServerOpt);
ontologyValidate.AddOption(ovFileOpt);

ontologyValidate.SetHandler(async (string server, string file) =>
{
    if (!File.Exists(file))
    {
        Console.Error.WriteLine($"[ontology-validate] File not found: {file}");
        Environment.ExitCode = 1;
        return;
    }

    server = server.TrimEnd('/');
    var validateUrl = $"{server}/api/ontology/validate";

    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

    // Check server availability
    if (!await CheckServerEndpoint(http, server, "/api/ontology/validate")) return;

    try
    {
        var json = await File.ReadAllTextAsync(file);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await http.PostAsync(validateUrl, content);

        var respText = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"[ontology-validate] {resp.StatusCode}: {respText}");
            Environment.ExitCode = 1;
            return;
        }

        var result = JsonSerializer.Deserialize<OntologyValidateResult>(respText);
        if (result?.Errors?.Count > 0)
        {
            Console.WriteLine($"[ontology-validate] {result.Errors.Count} validation errors:");
            foreach (var err in result.Errors)
                Console.WriteLine($"  - {err}");
            Environment.ExitCode = 1;
        }
        else
        {
            Console.WriteLine("[ontology-validate] ✅ Valid ontology.");
            Environment.ExitCode = 0;
        }
    }
    catch (HttpRequestException ex)
    {
        Console.Error.WriteLine($"[ontology-validate] Network error: {ex.Message}");
        Environment.ExitCode = 3;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[ontology-validate] Error: {ex.Message}");
        Environment.ExitCode = 1;
    }
},
ovServerOpt,
ovFileOpt);

ontology.AddCommand(ontologyValidate);

// -----------------------------
// Ontology: export
// -----------------------------
var ontologyExport = new Command("export", "Export the McpServer's active ontology");

// Define options as variables
var oeServerOpt = new Option<string>("--server", "MCP server URL (e.g., http://localhost:5179)") { IsRequired = true };
var oeFormatOpt = new Option<string>("--format", () => "jsonld", "Export format: jsonld | turtle");
var oeOutputOpt = new Option<string?>("--out", "Output file (default: stdout)");

ontologyExport.AddOption(oeServerOpt);
ontologyExport.AddOption(oeFormatOpt);
ontologyExport.AddOption(oeOutputOpt);

ontologyExport.SetHandler(async (string server, string format, string? outFile) =>
{
    server = server.TrimEnd('/');
    var exportUrl = $"{server}/api/ontology/export?format={format}";

    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

    // Check server availability
    if (!await CheckServerEndpoint(http, server, "/api/ontology/export")) return;

    try
    {
        using var resp = await http.GetAsync(exportUrl);
        var respText = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"[ontology-export] {resp.StatusCode}: {respText}");
            Environment.ExitCode = 1;
            return;
        }

        if (!string.IsNullOrWhiteSpace(outFile))
        {
            await File.WriteAllTextAsync(outFile, respText);
            Console.WriteLine($"[ontology-export] Saved to: {outFile}");
        }
        else
        {
            Console.WriteLine(respText);
        }
        Environment.ExitCode = 0;
    }
    catch (HttpRequestException ex)
    {
        Console.Error.WriteLine($"[ontology-export] Network error: {ex.Message}");
        Environment.ExitCode = 3;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[ontology-export] Error: {ex.Message}");
        Environment.ExitCode = 1;
    }
},
oeServerOpt,
oeFormatOpt,
oeOutputOpt);

ontology.AddCommand(ontologyExport);
root.AddCommand(ontology);

// -----------------------------
// Main entry
// -----------------------------
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Environment.ExitCode = 130; // SIGINT
};

try
{
    return await root.InvokeAsync(args);
}
catch (OperationCanceledException)
{
    return 130;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[cli] Unhandled error: {ex.Message}");
    return 1;
}

// -----------------------------
// Helpers (must be after all top-level statements)
// -----------------------------

async Task<bool> CheckServerEndpoint(HttpClient http, string server, string endpointPath)
{
    try
    {
        // First try OPTIONS to test connectivity
        using var options = new HttpRequestMessage(HttpMethod.Options, server);
        using var resp = await http.SendAsync(options);

        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            Console.Error.WriteLine($"[endpoint-check] Server not found: {server}");
            Environment.ExitCode = 3;
            return false;
        }

        // If OPTIONS not allowed, try HEAD
        if (resp.StatusCode == HttpStatusCode.MethodNotAllowed)
        {
            using var head = new HttpRequestMessage(HttpMethod.Head, endpointPath);
            using var headResp = await http.SendAsync(head);
            if ((int)headResp.StatusCode == 404)
            {
                Console.Error.WriteLine($"[endpoint-check] {endpointPath} not found on server.");
                Environment.ExitCode = 4;
                return false;
            }
        }
    }
    catch (HttpRequestException ex)
    {
        Console.Error.WriteLine($"[endpoint-check] Failed for {endpointPath}: {ex.Message}");
        Environment.ExitCode = 3;
        return false;
    }
    return true;
}

record OntologyValidateResult(List<string>? Errors);
