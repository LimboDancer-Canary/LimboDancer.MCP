using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace LimboDancer.MCP.McpServer.Telemetry;

/// <summary>
/// Extension methods for configuring telemetry.
/// </summary>
public static class TelemetryExtensions
{
    public static IServiceCollection AddMcpTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        // Register custom metrics
        services.AddSingleton<McpMetrics>();

        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService("LimboDancer.MCP")
                .AddDetector(sp => new EnvironmentResourceDetector()))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(McpMetrics.MeterName)
                .AddConsoleExporter()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(configuration["Telemetry:OtlpEndpoint"] ?? "http://localhost:4317");
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(McpActivitySource.ActivitySourceName)
                .AddConsoleExporter()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(configuration["Telemetry:OtlpEndpoint"] ?? "http://localhost:4317");
                }));

        return services;
    }
}