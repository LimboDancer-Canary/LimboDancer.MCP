using LimboDancer.MCP.McpServer.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Core;
using ModelContextProtocol.Core.Models;
using ModelContextProtocol.Core.Tools;
using ModelContextProtocol.Protocol;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LimboDancer.MCP.McpServer;

/// <summary>
/// MCP server implementation that handles tool registration and request routing.
/// </summary>
public class McpServer : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<McpServer> _logger;
    private readonly Dictionary<string, ToolRegistration> _tools = new();
    private readonly JsonSerializerOptions _jsonOptions;

    public McpServer(IServiceProvider serviceProvider, ILogger<McpServer> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        RegisterTools();
    }

    private void RegisterTools()
    {
        // Register each tool with its schema and executor
        RegisterTool<HistoryGetTool>("history_get",
            HistoryGetTool.ToolSchema,
            async (tool, input, ct) => await tool.ExecuteAsync(input, ct));

        RegisterTool<HistoryAppendTool>("history_append",
            HistoryAppendTool.ToolSchema,
            async (tool, input, ct) => await tool.ExecuteAsync(input, ct));

        RegisterTool<GraphQueryTool>("graph_query",
            GraphQueryTool.ToolSchema,
            async (tool, input, ct) => await tool.ExecuteAsync(input, ct));

        RegisterTool<MemorySearchTool>("memory_search",
            MemorySearchTool.ToolSchema,
            async (tool, input, ct) => await tool.ExecuteAsync(input, ct));

        _logger.LogInformation("Registered {Count} MCP tools", _tools.Count);
    }

    private void RegisterTool<T>(string name, string schema, Func<T, JsonElement, CancellationToken, Task<object>> executor)
        where T : class
    {
        _tools[name] = new ToolRegistration
        {
            Name = name,
            Schema = JsonDocument.Parse(schema).RootElement,
            ToolType = typeof(T),
            Executor = async (serviceProvider, input, ct) =>
            {
                var tool = serviceProvider.GetRequiredService<T>();
                var result = await executor(tool, input, ct);
                return JsonSerializer.SerializeToElement(result, _jsonOptions);
            }
        };
    }

    /// <summary>
    /// Get all registered tools for discovery.
    /// </summary>
    public IReadOnlyList<Tool> GetTools()
    {
        return _tools.Values.Select(reg => new Tool
        {
            Name = reg.Name,
            Description = GetToolDescription(reg.Schema),
            InputSchema = reg.Schema
        }).ToList();
    }

    /// <summary>
    /// Execute a tool by name with the given arguments.
    /// </summary>
    public async Task<ToolResult> ExecuteToolAsync(string toolName, JsonElement arguments, CancellationToken ct = default)
    {
        if (!_tools.TryGetValue(toolName, out var registration))
        {
            _logger.LogWarning("Tool {ToolName} not found", toolName);
            return new ToolResult
            {
                ToolUseId = Guid.NewGuid().ToString(),
                Content = new[]
                {
                    new TextContent { Text = $"Tool '{toolName}' not found" }
                },
                IsError = true
            };
        }

        try
        {
            _logger.LogInformation("Executing tool {ToolName}", toolName);

            // Create a scoped service provider for the tool execution
            using var scope = _serviceProvider.CreateScope();
            var result = await registration.Executor(scope.ServiceProvider, arguments, ct);

            return new ToolResult
            {
                ToolUseId = Guid.NewGuid().ToString(),
                Content = new[]
                {
                    new TextContent { Text = result.GetRawText() }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            return new ToolResult
            {
                ToolUseId = Guid.NewGuid().ToString(),
                Content = new[]
                {
                    new TextContent { Text = $"Error executing tool: {ex.Message}" }
                },
                IsError = true
            };
        }
    }

    private string GetToolDescription(JsonElement schema)
    {
        if (schema.TryGetProperty("description", out var desc))
            return desc.GetString() ?? "";

        if (schema.TryGetProperty("title", out var title))
            return title.GetString() ?? "";

        return "";
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    private record ToolRegistration
    {
        public required string Name { get; init; }
        public required JsonElement Schema { get; init; }
        public required Type ToolType { get; init; }
        public required Func<IServiceProvider, JsonElement, CancellationToken, Task<JsonElement>> Executor { get; init; }
    }
}