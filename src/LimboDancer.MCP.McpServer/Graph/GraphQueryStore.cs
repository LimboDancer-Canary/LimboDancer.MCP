using Gremlin.Net.Driver;
using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.Graph.CosmosGremlin;
using LimboDancer.MCP.McpServer.Tools;
using Microsoft.Extensions.Logging;

namespace LimboDancer.MCP.McpServer.Graph
{
    public sealed class GraphQueryStore : IGraphQueryStore
    {
        private readonly IGremlinClient _client;
        private readonly ITenantAccessor _tenant;
        private readonly ILogger<GraphQueryStore> _logger;

        public GraphQueryStore(IGremlinClient client, ITenantAccessor tenant, ILogger<GraphQueryStore> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GraphQueryResult> QueryAsync(GraphQueryRequest request, CancellationToken ct = default)
        {
            var tenantId = _tenant.TenantId;
            var vertices = new List<VertexProjection>();

            // Start with subject IDs or all vertices
            var query = "g";
            var bindings = new Dictionary<string, object>
            {
                ["tid"] = tenantId.ToString("D"),
                ["tprop"] = GraphWriteHelpers.TenantPropertyName
            };

            if (request.SubjectIds?.Count > 0)
            {
                // Start from specific vertices
                var vertexIds = request.SubjectIds.Select(localId => GraphWriteHelpers.ToVertexId(tenantId, localId)).ToList();
                query += ".V(vids).has(tprop, tid)";
                bindings["vids"] = vertexIds;
            }
            else
            {
                // Start from all tenant vertices
                query += ".V().has(tprop, tid)";
            }

            // Apply property filters
            if (request.Filters?.Count > 0)
            {
                foreach (var filter in request.Filters)
                {
                    switch (filter.Op.ToLowerInvariant())
                    {
                        case "eq":
                            query += $".has('{filter.PropertyKey}', filterValue_{filter.PropertyKey})";
                            bindings[$"filterValue_{filter.PropertyKey}"] = filter.Value ?? "";
                            break;
                        case "neq":
                            query += $".not(has('{filter.PropertyKey}', filterValue_{filter.PropertyKey}))";
                            bindings[$"filterValue_{filter.PropertyKey}"] = filter.Value ?? "";
                            break;
                        case "exists":
                            query += $".has('{filter.PropertyKey}')";
                            break;
                        case "not_exists":
                            query += $".not(has('{filter.PropertyKey}'))";
                            break;
                    }
                }
            }

            // Apply traversals
            if (request.Traverse?.Count > 0)
            {
                foreach (var step in request.Traverse)
                {
                    for (int i = 0; i < step.Hops; i++)
                    {
                        if (step.Direction == "out")
                            query += $".out('{step.EdgeLabel}')";
                        else if (step.Direction == "in")
                            query += $".in('{step.EdgeLabel}')";
                        else
                            query += $".both('{step.EdgeLabel}')";

                        // Ensure we stay within tenant
                        query += ".has(tprop, tid)";
                    }
                }
            }

            // Apply limit
            query += $".limit({request.Limit})";

            // Project properties
            query += ".project('id', 'label', 'properties')" +
                     ".by(id())" +
                     ".by(label())" +
                     ".by(valueMap())";

            _logger.LogDebug("Executing Gremlin query: {Query} with bindings: {Bindings}", query, bindings);

            var results = await _client.SubmitAsync<dynamic>(query, bindings, cancellationToken: ct);

            foreach (var result in results)
            {
                if (result is IDictionary<string, object> map)
                {
                    var id = map["id"]?.ToString() ?? "";
                    var label = map["label"]?.ToString() ?? "";
                    var props = new Dictionary<string, object?>();

                    if (map["properties"] is IDictionary<string, object> propMap)
                    {
                        foreach (var kvp in propMap)
                        {
                            // Gremlin returns property values as arrays
                            if (kvp.Value is IList<object> list && list.Count > 0)
                                props[kvp.Key] = list[0];
                            else
                                props[kvp.Key] = kvp.Value;
                        }
                    }

                    // Extract local ID from composite vertex ID
                    var localId = GraphWriteHelpers.GetLocalId(id, tenantId);
                    props["__id"] = localId;
                    props["__label"] = label;

                    vertices.Add(new VertexProjection
                    {
                        Id = localId,
                        Properties = props
                    });
                }
            }

            return new GraphQueryResult
            {
                Vertices = vertices,
                NextCursor = vertices.Count >= request.Limit ? "next" : null // Simplified cursor
            };
        }
    }
}