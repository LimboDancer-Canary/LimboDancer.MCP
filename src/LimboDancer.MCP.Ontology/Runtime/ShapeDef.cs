using System;
using System.Collections.Generic;

namespace LimboDancer.MCP.Ontology.Runtime
{
    /// <summary>
    /// Simple SHACL-like shape constraints for entities and their properties.
    /// </summary>
    public sealed record ShapeDef : IScored
    {
        public TenantScope Scope { get; init; }

        /// <summary>
        /// The entity local name this shape applies to.
        /// </summary>
        public string AppliesToEntity { get; init; } = default!;

        public IReadOnlyList<PropertyConstraint> PropertyConstraints { get; init; } = Array.Empty<PropertyConstraint>();

        // Governance
        public double Confidence { get; init; } = 1.0;
        public int Complexity { get; init; } = 1;
        public int Depth { get; init; } = 1;
        public PublicationStatus Status { get; init; } = PublicationStatus.Proposed;
        public string Version { get; init; } = "0.1.0";
        public ProvenanceRef? Provenance { get; init; }
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
    }

    public sealed record PropertyConstraint
    {
        /// <summary>
        /// Property local name under the owning entity.
        /// </summary>
        public string Property { get; init; } = default!;

        /// <summary>
        /// Optional XSD datatype or entity local name for validation.
        /// </summary>
        public string? ExpectedRange { get; init; }

        public int MinCardinality { get; init; } = 0;
        public int? MaxCardinality { get; init; } = 1;

        /// <summary>
        /// Simple regex to validate string content when applicable.
        /// </summary>
        public string? Pattern { get; init; }

        /// <summary>
        /// Optional enumerated set of allowed values (for string-like fields).
        /// </summary>
        public IReadOnlyList<string>? In { get; init; }
    }
}