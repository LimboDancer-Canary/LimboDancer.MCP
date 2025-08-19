namespace LimboDancer.MCP.Graph.CosmosGremlin;

public sealed class GremlinOptions
{
    /// <summary>Cosmos DB Gremlin host. Example: your-account.gremlin.cosmos.azure.com</summary>
    public string Host { get; set; } = "";

    /// <summary>Port (Cosmos = 443)</summary>
    public int Port { get; set; } = 443;

    /// <summary>Use TLS for Cosmos (true)</summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>Cosmos DB database (a.k.a. database/graph account db)</summary>
    public string Database { get; set; } = "";

    /// <summary>Graph (container) name</summary>
    public string Graph { get; set; } = "";

    /// <summary>Primary (or secondary) key</summary>
    public string AuthKey { get; set; } = "";

    /// <summary>Pool size for underlying WebSocket connections</summary>
    public int ConnectionPoolSize { get; set; } = 8;

    /// <summary>Request serializer version; Cosmos Gremlin API supports GraphSON 2.x</summary>
    public GraphSonVersion Serializer { get; set; } = GraphSonVersion.GraphSON2;
}

public enum GraphSonVersion
{
    GraphSON2,
    GraphSON3
}