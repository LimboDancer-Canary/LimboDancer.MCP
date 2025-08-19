# Bootstrapping LimboDancer.MCP Code

Absolutely. Here’s a single PowerShell script that:

1. creates the solution and all projects,
2. wires up project references and NuGet packages, and
3. lays down the **initial file set** (minimal, compilable stubs) in the right folders.

It’s idempotent (won’t overwrite files unless you pass `-Force`) and safe to run from the repo root.

> Save this as `scripts\bootstrap.ps1` (or anywhere) and run:
> `pwsh -File scripts/bootstrap.ps1`
> You can re-run with `-Force` to overwrite any generated files.

```powershell
<#  LimboDancer.MCP Bootstrap
    Creates solution, projects, references, packages, and initial file stubs.

    Usage:
      pwsh -File scripts/bootstrap.ps1 [-SolutionName "LimboDancer.MCP"] [-Root "src"] [-Force]

    Notes:
      - Requires .NET SDK 8+
      - Safe to re-run; pass -Force to overwrite any generated files
#>

param(
  [string]$SolutionName = "LimboDancer.MCP",
  [string]$Root = "src",
  [switch]$Force
)

$ErrorActionPreference = "Stop"

function Ensure-Dir {
  param([string]$Path)
  if (-not (Test-Path $Path)) { New-Item -ItemType Directory -Path $Path | Out-Null }
}

function New-FileSafely {
  param(
    [string]$Path,
    [string]$Content,
    [switch]$Force
  )
  $dir = Split-Path $Path -Parent
  Ensure-Dir $dir
  if ((Test-Path $Path) -and -not $Force) {
    Write-Host "SKIP   $Path (exists)"
    return
  }
  $Content | Set-Content -Path $Path -Encoding UTF8
  Write-Host "WRITE  $Path"
}

function Dotnet-Exec {
  param([string]$Args)
  Write-Host "RUN    dotnet $Args"
  dotnet $Args
}

# --- 0) Solution -------------------------------------------------------------
if (-not (Test-Path "$SolutionName.sln")) {
  Dotnet-Exec "new sln -n $SolutionName"
} else {
  Write-Host "SKIP   $SolutionName.sln (exists)"
}

# --- 1) Project map ----------------------------------------------------------
$projects = @(
  @{ name="LimboDancer.MCP.Core";               type="classlib" }
  @{ name="LimboDancer.MCP.Ontology";           type="classlib" }
  @{ name="LimboDancer.MCP.Storage";            type="classlib" }
  @{ name="LimboDancer.MCP.Vector.AzureSearch"; type="classlib" }
  @{ name="LimboDancer.MCP.Graph.CosmosGremlin";type="classlib" }
  @{ name="LimboDancer.MCP.Llm";                type="classlib" }
  @{ name="LimboDancer.MCP.McpServer";          type="classlib" }   # server host will come later
  @{ name="LimboDancer.MCP.Cli";                type="console"  }
  @{ name="LimboDancer.MCP.BlazorConsole";      type="blazorserver" }
)

foreach ($p in $projects) {
  $projDir = Join-Path $Root $p.name
  $csproj  = Join-Path $projDir "$($p.name).csproj"
  if (-not (Test-Path $csproj)) {
    switch ($p.type) {
      "classlib"    { Dotnet-Exec "new classlib -n $($p.name) -o $projDir" }
      "console"     { Dotnet-Exec "new console  -n $($p.name) -o $projDir" }
      "blazorserver"{ Dotnet-Exec "new blazorserver -n $($p.name) -o $projDir" }
    }
    Dotnet-Exec "sln add `"$csproj`""
  } else {
    Write-Host "SKIP   $csproj (exists)"
    if (-not (Select-String -Path "$SolutionName.sln" -Pattern [regex]::Escape($csproj) -Quiet)) {
      Dotnet-Exec "sln add `"$csproj`""
    }
  }
}

# --- 2) Project references ---------------------------------------------------
$refs = @(
  @{ from="LimboDancer.MCP.Storage";            to=@("LimboDancer.MCP.Core") }
  @{ from="LimboDancer.MCP.McpServer";          to=@("LimboDancer.MCP.Core","LimboDancer.MCP.Storage","LimboDancer.MCP.Ontology","LimboDancer.MCP.Vector.AzureSearch","LimboDancer.MCP.Graph.CosmosGremlin") }
  @{ from="LimboDancer.MCP.BlazorConsole";      to=@("LimboDancer.MCP.Storage","LimboDancer.MCP.Vector.AzureSearch","LimboDancer.MCP.Graph.CosmosGremlin") }
  @{ from="LimboDancer.MCP.Cli";                to=@("LimboDancer.MCP.Storage","LimboDancer.MCP.Vector.AzureSearch","LimboDancer.MCP.Graph.CosmosGremlin") }
)

foreach ($r in $refs) {
  $fromPath = Join-Path $Root $r.from "$($r.from).csproj"
  foreach ($t in $r.to) {
    $toPath = Join-Path $Root $t "$($t).csproj"
    Write-Host "REF    $($r.from) -> $t"
    Dotnet-Exec "add `"$fromPath`" reference `"$toPath`""
  }
}

# --- 3) NuGet packages -------------------------------------------------------
function Add-Package {
  param($proj, $pkg, $ver = $null)
  $projPath = Join-Path $Root $proj "$proj.csproj"
  $verArg = $null
  if ($ver) { $verArg = "--version $ver" }
  Dotnet-Exec "add `"$projPath`" package $pkg $verArg"
}

# EF Core + Npgsql in Storage
Add-Package "LimboDancer.MCP.Storage" "Microsoft.EntityFrameworkCore"
Add-Package "LimboDancer.MCP.Storage" "Microsoft.EntityFrameworkCore.Design"
Add-Package "LimboDancer.MCP.Storage" "Npgsql.EntityFrameworkCore.PostgreSQL"

# Azure Search
Add-Package "LimboDancer.MCP.Vector.AzureSearch" "Azure.Search.Documents"

# Gremlin
Add-Package "LimboDancer.MCP.Graph.CosmosGremlin" "Gremlin.Net"

# System.CommandLine for CLI
Add-Package "LimboDancer.MCP.Cli" "System.CommandLine"

# Blazor Console typical deps come from template already

# --- 4) File stubs -----------------------------------------------------------
# Core
New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.Core\Abstractions\IChatHistoryStore.cs") `
  -Content @"
namespace LimboDancer.MCP.Core.Abstractions;

public interface IChatHistoryStore
{
    Task<Guid> CreateSessionAsync(string title, CancellationToken ct = default);
    Task AppendMessageAsync(Guid sessionId, string role, string content, DateTimeOffset timestamp, CancellationToken ct = default);
    IAsyncEnumerable<(string Role, string Content, DateTimeOffset Ts)> ReadMessagesAsync(Guid sessionId, CancellationToken ct = default);
}
"@

# Storage
New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.Storage\Entities.cs") `
  -Content @"
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LimboDancer.MCP.Storage;

[Table(""sessions"")]
public class Session
{
    [Key] public Guid Id { get; set; }
    [MaxLength(256)] public string Title { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table(""messages"")]
public class Message
{
    [Key] public long Id { get; set; }
    public Guid SessionId { get; set; }
    [MaxLength(32)] public string Role { get; set; } = ""user"";
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset Ts { get; set; } = DateTimeOffset.UtcNow;
    public Session? Session { get; set; }
}
"@

New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.Storage\ChatDbContext.cs") `
  -Content @"
using Microsoft.EntityFrameworkCore;

namespace LimboDancer.MCP.Storage;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Message>().HasIndex(m => m.SessionId);
    }
}
"@

New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.Storage\StorageOptions.cs") `
  -Content @"
namespace LimboDancer.MCP.Storage;

public class StorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}
"@

New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.Storage\ServiceCollectionExtensions.cs") `
  -Content @"
using LimboDancer.MCP.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.MCP.Storage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMcpStorage(this IServiceCollection services, IConfiguration cfg)
    {
        var cs = cfg[""Persistence:ConnectionString""] ?? cfg.GetConnectionString(""ChatDb"");
        services.AddDbContext<ChatDbContext>(o => o.UseNpgsql(cs));
        services.AddScoped<IChatHistoryStore, ChatHistoryStore>();
        return services;
    }
}

internal class ChatHistoryStore(ChatDbContext db) : IChatHistoryStore
{
    public async Task<Guid> CreateSessionAsync(string title, CancellationToken ct = default)
    {
        var s = new Session { Id = Guid.NewGuid(), Title = title };
        db.Sessions.Add(s);
        await db.SaveChangesAsync(ct);
        return s.Id;
    }

    public async Task AppendMessageAsync(Guid sessionId, string role, string content, DateTimeOffset timestamp, CancellationToken ct = default)
    {
        db.Messages.Add(new Message { SessionId = sessionId, Role = role, Content = content, Ts = timestamp });
        await db.SaveChangesAsync(ct);
    }

    public async IAsyncEnumerable<(string Role, string Content, DateTimeOffset Ts)> ReadMessagesAsync(Guid sessionId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var q = db.Messages.AsNoTracking().Where(m => m.SessionId == sessionId).OrderBy(m => m.Id).AsAsyncEnumerable();
        await foreach (var m in q.WithCancellation(ct))
            yield return (m.Role, m.Content, m.Ts);
    }
}
"@

# Ontology
New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.Ontology\Ontology.cs") `
  -Content @"
namespace LimboDancer.MCP.Ontology;

// Minimal seed; extend per docs
public static class Ontology
{
    public const string ContextUrl = ""https://example.org/ld/context.json"";

    public static readonly IReadOnlyDictionary<string, string> CurieMap = new Dictionary<string, string>
    {
        [""ld""] = ""https://example.org/ld#"",
        [""mcp""] = ""https://example.org/mcp#""
    };
}
"@

# Vector (Azure AI Search)
New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.Vector.AzureSearch\VectorModels.cs") `
  -Content @"
namespace LimboDancer.MCP.Vector.AzureSearch;
public record MemoryDoc(string Id, string Text, string? Kind = null, string[]? Tags = null, float[]? Vector = null);
"@

New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.Vector.AzureSearch\SearchIndexBuilder.cs") `
  -Content @"
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Configuration;

namespace LimboDancer.MCP.Vector.AzureSearch;

public class SearchIndexBuilder(IConfiguration cfg, SearchIndexClient client)
{
    public string IndexName => cfg[""Search:Index""] ?? ""ld-mem"";

    public async Task EnsureAsync(int dim = 3072, CancellationToken ct = default)
    {
        var fields = new FieldBuilder().Build(typeof(IndexDoc));
        var vector = new SearchField(""vector"", SearchFieldDataType.Collection(SearchFieldDataType.Single))
        { Dimensions = dim, VectorSearchProfileName = ""vec"", IsHidden = false };
        var id = new SimpleField(""id"", SearchFieldDataType.String) { IsKey = true, IsFilterable = true };
        var text = new SearchField(""text"", SearchFieldDataType.String) { IsSearchable = true };
        var kind = new SearchField(""kind"", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true };
        var tags = new SearchField(""tags"", SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsFilterable = true, IsFacetable = true };

        var index = new SearchIndex(IndexName)
        {
            Fields = { id, text, kind, tags, vector },
            VectorSearch = new()
            {
                Algorithms = { new HnswAlgorithmConfiguration(""hnsw"") },
                Profiles = { new VectorSearchProfile(""vec"", ""hnsw"") }
            }
        };

        try { await client.CreateOrUpdateIndexAsync(index, cancellationToken: ct); }
        catch (RequestFailedException ex) when (ex.Status == 409) { /* ignore */ }
    }

    private class IndexDoc
    {
        public string id { get; set; } = string.Empty;
        public string text { get; set; } = string.Empty;
        public string? kind { get; set; }
        public string[]? tags { get; set; }
        public float[]? vector { get; set; }
    }
}
"@

New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.Vector.AzureSearch\VectorStore.cs") `
  -Content @"
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;

namespace LimboDancer.MCP.Vector.AzureSearch;

public class VectorStore(IConfiguration cfg, SearchClient client)
{
    string Index => cfg[""Search:Index""] ?? ""ld-mem"";

    public async Task UpsertAsync(IEnumerable<MemoryDoc> docs, CancellationToken ct = default)
    {
        var batch = IndexDocumentsBatch.Upload(docs.Select(d => new
        {
            id = d.Id, text = d.Text, kind = d.Kind, tags = d.Tags, vector = d.Vector
        }));
        await client.IndexDocumentsAsync(batch, ct);
    }

    public async Task<IReadOnlyList<SearchResult<SearchDocument>>> SearchHybridAsync(string query, int k = 10, CancellationToken ct = default)
    {
        var opt = new SearchOptions() { Size = k };
        var res = await client.SearchAsync<SearchDocument>(query, opt, ct);
        return await res.Value.GetResultsAsync().ToListAsync(ct);
    }
}
"@

# Graph (Cosmos Gremlin)
New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.Graph.CosmosGremlin\GremlinClientFactory.cs") `
  -Content @"
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Extensions.Configuration;

namespace LimboDancer.MCP.Graph.CosmosGremlin;

public class GremlinClientFactory(IConfiguration cfg)
{
    public GremlinClient Create()
    {
        var host = cfg[""Gremlin:Host""] ?? ""localhost"";
        var port = int.TryParse(cfg[""Gremlin:Port""], out var p) ? p : 443;
        var enableSsl = true;
        return new GremlinClient(new GremlinServer(host, port, enableSsl: enableSsl),
            new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType);
    }
}
"@

New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.Graph.CosmosGremlin\GraphStore.cs") `
  -Content @"
using Gremlin.Net.Driver;
using Gremlin.Net.Process.Traversal;

namespace LimboDancer.MCP.Graph.CosmosGremlin;

public class GraphStore(GremlinClient client)
{
    public async Task<long> PingCountAsync(CancellationToken ct = default)
    {
        var res = await client.SubmitAsync<long>(""g.V().limit(1).count()"");
        return res.FirstOrDefault();
    }
}
"@

# MCP Server (tool stubs)
New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.McpServer\Tools\HistoryGetTool.cs") `
  -Content @"
namespace LimboDancer.MCP.McpServer.Tools;

public class HistoryGetTool { /* TODO: implement per MCP tool spec */ }
"@

New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.McpServer\Tools\MemorySearchTool.cs") `
  -Content @"
namespace LimboDancer.MCP.McpServer.Tools;

public class MemorySearchTool { /* TODO: implement per MCP tool spec */ }
"@

# CLI
New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.Cli\Program.cs") `
  -Content @"
using System.CommandLine;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using LimboDancer.MCP.Graph.CosmosGremlin;
using LimboDancer.MCP.Storage;
using LimboDancer.MCP.Vector.AzureSearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var cfg = new ConfigurationBuilder()
    .AddJsonFile(""appsettings.json"", optional: true)
    .AddJsonFile(""appsettings.Development.json"", optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddMcpStorage(cfg);

var searchEndpoint = cfg[""Search:Endpoint""];
var searchKey = cfg[""Search:ApiKey""];
if (!string.IsNullOrEmpty(searchEndpoint) && !string.IsNullOrEmpty(searchKey))
{
    services.AddSingleton(new SearchIndexClient(new Uri(searchEndpoint!), new AzureKeyCredential(searchKey!)));
    services.AddSingleton(sp => new SearchClient(new Uri(searchEndpoint!), cfg[""Search:Index""] ?? ""ld-mem"", new AzureKeyCredential(searchKey!)));
    services.AddSingleton<SearchIndexBuilder>();
    services.AddSingleton<VectorStore>();
}

services.AddSingleton<GremlinClientFactory>();
services.AddSingleton(sp => new GraphStore(sp.GetRequiredService<GremlinClientFactory>().Create()));

var provider = services.BuildServiceProvider();

var root = new RootCommand(""LimboDancer CLI"");

var migrate = new Command(""db-migrate"", ""Apply EF Core migrations"");
migrate.SetHandler(async () =>
{
    using var scope = provider.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    await db.Database.MigrateAsync();
    Console.WriteLine(""DB migrated."");
});
root.Add(migrate);

var vectorInit = new Command(""vector-init"", ""Ensure Azure Search index"");
var dimOpt = new Option<int>(name: ""--dim"", getDefaultValue: ()=>3072, description: ""Embedding dimension"");
vectorInit.AddOption(dimOpt);
vectorInit.SetHandler(async (int dim) =>
{
    using var scope = provider.CreateScope();
    var builder = scope.ServiceProvider.GetRequiredService<SearchIndexBuilder>();
    await builder.EnsureAsync(dim);
    Console.WriteLine($""Index ensured (dim={dim})."");
}, dimOpt);
root.Add(vectorInit);

var kgPing = new Command(""kg-ping"", ""Ping Gremlin graph"");
kgPing.SetHandler(async () =>
{
    using var scope = provider.CreateScope();
    var g = scope.ServiceProvider.GetRequiredService<GraphStore>();
    var c = await g.PingCountAsync();
    Console.WriteLine($""Graph reachable, count={c}"");
});
root.Add(kgPing);

return await root.InvokeAsync(args);
"@

New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.Cli\appsettings.Development.json") `
  -Content @"
{
  ""Persistence"": {
    ""ConnectionString"": ""Host=localhost;Port=5432;Database=limbo;Username=postgres;Password=postgres""
  },
  ""Search"": {
    ""Endpoint"": ""https://<your-search>.search.windows.net"",
    ""ApiKey"": ""<admin-key>"",
    ""Index"": ""ld-mem""
  },
  ""Gremlin"": {
    ""Host"": ""localhost"",
    ""Port"": ""8182""
  }
}
"@

# Blazor Console: minimal pages
New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.BlazorConsole\Pages\Sessions.razor") `
  -Content @"
@page ""/sessions""
<h3>Sessions (read-only)</h3>
@code { }
"@

New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.BlazorConsole\Pages\Memory.razor") `
  -Content @"
@page ""/memory""
<h3>Memory (search)</h3>
@code { }
"@

New-FileSafely -Force:$Force `
  -Path (Join-Path $Root "LimboDancer.MCP.BlazorConsole\Pages\Graph.razor") `
  -Content @"
@page ""/graph""
<h3>Graph (read-only)</h3>
@code { }
"@

Write-Host ""
Write-Host "=== Build test ==="
Dotnet-Exec "build"
Write-Host "Done."
```

## What this gives you

* A fully wired **solution + project graph**, with:

  * EF Core/Postgres baseline (`Storage` + `Core` interfaces)
  * Azure AI Search scaffold (index builder + vector store)
  * Cosmos Gremlin client + simple `PingCountAsync`
  * CLI with `db-migrate`, `vector-init`, `kg-ping`
  * Blazor Console pages (placeholders for now)
* Config sample in `appsettings.Development.json` for the CLI.

## Next quick checks

```bash
# 1) Build
dotnet build

# 2) Migrate DB (requires Postgres running)
dotnet run --project src/LimboDancer.MCP.Cli -- db-migrate

# 3) Ensure Search index (set Search Endpoint/Key first)
dotnet run --project src/LimboDancer.MCP.Cli -- vector-init --dim 3072

# 4) Gremlin reachability (adjust Gremlin host/port)
dotnet run --project src/LimboDancer.MCP.Cli -- kg-ping
```

If you want me to **expand the file stubs** further (e.g., DI & pages hooked up with real queries, MCP tools fleshed out, or add unit test projects), say the word and I’ll extend this same script to generate those files too.
