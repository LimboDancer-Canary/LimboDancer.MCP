# Ontology C# Class
**starter `Ontology.cs`** 
you can drop into `/src/LimboDancer.MCP.Ontology/`.
It gives you:

* Stable **CURIE/URI constants** (`ldm:*`) for classes and properties.
* A **JSON-LD context** generator for wiring tool schemas.
* Typed **enums** (e.g., `MemoryKind`) aligned with your ER model.
* Minimal **precondition/effect** models for tool governance.
* Small **KG primitives** (vertex/edge labels) for Cosmos Gremlin usage.

> Uses **System.Text.Json** (your preference) and is .NET 9–friendly.

```csharp
// File: /src/LimboDancer.MCP.Ontology/Ontology.cs
// Purpose: Starter ontology surface for LimboDancer.MCP (URIs, CURIEs, JSON-LD context, typed enums, and minimal governance models).
// Notes: This is intentionally small and mechanical. Extend alongside docs/Ontology.md (Milestone 4+).

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LimboDancer.MCP.Ontology
{
    /// <summary>
    /// Canonical CURIEs and URIs for the LimboDancer ontology.
    /// Keep these constants in lockstep with docs/Ontology.md.
    /// </summary>
    public static class Ldm
    {
        public const string Prefix = "ldm";
        public const string Namespace = "https://limbodancer.ai/ontology/";

        public static class Classes
        {
            public const string Person         = $"{Prefix}:Person";
            public const string Trip           = $"{Prefix}:Trip";
            public const string Reservation    = $"{Prefix}:Reservation";
            public const string Flight         = $"{Prefix}:Flight";
            public const string PaymentMethod  = $"{Prefix}:PaymentMethod";
            public const string Session        = $"{Prefix}:Session";
            public const string Message        = $"{Prefix}:Message";
            public const string MemoryItem     = $"{Prefix}:MemoryItem";
            public const string Tool           = $"{Prefix}:Tool";
            public const string Skill          = $"{Prefix}:Skill";
            public const string State          = $"{Prefix}:State"; // e.g., Canceled, Active
        }

        public static class Properties
        {
            public const string Owns                 = $"{Prefix}:owns";            // Person -> Reservation
            public const string ForTrip              = $"{Prefix}:forTrip";         // Reservation -> Trip
            public const string FliesOn              = $"{Prefix}:fliesOn";         // Reservation -> Flight
            public const string PaidWith             = $"{Prefix}:paidWith";        // Reservation -> PaymentMethod
            public const string HasMessage           = $"{Prefix}:hasMessage";      // Session -> Message
            public const string References           = $"{Prefix}:references";      // Message -> MemoryItem
            public const string Requires             = $"{Prefix}:requires";        // Tool -> Skill
            public const string Produces             = $"{Prefix}:produces";        // Tool -> Entity changed/created
            public const string Status               = $"{Prefix}:status";          // Generic status property
            public const string Kind                 = $"{Prefix}:kind";            // MemoryItem kind
            public const string Label                = $"{Prefix}:label";           // Optional human label
        }

        /// <summary>
        /// Expand ldm:* CURIE to absolute URI.
        /// </summary>
        public static Uri Expand(string curie)
        {
            if (string.IsNullOrWhiteSpace(curie)) throw new ArgumentNullException(nameof(curie));
            if (!curie.StartsWith($"{Prefix}:", StringComparison.Ordinal)) return new Uri(curie, UriKind.Absolute);
            var local = curie[(Prefix.Length + 1)..];
            return new Uri(Namespace + local);
        }

        /// <summary>
        /// Try to compact an absolute URI back to ldm:* CURIE.
        /// Returns input if not under the ldm namespace.
        /// </summary>
        public static string Compact(Uri uri)
        {
            var s = uri?.ToString() ?? string.Empty;
            return s.StartsWith(Namespace, StringComparison.Ordinal)
                ? $"{Prefix}:{s.Substring(Namespace.Length)}"
                : s;
        }
    }

    /// <summary>
    /// JSON-LD context materialized as a JsonDocument for embedding in tool schemas or payloads.
    /// </summary>
    public static class JsonLdContext
    {
        /// <summary>
        /// Returns a canonical JSON-LD context object as a JSON string.
        /// </summary>
        public static string GetContextJson()
        {
            var ctx = new Dictionary<string, object?>
            {
                ["@context"] = new Dictionary<string, object?>
                {
                    [Ldm.Prefix] = Ldm.Namespace,

                    // Classes
                    ["Person"]        = Ldm.Classes.Person,
                    ["Trip"]          = Ldm.Classes.Trip,
                    ["Reservation"]   = Ldm.Classes.Reservation,
                    ["Flight"]        = Ldm.Classes.Flight,
                    ["PaymentMethod"] = Ldm.Classes.PaymentMethod,
                    ["Session"]       = Ldm.Classes.Session,
                    ["Message"]       = Ldm.Classes.Message,
                    ["MemoryItem"]    = Ldm.Classes.MemoryItem,
                    ["Tool"]          = Ldm.Classes.Tool,
                    ["Skill"]         = Ldm.Classes.Skill,
                    ["State"]         = Ldm.Classes.State,

                    // Properties
                    ["owns"]           = Ldm.Properties.Owns,
                    ["forTrip"]        = Ldm.Properties.ForTrip,
                    ["fliesOn"]        = Ldm.Properties.FliesOn,
                    ["paidWith"]       = Ldm.Properties.PaidWith,
                    ["hasMessage"]     = Ldm.Properties.HasMessage,
                    ["references"]     = Ldm.Properties.References,
                    ["requires"]       = Ldm.Properties.Requires,
                    ["produces"]       = Ldm.Properties.Produces,
                    ["status"]         = Ldm.Properties.Status,
                    ["kind"]           = Ldm.Properties.Kind,
                    ["label"]          = Ldm.Properties.Label
                }
            };

            return JsonSerializer.Serialize(ctx, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Create a JsonDocument ready to embed (for APIs that want a parsed instance).
        /// </summary>
        public static JsonDocument GetContextDocument()
            => JsonDocument.Parse(GetContextJson());
    }

    /// <summary>
    /// Memory kinds as used in docs/Architecture.md ER diagram and Roadmap.
    /// Serialized as strings via System.Text.Json.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MemoryKind
    {
        Vector,
        Graph,
        Reasoning
    }

    /// <summary>
    /// Minimal model for a tool schema binding to ontology. Keep this alongside your MCP tool definitions.
    /// </summary>
    public sealed class ToolSchemaBinding
    {
        public required string Name { get; init; }                       // e.g., cancelReservation
        public required Dictionary<string, ToolField> Input { get; init; } = new(); // name -> field
        public Dictionary<string, ToolField>? Output { get; init; }      // optional

        public List<ToolPrecondition> Preconditions { get; init; } = new();
        public List<ToolEffect> Effects { get; init; } = new();
        public string? JsonLdContext { get; init; } = JsonLdContext.GetContextJson();
    }

    public sealed class ToolField
    {
        /// <summary>JSON Schema "type" (string, integer, object, array, etc.).</summary>
        public required string Type { get; init; }

        /// <summary>Ontology CURIE or absolute URI (e.g., ldm:Reservation).</summary>
        public required string OntologyId { get; init; }

        /// <summary>Optional human label.</summary>
        public string? Label { get; init; }

        /// <summary>Optional "required" flag (defaults handled by the tool schema itself).</summary>
        public bool? Required { get; init; }
    }

    /// <summary>
    /// A simple, explicit precondition that can be evaluated against the KG.
    /// For richer logic, extend with expression trees or query templates.
    /// </summary>
    public sealed class ToolPrecondition
    {
        /// <summary>Ontology class or property this precondition refers to (CURIE or absolute URI).</summary>
        public required string Subject { get; init; } // e.g., ldm:Reservation

        /// <summary>Predicate identifier (CURIE or absolute URI). When null, implies "exists".</summary>
        public string? Predicate { get; init; } // e.g., ldm:status

        /// <summary>Expected value (string-typed for portability).</summary>
        public string? Equals { get; init; } // e.g., "Active"

        /// <summary>Optional textual description for audits.</summary>
        public string? Description { get; init; }
    }

    /// <summary>
    /// Declarative effect (state transition) to commit to KG or relational store post tool success.
    /// </summary>
    public sealed class ToolEffect
    {
        /// <summary>Target ontology subject (class or instance type).</summary>
        public required string Subject { get; init; } // e.g., ldm:Reservation

        /// <summary>Property to update (CURIE or absolute URI).</summary>
        public required string Property { get; init; } // e.g., ldm:status

        /// <summary>New value as string; adapters may coerce types as needed.</summary>
        public required string NewValue { get; init; } // e.g., "Canceled"

        /// <summary>Optional label for UX or audit trails.</summary>
        public string? Label { get; init; }
    }

    /// <summary>
    /// Minimal KG primitives for Cosmos Gremlin usage.
    /// These are just labels and field keys you can reuse in the graph adapter.
    /// </summary>
    public static class Kg
    {
        public static class Labels
        {
            // Vertex labels
            public const string Person         = "Person";
            public const string Trip           = "Trip";
            public const string Reservation    = "Reservation";
            public const string Flight         = "Flight";
            public const string PaymentMethod  = "PaymentMethod";
            public const string Session        = "Session";
            public const string Message        = "Message";
            public const string MemoryItem     = "MemoryItem";
            public const string Tool           = "Tool";
            public const string Skill          = "Skill";
            public const string State          = "State";

            // Edge labels (mirror ontology property local names)
            public const string Owns           = "owns";
            public const string ForTrip        = "forTrip";
            public const string FliesOn        = "fliesOn";
            public const string PaidWith       = "paidWith";
            public const string HasMessage     = "hasMessage";
            public const string References     = "references";
            public const string Requires       = "requires";
            public const string Produces       = "produces";
        }

        public static class Fields
        {
            public const string Id        = "id";        // graph id
            public const string Curie     = "curie";     // ldm:* identifier when applicable
            public const string Uri       = "uri";       // absolute URI
            public const string Label     = "label";     // human-readable label
            public const string Status    = "status";    // e.g., Active, Canceled
            public const string Kind      = "kind";      // MemoryItem kind
        }
    }
}
```

If you want, I can also generate a **tiny usage sample** that:

* Builds a `ToolSchemaBinding` for `cancelReservation`,
* Attaches the JSON-LD context,
* Adds a precondition (`ldm:status == Active`) and an effect (`ldm:status = Canceled`).

---  
  
## Usage Sample  
  
Here’s a **tiny, compilable usage sample** you can drop in a console project that references `LimboDancer.MCP.Ontology`. It builds a `ToolSchemaBinding` for `cancelReservation`, attaches JSON-LD, adds a precondition and an effect, and shows URI expand/compact plus a stubbed precondition check.

```csharp
// File: samples/CancelReservationSample/Program.cs
// Requires project reference: LimboDancer.MCP.Ontology

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using LimboDancer.MCP.Ontology;

var binding = new ToolSchemaBinding
{
    Name = "cancelReservation",
    Input = new Dictionary<string, ToolField>
    {
        {
            "reservationId",
            new ToolField
            {
                Type = "string",
                OntologyId = Ldm.Classes.Reservation,
                Label = "ReservationId",
                Required = true
            }
        }
    },
    Preconditions =
    {
        new ToolPrecondition
        {
            Subject = Ldm.Classes.Reservation,
            Predicate = Ldm.Properties.Status,
            Equals = "Active",
            Description = "Reservation must be Active before cancel."
        }
    },
    Effects =
    {
        new ToolEffect
        {
            Subject = Ldm.Classes.Reservation,
            Property = Ldm.Properties.Status,
            NewValue = "Canceled",
            Label = "Set Reservation.status to Canceled"
        }
    },
    JsonLdContext = JsonLdContext.GetContextJson()
};

// Pretty-print the JSON-LD context and tool binding
var jsonOpts = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() }
};

Console.WriteLine("=== JSON-LD Context ===");
Console.WriteLine(JsonLdContext.GetContextJson());
Console.WriteLine();

Console.WriteLine("=== Tool Schema Binding (cancelReservation) ===");
Console.WriteLine(JsonSerializer.Serialize(binding, jsonOpts));
Console.WriteLine();

// CURIE expand/compact demo
var curie = Ldm.Classes.Reservation;                  // "ldm:Reservation"
var uri = Ldm.Expand(curie);                          // https://limbodancer.ai/ontology/Reservation
Console.WriteLine($"Expand {curie} -> {uri}");
Console.WriteLine($"Compact {uri} -> {Ldm.Compact(uri)}");
Console.WriteLine();

// Simulate precondition evaluation (in production, query the KG)
var simulatedReservationStatus = "Active";
bool preconditionsOk = SimulatePreconditionCheck(simulatedReservationStatus, binding.Preconditions);

Console.WriteLine($"Preconditions satisfied? {preconditionsOk}");
if (preconditionsOk)
{
    // Apply effect (in production, commit to KG and history)
    Console.WriteLine($"Applying effect: {binding.Effects[0].Property} = {binding.Effects[0].NewValue}");
}

static bool SimulatePreconditionCheck(string reservationStatus, List<ToolPrecondition> preconditions)
{
    foreach (var p in preconditions)
    {
        // Minimal demo: only check ldm:status equals expected
        if (p.Predicate == Ldm.Properties.Status && p.Equals is { } expected)
        {
            if (!string.Equals(reservationStatus, expected, StringComparison.OrdinalIgnoreCase))
                return false;
        }
    }
    return true;
}
```

If you want, I can also provide a **unit test** version (xUnit) of this sample, or a **minimal MCP tool registration** snippet that consumes `ToolSchemaBinding` during `tools/list` exposure.

---

Here you go—both pieces, ready to drop in.

---

## xUnit test version

**File:** `tests/LimboDancer.MCP.Ontology.Tests/CancelReservationSampleTests.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using LimboDancer.MCP.Ontology;
using Xunit;

namespace LimboDancer.MCP.Ontology.Tests
{
    public class CancelReservationSampleTests
    {
        private static ToolSchemaBinding BuildBinding()
        {
            return new ToolSchemaBinding
            {
                Name = "cancelReservation",
                Input = new Dictionary<string, ToolField>
                {
                    {
                        "reservationId",
                        new ToolField
                        {
                            Type = "string",
                            OntologyId = Ldm.Classes.Reservation,
                            Label = "ReservationId",
                            Required = true
                        }
                    }
                },
                Preconditions =
                {
                    new ToolPrecondition
                    {
                        Subject = Ldm.Classes.Reservation,
                        Predicate = Ldm.Properties.Status,
                        Equals = "Active",
                        Description = "Reservation must be Active before cancel."
                    }
                },
                Effects =
                {
                    new ToolEffect
                    {
                        Subject = Ldm.Classes.Reservation,
                        Property = Ldm.Properties.Status,
                        NewValue = "Canceled",
                        Label = "Set Reservation.status to Canceled"
                    }
                },
                JsonLdContext = JsonLdContext.GetContextJson()
            };
        }

        [Fact]
        public void JsonLdContext_IsValidJson_AndContainsCoreTerms()
        {
            var json = JsonLdContext.GetContextJson();
            using var doc = JsonDocument.Parse(json);

            Assert.True(doc.RootElement.TryGetProperty("@context", out var ctx));
            Assert.True(ctx.TryGetProperty(Ldm.Prefix, out var ns));
            Assert.Equal(Ldm.Namespace, ns.GetString());

            // Spot-check a couple of classes and properties
            Assert.True(ctx.TryGetProperty("Reservation", out _));
            Assert.True(ctx.TryGetProperty("status", out _));
        }

        [Fact]
        public void Curie_Expand_And_Compact_RoundTrip()
        {
            var curie = Ldm.Classes.Reservation; // "ldm:Reservation"
            var uri = Ldm.Expand(curie);
            Assert.Equal("https://limbodancer.ai/ontology/Reservation", uri.ToString());

            var compact = Ldm.Compact(uri);
            Assert.Equal(curie, compact);
        }

        [Fact]
        public void Preconditions_Pass_When_Status_Is_Active()
        {
            var binding = BuildBinding();
            var ok = SimulatePreconditionCheck("Active", binding.Preconditions);
            Assert.True(ok);
        }

        [Fact]
        public void Preconditions_Fail_When_Status_Is_Not_Active()
        {
            var binding = BuildBinding();
            var ok = SimulatePreconditionCheck("Canceled", binding.Preconditions);
            Assert.False(ok);
        }

        [Fact]
        public void ToolBinding_Serializes_With_Context_And_Fields()
        {
            var binding = BuildBinding();
            var json = JsonSerializer.Serialize(binding, new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            });

            using var doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.TryGetProperty("name", out var name));
            Assert.Equal("cancelReservation", name.GetString());

            // Ensure JSON-LD context is embedded
            Assert.True(doc.RootElement.TryGetProperty("jsonLdContext", out var ctx));
            Assert.Contains("@context", ctx.GetString() ?? string.Empty);

            // Ensure input field is present
            Assert.True(doc.RootElement.TryGetProperty("input", out var input));
            Assert.True(input.TryGetProperty("reservationId", out var rid));
            Assert.True(rid.TryGetProperty("ontologyId", out var onto));
            Assert.Equal(Ldm.Classes.Reservation, onto.GetString());
        }

        private static bool SimulatePreconditionCheck(string reservationStatus, List<ToolPrecondition> preconditions)
        {
            foreach (var p in preconditions)
            {
                if (p.Predicate == Ldm.Properties.Status && p.Equals is { } expected)
                {
                    if (!string.Equals(reservationStatus, expected, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }
            return true;
        }
    }
}
```

---

## Minimal MCP “tools/list” registration snippet

This shows how to **consume `ToolSchemaBinding`** to produce a **tools list** payload that MCP clients expect. It uses a minimal ASP.NET Core endpoint to return a tools list. You can adapt it later to the official MCP C# SDK server—this keeps the shape explicit and easy to wire up now.

**File:** `samples/McpToolsListHost/Program.cs`

```csharp
// Minimal host to expose tools/list using ToolSchemaBinding.
// Run: dotnet run --project samples/McpToolsListHost
// GET http://localhost:5080/mcp/tools/list

using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.MCP.Ontology;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Build a binding we want to expose
var cancelReservation = new ToolSchemaBinding
{
    Name = "cancelReservation",
    Input = new Dictionary<string, ToolField>
    {
        {
            "reservationId",
            new ToolField
            {
                Type = "string",
                OntologyId = Ldm.Classes.Reservation,
                Label = "ReservationId",
                Required = true
            }
        }
    },
    Preconditions =
    {
        new ToolPrecondition
        {
            Subject = Ldm.Classes.Reservation,
            Predicate = Ldm.Properties.Status,
            Equals = "Active",
            Description = "Reservation must be Active before cancel."
        }
    },
    Effects =
    {
        new ToolEffect
        {
            Subject = Ldm.Classes.Reservation,
            Property = Ldm.Properties.Status,
            NewValue = "Canceled",
            Label = "Set Reservation.status to Canceled"
        }
    },
    JsonLdContext = JsonLdContext.GetContextJson()
};

// Convert ToolSchemaBinding -> MCP tool descriptor
object ToMcpTool(ToolSchemaBinding binding)
{
    // Build a JSON Schema for input with @context merged in.
    // Many MCP clients accept a plain JSON Schema object under "input_schema".
    var inputSchema = new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject(),
        ["required"] = new JsonArray()
    };

    // Merge JSON-LD context at top of schema for ontology binding
    if (!string.IsNullOrWhiteSpace(binding.JsonLdContext))
    {
        try
        {
            var ctxDoc = JsonNode.Parse(binding.JsonLdContext) as JsonObject;
            if (ctxDoc is not null && ctxDoc.TryGetPropertyValue("@context", out var ctxValue))
            {
                inputSchema["@context"] = ctxValue;
            }
        }
        catch
        {
            // ignore context parse errors; schema still valid
        }
    }

    if (binding.Input is not null)
    {
        var props = (JsonObject)inputSchema["properties"]!;
        var req = (JsonArray)inputSchema["required"]!;

        foreach (var kv in binding.Input)
        {
            var fieldName = kv.Key;
            var field = kv.Value;

            var fieldObj = new JsonObject
            {
                ["type"] = field.Type,
                ["@id"] = field.OntologyId
            };

            if (!string.IsNullOrWhiteSpace(field.Label))
                fieldObj["title"] = field.Label;

            props[fieldName] = fieldObj;

            if (field.Required == true)
                req.Add(fieldName);
        }
    }

    // Minimal MCP tool descriptor shape
    var tool = new
    {
        name = binding.Name,
        description = "Cancel a reservation by id. Preconditions: status == Active.",
        input_schema = inputSchema
    };

    return tool;
}

app.MapGet("/mcp/tools/list", () =>
{
    var tools = new[]
    {
        ToMcpTool(cancelReservation)
    };

    // Typical MCP response shape:
    // { "tools": [ { "name": "...", "description": "...", "input_schema": { /* JSON Schema */ } } ] }
    return Results.Json(new { tools }, new JsonSerializerOptions { WriteIndented = true });
});

app.Urls.Add("http://localhost:5080");
await app.RunAsync();
```

> Later, when you switch to the **official MCP C# SDK**, you can plug `ToMcpTool(binding)` into the SDK’s tool registration API by mapping:
>
> * `name` -> tool name,
> * `description` -> tool description,
> * `input_schema` -> the provided JSON Schema object (including the `@context` we merged for ontology).

If you want, I can also provide a **companion `/mcp/invoke` endpoint** that reads `reservationId`, runs a precondition check (stubbed to Active), and returns a result payload plus a minimal “effect” object you can use to commit to the KG.

##  Sample Companion /mcp/invoke

Here’s an updated **single-file host** that now includes **`POST /mcp/invoke`** alongside **`GET /mcp/tools/list`**. It validates `reservationId`, checks the **precondition** (`status == Active`), and returns a **result** plus a **proposed effect** you can commit to the KG. If `dryRun=false`, it also **applies** the effect to an in-memory store (simulating a commit).

**File:** `samples/McpToolsListHost/Program.cs`

```csharp
// Minimal host to expose tools/list and mcp/invoke using ToolSchemaBinding.
// Run: dotnet run --project samples/McpToolsListHost
// GET  http://localhost:5080/mcp/tools/list
// POST http://localhost:5080/mcp/invoke
//
// Body example:
// {
//   "name": "cancelReservation",
//   "arguments": { "reservationId": "R123" },
//   "dryRun": true
// }

using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.MCP.Ontology;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// -----------------------------------------------------------------------------
// In-memory "Reservation" status store (simulates KG-state + RDBMS history)
// -----------------------------------------------------------------------------
var reservations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["R123"] = "Active",
    ["R124"] = "Canceled",
    ["R125"] = "Active"
};

// -----------------------------------------------------------------------------
// Build a binding we want to expose
// -----------------------------------------------------------------------------
var cancelReservation = new ToolSchemaBinding
{
    Name = "cancelReservation",
    Input = new Dictionary<string, ToolField>
    {
        {
            "reservationId",
            new ToolField
            {
                Type = "string",
                OntologyId = Ldm.Classes.Reservation,
                Label = "ReservationId",
                Required = true
            }
        }
    },
    Preconditions =
    {
        new ToolPrecondition
        {
            Subject = Ldm.Classes.Reservation,
            Predicate = Ldm.Properties.Status,
            Equals = "Active",
            Description = "Reservation must be Active before cancel."
        }
    },
    Effects =
    {
        new ToolEffect
        {
            Subject = Ldm.Classes.Reservation,
            Property = Ldm.Properties.Status,
            NewValue = "Canceled",
            Label = "Set Reservation.status to Canceled"
        }
    },
    JsonLdContext = JsonLdContext.GetContextJson()
};

// -----------------------------------------------------------------------------
// Convert ToolSchemaBinding -> MCP tool descriptor
// -----------------------------------------------------------------------------
object ToMcpTool(ToolSchemaBinding binding)
{
    var inputSchema = new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject(),
        ["required"] = new JsonArray()
    };

    if (!string.IsNullOrWhiteSpace(binding.JsonLdContext))
    {
        try
        {
            var ctxDoc = JsonNode.Parse(binding.JsonLdContext) as JsonObject;
            if (ctxDoc is not null && ctxDoc.TryGetPropertyValue("@context", out var ctxValue))
            {
                inputSchema["@context"] = ctxValue;
            }
        }
        catch
        {
            // ignore context parse errors
        }
    }

    if (binding.Input is not null)
    {
        var props = (JsonObject)inputSchema["properties"]!;
        var req = (JsonArray)inputSchema["required"]!;

        foreach (var kv in binding.Input)
        {
            var fieldName = kv.Key;
            var field = kv.Value;

            var fieldObj = new JsonObject
            {
                ["type"] = field.Type,
                ["@id"] = field.OntologyId
            };

            if (!string.IsNullOrWhiteSpace(field.Label))
                fieldObj["title"] = field.Label;

            props[fieldName] = fieldObj;

            if (field.Required == true)
                req.Add(fieldName);
        }
    }

    return new
    {
        name = binding.Name,
        description = "Cancel a reservation by id. Preconditions: status == Active.",
        input_schema = inputSchema
    };
}

// -----------------------------------------------------------------------------
// GET /mcp/tools/list
// -----------------------------------------------------------------------------
app.MapGet("/mcp/tools/list", () =>
{
    var tools = new[] { ToMcpTool(cancelReservation) };
    return Results.Json(new { tools }, new JsonSerializerOptions { WriteIndented = true });
});

// -----------------------------------------------------------------------------
// Request/Response models for /mcp/invoke
// -----------------------------------------------------------------------------
record InvokeRequest(string name, JsonObject? arguments, bool? dryRun);
record InvokeResult(
    string status,                     // ok | precondition_failed | not_found | invalid_arguments
    object? data,
    object[]? proposed_effects,        // array of ToolEffect-like objects (proposed)
    bool effect_applied,               // if dryRun == false and applied
    object? audit                      // extra info
);

// -----------------------------------------------------------------------------
// Helpers: precondition check + effect application
// -----------------------------------------------------------------------------
bool CheckPreconditions(string reservationStatus, List<ToolPrecondition> preconditions, out string? failedReason)
{
    foreach (var p in preconditions)
    {
        if (p.Predicate == Ldm.Properties.Status && p.Equals is { } expected)
        {
            if (!string.Equals(reservationStatus, expected, StringComparison.OrdinalIgnoreCase))
            {
                failedReason = $"Precondition failed: {p.Predicate} != {expected}";
                return false;
            }
        }
    }
    failedReason = null;
    return true;
}

bool ApplyEffect(string reservationId, ToolEffect effect)
{
    // Minimal simulation: update in-memory dictionary; in production, commit to KG and history
    if (!reservations.ContainsKey(reservationId)) return false;
    if (effect.Property == Ldm.Properties.Status)
    {
        reservations[reservationId] = effect.NewValue;
        return true;
    }
    return false;
}

// -----------------------------------------------------------------------------
// POST /mcp/invoke
// -----------------------------------------------------------------------------
app.MapPost("/mcp/invoke", async (InvokeRequest req) =>
{
    // Validate request
    if (!string.Equals(req.name, cancelReservation.Name, StringComparison.Ordinal))
    {
        return Results.Json(new InvokeResult(
            status: "not_found",
            data: null,
            proposed_effects: null,
            effect_applied: false,
            audit: new { message = "Unknown tool name." }),
            new JsonSerializerOptions { WriteIndented = true });
    }

    var args = req.arguments;
    if (args is null || !args.TryGetPropertyValue("reservationId", out var ridNode) || ridNode is null)
    {
        return Results.Json(new InvokeResult(
            status: "invalid_arguments",
            data: null,
            proposed_effects: null,
            effect_applied: false,
            audit: new { message = "Missing required argument: reservationId" }),
            new JsonSerializerOptions { WriteIndented = true });
    }

    var reservationId = ridNode!.GetValue<string?>();
    if (string.IsNullOrWhiteSpace(reservationId))
    {
        return Results.Json(new InvokeResult(
            status: "invalid_arguments",
            data: null,
            proposed_effects: null,
            effect_applied: false,
            audit: new { message = "reservationId cannot be empty." }),
            new JsonSerializerOptions { WriteIndented = true });
    }

    if (!reservations.TryGetValue(reservationId, out var currentStatus))
    {
        return Results.Json(new InvokeResult(
            status: "not_found",
            data: null,
            proposed_effects: null,
            effect_applied: false,
            audit: new { message = $"Reservation {reservationId} not found." }),
            new JsonSerializerOptions { WriteIndented = true });
    }

    // Preconditions
    if (!CheckPreconditions(currentStatus, cancelReservation.Preconditions, out var reason))
    {
        return Results.Json(new InvokeResult(
            status: "precondition_failed",
            data: new { reservationId, currentStatus },
            proposed_effects: null,
            effect_applied: false,
            audit: new { reason }),
            new JsonSerializerOptions { WriteIndented = true });
    }

    // Proposed effect (to commit to KG)
    var effect = cancelReservation.Effects[0];
    var proposed = new object[]
    {
        new
        {
            subject = Ldm.Classes.Reservation,
            property = effect.Property,
            newValue = effect.NewValue,
            label = effect.Label,
            targetId = reservationId
        }
    };

    var dryRun = req.dryRun is null || req.dryRun == true;
    var applied = false;

    if (!dryRun)
    {
        applied = ApplyEffect(reservationId, effect);
    }

    // Result payload (what the tool did or proposes to do)
    var resultPayload = new
    {
        reservationId,
        previousStatus = currentStatus,
        nextStatus = effect.NewValue,
        dryRun = dryRun,
        applied = applied
    };

    return Results.Json(new InvokeResult(
        status: "ok",
        data: resultPayload,
        proposed_effects: proposed,
        effect_applied: applied,
        audit: new
        {
            tool = cancelReservation.Name,
            preconditions = cancelReservation.Preconditions.Select(p => new
            {
                subject = p.Subject,
                predicate = p.Predicate,
                expect = p.Equals
            }),
            context = JsonNode.Parse(cancelReservation.JsonLdContext!)
        }),
        new JsonSerializerOptions { WriteIndented = true });
});

app.Urls.Add("http://localhost:5080");
await app.RunAsync();
```

### Quick checks

List tools:

```bash
curl http://localhost:5080/mcp/tools/list
```

Dry run (no state change):

```bash
curl -X POST http://localhost:5080/mcp/invoke \
  -H "Content-Type: application/json" \
  -d '{ "name":"cancelReservation", "arguments": { "reservationId":"R123" }, "dryRun": true }'
```

Apply (simulate commit):

```bash
curl -X POST http://localhost:5080/mcp/invoke \
  -H "Content-Type: application/json" \
  -d '{ "name":"cancelReservation", "arguments": { "reservationId":"R123" }, "dryRun": false }'
```

If you want this to speak **real MCP stdio/HTTP** next, I can adapt this shape to the **official MCP C# SDK** APIs and wire the same `ToolSchemaBinding` and precondition/effect flow into the SDK’s tool registration and invoke handlers.

---

## Sample Real MCP stdio/HTTP to the **official MCP C# SDK**  
  
Awesome — here’s a **drop-in MCP C# SDK adaptation** that uses your existing `ToolSchemaBinding` (with JSON-LD `@context`) to serve **`tools/list`** and **`tools/call`** over the official protocol.

I’m giving you a **stdio server** that’s 100% aligned with the SDK’s “manual options” pattern (so we can inject our custom JSON Schema with ontology IDs), plus a brief note on the ASP.NET Core SSE/HTTP hosting path.

---

### A) MCP stdio server (uses your `ToolSchemaBinding` directly)

**File:** `src/LimboDancer.MCP.McpServer/Program.cs`

```csharp
// .NET 9
// <ProjectReference Include="..\LimboDancer.MCP.Ontology\LimboDancer.MCP.Ontology.csproj" />
// NuGet: ModelContextProtocol (pre-release), Microsoft.Extensions.Hosting

using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.MCP.Ontology;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// -----------------------------------------------------------------------------
// 1) Tool registry built from your ToolSchemaBinding
// -----------------------------------------------------------------------------
var cancelReservation = new ToolSchemaBinding
{
    Name = "cancelReservation",
    Input = new Dictionary<string, ToolField>
    {
        ["reservationId"] = new ToolField
        {
            Type = "string",
            OntologyId = Ldm.Classes.Reservation,
            Label = "ReservationId",
            Required = true
        }
    },
    Preconditions =
    {
        new ToolPrecondition
        {
            Subject = Ldm.Classes.Reservation,
            Predicate = Ldm.Properties.Status,
            Equals = "Active",
            Description = "Reservation must be Active before cancel."
        }
    },
    Effects =
    {
        new ToolEffect
        {
            Subject = Ldm.Classes.Reservation,
            Property = Ldm.Properties.Status,
            NewValue = "Canceled",
            Label = "Set Reservation.status to Canceled"
        }
    },
    JsonLdContext = JsonLdContext.GetContextJson()
};

// “In-memory KG/state” to keep the sample self-contained.
var reservations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["R123"] = "Active",
    ["R124"] = "Canceled",
    ["R125"] = "Active"
};

var registry = new Dictionary<string, ToolSchemaBinding>(StringComparer.OrdinalIgnoreCase)
{
    [cancelReservation.Name] = cancelReservation
};

// -----------------------------------------------------------------------------
// 2) Helpers to map ToolSchemaBinding -> MCP Tool + call logic
// -----------------------------------------------------------------------------
Tool ToMcpTool(ToolSchemaBinding binding)
{
    // Build a JSON Schema object and merge in @context for ontology binding.
    var schema = new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject(),
        ["required"] = new JsonArray()
    };

    if (!string.IsNullOrWhiteSpace(binding.JsonLdContext))
    {
        if (JsonNode.Parse(binding.JsonLdContext) is JsonObject ctx &&
            ctx.TryGetPropertyValue("@context", out var ctxVal))
        {
            schema["@context"] = ctxVal;
        }
    }

    if (binding.Input is not null)
    {
        var props = (JsonObject)schema["properties"]!;
        var req   = (JsonArray) schema["required"]!;
        foreach (var (name, field) in binding.Input)
        {
            var fieldObj = new JsonObject
            {
                ["type"] = field.Type,
                ["@id"]  = field.OntologyId
            };
            if (!string.IsNullOrWhiteSpace(field.Label))
                fieldObj["title"] = field.Label;

            props[name] = fieldObj;
            if (field.Required == true) req.Add(name);
        }
    }

    return new Tool
    {
        Name        = binding.Name,
        Description = "Cancel a reservation by id. Preconditions: status == Active.",
        InputSchema = JsonSerializer.Deserialize<JsonElement>(schema.ToJsonString())
    };
}

bool CheckPreconditions(string reservationStatus, List<ToolPrecondition> pcs, out string? reason)
{
    foreach (var p in pcs)
    {
        if (p.Predicate == Ldm.Properties.Status && p.Equals is { } expected)
        {
            if (!string.Equals(reservationStatus, expected, StringComparison.OrdinalIgnoreCase))
            {
                reason = $"Precondition failed: {p.Predicate} != {expected}";
                return false;
            }
        }
    }
    reason = null;
    return true;
}

bool ApplyEffect(string reservationId, ToolEffect effect)
{
    if (!reservations.ContainsKey(reservationId)) return false;
    if (effect.Property == Ldm.Properties.Status)
    {
        reservations[reservationId] = effect.NewValue;
        return true;
    }
    return false;
}

// -----------------------------------------------------------------------------
// 3) Configure MCP server options with custom Tools handlers
//    (this mirrors the SDK README “More control” sample)
// -----------------------------------------------------------------------------
var options = new McpServerOptions
{
    ServerInfo = new Implementation { Name = "LimboDancer.MCP", Version = "0.1.0" },
    Capabilities = new ServerCapabilities
    {
        Tools = new ToolsCapability
        {
            ListToolsHandler = (request, ct) =>
            {
                // Expose all registered ToolSchemaBindings
                var tools = registry.Values.Select(ToMcpTool).ToArray();
                return ValueTask.FromResult(new ListToolsResult { Tools = [.. tools] });
            },

            CallToolHandler = async (request, ct) =>
            {
                if (request.Params?.Name is not { } name || !registry.TryGetValue(name, out var binding))
                    throw new McpException($"Unknown tool: '{request.Params?.Name}'");

                // Extract args
                if (request.Params?.Arguments is null ||
                    !request.Params.Arguments.TryGetValue("reservationId", out var ridObj) ||
                    ridObj is null)
                    throw new McpException("Missing required argument 'reservationId'");

                var reservationId = ridObj?.ToString();
                if (string.IsNullOrWhiteSpace(reservationId))
                    throw new McpException("'reservationId' cannot be empty.");

                if (!reservations.TryGetValue(reservationId, out var currentStatus))
                    throw new McpException($"Reservation {reservationId} not found.");

                // Preconditions
                if (!CheckPreconditions(currentStatus, binding.Preconditions, out var reason))
                {
                    var fail = new
                    {
                        status = "precondition_failed",
                        reservationId,
                        currentStatus,
                        reason
                    };
                    return new CallToolResult
                    {
                        Content = [new TextContentBlock { Type = "text", Text = JsonSerializer.Serialize(fail) }]
                    };
                }

                // “Proposed” effect
                var effect = binding.Effects[0];
                var proposed = new
                {
                    subject  = Ldm.Classes.Reservation,
                    property = effect.Property,
                    newValue = effect.NewValue,
                    label    = effect.Label,
                    targetId = reservationId
                };

                // Optional: check for dry-run flag (non-standard; many clients don’t send it)
                var dryRun = true;
                if (request.Params.Arguments.TryGetValue("dryRun", out var dr) && dr is not null &&
                    bool.TryParse(dr.ToString(), out var parsed)) dryRun = parsed;

                var applied = false;
                if (!dryRun) applied = ApplyEffect(reservationId!, effect);

                var ok = new
                {
                    status = "ok",
                    reservationId,
                    previousStatus = currentStatus,
                    nextStatus     = effect.NewValue,
                    dryRun,
                    applied,
                    proposed_effects = new[] { proposed }
                };

                return new CallToolResult
                {
                    Content = [new TextContentBlock { Type = "text", Text = JsonSerializer.Serialize(ok) }]
                };
            }
        }
    }
};

// -----------------------------------------------------------------------------
// 4) Start stdio server (official SDK pattern)
// -----------------------------------------------------------------------------
await using IMcpServer server =
    McpServerFactory.Create(new StdioServerTransport("LimboDancer.MCP"), options);
await server.RunAsync();
```

**Why this shape?** It follows the SDK’s documented “manual options” pattern, where you pass a `ServerCapabilities.Tools` with custom **`ListToolsHandler`** and **`CallToolHandler`**. The SDK README shows this exact approach (including `McpServerOptions`, `ListToolsHandler`, `CallToolHandler`, and creation via `McpServerFactory.Create(new StdioServerTransport(...), options)`) — we’re just swapping the “echo” sample for your ontology-aware mapping. ([GitHub][1])

**Run it**

```bash
dotnet run --project src/LimboDancer.MCP.McpServer
# Connect from an MCP client or Inspector; or use the SDK client
```

---

#### Optional: quick SDK client sanity check

```csharp
// Tiny smoke test (separate console app)
// NuGet: ModelContextProtocol (pre-release)
using ModelContextProtocol;
using ModelContextProtocol.Client;

var client = await McpClientFactory.CreateAsync(
    new StdioClientTransport(new StdioClientTransportOptions {
        Name="LimboDancer.MCP",
        Command="dotnet",
        Arguments = ["run","--project","src/LimboDancer.MCP.McpServer"]
    })
);

// List tools
var tools = await client.ListToolsAsync();
Console.WriteLine(string.Join(", ", tools.Select(t => t.Name)));

// Call cancelReservation as dry-run
var result = await client.CallToolAsync("cancelReservation",
    new Dictionary<string, object?> { ["reservationId"]="R123", ["dryRun"]=true });
Console.WriteLine(result.Content.OfType<TextContentBlock>().First().Text);
```

The SDK README shows how to make clients and call tools (we mirror that shape here). ([GitHub][1])

---

### B) Hosting the same server over Streamable HTTP (ASP.NET Core)

If you want **remote** clients (Claude Desktop, Copilot Chat, etc.) to reach your server over HTTP/SSE, use the ASP.NET Core package and map MCP endpoints:

```csharp
// .NET 9 web project
// NuGet: ModelContextProtocol.AspNetCore (pre-release)
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer();        // registers MCP services

var app = builder.Build();
app.MapMcp();               // exposes /messages and /sse endpoints
app.Run("http://0.0.0.0:5080");
```

This pattern is documented by the community (and mirrors the SDK samples) — `AddMcpServer()` registers MCP services and `app.MapMcp()` exposes the **Streamable HTTP/SSE** endpoints. ([laurentkempe.com][2])

> **How do we plug in your `ToolSchemaBinding` here?**
> Under the hood, `AddMcpServer` + `MapMcp` use the very same `McpServerOptions` object you saw above. You can configure **`ListToolsHandler`** and **`CallToolHandler`** with your mapping just like we did for stdio; the SDK README’s “More control” section shows the exact options type and handlers we used. (Same types, same handlers — different transport.) ([GitHub][1])

---

#### Notes & gotchas

* **Schemas & JSON-LD:** We inject `@context` directly into `input_schema`. That’s why we use the **manual options** API rather than attribute-based tools (attributes auto-infer schemas from method signatures and won’t include your ontology context by default). The SDK’s samples confirm both ways are supported. ([GitHub][1])
* **Transports:** MCP standardizes **stdio** and **Streamable HTTP**. The SDK supports both; `StdioServerTransport` is used in the code above, and `ModelContextProtocol.AspNetCore` offers HTTP/SSE via `MapMcp`. ([Model Context Protocol][3], [GitHub][1], [laurentkempe.com][2])
* **Output schemas:** If you later switch some tools to the attribute model, the SDK can auto-generate tool **output schemas** from return types (June 18 update). We don’t rely on that here, but it’s handy. ([Microsoft for Developers][4])

---

#### Want me to wire this into your repo now?

I can translate this into two projects:

* `LimboDancer.MCP.McpServer` (stdio) — minimal footprint for local runs and Copilot/Inspector testing.
* `LimboDancer.MCP.McpServer.Http` (ASP.NET Core) — remote/serverless-friendly; great for **Azure Container Apps**.

Both will consume `LimboDancer.MCP.Ontology` and the same `ToolSchemaBinding` mapping so your ontology, preconditions, and effects stay **first-class**.

[1]: https://github.com/modelcontextprotocol/csharp-sdk "GitHub - modelcontextprotocol/csharp-sdk: The official C# SDK for Model Context Protocol servers and clients. Maintained in collaboration with Microsoft."
[2]: https://laurentkempe.com/2025/04/05/sse-powered-mcp-server-with-csharp-and-dotnet-in-157mb-executable/?utm_source=chatgpt.com "SSE-Powered MCP Server with C# and .NET in 15.7MB ..."
[3]: https://modelcontextprotocol.io/specification/2025-03-26/basic/transports?utm_source=chatgpt.com "Transports"
[4]: https://devblogs.microsoft.com/dotnet/mcp-csharp-sdk-2025-06-18-update/?utm_source=chatgpt.com "MCP C# SDK Gets Major Update: Support for Protocol ..."

---

Awesome — here are **PR-ready projects** you can drop into your repo to wire the MCP SDK in **both transports**:

* `src/LimboDancer.MCP.McpServer` → **stdio** transport (great for Copilot/Claude Desktop/Inspector).
* `src/LimboDancer.MCP.McpServer.Http` → **Streamable HTTP/SSE** (great for Azure Container Apps).

I used the **official MCP C# SDK** packages (`ModelContextProtocol`, `ModelContextProtocol.AspNetCore`) and the **manual handler** pattern so your **ontology-aware JSON-LD `@context`** stays first-class in `tools/list` and `tools/call`. (Package IDs and usage are from the official NuGet and docs.) ([NuGet][1], [packages.nuget.org][2], [Microsoft for Developers][3], [Medium][4])

---

### 1) `src/LimboDancer.MCP.McpServer/LimboDancer.MCP.McpServer.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\LimboDancer.MCP.Ontology\LimboDancer.MCP.Ontology.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="ModelContextProtocol" Version="0.3.0-preview.3" />
  </ItemGroup>
</Project>
```

> `ModelContextProtocol` is the official MCP C# SDK (preview). ([NuGet][1])

---

#### 2) `src/LimboDancer.MCP.McpServer/Program.cs` (stdio server)

```csharp
// .NET 9 stdio MCP server exposing ontology-aware tools.
// Requires: LimboDancer.MCP.Ontology project + NuGet ModelContextProtocol (preview).

using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.MCP.Ontology;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Information);

// -----------------------------------------------------------------------------
// 1) Tool registry from your ontology-aware binding
// -----------------------------------------------------------------------------
var cancelReservation = new ToolSchemaBinding
{
    Name = "cancelReservation",
    Input = new Dictionary<string, ToolField>
    {
        ["reservationId"] = new ToolField
        {
            Type = "string",
            OntologyId = Ldm.Classes.Reservation,
            Label = "ReservationId",
            Required = true
        }
    },
    Preconditions =
    {
        new ToolPrecondition
        {
            Subject = Ldm.Classes.Reservation,
            Predicate = Ldm.Properties.Status,
            Equals = "Active",
            Description = "Reservation must be Active before cancel."
        }
    },
    Effects =
    {
        new ToolEffect
        {
            Subject = Ldm.Classes.Reservation,
            Property = Ldm.Properties.Status,
            NewValue = "Canceled",
            Label = "Set Reservation.status to Canceled"
        }
    },
    JsonLdContext = JsonLdContext.GetContextJson()
};

// Minimal in-memory state to demo preconditions/effects.
var reservations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["R123"] = "Active",
    ["R124"] = "Canceled",
    ["R125"] = "Active"
};

var registry = new Dictionary<string, ToolSchemaBinding>(StringComparer.OrdinalIgnoreCase)
{
    [cancelReservation.Name] = cancelReservation
};

// -----------------------------------------------------------------------------
// 2) Helpers: Binding -> MCP Tool; precondition check; effect apply
// -----------------------------------------------------------------------------
Tool ToMcpTool(ToolSchemaBinding binding)
{
    var schema = new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject(),
        ["required"] = new JsonArray()
    };

    if (!string.IsNullOrWhiteSpace(binding.JsonLdContext) &&
        JsonNode.Parse(binding.JsonLdContext) is JsonObject ctx &&
        ctx.TryGetPropertyValue("@context", out var ctxVal))
    {
        schema["@context"] = ctxVal; // keep ontology @context in schema
    }

    if (binding.Input is not null)
    {
        var props = (JsonObject)schema["properties"]!;
        var req   = (JsonArray) schema["required"]!;
        foreach (var (name, field) in binding.Input)
        {
            var fieldObj = new JsonObject
            {
                ["type"] = field.Type,
                ["@id"]  = field.OntologyId
            };
            if (!string.IsNullOrWhiteSpace(field.Label)) fieldObj["title"] = field.Label;
            props[name] = fieldObj;
            if (field.Required == true) req.Add(name);
        }
    }

    return new Tool
    {
        Name        = binding.Name,
        Description = "Cancel a reservation by id. Preconditions: status == Active.",
        InputSchema = JsonSerializer.Deserialize<JsonElement>(schema.ToJsonString())
    };
}

bool CheckPreconditions(string reservationStatus, List<ToolPrecondition> pcs, out string? reason)
{
    foreach (var p in pcs)
    {
        if (p.Predicate == Ldm.Properties.Status && p.Equals is { } expected)
        {
            if (!string.Equals(reservationStatus, expected, StringComparison.OrdinalIgnoreCase))
            {
                reason = $"Precondition failed: {p.Predicate} != {expected}";
                return false;
            }
        }
    }
    reason = null;
    return true;
}

bool ApplyEffect(string reservationId, ToolEffect effect)
{
    if (!reservations.ContainsKey(reservationId)) return false;
    if (effect.Property == Ldm.Properties.Status)
    {
        reservations[reservationId] = effect.NewValue;
        return true;
    }
    return false;
}

// -----------------------------------------------------------------------------
// 3) MCP server options with custom Tools handlers (manual options pattern)
// -----------------------------------------------------------------------------
var options = new McpServerOptions
{
    ServerInfo = new Implementation { Name = "LimboDancer.MCP", Version = "0.1.0" },
    Capabilities = new ServerCapabilities
    {
        Tools = new ToolsCapability
        {
            ListToolsHandler = (request, ct) =>
            {
                var tools = registry.Values.Select(ToMcpTool).ToArray();
                return ValueTask.FromResult(new ListToolsResult { Tools = [.. tools] });
            },

            CallToolHandler = async (request, ct) =>
            {
                if (request.Params?.Name is not { } name || !registry.TryGetValue(name, out var binding))
                    throw new McpException($"Unknown tool: '{request.Params?.Name}'");

                if (request.Params?.Arguments is null ||
                    !request.Params.Arguments.TryGetValue("reservationId", out var ridObj) ||
                    ridObj is null)
                    throw new McpException("Missing required argument 'reservationId'");

                var reservationId = ridObj.ToString();
                if (string.IsNullOrWhiteSpace(reservationId))
                    throw new McpException("'reservationId' cannot be empty.");

                if (!reservations.TryGetValue(reservationId, out var currentStatus))
                    throw new McpException($"Reservation {reservationId} not found.");

                if (!CheckPreconditions(currentStatus, binding.Preconditions, out var reason))
                {
                    var fail = new { status = "precondition_failed", reservationId, currentStatus, reason };
                    return new CallToolResult
                    {
                        Content = [new TextContentBlock { Type = "text", Text = JsonSerializer.Serialize(fail) }]
                    };
                }

                var effect  = binding.Effects[0];
                var proposed = new
                {
                    subject  = Ldm.Classes.Reservation,
                    property = effect.Property,
                    newValue = effect.NewValue,
                    label    = effect.Label,
                    targetId = reservationId
                };

                var dryRun = true;
                if (request.Params.Arguments.TryGetValue("dryRun", out var dr) &&
                    dr is not null && bool.TryParse(dr.ToString(), out var parsed)) dryRun = parsed;

                var applied = !dryRun && ApplyEffect(reservationId, effect);

                var ok = new
                {
                    status = "ok",
                    reservationId,
                    previousStatus = currentStatus,
                    nextStatus     = effect.NewValue,
                    dryRun,
                    applied,
                    proposed_effects = new[] { proposed }
                };

                return new CallToolResult
                {
                    Content = [new TextContentBlock { Type = "text", Text = JsonSerializer.Serialize(ok) }]
                };
            }
        }
    }
};

// -----------------------------------------------------------------------------
// 4) Start stdio transport (official SDK supports stdio + streamable HTTP)
// -----------------------------------------------------------------------------
await using IMcpServer server =
    McpServerFactory.Create(new StdioServerTransport("LimboDancer.MCP"), options);
await server.RunAsync();
```

> The stdio pattern (server reads stdin/writes stdout) is the canonical local transport. ([Model Context Protocol][5])

---

### 3) `src/LimboDancer.MCP.McpServer.Http/LimboDancer.MCP.McpServer.Http.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\LimboDancer.MCP.Ontology\LimboDancer.MCP.Ontology.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.3.0-preview.3" />
  </ItemGroup>
</Project>
```

> `ModelContextProtocol.AspNetCore` exposes **Streamable HTTP/SSE** endpoints via `MapMcp()`. ([packages.nuget.org][2], [Medium][4])

---

#### 4) `src/LimboDancer.MCP.McpServer.Http/Program.cs` (HTTP/SSE server)

```csharp
// ASP.NET Core host exposing MCP over Streamable HTTP/SSE.
// Requires: LimboDancer.MCP.Ontology + NuGet ModelContextProtocol.AspNetCore (preview).

using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.MCP.Ontology;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

// --- Build ontology-aware tool binding (same as stdio server) ---
var cancelReservation = new ToolSchemaBinding
{
    Name = "cancelReservation",
    Input = new Dictionary<string, ToolField>
    {
        ["reservationId"] = new ToolField
        {
            Type = "string",
            OntologyId = Ldm.Classes.Reservation,
            Label = "ReservationId",
            Required = true
        }
    },
    Preconditions =
    {
        new ToolPrecondition
        {
            Subject = Ldm.Classes.Reservation,
            Predicate = Ldm.Properties.Status,
            Equals = "Active",
            Description = "Reservation must be Active before cancel."
        }
    },
    Effects =
    {
        new ToolEffect
        {
            Subject = Ldm.Classes.Reservation,
            Property = Ldm.Properties.Status,
            NewValue = "Canceled",
            Label = "Set Reservation.status to Canceled"
        }
    },
    JsonLdContext = JsonLdContext.GetContextJson()
};

var reservations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["R123"] = "Active",
    ["R124"] = "Canceled",
    ["R125"] = "Active"
};

var registry = new Dictionary<string, ToolSchemaBinding>(StringComparer.OrdinalIgnoreCase)
{
    [cancelReservation.Name] = cancelReservation
};

// --- Helper mappers/logic (same as stdio) ---
Tool ToMcpTool(ToolSchemaBinding binding)
{
    var schema = new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject(),
        ["required"] = new JsonArray()
    };

    if (!string.IsNullOrWhiteSpace(binding.JsonLdContext) &&
        JsonNode.Parse(binding.JsonLdContext) is JsonObject ctx &&
        ctx.TryGetPropertyValue("@context", out var ctxVal))
    {
        schema["@context"] = ctxVal;
    }

    if (binding.Input is not null)
    {
        var props = (JsonObject)schema["properties"]!;
        var req   = (JsonArray) schema["required"]!;
        foreach (var (name, field) in binding.Input)
        {
            var fieldObj = new JsonObject
            {
                ["type"] = field.Type,
                ["@id"]  = field.OntologyId
            };
            if (!string.IsNullOrWhiteSpace(field.Label)) fieldObj["title"] = field.Label;
            props[name] = fieldObj;
            if (field.Required == true) req.Add(name);
        }
    }

    return new Tool
    {
        Name        = binding.Name,
        Description = "Cancel a reservation by id. Preconditions: status == Active.",
        InputSchema = JsonSerializer.Deserialize<JsonElement>(schema.ToJsonString())
    };
}

bool CheckPreconditions(string reservationStatus, List<ToolPrecondition> pcs, out string? reason)
{
    foreach (var p in pcs)
    {
        if (p.Predicate == Ldm.Properties.Status && p.Equals is { } expected)
        {
            if (!string.Equals(reservationStatus, expected, StringComparison.OrdinalIgnoreCase))
            {
                reason = $"Precondition failed: {p.Predicate} != {expected}";
                return false;
            }
        }
    }
    reason = null;
    return true;
}

bool ApplyEffect(string reservationId, ToolEffect effect)
{
    if (!reservations.ContainsKey(reservationId)) return false;
    if (effect.Property == Ldm.Properties.Status)
    {
        reservations[reservationId] = effect.NewValue;
        return true;
    }
    return false;
}

// --- Register MCP server with manual handlers in DI ---
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new Implementation { Name = "LimboDancer.MCP.Http", Version = "0.1.0" };
    options.Capabilities = new ServerCapabilities
    {
        Tools = new ToolsCapability
        {
            ListToolsHandler = (request, ct) =>
            {
                var tools = registry.Values.Select(ToMcpTool).ToArray();
                return ValueTask.FromResult(new ListToolsResult { Tools = [.. tools] });
            },
            CallToolHandler = async (request, ct) =>
            {
                if (request.Params?.Name is not { } name || !registry.TryGetValue(name, out var binding))
                    throw new McpException($"Unknown tool: '{request.Params?.Name}'");

                if (request.Params?.Arguments is null ||
                    !request.Params.Arguments.TryGetValue("reservationId", out var ridObj) ||
                    ridObj is null)
                    throw new McpException("Missing required argument 'reservationId'");

                var reservationId = ridObj.ToString();
                if (string.IsNullOrWhiteSpace(reservationId))
                    throw new McpException("'reservationId' cannot be empty.");

                if (!reservations.TryGetValue(reservationId, out var currentStatus))
                    throw new McpException($"Reservation {reservationId} not found.");

                if (!CheckPreconditions(currentStatus, binding.Preconditions, out var reason))
                {
                    var fail = new { status = "precondition_failed", reservationId, currentStatus, reason };
                    return new CallToolResult
                    {
                        Content = [new TextContentBlock { Type = "text", Text = JsonSerializer.Serialize(fail) }]
                    };
                }

                var effect   = binding.Effects[0];
                var proposed = new
                {
                    subject  = Ldm.Classes.Reservation,
                    property = effect.Property,
                    newValue = effect.NewValue,
                    label    = effect.Label,
                    targetId = reservationId
                };

                var dryRun = true;
                if (request.Params.Arguments.TryGetValue("dryRun", out var dr) &&
                    dr is not null && bool.TryParse(dr.ToString(), out var parsed)) dryRun = parsed;

                var applied = !dryRun && ApplyEffect(reservationId, effect);

                var ok = new
                {
                    status = "ok",
                    reservationId,
                    previousStatus = currentStatus,
                    nextStatus     = effect.NewValue,
                    dryRun,
                    applied,
                    proposed_effects = new[] { proposed }
                };

                return new CallToolResult
                {
                    Content = [new TextContentBlock { Type = "text", Text = JsonSerializer.Serialize(ok) }]
                };
            }
        }
    };
});

// Build the app and map MCP endpoints (adds /messages and /sse for Streamable HTTP).
var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok("ok"));
app.MapMcp(); // provided by ModelContextProtocol.AspNetCore to expose MCP HTTP/SSE endpoints

// Bind to 0.0.0.0:5080 for ACA
app.Urls.Add("http://0.0.0.0:5080");
await app.RunAsync();
```

> `MapMcp()` is the ASP.NET Core extension that exposes MCP’s **/messages** and **/sse** endpoints for Streamable HTTP. ([Medium][4])

---

### 5) How to run

**stdio (local):**

```bash
dotnet run --project src/LimboDancer.MCP.McpServer
```

* Connect with MCP Inspector / Copilot / Claude Desktop as a **stdio** server.
* The SDK’s stdio transport is the canonical local mode. ([Model Context Protocol][5])

**HTTP/SSE (remote or local):**

```bash
dotnet run --project src/LimboDancer.MCP.McpServer.Http
# Server listens on http://localhost:5080 (or 0.0.0.0:5080 in containers)
```

* Use MCP Inspector in **remote** mode and point to the URL, or configure your client to the Streamable HTTP endpoint. The AspNetCore package wires **/messages** and **/sse** automatically. ([Medium][4])

---

### 6) NuGet notes

* Install from CLI if you prefer:

  ```bash
  dotnet add src/LimboDancer.MCP.McpServer package ModelContextProtocol --prerelease
  dotnet add src/LimboDancer.MCP.McpServer.Http package ModelContextProtocol.AspNetCore --prerelease
  ```

  (Official packages and latest preview versions referenced here.) ([NuGet][1], [packages.nuget.org][2])

---

If you want, I can also add **GitHub Actions** to build/publish both containers and a **Container Apps** Bicep stub.

[1]: https://www.nuget.org/packages/ModelContextProtocol?utm_source=chatgpt.com "ModelContextProtocol 0.3.0-preview.3"
[2]: https://packages.nuget.org/packages/ModelContextProtocol.AspNetCore/0.3.0-preview.3?utm_source=chatgpt.com "ModelContextProtocol.AspNetCore 0.3.0-preview.3"
[3]: https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/?utm_source=chatgpt.com "Build a Model Context Protocol (MCP) server in C# - .NET ..."
[4]: https://medium.com/%40hemant.mulay/using-microsofts-c-sdk-for-model-context-protocol-to-build-mcp-server-for-net-web-api-3f4a1c45c516?utm_source=chatgpt.com "Using Microsoft's C# SDK for Model Context Protocol to ..."
[5]: https://modelcontextprotocol.io/docs/concepts/transports?utm_source=chatgpt.com "Transports"

---

Sweet — here are **PR-ready files** to (1) build & push your containers and (2) deploy **Azure Container Apps** via Bicep.

---

## **GitHub Actions**

### 1) CI: build, test, and publish images to GHCR

**File:** `.github/workflows/ci.yml`

```yaml
name: ci

on:
  push:
    branches: [ main ]
    tags: [ 'v*.*.*' ]
  pull_request:

permissions:
  contents: read
  packages: write

env:
  REGISTRY: ghcr.io
  IMAGE_MCP_HTTP: ${{ github.repository_owner }}/limbodancer-mcp-http
  IMAGE_CONSOLE:  ${{ github.repository_owner }}/limbodancer-console

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build (Release)
        run: dotnet build -c Release --no-restore

      - name: Test
        run: dotnet test -c Release --no-build --verbosity normal

  build-and-push-images:
    needs: build-and-test
    runs-on: ubuntu-latest
    if: github.event_name != 'pull_request' # only push on main/tags
    steps:
      - uses: actions/checkout@v4

      - name: Login to GHCR
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Set up Buildx
        uses: docker/setup-buildx-action@v3

      - name: Docker meta (MCP HTTP)
        id: meta_mcp
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY}}/${{ env.IMAGE_MCP_HTTP }}
          tags: |
            type=sha,prefix=sha-,format=short
            type=ref,event=branch
            type=semver,pattern={{version}}

      - name: Docker meta (Console)
        id: meta_console
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY}}/${{ env.IMAGE_CONSOLE }}
          tags: |
            type=sha,prefix=sha-,format=short
            type=ref,event=branch
            type=semver,pattern={{version}}

      - name: Build+Push MCP HTTP
        uses: docker/build-push-action@v6
        with:
          context: .
          file: src/LimboDancer.MCP.McpServer.Http/Dockerfile
          push: true
          tags: ${{ steps.meta_mcp.outputs.tags }}
          labels: ${{ steps.meta_mcp.outputs.labels }}
          cache-from: type=registry,ref=${{ env.REGISTRY}}/${{ env.IMAGE_MCP_HTTP }}:buildcache
          cache-to: type=registry,ref=${{ env.REGISTRY}}/${{ env.IMAGE_MCP_HTTP }}:buildcache,mode=max

      - name: Build+Push Console
        uses: docker/build-push-action@v6
        with:
          context: .
          file: src/LimboDancer.MCP.BlazorConsole/Dockerfile
          push: true
          tags: ${{ steps.meta_console.outputs.tags }}
          labels: ${{ steps.meta_console.outputs.labels }}
          cache-from: type=registry,ref=${{ env.REGISTRY}}/${{ env.IMAGE_CONSOLE }}:buildcache
          cache-to: type=registry,ref=${{ env.REGISTRY}}/${{ env.IMAGE_CONSOLE }}:buildcache,mode=max

      - name: Image tags output
        run: |
          echo "MCP HTTP tags:"
          echo "${{ steps.meta_mcp.outputs.tags }}"
          echo "Console tags:"
          echo "${{ steps.meta_console.outputs.tags }}"
```

---

## **Container Apps** 
### 2) CD: deploy Container Apps with Bicep (manual trigger)

**File:** `.github/workflows/deploy-aca.yml`

```yaml
name: deploy-aca

on:
  workflow_dispatch:
    inputs:
      resourceGroup:
        description: 'Azure Resource Group'
        required: true
      location:
        description: 'Azure region (e.g. eastus)'
        required: true
        default: 'eastus'
      envName:
        description: 'ACA Environment name'
        required: true
        default: 'ldm-aca-env'
      mcpImage:
        description: 'Full image ref for MCP HTTP (e.g. ghcr.io/OWNER/limbodancer-mcp-http:sha-abc1234)'
        required: true
      consoleImage:
        description: 'Full image ref for Console (e.g. ghcr.io/OWNER/limbodancer-console:sha-abc1234)'
        required: true
      registryServer:
        description: 'Container registry server (ghcr.io or myacr.azurecr.io)'
        required: true
        default: 'ghcr.io'
      registryUsername:
        description: 'Registry username (for GHCR: your GitHub username)'
        required: true
      registryPasswordSecret:
        description: 'Name of GH secret containing registry password or PAT (e.g. GHCR_READ_TOKEN)'
        required: true
        default: 'GHCR_READ_TOKEN'
      publicIngressConsole:
        description: 'Expose Console publicly'
        required: true
        default: 'true'
      publicIngressMcp:
        description: 'Expose MCP HTTP publicly'
        required: true
        default: 'true'

permissions:
  id-token: write
  contents: read

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Azure login (OIDC)
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Deploy Bicep
        uses: azure/arm-deploy@v2
        with:
          scope: resourcegroup
          resourceGroupName: ${{ inputs.resourceGroup }}
          region: ${{ inputs.location }}
          template: infra/aca-main.bicep
          parameters: >
            envName=${{ inputs.envName }}
            location=${{ inputs.location }}
            registryServer=${{ inputs.registryServer }}
            registryUsername=${{ inputs.registryUsername }}
            registryPassword=${{ secrets[inputs.registryPasswordSecret] }}
            mcpHttpImage=${{ inputs.mcpImage }}
            consoleImage=${{ inputs.consoleImage }}
            publicIngressConsole=${{ inputs.publicIngressConsole }}
            publicIngressMcp=${{ inputs.publicIngressMcp }}

      - name: Show endpoints
        run: |
          az config set extension.use_dynamic_install=yes_without_prompt
          echo "Console URL:"
          az containerapp show -g "${{ inputs.resourceGroup }}" -n ldm-console --query properties.configuration.ingress.fqdn -o tsv
          echo "MCP HTTP URL:"
          az containerapp show -g "${{ inputs.resourceGroup }}" -n ldm-mcp-http --query properties.configuration.ingress.fqdn -o tsv
```

> **Registry auth:** For **GHCR**, create a classic PAT with `read:packages` and store it as repo secret `GHCR_READ_TOKEN`. Or make your images public and set empty creds.

---

### 3) Bicep: Container Apps environment + two apps

**File:** `infra/aca-main.bicep`

```bicep
@description('Azure region')
param location string

@description('ACA Environment name')
param envName string = 'ldm-aca-env'

@description('Container registry server (e.g. ghcr.io or myacr.azurecr.io)')
param registryServer string

@description('Registry username')
param registryUsername string

@secure()
@description('Registry password or PAT with read access')
param registryPassword string

@description('Full image for MCP HTTP (e.g. ghcr.io/OWNER/limbodancer-mcp-http:sha-abc1234)')
param mcpHttpImage string

@description('Full image for Console (e.g. ghcr.io/OWNER/limbodancer-console:sha-abc1234)')
param consoleImage string

@description('Expose Console publicly')
param publicIngressConsole bool = true

@description('Expose MCP HTTP publicly')
param publicIngressMcp bool = true

@description('Console target port')
param consolePort int = 8080

@description('MCP HTTP target port')
param mcpPort int = 5080

// --- Log Analytics (required by ACA env) ---
resource law 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${envName}-log'
  location: location
  properties: {
    retentionInDays: 30
    sku: { name: 'PerGB2018' }
  }
}

// --- Container Apps Environment ---
resource acaEnv 'Microsoft.App/managedEnvironments@2024-02-02' = {
  name: envName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: law.properties.customerId
        sharedKey: law.listKeys().primarySharedKey
      }
    }
  }
}

// --- Console (Blazor Server) ---
resource consoleApp 'Microsoft.App/containerApps@2024-02-02' = {
  name: 'ldm-console'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: acaEnv.id
    configuration: {
      ingress: {
        external: publicIngressConsole
        targetPort: consolePort
        transport: 'auto'
      }
      registries: [
        {
          server: registryServer
          username: registryUsername
          passwordSecretRef: 'regpwd'
        }
      ]
      secrets: [
        {
          name: 'regpwd'
          value: registryPassword
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'console'
          image: consoleImage
          resources: {
            cpu: 0.5
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://0.0.0.0:${consolePort}'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

// --- MCP HTTP server ---
resource mcpApp 'Microsoft.App/containerApps@2024-02-02' = {
  name: 'ldm-mcp-http'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: acaEnv.id
    configuration: {
      ingress: {
        external: publicIngressMcp
        targetPort: mcpPort
        transport: 'auto'
      }
      registries: [
        {
          server: registryServer
          username: registryUsername
          passwordSecretRef: 'regpwd'
        }
      ]
      secrets: [
        {
          name: 'regpwd'
          value: registryPassword
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'mcp'
          image: mcpHttpImage
          resources: {
            cpu: 0.5
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://0.0.0.0:${mcpPort}'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
      }
    }
  }
}

output consoleFqdn string = consoleApp.properties.configuration.ingress.fqdn
output mcpFqdn string     = mcpApp.properties.configuration.ingress.fqdn
```

**Optional params file** (helps local testing):

**File:** `infra/aca.parameters.json`

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "location": { "value": "eastus" },
    "envName": { "value": "ldm-aca-env" },
    "registryServer": { "value": "ghcr.io" },
    "registryUsername": { "value": "YOUR_GH_USERNAME" },
    "registryPassword": { "value": "<token-or-password>" },
    "mcpHttpImage": { "value": "ghcr.io/YOUR_GH_USERNAME/limbodancer-mcp-http:sha-abc1234" },
    "consoleImage": { "value": "ghcr.io/YOUR_GH_USERNAME/limbodancer-console:sha-abc1234" },
    "publicIngressConsole": { "value": true },
    "publicIngressMcp": { "value": true }
  }
}
```

---

## Dockerfiles

### 4) Dockerfiles (multi-stage, .NET 9)

**File:** `src/LimboDancer.MCP.McpServer.Http/Dockerfile`

```dockerfile
# build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ./ ./
RUN dotnet restore
RUN dotnet publish src/LimboDancer.MCP.McpServer.Http/LimboDancer.MCP.McpServer.Http.csproj -c Release -o /app/publish /p:PublishTrimmed=false

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
# Kestrel bound by Bicep env (ASPNETCORE_URLS)
ENV DOTNET_EnableDiagnostics=0
ENTRYPOINT ["dotnet", "LimboDancer.MCP.McpServer.Http.dll"]
```

**File:** `src/LimboDancer.MCP.BlazorConsole/Dockerfile`

```dockerfile
# build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ./ ./
RUN dotnet restore
RUN dotnet publish src/LimboDancer.MCP.BlazorConsole/LimboDancer.MCP.BlazorConsole.csproj -c Release -o /app/publish /p:PublishTrimmed=false

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV DOTNET_EnableDiagnostics=0
ENTRYPOINT ["dotnet", "LimboDancer.MCP.BlazorConsole.dll"]
```

---

## How to use

1. **Push images**: merge to `main` (or tag as `vX.Y.Z`).

2. **Deploy**: run **“deploy-aca”** workflow manually with:

   * `resourceGroup`: your RG
   * `location`: e.g., `eastus`
   * `mcpImage` / `consoleImage`: copy from CI output (e.g., `ghcr.io/OWNER/limbodancer-mcp-http:sha-abc1234`)
   * `registryUsername`: your GH username
   * `registryPasswordSecret`: `GHCR_READ_TOKEN` (repo secret containing PAT with `read:packages`)

3. Workflow prints **FQDNs** for both apps.

---

If you want, I can add **KEDA scale rules**, **managed identity bindings**, and **App Insights** wiring next.

---

Awesome — I’ve added **KEDA scale rules**, **managed identity wiring (system-assigned)**, and **Application Insights (OTEL) export** across infra + code. Everything below is PR-ready.

---

## **KEDA scale rules**

### 1) Bicep: scale rules, App Insights, secrets, env

**Replace** your `infra/aca-main.bicep` with this version (kept your structure, added params and features):

```bicep
@description('Azure region')
param location string

@description('ACA Environment name')
param envName string = 'ldm-aca-env'

@description('Container registry server (e.g. ghcr.io or myacr.azurecr.io)')
param registryServer string

@description('Registry username')
param registryUsername string

@secure()
@description('Registry password or PAT with read access')
param registryPassword string

@description('Full image for MCP HTTP (e.g. ghcr.io/OWNER/limbodancer-mcp-http:sha-abc1234)')
param mcpHttpImage string

@description('Full image for Console (e.g. ghcr.io/OWNER/limbodancer-console:sha-abc1234)')
param consoleImage string

@description('Expose Console publicly')
param publicIngressConsole bool = true

@description('Expose MCP HTTP publicly')
param publicIngressMcp bool = true

@description('Console target port')
param consolePort int = 8080

@description('MCP HTTP target port')
param mcpPort int = 5080

// ---------- NEW: App Insights ----------
@description('Name for Application Insights component')
param appInsightsName string = 'ldm-appinsights'

// ---------- NEW: Service Bus autoscale (optional) ----------
@description('Enable Service Bus queue based scale for MCP')
param enableServiceBusScale bool = false

@description('Service Bus connection string (if enableServiceBusScale = true)')
@secure()
param serviceBusConnectionString string = ''

@description('Service Bus queue name (if enableServiceBusScale = true)')
param serviceBusQueueName string = ''

// --- Log Analytics (required by ACA env) ---
resource law 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${envName}-log'
  location: location
  properties: {
    retentionInDays: 30
    sku: { name: 'PerGB2018' }
  }
}

// --- Application Insights (connected to LAW) ---
resource appi 'microsoft.insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    workspaceResourceId: law.id
    Application_Type: 'other'
    IngestionMode: 'ApplicationInsights'
  }
}

// --- Container Apps Environment ---
resource acaEnv 'Microsoft.App/managedEnvironments@2024-02-02' = {
  name: envName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: law.properties.customerId
        sharedKey: law.listKeys().primarySharedKey
      }
    }
  }
}

// --- Console (Blazor Server) ---
resource consoleApp 'Microsoft.App/containerApps@2024-02-02' = {
  name: 'ldm-console'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: acaEnv.id
    configuration: {
      ingress: {
        external: publicIngressConsole
        targetPort: consolePort
        transport: 'auto'
      }
      registries: [
        {
          server: registryServer
          username: registryUsername
          passwordSecretRef: 'regpwd'
        }
      ]
      secrets: [
        {
          name: 'regpwd'
          value: registryPassword
        },
        // NEW: App Insights connection string
        {
          name: 'appinsights-conn'
          value: appi.properties.ConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'console'
          image: consoleImage
          resources: {
            cpu: 0.5
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://0.0.0.0:${consolePort}'
            },
            // NEW: wire AI connection string for OTEL exporter
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'appinsights-conn'
            }
          ]
        }
      ]
      // NEW: KEDA rules
      scale: {
        minReplicas: 1
        maxReplicas: 3
        rules: [
          // HTTP concurrency scaling (good fit for Blazor Server)
          {
            name: 'http-concurrency'
            http: {
              metadata: {
                concurrentRequests: '60'
              }
            }
          },
          // CPU utilization guardrail
          {
            name: 'cpu-util'
            custom: {
              type: 'cpu'
              metadata: {
                type: 'Utilization'
                value: '70' // target 70% CPU
              }
            }
          }
        ]
      }
    }
  }
}

// --- MCP HTTP server ---
resource mcpApp 'Microsoft.App/containerApps@2024-02-02' = {
  name: 'ldm-mcp-http'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: acaEnv.id
    configuration: {
      ingress: {
        external: publicIngressMcp
        targetPort: mcpPort
        transport: 'auto'
      }
      registries: [
        {
          server: registryServer
          username: registryUsername
          passwordSecretRef: 'regpwd'
        }
      ]
      secrets: [
        {
          name: 'regpwd'
          value: registryPassword
        },
        // NEW: App Insights connection string
        {
          name: 'appinsights-conn'
          value: appi.properties.ConnectionString
        },
        // Optional SB connection for scale rule
        if (enableServiceBusScale) {
          name: 'sb-conn'
          value: serviceBusConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'mcp'
          image: mcpHttpImage
          resources: {
            cpu: 0.5
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://0.0.0.0:${mcpPort}'
            },
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'appinsights-conn'
            }
          ]
        }
      ]
      // NEW: KEDA rules
      scale: {
        minReplicas: 1
        maxReplicas: 10
        rules: [
          {
            name: 'http-concurrency'
            http: {
              metadata: {
                concurrentRequests: '80'
              }
            }
          },
          {
            name: 'cpu-util'
            custom: {
              type: 'cpu'
              metadata: {
                type: 'Utilization'
                value: '70'
              }
            }
          },
          // Optional: Service Bus queue length drives background scale
          if (enableServiceBusScale) {
            name: 'sb-queue-depth'
            custom: {
              type: 'azure-servicebus'
              metadata: {
                queueName: serviceBusQueueName
                messageCount: '5'
              }
              auth: [
                {
                  secretRef: 'sb-conn'
                  triggerParameter: 'connection'
                }
              ]
            }
          }
        ]
      }
    }
  }
}

output consoleFqdn string = consoleApp.properties.configuration.ingress.fqdn
output mcpFqdn string     = mcpApp.properties.configuration.ingress.fqdn
output appInsightsConn string = appi.properties.ConnectionString
```

**Notes**

* **HTTP scaler** keeps latency stable under load.
* **CPU scaler** adds a safety backstop.
* **Service Bus scaler** is **optional** (set `enableServiceBusScale=true` and pass `serviceBusConnectionString` + `serviceBusQueueName`). This uses a connection string for simplicity; we can later swap to **Managed Identity** auth when you’re ready.

---

### 2) .NET: OpenTelemetry + Azure Monitor exporter

#### 2.1 ASP.NET Core (HTTP MCP server)

**`src/LimboDancer.MCP.McpServer.Http/LimboDancer.MCP.McpServer.Http.csproj`** – add OTEL packages:

```xml
<ItemGroup>
  <PackageReference Include="Azure.Monitor.OpenTelemetry.AspNetCore" Version="1.3.0" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.9.0" />
</ItemGroup>
```

**`src/LimboDancer.MCP.McpServer.Http/Program.cs`** – minimal OTEL setup (top of file after builder):

```csharp
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;

var serviceName = "LimboDancer.MCP.Http";
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor() // reads APPLICATIONINSIGHTS_CONNECTION_STRING
    .WithTracing(t => t
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithMetrics(m => m
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

builder.Logging.AddOpenTelemetry(o =>
{
    o.IncludeScopes = false;
    o.ParseStateValues = true;
    o.IncludeFormattedMessage = true;
    o.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
    o.AddAzureMonitorLogExporter(); // uses same connection string
});
```

#### 2.2 Console/stdio MCP server

**`src/LimboDancer.MCP.McpServer/LimboDancer.MCP.McpServer.csproj`** – add OTEL packages:

```xml
<ItemGroup>
  <PackageReference Include="OpenTelemetry" Version="1.9.0" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.9.0" />
  <PackageReference Include="OpenTelemetry.Exporter.AzureMonitor" Version="1.3.0" />
</ItemGroup>
```

**`src/LimboDancer.MCP.McpServer/Program.cs`** – minimal OTEL for worker:

```csharp
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;
using OpenTelemetry.Exporter;

var serviceName = "LimboDancer.MCP.Stdio";
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
        .AddSource("ModelContextProtocol") // if SDK emits ActivitySource, keep; otherwise omit
        .AddHttpClientInstrumentation()
        .AddProcessInstrumentation()
        .AddRuntimeInstrumentation()
        .AddAzureMonitorTraceExporter())
    .WithMetrics(m => m
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddMeter("System.Net.Http")
        .AddAzureMonitorMetricExporter());

builder.Logging.AddOpenTelemetry(o =>
{
    o.IncludeScopes = false;
    o.ParseStateValues = true;
    o.IncludeFormattedMessage = true;
    o.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
    o.AddAzureMonitorLogExporter();
});
```

> Both apps read `APPLICATIONINSIGHTS_CONNECTION_STRING` (set via Bicep secrets). You’ll see **traces, metrics, logs** in App Insights with service names `LimboDancer.MCP.Http` and `LimboDancer.MCP.Stdio`.

---

### 3) Managed identity (RBAC) quick-bind (post-deploy)

System-assigned identities are already enabled in Bicep. Grant the apps data-plane access to Azure resources with these **one-liners** (adjust scopes):

```bash
# Get principal IDs
CONSOLE_ID=$(az containerapp show -g <rg> -n ldm-console   --query identity.principalId -o tsv)
MCP_ID=$(az containerapp show     -g <rg> -n ldm-mcp-http --query identity.principalId -o tsv)

# Cosmos DB (data-plane)
az role assignment create --assignee $MCP_ID --role "Cosmos DB Built-in Data Contributor" --scope /subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.DocumentDB/databaseAccounts/<cosmosAccount>

# Azure AI Search (index data contributor)
az role assignment create --assignee $MCP_ID --role "Search Index Data Contributor" --scope /subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.Search/searchServices/<searchService>

# Service Bus (if consuming)
az role assignment create --assignee $MCP_ID --role "Azure Service Bus Data Receiver" --scope /subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.ServiceBus/namespaces/<sbNamespace>

# Blob Storage (ingestion artifacts)
az role assignment create --assignee $MCP_ID --role "Storage Blob Data Contributor" --scope /subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.Storage/storageAccounts/<storageAccount>
```

> After RBAC propagates (can take \~1–5 min), switch your code from connection strings to **AAD/MSI** (e.g., Azure SDK clients with `DefaultAzureCredential`). For the **KEDA Service Bus** scaler, we used a connection string for now; we can later convert that scaler to **identity-based auth**.

---

### 4) CI/CD tweaks

No changes are required in the CI pipelines. The **deploy workflow** already runs the Bicep with your images. To enable SB autoscale, pass:

* `enableServiceBusScale: true`
* `serviceBusConnectionString: <SB connection string>`
* `serviceBusQueueName: <queue name>`

---

### 5) Sanity checklist

* [ ] Merged csproj changes (add OTEL packages).
* [ ] Merged Program.cs changes (OTEL setup) in both servers.
* [ ] Rebuilt and pushed images (CI will do this on `main`).
* [ ] Ran **Deploy ACA** workflow with your image tags and region.
* [ ] Validated App Insights traces show up (Search by operation name or service name).
* [ ] (Optional) Enabled Service Bus scale and confirmed autoscale events.

---

If you want, I can also add:

* **KEDA scale rules for Event Grid or Azure Monitor** (e.g., requests per second),
* **Connection string–free SB scaler** using **Managed Identity**, and
* A **dashboard workbook** for App Insights showing tool latency, precondition failures, and HTTP percentiles.


---  
  
The following is:
* KEDA scale rules for **Event Grid (via Azure Monitor)** with RPS-style thresholds
* A **connection string–free** Service Bus scaler using **Managed Identity**
* An **Application Insights Workbook** (KQL + importable JSON) for tool latency, precondition failures, and HTTP percentiles

I’ve added short “why” notes and citations inline so future-you knows why each choice was made.

---

## KEDA scale rules (Container Apps)

> Container Apps exposes KEDA scalers as “custom” scale rules. Managed identity is set on each rule via the `identity` property (not `auth`). ([Microsoft Learn][1])

### 1) Event Grid (push) via **Azure Monitor** scaler — scale on published events per second

> There’s no native Event Grid scaler yet; use the `azure-monitor` KEDA scaler against Event Grid **topic** metrics such as `PublishSuccessCount`. (Counts are per minute; for “RPS” thresholds, multiply your per-second target by 60.) ([GitHub][2], [KEDA][3], [Microsoft Learn][4])

```bicep
// Container App excerpt (apiVersion 2025-02-02-preview or newer)
resource mcp 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'mcp-server'
  location: resourceGroup().location
  identity: { type: 'SystemAssigned' } // required to use rule-level identity: 'system'
  properties: {
    template: {
      containers: [
        {
          name: 'server'
          image: 'ghcr.io/your/image:tag'
          probes: []
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 20
        rules: [
          {
            name: 'eg-publish-rps'
            custom: {
              type: 'azure-monitor'
              // Use system MI for metric auth (Monitoring Reader permission is required on the target resource)
              identity: 'system'
              metadata: {
                // Event Grid Topic resource (push)
                // Format: <RP>/<Type>/<Name>
                // See: azure-monitor scaler 'resourceURI' format
                resourceURI: 'Microsoft.EventGrid/topics/my-eg-topic' // or 'Microsoft.EventGrid/domains/my-eg-domain'
                tenantId: subscription().tenantId
                subscriptionId: subscription().subscriptionId
                resourceGroupName: resourceGroup().name

                // Metric names taken from supported metrics table
                // (examples: PublishSuccessCount, PublishFailCount, DeliverySuccessCount)
                metricName: 'PublishSuccessCount' // published events (per-minute count)
                metricAggregationType: 'Total'    // match the docs' default aggregation
                metricAggregationInterval: '0:1:0' // 1 minute

                // “Requests per second” target (e.g., 30 rps) => 30 * 60 = 1800 per minute
                targetValue: '1800'
                // Optional activation threshold (avoid cold starts on trickle)
                activationTargetValue: '60'
              }
            }
          }
        ]
      }
    }
  }
}
```

**Why this works**

* `azure-monitor` scaler fields are per KEDA spec (`resourceURI`, `metricName`, `metricAggregationInterval`, `targetValue`). ([KEDA][3])
* Event Grid topic metric REST names include `PublishSuccessCount`, `DeliverySuccessCount`, etc. (use whichever best maps to your scenario). ([Microsoft Learn][4])

### 2) Event Grid **Namespace (pull)** via **Azure Monitor** scaler — scale on received/acknowledged events

> For pull delivery, point at the **namespace** and use metrics such as `SuccessfulReceivedEvents` or `SuccessfulAcknowledgedEvents`. Dimension filters (like `EventSubscriptionName`) help isolate a specific subscription. ([Microsoft Learn][5])

```bicep
rules: [
  {
    name: 'eg-pull-rps'
    custom: {
      type: 'azure-monitor'
      identity: 'system'
      metadata: {
        resourceURI: 'Microsoft.EventGrid/namespaces/my-eg-namespace'
        tenantId: subscription().tenantId
        subscriptionId: subscription().subscriptionId
        resourceGroupName: resourceGroup().name

        metricName: 'SuccessfulReceivedEvents' // per-minute count
        metricAggregationType: 'Total'
        metricAggregationInterval: '0:1:0'
        // Optional: narrow to one subscription or topic
        // Use dimension names listed in the docs (e.g., Topic, EventSubscriptionName)
        metricFilter: "EventSubscriptionName eq 'mcp-tool-executor'"
        targetValue: '1200' // 20 rps => 1200 /min
        activationTargetValue: '60'
      }
    }
  }
]
```

### 3) HTTP concurrency (built-in)

> For pure HTTP load on the MCP server, prefer the built-in HTTP rule (scales to zero; simpler than CPU/memory). Keep it alongside the Azure Monitor rules if you want both. ([Microsoft Learn][6])

```bicep
rules: [
  {
    name: 'http-concurrency'
    http: {
      metadata: { concurrentRequests: '50' } // add a replica when >50 concurrent reqs
      identity: 'system'
    }
  }
]
```

---

## Service Bus scaler (no connection string) via **Managed Identity**

> In Container Apps, you set **`custom.identity`** on the rule (for MI) and put **`namespace`** in metadata. Assign an RBAC role like **Azure Service Bus Data Receiver** to the app’s managed identity at the namespace (or queue) scope. ([Microsoft Learn][1], [KEDA][7])

```bicep
resource mcp 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'mcp-worker'
  location: resourceGroup().location
  identity: { type: 'SystemAssigned' }
  properties: {
    template: {
      containers: [
        { name: 'worker'; image: 'ghcr.io/your/worker:tag' }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 30
        rules: [
          {
            name: 'sb-queue-mi'
            custom: {
              type: 'azure-servicebus'
              identity: 'system' // <- MI auth (no secrets)
              metadata: {
                // Required with MI:
                namespace: 'my-sb-namespace'
                queueName: 'mcp-tasks'
                messageCount: '10'          // add a replica when >= 10 active msgs
                activationMessageCount: '2'  // optional warm activation
              }
            }
          }
        ]
      }
    }
  }
}

// Optional RBAC: grant the Container App's **system-assigned** identity Data Receiver at SB namespace scope
@description('Service Bus namespace resource ID')
param sbNamespaceId string

// Azure Service Bus Data Receiver (built-in role) — roleDefinitionId GUID:
var sbDataReceiverRoleId = '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0' // reference: azadvertizer roles
resource sbReceiverRA 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(sbNamespaceId, 'mcp-worker', sbDataReceiverRoleId)
  scope: sbNamespaceId
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', sbDataReceiverRoleId)
    principalId: mcp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}
```

> Notes
> • MI on the rule requires Container Apps API that supports `scale.rules.custom.identity`. (Docs call it out explicitly.) ([Microsoft Learn][1])
> • RBAC options: Data Receiver (listen only), Data Sender (send), or Data Owner (full). Pick the least privilege needed. ([Microsoft Learn][8])

---

## App Insights Workbook — Tool latency, precondition failures, HTTP percentiles

Below are **KQL queries** plus a minimal **Workbook JSON** you can import via *Workbooks → New → Advanced editor → Gallery Template*.

### Parameters (use across queries)

```kusto
let Role = 'mcp-server';      // cloud_RoleName for the Container App
let ToolPrefix = 'Tool:';     // if you prefix request names like "Tool:VectorSearch"
```

### A) Tool latency percentiles (p50/p90/p95/p99)

```kusto
let span = 1m;
union isfuzzy=true
  requests
    | where cloud_RoleName == Role
    | where name startswith ToolPrefix or url has "/tools/invoke"
    | extend tool = tostring(coalesce(customDimensions['toolName'], name))
    | project timestamp, tool, duration
, customEvents
    | where name == "mcp_tool_invoke"
    | extend tool = tostring(customDimensions['toolName'])
    | extend duration = toduration(tolong(tostring(customMeasurements['durationMs']))*1ms)
    | project timestamp, tool, duration
| summarize p50=percentile(duration,50), p90=percentile(duration,90), p95=percentile(duration,95), p99=percentile(duration,99)
  by bin(timestamp, span), tool
| order by timestamp asc
```

### B) Precondition failures over time

```kusto
let span = 5m;
union isfuzzy=true
  traces
    | where cloud_RoleName == Role
    | where message has "PreconditionFailed" or tostring(customDimensions['precondition']) == "Failed"
    | extend tool = tostring(customDimensions['toolName'])
    | project timestamp, tool
, exceptions
    | where cloud_RoleName == Role
    | where type endswith "PreconditionFailedException" or tostring(customDimensions['precondition']) == "Failed"
    | extend tool = tostring(customDimensions['toolName'])
    | project timestamp, tool
, customEvents
    | where name == "mcp_precondition_failed"
    | extend tool = tostring(customDimensions['toolName'])
    | project timestamp, tool
| summarize failures = count() by bin(timestamp, span), coalesce(tool, 'unknown')
| order by timestamp asc
```

### C) HTTP percentiles by operation/route

```kusto
let span = 1m;
requests
| where cloud_RoleName == Role
| summarize
    p50=percentile(duration,50),
    p90=percentile(duration,90),
    p95=percentile(duration,95),
    p99=percentile(duration,99)
  by bin(timestamp, span), operation_Name
| order by timestamp asc
```

#### Optional “last 24h by tool” summary table

```kusto
requests
| where cloud_RoleName == Role
| where timestamp > ago(24h)
| extend tool = tostring(coalesce(customDimensions['toolName'], name))
| summarize count_=count(), p50=percentile(duration,50), p95=percentile(duration,95), p99=percentile(duration,99)
  by tool
| order by p95 desc
```

### Minimal Workbook JSON (import)

> This uses three KQL tiles. After import, set the Workbook time range as needed.

```json
{
  "version": "Notebook/1.0",
  "items": [
    {
      "type": 1,
      "content": {
        "json": {
          "version": "KqlItem/1.0",
          "query": "/* Tool latency percentiles */\nlet Role = 'mcp-server'; let ToolPrefix = 'Tool:'; let span = 1m;\nunion isfuzzy=true\n  requests | where cloud_RoleName == Role | where name startswith ToolPrefix or url has \"/tools/invoke\" | extend tool = tostring(coalesce(customDimensions['toolName'], name)) | project timestamp, tool, duration\n, customEvents | where name == \"mcp_tool_invoke\" | extend tool = tostring(customDimensions['toolName']) | extend duration = toduration(tolong(tostring(customMeasurements['durationMs']))*1ms) | project timestamp, tool, duration\n| summarize p50=percentile(duration,50), p90=percentile(duration,90), p95=percentile(duration,95), p99=percentile(duration,99) by bin(timestamp, span), tool | order by timestamp asc",
          "size": 1,
          "visualization": "timechart",
          "title": "Tool latency percentiles"
        }
      },
      "name": "tool-latency"
    },
    {
      "type": 1,
      "content": {
        "json": {
          "version": "KqlItem/1.0",
          "query": "/* Precondition failures */\nlet Role = 'mcp-server'; let span = 5m;\nunion isfuzzy=true\n  traces | where cloud_RoleName == Role | where message has \"PreconditionFailed\" or tostring(customDimensions['precondition']) == \"Failed\" | extend tool = tostring(customDimensions['toolName']) | project timestamp, tool\n, exceptions | where cloud_RoleName == Role | where type endswith \"PreconditionFailedException\" or tostring(customDimensions['precondition']) == \"Failed\" | extend tool = tostring(customDimensions['toolName']) | project timestamp, tool\n, customEvents | where name == \"mcp_precondition_failed\" | extend tool = tostring(customDimensions['toolName']) | project timestamp, tool\n| summarize failures = count() by bin(timestamp, span), coalesce(tool, 'unknown') | order by timestamp asc",
          "size": 1,
          "visualization": "timechart",
          "title": "Precondition failures (count)"
        }
      },
      "name": "preconditions"
    },
    {
      "type": 1,
      "content": {
        "json": {
          "version": "KqlItem/1.0",
          "query": "/* HTTP percentiles by operation */\nlet Role = 'mcp-server'; let span = 1m;\nrequests | where cloud_RoleName == Role\n| summarize p50=percentile(duration,50), p90=percentile(duration,90), p95=percentile(duration,95), p99=percentile(duration,99) by bin(timestamp, span), operation_Name\n| order by timestamp asc",
          "size": 1,
          "visualization": "timechart",
          "title": "HTTP percentiles by operation"
        }
      },
      "name": "http-percentiles"
    }
  ],
  "isLocked": false
}
```

---

## Quick validation checklist

* **Event Grid (push/pull) metric names** you can use (REST names):
  `PublishSuccessCount`, `PublishFailCount`, `DeliverySuccessCount`, `MatchedEventCount`, `SuccessfulReceivedEvents`, `SuccessfulAcknowledgedEvents`, etc. (see Microsoft.EventGrid/\* supported metrics tables). ([Microsoft Learn][4])
* **Azure Monitor scaler fields** (`resourceURI`, `metricName`, `metricAggregationType`, etc.) per KEDA docs. ([KEDA][3])
* **Managed identity on scale rules** uses the `identity` field on the rule, value `'system'` or a user-assigned identity resource ID. ([Microsoft Learn][1])
* **RBAC for Service Bus**: assign **Data Receiver**/**Sender**/**Data Owner** as needed to the Container App’s identity. ([Microsoft Learn][8])

If you want, I can tailor these snippets to your exact resource names (topic/namespace), or wire the workbook as a deployable `Microsoft.Insights/workbooks` Bicep resource.

[1]: https://learn.microsoft.com/en-us/azure/templates/microsoft.app/containerapps "Microsoft.App/containerApps - Bicep, ARM template & Terraform AzAPI reference | Microsoft Learn"
[2]: https://github.com/kedacore/keda/issues/5495?utm_source=chatgpt.com "Provide scaler for Azure Event Grid Namespace Topics (Pull)"
[3]: https://keda.sh/docs/2.17/scalers/azure-monitor/ "Azure Monitor | KEDA"
[4]: https://learn.microsoft.com/en-us/azure/azure-monitor/reference/supported-metrics/microsoft-eventgrid-topics-metrics "Supported metrics - Microsoft.EventGrid/topics - Azure Monitor | Microsoft Learn"
[5]: https://learn.microsoft.com/en-us/azure/azure-monitor/reference/supported-metrics/microsoft-eventgrid-namespaces-metrics "Supported metrics - Microsoft.EventGrid/namespaces - Azure Monitor | Microsoft Learn"
[6]: https://learn.microsoft.com/en-us/azure/container-apps/tutorial-scaling?utm_source=chatgpt.com "Tutorial: Scale an Azure Container Apps application"
[7]: https://keda.sh/docs/2.11/scalers/azure-service-bus/ "Azure Service Bus | KEDA"
[8]: https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-managed-service-identity?utm_source=chatgpt.com "Managed identities for Azure resources with Service Bus"
