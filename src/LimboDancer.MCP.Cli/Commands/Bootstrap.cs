using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LimboDancer.MCP.Cli.Commands;

/// <summary>
/// Bootstrap helper for CLI commands that need DI container.
/// </summary>
internal static class Bootstrap
{
    /// <summary>
    /// Build a host with standard services configured.
    /// </summary>
    public static IHost BuildHost()
    {
        return CreateHostBuilder().Build();
    }

    /// <summary>
    /// Create a host builder with standard configuration.
    /// </summary>
    public static HostApplicationBuilder CreateHostBuilder()
    {
        var builder = Host.CreateApplicationBuilder();

        // Configure configuration sources
        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();

        // Configure logging
        builder.Logging
            .ClearProviders()
            .AddConsole()
            .SetMinimumLevel(LogLevel.Information);

        // Configure services
        ServicesBootstrap.Configure(builder);

        return builder;
    }
}