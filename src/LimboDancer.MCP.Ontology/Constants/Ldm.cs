namespace LimboDancer.MCP.Ontology.Constants
{
    /// <summary>
    /// Minimal platform constants for the LimboDancer ontology.
    /// Prefer data-first ontology definitions; keep this list thin and focused on ubiquitous terms.
    /// </summary>
    public static class Ldm
    {
        /// <summary>
        /// Base namespace template. Replace tokens per scope/channel as needed.
        /// Example resolved form:
        ///   https://ontology.limbodancer.mcp/{tenant}/{package}/{channel}#
        /// </summary>
        public const string BaseNamespaceTemplate = "https://ontology.limbodancer.mcp/{tenant}/{package}/{channel}#";

        /// <summary>
        /// Resolves the scoped base namespace IRI for a given tenant/package/channel.
        /// </summary>
        public static string BaseNamespace(string tenant, string @package, string channel)
            => BaseNamespaceTemplate
                .Replace("{tenant}", tenant)
                .Replace("{package}", @package)
                .Replace("{channel}", channel);

        /// <summary>
        /// CURIE helpers and well-known prefix for LimboDancer ontology terms.
        /// </summary>
        public static class Prefix
        {
            public const string Curie = "ldm";

            /// <summary>
            /// Builds a CURIE for a local name, e.g., "status" -> "ldm:status".
            /// </summary>
            public static string CurieOf(string local) => $"{Curie}:{local}";
        }

        /// <summary>
        /// Common property CURIEs used across code paths.
        /// These are CURIEs rather than fully-expanded IRIs so they remain scope-agnostic.
        /// </summary>
        public static class Properties
        {
            /// <summary>General-purpose human-friendly label.</summary>
            public const string Label = "ldm:label";

            /// <summary>Lifecycle or publication status.</summary>
            public const string Status = "ldm:status";

            /// <summary>Kind/category discriminator.</summary>
            public const string Kind = "ldm:kind";

            /// <summary>Identifier (semantic id, not necessarily storage key).</summary>
            public const string Id = "ldm:id";

            /// <summary>Generic property key reference (used by tools that take a property argument).</summary>
            public const string Property = "ldm:property";

            /// <summary>Generic relation key reference (used by tools that take an edge/label argument).</summary>
            public const string Relation = "ldm:relation";
        }

        /// <summary>
        /// Common entity CURIEs used by tools and UI (kept minimal).
        /// </summary>
        public static class Entities
        {
            public const string Session = "ldm:Session";
            public const string Message = "ldm:Message";
        }

        /// <summary>
        /// Message-specific properties (kept here to avoid scattering literal strings).
        /// </summary>
        public static class MessageProperties
        {
            public const string Role = "ldm:Message.role";
            public const string Content = "ldm:Message.content";
        }

        /// <summary>
        /// Representative relations used in examples and effects.
        /// </summary>
        public static class Relations
        {
            /// <summary>Session hasMessage Message.</summary>
            public const string HasMessage = "ldm:hasMessage";
        }
    }
}