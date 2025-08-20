using System;

namespace LimboDancer.MCP.Ontology.Runtime
{
    /// <summary>
    /// Governance/quality facet present on each ontology artifact.
    /// </summary>
    public interface IScored
    {
        /// <summary>
        /// 0.0..1.0 confidence score for the definition’s correctness.
        /// </summary>
        double Confidence { get; init; }

        /// <summary>
        /// A normalized complexity score (1=low, higher=more complex). Use a small bounded range, e.g., 1..9.
        /// </summary>
        int Complexity { get; init; }

        /// <summary>
        /// A normalized depth score (1=shallow, higher=deeper dependency or nesting).
        /// </summary>
        int Depth { get; init; }

        PublicationStatus Status { get; init; }

        /// <summary>
        /// Semantic or opaque version identifier for the artifact.
        /// </summary>
        string Version { get; init; }

        ProvenanceRef? Provenance { get; init; }

        DateTimeOffset CreatedAt { get; init; }
        DateTimeOffset UpdatedAt { get; init; }
    }

    public enum PublicationStatus
    {
        Proposed = 0,
        Published = 1,
        Deprecated = 2,
        Rejected = 3
    }
}