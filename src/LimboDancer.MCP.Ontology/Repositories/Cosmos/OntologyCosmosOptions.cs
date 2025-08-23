namespace LimboDancer.MCP.Ontology.Repositories.Cosmos;

/// <summary>
/// Configuration options for the Cosmos DB ontology repository.
/// </summary>
public sealed class OntologyCosmosOptions
{
    public string AccountEndpoint { get; set; } = string.Empty;
    public string AccountKey { get; set; } = string.Empty;
    public string Database { get; set; } = "ontology";
    public string Container { get; set; } = "catalog";
    public string LeasesContainer { get; set; } = "leases";
}