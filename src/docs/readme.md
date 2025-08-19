# LimboDancer.MCP — Documentation Index

This folder contains the design and architecture documentation for **LimboDancer.MCP**.
Files are grouped into categories for easier navigation.

---

## 🗺 Mermaid: Documentation Map

```mermaid
flowchart TD
  %% Core Design
  subgraph CORE[Core Design]
    ARCH[Architecture]
    DMAP[Design Map]
    RMAP[Roadmap]
  end

  %% Ontology
  subgraph ONTO[Ontology]
    OREF[Ontology Reference]
    OAAI[Ontology and Agentic AI]
    O0["0. Ontology Implementation in CSharp"]
  end

  %% Data Layer
  subgraph DATA[Persistence, Index & Graph]
    P1["1. Persistence baseline"]
    V2["2. Vector index (Azure AI Search)"]
    G3["3. Cosmos Gremlin scaffold"]
    T4["4. MCP tool surface"]
  end

  %% Runtime
  subgraph RUNTIME[Planner & Transport]
    P5["5. Planner hook + preconditions"]
    H6["6. HTTP transport + SSE"]
  end

  %% Operator Console & Dev
  subgraph OPS[Operator Console & Dev]
    U7["7. Operator Console scaffold"]
    C8["8. Dev bootstrap + CLI"]
    D9["9. Full Operator Console"]
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

## 📐 Core Design
- [LimboDancer.MCP — Architecture](./LimboDancer.MCP%20—%20Architecture.md)
- [LimboDancer.MCP — Design Map](./LimboDancer.MCP%20—%20Design%20Map.md)
- [LimboDancer.MCP — Roadmap](./LimboDancer.MCP%20—%20Roadmap.md)

---

## 🧩 Ontology
- [LimboDancer Ontology Reference](./LimboDancer%20Ontology%20Reference.md)
- [Ontology and Agentic AI in LimboDancer.MCP](./Ontology%20and%20Agentic%20AI%20in%20LimboDancer.MCP.md)
- [_0. Ontology Implementation in CSharp](./_0.%20Ontology%20Implementation%20in%20CSharp.md)

---

## 🗄 Persistence, Index & Graph
- [_1. Persistence baseline (EF Core - Postgres)](./_1.%20Persistence%20baseline%20(EF%20Core%20-%20Postgres).md)
- [_2. Vector index for Azure AI Search (hybrid)](./_2.%20Vector%20index%20for%20Azure%20AI%20Search%20(hybrid).md)
- [_3. Cosmos Gremlin graph scaffold](./_3.%20Cosmos%20Gremlin%20graph%20scaffold.md)
- [_4. MCP tool surface for history/memory/graph (first useful tools)](./_4.%20MCP%20tool%20surface%20for%20historymemorygraph%20(first%20useful%20tools).md)

---

## 🔀 Planner & Transport
- [_5. Planner hook  precondition gate (typed ReAct "thin slice")](./_5.%20Planner%20hook%20%20precondition%20gate%20(typed%20ReAct%20thin%20slice).md)
- [_6. HTTP transport hardening + SSE events](./_6.%20HTTP%20transport%20hardening%20%20SSE%20events.md)

---

## 🖥 Operator Console & Developer Experience
- [_7. Operator Console scaffold (Blazor Server)](./_7.%20Operator%20Console%20scaffold%20(Blazor%20Server).md)
- [_8. Dev bootstrap + CLI](./_8.%20Dev%20bootstrap%20%20CLI.md)
- [_9. Full Operator Console (interactive/debugging)](./_9.%20Full%20Operator%20Console%20(interactivedebugging).md)

---



---

## 🔢 Mermaid: Build-out Sequence

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

* This index reflects the current set of documentation in `src/docs`.
* Files are ordered by design sequence (`_0` through `_9`) where applicable.
* Diagrams use Mermaid and follow project conventions.

```

If you want, I can open a quick PR that adds this file to `src/docs/README.md`.
