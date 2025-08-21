using System;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Extensions.Options;

namespace LimboDancer.MCP.Graph.CosmosGremlin;

public interface IGremlinClientFactory
{
    IGremlinClient Create();
}

public sealed class GremlinClientFactory : IGremlinClientFactory
{
    private readonly GremlinOptions _options;

    public GremlinClientFactory(IOptions<GremlinOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Create a configured GremlinClient for Cosmos DB Gremlin API.</summary>
    public IGremlinClient Create()
    {
        var connectionPoolSettings = new ConnectionPoolSettings
        {
            MaxInProcessPerConnection = 64,
            PoolSize = 4,
            ReconnectionAttempts = 3
        };

        var gremlinServer = new GremlinServer(
            hostname: _options.Host,
            port: _options.Port,
            enableSsl: _options.EnableSsl,
            username: $"/dbs/{_options.Database}/colls/{_options.Graph}",
            password: _options.AuthKey);

        // Cosmos Gremlin currently uses GraphSON2
        var serializer = new GraphSON2MessageSerializer();

        return new GremlinClient(
            gremlinServer: gremlinServer,
            messageSerializer: serializer,
            connectionPoolSettings: connectionPoolSettings);
    }
}
