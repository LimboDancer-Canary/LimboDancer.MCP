using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LimboDancer.MCP.McpServer.Telemetry;

/// <summary>
/// Extension methods for telemetry configuration.
/// </summary>
public static class TelemetryExtensions
{
    public static IServiceCollection AddMcpTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TelemetryOptions>(configuration.GetSection(TelemetryOptions.SectionName));
        services.AddSingleton<McpMetrics>();

        var telemetryOptions = configuration
            .GetSection(TelemetryOptions.SectionName)
            .Get<TelemetryOptions>() ?? new TelemetryOptions();

        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: telemetryOptions.ServiceName,
                    serviceVersion: telemetryOptions.ServiceVersion);

                // Add custom resource attributes
                foreach (var attr in telemetryOptions.ResourceAttributes)
                {
                    resource.AddAttributes(new[] { new KeyValuePair<string, object>(attr.Key, attr.Value) });
                }

                // Add environment detection
                resource.AddDetector(new EnvironmentResourceDetector());
            })
            .WithTracing(tracing =>
            {
                if (!telemetryOptions.EnableTracing) return;

                tracing
                    .AddSource(McpActivitySource.Name)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = (httpContext) =>
                        {
                            // Don't trace health checks
                            return !httpContext.Request.Path.StartsWithSegments("/health");
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                    })
                    .SetSampler(new TraceIdRatioBasedSampler(0.1)); // Sample 10% of traces

                if (!string.IsNullOrEmpty(telemetryOptions.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(telemetryOptions.OtlpEndpoint);
                    });
                }
                else
                {
                    tracing.AddConsoleExporter(); // For development
                }
            })
            .WithMetrics(metrics =>
            {
                if (!telemetryOptions.EnableMetrics) return;

                metrics
                    .AddMeter("LimboDancer.MCP")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddView("mcp.tool.duration",
                        new ExplicitBucketHistogramConfiguration
                        {
                            Boundaries = new double[] { 10, 50, 100, 250, 500, 1000, 2500, 5000, 10000 }
                        });

                if (!string.IsNullOrEmpty(telemetryOptions.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(telemetryOptions.OtlpEndpoint);
                    });
                }
                else
                {
                    metrics.AddConsoleExporter(); // For development
                }
            });

        if (telemetryOptions.EnableLogging)
        {
            services.AddLogging(logging =>
            {
                logging.AddOpenTelemetry(options =>
                {
                    options.SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(telemetryOptions.ServiceName, telemetryOptions.ServiceVersion));

                    if (!string.IsNullOrEmpty(telemetryOptions.OtlpEndpoint))
                    {
                        options.AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(telemetryOptions.OtlpEndpoint);
                        });
                    }
                });
            });
        }

        return services;
    }

    public static IApplicationBuilder UseMcpTelemetry(this IApplicationBuilder app)
    {
        // Add custom middleware for request/response size tracking
        app.UseMiddleware<TelemetryMiddleware>();
        return app;
    }
}