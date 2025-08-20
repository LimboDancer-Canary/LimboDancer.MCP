using System;
using System.Collections.Generic;

namespace LimboDancer.MCP.Ontology.Runtime
{
    /// <summary>
    /// Alias mapping to a canonical term. Aliases may be localized.
    /// </summary>
    public sealed record AliasDef : IScored
    {
        public TenantScope Scope { get; init; }

        /// <summary>
        /// Canonical target local name.
        /// </summary>
        public string Canonical { get; init; } = default!;

        /// <summary>
        /// Alternate names mapped to the canonical term.
        /// </summary>
        public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();

        /// <summary>
        /// IETF language tag for localized aliases (optional).
        /// </summary>
        public string? Locale { get; init; }

        public string? Notes { get; init; }

        // Governance
        public double Confidence { get; init; } = 1.0;
        public int Complexity { get; init; } = 1;
        public int Depth { get; init; } = 1;
        public PublicationStatus Status { get; init; } = PublicationStatus.Proposed;
        public string Version { get; init; } = "0.1.0";
        public ProvenanceRef? Provenance { get; init; }
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

        public static AliasDef Create(TenantScope scope, string canonical, IEnumerable<string> aliases, string? locale = null, string? notes = null)
        {
            if (string.IsNullOrWhiteSpace(canonical)) throw new ArgumentException("Canonical is required.", nameof(canonical));
            return new AliasDef
            {
                Scope = scope,
                Canonical = canonical.Trim(),
                Aliases = aliases is null ? Array.Empty<string>() : new List<string>(aliases),
                Locale = string.IsNullOrWhiteSpace(locale) ? null : locale,
                Notes = notes
            };
        }
    }
}