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

public sealed class GremlinClientFactory
{
    private readonly GremlinOptions _options;

    public GremlinClientFactory(IOptions<GremlinOptions> options) => _options = options.Value;

    /// <summary>Create a configured GremlinClient for Cosmos DB Gremlin API.</summary>
    public GremlinClient Create()
    {
        var connectionPoolSettings = new ConnectionPoolSettings { MaxInProcessPerConnection = 64, PoolSize = 4, ReconnectionAttempts = 3 };
        var gremlinServer = new GremlinServer(
            hostname: _options.Host,
            port: _options.Port,
            enableSsl: _options.UseSsl,
            username: $"/dbs/{_options.Database}/colls/{_options.Graph}",
            password: _options.AuthKey);

        // GraphSON3 is required for Cosmos Gremlin
        return new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType, connectionPoolSettings);
    }



}
