using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using LimboDancer.MCP.Ontology.JsonLd;
using LimboDancer.MCP.Ontology.Repositories;
using LimboDancer.MCP.Ontology.Runtime;
using LimboDancer.MCP.Ontology.Store;

namespace LimboDancer.MCP.Ontology.Export;

/// <summary>
/// Exports an ontology channel to a scoped JSON-LD bundle.
/// </summary>
public sealed class JsonLdExportService
{
    private readonly IOntologyRepository _repo;

    public JsonLdExportService(IOntologyRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Export the ontology for the given scope into a JSON-LD document (as a JsonObject).
    /// </summary>
    public async Task<JsonObject> ExportAsync(TenantScope scope, string baseNamespace, CancellationToken ct = default)
    {
        var store = new OntologyStore(_repo);
        await store.LoadAsync(scope, ct).ConfigureAwait(false);

        if (!baseNamespace.EndsWith("#") && !baseNamespace.EndsWith("/")) baseNamespace += "#";

        var context = JsonLdContextBuilder.BuildContext(scope, baseNamespace);

        var doc = new JsonObject
        {
            ["@context"] = context,
            ["scope"] = scope.ToString(),
            ["entities"] = new JsonArray(store.Entities().Select(e => (JsonNode)e.LocalName).ToArray()),
            ["properties"] = new JsonArray(store.Properties().Select(p => (JsonNode)new JsonObject
            {
                ["owner"] = p.OwnerEntity,
                ["name"] = p.LocalName,
                ["range"] = p.Range.Value,
                ["required"] = p.MinCardinality > 0
            }).ToArray()),
            ["relations"] = new JsonArray(store.Relations().Select(r => (JsonNode)new JsonObject
            {
                ["name"] = r.LocalName,
                ["from"] = r.FromEntity,
                ["to"] = r.ToEntity,
                ["min"] = r.MinCardinality,
                ["max"] = r.MaxCardinality
            }).ToArray()),
            ["enums"] = new JsonArray(store.Enums().Select(en => (JsonNode)new JsonObject
            {
                ["name"] = en.LocalName,
                ["values"] = new JsonArray(en.Values.Select(v => (JsonNode)v).ToArray())
            }).ToArray()),
            ["aliases"] = new JsonArray(store.Aliases().Select(a => (JsonNode)new JsonObject
            {
                ["canonical"] = a.Canonical,
                ["locale"] = a.Locale,
                ["aliases"] = new JsonArray(a.Aliases.Select(v => (JsonNode)v).ToArray())
            }).ToArray()),
            ["shapes"] = new JsonArray(store.Shapes().Select(s => (JsonNode)new JsonObject
            {
                ["appliesTo"] = s.AppliesToEntity,
                ["constraints"] = new JsonArray(s.PropertyConstraints.Select(pc => (JsonNode)new JsonObject
                {
                    ["property"] = pc.Property,
                    ["expectedRange"] = pc.ExpectedRange,
                    ["min"] = pc.MinCardinality,
                    ["max"] = pc.MaxCardinality,
                    ["pattern"] = pc.Pattern,
                    ["in"] = pc.In is null ? null : new JsonArray(pc.In.Select(v => (JsonNode)v).ToArray())
                }).ToArray())
            }).ToArray())
        };

        return doc;
    }

    /// <summary>
    /// Export the ontology for the given scope into a JSON-LD document as a string.
    /// </summary>
    public async Task<string> ExportStringAsync(TenantScope scope, string baseNamespace, bool indented = true, CancellationToken ct = default)
    {
        var json = await ExportAsync(scope, baseNamespace, ct).ConfigureAwait(false);
        return json.ToJsonString(new JsonSerializerOptions { WriteIndented = indented });
    }
}