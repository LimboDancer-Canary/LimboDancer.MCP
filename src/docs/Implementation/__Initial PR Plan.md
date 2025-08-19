# Initial PR Plan
# First 5 PRs (tiny, incremental)

## PR‑1: Persistence baseline + Core contracts

**Projects**

* `src/LimboDancer.MCP.Core` (classlib)
* `src/LimboDancer.MCP.Storage` (classlib)

**Contents**

* Core: `IChatHistoryStore` and simple result/exception types.
* Storage: EF Core models `Session`, `Message`, `MemoryItem`; `ChatDbContext`; `ChatHistoryStore` (create session, append, read); options + DI extension.

**Packages**

* `Npgsql.EntityFrameworkCore.PostgreSQL`
* `Microsoft.EntityFrameworkCore` (+ Design as PrivateAssets)

**Smoke test**

* Console `dotnet ef migrations add Init && dotnet ef database update` (locally).
* Minimal unit test: create session → append two messages → read back 2.

---

## PR‑2: Vector layer (Azure AI Search)

**Project**

* `src/LimboDancer.MCP.Vector.AzureSearch` (classlib)

**Contents**

* `SearchIndexBuilder` (ensure index with BM25+vector fields).
* `VectorStore` with:

  * `UpsertAsync(IEnumerable<MemoryDoc>)` (optionally embed)
  * `SearchHybridAsync(queryText/vector, k, filters)`

**Packages**

* `Azure.Search.Documents`

**Smoke test**

* Hard‑code an embedding delegate returning random vectors; upsert two docs; hybrid search returns them.

---

## PR‑3: Graph layer (Cosmos Gremlin)

**Project**

* `src/LimboDancer.MCP.Graph.CosmosGremlin` (classlib)

**Contents**

* `GremlinClientFactory` + `GremlinOptions`
* Small helpers: `UpsertVertexAsync`, `UpsertEdgeAsync` (you can keep read‑only for now if preferred)

**Packages**

* `Gremlin.Net`

**Smoke test**

* Connect and run `g.V().limit(1).count()`.

---

## PR‑4: Developer CLI

**Project**

* `src/LimboDancer.MCP.Cli` (console)

**Commands**

* `serve --stdio` (stub runner—prints “wire MCP here”)
* `db migrate` (applies EF migrations for Chat + Audit—or just Chat if Audit not ready)
* `vector init` (calls `SearchIndexBuilder`)
* `kg ping` (Gremlin count)
* `mem add` (file/text → chunk → embed → upsert)
* `mem search` (hybrid search)

**Packages**

* `System.CommandLine`
* Reuse the libs from PR‑1/2/3

**Smoke test**

* `dotnet run --project src/LimboDancer.MCP.Cli -- db migrate`
* `… mem add --text "hello world" --tags test`
* `… mem search --query hello`

---

## PR‑5: Operator Console (Blazor Server, read‑only)

**Project**

* `src/LimboDancer.MCP.BlazorConsole` (Blazor Server)

**Pages**

* `/sessions` (sessions + messages via EF)
* `/memory` (AI Search query with simple filters)
* `/graph` (list vertices by label + show out‑edges)

**Packages**

* `Microsoft.AspNetCore.Components.Web`, `Azure.Search.Documents`, `Gremlin.Net`, `Microsoft.EntityFrameworkCore`

**Smoke test**

* Run, load each page, confirm tables render.

---

# Solution & wiring (once, then reused)

```bash
# Create sln & projects (adjust if already created)
dotnet new sln -n LimboDancer.MCP

dotnet new classlib -n LimboDancer.MCP.Core             -o src/LimboDancer.MCP.Core
dotnet new classlib -n LimboDancer.MCP.Storage          -o src/LimboDancer.MCP.Storage
dotnet new classlib -n LimboDancer.MCP.Vector.AzureSearch -o src/LimboDancer.MCP.Vector.AzureSearch
dotnet new classlib -n LimboDancer.MCP.Graph.CosmosGremlin -o src/LimboDancer.MCP.Graph.CosmosGremlin
dotnet new console  -n LimboDancer.MCP.Cli              -o src/LimboDancer.MCP.Cli
dotnet new blazorserver -n LimboDancer.MCP.BlazorConsole -o src/LimboDancer.MCP.BlazorConsole

# Add to solution
dotnet sln add src/**/**.csproj

# References
dotnet add src/LimboDancer.MCP.Storage/LimboDancer.MCP.Storage.csproj reference src/LimboDancer.MCP.Core/LimboDancer.MCP.Core.csproj
dotnet add src/LimboDancer.MCP.Cli/LimboDancer.MCP.Cli.csproj reference \
  src/LimboDancer.MCP.Core/LimboDancer.MCP.Core.csproj \
  src/LimboDancer.MCP.Storage/LimboDancer.MCP.Storage.csproj \
  src/LimboDancer.MCP.Vector.AzureSearch/LimboDancer.MCP.Vector.AzureSearch.csproj \
  src/LimboDancer.MCP.Graph.CosmosGremlin/LimboDancer.MCP.Graph.CosmosGremlin.csproj

dotnet add src/LimboDancer.MCP.BlazorConsole/LimboDancer.MCP.BlazorConsole.csproj reference \
  src/LimboDancer.MCP.Storage/LimboDancer.MCP.Storage.csproj \
  src/LimboDancer.MCP.Vector.AzureSearch/LimboDancer.MCP.Vector.AzureSearch.csproj \
  src/LimboDancer.MCP.Graph.CosmosGremlin/LimboDancer.MCP.Graph.CosmosGremlin.csproj
```

**App settings (dev)**

* BlazorConsole & CLI share keys:

  * `Persistence:ConnectionString`
  * `Search:{Endpoint,ApiKey,Index}`
  * `Gremlin:{Host,Port,Database,Graph,Key}`

---

# Branch & review checklist

* **branch:** `feat/persistence-core` → PR‑1
  ✅ build + one EF test passes

* **branch:** `feat/vector-layer` → PR‑2
  ✅ `vector init` + `mem add/search` work (fake embed ok)

* **branch:** `feat/graph-layer` → PR‑3
  ✅ `kg ping` prints a count without exceptions

* **branch:** `feat/dev-cli` → PR‑4
  ✅ all commands show help; basic flows run

* **branch:** `feat/operator-console-ro` → PR‑5
  ✅ pages render; no writes yet

---

# What I’ll do next (once you confirm)

* Drop in minimal, production‑ready code for each PR exactly as outlined (matching your namespaces and folder names), including DI extensions and example `appsettings.Development.json`.

If you want, we can start with PR‑1 right now and I’ll provide the full files.
