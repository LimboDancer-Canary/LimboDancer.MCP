using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using LimboDancer.MCP.Core.Tenancy;
using Microsoft.Extensions.DependencyInjection;

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
        cmd.AddOption(stdio);
        cmd.AddOption(tenant);
        cmd.AddOption(package);
        cmd.AddOption(channel);

        cmd.SetHandler((bool useStdio, string? tenantOpt, string? pkg, string? chan) =>
        {
            using var host = Bootstrap.BuildHost();

            ApplyTenant(host, tenantOpt);

            if (useStdio)
            {
                Console.WriteLine("serve --stdio: stub runner – wire MCP here");
                return;
            }

            Console.WriteLine("No HTTP runner exposed via CLI yet.");
        }, stdio, tenant, package, channel);

        return cmd;
    }

    private static void ApplyTenant(IHost host, string? tenantOpt)
    {
        if (!string.IsNullOrWhiteSpace(tenantOpt))
        {
            AmbientTenantAccessor.Set(tenantOpt);
            return;
        }

        var env = host.Services.GetRequiredService<IHostEnvironment>();
        var cfg = host.Services.GetRequiredService<IConfiguration>();

        if (env.IsDevelopment())
        {
            var cfgTenant = cfg["Tenancy:DefaultTenantId"];
            if (!string.IsNullOrWhiteSpace(cfgTenant))
            {
                AmbientTenantAccessor.Set(cfgTenant);
            }
        }
    }
}