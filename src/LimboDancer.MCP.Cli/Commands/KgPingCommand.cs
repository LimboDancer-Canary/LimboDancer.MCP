using System.CommandLine;
using Gremlin.Net.Driver;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.MCP.Cli.Commands;

internal static class KgPingCommand
{
    public static Command Build()
    {
        var cmd = new Command("kg", "Knowledge graph utilities");
        var ping = new Command("ping", "Gremlin count");

        ping.SetHandler(async () =>
        {
            using var host = Bootstrap.BuildHost();
            var g = host.Services.GetRequiredService<IGremlinClient>();

            var rs = await g.SubmitAsync<long>("g.V().limit(1).count()");
            var count = rs.Count > 0 ? rs[0] : 0;
            Console.WriteLine($"Gremlin OK. Sample vertex count: {count}");
        });

        cmd.AddCommand(ping);
        return cmd;
    }
}