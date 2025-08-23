namespace LimboDancer.MCP.McpServer.Telemetry;

/// <summary>
/// Middleware for capturing telemetry data.
/// </summary>
public class TelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly McpMetrics _metrics;

    public TelemetryMiddleware(RequestDelegate next, McpMetrics metrics)
    {
        _next = next;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Capture request size
        if (context.Request.ContentLength.HasValue)
        {
            _metrics.RecordRequestSize(context.Request.ContentLength.Value, context.Request.Method);
        }

        // Capture response size
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        _metrics.RecordResponseSize(responseBody.Length, context.Request.Method, context.Response.StatusCode);

        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }
}