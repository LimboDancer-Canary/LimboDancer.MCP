using LimboDancer.MCP.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.MCP.Storage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));

        services.AddDbContext<ChatDbContext>(options =>
        {
            var connectionString = configuration["Storage:ConnectionString"]
                                   ?? configuration["Persistence:ConnectionString"] // legacy fallback
                                   ?? throw new InvalidOperationException("Storage:ConnectionString is required");

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IChatHistoryStore, ChatHistoryStore>();

        return services;
    }
}