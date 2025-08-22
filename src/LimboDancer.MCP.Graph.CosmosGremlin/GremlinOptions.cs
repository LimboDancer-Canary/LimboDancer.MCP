#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace LimboDancer.MCP.Graph.CosmosGremlin;

/// <summary>
/// Configuration options for Gremlin clients.
/// Note: Obsolete alias properties (UseSsl, PrimaryKey, Serializer) have been removed.
/// </summary>
public sealed class GremlinOptions
{
    /// <summary>Cosmos DB Gremlin host. Example: your-account.gremlin.cosmos.azure.com</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Port (Cosmos = 443)</summary>
    public int Port { get; set; } = 443;

    /// <summary>Use TLS for Cosmos (true)</summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>Cosmos DB database (a.k.a. database/graph account db)</summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>Graph (container) name</summary>
    public string Graph { get; set; } = string.Empty;

    /// <summary>Primary (or secondary) key</summary>
    public string AuthKey { get; set; } = string.Empty;

    /// <summary>Pool size for underlying WebSocket connections</summary>
    public int ConnectionPoolSize { get; set; } = 8;

    /// <summary>Whether this is a Cosmos DB Gremlin endpoint (affects username/password formatting)</summary>
    public bool IsCosmos { get; set; } = true;

    /// <summary>Request timeout for Gremlin operations</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Request serializer version; Cosmos Gremlin API supports GraphSON 2.x</summary>
    public GraphSonVersion GraphSONVersion { get; set; } = GraphSonVersion.GraphSON2;

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

    /// <summary>
    /// Parses a connection string in the format: cosmosgremlin://account:key@host:port/database/graph
    /// </summary>
    public static GremlinOptions Parse(string connectionString)
    {
        if (!TryParse(connectionString, out var options, out var error))
            throw new FormatException($"Invalid connection string: {error}");
        return options;
    }

    /// <summary>
    /// Attempts to parse a connection string.
    /// Format: cosmosgremlin://account:key@host:port/database/graph
    /// </summary>
    public static bool TryParse(string connectionString, out GremlinOptions options, out string? error)
    {
        options = new GremlinOptions();
        error = null;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            error = "Connection string cannot be empty";
            return false;
        }

        try
        {
            var uri = new Uri(connectionString);

            if (uri.Scheme != "cosmosgremlin" && uri.Scheme != "gremlin")
            {
                error = $"Invalid scheme '{uri.Scheme}'. Expected 'cosmosgremlin' or 'gremlin'";
                return false;
            }

            options.IsCosmos = uri.Scheme == "cosmosgremlin";
            options.Host = uri.Host;
            options.Port = uri.Port > 0 ? uri.Port : 443;
            options.EnableSsl = true;

            // Extract auth key from user info
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var parts = uri.UserInfo.Split(':');
                if (parts.Length == 2)
                {
                    options.AuthKey = Uri.UnescapeDataString(parts[1]);
                }
            }

            // Extract database and graph from path
            var pathSegments = uri.AbsolutePath.Trim('/').Split('/');
            if (pathSegments.Length >= 1)
                options.Database = Uri.UnescapeDataString(pathSegments[0]);
            if (pathSegments.Length >= 2)
                options.Graph = Uri.UnescapeDataString(pathSegments[1]);

            // Parse query parameters for additional options
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            if (int.TryParse(query["poolSize"], out var poolSize))
                options.ConnectionPoolSize = poolSize;
            if (int.TryParse(query["timeout"], out var timeout))
                options.RequestTimeout = TimeSpan.FromSeconds(timeout);

            options.Validate();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Returns a redacted string representation suitable for logging.
    /// </summary>
    public string ToString()
    {
        var keyDisplay = string.IsNullOrWhiteSpace(AuthKey) ? "<empty>" : "****";
        return $"GremlinOptions[Host={Host}:{Port}, Database={Database}, Graph={Graph}, AuthKey={keyDisplay}, IsCosmos={IsCosmos}]";
    }
}

public enum GraphSonVersion
{
    GraphSON2,
    [Obsolete("Cosmos Gremlin only supports GraphSON2")]
    GraphSON3
}
#pragma warning restore CS1591