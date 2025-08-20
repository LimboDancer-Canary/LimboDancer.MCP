using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LimboDancer.MCP.Ontology.Repositories;
using LimboDancer.MCP.Ontology.Runtime;
using LimboDancer.MCP.Ontology.Store;

namespace LimboDancer.MCP.Ontology.Export;

/// <summary>
/// Exports an ontology channel to a compact Turtle (TTL) representation.
/// Intended as a minimal, readable snapshot; not a complete OWL export.
/// </summary>
public sealed class RdfExportService
{
    private readonly IOntologyRepository _repo;

    public RdfExportService(IOntologyRepository repo)
    {
        _repo = repo;
    }

    public async Task<string> ExportTurtleAsync(TenantScope scope, string baseNamespace, CancellationToken ct = default)
    {
        var store = new OntologyStore(_repo);
        await store.LoadAsync(scope, ct).ConfigureAwait(false);

        baseNamespace = EnsureNs(baseNamespace);

        var sb = new StringBuilder();
        sb.AppendLine($"@prefix ldm: <{baseNamespace}> .");
        sb.AppendLine("@prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .");
        sb.AppendLine("@prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .");
        sb.AppendLine("@prefix xsd: <http://www.w3.org/2001/XMLSchema#> .");
        sb.AppendLine("@prefix owl: <http://www.w3.org/2002/07/owl#> .");
        sb.AppendLine();

        // Classes
        foreach (var e in store.Entities().OrderBy(e => e.LocalName))
        {
            sb.AppendLine($"ldm:{e.LocalName} a rdfs:Class .");
        }
        sb.AppendLine();

        // Datatype/Object properties (properties are datatypes or entity refs)
        foreach (var p in store.Properties().OrderBy(p => p.OwnerEntity).ThenBy(p => p.LocalName))
        {
            var range = p.Range.Kind == RangeKind.XsdDatatype ? MapXsd(p.Range.Value) : $"ldm:{p.Range.Value}";
            sb.AppendLine($"ldm:{p.LocalName} a rdf:Property ;");
            sb.AppendLine($"  rdfs:domain ldm:{p.OwnerEntity} ;");
            sb.AppendLine($"  rdfs:range {range} .");
        }
        sb.AppendLine();

        // Relations as object properties
        foreach (var r in store.Relations().OrderBy(r => r.LocalName))
        {
            sb.AppendLine($"ldm:{r.LocalName} a owl:ObjectProperty ;");
            sb.AppendLine($"  rdfs:domain ldm:{r.FromEntity} ;");
            sb.AppendLine($"  rdfs:range ldm:{r.ToEntity} .");
        }
        sb.AppendLine();

        // Enums as simple RDFS classes with individuals (optional minimal form)
        foreach (var en in store.Enums().OrderBy(e => e.LocalName))
        {
            var enumClass = $"ldm:{en.LocalName}";
            sb.AppendLine($"{enumClass} a rdfs:Class .");
            foreach (var v in en.Values)
            {
                var ind = $"ldm:{SanitizeLocal(v)}";
                sb.AppendLine($"{ind} a {enumClass} .");
            }
        }

        return sb.ToString();
    }

    private static string EnsureNs(string ns)
        => ns.EndsWith("#") || ns.EndsWith("/") ? ns : ns + "#";

    private static string MapXsd(string value)
    {
        // value may be "xsd:string" or an absolute XSD URI
        if (value.StartsWith("xsd:", System.StringComparison.Ordinal)) return value.Replace("xsd:", "xsd:");
        if (value.StartsWith("http://www.w3.org/2001/XMLSchema#", System.StringComparison.Ordinal)) return "xsd:" + value.Substring("http://www.w3.org/2001/XMLSchema#".Length);
        return "xsd:string";
    }

    private static string SanitizeLocal(string v)
    {
        var s = new string(v.Where(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrWhiteSpace(s) ? "Value" : s;
    }
}