namespace LimboDancer.MCP.Ontology.Runtime
{
    /// <summary>
    /// Enumeration definition to constrain property values to a fixed set of choices.
    /// </summary>
    public sealed record EnumDef : IScored
    {
        public TenantScope Scope { get; init; }

        public string LocalName { get; init; } = default!;
        public string CanonicalUri { get; init; } = default!;
        public string? Label { get; init; }
        public string? Description { get; init; }

        public IReadOnlyList<string> Values { get; init; } = Array.Empty<string>();

        // Governance
        public double Confidence { get; init; } = 1.0;
        public int Complexity { get; init; } = 1;
        public int Depth { get; init; } = 1;
        public PublicationStatus Status { get; init; } = PublicationStatus.Proposed;
        public string Version { get; init; } = "0.1.0";
        public ProvenanceRef? Provenance { get; init; }
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

        public static EnumDef Create(TenantScope scope, string localName, IEnumerable<string> values, string? label = null, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(localName)) throw new ArgumentException("LocalName is required.", nameof(localName));

            // Fixed: ensure consistent type for Values
            IReadOnlyList<string> list = values is null
                ? Array.Empty<string>()
                : values.ToList();

            return new EnumDef
            {
                Scope = scope,
                LocalName = localName.Trim(),
                CanonicalUri = Curie.Expand($"ldm:{localName}"),
                Values = list,
                Label = label,
                Description = description
            };
        }
    }
}