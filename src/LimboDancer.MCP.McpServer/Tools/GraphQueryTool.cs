using LimboDancer.MCP.Core.Tenancy;
using LimboDancer.MCP.Graph.CosmosGremlin;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LimboDancer.MCP.McpServer.Tools;

public sealed class GraphQueryTool : IMcpTool
{
    public string Name => "graph.query";
    public Tool ToolDescriptor { get; }

    private readonly GraphStore _graph;
    private readonly ITenantAccessor _tenant;

    public GraphQueryTool(GraphStore graph, ITenantAccessor tenant)
    {
        _graph = graph;
        _tenant = tenant;

        ToolDescriptor = new Tool
        {
            Name = Name,
            Description = "Query the knowledge graph (read-only). Tenancy is enforced by server context.",
            InputSchema = ToolSchema.Build((schema, props, req) =>
            {
                ToolSchema.Prop(props, "mode", "string", "getProperty | edgeExists");

                // getProperty
                ToolSchema.Prop(props, "subjectId", "string", "Vertex local id (without tenant prefix)", ontologyId: "ldm:id");
                ToolSchema.Prop(props, "property", "string", "Property key to read", ontologyId: "ldm:property");

                // edgeExists
                ToolSchema.Prop(props, "outId", "string", "Out-vertex local id", ontologyId: "ldm:id");
                ToolSchema.Prop(props, "edgeLabel", "string", "Edge label", ontologyId: "ldm:relation");
                ToolSchema.Prop(props, "inId", "string", "In-vertex local id", ontologyId: "ldm:id");

                req.Add("mode");
            })
        };
    }

    public async Task<CallToolResult> CallAsync(Dictionary<string, object?> args, CancellationToken ct)
    {
        if (!args.TryGetValue("mode", out var mObj) || mObj is null)
            throw new McpException("Missing 'mode' (getProperty|edgeExists).");

        switch (mObj.ToString()!.Trim().ToLowerInvariant())
        {
            case "getproperty":
                {
                    if (!args.TryGetValue("subjectId", out var idObj) || idObj is null)
                        throw new McpException("Missing 'subjectId'.");
                    if (!args.TryGetValue("property", out var propObj) || propObj is null)
                        throw new McpException("Missing 'property'.");

                    // NOTE: GraphStore should internally add tenant guards and id prefixing.
                    var value = await _graph.GetVertexPropertyAsync(idObj.ToString()!, propObj.ToString()!, ct);
                    var payload = new { tenantId = _tenant.TenantId, subjectId = idObj.ToString(), property = propObj.ToString(), value };
                    return new CallToolResult
                    {
                        Content = [new TextContentBlock { Type = "text", Text = JsonSerializer.Serialize(payload) }]
                    };
                }

            case "edgeexists":
                {
                    if (!args.TryGetValue("outId", out var outObj) || outObj is null)
                        throw new McpException("Missing 'outId'.");
                    if (!args.TryGetValue("edgeLabel", out var elObj) || elObj is null)
                        throw new McpException("Missing 'edgeLabel'.");
                    if (!args.TryGetValue("inId", out var inObj) || inObj is null)
                        throw new McpException("Missing 'inId'.");

                    var exists = await _graph.EdgeExistsAsync(outObj.ToString()!, elObj.ToString()!, inObj.ToString()!, ct);
                    var payload = new { tenantId = _tenant.TenantId, outId = outObj.ToString(), edgeLabel = elObj.ToString(), inId = inObj.ToString(), exists };
                    return new CallToolResult
                    {
                        Content = [new TextContentBlock { Type = "text", Text = JsonSerializer.Serialize(payload) }]
                    };
                }

            default:
                throw new McpException("Unsupported 'mode'.");
        }
    }
}