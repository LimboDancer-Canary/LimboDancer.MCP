using Gremlin.Net.Driver;
using LimboDancer.MCP.Core.Tenancy;

namespace LimboDancer.MCP.Graph.CosmosGremlin;

/// <summary>
/// Minimal, tenant-safe graph store for Cosmos Gremlin.
/// Strategy:
///  - Vertex IDs are prefixed with tenant (N-format Guid): "{tenantN}:{localId}"
///  - Every vertex/edge also carries a 'tenantId' string property (tenant Guid N-format)
///  - Every traversal guards with has('tenantId', t)
/// </summary>
public sealed class GraphStore
{
    private readonly GremlinClient _g;
    private readonly ITenantAccessor _tenant;

    public GraphStore(GremlinClient client, ITenantAccessor tenant)
    {
        _g = client ?? throw new ArgumentNullException(nameof(client));
        _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
    }

    private string T() => _tenant.TenantId.ToString("N");
    private string VId(string localId) => $"{T()}:{localId}";

    /// <summary>Upsert a vertex with a label and properties. 'localId' is tenant-local; the method prefixes it.</summary>
    public async Task UpsertVertexAsync(string localId, string label, IDictionary<string, object?>? props = null, CancellationToken ct = default)
    {
        props ??= new Dictionary<string, object?>();
        props["tenantId"] = T();

        var vid = VId(localId);
        var propAssign = GraphWriteHelpers.BuildPropAssignments(props);

        // Upsert pattern: fold()/coalesce(addV) then property assignments
        var script = $@"
                        g.V('{vid}').fold().
                          coalesce(unfold(), addV('{label}').property(id,'{vid}').property('tenantId','{T()}'))
                          {propAssign}
";

        await _g.SubmitAsync<dynamic>(script, cancellationToken: ct);
    }

    /// <summary>Add or replace an edge between two local vertices (same tenant).</summary>
    public async Task UpsertEdgeAsync(string outLocalId, string edgeLabel, string inLocalId, IDictionary<string, object?>? props = null, CancellationToken ct = default)
    {
        props ??= new Dictionary<string, object?>();
        props["tenantId"] = T();

        var outVid = VId(outLocalId);
        var inVid = VId(inLocalId);
        var propAssign = GraphWriteHelpers.BuildPropAssignments(props);

        // Remove existing edges with same label between those nodes for idempotence, then add
        var script = $@"
g.V('{outVid}').has('tenantId','{T()}')
 .outE('{edgeLabel}').has('tenantId','{T()}').where(inV().hasId('{inVid}').has('tenantId','{T()}')).drop();
g.V('{outVid}').has('tenantId','{T()}')
 .addE('{edgeLabel}').property('tenantId','{T()}'){propAssign}
 .to(g.V('{inVid}').has('tenantId','{T()}'))
";

        await _g.SubmitAsync<dynamic>(script, cancellationToken: ct);
    }

    /// <summary>Get a vertex property for a tenant-scoped vertex.</summary>
    public async Task<string?> GetVertexPropertyAsync(string localId, string property, CancellationToken ct = default)
    {
        var vid = VId(localId);
        var script = $@"g.V('{vid}').has('tenantId','{T()}').values('{property}').limit(1)";
        var result = await _g.SubmitAsync<dynamic>(script, cancellationToken: ct);
        return result.Count > 0 ? result[0]?.ToString() : null;
    }

    /// <summary>Check if an edge exists between two tenant-scoped vertices.</summary>
    public async Task<bool> EdgeExistsAsync(string outLocalId, string edgeLabel, string inLocalId, CancellationToken ct = default)
    {
        var outVid = VId(outLocalId);
        var inVid = VId(inLocalId);
        var script = $@"
g.V('{outVid}').has('tenantId','{T()}')
 .outE('{edgeLabel}').has('tenantId','{T()}')
 .where(inV().hasId('{inVid}').has('tenantId','{T()}'))
 .limit(1).count()
";
        var result = await _g.SubmitAsync<long>(script, cancellationToken: ct);
        return result.Count > 0 && result[0] > 0;
    }

    /// <summary>Return the in-neighbors connected via edgeLabel (tenant-only).</summary>
    public async Task<IReadOnlyList<string>> GetNeighborsAsync(string localId, string edgeLabel, CancellationToken ct = default)
    {
        var vid = VId(localId);
        var script = $@"g.V('{vid}').has('tenantId','{T()}').outE('{edgeLabel}').has('tenantId','{T()}').inV().values('id')";
        var ids = await _g.SubmitAsync<string>(script, cancellationToken: ct);
        // Strip the "{tenantN}:" prefix to return local ids
        var prefix = T() + ":";
        return ids.Select(id => id.StartsWith(prefix, StringComparison.Ordinal) ? id[prefix.Length..] : id).ToList();
    }
}
