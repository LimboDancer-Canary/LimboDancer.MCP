using System.Diagnostics;
using System.Text.Json;
using OpenTelemetry.Trace;

namespace LimboDancer.MCP.McpServer.Telemetry;

/// <summary>
/// Instrumented version of McpServer with telemetry.
/// </summary>
public class InstrumentedMcpServer : McpServer
{
    private readonly McpMetrics _metrics;
    private readonly ILogger<InstrumentedMcpServer> _logger;

    public InstrumentedMcpServer(
        IServiceProvider serviceProvider,
        McpMetrics metrics,
        ILogger<InstrumentedMcpServer> logger)
        : base(serviceProvider, logger)
    {
        _metrics = metrics;
        _logger = logger;
    }

    public override async Task<ToolResult> ExecuteToolAsync(string toolName, JsonElement arguments, CancellationToken ct = default)
    {
        using var activity = McpActivitySource.Instance.StartActivity("mcp.tool.execute", ActivityKind.Server);
        activity?.SetTag("tool.name", toolName);
        activity?.SetTag("tenant.id", GetCurrentTenantId());

        var stopwatch = Stopwatch.StartNew();
        bool success = false;

        try
        {
            var result = await base.ExecuteToolAsync(toolName, arguments, ct);
            success = !result.IsError;

            activity?.SetTag("tool.success", success);
            return result;
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordToolExecution(toolName, GetCurrentTenantId(), success, stopwatch.ElapsedMilliseconds);

            _logger.LogInformation(
                "Tool {ToolName} executed in {Duration}ms with result: {Success}",
                toolName, stopwatch.ElapsedMilliseconds, success ? "success" : "failure");
        }
    }

    private string GetCurrentTenantId()
    {
        // Get from service provider or context
        return "default";
    }
}