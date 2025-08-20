Here’s a **rewritten PR-ready `LimboDancer.MCP Roadmap.md`** with all the new requirements integrated:

---

# LimboDancer.MCP — Roadmap

## Guiding Principles

* Built in **.NET 9**.
* Hosted in **Azure Container Apps**, leveraging **Azure-managed subsystems** (PostgreSQL, AI Search, Cosmos DB Gremlin, Service Bus, Blob, OpenAI, App Insights).
* **MCP runtime** = stateless headless worker/web API.
* **Blazor Server UI** = operator/console only (separate container, sticky sessions).
* **Ontology is first-class**: every tool, memory, and KG entry tied to ontology terms (JSON-LD contexts, typed schemas, precondition/effect validation).
* **Incremental milestones** with acceptance gates (Alpha → Beta → 1.0).
* Keep **.NET Aspire** in mind for future developer experience (local orchestration, observability, Azure resource wiring).

---

## Milestones

### **Milestone 1 — MCP Skeleton**

* Scaffold solution (`Core`, `Storage`, `Vector`, `Graph`, `Llm`, `Ontology`, `McpServer`, `Cli`, `BlazorConsole`).
* Implement MCP server in C# with stdio + noop tool.
* Health check endpoint + OpenTelemetry traces.
* ✅ *Acceptance: Agent client connects, `tools/list` returns noop.*

---

### **Milestone 2 — Persistence**

* EF Core + PostgreSQL Flexible Server.
* Entities: `Session`, `Message`, `MemoryItem`.
* Simple history persistence and retrieval.
* ✅ *Acceptance: Messages survive restart, persisted in Postgres.*

---

### **Milestone 3 — Embeddings and Vector Store**

* Integrate Azure OpenAI embeddings.
* Store/retrieve vectors in Azure AI Search (vector + BM25).
* Hybrid retrieval with ontology type filters.
* ✅ *Acceptance: Can add/search vectors with ontology metadata.*

---

### **Milestone 4 — Ontology v1**

* Introduce `/src/LimboDancer.MCP.Ontology` project.
* JSON-LD context with base classes (Person, Trip, Reservation, Tool, Skill, etc.).
* Map EF Core models and tool schemas to ontology URIs.
* ✅ *Acceptance: At least 3 tools expose ontology-bound schemas.*

---

### **Milestone 5 — Planner + Precondition/Effect Checks**

* Typed ReAct loop planner.
* KG precondition checks before tool execution.
* Commit effects to Postgres + KG.
* ✅ *Acceptance: A tool invocation fails if precondition not met, succeeds if met.*

---

### **Milestone 6 — Knowledge Graph Integration**

* Cosmos DB Gremlin API as graph store.
* Ingest ontology instances into graph.
* Queries for neighborhoods and relation traversal.
* ✅ *Acceptance: Can expand context with KG neighborhoods.*

---

### **Milestone 7 — Ingestion Pipeline**

* Event Grid trigger on Blob create/update.
* Worker processes doc → chunks → embeddings → ontology extraction.
* Upserts to AI Search and KG.
* ✅ *Acceptance: Blob drop triggers ingestion, data visible in vector store and KG.*

---

### **Milestone 8 — HTTP Transport**

* Streamable HTTP MCP endpoints (`/mcp/tools/list`, `/mcp/invoke`, `/mcp/events`).
* Authentication via Entra ID + Managed Identity.
* ✅ *Acceptance: Remote MCP client calls tool successfully over HTTP.*

---

### **Milestone 9 — Blazor Server Operator Console**

* Blazor Server app for admin/operators.
* Dashboards: sessions, memory items, KG explorer, event tail.
* Sticky sessions enabled.
* ✅ *Acceptance: Operator can browse sessions, tail events, and inspect ontology entities.*

---

### **Milestone 10 — Multi-tenant hardening (acceptance):**

* ✅ Ontology HPK in Cosmos (`/tenant,/package,/channel`)
* ✅ Schema/context loaders accept scope and cache per HPK
* ☐ AI Search index includes `tenant` (± `package`,`channel`); queries enforce `tenant`
* ☐ Gremlin upserts and traversals tenant‑guarded (ID prefix or `.has('tenant')`)
* ☐ MCP tool schemas/handlers include and enforce scope
* ☐ Operator Console has tenant selector; all pages pass scope
* ☐ CLI verbs accept `--tenant [--package --channel]`
* ☐ Isolation tests verify no cross‑tenant leakage

---

### **Milestone 11 — Observability & Governance**

* Full OTEL traces/metrics/logs into App Insights.
* Governance rules (ontology constraints, SHACL-like validators).
* Replay UI for DLQ events.
* ✅ *Acceptance: Violations logged, dashboards show latency/error metrics.*

---

### **Milestone 12 — Packaging & 1.0 Release**

* Containers published for MCP + Blazor console.
* GitHub Actions CI/CD: build, test, publish, deploy to ACA.
* Docs: `Architecture.md`, `Ontology.md`, `Roadmap.md`.
* ✅ *Acceptance: End-to-end scenario works: task → MCP → vector + KG retrieval → tool chain → effect committed → observable in UI.*

---

## Acceptance Gates

* **Alpha (Milestones 1–3):**
  Stdio server with persistence + embeddings + vector search.

* **Beta (Milestones 4–8):**
  Ontology, KG integration, planner, HTTP transport, ingestion.

* **1.0 (Milestones 9–11):**
  Operator UI, observability, governance, packaging.

---

Would you like me to also go back and **update `LimboDancer.MCP.md`** (the design map) so it stays consistent with this updated Roadmap?
