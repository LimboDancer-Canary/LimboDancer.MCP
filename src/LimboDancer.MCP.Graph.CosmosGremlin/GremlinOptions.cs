using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace LimboDancer.MCP.Graph.CosmosGremlin;

/// <summary>
/// Options for configuring a Gremlin client targeting Azure Cosmos DB Gremlin API,
/// with sensible defaults and validation for Cosmos.
/// </summary>
public sealed class GremlinOptions
{
    /// <summary>Cosmos DB Gremlin host. Example: your-account.gremlin.cosmos.azure.com</summary>
    [Required]
    public string Host { get; set; } = string.Empty;

    /// <summary>Port (Cosmos = 443)</summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 443;

    /// <summary>Use TLS for Cosmos (true). Cosmos requires TLS.</summary>
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
    [Required]
    public string Database { get; set; } = string.Empty;

    /// <summary>Graph (container) name</summary>
    [Required]
    public string Graph { get; set; } = string.Empty;

    /// <summary>Primary (or secondary) key</summary>
    [Required]
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
    [Range(1, int.MaxValue)]
    public int ConnectionPoolSize { get; set; } = 8;

    /// <summary>
    /// Maximum number of in-process requests per connection (Gremlin driver tuning).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxInProcessPerConnection { get; set; } = 32;

    /// <summary>
    /// Maximum number of simultaneous requests per connection (Gremlin driver tuning).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxSimultaneousRequestsPerConnection { get; set; } = 64;

    /// <summary>
    /// Request timeout for Gremlin queries. Cosmos often benefits from >= 60s.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Request serializer version; Cosmos Gremlin API supports GraphSON 2.x</summary>
    public GraphSonVersion Serializer { get; set; } = GraphSonVersion.GraphSON2;

    /// <summary>
    /// Heuristic: true if the host appears to be a Cosmos DB Gremlin endpoint.
    /// </summary>
    [Browsable(false)]
    public bool IsCosmos =>
        !string.IsNullOrWhiteSpace(Host) &&
        Host.EndsWith(".gremlin.cosmos.azure.com", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Validate the option values, throwing <see cref="ValidationException"/> for invalid configurations.
    /// Enforces Cosmos-specific constraints (TLS, port 443, GraphSON2).
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
            throw new ValidationException("Host is required.");

        if (Port is < 1 or > 65535)
            throw new ValidationException("Port must be between 1 and 65535.");

        if (string.IsNullOrWhiteSpace(Database))
            throw new ValidationException("Database is required.");

        if (string.IsNullOrWhiteSpace(Graph))
            throw new ValidationException("Graph is required.");

        if (string.IsNullOrWhiteSpace(AuthKey))
            throw new ValidationException("AuthKey is required.");

        if (ConnectionPoolSize <= 0)
            throw new ValidationException("ConnectionPoolSize must be greater than 0.");

        if (MaxInProcessPerConnection <= 0)
            throw new ValidationException("MaxInProcessPerConnection must be greater than 0.");

        if (MaxSimultaneousRequestsPerConnection <= 0)
            throw new ValidationException("MaxSimultaneousRequestsPerConnection must be greater than 0.");

        if (IsCosmos)
        {
            if (!EnableSsl)
                throw new ValidationException("Cosmos Gremlin requires TLS (EnableSsl = true).");

            if (Port != 443)
                throw new ValidationException("Cosmos Gremlin requires Port = 443.");

            if (Serializer != GraphSonVersion.GraphSON2)
                throw new ValidationException("Cosmos Gremlin supports GraphSON 2.x only. Set Serializer = GraphSonVersion.GraphSON2.");
        }
    }

    /// <summary>
    /// Returns a redacted string representation suitable for logs (hides AuthKey).
    /// </summary>
    public override string ToString()
    {
        var redactedKey = string.IsNullOrEmpty(AuthKey) ? "" : "****";
        return $"Host={Host};Port={Port};EnableSsl={EnableSsl};Database={Database};Graph={Graph};AuthKey={redactedKey};Serializer={Serializer};Pool={ConnectionPoolSize};TimeoutSeconds={(int)RequestTimeout.TotalSeconds}";
    }

    /// <summary>
    /// Parse a semicolon-separated connection string into <see cref="GremlinOptions"/>.
    /// Supported keys: Host, Port, EnableSsl, Database, Graph, AuthKey, Serializer, ConnectionPoolSize, MaxInProcessPerConnection, MaxSimultaneousRequestsPerConnection, RequestTimeoutSeconds.
    /// </summary>
    public static GremlinOptions ParseConnectionString(string connectionString)
    {
        if (!TryParseConnectionString(connectionString, out var options, out var error))
            throw new FormatException(error);
        return options!;
    }

    /// <summary>
    /// Try to parse a connection string into options.
    /// </summary>
    public static bool TryParseConnectionString(string connectionString, [NotNullWhen(true)] out GremlinOptions? options, out string error)
    {
        options = null;
        error = string.Empty;

        if (connectionString is null)
        {
            error = "Connection string is null.";
            return false;
        }

        var result = new GremlinOptions();
        try
        {
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                var kvp = part.Split('=', 2, StringSplitOptions.TrimEntries);
                if (kvp.Length != 2) continue;

                var key = kvp[0];
                var value = kvp[1];

                switch (key.ToLowerInvariant())
                {
                    case "host":
                        result.Host = value;
                        break;
                    case "port":
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
                            result.Port = port;
                        else
                            throw new FormatException("Invalid Port.");
                        break;
                    case "enablessl":
                    case "usessl": // legacy alias
                        if (bool.TryParse(value, out var ssl))
                            result.EnableSsl = ssl;
                        else
                            throw new FormatException("Invalid EnableSsl.");
                        break;
                    case "database":
                        result.Database = value;
                        break;
                    case "graph":
                        result.Graph = value;
                        break;
                    case "authkey":
                    case "primarykey": // legacy alias
                        result.AuthKey = value;
                        break;
                    case "serializer":
                        result.Serializer = value.Equals("graphson2", StringComparison.OrdinalIgnoreCase)
                            ? GraphSonVersion.GraphSON2
                            : value.Equals("graphson3", StringComparison.OrdinalIgnoreCase)
                                ? GraphSonVersion.GraphSON3
                                : throw new FormatException("Invalid Serializer. Use GraphSON2 or GraphSON3.");
                        break;
                    case "connectionpoolsize":
                    case "pool":
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pool))
                            result.ConnectionPoolSize = pool;
                        else
                            throw new FormatException("Invalid ConnectionPoolSize.");
                        break;
                    case "maxinprocessperconnection":
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxInProc))
                            result.MaxInProcessPerConnection = maxInProc;
                        else
                            throw new FormatException("Invalid MaxInProcessPerConnection.");
                        break;
                    case "maxsimultaneousrequestsperconnection":
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxSim))
                            result.MaxSimultaneousRequestsPerConnection = maxSim;
                        else
                            throw new FormatException("Invalid MaxSimultaneousRequestsPerConnection.");
                        break;
                    case "requesttimeoutseconds":
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var secs) && secs >= 0)
                            result.RequestTimeout = TimeSpan.FromSeconds(secs);
                        else
                            throw new FormatException("Invalid RequestTimeoutSeconds.");
                        break;
                }
            }

            // Final validation
            result.Validate();

            options = result;
            return true;
        }
        catch (Exception ex) when (ex is FormatException or ValidationException)
        {
            error = ex.Message;
            options = null;
            return false;
        }
    }
}

public enum GraphSonVersion
{
    /// <summary>GraphSON 2.x (supported by Cosmos DB Gremlin API).</summary>
    GraphSON2,

    /// <summary>GraphSON 3.x (NOT supported by Cosmos DB Gremlin API).</summary>
    [Obsolete("Cosmos DB Gremlin API only supports GraphSON 2.x. Use GraphSON2.")]
    GraphSON3
}