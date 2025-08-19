using System.CommandLine;

namespace LimboDancer.MCP.Cli.Commands;

internal static class ServeCommand
{
    public static Command Build()
    {
        var cmd = new Command("serve", "Run the MCP server");
        var stdio = new Option<bool>("--stdio", description: "Run in stdio mode.");
        cmd.AddOption(stdio);

        cmd.SetHandler((bool useStdio) =>
        {
            using var host = Bootstrap.BuildHost();

            if (useStdio)
            {
                Console.WriteLine("serve --stdio: stub runner — wire MCP here");
                return;
            }

            Console.WriteLine("No HTTP runner exposed via CLI yet.");
        }, stdio);

        return cmd;
    }
}