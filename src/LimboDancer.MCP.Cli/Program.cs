using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LimboDancer.MCP.Cli.Commands;

var root = new RootCommand("LimboDancer.MCP CLI");

root.AddCommand(ServeCommand.Build());
root.AddCommand(DbMigrateCommand.Build());
root.AddCommand(VectorInitCommand.Build());
root.AddCommand(KgPingCommand.Build());
root.AddCommand(MemAddCommand.Build());
root.AddCommand(MemSearchCommand.Build());

return await root.InvokeAsync(args);

// Shared host bootstrap for all commands
internal static class Bootstrap
{
    public static IHost BuildHost(string[]? args = null)
    {
        var b = Host.CreateApplicationBuilder(args ?? Array.Empty<string>());

        b.Configuration
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables();

        ServicesBootstrap.Configure(b);
        return b.Build();
    }
}