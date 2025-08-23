using LimboDancer.MCP.McpServer.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace LimboDancer.MCP.McpServer.Transport;

/// <summary>
/// Enhanced MCP manifest generation with detailed tool metadata.
/// </summary>
public partial class McpController
{
    private readonly IOptions<McpOptions> _mcpOptions;

    // Add this to the constructor parameters
    // IOptions<McpOptions> mcpOptions

    /// <summary>
    /// Get the complete MCP server manifest including all tools and capabilities.
    /// </summary>
    [HttpGet("manifest")]
    [AllowAnonymous] // Allow public access to manifest
    public async Task<IActionResult> GetManifest()
    {
        var tools = _mcpServer.GetTools();
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
        var buildDate = File.GetLastWriteTimeUtc(assembly.Location);

        var manifest = new
        {
            name = "LimboDancer.MCP",
            version = version,
            description = "Azure-first Model Context Protocol server with ontology integration",
            buildDate = buildDate.ToString("O"),
            protocol = new
            {
                version = "2024-11-01",
                features = new[]
                {
                    "tools",
                    "streaming",
                    "cancellation",
                    "progress"
                }
            },
            server = new
            {
                capabilities = new
                {
                    tools = new
                    {
                        supportsProgress = true,
                        supportsCancellation = true,
                        maxConcurrentExecutions = _mcpOptions.Value.MaxConcurrentToolExecutions
                    },
                    transports = new[]
                    {
                        new
                        {
                            type = "stdio",
                            description = "Standard input/output for CLI integration"
                        },
                        new
                        {
                            type = "http",
                            description = "HTTP/REST API with authentication",
                            endpoints = new
                            {
                                baseUrl = $"{Request.Scheme}://{Request.Host}/api/mcp",
                                authentication = "Bearer JWT"
                            }
                        },
                        new
                        {
                            type = "sse",
                            description = "Server-Sent Events for real-time updates",
                            endpoint = $"{Request.Scheme}://{Request.Host}/api/mcp/events"
                        }
                    }
                },
                limits = new
                {
                    maxRequestSize = "10MB",
                    maxToolExecutionTime = _mcpOptions.Value.MaxToolExecutionTime.ToString(),
                    rateLimits = new
                    {
                        requestsPerMinute = 60,
                        toolExecutionsPerMinute = 30
                    }
                }
            },
            tools = tools.Select(tool => new
            {
                name = tool.Name,
                description = tool.Description,
                category = GetToolCategory(tool.Name),
                inputSchema = tool.InputSchema,
                outputSchema = GetOutputSchema(tool.Name),
                examples = GetToolExamples(tool.Name),
                permissions = GetToolPermissions(tool.Name),
                timeout = GetToolTimeout(tool.Name),
                retryable = IsToolRetryable(tool.Name),
                tags = GetToolTags(tool.Name)
            }).ToArray(),
            ontology = new
            {
                enabled = true,
                baseUri = "https://example.org/ldm#",
                prefixes = new
                {
                    ldm = "https://example.org/ldm#",
                    kg = "https://example.org/kg#",
                    schema = "http://schema.org/",
                    xsd = "http://www.w3.org/2001/XMLSchema#"
                }
            },
            documentation = new
            {
                apiDocs = $"{Request.Scheme}://{Request.Host}/api/docs",
                userGuide = "https://github.com/yourusername/LimboDancer.MCP/wiki",
                examples = "https://github.com/yourusername/LimboDancer.MCP/examples"
            }
        };

        return Ok(manifest);
    }

    private string GetToolCategory(string toolName) => toolName switch
    {
        "history_get" or "history_append" => "Session Management",
        "memory_search" => "Memory & Retrieval",
        "graph_query" => "Knowledge Graph",
        _ => "General"
    };

    private object? GetOutputSchema(string toolName)
    {
        // In a real implementation, parse the tool schema to extract output schema
        return toolName switch
        {
            "history_get" => new
            {
                type = "object",
                properties = new
                {
                    sessionId = new { type = "string" },
                    messages = new { type = "array" }
                }
            },
            _ => null
        };
    }

    private object[] GetToolExamples(string toolName) => toolName switch
    {
        "history_get" => new[]
        {
            new
            {
                description = "Get last 10 messages from a session",
                input = new
                {
                    sessionId = "123e4567-e89b-12d3-a456-426614174000",
                    limit = 10
                }
            }
        },
        "memory_search" => new[]
        {
            new
            {
                description = "Search for documents about Azure",
                input = new
                {
                    queryText = "Azure cloud services",
                    k = 5,
                    ontologyClass = "TechnicalDocument"
                }
            }
        },
        _ => Array.Empty<object>()
    };

    private string[] GetToolPermissions(string toolName) => toolName switch
    {
        "history_append" => new[] { "write:history", "write:session" },
        "history_get" => new[] { "read:history", "read:session" },
        "memory_search" => new[] { "read:memory" },
        "graph_query" => new[] { "read:graph" },
        _ => new[] { "execute:tool" }
    };

    private string GetToolTimeout(string toolName)
    {
        if (_mcpOptions.Value.ToolTimeouts.TryGetValue(toolName, out var timeout))
        {
            return timeout.ToString();
        }
        return _mcpOptions.Value.MaxToolExecutionTime.ToString();
    }

    private bool IsToolRetryable(string toolName) => toolName switch
    {
        "history_append" => false, // Don't retry writes
        _ => true // Most tools are safe to retry
    };

    private string[] GetToolTags(string toolName) => toolName switch
    {
        "history_get" or "history_append" => new[] { "history", "session", "chat" },
        "memory_search" => new[] { "memory", "search", "vector", "retrieval" },
        "graph_query" => new[] { "graph", "knowledge", "query" },
        _ => Array.Empty<string>()
    };
}