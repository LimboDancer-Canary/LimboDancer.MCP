<# 
  LimboDancer.MCP bootstrap (create-only)
  - Creates expected directories and BLANK files (no content).
  - Idempotent: safe to re-run.
  - Adds .gitkeep to any explicitly-empty directories.

  Usage:
    pwsh -File .\src\scripts\Code_Bootstrapper_01.ps1
    pwsh -File .\src\scripts\Code_Bootstrapper_01.ps1 -WhatIf   # dry run
#>

[CmdletBinding(SupportsShouldProcess)]
param()

function Ensure-Dir {
  param([string]$Path)
  if (Test-Path -LiteralPath $Path) { return $true }
  New-Item -ItemType Directory -Path $Path -Force | Out-Null
  return $true
}

function Ensure-File {
  param([string]$Path)
  $dir = Split-Path -Parent $Path
  if (-not [string]::IsNullOrWhiteSpace($dir)) { Ensure-Dir $dir | Out-Null }
  if (-not (Test-Path -LiteralPath $Path)) {
    New-Item -ItemType File -Path $Path -Force | Out-Null
  }
}

function Ensure-Gitkeep {
  param([string]$Dir)
  Ensure-Dir $Dir | Out-Null
  $keep = Join-Path $Dir ".gitkeep"
  if (-not (Test-Path -LiteralPath $keep)) {
    New-Item -ItemType File -Path $keep -Force | Out-Null
  }
}

# --- Resolve repo root (script is in src/scripts) ---
$ScriptDir = Split-Path -Parent $PSCommandPath
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "..\..")

Write-Verbose "ScriptDir: $ScriptDir"
Write-Verbose "RepoRoot:  $RepoRoot"

# --------------------------------------------
# FOLDERS
# --------------------------------------------
$Folders = @(
  # Vector index (Azure AI Search)
  "src/LimboDancer.MCP.Vector.AzureSearch",

  # Cosmos Gremlin graph scaffold
  "src/LimboDancer.MCP.Graph.CosmosGremlin",
  "tests/LimboDancer.MCP.Graph.CosmosGremlin.Tests",
  "tests/LimboDancer.MCP.Graph.CosmosGremlin.Tests/Fakes",

  # MCP tools (history/memory/graph)
  "src/LimboDancer.MCP.McpServer/Tools",

  # Operator Console (Blazor)
  "src/LimboDancer.MCP.BlazorConsole/Services",
  "src/LimboDancer.MCP.BlazorConsole/Pages",

  # Dev bootstrap + CLI
  "src/LimboDancer.MCP.Cli/Commands",

  # Persistence (EF Core)
  "src/LimboDancer.MCP.Storage/Repositories",

  # Core abstractions + Ontology
  "src/LimboDancer.MCP.Core/Abstractions",
  "src/LimboDancer.MCP.Ontology",

  # HTTP host (placeholder)
  "src/LimboDancer.MCP.McpServer.Http",

  # Tools manifest & docs
  "tools",
  "docs"
)

# --------------------------------------------
# FILES (create blank placeholders)
# --------------------------------------------
$Files = @(
  # Vector index (Azure AI Search)
  "src/LimboDancer.MCP.Vector.AzureSearch/SearchIndexBuilder.cs",
  "src/LimboDancer.MCP.Vector.AzureSearch/VectorStore.cs",
  "tools/ai-search-index.json",

  # Cosmos Gremlin graph scaffold
  "src/LimboDancer.MCP.Graph.CosmosGremlin/LimboDancer.MCP.Graph.CosmosGremlin.csproj",
  "src/LimboDancer.MCP.Graph.CosmosGremlin/GremlinClientFactory.cs",
  "src/LimboDancer.MCP.Graph.CosmosGremlin/GraphStore.cs",
  "src/LimboDancer.MCP.Graph.CosmosGremlin/Preconditions.cs",
  "src/LimboDancer.MCP.Graph.CosmosGremlin/Effects.cs",

  # Cosmos Gremlin tests + fake
  "tests/LimboDancer.MCP.Graph.CosmosGremlin.Tests/LimboDancer.MCP.Graph.CosmosGremlin.Tests.csproj",
  "tests/LimboDancer.MCP.Graph.CosmosGremlin.Tests/EmulatorFixture.cs",
  "tests/LimboDancer.MCP.Graph.CosmosGremlin.Tests/GraphStore_EmulatorTests.cs",
  "tests/LimboDancer.MCP.Graph.CosmosGremlin.Tests/Fakes/FakeGremlinClient.cs",

  # MCP tool surface (first useful tools)
  "src/LimboDancer.MCP.McpServer/Tools/HistoryGetTool.cs",
  "src/LimboDancer.MCP.McpServer/Tools/HistoryAppendTool.cs",
  "src/LimboDancer.MCP.McpServer/Tools/MemorySearchTool.cs",
  "src/LimboDancer.MCP.McpServer/Tools/GraphQueryTool.cs",

  # Operator Console (Blazor) services/pages
  "src/LimboDancer.MCP.BlazorConsole/Services/SessionsService.cs",
  "src/LimboDancer.MCP.BlazorConsole/Services/MemoryService.cs",
  "src/LimboDancer.MCP.BlazorConsole/Services/GraphService.cs",
  "src/LimboDancer.MCP.BlazorConsole/Pages/Sessions.razor",
  "src/LimboDancer.MCP.BlazorConsole/Pages/Memory.razor",
  "src/LimboDancer.MCP.BlazorConsole/Pages/Graph.razor",
  "src/LimboDancer.MCP.BlazorConsole/Pages/Ingestion.razor",

  # Dev bootstrap + CLI
  "src/LimboDancer.MCP.Cli/Program.cs",
  "src/LimboDancer.MCP.Cli/Commands/ServicesBootstrap.cs",
  "src/LimboDancer.MCP.Cli/Commands/ServeCommand.cs",
  "src/LimboDancer.MCP.Cli/Commands/DbMigrateCommand.cs",
  "src/LimboDancer.MCP.Cli/Commands/VectorInitCommand.cs",
  "src/LimboDancer.MCP.Cli/Commands/KgPingCommand.cs",
  "src/LimboDancer.MCP.Cli/Commands/MemAddCommand.cs",
  "src/LimboDancer.MCP.Cli/Commands/MemSearchCommand.cs",

  # Persistence (EF Core)
  "src/LimboDancer.MCP.Storage/LimboDancer.MCP.Storage.csproj",
  "src/LimboDancer.MCP.Storage/Entities.cs",
  "src/LimboDancer.MCP.Storage/ChatDbContext.cs",
  "src/LimboDancer.MCP.Storage/StorageOptions.cs",
  "src/LimboDancer.MCP.Storage/ServiceCollectionExtensions.cs",
  "src/LimboDancer.MCP.Storage/Repositories/ChatHistoryStore.cs",

  # Core abstractions
  "src/LimboDancer.MCP.Core/Abstractions/IChatHistoryStore.cs",

  # Ontology (placeholder surface for runtime/consts)
  "src/LimboDancer.MCP.Ontology/Ontology.cs",
  "src/LimboDancer.MCP.Ontology/JsonLdContext.cs",

  # HTTP host (placeholder config)
  "src/LimboDancer.MCP.McpServer.Http/appsettings.json",
  "src/LimboDancer.MCP.McpServer.Http/appsettings.Development.json"
)

# --------------------------------------------
# EXECUTION
# --------------------------------------------

# Create folders
foreach ($f in $Folders) {
  $abs = Join-Path $RepoRoot $f
  if ($PSCmdlet.ShouldProcess($abs, "Ensure-Dir")) { Ensure-Dir $abs | Out-Null }
}

# Create files (blank)
foreach ($p in $Files) {
  $abs = Join-Path $RepoRoot $p
  if ($PSCmdlet.ShouldProcess($abs, "Ensure-File")) { Ensure-File $abs }
}

# Add .gitkeep to any intentionally-empty directories (customize as needed)
$EmptyDirs = @(
  # add any directories you want to keep even when empty
)
foreach ($d in $EmptyDirs) {
  $abs = Join-Path $RepoRoot $d
  if ($PSCmdlet.ShouldProcess($abs, "Ensure-Gitkeep")) { Ensure-Gitkeep $abs }
}

Write-Host "Create-only bootstrap complete."
