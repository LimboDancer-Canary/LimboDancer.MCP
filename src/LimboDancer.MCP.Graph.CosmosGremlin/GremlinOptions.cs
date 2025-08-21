using System;

namespace LimboDancer.MCP.Graph.CosmosGremlin;

public sealed class GremlinOptions
{
    /// <summary>Hostname of the Cosmos DB (Gremlin API) endpoint.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Port (usually 443).</summary>
    public int Port { get; set; } = 443;

    /// <summary>Whether to use TLS.</summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>Deprecated alias for EnableSsl.</summary>
    [Obsolete("Use EnableSsl instead. This alias will be removed in a future version.")]
    public bool UseSsl { get => EnableSsl; set => EnableSsl = value; }

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