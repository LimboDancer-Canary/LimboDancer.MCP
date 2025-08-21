using System.ComponentModel;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
    /// Creates a Gremlin client configured for Cosmos DB Gremlin.
    /// Username must be in the form: /dbs/{db}/colls/{graph}
    /// Password is the Cosmos account key.
    /// </summary>
    public IGremlinClient Create()
    {
        // Enforce GraphSON2 only for Cosmos Gremlin
        if (_options.Serializer == GraphSonVersion.GraphSON3)
        {
            throw new NotSupportedException(
                "Cosmos Gremlin only supports GraphSON2. Please update GremlinOptions.Serializer to GraphSonVersion.GraphSON2.");
        }

        var username = $"/dbs/{_options.Database}/colls/{_options.Graph}";
        var server = new GremlinServer(
            hostname: _options.Host,
            port: _options.Port,
            enableSsl: _options.EnableSsl,
            username: username,
            password: _options.AuthKey);

        // Always use GraphSON2MessageSerializer for Cosmos Gremlin
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

/// <summary>
/// Extension methods for dependency injection configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// DI helper registration for Cosmos Gremlin client factory.
    /// </summary>
    public static IServiceCollection AddCosmosGremlin(
        this IServiceCollection services,
        IConfiguration config,
        string sectionName = "CosmosGremlin")
    {
        services.Configure<GremlinOptions>(config.GetSection(sectionName));
        services.AddSingleton<IGremlinClientFactory, GremlinClientFactory>();
        return services;
    }
}
