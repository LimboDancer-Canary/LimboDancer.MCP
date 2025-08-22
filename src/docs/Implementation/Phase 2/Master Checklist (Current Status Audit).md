# Master Checklist

Repository: LimboDancer.MCP

Legend:
* ✅ = Completed
* ⚠️ = Partially implemented
* ❌ = Not implemented
* (—) = De-scoped

---

## Section A. Compile / Structural Blockers ✅ ALL COMPLETE

* **C1.** GremlinClientFactory implements `IGremlinClientFactory` ✅
* **C2.** Standardize `GremlinOptions` property names ✅
* **C3.** Use consistent GraphSON2 serializer ✅
* **C4.** Fix `GraphStore` constructor with `ILoggerFactory` ✅
* **C5.** GraphStore uses `ITenantAccessor` ✅
* **C6.** Vector.AzureSearch unified on MemoryDoc ✅
* **C7.** Blazor Program.cs IPropertyKeyMapper resolved ✅
* **C8.** OntologyValidationService registration unified ✅
* **C9.** Preconditions helper FirstOrDefault ✅
* **C10.** GraphWriteHelpers tenant utilities ✅
* **C11.** Ensure all .csproj target net9.0 ✅
* **C12.** Remove duplicate using directives ✅
* **C13.** Npgsql package reference in Storage ✅
* **C14.** Tenancy services properly registered ✅

---

## Section B. API / Runtime Surface ✅ ALL COMPLETE

* **API1.** Implement `POST /api/ontology/validate` ✅
* **API2.** Implement `GET /api/ontology/export?format=(jsonld|turtle|ttl)` ✅
* **API3.** Blazor Chat endpoints ✅
* **API4.** Authorization configured ✅
* **API5.** Canonical tenant headers documented ✅
* **API6.** Health/ready endpoints with persistence check ✅
* **API7.** AmbientTenantAccessor defaults to TenancyOptions ✅

---

## Section C. Tools & Server Feature Set ✅ ALL COMPLETE

* **T1.** Implement `HistoryGetTool` ✅
* **T2.** Implement `MemorySearchTool` ✅
* **T3.** Register implemented tools ✅ (4/4 registered)
* **T4.** Error handling & logging in tools ✅
* **T5.** Align vector index naming ✅

---

## Section D. Vector / Search Layer ✅ ALL COMPLETE

* **V1.** Index schema matches MemoryDoc ✅
* **V2.** Vector dimension constant alignment ✅
* **V3.** Profile/field names consistent ✅
* **V4.** Document final index name ✅

---

## Section E. Graph / Cosmos Gremlin Layer

* **G1.** Username formatting `/dbs/{db}/colls/{graph}` ✅
* **G2.** GremlinOptions complete fields ✅
* **G3.** Add Validate() method ✅
* **G4.** Parse/TryParseConnectionString ✅
* **G5.** Redacted ToString() ✅
* **G6.** Tenant guard traversal tests (deferred)
* **G7.** DI extension AddCosmosGremlinGraph ✅

---

## Section F. Ontology & Repository Layer ✅ ALL COMPLETE

* **O1.** CosmosOntologyRepository implementations ✅
* **O2.** OntologyValidationService consolidation ✅
* **O3.** Ontology endpoints error handling ✅

---

## Section G. CLI & Blazor Console Integration ✅ ALL COMPLETE

* **CL1.** CLI wiring for new GraphStore ✅
* **CL2.** CLI tenant option ✅
* **CL3.** TenantHeaderHandler sets header ✅
* **CL4.** Remove dead Chat UI ✅ (kept as intended)
* **CL5.** Ontology validation UI to active endpoint ✅
* **CL6.** Document required env/config ✅

---

## Section H. Documentation / Consistency

* **D1.** Update README/docs with finalized options ❌
* **D2.** Remove divergent code snippets ❌
* **D3.** CONTRIBUTING note for checklist ❌
* **D4.** Sample appsettings ❌
* **D5.** Document canonical HTTP headers ❌
* **D6.** .NET 9 rationale note ❌

---

## Section I. Testing & Validation

* **TST1.** GremlinOptions tests ❌
* **TST2.** GremlinClientFactory integration ❌
* **TST3.** Vector round-trip test ❌
* **TST4.** Ontology validation endpoint test ❌
* **TST5.** Tenancy header propagation test ❌
* **TST6.** Tool discovery test ❌

---

## Section J. Cleanup / Polish

* **P1.** Remove obsolete TODOs ❌
* **P2.** Consistent logging categories ⚠️
* **P3.** Secrets not logged ✅
* **P4.** CancellationToken usage ⚠️
* **P5.** Guard clauses in public APIs ✅

---

## Progress Summary

Sections Complete: A ✅, B ✅
Sections In Progress: C, D, E, F, G
Sections Not Started: H, I
Polish Items: J (partial)