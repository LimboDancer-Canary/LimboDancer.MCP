using System.CommandLine;
using LimboDancer.MCP.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.MCP.Cli.Commands;

internal static class DbMigrateCommand
{
    public static Command Build()
    {
        var cmd = new Command("db", "Database utilities");
        var migrate = new Command("migrate", "Apply EF Core migrations (Chat; Audit optional later)");

        migrate.SetHandler(async () =>
        {
            using var host = Bootstrap.BuildHost();
            await using var scope = host.Services.CreateAsyncScope();

            var chat = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            Console.WriteLine("Applying ChatDbContext migrations…");
            await chat.Database.MigrateAsync();
            Console.WriteLine("Done.");

            // If/when you add AuditDbContext, repeat here (guarded).
        });

        cmd.AddCommand(migrate);
        return cmd;
    }
}