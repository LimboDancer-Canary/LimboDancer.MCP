using FluentAssertions;
using Gremlin.Net.Driver;
using Gremlin.Net.Process.Traversal;
using LimboDancer.MCP.Graph.CosmosGremlin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

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
        GraphSONVersion = GraphSonVersion.GraphSON2
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
            o.GraphSONVersion = opts.GraphSONVersion;
        });
        services.AddSingleton<IGremlinClientFactory, GremlinClientFactory>();

        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IGremlinClientFactory>();
        using var client = factory.Create(); // Changed from 'await using' to 'using'

        // g.V().limit(1).count()
        var result = await client.SubmitAsync<long>("g.V().limit(1).count()");
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result.First().Should().BeGreaterThanOrEqualTo(0);
    }
}