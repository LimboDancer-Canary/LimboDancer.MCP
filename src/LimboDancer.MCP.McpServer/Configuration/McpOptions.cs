// File: McpOptions.cs
using System.ComponentModel.DataAnnotations;

namespace LimboDancer.MCP.McpServer.Configuration;

/// <summary>
/// Configuration options for the MCP server.
/// </summary>
public class McpOptions
{
    public const string SectionName = "Mcp";

    /// <summary>
    /// Maximum time allowed for tool execution before timeout.
    /// </summary>
    [Required]
    public TimeSpan MaxToolExecutionTime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Enable detailed request/response logging for debugging.
    /// </summary>
    public bool EnableRequestLogging { get; set; } = false;

    /// <summary>
    /// Enable tool execution metrics collection.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Maximum concurrent tool executions per tenant.
    /// </summary>
    [Range(1, 100)]
    public int MaxConcurrentToolExecutions { get; set; } = 10;

    /// <summary>
    /// Tool-specific timeout overrides.
    /// </summary>
    public Dictionary<string, TimeSpan> ToolTimeouts { get; set; } = new();

    /// <summary>
    /// Retry policy configuration.
    /// </summary>
    public RetryPolicyOptions RetryPolicy { get; set; } = new();

    /// <summary>
    /// Circuit breaker configuration.
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
}

// File: appsettings.json
/*
{
  "Mcp": {
    "MaxToolExecutionTime": "00:05:00",
    "EnableRequestLogging": false,
    "EnableMetrics": true,
    "MaxConcurrentToolExecutions": 10,
    "ToolTimeouts": {
      "graph_query": "00:02:00",
      "memory_search": "00:01:30"
    },
    "RetryPolicy": {
    "MaxRetryAttempts": 3,
      "BaseDelay": "00:00:01",
      "MaxDelay": "00:00:30",
      "JitterFactor": 0.2
    },
    "CircuitBreaker": {
    "FailureThreshold": 5,
      "BreakDuration": "00:00:30",
      "SamplingDuration": "00:01:00",
      "MinimumThroughput": 10
    }
  },
  "Logging": {
    "LogLevel": {
        "Default": "Information",
      "LimboDancer.MCP.McpServer": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
},
  "AllowedHosts": "*",
  "Tenancy": {
    "DefaultTenantId": "00000000-0000-0000-0000-000000000000",
    "RequireTenantHeader": true
  },
  "Storage": {
    "ConnectionString": "Host=localhost;Database=limbodancer;Username=postgres;Password=postgres"
  },
  "Vector": {
    "Endpoint": "https://your-search.search.windows.net",
    "ApiKey": "your-api-key",
    "IndexName": "ldm-memory",
    "VectorDimensions": 1536
  },
  "Graph": {
    "Endpoint": "wss://your-cosmos.gremlin.cosmos.azure.com:443/",
    "Database": "ldm",
    "Container": "kg",
    "AuthKey": "your-auth-key"
  },
  "Authentication": {
    "Authority": "https://login.microsoftonline.com/your-tenant-id",
    "Audience": "api://your-app-id"
  },
  "OpenTelemetry": {
    "ServiceName": "LimboDancer.MCP",
    "ServiceVersion": "1.0.0",
    "OtlpEndpoint": "http://localhost:4317",
    "EnableTracing": true,
    "EnableMetrics": true,
    "EnableLogging": false
  }
}

*/