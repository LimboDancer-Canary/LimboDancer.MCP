[CmdletBinding()]
param(
  [switch]$Create,                          # scaffold missing projects
  [string]$SolutionName = "LimboDancer.MCP",
  [switch]$NoRestore = $true                # skip restore during dotnet new
)

$ErrorActionPreference = 'Stop'

# --- Resolve repo root from script location ---
$ScriptRoot = $PSScriptRoot
if (-not $ScriptRoot) { $ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path }
$RepoRoot  = (Resolve-Path (Join-Path $ScriptRoot "..\..")).Path

function Join-RepoPath {
  param([Parameter(Mandatory)][string]$Relative)
  Join-Path $RepoRoot $Relative
}

# Prefer solution at repo root
$sln = Join-RepoPath "$SolutionName.sln"
$haveSln = Test-Path $sln

# Expected projects (paths are RELATIVE TO REPO ROOT)
$ProjMap = @(
  @{ Path="src/LimboDancer.MCP.Core";                Name="LimboDancer.MCP.Core";                Template="classlib"  }
  @{ Path="src/LimboDancer.MCP.Ontology";            Name="LimboDancer.MCP.Ontology";            Template="classlib"  }
  @{ Path="src/LimboDancer.MCP.Storage";             Name="LimboDancer.MCP.Storage";             Template="classlib"  }
  @{ Path="src/LimboDancer.MCP.Vector.AzureSearch";  Name="LimboDancer.MCP.Vector.AzureSearch";  Template="classlib"  }
  @{ Path="src/LimboDancer.MCP.Graph.CosmosGremlin"; Name="LimboDancer.MCP.Graph.CosmosGremlin"; Template="classlib"  }
  @{ Path="src/LimboDancer.MCP.Llm";                 Name="LimboDancer.MCP.Llm";                 Template="classlib"  }
  @{ Path="src/LimboDancer.MCP.McpServer";           Name="LimboDancer.MCP.McpServer";           Template="classlib"  }
  @{ Path="src/LimboDancer.MCP.McpServer.Http";      Name="LimboDancer.MCP.McpServer.Http";      Template="webapi"    }
  @{ Path="src/LimboDancer.MCP.Cli";                 Name="LimboDancer.MCP.Cli";                 Template="console"   }
  @{ Path="src/LimboDancer.MCP.BlazorConsole";       Name="LimboDancer.MCP.BlazorConsole";       Template="blazorserver" }
  @{ Path="tests/LimboDancer.MCP.Graph.CosmosGremlin.Tests"; Name="LimboDancer.MCP.Graph.CosmosGremlin.Tests"; Template="xunit" }
)

function Invoke-Dotnet([string[]]$Args) {
  Write-Host "dotnet $($Args -join ' ')" -ForegroundColor DarkCyan
  & dotnet @Args
  if ($LASTEXITCODE -ne 0) { throw "dotnet failed: $($Args -join ' ')" }
}

# Ensure one project (verify after create, add to sln)
function Ensure-Project([hashtable]$P) {
  $projDir  = Join-RepoPath $P.Path
  $projFile = Join-Path   $projDir ($P.Name + ".csproj")
  $exists   = Test-Path $projFile

  if ($exists -or -not $Create) {
    return [pscustomobject]@{ Project=$P.Name; Path=$P.Path; Status = $exists ? "OK" : "MISSING" }
  }

  if (-not (Test-Path $projDir)) { New-Item -ItemType Directory -Path $projDir | Out-Null }

  $dotnetArgs = @("new", $P.Template, "-n", $P.Name, "-o", $projDir)
  if ($NoRestore) { $dotnetArgs += "--no-restore" }
  Invoke-Dotnet $dotnetArgs

  if (-not (Test-Path $projFile)) {
    throw "Project creation failed: $projFile not found after 'dotnet new'."
  }

  if (-not $haveSln) {
    Invoke-Dotnet @("new","sln","-n",$SolutionName,"-o",$RepoRoot)
    $haveSln = $true
  }

  Invoke-Dotnet @("sln",$sln,"add",$projFile)
  [pscustomobject]@{ Project=$P.Name; Path=$P.Path; Status = "CREATED" }
}

# Create / verify
$report = foreach ($p in $ProjMap) { Ensure-Project $p }

# Add references only where both sides exist
function Add-Ref([string]$from,[string]$to) {
  $fromFile = Join-Path (Join-RepoPath $from) ($from.Split('/')[-1] + ".csproj")
  $toFile   = Join-Path (Join-RepoPath $to)   ($to.Split('/')[-1]   + ".csproj")
  if ((Test-Path $fromFile) -and (Test-Path $toFile)) {
    Invoke-Dotnet @("add",$fromFile,"reference",$toFile)
  } else {
    if (-not (Test-Path $fromFile)) { Write-Host "SKIP ref: missing $fromFile" -ForegroundColor Yellow }
    if (-not (Test-Path $toFile))   { Write-Host "SKIP ref: missing $toFile"   -ForegroundColor Yellow }
  }
}

# Wire references (idempotent)
Add-Ref "src/LimboDancer.MCP.Storage"       "src/LimboDancer.MCP.Core"
Add-Ref "src/LimboDancer.MCP.McpServer"     "src/LimboDancer.MCP.Core"
Add-Ref "src/LimboDancer.MCP.McpServer"     "src/LimboDancer.MCP.Storage"
Add-Ref "src/LimboDancer.MCP.McpServer"     "src/LimboDancer.MCP.Ontology"
Add-Ref "src/LimboDancer.MCP.McpServer"     "src/LimboDancer.MCP.Vector.AzureSearch"
Add-Ref "src/LimboDancer.MCP.McpServer"     "src/LimboDancer.MCP.Graph.CosmosGremlin"

Add-Ref "src/LimboDancer.MCP.BlazorConsole" "src/LimboDancer.MCP.Storage"
Add-Ref "src/LimboDancer.MCP.BlazorConsole" "src/LimboDancer.MCP.Vector.AzureSearch"
Add-Ref "src/LimboDancer.MCP.BlazorConsole" "src/LimboDancer.MCP.Graph.CosmosGremlin"

Add-Ref "src/LimboDancer.MCP.Cli"           "src/LimboDancer.MCP.Storage"
Add-Ref "src/LimboDancer.MCP.Cli"           "src/LimboDancer.MCP.Vector.AzureSearch"
Add-Ref "src/LimboDancer.MCP.Cli"           "src/LimboDancer.MCP.Graph.CosmosGremlin"

# Summary
$report | Sort-Object Project | Format-Table -AutoSize
Write-Host ""
if ($Create) {
  Write-Host "✅ Verified creation of any missing projects; solution and references updated." -ForegroundColor Green
} else {
  Write-Host "ℹ️  Run with -Create to scaffold any missing projects (fast: --no-restore default)." -ForegroundColor Yellow
}
