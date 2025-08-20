namespace LimboDancer.MCP.Ontology.Constants
{
    /// <summary>
    /// Suggested graph constants. Keep aligned to ontology local names where possible.
    /// Use <see cref="Fields"/> for property keys (graph storage-level),
    /// and <see cref="Labels"/> for vertex/edge labels.
    /// </summary>
    public static class Kg
    {
        // ---------------------------------------------------------------------
        // Back-compat (pre-refactor) flat constants
        // Prefer using the nested classes below in new code.
        // ---------------------------------------------------------------------

        /// <summary>Default vertex label (ontology-curie).</summary>
        public const string VertexLabel = "ldm:Vertex";

        /// <summary>Default edge label (ontology-curie).</summary>
        public const string EdgeLabel = "ldm:Edge";

        /// <summary>Storage-level identifier property key.</summary>
        public const string Id = "id";

        /// <summary>Storage-level discriminator/type property key.</summary>
        public const string Type = "type";

        /// <summary>Storage-level name property key.</summary>
        public const string Name = "name";

        // ---------------------------------------------------------------------
        // Preferred constants
        // ---------------------------------------------------------------------

        /// <summary>
        /// Vertex/edge labels (ontology-curie values).
        /// </summary>
        public static class Labels
        {
            /// <summary>Default vertex label (ontology-curie).</summary>
            public const string Vertex = "ldm:Vertex";

            /// <summary>Default edge label (ontology-curie).</summary>
            public const string Edge = "ldm:Edge";
        }

        /// <summary>
        /// Property keys used in the graph storage (storage-level, not ontology URIs).
        /// These should align with how <c>GraphStore</c> reads/writes properties.
        /// </summary>
        public static class Fields
        {
            /// <summary>Storage-level identifier property key.</summary>
            public const string Id = "id";

            /// <summary>Storage-level discriminator/type property key.</summary>
            public const string Type = "type";

            /// <summary>Human-friendly label/title.</summary>
            public const string Label = "label";

            /// <summary>Kind/category discriminator.</summary>
            public const string Kind = "kind";

            /// <summary>Lifecycle or publication status.</summary>
            public const string Status = "status";

            /// <summary>Tenant partition key (if modeled as a property on vertices).</summary>
            public const string TenantId = "tenantId";

            /// <summary>Creation timestamp (UTC ISO 8601 recommended).</summary>
            public const string CreatedAt = "createdAt";

            /// <summary>Update timestamp (UTC ISO 8601 recommended).</summary>
            public const string UpdatedAt = "updatedAt";

            /// <summary>Optional display/name field (distinct from label in some models).</summary>
            public const string Name = "name";
        }
    }
}