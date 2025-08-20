// File: /src/LimboDancer.MCP.Ontology/Mapping/PropertyKeyMapper.cs
// Purpose: Central mapping for ontology predicates to concrete graph keys/labels.
//DI: services.AddSingleton<IPropertyKeyMapper, DefaultPropertyKeyMapper>();

using System;
using System.Collections.Generic;

namespace LimboDancer.MCP.Ontology.Mapping
{
    public interface IPropertyKeyMapper
    {
        bool TryMapPropertyKey(string predicate, out string graphPropertyKey);
        bool TryMapEdgeLabel(string predicate, out string graphEdgeLabel);
    }

    /// <summary>
    /// Default in-memory mapper. Replace dictionaries with config-backed loading if desired.
    /// </summary>
    public sealed class DefaultPropertyKeyMapper : IPropertyKeyMapper
    {
        private readonly IReadOnlyDictionary<string, string> _prop;
        private readonly IReadOnlyDictionary<string, string> _edge;

        public DefaultPropertyKeyMapper(
            IReadOnlyDictionary<string, string>? propertyMap = null,
            IReadOnlyDictionary<string, string>? edgeMap = null)
        {
            _prop = propertyMap ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // e.g., { "ldm:label", "label" }, { "ldm:status", "status" }, { "kg:kind", "kind" }
            };
            _edge = edgeMap ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // e.g., { "kg:relatedTo", "RELATED_TO" }, { "kg:parentOf", "PARENT_OF" }
            };
        }

        public bool TryMapPropertyKey(string predicate, out string graphPropertyKey)
            => _prop.TryGetValue(predicate, out graphPropertyKey!);

        public bool TryMapEdgeLabel(string predicate, out string graphEdgeLabel)
            => _edge.TryGetValue(predicate, out graphEdgeLabel!);
    }
}
