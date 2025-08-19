
# LimboDancer.MCP — Documentation Index

This folder contains the design and architecture documentation for **LimboDancer.MCP**.
Use the diagrams below to orient yourself, then drill into specific documents.

---

## 🗺 Overview Map

```mermaid
flowchart TD
  %% Core Design
  subgraph CORE [Core Design]
    ARCH[Architecture]
    DMAP[Design Map]
    RMAP[Roadmap]
  end

  %% Ontology
  subgraph ONTO [Ontology]
    OREF[Ontology Reference]
    OAAI[Ontology and Agentic AI]
    O0["0_Ontology Implementation in CSharp"]
  end

  %% Data Layer
  subgraph DATA [Persistence, Index & Graph]
    P1["1_Persistence baseline"]
    V2["2_Vector index (Azure AI Search)"]
    G3["3_Cosmos Gremlin scaffold"]
    T4["4_MCP tool surface"]
  end

  %% Runtime
  subgraph RUNTIME [Planner & Transport]
    P5["5_Planner hook + preconditions"]
    H6["6_HTTP transport + SSE"]
  end

  %% Operator Console & Dev
  subgraph OPS [Operator Console & Dev]
    U7["7_Operator Console scaffold"]
    C8["8_Dev bootstrap + CLI"]
    D9["9_Full Operator Console"]
  end

  %% Connections
  ARCH --> OREF
  ARCH --> P1
  ARCH --> V2
  ARCH --> G3
  ARCH --> H6

  DMAP --> P5
  DMAP --> T4

  O0 --> T4

  T4 --> P1
  T4 --> V2
  T4 --> G3

  H6 --> U7
  U7 --> D9

  C8 --> T4
  C8 --> U7
````

---

## 📂 Documentation by Cluster

### 📐 Core Design

* **ARCH — [Architecture](./LimboDancer.MCP%20—%20Architecture.md)**
  Overall system architecture and components.
* **DMAP — [Design Map](./LimboDancer.MCP%20—%20Design%20Map.md)**
  Visual and conceptual design map.
* **RMAP — [Roadmap](./LimboDancer.MCP%20—%20Roadmap.md)**
  Milestones and planned feature sequence.

### 🧩 Ontology

* **OREF — [Ontology Reference](./LimboDancer%20Ontology%20Reference.md)**
  Reference model for ontology terms and usage.
* **OAAI — [Ontology and Agentic AI](./Ontology%20and%20Agentic%20AI%20in%20LimboDancer.MCP.md)**
  How ontology underpins agentic AI behavior.
* **O0 — [\_0. Ontology Implementation in CSharp](./_0.%20Ontology%20Implementation%20in%20CSharp.md)**
  Implementation details and migration from C# to Cosmos.

### 🗄 Persistence, Index & Graph

* **P1 — [\_1. Persistence baseline (EF Core - Postgres)](./_1.%20Persistence%20baseline%20%28EF%20Core%20-%20Postgres%29.md)**
  Database baseline using EF Core + PostgreSQL.
* **V2 — [\_2. Vector index (Azure AI Search)](./_2.%20Vector%20index%20for%20Azure%20AI%20Search%20%28hybrid%29.md)**
  Hybrid vector index for retrieval.
* **G3 — [\_3. Cosmos Gremlin graph scaffold](./_3.%20Cosmos%20Gremlin%20graph%20scaffold.md)**
  Graph database scaffold for relationships.
* **T4 — [\_4. MCP tool surface](./_4.%20MCP%20tool%20surface%20for%20historymemorygraph%20%28first%20useful%20tools%29.md)**
  Tool APIs for history/memory/graph access.

### 🔀 Planner & Transport

* **P5 — [\_5. Planner hook (typed ReAct thin slice)](./_5.%20Planner%20hook%20%20precondition%20gate%20%28typed%20ReAct%20“thin%20slice”%29.md)**
  Planner hooks and preconditions for reasoning.
* **H6 — [\_6. HTTP transport hardening + SSE](./_6.%20HTTP%20transport%20hardening%20%20SSE%20events.md)**
  Transport security and Server-Sent Events.

### 🖥 Operator Console & Dev Experience

* **U7 — [\_7. Operator Console scaffold (Blazor Server)](./_7.%20Operator%20Console%20scaffold%20%28Blazor%20Server%29.md)**
  Initial Blazor console pages and scaffolding.
* **C8 — [\_8. Dev bootstrap + CLI](./_8.%20Dev%20bootstrap%20%20CLI.md)**
  Developer CLI and setup flow.
* **D9 — [\_9. Full Operator Console](./_9.%20Full%20Operator%20Console%20%28interactivedebugging%29.md)**
  Expanded console with interactive debugging.

---

## 🔢 Build-out Sequence

This diagram illustrates the **recommended implementation order** for MCP subsystems.

```mermaid
sequenceDiagram
  participant D as Design
  participant O as Ontology
  participant P as Persistence
  participant V as Vector
  participant G as Graph
  participant T as MCP Tools
  participant H as HTTP/SSE
  participant UI as Operator Console
  participant CLI as Dev CLI

  D->>O: Define ontology docs
  O->>P: Map to EF Core baseline
  O->>V: Define vector fields and filters
  O->>G: Define vertex and edge labels
  P->>T: History storage ready
  V->>T: Memory search ready
  G->>T: Graph read ready
  T->>H: Expose MCP tools over HTTP/stdio
  H->>UI: Read-only console pages
  CLI->>V: mem add/search
  CLI->>G: kg ping
  CLI->>P: db migrate
```

---

## ℹ️ Notes

* Labels in diagrams (ARCH, P1, etc.) map directly to the clickable docs above.
* Mermaid diagrams use plain labels to avoid GitHub Markdown parsing errors.
* Sequence diagrams emphasize build order, while the flowchart shows structural dependencies.


