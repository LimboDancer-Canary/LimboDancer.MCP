# Implementation Prototype Plan

1. **Persistence baseline (EF Core + Postgres)**

   * Files: `src/LimboDancer.MCP.Storage/{ChatDbContext.cs, Entities.cs}`, `appsettings.*`, first migration.
   * Entities: `Session`, `Message`, `MemoryItem` (as per data model).
   * Why: It’s Milestone 2 and unblocks history tools and testing. &#x20;

2. **Vector index for Azure AI Search (hybrid)**

   * Files: `src/LimboDancer.MCP.Vector.AzureSearch/{SearchIndexBuilder.cs, VectorStore.cs}`, `tools/ai-search-index.json`.
   * Include fields for ontology filters (class, uri, tags) and embeddings (dim, profile).
   * Why: Milestone 3 requires vector+BM25 with ontology filters; the architecture expects a dedicated vector lib. &#x20;

3. **Cosmos Gremlin graph scaffold**

   * Files: `src/LimboDancer.MCP.Graph.CosmosGremlin/{GremlinClientFactory.cs, GraphStore.cs, Preconditions.cs, Effects.cs}`.
   * Implement upsert of vertices/edges for core classes (Person, Trip, Reservation, Tool/Skill) and helpers to evaluate preconditions/effects.
   * Why: It’s called out as a core library and feeds the precondition/effect path. &#x20;

4. **MCP tool surface for history/memory/graph (first useful tools)**

   * Files: `src/LimboDancer.MCP.McpServer/Tools/{HistoryGetTool.cs, HistoryAppendTool.cs, MemorySearchTool.cs, GraphQueryTool.cs}`.
   * Each tool’s `input_schema` should embed your JSON-LD `@context` (ontology-bound fields) like we did for `cancelReservation`.
   * Why: Makes the server actually useful while we wire KG/vector; aligns with the design map and ontology-first approach. &#x20;

5. **Planner hook + precondition gate (typed ReAct “thin slice”)**

   * Files: `src/LimboDancer.MCP.Core/Planning/{PlanStep.cs, Planner.cs}` with an interface the server calls before invoking a tool; use KG precondition check helper.
   * Start with a simple “one-step” plan that refuses when preconditions fail; log an audit record.
   * Why: This is Milestone 5’s minimum and ties ontology to real execution.&#x20;

6. **HTTP transport hardening + SSE events**

   * Files: `src/LimboDancer.MCP.McpServer.Http/{Auth.cs, SseEndpoints.cs}`; wire Entra ID for auth and add `/mcp/events` for progress/trace streaming.
   * Why: It’s the next transport milestone and aligns with your component map. &#x20;

7. **Operator Console scaffold (Blazor Server)**

   * Files: `src/LimboDancer.MCP.BlazorConsole/Pages/{Sessions.razor, Memory.razor, Graph.razor, Ingestion.razor}`, shared data clients.
   * Start with read-only grids: sessions/messages (from Postgres), vector items (from AI Search), KG explorer (read).
   * Why: It’s the first step toward Milestone 9 and helps validation/debugging early. &#x20;

8. **Dev bootstrap + CLI**

   * Files: `src/LimboDancer.MCP.Cli/{Program.cs, Commands/*}`, `docs/DEVSETUP.md`, `appsettings.Development.json`.
   * Verbs: `serve --stdio`, `db migrate`, `vector init`, `kg ping`, `mem add/search`.
   * Why: Improves DX and hits the Roadmap DX tasks.&#x20;

If you’d like me to generate one of these right away, I recommend starting with **(1) Persistence baseline** and **(2) Vector index**, because they unlock quick end-to-end flows (store chat → embed → search) and are explicitly called out in Milestones 2–3.&#x20;
