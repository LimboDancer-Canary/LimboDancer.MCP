using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace LimboDancer.MCP.McpServer.Telemetry;

/// <summary>
/// Custom metrics for MCP server monitoring.
/// </summary>
public class McpMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _toolExecutionCount;
    private readonly Histogram<double> _toolExecutionDuration;
    private readonly Counter<long> _toolExecutionErrors;
    private readonly UpDownCounter<long> _activeSessions;
    private readonly Histogram<long> _requestSize;
    private readonly Histogram<long> _responseSize;

    public const string MeterName = "LimboDancer.MCP";

    public McpMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _toolExecutionCount = _meter.CreateCounter<long>(
            "mcp.tool.executions",
            description: "Number of tool executions");

        _toolExecutionDuration = _meter.CreateHistogram<double>(
            "mcp.tool.duration",
            unit: "ms",
            description: "Tool execution duration in milliseconds");

        _toolExecutionErrors = _meter.CreateCounter<long>(
            "mcp.tool.errors",
            description: "Number of tool execution errors");

        _activeSessions = _meter.CreateUpDownCounter<long>(
            "mcp.sessions.active",
            description: "Number of active MCP sessions");

        _requestSize = _meter.CreateHistogram<long>(
            "mcp.request.size",
            unit: "bytes",
            description: "Size of MCP requests");

        _responseSize = _meter.CreateHistogram<long>(
            "mcp.response.size",
            unit: "bytes",
            description: "Size of MCP responses");
    }

    public void RecordToolExecution(string toolName, string tenantId, bool success, double durationMs)
    {
        var tags = new TagList
        {
            { "tool.name", toolName },
            { "tenant.id", tenantId },
            { "success", success }
        };

        _toolExecutionCount.Add(1, tags);
        _toolExecutionDuration.Record(durationMs, tags);

        if (!success)
        {
            _toolExecutionErrors.Add(1, tags);
        }
    }

    public void RecordRequestSize(long bytes, string method)
    {
        _requestSize.Record(bytes, new TagList { { "method", method } });
    }

    public void RecordResponseSize(long bytes, string method, int statusCode)
    {
        _responseSize.Record(bytes, new TagList
        {
            { "method", method },
            { "status_code", statusCode }
        });
    }

    public void IncrementActiveSessions(string transport)
    {
        _activeSessions.Add(1, new TagList { { "transport", transport } });
    }

    public void DecrementActiveSessions(string transport)
    {
        _activeSessions.Add(-1, new TagList { { "transport", transport } });
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }
}