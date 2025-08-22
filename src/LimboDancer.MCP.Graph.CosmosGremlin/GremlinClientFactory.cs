#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace LimboDancer.MCP.Graph.CosmosGremlin;

public interface IGremlinClientFactory
{
    IGremlinClient Create();
}

public sealed class GremlinClientFactory : IGremlinClientFactory
{
    private readonly GremlinOptions _options;

    public GremlinClientFactory(IOptions<GremlinOptions> options) => _options = options.Value;

    /// <summary>
    /// Creates a Gremlin client configured for Cosmos DB Gremlin or other Gremlin servers.
    /// For Cosmos DB: Username must be in the form: /dbs/{db}/colls/{graph}; Password is the Cosmos account key.
    /// For non-Cosmos: Uses empty username and null password.
    /// Note: RequestTimeout is not enforced directly (future work G3); IsCosmos currently only toggles username/password formatting.
    /// </summary>
    public IGremlinClient Create()
    {
        // Validate options at the start
        _options.Validate();

        // Enforce GraphSON2 only for Cosmos Gremlin
        if (_options.GraphSONVersion == GraphSonVersion.GraphSON3)
        {
            throw new NotSupportedException(
                "Cosmos Gremlin only supports GraphSON2. Please update GremlinOptions.GraphSONVersion to GraphSonVersion.GraphSON2.");
        }

        // Conditionally set username/password based on IsCosmos
        string? username;
        string? password;
        
        if (_options.IsCosmos)
        {
            username = $"/dbs/{_options.Database}/colls/{_options.Graph}";
            password = _options.AuthKey;
        }
        else
        {
            username = string.Empty;
            password = null;
        }

        var server = new GremlinServer(
            hostname: _options.Host,
            port: _options.Port,
            enableSsl: _options.EnableSsl,
            username: username,
            password: password);

        // Always use GraphSON2MessageSerializer for Cosmos Gremlin compatibility
        var serializer = new GraphSON2MessageSerializer();

        // Pooling via GremlinClient constructor overload
        var connectionPoolSettings = new ConnectionPoolSettings
        {
            MaxInProcessPerConnection = 4,
            PoolSize = _options.ConnectionPoolSize,
            ReconnectionAttempts = 3,
            ReconnectionBaseDelay = TimeSpan.FromSeconds(2)
        };

        return new GremlinClient(
            gremlinServer: server,
            messageSerializer: serializer,
            connectionPoolSettings: connectionPoolSettings);
    }

    /// <summary>
    /// Static factory method for backward compatibility.
    /// For new code, prefer using dependency injection with AddCosmosGremlin.
    /// </summary>
    [Obsolete("Use dependency injection with AddCosmosGremlin and IGremlinClientFactory instead. This method will be removed in a future version.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IGremlinClient Create(GremlinOptions options)
    {
        var factory = new GremlinClientFactory(Microsoft.Extensions.Options.Options.Create(options));
        return factory.Create();
    }
}

#pragma warning restore CS1591
