using System;

namespace LimboDancer.MCP.Ontology.Runtime
{
    /// <summary>
    /// Lightweight provenance reference for an ontology artifact.
    /// </summary>
    public readonly record struct ProvenanceRef(
        string SourceUri,
        DateTimeOffset RetrievedAt,
        string? Agent = null,
        double RetrievalScore = 1.0,
        string? Notes = null
    );
}