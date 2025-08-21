#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System.ComponentModel;

namespace LimboDancer.MCP.Graph.CosmosGremlin;

public sealed class GremlinOptions
{
    /// <summary>Cosmos DB Gremlin host. Example: your-account.gremlin.cosmos.azure.com</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Port (Cosmos = 443)</summary>
    public int Port { get; set; } = 443;

    /// <summary>Use TLS for Cosmos (true)</summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>Whether to use TLS.</summary>
    [Obsolete("Use EnableSsl instead. This property will be removed in a future version.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool UseSsl 
    { 
        get => EnableSsl; 
        set => EnableSsl = value; 
    }

    /// <summary>Cosmos DB database (a.k.a. database/graph account db)</summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>Graph (container) name</summary>
    public string Graph { get; set; } = string.Empty;

    /// <summary>Primary (or secondary) key</summary>
    public string AuthKey { get; set; } = string.Empty;

    /// <summary>Primary (or secondary) key</summary>
    [Obsolete("Use AuthKey instead. This property will be removed in a future version.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string PrimaryKey 
    { 
        get => AuthKey; 
        set => AuthKey = value; 
    }

    /// <summary>Pool size for underlying WebSocket connections</summary>
    public int ConnectionPoolSize { get; set; } = 8;

    /// <summary>Whether this is a Cosmos DB Gremlin endpoint (affects username/password formatting)</summary>
    public bool IsCosmos { get; set; } = true;

    /// <summary>Request timeout for Gremlin operations</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Request serializer version; Cosmos Gremlin API supports GraphSON 2.x</summary>
    public GraphSonVersion GraphSONVersion { get; set; } = GraphSonVersion.GraphSON2;

    /// <summary>Request serializer version; Cosmos Gremlin API supports GraphSON 2.x</summary>
    [Obsolete("Use GraphSONVersion instead. This property will be removed in a future version.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public GraphSonVersion Serializer 
    { 
        get => GraphSONVersion; 
        set => GraphSONVersion = value; 
    }

    /// <summary>
    /// Validates the options for basic correctness.
    /// Throws InvalidOperationException for missing or invalid required fields.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
            throw new InvalidOperationException("Host is required and cannot be empty.");

        if (Port <= 0 || Port > 65535)
            throw new InvalidOperationException($"Port must be between 1 and 65535, but was {Port}.");

        if (IsCosmos)
        {
            if (string.IsNullOrWhiteSpace(Database))
                throw new InvalidOperationException("Database is required and cannot be empty when IsCosmos is true.");

            if (string.IsNullOrWhiteSpace(Graph))
                throw new InvalidOperationException("Graph is required and cannot be empty when IsCosmos is true.");

            if (string.IsNullOrWhiteSpace(AuthKey))
                throw new InvalidOperationException("AuthKey is required and cannot be empty when IsCosmos is true.");
        }

        if (ConnectionPoolSize <= 0)
            throw new InvalidOperationException($"ConnectionPoolSize must be greater than 0, but was {ConnectionPoolSize}.");
    }
}


public enum GraphSonVersion
{
    GraphSON2,
    [Obsolete("Cosmos Gremlin only supports GraphSON2")]
    GraphSON3
}
#pragma warning restore CS1591