namespace LimboDancer.MCP.Ontology.Repositories.Cosmos
{
    /// <summary>
    /// Defines Cosmos container names and partitioning conventions for ontology storage.
    /// NOTE: This class does not depend on Cosmos SDK and can be referenced by infra implementations.
    /// </summary>
    public static class CosmosOntologyContainers
    {
        // Primary ontology container (published)
        public const string Ontology = "ontology";

        // Proposals container (unpublished drafts / candidate changes)
        public const string OntologyProposals = "ontology-proposals";

        // Coordination and leases (if needed for background processing)
        public const string Leases = "leases";

        /// <summary>
        /// Hierarchical Partition Key path for the main containers.
        /// </summary>
        public static string[] PartitionKeyPaths => new[]
        {
            "/tenantId",
            "/packageId",
            "/channelId"
        };

        public static class Discriminators
        {
            public const string Entity = "entity";
            public const string Property = "property";
            public const string Relation = "relation";
            public const string Enum = "enum";
            public const string Alias = "alias";
            public const string Shape = "shape";
        }
    }
}