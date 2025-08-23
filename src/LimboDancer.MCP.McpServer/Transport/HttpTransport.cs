using LimboDancer.MCP.Core.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LimboDancer.MCP.McpServer.Transport;

/// <summary>
/// HTTP endpoints for MCP protocol communication.
/// </summary>
[ApiController]
[Route("api/mcp")]
[Authorize]
public partial class McpController : ControllerBase
{
    private readonly McpServer _mcpServer;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly ILogger<McpController> _logger;

    public McpController(
        McpServer mcpServer,
        ITenantAccessor tenantAccessor,
        ILogger<McpController> logger)
    {
        _mcpServer = mcpServer ?? throw new ArgumentNullException(nameof(mcpServer));
        _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initialize MCP session.
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize([FromBody] JsonElement request)
    {
        _logger.LogInformation("HTTP initialize request from tenant {TenantId}", _tenantAccessor.TenantId);

        var response = new
        {
            protocolVersion = "2024-11-01",
            capabilities = new
            {
                tools = new { }
            },
            serverInfo = new
            {
                name = "LimboDancer.MCP",
                version = "1.0.0"
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// List available tools.
    /// </summary>
    [HttpGet("tools")]
    public async Task<IActionResult> ListTools()
    {
        _logger.LogInformation("HTTP list tools request from tenant {TenantId}", _tenantAccessor.TenantId);

        var tools = _mcpServer.GetTools();
        var response = new
        {
            tools = tools.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                inputSchema = t.InputSchema
            })
        };

        return Ok(response);
    }

    /// <summary>
    /// Execute a tool.
    /// </summary>
    [HttpPost("tools/{toolName}")]
    public async Task<IActionResult> ExecuteTool(string toolName, [FromBody] JsonElement arguments)
    {
        _logger.LogInformation("HTTP execute tool {ToolName} for tenant {TenantId}",
            toolName, _tenantAccessor.TenantId);

        try
        {
            var result = await _mcpServer.ExecuteToolAsync(toolName, arguments, HttpContext.RequestAborted);

            // The ExecuteToolAsync method returns a JsonElement directly containing the result
            // Errors are handled via exceptions, so if we get here, it was successful
            return Ok(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = result.GetRawText()
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            return StatusCode(500, new
            {
                error = new
                {
                    code = -32603,
                    message = "Internal error",
                    data = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Server-Sent Events endpoint for streaming updates.
    /// </summary>
    [HttpGet("events")]
    public async Task McpEvents()
    {
        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");

        _logger.LogInformation("SSE connection established for tenant {TenantId}", _tenantAccessor.TenantId);

        // Send initial connection event
        await WriteEventAsync("connected", new { tenant = _tenantAccessor.TenantId });

        // Keep connection alive and handle tool execution events
        var cancellationToken = HttpContext.RequestAborted;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Send heartbeat every 30 seconds
                await Task.Delay(30000, cancellationToken);
                await WriteEventAsync("heartbeat", new { timestamp = DateTime.UtcNow });
            }
        }
        catch (TaskCanceledException)
        {
            // Client disconnected
        }
        finally
        {
            _logger.LogInformation("SSE connection closed for tenant {TenantId}", _tenantAccessor.TenantId);
        }
    }

    private async Task WriteEventAsync(string eventType, object data)
    {
        var json = JsonSerializer.Serialize(data);
        await Response.WriteAsync($"event: {eventType}\n");
        await Response.WriteAsync($"data: {json}\n\n");
        await Response.Body.FlushAsync();
    }
}

/// <summary>
/// Extension methods for configuring MCP HTTP endpoints.
/// </summary>
public static class McpEndpointExtensions
{
    public static IServiceCollection AddMcpServer(this IServiceCollection services)
    {
        services.AddSingleton<McpServer>();
        services.AddHostedService<McpServerHostedService>();
        return services;
    }

    public static IEndpointRouteBuilder MapMcpEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();
        return endpoints;
    }
}

/// <summary>
/// Background service for MCP server lifecycle management.
/// </summary>
internal class McpServerHostedService : IHostedService
{
    private readonly McpServer _mcpServer;
    private readonly ILogger<McpServerHostedService> _logger;

    public McpServerHostedService(McpServer mcpServer, ILogger<McpServerHostedService> logger)
    {
        _mcpServer = mcpServer;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MCP server started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MCP server stopping");
        _mcpServer.Dispose();
        return Task.CompletedTask;
    }
}