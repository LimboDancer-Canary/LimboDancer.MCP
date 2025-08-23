using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using LimboDancer.MCP.Core.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LimboDancer.MCP.McpServer.Transport;

namespace LimboDancer.MCP.Cli.Commands;

internal static class ServeCommand
{
    public static Command Build()
    {
        var cmd = new Command("serve", "Run the MCP server");
        var stdio = new Option<bool>("--stdio", description: "Run in stdio mode.");
        var tenant = new Option<string?>("--tenant", description: "Tenant Id (GUID). Defaults in Development from config.");
        var package = new Option<string?>("--package", description: "Optional package identifier.");
        var channel = new Option<string?>("--channel", description: "Optional channel identifier.");
        var verbose = new Option<bool>("--verbose", description: "Enable verbose logging.");

        cmd.AddOption(stdio);
        cmd.AddOption(tenant);
        cmd.AddOption(package);
        cmd.AddOption(channel);
        cmd.AddOption(verbose);

        cmd.SetHandler(async (bool useStdio, string? tenantOpt, string? pkg, string? chan, bool verbose) =>
        {
            // Build the host with MCP server registered
            var hostBuilder = Host.CreateApplicationBuilder();

            // Configure services
            hostBuilder.Services.AddSingleton<LimboDancer.MCP.McpServer.McpServer>();

            // Configure logging
            hostBuilder.Services.AddLogging(logging =>
            {
                logging.ClearProviders();

                if (useStdio)
                {
                    // In stdio mode, only log errors to stderr
                    logging.AddFilter("*", verbose ? LogLevel.Debug : LogLevel.Error);
                    logging.AddProvider(new StderrLoggerProvider());
                }
                else
                {
                    // In HTTP mode, use normal console logging
                    logging.AddConsole();
                    logging.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
                }
            });

            using var host = hostBuilder.Build();

            // Apply tenant configuration
            ApplyTenant(host, tenantOpt);

            if (useStdio)
            {
                await RunStdioModeAsync(host);
            }
            else
            {
                // For HTTP mode, run the full web host
                Console.WriteLine("Starting MCP HTTP server...");
                await host.RunAsync();
            }
        }, stdio, tenant, package, channel, verbose);

        return cmd;
    }

    private static async Task RunStdioModeAsync(IHost host)
    {
        var logger = host.Services.GetRequiredService<ILogger<StdioTransport>>();
        var mcpServer = host.Services.GetRequiredService<LimboDancer.MCP.McpServer.McpServer>();

        try
        {
            using var transport = new StdioTransport(mcpServer, logger);

            // Send initialization message to stderr
            Console.Error.WriteLine("MCP server ready (stdio mode)");

            // Run until cancelled
            await transport.RunAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void ApplyTenant(IHost host, string? tenantOpt)
    {
        var tenantAccessor = host.Services.GetRequiredService<ITenantAccessor>();

        if (!string.IsNullOrWhiteSpace(tenantOpt))
        {
            if (Guid.TryParse(tenantOpt, out var tenantGuid))
            {
                if (tenantAccessor is AmbientTenantAccessor)
                {
                    AmbientTenantAccessor.Set(tenantGuid);
                }
                return;
            }
        }

        var env = host.Services.GetRequiredService<IHostEnvironment>();
        var cfg = host.Services.GetRequiredService<IConfiguration>();

        if (env.IsDevelopment())
        {
            var cfgTenant = cfg["Tenancy:DefaultTenantId"];
            if (!string.IsNullOrWhiteSpace(cfgTenant) && Guid.TryParse(cfgTenant, out var guid))
            {
                if (tenantAccessor is AmbientTenantAccessor)
                {
                    AmbientTenantAccessor.Set(guid);
                }
            }
        }
    }
}