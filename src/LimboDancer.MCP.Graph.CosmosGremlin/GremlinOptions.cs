namespace LimboDancer.MCP.Graph.CosmosGremlin;

public sealed class GremlinOptions
{
    /// <summary>Hostname of the Cosmos DB (Gremlin API) endpoint.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Port (usually 443).</summary>
    public int Port { get; set; } = 443;

    /// <summary>Whether to use TLS.</summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>Auth key (Cosmos primary key).</summary>
    public string AuthKey { get; set; } = string.Empty;

    /// <summary>Cosmos DB database name (Graph API).</summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>Graph (container) name.</summary>
    public string Graph { get; set; } = string.Empty;
}


public enum GraphSonVersion
{
    GraphSON2,
    GraphSON3
}