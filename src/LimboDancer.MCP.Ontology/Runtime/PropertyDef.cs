using System;
using System.Collections.Generic;

namespace LimboDancer.MCP.Ontology.Runtime
{
    /// <summary>
    /// Data property definition.
    /// </summary>
    public sealed record PropertyDef : IScored
    {
        public TenantScope Scope { get; init; }

        /// <summary>
        /// The entity that owns this property (local name).
        /// </summary>
        public string OwnerEntity { get; init; } = default!;

        /// <summary>
        /// Local name of the property.
        /// </summary>
        public string LocalName { get; init; } = default!;

        public string CanonicalUri { get; init; } = default!;

        public string? Label { get; init; }
        public string? Description { get; init; }

        public PropertyRange Range { get; init; } = PropertyRange.String();

        public int MinCardinality { get; init; } = 0;
        public int? MaxCardinality { get; init; } = 1;

        public bool Required => MinCardinality > 0;

        public IReadOnlyDictionary<string, string> Annotations { get; init; } = new Dictionary<string, string>();

        // Governance
        public double Confidence { get; init; } = 1.0;
        public int Complexity { get; init; } = 1;
        public int Depth { get; init; } = 1;
        public PublicationStatus Status { get; init; } = PublicationStatus.Proposed;
        public string Version { get; init; } = "0.1.0";
        public ProvenanceRef? Provenance { get; init; }
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

        public static PropertyDef Create(TenantScope scope, string ownerEntity, string localName, PropertyRange range, int minCard = 0, int? maxCard = 1, string? label = null, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(ownerEntity)) throw new ArgumentException("OwnerEntity is required.", nameof(ownerEntity));
            if (string.IsNullOrWhiteSpace(localName)) throw new ArgumentException("LocalName is required.", nameof(localName));

            return new PropertyDef
            {
                Scope = scope,
                OwnerEntity = ownerEntity.Trim(),
                LocalName = localName.Trim(),
                CanonicalUri = Curie.Expand($"ldm:{localName}"),
                Range = range,
                MinCardinality = minCard,
                MaxCardinality = maxCard,
                Label = label,
                Description = description
            };
        }
    }

    public enum RangeKind
    {
        XsdDatatype,
        EntityRef
    }

    public sealed record PropertyRange
    {
        public RangeKind Kind { get; init; }
        /// <summary>
        /// For XsdDatatype ranges: absolute or compacted XSD URI (e.g., xsd:string).
        /// For EntityRef ranges: target entity local name.
        /// </summary>
        public string Value { get; init; } = default!;

        public static PropertyRange String() => new() { Kind = RangeKind.XsdDatatype, Value = "xsd:string" };
        public static PropertyRange Integer() => new() { Kind = RangeKind.XsdDatatype, Value = "xsd:integer" };
        public static PropertyRange Boolean() => new() { Kind = RangeKind.XsdDatatype, Value = "xsd:boolean" };
        public static PropertyRange DateTime() => new() { Kind = RangeKind.XsdDatatype, Value = "xsd:dateTime" };
        public static PropertyRange Decimal() => new() { Kind = RangeKind.XsdDatatype, Value = "xsd:decimal" };
        public static PropertyRange Entity(string targetEntityLocalName)
            => new() { Kind = RangeKind.EntityRef, Value = targetEntityLocalName };
    }
}