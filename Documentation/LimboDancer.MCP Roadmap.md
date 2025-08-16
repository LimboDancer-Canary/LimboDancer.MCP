# LimboDancer.MCP Roadmap

Here’s a practical, incremental roadmap to build **LimboDancer.MCP**—a clean-room C#/.NET re-implementation of Cipher’s MCP memory layer—with clear milestones, deliverables, and parity targets.

---

# 0) Foundations — scope, compliance, and repo setup

**Goals**

* Define feature parity targets by reviewing Cipher’s core behaviors (MCP server mode, built-in tools, dual memory layer, vector-store & chat history options). ([GitHub][1])
* Choose initial transports (**stdio** first; add **Streamable HTTP** next, per MCP spec). ([Model Context Protocol][2])
* Confirm license posture: Cipher is **Elastic License 2.0**—reimplement via clean-room design, retain attribution, and avoid “as-a-service” offering. ([GitHub][1])

**Deliverables**

* `ARCHITECTURE.md` (high level): protocol, transports, storage, observability.
* `CODE_OF_CONDUCT.md`, `CONTRIBUTING.md`, `SECURITY.md`, and repo scaffolding.
* Parity matrix drafted from Cipher README (e.g., `cipher_memory_search`, `cipher_workspace_search`, graph APIs, etc.). ([GitHub][1])

---

# 1) Protocol & Host (MCP server skeleton)

**Why now**
Everything hangs off MCP; get a minimal server booting and visible to MCP tooling.

**Tasks**

* Stand up an **MCP server** using the **official C# SDK**; implement health & `tools/list` with a single noop tool. ([GitHub][3], [Microsoft for Developers][4])
* Support **stdio** transport; structure for **Streamable HTTP** (add after milestone 2). ([Model Context Protocol][5])
* Define version targeting against current **MCP spec** (date-based versioning). ([Model Context Protocol][2], [Model Context Protocol][6])

**Definition of Done**

* `ciphernet serve --stdio` launches; MCP Inspector/clients enumerate tools successfully. ([GitHub][7])

---

# 2) Core domain & persistence (sessions, chat history)

**Why now**
You need durable sessions and message metadata before layering retrieval/graph.

**Tasks**

* Model entities: `Session`, `Message`, `Attachment`, `ToolCall`, `MemoryItem`.
* Implement **EF Core** with **SQLite** first; add **PostgreSQL** (Npgsql) next. ([Microsoft Learn][8])
* Add migration + seeding CLI: `ciphernet db migrate/init`.
* Expose MCP tools: `history/get`, `history/append`.

**DoD**

* Round-trip chat saved and retrievable across restarts; unit + integration tests (in-proc server).
* Basic OpenTelemetry spans around each tool call. ([Microsoft Learn][9])

---

# 3) Embeddings & LLM adapters

**Why now**
Vector memory requires embeddings; unify through `Microsoft.Extensions.AI` abstractions.

**Tasks**

* Add `IEmbeddingGenerator` & `IChatClient` via **Microsoft.Extensions.AI**; wire **OpenAI .NET** and **Azure OpenAI** providers. ([Microsoft Learn][10], [GitHub][11], [Microsoft for Developers][12])
* Optional community providers for **Anthropic** and **Gemini**, gated behind interfaces. ([Anthropic][13], [GitHub][14], [NuGet][15], [Google Cloud][16])
* Config via `appsettings.json`/env with provider selection and model IDs.

**DoD**

* `ciphernet embeddings test "hello world"` stores & retrieves a sample vector.
* CI ensures keys absent → graceful failures with clear logs.

---

# 4) Vector store layer (knowledge memory)

**Why now**
Enables Cipher’s “knowledge memory” (System-1) parity.

**Tasks**

* Implement **Qdrant** connector (priority), then **Milvus**, **Chroma**; align with Cipher’s supported stores. ([GitHub][17], [Qdrant][18], [Microsoft Learn][19], [Milvus][20], [NuGet][21], [Microsoft for Developers][22])
* Define CRUD + query API: `AddEmbeddingsAsync`, `QueryAsync` (kNN + filters), `DeleteAsync`.
* MCP tools: `cipher_memory_search`, `cipher_extract_and_operate_memory` (search + ADD/UPDATE/DELETE). ([GitHub][1])

**DoD**

* End-to-end: add N docs → vectorize → retrieve by semantic query; measured latency & recall.

---

# 5) Reasoning memory (System-2) & workspace memory

**Why now**
Completes dual-memory parity from Cipher, plus team/workspace context.

**Tasks**

* Add a “reasoning traces” store (schema + EF Core).
* Implement MCP tools mirroring Cipher: `cipher_store_reasoning_memory`, `cipher_search_reasoning_patterns`.
* Workspace memory (team/project): collections + MCP tools `cipher_workspace_store/search`. ([GitHub][1])

**DoD**

* Structured reasoning traces accepted, stored, and discoverable via search.

---

# 6) Knowledge graph (optional but important parity)

**Why now**
Graph augments retrieval with relationships (entities, edges).

**Tasks**

* Add **Neo4j** provider (`Neo4j.Driver`), with upsert/query helpers. ([Graph Database & Analytics][23], [GitHub][24])
* MCP tools: `cipher_add_node/edge`, `cipher_search_graph`, `cipher_query_graph`. ([GitHub][1])
* Map graph hits to hybrid results (graph + vector candidates).

**DoD**

* Create nodes/edges; query neighbors and get merged results with vector hits.

---

# 7) Transports: Streamable HTTP (+ SSE where applicable)

**Why now**
Remote/debug clients increasingly expect **Streamable HTTP**; MCP formalizes it.

**Tasks**

* Add **Streamable HTTP** transport endpoint with optional SSE event streams. ([Model Context Protocol][5])
* Harden auth and request limits (per evolving MCP guidance). ([Model Context Protocol][2])
* Load test under concurrent clients.

**DoD**

* Inspector & example clients validate tool discovery and streaming responses over HTTP. ([GitHub][7], [Model Context Protocol][25])

---

# 8) Observability, logging, and tracing

**Tasks**

* Standardize on `ILogger<T>` + **Serilog** (structured logging) with sinks; OTEL traces via `ActivitySource` and exporters. ([serilog.net][26], [Seq][27], [Microsoft Learn][9])
* Emit spans per MCP request/tool call; include vector DB & LLM timing. ([OpenTelemetry][28])

**DoD**

* One-click local observability (logs + console exporter). Dashboards doc’d.

---

# 9) Compatibility & compliance test suite

**Tasks**

* Add MCP conformance checks using community validators/inspectors; keep pace with **date-based spec** updates. ([Model Context Protocol][29], [GitHub][7])
* Wire reference clients from the MCP ecosystem (examples) into CI smoke tests. ([Model Context Protocol][30])

**DoD**

* CI gate that fails on protocol regressions; compatibility matrix published.

---

# 10) Developer Experience (DX) & packaging

**Tasks**

* **dotnet tool**: `dotnet tool install -g LimboDancer.MCP.CLI` with verbs (`serve`, `db`, `mem add/search`).
* Dockerfile + compose for `ciphernet` + Qdrant/Neo4j dev stacks.
* Samples: minimal MCP client script + how-to with MCP Inspector. ([GitHub][7])

**DoD**

* “5-minute demo” doc: install → run → store → search → retrieve.

---

# 11) Parity closeout with Cipher

**Tasks**

* Reconcile remaining built-in tool gaps (from README list) and env options (vector store, chat DB). ([GitHub][1])
* Author migration guide: “Cipher (TS) → LimboDancer.MCP” config mapping (`cipher.yml` → `appsettings.json`/env).

**DoD**

* Parity checklist 100% or explicitly deferred items documented.

---

## Acceptance Gates (suggested)

* **Alpha**: Milestones 1–4 complete (stdio + SQLite + one vector store + embeddings).
* **Beta**: + Milestones 5–8 (reasoning/workspace memory, Neo4j optional, HTTP transport, observability).
* **1.0**: + Milestones 9–11 (compliance suite, packaging, parity closeout).

---

## Implementation notes & references

* Cipher’s modes, tools, env & storage options (vector stores, Postgres/SQLite) provide the parity baseline. ([GitHub][1])
* MCP C# SDK + guidance for building servers in .NET anchors the hosting & transports. ([GitHub][3], [Microsoft for Developers][4])
* .NET AI abstractions unify LLMs/embeddings; official **OpenAI .NET** and **Azure OpenAI** SDKs provide first-party providers. ([Microsoft Learn][10], [GitHub][11])
* Production-grade vector DB clients exist for **Qdrant**, **Milvus**, and **Chroma** (community/official), matching Cipher’s documented stores. ([GitHub][17], [Qdrant][18], [Microsoft Learn][19], [Milvus][20], [NuGet][21], [Microsoft for Developers][22])
* **Neo4j .NET driver** covers graph parity. ([Graph Database & Analytics][23], [GitHub][24])
* **EF Core** is the recommended path for chat history persistence. ([Microsoft Learn][8])
* MCP transport specifics and versioning are evolving—track spec updates (date-keyed). ([Model Context Protocol][2], [Model Context Protocol][6])

If you want, I can turn this into a living `ROADMAP.md` plus a parity checklist you can track in GitHub Projects, and generate the initial solution layout with the CLI/host, persistence, and tests wired up.

[1]: https://github.com/campfirein/cipher "GitHub - campfirein/cipher: Cipher is an opensource memory layer specifically designed for coding agents. Compatible with Cursor, Windsurf, Claude Desktop, Claude Code, Gemini CLI, AWS's Kiro, VS Code, and Roo Code through MCP, and coding agents, such as Kimi K2. Built by https://byterover.dev/"
[2]: https://modelcontextprotocol.io/specification/2025-06-18?utm_source=chatgpt.com "Specification"
[3]: https://github.com/modelcontextprotocol/csharp-sdk?utm_source=chatgpt.com "modelcontextprotocol/csharp-sdk: The official C# ..."
[4]: https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/?utm_source=chatgpt.com "Build a Model Context Protocol (MCP) server in C# - .NET ..."
[5]: https://modelcontextprotocol.io/docs/concepts/transports?utm_source=chatgpt.com "Transports"
[6]: https://spec.modelcontextprotocol.io/?utm_source=chatgpt.com "Model Context Protocol: Versioning"
[7]: https://github.com/modelcontextprotocol/inspector?utm_source=chatgpt.com "modelcontextprotocol/inspector: Visual testing tool for MCP ..."
[8]: https://learn.microsoft.com/en-us/ef/core/?utm_source=chatgpt.com "Overview of Entity Framework Core - EF Core"
[9]: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs?utm_source=chatgpt.com "Add distributed tracing instrumentation - .NET"
[10]: https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai?utm_source=chatgpt.com "Microsoft.Extensions.AI libraries - .NET"
[11]: https://github.com/openai/openai-dotnet?utm_source=chatgpt.com "The official .NET library for the OpenAI API"
[12]: https://devblogs.microsoft.com/dotnet/openai-dotnet-library/?utm_source=chatgpt.com "Announcing the official OpenAI library for .NET - .NET Blog"
[13]: https://docs.anthropic.com/en/api/client-sdks?utm_source=chatgpt.com "Client SDKs"
[14]: https://github.com/tghamm/Anthropic.SDK?utm_source=chatgpt.com "tghamm/Anthropic.SDK: An unofficial C#/.NET ..."
[15]: https://www.nuget.org/packages/Anthropic.SDK/2.0.0?utm_source=chatgpt.com "Anthropic.SDK 2.0.0"
[16]: https://cloud.google.com/vertex-ai/generative-ai/docs/sdks/overview?utm_source=chatgpt.com "Google Gen AI SDK | Generative AI on Vertex AI"
[17]: https://github.com/qdrant/qdrant-dotnet?utm_source=chatgpt.com "Qdrant .Net SDK"
[18]: https://qdrant.tech/documentation/interfaces/?utm_source=chatgpt.com "API & SDKs"
[19]: https://learn.microsoft.com/en-us/dotnet/aspire/database/qdrant-integration?utm_source=chatgpt.com "NET Aspire Qdrant integration"
[20]: https://milvus.io/docs/v2.2.x/install-csharp.md?utm_source=chatgpt.com "Install Milvus C# SDK Milvus v2.2.x documentation"
[21]: https://www.nuget.org/packages/Milvus.Client?utm_source=chatgpt.com "Milvus.Client 2.3.0-preview.1"
[22]: https://devblogs.microsoft.com/dotnet/announcing-chroma-db-csharp-sdk/?utm_source=chatgpt.com "Building .NET AI apps with Chroma"
[23]: https://neo4j.com/docs/dotnet-manual/current/?utm_source=chatgpt.com "Build applications with Neo4j and .NET"
[24]: https://github.com/neo4j/neo4j-dotnet-driver?utm_source=chatgpt.com "neo4j/neo4j-dotnet-driver: Neo4j Bolt driver for .NET"
[25]: https://modelcontextprotocol.io/clients?utm_source=chatgpt.com "Example Clients"
[26]: https://serilog.net/?utm_source=chatgpt.com "Serilog — simple .NET logging with fully-structured events"
[27]: https://datalust.co/docs/using-serilog?utm_source=chatgpt.com "Serilog"
[28]: https://opentelemetry.io/docs/languages/dotnet/traces/getting-started-console/?utm_source=chatgpt.com "Getting started with traces - Console"
[29]: https://modelcontextprotocol.io/development/roadmap?utm_source=chatgpt.com "Roadmap"
[30]: https://modelcontextprotocol.io/examples?utm_source=chatgpt.com "Example Servers"
