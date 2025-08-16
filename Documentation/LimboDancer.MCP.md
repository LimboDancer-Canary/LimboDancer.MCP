# LimboDancer.MCP

## Porting Cipher to .NET

“LimboDancer.MCP” is technically feasible. Cipher’s current design (TypeScript + MCP server + vector-store + SQL chat history + optional knowledge graph + event system) maps cleanly onto modern .NET 8/9 libraries, and the MCP ecosystem already has a C# SDK. Below is a crisp feasibility read plus an implementation map.

# What Cipher is (in brief)

* **Acts as an MCP server** so IDEs/agents can call tools for memory, retrieval, etc. ([OpenAI Platform][1], [Byterover][2])
* **Dual memory**: (1) semantic “knowledge memory” in a vector DB; (2) an optional **knowledge graph** for relationships; plus **chat history & metadata** in SQLite/Postgres. ([Byterover][3], [GitHub][4])
* **Event system** for service/session tracing and integrations. ([Byterover][5])
* **LLM/embedding providers** are configurable. (Docs + repo indicate multi-provider support.) ([GitHub][6], [Byterover][7])
* **License**: Elastic License 2.0 (ELv2). Porting is allowed but with restrictions (no “as-a-service,” don’t strip notices, etc.). ([GitHub][8], [Elastic][9])

# Feasibility in .NET/C\#

## Protocol & host

* **MCP server in C#**: build with the official C# MCP SDK (or Microsoft’s MCP package). This gives JSON-RPC transports (stdio/websocket) and schema’d tool calls. ([GitHub][10], [Microsoft for Developers][11])
* **Background automation**: use `IHostedService` in an ASP.NET Core worker or `dotnet tool` CLI—fits Cipher’s “auto tools / eventing” model. (General mapping; Cipher docs show background/automatic tools & events.) ([Byterover][5])

## Memory layers

* **Vector store connectors**: solid .NET clients exist for **Qdrant**, **Milvus**, and **Chroma**. You can code direct or go via Semantic Kernel/ASPire integrations. ([NuGet][12], [Microsoft for Developers][13], [Microsoft Learn][14])
* **Knowledge graph** (optional but supported by Cipher): use **Neo4j** (`Neo4j.Driver`) or a Gremlin store; both have .NET drivers. ([NuGet][15])
* **Chat history & metadata**: **EF Core** with **SQLite**/**Postgres** (Npgsql) or Dapper for micro-ORM. ([NuGet][16])

## LLM & embeddings

* **OpenAI/Azure OpenAI** official .NET SDKs and **Microsoft.Extensions.AI** abstractions (chat + embeddings) slot in cleanly; community SDKs cover **Anthropic**, **Gemini**, and **OpenRouter** if needed. ([GitHub][17], [Microsoft Learn][18], [Microsoft for Developers][19], [NuGet][20])

## Events/observability

* Map Cipher’s **service/session events** to **`ILogger` + `ActivitySource`** (OpenTelemetry exporter optional). The event types & tiers are documented; you can mirror them in C# event records. ([Byterover][5])

## Licensing check

* ELv2 permits use/modification/redistribution with **three limits** (not offering it as a managed service, not circumventing license-keyed features, don’t remove notices). A straight **clean-room re-implementation** in C# with attribution is compatible, but avoid “Cipher-as-a-service.” (Not legal advice.) ([Elastic][9])

# Suggested project layout for “LimboDancer.MCP”

* **LimboDancer.MCP.Core** – Abstractions: `IMemoryStore`, `IVectorStore`, `IKnowledgeGraph`, `IEventSink`, `IChatHistoryStore`.
* **LimboDancer.MCP.Vector.Qdrant/Milvus/Chroma** – Concrete vector connectors. ([NuGet][12])
* **LimboDancer.MCP.Graph.Neo4j** – Graph store with Cypher helpers. (Alternative: `Gremlin.Net` provider.) ([NuGet][15])
* **LimboDancer.MCP.Storage** – EF Core models/migrations for sessions, messages, metadata. ([NuGet][16])
* **LimboDancer.MCP.Llm** – Adapters over `Microsoft.Extensions.AI` for chat/embeddings; providers: OpenAI/Azure OpenAI/Anthropic/Gemini. ([Microsoft Learn][21], [GitHub][17], [NuGet][20])
* **LimboDancer.MCP.McpServer** – MCP server executable exposing tools (`tools/list`, `memory/search`, `graph/query`, `history/get`, etc.). ([GitHub][10])
* **LimboDancer.MCP.Cli** – `dotnet tool` to run local server, init stores, print config.

# Porting map (TS → C#)

* **Config (`cipher.yml`)** → `appsettings.json` with YAML support via YamlDotNet (or keep YAML and load at startup).
* **Zod schemas** → `record` types + `System.ComponentModel.DataAnnotations` or `FluentValidation`.
* **Node streams & events** → `IAsyncEnumerable<>` streams, `Channel<T>`, `ILogger` + OTEL.
* **Background loops** → `IHostedService` timers.
* **File watchers** (if used) → `FileSystemWatcher`.
* **JSON** → `System.Text.Json` (source-gen for perf).

# MVP scope & milestones

1. **MCP skeleton**: stand up `LimboDancer.MCP.McpServer` with one noop tool + health check (stdio). ([GitHub][10])
2. **Chat history**: EF Core + SQLite first; add Postgres later. ([NuGet][16])
3. **Vector search**: Qdrant connector; implement add/query/delete + metadata filters. (Milvus/Chroma as pluggable backends.) ([NuGet][12])
4. **Knowledge graph** (optional): Neo4j provider with simple upsert/query APIs; wire behind `IKnowledgeGraph`. ([NuGet][15])
5. **LLM adapters**: wire `IChatClient` + `IEmbeddingGenerator` (OpenAI/Azure OpenAI first). ([Microsoft Learn][21], [GitHub][17])
6. **Event bus**: service/session events, OTEL hooks; add a `events tail` CLI. ([Byterover][5])
7. **Prompts & “providers”**: load system prompts (the docs list built-in providers) into a provider pipeline. ([Byterover][22])

# Integration notes for Visual Studio (later)

Once LimboDancer.MCP is running as an **MCP server**, any MCP-aware client can connect. For VS specifically (which doesn’t natively speak MCP yet), options are:

* run LimboDancer.MCP locally and use an **MCP-capable agent** (Cursor/Windsurf/Claude Desktop, etc.) alongside VS; or
* build a thin **VSIX** that opens a pipe to the MCP server and surfaces core tools (search, save, retrieve). (MCP server behavior and memory components are already documented.) ([Byterover][2])

# Risks & call-outs

* **Feature parity**: some TypeScript libraries (e.g., zod, certain MCP helpers) don’t have one-to-one C# twins—minor glue code needed. (Feasible.)
* **Graph backend choice**: Neo4j is straightforward in .NET; Gremlin works too but watch .NET version compat notes. ([NuGet][15])
* **Licensing**: observe ELv2’s three limits; avoid turning LimboDancer.MCP into a managed service. Include attribution. (Review with counsel.) ([Elastic][9])

---

## Verdict

**Feasible**. The protocol (MCP), storage backends (Qdrant/Milvus/Chroma + SQLite/Postgres), LLM providers, and telemetry all have first-class .NET options. A pragmatic MVP could be delivered incrementally with clean abstractions, then expanded to match more of Cipher’s tooling and eventing model as documented. ([OpenAI Platform][1], [Byterover][3], [NuGet][12])

*(Sources reviewed: Cipher docs/README & pages on MCP connection, memory components, eventing, and license; plus .NET MCP/LLM/vector/graph client libraries.)* ([Byterover][7], [GitHub][23], [NuGet][12])

[1]: https://platform.openai.com/docs/mcp?utm_source=chatgpt.com "Building MCP servers for ChatGPT and API integrations"
[2]: https://docs.byterover.dev/cipher/connections?utm_source=chatgpt.com "Connections"
[3]: https://docs.byterover.dev/cipher/mcp-servers?utm_source=chatgpt.com "Mcp servers"
[4]: https://github.com/campfirein/cipher/blob/main/docs/chat-history.md "cipher/docs/chat-history.md at main · campfirein/cipher · GitHub"
[5]: https://docs.byterover.dev/cipher/event-system?utm_source=chatgpt.com "Event system"
[6]: https://github.com/campfirein/cipher/blob/main/docs/llm-providers.md "cipher/docs/llm-providers.md at main · campfirein/cipher · GitHub"
[7]: https://docs.byterover.dev/cipher/overview?utm_source=chatgpt.com "Overview"
[8]: https://github.com/campfirein/cipher "GitHub - campfirein/cipher: Cipher is an opensource memory layer specifically designed for coding agents. Compatible with Cursor, Windsurf, Claude Desktop, Claude Code, Gemini CLI, AWS's Kiro, VS Code, and Roo Code through MCP, and coding agents, such as Kimi K2. Built by https://byterover.dev/"
[9]: https://www.elastic.co/blog/elastic-license-v2?utm_source=chatgpt.com "Introducing Elastic License v2, simplified and more ..."
[10]: https://github.com/modelcontextprotocol/csharp-sdk?utm_source=chatgpt.com "modelcontextprotocol/csharp-sdk: The official C# ..."
[11]: https://devblogs.microsoft.com/blog/microsoft-partners-with-anthropic-to-create-official-c-sdk-for-model-context-protocol?utm_source=chatgpt.com "Microsoft partners with Anthropic to create official C# SDK ..."
[12]: https://www.nuget.org/packages/Qdrant.Client?utm_source=chatgpt.com "Qdrant.Client 1.15.0"
[13]: https://devblogs.microsoft.com/dotnet/announcing-chroma-db-csharp-sdk/?utm_source=chatgpt.com "Building .NET AI apps with Chroma"
[14]: https://learn.microsoft.com/en-us/dotnet/aspire/database/qdrant-integration?utm_source=chatgpt.com "NET Aspire Qdrant integration"
[15]: https://www.nuget.org/packages/neo4j.driver?utm_source=chatgpt.com "Neo4j.Driver 5.28.3"
[16]: https://www.nuget.org/packages/microsoft.entityframeworkcore.sqlite?utm_source=chatgpt.com "Microsoft.EntityFrameworkCore.Sqlite 9.0.8"
[17]: https://github.com/openai/openai-dotnet?utm_source=chatgpt.com "The official .NET library for the OpenAI API"
[18]: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.openai-readme?view=azure-dotnet&utm_source=chatgpt.com "Azure OpenAI client library for .NET"
[19]: https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/?utm_source=chatgpt.com "Introducing Microsoft.Extensions.AI Preview - Unified AI ..."
[20]: https://www.nuget.org/packages/Anthropic.SDK/?utm_source=chatgpt.com "Anthropic.SDK 5.5.0"
[21]: https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai?utm_source=chatgpt.com "Microsoft.Extensions.AI libraries - .NET"
[22]: https://docs.byterover.dev/cipher/system-prompt-management?utm_source=chatgpt.com "System prompt management"
[23]: https://github.com/campfirein/cipher/blob/main/LICENSE "cipher/LICENSE at main · campfirein/cipher · GitHub"
