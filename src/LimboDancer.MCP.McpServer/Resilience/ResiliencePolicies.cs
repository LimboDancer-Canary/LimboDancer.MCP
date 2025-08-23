using System.Net;
using LimboDancer.MCP.McpServer.Configuration;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;

namespace LimboDancer.MCP.McpServer.Resilience;

public class ResiliencePolicies : IResiliencePolicies
{
    private readonly IOptions<McpOptions> _options;
    private readonly ILogger<ResiliencePolicies> _logger;
    private readonly Dictionary<string, IAsyncPolicy> _toolPolicies = new();

    public ResiliencePolicies(IOptions<McpOptions> options, ILogger<ResiliencePolicies> logger)
    {
        _options = options;
        _logger = logger;
        InitializePolicies();
    }

    private void InitializePolicies()
    {
        // Create a default policy for all tools
        var defaultPolicy = CreateDefaultToolPolicy();

        // Register tool-specific policies if needed
        _toolPolicies["default"] = defaultPolicy;
        _toolPolicies["graph_query"] = CreateGraphQueryPolicy();
        _toolPolicies["memory_search"] = CreateMemorySearchPolicy();
    }

    public IAsyncPolicy<T> GetToolExecutionPolicy<T>(string toolName)
    {
        if (_toolPolicies.TryGetValue(toolName, out var policy))
        {
            return policy.AsAsyncPolicy<T>();
        }
        return _toolPolicies["default"].AsAsyncPolicy<T>();
    }

    public IAsyncPolicy<HttpResponseMessage> GetHttpPolicy()
    {
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                _options.Value.RetryPolicy.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var requestUri = outcome.Result?.RequestMessage?.RequestUri;
                    _logger.LogWarning("HTTP retry {RetryCount} after {Delay}ms for {Uri}",
                        retryCount, timespan.TotalMilliseconds, requestUri);
                });

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                _options.Value.CircuitBreaker.FailureThreshold,
                _options.Value.CircuitBreaker.BreakDuration,
                onBreak: (result, duration) =>
                {
                    _logger.LogError("Circuit breaker opened for {Duration}", duration);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset");
                });

        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
    }

    private IAsyncPolicy CreateDefaultToolPolicy()
    {
        var retryOptions = _options.Value.RetryPolicy;
        var circuitOptions = _options.Value.CircuitBreaker;

        // Retry policy with exponential backoff and jitter
        var retryPolicy = Policy
            .Handle<TransientException>()
            .Or<TimeoutException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryOptions.MaxRetryAttempts,
                retryAttempt =>
                {
                    var exponentialDelay = TimeSpan.FromMilliseconds(
                        Math.Min(
                            retryOptions.BaseDelay.TotalMilliseconds * Math.Pow(2, retryAttempt - 1),
                            retryOptions.MaxDelay.TotalMilliseconds
                        )
                    );

                    // Add jitter
                    var jitter = exponentialDelay.TotalMilliseconds * retryOptions.JitterFactor * Random.Shared.NextDouble();
                    return exponentialDelay + TimeSpan.FromMilliseconds(jitter);
                },
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    var toolName = context.Values.ContainsKey("ToolName") ? context["ToolName"] : "unknown";
                    _logger.LogWarning(exception,
                        "Tool {ToolName} retry {RetryCount} after {Delay}ms",
                        toolName, retryCount, timeSpan.TotalMilliseconds);
                });

        // Circuit breaker policy
        var circuitBreakerPolicy = Policy
            .Handle<Exception>(ex => !(ex is OperationCanceledException))
            .CircuitBreakerAsync(
                circuitOptions.FailureThreshold,
                circuitOptions.BreakDuration,
                onBreak: (exception, duration, context) =>
                {
                    var toolName = context.Values.ContainsKey("ToolName") ? context["ToolName"] : "unknown";
                    _logger.LogError(exception,
                        "Circuit breaker opened for tool {ToolName} for {Duration}",
                        toolName, duration);
                },
                onReset: context =>
                {
                    var toolName = context.Values.ContainsKey("ToolName") ? context["ToolName"] : "unknown";
                    _logger.LogInformation("Circuit breaker reset for tool {ToolName}", toolName);
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit breaker is half-open, testing with next request");
                });

        // Timeout policy
        var timeoutPolicy = Policy.TimeoutAsync(
            _options.Value.MaxToolExecutionTime,
            TimeoutStrategy.Pessimistic,
            onTimeoutAsync: async (context, timespan, task) =>
            {
                var toolName = context.Values.ContainsKey("ToolName") ? context["ToolName"] : "unknown";
                _logger.LogError("Tool {ToolName} execution timed out after {Timeout}",
                    toolName, timespan);
            });

        // Combine policies: Timeout wraps CircuitBreaker wraps Retry
        return Policy.WrapAsync(timeoutPolicy, circuitBreakerPolicy, retryPolicy);
    }

    private IAsyncPolicy CreateGraphQueryPolicy()
    {
        // Graph queries might need different timeout
        var timeout = _options.Value.ToolTimeouts.GetValueOrDefault("graph_query",
            _options.Value.MaxToolExecutionTime);

        return Policy
            .Handle<TransientException>()
            .Or<Gremlin.Net.Driver.Exceptions.ResponseException>(ex =>
                ex.StatusCode == HttpStatusCode.TooManyRequests ||
                ex.StatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Graph query retry {RetryCount} after {Delay}ms",
                        retryCount, timeSpan.TotalMilliseconds);
                })
            .WrapAsync(Policy.TimeoutAsync(timeout));
    }

    private IAsyncPolicy CreateMemorySearchPolicy()
    {
        // Memory search might need different retry strategy
        var timeout = _options.Value.ToolTimeouts.GetValueOrDefault("memory_search",
            _options.Value.MaxToolExecutionTime);

        return Policy
            .Handle<TransientException>()
            .Or<Azure.RequestFailedException>(ex =>
                ex.Status == 429 || // Too Many Requests
                ex.Status == 503)   // Service Unavailable
            .WaitAndRetryAsync(
                2, // Fewer retries for search
                retryAttempt => TimeSpan.FromSeconds(retryAttempt),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Memory search retry {RetryCount} after {Delay}ms",
                        retryCount, timeSpan.TotalMilliseconds);
                })
            .WrapAsync(Policy.TimeoutAsync(timeout));
    }
}