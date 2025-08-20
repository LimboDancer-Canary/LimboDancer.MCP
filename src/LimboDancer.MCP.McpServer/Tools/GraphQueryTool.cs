// UPDATED: (1) shared @context via ToolSchemas.JsonLdContext, (2) uses central IPropertyKeyMapper
// from LimboDancer.MCP.Ontology.Mapping (no local interface), (3) unused usings removed.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LimboDancer.MCP.Ontology.Mapping;   // ⬅ central mapper
using Microsoft.Extensions.Logging;

namespace LimboDancer.MCP.McpServer.Tools
{
    public interface IGraphQueryStore
    {
        Task<GraphQueryResult> QueryAsync(GraphQueryRequest request, CancellationToken ct = default);
    }

    #region DTOs

    public sealed class GraphQueryInput
    {
        [JsonPropertyName("subjectIds")] public List<string>? SubjectIds { get; set; }
        [JsonPropertyName("keyMode")] public string KeyMode { get; set; } = "ontology";
        [JsonPropertyName("filters")] public List<PropertyFilter>? Filters { get; set; }
        [JsonPropertyName("traverse")] public List<TraversalStep>? Traverse { get; set; }
        [JsonPropertyName("limit")] public int Limit { get; set; } = 50;
        [JsonPropertyName("cursor")] public string? Cursor { get; set; }
    }

    public sealed class PropertyFilter
    {
        [JsonPropertyName("property")] public string Property { get; set; } = default!;
        [JsonPropertyName("op")] public string Op { get; set; } = "eq";
        [JsonPropertyName("value")] public object? Value { get; set; }
    }

    public sealed class TraversalStep
    {
        [JsonPropertyName("direction")] public string Direction { get; set; } = "out";
        [JsonPropertyName("relation")] public string Relation { get; set; } = default!;
        [JsonPropertyName("hops")] public int Hops { get; set; } = 1;
    }

    public sealed class GraphQueryOutput
    {
        [JsonPropertyName("vertices")] public List<VertexProjection> Vertices { get; set; } = new();
        [JsonPropertyName("nextCursor")] public string? NextCursor { get; set; }
    }

    public sealed class VertexProjection
    {
        [JsonPropertyName("id")] public string Id { get; set; } = default!;
        [JsonPropertyName("properties")] public Dictionary<string, object?> Properties { get; set; } = new();
    }

    public sealed class GraphQueryRequest
    {
        public List<string>? SubjectIds { get; init; }
        public List<MappedPropertyFilter>? Filters { get; init; }
        public List<MappedTraversalStep>? Traverse { get; init; }
        public int Limit { get; init; }
        public string? Cursor { get; init; }
    }

    public sealed class MappedPropertyFilter
    {
        public string PropertyKey { get; init; } = default!;
        public string Op { get; init; } = "eq";
        public object? Value { get; init; }
    }

    public sealed class MappedTraversalStep
    {
        public string Direction { get; init; } = "out";
        public string EdgeLabel { get; init; } = default!;
        public int Hops { get; init; } = 1;
    }

    public sealed class GraphQueryResult
    {
        public List<VertexProjection> Vertices { get; init; } = new();
        public string? NextCursor { get; init; }
    }

    #endregion

    public sealed class GraphQueryTool
    {
        private readonly IGraphQueryStore _store;
        private readonly IPropertyKeyMapper _mapper;   // ⬅ central mapper
        private readonly ILogger<GraphQueryTool> _log;

        public GraphQueryTool(IGraphQueryStore store, IPropertyKeyMapper mapper, ILogger<GraphQueryTool> log)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public static string ToolSchema =>
            "{\n" +
            $"  \"@context\": {ToolSchemas.JsonLdContext},\n" +
            "  \"@id\": \"ldm:tool/GraphQuery\",\n" +
            "  \"title\": \"Query the knowledge graph\",\n" +
            "  \"description\": \"Filter and traverse vertices using ontology predicates (mapped) or graph keys.\",\n" +
            "  \"input\": {\n" +
            "    \"type\": \"object\",\n" +
            "    \"properties\": {\n" +
            "      \"subjectIds\": { \"type\": \"array\", \"items\": { \"type\": \"string\" } },\n" +
            "      \"keyMode\": { \"type\": \"string\", \"enum\": [\"ontology\",\"graph\"], \"default\": \"ontology\" },\n" +
            "      \"filters\": { \"type\": \"array\" },\n" +
            "      \"traverse\": { \"type\": \"array\" },\n" +
            "      \"limit\": { \"type\": \"integer\", \"minimum\": 1, \"maximum\": 500, \"default\": 50 },\n" +
            "      \"cursor\": { \"type\": \"string\" }\n" +
            "    }\n" +
            "  },\n" +
            "  \"output\": {\n" +
            "    \"type\": \"object\",\n" +
            "    \"required\": [\"vertices\"],\n" +
            "    \"properties\": {\n" +
            "      \"vertices\": { \"type\": \"array\" },\n" +
            "      \"nextCursor\": { \"type\": \"string\", \"nullable\": true }\n" +
            "    }\n" +
            "  }\n" +
            "}";

        public async Task<GraphQueryOutput> ExecuteAsync(GraphQueryInput input, CancellationToken ct = default)
        {
            Validate(input);

            var keyMode = (input.KeyMode ?? "ontology").Trim().ToLowerInvariant();
            var useOntology = keyMode == "ontology";

            var mappedFilters = new List<MappedPropertyFilter>();
            if (input.Filters is { Count: > 0 })
            {
                foreach (var f in input.Filters)
                {
                    if (string.IsNullOrWhiteSpace(f.Property)) continue;
                    var propertyKey = f.Property;

                    if (useOntology && !_mapper.TryMapPropertyKey(f.Property, out propertyKey))
                    {
                        _log.LogWarning("Unknown property predicate '{Predicate}' — skipping.", f.Property);
                        continue;
                    }

                    mappedFilters.Add(new MappedPropertyFilter { PropertyKey = propertyKey, Op = f.Op ?? "eq", Value = f.Value });
                }
            }

            var mappedTraverse = new List<MappedTraversalStep>();
            if (input.Traverse is { Count: > 0 })
            {
                foreach (var step in input.Traverse)
                {
                    if (string.IsNullOrWhiteSpace(step.Relation)) continue;
                    var edgeLabel = step.Relation;

                    if (useOntology && !_mapper.TryMapEdgeLabel(step.Relation, out edgeLabel))
                    {
                        _log.LogWarning("Unknown edge predicate '{Predicate}' — skipping.", step.Relation);
                        continue;
                    }

                    var direction = string.IsNullOrWhiteSpace(step.Direction) ? "out" : step.Direction.ToLowerInvariant();
                    if (direction != "out" && direction != "in") direction = "out";
                    var hops = step.Hops <= 0 ? 1 : step.Hops;

                    mappedTraverse.Add(new MappedTraversalStep { Direction = direction, EdgeLabel = edgeLabel, Hops = hops });
                }
            }

            var request = new GraphQueryRequest
            {
                SubjectIds = input.SubjectIds,
                Filters = mappedFilters.Count > 0 ? mappedFilters : null,
                Traverse = mappedTraverse.Count > 0 ? mappedTraverse : null,
                Limit = input.Limit <= 0 ? 50 : Math.Min(input.Limit, 500),
                Cursor = input.Cursor
            };

            var result = await _store.QueryAsync(request, ct);
            return new GraphQueryOutput { Vertices = result.Vertices, NextCursor = result.NextCursor };
        }

        public Task<GraphQueryOutput> ExecuteAsync(JsonElement json, CancellationToken ct = default)
        {
            var input = JsonSerializer.Deserialize<GraphQueryInput>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? throw new ArgumentException("Invalid GraphQuery input payload.");
            return ExecuteAsync(input, ct);
        }

        private static void Validate(GraphQueryInput input)
        {
            if (!string.IsNullOrWhiteSpace(input.KeyMode))
            {
                var km = input.KeyMode.Trim().ToLowerInvariant();
                if (km != "ontology" && km != "graph") throw new ArgumentException("keyMode must be 'ontology' or 'graph'.");
            }
        }
    }
}
