namespace LimboDancer.MCP.Ontology.Runtime
{
    /// <summary>
    /// Ontology entity/class definition.
    /// </summary>
    public sealed record EntityDef : IScored
    {
        public TenantScope Scope { get; init; }

        /// <summary>
        /// Local name (CURIE suffix) within the channel scope. Example: "Person"
        /// </summary>
        public string LocalName { get; init; } = default!;

        /// <summary>
        /// Canonical absolute URI for this entity (e.g., expanded CURIE).
        /// </summary>
        public string CanonicalUri { get; init; } = default!;

        public string? Label { get; init; }
        public string? Description { get; init; }

        /// <summary>
        /// Superclass refs by local name (inheritance). Empty if root.
        /// </summary>
        public IReadOnlyList<string> Parents { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Arbitrary lightweight annotations (key=value).
        /// </summary>
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

        public static EntityDef Create(TenantScope scope, string localName, string? label = null, string? description = null, IEnumerable<string>? parents = null)
        {
            if (string.IsNullOrWhiteSpace(localName)) throw new ArgumentException("LocalName is required.", nameof(localName));
            var canon = Curie.Expand($"ldm:{localName}");
            return new EntityDef
            {
                Scope = scope,
                LocalName = localName.Trim(),
                CanonicalUri = canon,
                Label = label,
                Description = description,
                Parents = parents is null ? Array.Empty<string>() : new List<string>(parents)
            };
        }
    }
}