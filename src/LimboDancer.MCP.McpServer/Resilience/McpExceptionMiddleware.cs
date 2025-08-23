using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using Polly.CircuitBreaker;

namespace LimboDancer.MCP.McpServer.Resilience;

/// <summary>
/// Global exception handler middleware.
/// </summary>
public class McpExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<McpExceptionMiddleware> _logger;

    public McpExceptionMiddleware(RequestDelegate next, ILogger<McpExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var (statusCode, errorResponse) = exception switch
        {
            OperationCanceledException => (StatusCodes.Status408RequestTimeout,
                new McpErrorResponse
                {
                    ErrorCode = "TIMEOUT",
                    Message = "The operation was cancelled or timed out",
                    TraceId = traceId
                }),

            BrokenCircuitException => (StatusCodes.Status503ServiceUnavailable,
                new McpErrorResponse
                {
                    ErrorCode = "CIRCUIT_BREAKER_OPEN",
                    Message = "Service temporarily unavailable due to circuit breaker",
                    TraceId = traceId,
                    RetryInfo = new RetryInfo
                    {
                        CanRetry = true,
                        RetryAfter = TimeSpan.FromSeconds(30)
                    }
                }),

            ValidationException valEx => (StatusCodes.Status400BadRequest,
                new McpErrorResponse
                {
                    ErrorCode = "VALIDATION_ERROR",
                    Message = valEx.Message,
                    Details = valEx.ValidationResult?.ErrorMessage,
                    TraceId = traceId
                }),

            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized,
                new McpErrorResponse
                {
                    ErrorCode = "UNAUTHORIZED",
                    Message = "Authentication required",
                    TraceId = traceId
                }),

            _ => (StatusCodes.Status500InternalServerError,
                new McpErrorResponse
                {
                    ErrorCode = "INTERNAL_ERROR",
                    Message = "An unexpected error occurred",
                    Details = exception.Message,
                    TraceId = traceId
                })
        };

        _logger.LogError(exception,
            "Unhandled exception occurred. TraceId: {TraceId}, ErrorCode: {ErrorCode}",
            traceId, errorResponse.ErrorCode);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}