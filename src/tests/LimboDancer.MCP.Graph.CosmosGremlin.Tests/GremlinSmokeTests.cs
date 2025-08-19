using FluentAssertions;
using Gremlin.Net.Process.Traversal;
using LimboDancer.MCP.Graph.CosmosGremlin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class GremlinSmokeTests
{
    private static GremlinOptions LoadFromEnv() => new GremlinOptions
    {
        Host = Environment.GetEnvironmentVariable("COSMOS_GREMLIN_HOST") ?? "",
        Port = int.TryParse(Environment.GetEnvironmentVariable("COSMOS_GREMLIN_PORT"), out var p) ? p : 443,
        EnableSsl = true,
        Database = Environment.GetEnvironmentVariable("COSMOS_GREMLIN_DB") ?? "",
        Graph = Environment.GetEnvironmentVariable("COSMOS_GREMLIN_GRAPH") ?? "",
        AuthKey = Environment.GetEnvironmentVariable("COSMOS_GREMLIN_KEY") ?? "",
        Serializer = GraphSonVersion.GraphSON2
    };

    [Fact(Skip = "Set COSMOS_GREMLIN_* env vars to run against a live Cosmos Gremlin API.")]
    public async Task Connect_And_Count_Succeeds()
    {
        var opts = LoadFromEnv();
        if (string.IsNullOrWhiteSpace(opts.Host)) return;

        var services = new ServiceCollection();
        services.AddOptions<GremlinOptions>().Configure(o =>
        {
            o.Host = opts.Host;
            o.Port = opts.Port;
            o.EnableSsl = opts.EnableSsl;
            o.Database = opts.Database;
            o.Graph = opts.Graph;
            o.AuthKey = opts.AuthKey;
            o.Serializer = opts.Serializer;
        });
        services.AddSingleton<IGremlinClientFactory, GremlinClientFactory>();

        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IGremlinClientFactory>();
        await using var client = factory.Create();

        // g.V().limit(1).count()
        var result = await client.SubmitAsync<long>("g.V().limit(1).count()");
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].Should().BeGreaterThanOrEqualTo(0);
    }
}
