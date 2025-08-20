using System;
using System.Collections.Generic;

namespace LimboDancer.MCP.Ontology.Runtime
{
    /// <summary>
    /// Relation (object property) definition between entities.
    /// </summary>
    public sealed record RelationDef : IScored
    {
        public TenantScope Scope { get; init; }

        public string LocalName { get; init; } = default!;
        public string CanonicalUri { get; init; } = default!;
        public string? Label { get; init; }
        public string? Description { get; init; }

        /// <summary>
        /// Outbound (subject) entity local name.
        /// </summary>
        public string FromEntity { get; init; } = default!;

        /// <summary>
        /// Inbound (object) entity local name.
        /// </summary>
        public string ToEntity { get; init; } = default!;

        public int MinCardinality { get; init; } = 0;
        public int? MaxCardinality { get; init; } = 1;

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

        public static RelationDef Create(TenantScope scope, string localName, string fromEntity, string toEntity, int minCard = 0, int? maxCard = 1, string? label = null, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(localName)) throw new ArgumentException("LocalName is required.", nameof(localName));
            if (string.IsNullOrWhiteSpace(fromEntity)) throw new ArgumentException("FromEntity is required.", nameof(fromEntity));
            if (string.IsNullOrWhiteSpace(toEntity)) throw new ArgumentException("ToEntity is required.", nameof(toEntity));

            return new RelationDef
            {
                Scope = scope,
                LocalName = localName.Trim(),
                CanonicalUri = Curie.Expand($"ldm:{localName}"),
                FromEntity = fromEntity.Trim(),
                ToEntity = toEntity.Trim(),
                MinCardinality = minCard,
                MaxCardinality = maxCard,
                Label = label,
                Description = description
            };
        }
    }
}