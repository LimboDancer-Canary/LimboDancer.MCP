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
var vectorInitCmd = new Command("init", "Ensure Azure AI Search index exists (create/update)")
{
    new Option<string>("--endpoint", "Azure Search endpoint (https://<service>.search.windows.net)") { IsRequired = true },
    new Option<string>("--api-key",  "Admin API key") { IsRequired = true },
    new Option<string>("--index-name", () => LimboDancer.MCP.Vector.AzureSearch.SearchIndexBuilder.DefaultIndexName, "Index name"),
    new Option<int>("--vector-dimensions", () => 1536, "Embedding dimensions"),
    new Option<string>("--semantic-config", () => LimboDancer.MCP.Vector.AzureSearch.SearchIndexBuilder.DefaultSemanticConfig, "Semantic config name"),
    new Option<bool>("--quiet", "Suppress non-error output")
};
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
vectorInitCmd.Options[0] as Option<string>!,
vectorInitCmd.Options[1] as Option<string>!,
vectorInitCmd.Options[2] as Option<string>!,
vectorInitCmd.Options[3] as Option<int>!,
vectorInitCmd.Options[4] as Option<string>!,
vectorInitCmd.Options[5] as Option<bool>!);
vectorInit.AddCommand(vectorInitCmd);
root.AddCommand(vectorInit);

// -----------------------------
// Vector: mem add
// -----------------------------
var mem = new Command("mem", "Memory/vector document commands");
var memAdd = new Command("add", "Ingest documents into the vector index")
{
    new Option<string>("--endpoint", "Azure Search endpoint (https://<service>.search.windows.net)") { IsRequired = true },
    new Option<string>("--api-key",  "Admin API key") { IsRequired = true },
    new Option<string>("--index-name", () => LimboDancer.MCP.Vector.AzureSearch.SearchIndexBuilder.DefaultIndexName, "Index name"),
    new Option<string>("--tenant", "Tenant identifier (required for ingestion)") { IsRequired = true },
    new Option<string?>("--file", "JSON file: MemoryDoc or MemoryDoc[]"),
    new Option<string?>("--content", "Inline content for a single doc"),
    new Option<string?>("--id", "Optional explicit id (defaults to GUID)"),
    new Option<string?>("--label", "Label"),
    new Option<string?>("--kind", "Kind"),
    new Option<string?>("--status", "Status"),
    new Option<string?>("--tags", "Tags"),
    new Option<int?>("--vector-dims", "Expected vector dimensions (sanity check)"),
    new Option<bool>("--quiet", "Suppress non-error output")
};
memAdd.SetHandler(async (
    string endpoint, string apiKey, string indexName, string tenant,
    string? file, string? content, string? id, string? label, string? kind, string? status, string? tags,
    int? vectorDims, bool quiet) =>
{
    var opts = new MemAddCommand.Options
    {
        Endpoint = endpoint,
        ApiKey = apiKey,
        IndexName = indexName,
        Tenant = tenant,
        File = file,
        Content = content,
        Id = id,
        Label = label,
        Kind = kind,
        Status = status,
        Tags = tags,
        VectorDims = vectorDims,
        Quiet = quiet
    };
    Environment.ExitCode = await MemAddCommand.RunAsync(opts);
},
memAdd.Options[0] as Option<string>!,
memAdd.Options[1] as Option<string>!,
memAdd.Options[2] as Option<string>!,
memAdd.Options[3] as Option<string>!,
memAdd.Options[4] as Option<string?>!,
memAdd.Options[5] as Option<string?>!,
memAdd.Options[6] as Option<string?>!,
memAdd.Options[7] as Option<string?>!,
memAdd.Options[8] as Option<string?>!,
memAdd.Options[9] as Option<string?>!,
memAdd.Options[10] as Option<string?>!,
memAdd.Options[11] as Option<int?>!,
memAdd.Options[12] as Option<bool>!);
mem.AddCommand(memAdd);
root.AddCommand(mem);

// -----------------------------
// Ontology commands (server-dependent)
// -----------------------------
var ontology = new Command("ontology", "Ontology commands (requires MCP server endpoints)");

var serverOpt = new Option<string>("--server", "MCP Server base URL (e.g., http://localhost:5179)") { IsRequired = true };
var timeoutOpt = new Option<int>("--timeout", () => 4000, "Timeout in milliseconds for server checks");

// validate
var validateCmd = new Command("validate", "Validate ontology via server endpoint (/api/ontology/validate)")
{
    serverOpt,
    timeoutOpt,
    new Option<string>("--input", "Path to ontology JSON to validate") { IsRequired = true }
};
validateCmd.SetHandler(async (string server, int timeoutMs, string inputPath) =>
{
    try
    {
        using var http = CreateHttpClient(server, timeoutMs);
        var ok = await EnsureServerAndEndpointAsync(http, server, "/api/ontology/validate");
        if (!ok) { Environment.ExitCode = 4; return; }

        var json = await File.ReadAllTextAsync(inputPath, Encoding.UTF8);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var resp = await http.PostAsync("/api/ontology/validate", content);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine(body);
            Environment.ExitCode = 0;
            return;
        }

        Console.Error.WriteLine($"[ontology validate] Server returned {(int)resp.StatusCode} {resp.ReasonPhrase}");
        var err = await resp.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(err)) Console.Error.WriteLine(err);
        Environment.ExitCode = resp.StatusCode == HttpStatusCode.NotFound ? 4 : 1;
    }
    catch (OperationCanceledException)
    {
        Console.Error.WriteLine("[ontology validate] Canceled.");
        Environment.ExitCode = 130;
    }
    catch (HttpRequestException ex)
    {
        Console.Error.WriteLine($"[ontology validate] Server unavailable: {ex.Message}");
        Environment.ExitCode = 3;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[ontology validate] Error: {ex.Message}");
        Environment.ExitCode = 1;
    }
},
validateCmd.Options[0] as Option<string>!,
validateCmd.Options[1] as Option<int>!,
validateCmd.Options[2] as Option<string>!);

// export
var exportCmd = new Command("export", "Export ontology from server (/api/ontology/export)")
{
    serverOpt,
    timeoutOpt,
    new Option<string>("--out", description: "Output file path") { IsRequired = true }
};
exportCmd.SetHandler(async (string server, int timeoutMs, string outPath) =>
{
    try
    {
        using var http = CreateHttpClient(server, timeoutMs);
        var ok = await EnsureServerAndEndpointAsync(http, server, "/api/ontology/export");
        if (!ok) { Environment.ExitCode = 4; return; }

        var resp = await http.GetAsync("/api/ontology/export");
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            await File.WriteAllTextAsync(outPath, body, Encoding.UTF8);
            Console.WriteLine($"[ontology export] ✅ Wrote {outPath}");
            Environment.ExitCode = 0;
            return;
        }

        Console.Error.WriteLine($"[ontology export] Server returned {(int)resp.StatusCode} {resp.ReasonPhrase}");
        var err = await resp.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(err)) Console.Error.WriteLine(err);
        Environment.ExitCode = resp.StatusCode == HttpStatusCode.NotFound ? 4 : 1;
    }
    catch (OperationCanceledException)
    {
        Console.Error.WriteLine("[ontology export] Canceled.");
        Environment.ExitCode = 130;
    }
    catch (HttpRequestException ex)
    {
        Console.Error.WriteLine($"[ontology export] Server unavailable: {ex.Message}");
        Environment.ExitCode = 3;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[ontology export] Error: {ex.Message}");
        Environment.ExitCode = 1;
    }
},
exportCmd.Options[0] as Option<string>!,
exportCmd.Options[1] as Option<int>!,
exportCmd.Options[2] as Option<string>!);

ontology.AddCommand(validateCmd);
ontology.AddCommand(exportCmd);
root.AddCommand(ontology);

// -----------------------------
// Run
// -----------------------------
return await root.InvokeAsync(args);

// =============================
// Helpers
// =============================
static HttpClient CreateHttpClient(string serverBase, int timeoutMs)
{
    var http = new HttpClient
    {
        BaseAddress = new Uri(serverBase, UriKind.Absolute),
        Timeout = TimeSpan.FromMilliseconds(Math.Clamp(timeoutMs, 500, 30000))
    };
    return http;
}

static async Task<bool> EnsureServerAndEndpointAsync(HttpClient http, string serverBase, string endpointPath)
{
    // 1) Server up? (HEAD / or GET /health if you add it later)
    try
    {
        using var head = new HttpRequestMessage(HttpMethod.Head, "/");
        using var resp = await http.SendAsync(head);
        // Some dev servers don’t implement HEAD; fall back to GET /
        if (!resp.IsSuccessStatusCode)
        {
            using var get = await http.GetAsync("/");
            if (!get.IsSuccessStatusCode && get.StatusCode != HttpStatusCode.NotFound)
            {
                Console.Error.WriteLine($"[server-check] {serverBase} responded {(int)get.StatusCode} {get.ReasonPhrase}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[server-check] Cannot reach {serverBase}: {ex.Message}");
        Environment.ExitCode = 3;
        return false;
    }

    // 2) Endpoint available? (OPTIONS or HEAD; fall back to GET with no side effects)
    try
    {
        using var req = new HttpRequestMessage(HttpMethod.Options, endpointPath);
        using var resp = await http.SendAsync(req);
        if ((int)resp.StatusCode == 404)
        {
            Console.Error.WriteLine($"[endpoint-check] {endpointPath} not found on server.");
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
