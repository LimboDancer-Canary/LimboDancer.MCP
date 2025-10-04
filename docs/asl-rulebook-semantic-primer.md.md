

# **ASL Rulebook Semantic Primer**

*How we use Vectors, Embeddings, RDF, and SHACL to make the Rulebook searchable in MCP*

---

## 1. **Why Vectors & Embeddings for ASL?**

The ASL Rulebook is complex, highly cross-referenced, and often uses jargon.

* A **vector** is a mathematical array (a long list of numbers, e.g., length 3072) that encodes the meaning of a text span.
* An **embedding** is the vector **plus its metadata**: what chunk of the rulebook it came from, what model produced it, what tenant it belongs to.

👉 In practice:

* Rule “A4.3 Residual Firepower” → embedding → vector stored in Azure AI Search → retrievable by semantic similarity.
* A player query *“What happens when a squad moves into residual fire?”* gets embedded too → nearest vectors correspond to A4.3, even if words differ.

---

## 2. **Chunking the Rulebook**

We don’t embed the entire rulebook at once. Instead:

* Each **section** or **paragraph** becomes a **Document** node.
* Example:

  * Document URI: `asl-rulebook://A4.3`
  * Content: “Residual Firepower is halved when applied against a subsequent moving unit in the same phase...”

This becomes the **anchor** for its embedding.

---

## 3. **Ontology Entities (ASL Rulebook)**

### **Document (Rule/Section/Paragraph)**

Represents a chunk of the ASL Rulebook.

* `ex:Document` node
* Properties:

  * `ex:id` → unique identifier (e.g., `A4.3`)
  * `ex:uri` → canonical reference (`asl-rulebook://A4.3`)
  * `ex:tenantId` → ensures this belongs to the ASL tenant
  * `ex:contentType` = `"rule-chunk/text"`

### **VectorEmbedding (Rule Chunk Embedding)**

Represents the embedding of a Document chunk.

* `ex:VectorEmbedding` node
* Properties:

  * `ex:embeddingOf` → link to the Document
  * `ex:embeddingModel` → `"text-embedding-3-large"`
  * `ex:dimensions` → `3072`
  * `ex:vectorStoreKey` → storage key in Azure AI Search

---

## 4. **RDF Projections for ASL Data**

### Example: Rule **A4.3 Residual Firepower**

```turtle
PREFIX ex: <https://limbodancer.ai/ontology#>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>

# Document node (the rule itself)
<https://limbodancer.ai/ontology#document/A4.3>
    rdf:type ex:Document ;
    ex:id "A4.3" ;
    ex:tenantId "asl-rulebook" ;
    ex:uri "asl-rulebook://A4.3" ;
    ex:contentType "rule-chunk/text" ;
    ex:createdAt "2025-10-01T15:00:00Z"^^xsd:dateTime .

# Embedding node (semantic representation)
<https://limbodancer.ai/ontology#embedding/A4.3>
    rdf:type ex:VectorEmbedding ;
    ex:id "emb_A4.3" ;
    ex:tenantId "asl-rulebook" ;
    ex:createdAt "2025-10-01T15:00:00Z"^^xsd:dateTime ;
    ex:dimensions 3072^^xsd:integer ;
    ex:embeddingModel "text-embedding-3-large" ;
    ex:vectorStoreKey "ai-search:asl-rulebook:A4.3" ;
    ex:embeddingOf <https://limbodancer.ai/ontology#document/A4.3> .
```

---

## 5. **SHACL Shapes for ASL Rulebook Data**

Validation ensures integrity:

### **Document Shape**

```turtle
ex:DocumentShape a sh:NodeShape ;
  sh:targetClass ex:Document ;
  sh:property ex:IdProp, ex:TenantIdProp, ex:CreatedAtProp ;
  sh:property [ sh:path ex:uri ; sh:datatype xsd:string ; sh:minCount 1 ; sh:maxCount 1 ] .
```

### **VectorEmbedding Shape**

```turtle
ex:VectorEmbeddingShape a sh:NodeShape ;
  sh:targetClass ex:VectorEmbedding ;
  sh:property ex:IdProp, ex:TenantIdProp, ex:CreatedAtProp ;
  sh:property [ sh:path ex:dimensions ; sh:datatype xsd:integer ; sh:minCount 1 ] ;
  sh:property [ sh:path ex:embeddingModel ; sh:datatype xsd:string ; sh:minCount 1 ] ;
  sh:property [ sh:path ex:embeddingOf ; sh:nodeKind sh:IRI ; sh:class ex:Document ; sh:minCount 1 ; sh:maxCount 1 ] .
```

This enforces:

* Every embedding belongs to one document.
* Dimensions/model must be present.
* Tenant IDs match.

---

## 6. **Validation Workflow (“Running SHACL”)**

1. **Projection:** Convert incoming ASL data into RDF/Turtle using `RdfProjection.ToTurtle(documentDto)` + `ToTurtle(vectorEmbeddingDto)`.
2. **Validation:** Use `dotNetRDF.Shacl` to run SHACL shapes (`docs/ontology/shapes/*.ttl`) against the RDF data.
3. **Report:**

   * If **conforms** → persist embedding + vector.
   * If **violations** → reject write, return validation report (e.g., “missing tenantId on Document A4.3”).

---

## 7. **End-to-End Flow (ASL Query Example)**

1. User asks: *“What happens if a squad moves into residual fire?”*
2. Query is embedded → vector.
3. Vector search (Azure AI Search) finds nearest embedding nodes.

   * Top match: `emb_A4.3` → Document `A4.3 Residual Firepower`.
4. SHACL ensures the embedding is valid and properly linked.
5. Response pipeline fetches the rule text from the Document node and returns it to the user.

---

## 8. **Why This Matters for ASL**

* **Semantic search**: players can ask in their own words.
* **Cross-tenant safety**: ASL embeddings never leak into other tenants (SHACL constraint).
* **Auditability**: every vector has provenance (which rule, which model, when created).
* **Future extensibility**: we can add images, diagrams, or rule errata chunks as new Document + Embedding pairs.

---

✅ **Summary**:
In the ASL Rulebook project, **a vector is the math representation of a rule chunk**, and **an embedding is that vector with metadata binding it to the rule, model, and tenant**.
We use **RDF projections** to serialize rulebook data, and **SHACL** to enforce contracts: every embedding links cleanly to its rule and tenant.
This gives us a robust foundation for **semantic ASL rulebook search and retrieval** inside the LimboDancer MCP platform.

---


## Appendix A — Reference Source Code (ASL Rulebook Semantic Stack)

This appendix provides drop-in, production-ready C# files and usage examples that implement:

1. **RDF Projection** helpers that serialize ASL Rulebook entities (rules/sections/paragraphs as `Document`, chat `Message`, `Session`, and `VectorEmbedding`) into SHACL-ready **Turtle**.
2. A **SHACL Validator** wrapper (using `dotNetRDF.Shacl`) to validate those triples against your shapes.

> All comments are written with the **ASL Rulebook** as the working example.  
> File paths are suggestions that align with the repo structure used in this project.

---

### A.1 `src/LimboDancer.MCP.Core/Rdf/RdfProjection.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LimboDancer.MCP.Core.Rdf
{
    /// <summary>
    /// ASL Rulebook context:
    /// This utility emits minimal, SHACL-ready Turtle for the domain entities we care about:
    ///   - Document: a rulebook "chunk" (e.g., A4.3 Residual Firepower)
    ///   - Message: a chat/message entity inside a Session (optional for rulebook UIs)
    ///   - Session: a conversation or retrieval run
    ///   - VectorEmbedding: metadata describing a stored vector for a rule chunk
    ///
    /// Why emit Turtle?
    ///   - We validate consistency with SHACL before writes (e.g., every embedding must link to a Document).
    ///   - We keep a canonical RDF projection that matches the ontology predicates used in SHACL.
    ///
    /// Notes:
    ///   - We intentionally keep this simple (string builder) to avoid over-engineering.
    ///   - Every emitted literal is properly typed/escaped for SHACL checks (dateTimes, integers, strings).
    ///   - The "ex:" namespace MUST match the predicates used by your SHACL shapes files.
    /// </summary>
    public static class RdfProjection
    {
        // --- Ontology / Prefixes (must align with SHACL files) ----------------
        public const string ExNs  = "https://limbodancer.ai/ontology#";          // Your project IRI space
        public const string XsdNs = "http://www.w3.org/2001/XMLSchema#";
        public const string RdfNs = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

        // ---------------------------------------------------------------------
        // Public API: one method per domain entity → returns a single Turtle doc
        // ---------------------------------------------------------------------

        /// <summary>
        /// Project an ASL Rulebook "Session" (e.g., a semantic lookup session).
        /// The minimal fields enforced by SHACL: id, tenantId, timestamps.
        /// We also include a link to the Principal (who started it) so cross-node validation can succeed.
        /// </summary>
        public static string ToTurtle(SessionDto s)
        {
            var t = new TurtleDoc();
            t.Prefix("ex", ExNs).Prefix("xsd", XsdNs).Prefix("rdf", RdfNs);

            // Subject IRI for this Session (ex:session/{id})
            var subj = Iri(Ex("session/") + s.Id);

            // rdf:type triple: declare node type
            t.TripleA(subj, "rdf:type", "ex:Session");

            // Basic required fields (strings and dateTimes)
            t.Lit(subj, "ex:id", s.Id);
            t.Lit(subj, "ex:tenantId", s.TenantId);
            t.DateTime(subj, "ex:createdAt", s.CreatedAtUtc);
            if (s.UpdatedAtUtc.HasValue) t.DateTime(subj, "ex:updatedAt", s.UpdatedAtUtc.Value);

            // Optional: startedBy principal (useful for audit & multi-tenant checks)
            if (!string.IsNullOrWhiteSpace(s.StartedByPrincipalId))
            {
                var principalIri = Iri(Ex("principal/") + s.StartedByPrincipalId);
                t.Iri(subj, "ex:startedBy", principalIri);

                // Minimal "declare" of the principal to help SHACL join across nodes during validation
                t.TripleA(principalIri, "rdf:type", "ex:Principal");
                t.Lit(principalIri, "ex:id", s.StartedByPrincipalId);
                t.Lit(principalIri, "ex:tenantId", s.TenantId);
            }

            return t.Build();
        }

        /// <summary>
        /// Project an ASL Rulebook "Document" = one chunk of the rulebook.
        /// Example: A4.3 Residual Firepower with canonical URI "asl-rulebook://A4.3".
        /// SHACL requires: id, tenantId, createdAt (and typically a URI).
        /// </summary>
        public static string ToTurtle(DocumentDto d)
        {
            var t = new TurtleDoc();
            t.Prefix("ex", ExNs).Prefix("xsd", XsdNs).Prefix("rdf", RdfNs);

            // Subject IRI for this Document (ex:document/{id})
            var subj = Iri(Ex("document/") + d.Id);

            t.TripleA(subj, "rdf:type", "ex:Document");
            t.Lit(subj, "ex:id", d.Id);
            t.Lit(subj, "ex:tenantId", d.TenantId);
            t.DateTime(subj, "ex:createdAt", d.CreatedAtUtc);
            if (d.UpdatedAtUtc.HasValue) t.DateTime(subj, "ex:updatedAt", d.UpdatedAtUtc.Value);

            // Canonical reference to this rule chunk (e.g., "asl-rulebook://A4.3")
            t.Lit(subj, "ex:uri", d.Uri);

            // Useful for downstream filters (optional)
            if (!string.IsNullOrWhiteSpace(d.ContentType))
                t.Lit(subj, "ex:contentType", d.ContentType);

            return t.Build();
        }

        /// <summary>
        /// Project a "Message" in a Session—handy for chat-style UIs where users
        /// query the ASL Rulebook. SHACL will enforce role, content, and the link to session.
        /// </summary>
        public static string ToTurtle(MessageDto m)
        {
            var t = new TurtleDoc();
            t.Prefix("ex", ExNs).Prefix("xsd", XsdNs).Prefix("rdf", RdfNs);

            // Subject IRI for this Message (ex:message/{id})
            var subj = Iri(Ex("message/") + m.Id);

            t.TripleA(subj, "rdf:type", "ex:Message");
            t.Lit(subj, "ex:id", m.Id);
            t.Lit(subj, "ex:tenantId", m.TenantId);
            t.DateTime(subj, "ex:createdAt", m.CreatedAtUtc);
            if (m.UpdatedAtUtc.HasValue) t.DateTime(subj, "ex:updatedAt", m.UpdatedAtUtc.Value);

            // SHACL restricts ex:role to {user, assistant, tool, system}
            t.Lit(subj, "ex:role", m.Role);

            // Keep original user/assistant content as a string literal
            t.Lit(subj, "ex:content", m.Content);

            // Required: link the Message to its parent Session
            var sessionIri = Iri(Ex("session/") + m.SessionId);
            t.Iri(subj, "ex:session", sessionIri);

            // Optional: the message can reference one or more ASL rule Documents
            if (m.ReferencedDocumentIds is { Count: > 0 })
            {
                foreach (var docId in m.ReferencedDocumentIds.Distinct())
                {
                    var docIri = Iri(Ex("document/") + docId);
                    t.Iri(subj, "ex:referencesDoc", docIri);
                }
            }

            return t.Build();
        }

        /// <summary>
        /// Project a "VectorEmbedding" for an ASL rule Document.
        /// This does not store the raw vector values; it stores the metadata needed to:
        ///  - Validate (SHACL): dimensions, model, tenant, link to Document (embeddingOf)
        ///  - Retrieve from your vector store (vectorStoreKey)
        /// Typical flow:
        ///  1) Chunk rule text (e.g., A4.3) → compute embedding via model
        ///  2) Upsert vector to Azure AI Search (use a key you control)
        ///  3) Emit this metadata node + validate with SHACL → then persist
        /// </summary>
        public static string ToTurtle(VectorEmbeddingDto e)
        {
            var t = new TurtleDoc();
            t.Prefix("ex", ExNs).Prefix("xsd", XsdNs).Prefix("rdf", RdfNs);

            // Subject IRI for this Embedding (ex:embedding/{id})
            var subj = Iri(Ex("embedding/") + e.Id);

            t.TripleA(subj, "rdf:type", "ex:VectorEmbedding");
            t.Lit(subj, "ex:id", e.Id);
            t.Lit(subj, "ex:tenantId", e.TenantId);
            t.DateTime(subj, "ex:createdAt", e.CreatedAtUtc);
            if (e.UpdatedAtUtc.HasValue) t.DateTime(subj, "ex:updatedAt", e.UpdatedAtUtc.Value);

            // Embedding metadata required by SHACL
            t.Int(subj, "ex:dimensions", e.Dimensions);
            t.Lit(subj, "ex:embeddingModel", e.EmbeddingModel);
            t.Lit(subj, "ex:vectorStoreKey", e.VectorStoreKey);

            // Required: link the embedding to the ASL Document (rule chunk) it represents
            var docIri = Iri(Ex("document/") + e.DocumentId);
            t.Iri(subj, "ex:embeddingOf", docIri);

            return t.Build();
        }

        // ---------------------------------------------------------------------
        // DTOs you can map from your domain models (ASL Rulebook context)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Represents a semantic lookup / chat session around ASL Rulebook data.
        /// </summary>
        public sealed class SessionDto
        {
            public string Id { get; init; } = default!;
            public string TenantId { get; init; } = default!;
            public DateTime CreatedAtUtc { get; init; }
            public DateTime? UpdatedAtUtc { get; init; }
            public string StartedByPrincipalId { get; init; } = default!;
        }

        /// <summary>
        /// Represents one chunk of the ASL Rulebook (e.g., a section, paragraph, or rule like A4.3).
        /// </summary>
        public sealed class DocumentDto
        {
            public string Id { get; init; } = default!;
            public string TenantId { get; init; } = default!;
            public DateTime CreatedAtUtc { get; init; }
            public DateTime? UpdatedAtUtc { get; init; }

            /// <summary>
            /// Canonical URI for this rule chunk, e.g., "asl-rulebook://A4.3".
            /// </summary>
            public string Uri { get; init; } = default!;

            /// <summary>
            /// Useful for filtering; for rulebook text use "rule-chunk/text".
            /// </summary>
            public string? ContentType { get; init; }
        }

        /// <summary>
        /// Represents a chat message that may reference ASL Rulebook chunks.
        /// </summary>
        public sealed class MessageDto
        {
            public string Id { get; init; } = default!;
            public string TenantId { get; init; } = default!;
            public DateTime CreatedAtUtc { get; init; }
            public DateTime? UpdatedAtUtc { get; init; }

            /// <summary>
            /// "user" | "assistant" | "tool" | "system"
            /// </summary>
            public string Role { get; init; } = default!;

            /// <summary>
            /// Original message text.
            /// </summary>
            public string Content { get; init; } = default!;

            /// <summary>
            /// The owning Session id.
            /// </summary>
            public string SessionId { get; init; } = default!;

            /// <summary>
            /// Optional list of ASL Document ids referenced by this message (e.g., ["A4.3"]).
            /// </summary>
            public List<string>? ReferencedDocumentIds { get; init; }
        }

        /// <summary>
        /// Represents the metadata for a stored vector that encodes the semantics of an ASL rule chunk.
        /// </summary>
        public sealed class VectorEmbeddingDto
        {
            public string Id { get; init; } = default!;
            public string TenantId { get; init; } = default!;
            public DateTime CreatedAtUtc { get; init; }
            public DateTime? UpdatedAtUtc { get; init; }

            /// <summary>
            /// Number of dimensions produced by the embedding model (e.g., 3072).
            /// </summary>
            public int Dimensions { get; init; }

            /// <summary>
            /// Embedding model identifier (e.g., "text-embedding-3-large").
            /// </summary>
            public string EmbeddingModel { get; init; } = default!;

            /// <summary>
            /// Your pointer into the vector store (Azure AI Search doc key, etc.).
            /// </summary>
            public string VectorStoreKey { get; init; } = default!;

            /// <summary>
            /// The ASL Document id this embedding represents (e.g., "A4.3").
            /// </summary>
            public string DocumentId { get; init; } = default!;
        }

        // ---------------------------------------------------------------------
        // Internal helpers for Turtle emission
        // ---------------------------------------------------------------------

        private static string Ex(string local) => ExNs + local;

        /// <summary>
        /// Wrap an absolute IRI in angle brackets for Turtle.
        /// </summary>
        private static string Iri(string absolute) => $"<{absolute}>";

        /// <summary>
        /// Minimal string escaping for Turtle literals.
        /// </summary>
        private static string EscapeString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        /// <summary>
        /// Format a UTC DateTime as xsd:dateTime (ISO-8601 with 'Z').
        /// </summary>
        private static string XsdDateTime(DateTime utc)
        {
            var normalized = utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime();
            return normalized.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Lightweight Turtle document builder.
        /// </summary>
        private sealed class TurtleDoc
        {
            private readonly StringBuilder _sb = new();

            public TurtleDoc Prefix(string pfx, string iri)
            {
                _sb.Append("PREFIX ").Append(pfx).Append(": <").Append(iri).AppendLine(">");
                return this;
            }

            /// <summary>
            /// Triple with a QName object (e.g., rdf:type ex:Document).
            /// </summary>
            public void TripleA(string subjIri, string predQname, string objQname)
            {
                _sb.Append(subjIri).Append(' ').Append(predQname)
                   .Append(' ').Append(objQname).AppendLine(" .");
            }

            /// <summary>
            /// Triple with an IRI object.
            /// </summary>
            public void Iri(string subjIri, string predQname, string objIri)
            {
                _sb.Append(subjIri).Append(' ').Append(predQname)
                   .Append(' ').Append(objIri).AppendLine(" .");
            }

            /// <summary>
            /// Triple with a string literal (untyped).
            /// </summary>
            public void Lit(string subjIri, string predQname, string value)
            {
                _sb.Append(subjIri).Append(' ').Append(predQname)
                   .Append(" \"").Append(EscapeString(value)).AppendLine("\" .");
            }

            /// <summary>
            /// Triple with a typed integer literal.
            /// </summary>
            public void Int(string subjIri, string predQname, int value)
            {
                _sb.Append(subjIri).Append(' ').Append(predQname)
                   .Append(' ').Append(value.ToString(CultureInfo.InvariantCulture))
                   .Append("^^xsd:integer").AppendLine(" .");
            }

            /// <summary>
            /// Triple with a typed xsd:dateTime literal.
            /// </summary>
            public void DateTime(string subjIri, string predQname, DateTime utc)
            {
                _sb.Append(subjIri).Append(' ').Append(predQname)
                   .Append(" \"").Append(XsdDateTime(utc)).Append("\"^^xsd:dateTime").AppendLine(" .");
            }

            /// <summary>
            /// Finalize and return the Turtle document.
            /// </summary>
            public string Build() => _sb.ToString();
        }
    }
}
````

---

### A.2 `src/LimboDancer.MCP.Core/Validation/ShaclValidator.cs`

```csharp
using System.IO;
using System.Linq;
using System.Collections.Generic;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Shacl;
using VDS.RDF.Shacl.Validation;

namespace LimboDancer.MCP.Core.Validation
{
    /// <summary>
    /// ASL Rulebook context:
    /// "Running SHACL" means validating a data graph (our Turtle for Documents/Embeddings)
    /// against a shapes graph (our SHACL .ttl files). The outcome is a conformance report.
    ///
    /// We wrap dotNetRDF.Shacl here so pipeline code can stay simple:
    ///   - Prepare Turtle for a pending write (e.g., Document A4.3 + its Embedding)
    ///   - Call ValidateTurtle(data, shapesDirectory)
    ///   - If report.Conforms == false → reject write with a 400 and include violations
    ///
    /// Typical usage in the ASL rulebook pipeline:
    ///   1) Build RDF for Document A4.3 and its VectorEmbedding node
    ///   2) Validate with SHACL (checks tenantId, embeddingOf link, model/dimensions)
    ///   3) Only then persist the metadata and the vector in your stores
    /// </summary>
    public interface IShaclValidator
    {
        /// <summary>
        /// Validate a data graph (as a single Turtle string) against one or more SHACL shape files.
        /// Returns: (Conforms, ReportTurtle, Results)
        ///   - Conforms: overall pass/fail
        ///   - ReportTurtle: serialized SHACL validation report (handy for logs)
        ///   - Results: individual constraint violations (path, message, source shape)
        /// </summary>
        (bool Conforms, string ReportTurtle, IReadOnlyList<Result> Results)
            ValidateTurtle(string dataTurtle, IEnumerable<string> shapeFilePaths);
    }

    public sealed class ShaclValidator : IShaclValidator
    {
        public (bool Conforms, string ReportTurtle, IReadOnlyList<Result> Results)
            ValidateTurtle(string dataTurtle, IEnumerable<string> shapeFilePaths)
        {
            // 1) Load the RDF data graph from a Turtle string
            IGraph data = new Graph();
            new TurtleParser().Load(data, new StringReader(dataTurtle));

            // 2) Load and merge all SHACL shapes (multiple .ttl files allowed)
            IGraph shapesGraph = new Graph();
            foreach (var path in shapeFilePaths)
            {
                var g = new Graph();
                FileLoader.Load(g, path); // auto-detects by extension; TTL expected
                shapesGraph.Merge(g, true);
            }

            // 3) Run SHACL validation: core step
            var shapes = new ShapesGraph(shapesGraph);
            Report report = shapes.Validate(data);

            // 4) Serialize the report (Turtle) for diagnostics and observability
            var sw = new StringWriter();
            var ttlWriter = new CompressingTurtleWriter();
            ttlWriter.Save(report.ReportGraph, sw);

            return (report.Conforms, sw.ToString(), report.Results.ToList());
        }
    }
}
```

---

### A.3 Usage Snippets (ASL Rulebook Examples)

> These short examples show how to project **ASL rulebook data** to RDF and **validate with SHACL** before persisting.

```csharp
using System;
using System.IO;
using System.Linq;
using LimboDancer.MCP.Core.Rdf;
using LimboDancer.MCP.Core.Validation;

// 1) Build an ASL Document (rule chunk) for A4.3 Residual Firepower
var doc = new RdfProjection.DocumentDto
{
    Id = "A4.3",
    TenantId = "asl-rulebook",
    CreatedAtUtc = DateTime.UtcNow,
    Uri = "asl-rulebook://A4.3",
    ContentType = "rule-chunk/text"
};

string docTtl = RdfProjection.ToTurtle(doc);

// 2) Build its VectorEmbedding metadata (vector lives in Azure AI Search)
var emb = new RdfProjection.VectorEmbeddingDto
{
    Id = "emb_A4.3",
    TenantId = "asl-rulebook",
    CreatedAtUtc = DateTime.UtcNow,
    Dimensions = 3072,
    EmbeddingModel = "text-embedding-3-large",
    VectorStoreKey = "ai-search:asl-rulebook:A4.3", // your index key
    DocumentId = "A4.3"
};

string embTtl = RdfProjection.ToTurtle(emb);

// 3) Combine both Turtle docs into a single validation batch (simple concat)
string dataTurtle = docTtl + Environment.NewLine + embTtl;

// 4) Collect shape files (the SHACL shapes you placed in docs/ontology/shapes/)
var shapesDir = "docs/ontology/shapes";
var shapeFiles = Directory.GetFiles(shapesDir, "*.ttl", SearchOption.TopDirectoryOnly);

// 5) Run SHACL validation
var (conforms, reportTurtle, results) = new ShaclValidator().ValidateTurtle(dataTurtle, shapeFiles);

// 6) Gate persistence on SHACL conformance
if (!conforms)
{
    // Example: project violations to a consistent 400 response
    var errors = results.Select(r => new
    {
        Focus = r.FocusNode?.ToString(),
        Message = r.Message,
        Severity = r.Severity?.ToString(),
        SourceShape = r.SourceShape?.ToString(),
        Constraint = r.SourceConstraintComponent?.ToString()
    });

    // Log reportTurtle for forensics; return errors to caller
    // throw new ValidationException("SHACL validation failed", errors);
}
else
{
    // Proceed:
    // - Upsert vector to Azure AI Search using VectorStoreKey
    // - Persist Document + Embedding metadata to your stores
}
```

---

