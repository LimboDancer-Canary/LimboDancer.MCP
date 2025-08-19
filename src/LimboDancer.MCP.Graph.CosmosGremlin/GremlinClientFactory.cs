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
    private readonly GremlinOptions _opts;

    public GremlinClientFactory(IOptions<GremlinOptions> options) => _opts = options.Value;

    /// <summary>
    /// Creates a Gremlin client configured for Cosmos DB Gremlin.
    /// Username must be in the form: /dbs/{db}/colls/{graph}
    /// Password is the Cosmos account key.
    /// </summary>
    public IGremlinClient Create()
    {
        var username = $"/dbs/{_opts.Database}/colls/{_opts.Graph}";
        var server = new GremlinServer(
            hostname: _opts.Host,
            port: _opts.Port,
            enableSsl: _opts.EnableSsl,
            username: username,
            password: _opts.AuthKey);

        // Cosmos Gremlin speaks GraphSON2 today
        IGraphSONMessageSerializer serializer = _opts.Serializer switch
        {
            GraphSonVersion.GraphSON3 => new GraphSON3MessageSerializer(),
            _ => new GraphSON2MessageSerializer()
        };

        // Pooling via GremlinClient constructor overload
        var connectionPoolSettings = new ConnectionPoolSettings
        {
            MaxInProcessPerConnection = 4,
            PoolSize = _opts.ConnectionPoolSize,
            ReconnectionAttempts = 3,
            ReconnectionBaseDelay = TimeSpan.FromSeconds(2)
        };

        return new GremlinClient(
            gremlinServer: server,
            messageSerializer: serializer,
            connectionPoolSettings: connectionPoolSettings);
    }

    // DI helper registration
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
