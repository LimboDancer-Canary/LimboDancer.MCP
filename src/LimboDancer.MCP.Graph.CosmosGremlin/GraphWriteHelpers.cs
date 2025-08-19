using Gremlin.Net.Driver;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure;

namespace LimboDancer.MCP.Graph.CosmosGremlin;

/// <summary>
/// Minimal helpers for idempotent vertex/edge creation (upsert).
/// Read-sides can be added later.
/// </summary>
public static class GraphWriteHelpers
{
    /// <summary>
    /// Upsert a vertex by id + label with optional properties.
    /// Pattern: g.V(id).fold().coalesce(unfold(), addV(label).property(id,id)).property(k,v)...
    /// </summary>
    public static async Task UpsertVertexAsync(
        IGremlinClient client,
        string id,
        string label,
        IReadOnlyDictionary<string, object>? properties = null,
        CancellationToken ct = default)
    {
        // build gremlin script
        var gremlin = "g.V(id).fold().coalesce(unfold()," +
                      " addV(label).property('id', id))";

        // Add .property(k, p_k) for each provided property
        var bindings = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["label"] = label
        };

        if (properties is { Count: > 0 })
        {
            int i = 0;
            foreach (var (k, v) in properties)
            {
                var keyParam = $"k{i}";
                var valParam = $"v{i}";
                gremlin += $".property({keyParam},{valParam})";
                bindings[keyParam] = k;
                bindings[valParam] = v;
                i++;
            }
        }

        await client.SubmitAsync<dynamic>(gremlin, bindings, ct);
    }

    /// <summary>
    /// Upsert a directed edge (from -> to) with label and optional properties.
    /// Pattern:
    /// g.V(fromId).as('a')
    ///  .V(toId).as('b')
    ///  .select('a').coalesce(
    ///      outE(label).where(inV().hasId(toId)),
    ///      addE(label).to(select('b'))
    ///  ) .property(k,v)...
    /// </summary>
    public static async Task UpsertEdgeAsync(
        IGremlinClient client,
        string @from,
        string to,
        string label,
        IReadOnlyDictionary<string, object>? properties = null,
        CancellationToken ct = default)
    {
        var gremlin =
            "g.V(fromId).as('a').V(toId).as('b')" +
            ".select('a').coalesce(" +
                "outE(label).where(inV().hasId(toId))," +
                "addE(label).to(select('b'))" +
            ")";

        var bindings = new Dictionary<string, object?>
        {
            ["fromId"] = from,
            ["toId"] = to,
            ["label"] = label
        };

        if (properties is { Count: > 0 })
        {
            int i = 0;
            foreach (var (k, v) in properties)
            {
                var keyParam = $"k{i}";
                var valParam = $"v{i}";
                gremlin += $".property({keyParam},{valParam})";
                bindings[keyParam] = k;
                bindings[valParam] = v;
                i++;
            }
        }

        await client.SubmitAsync<dynamic>(gremlin, bindings, ct);
    }
}
