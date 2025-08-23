using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace LimboDancer.MCP.McpServer.Transport;

/// <summary>
/// MCP standard input/output transport for CLI mode.
/// </summary>
public class StdioTransport : IDisposable
{
    private readonly McpServer _mcpServer;
    private readonly ILogger<StdioTransport> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly JsonRpc _jsonRpc;

    public StdioTransport(McpServer mcpServer, ILogger<StdioTransport> logger)
    {
        _mcpServer = mcpServer ?? throw new ArgumentNullException(nameof(mcpServer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cancellationTokenSource = new CancellationTokenSource();
        _jsonRpc = new JsonRpc();

        RegisterHandlers();
    }

    public async Task RunAsync()
    {
        var readTask = Task.Run(ReadLoopAsync);
        var writeTask = Task.Run(WriteLoopAsync);

        await Task.WhenAny(readTask, writeTask);
        _cancellationTokenSource.Cancel();

        await Task.WhenAll(readTask, writeTask);
    }

    private void RegisterHandlers()
    {
        // Handle discovery
        _jsonRpc.RegisterMethod("discover", async (JsonNode? @params) =>
        {
            var tools = _mcpServer.GetTools();
            return new JsonObject
            {
                ["tools"] = JsonSerializer.SerializeToNode(tools)
            };
        });

        // Handle tool execution
        _jsonRpc.RegisterMethod("execute", async (JsonNode? @params) =>
        {
            if (@params?["name"]?.GetValue<string>() is not string toolName)
            {
                throw new JsonRpcException(-32602, "Missing tool name");
            }

            var arguments = @params["arguments"] ?? new JsonObject();
            var argumentsElement = JsonSerializer.Deserialize<JsonElement>(arguments.ToJsonString());

            _logger.LogInformation("Executing tool {ToolName}", toolName);

            var result = await _mcpServer.ExecuteToolAsync(
                toolName,
                argumentsElement,
                _cancellationTokenSource.Token);

            return new JsonObject
            {
                ["toolResult"] = JsonSerializer.SerializeToNode(result)
            };
        });

        // Handle shutdown
        _jsonRpc.RegisterNotification("shutdown", async (JsonNode? @params) =>
        {
            _logger.LogInformation("Received shutdown notification");
            _cancellationTokenSource.Cancel();
        });
    }

    private async Task ReadLoopAsync()
    {
        try
        {
            using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
            var buffer = new StringBuilder();

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break; // EOF

                // MCP uses line-delimited JSON
                if (!string.IsNullOrWhiteSpace(line))
                {
                    try
                    {
                        var request = JsonNode.Parse(line);
                        if (request != null)
                        {
                            _ = Task.Run(async () => await ProcessRequestAsync(request));
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to parse JSON-RPC request");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in read loop");
            _cancellationTokenSource.Cancel();
        }
    }

    private async Task WriteLoopAsync()
    {
        // For stdio mode, responses are written directly by ProcessRequestAsync
        await Task.Delay(Timeout.Infinite, _cancellationTokenSource.Token);
    }

    private async Task ProcessRequestAsync(JsonNode request)
    {
        try
        {
            var response = await _jsonRpc.ProcessAsync(request);
            if (response != null)
            {
                var json = response.ToJsonString();
                Console.WriteLine(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request");

            var errorResponse = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["error"] = new JsonObject
                {
                    ["code"] = -32603,
                    ["message"] = "Internal error"
                },
                ["id"] = request["id"]?.AsValue()
            };

            Console.WriteLine(errorResponse.ToJsonString());
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}

/// <summary>
/// Simple JSON-RPC 2.0 handler.
/// </summary>
internal class JsonRpc
{
    private readonly Dictionary<string, Func<JsonNode?, Task<JsonNode?>>> _methods = new();
    private readonly Dictionary<string, Func<JsonNode?, Task>> _notifications = new();

    public void RegisterMethod(string name, Func<JsonNode?, Task<JsonNode?>> handler)
    {
        _methods[name] = handler;
    }

    public void RegisterNotification(string name, Func<JsonNode?, Task> handler)
    {
        _notifications[name] = handler;
    }

    public async Task<JsonNode?> ProcessAsync(JsonNode request)
    {
        var method = request["method"]?.GetValue<string>();
        if (string.IsNullOrEmpty(method))
        {
            return CreateErrorResponse(request["id"], -32600, "Invalid Request");
        }

        var @params = request["params"];
        var id = request["id"];

        // Notification (no id)
        if (id == null)
        {
            if (_notifications.TryGetValue(method, out var notificationHandler))
            {
                await notificationHandler(@params);
            }
            return null;
        }

        // Method call
        if (_methods.TryGetValue(method, out var methodHandler))
        {
            try
            {
                var result = await methodHandler(@params);
                return new JsonObject
                {
                    ["jsonrpc"] = "2.0",
                    ["result"] = result,
                    ["id"] = id
                };
            }
            catch (JsonRpcException ex)
            {
                return CreateErrorResponse(id, ex.Code, ex.Message);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(id, -32603, $"Internal error: {ex.Message}");
            }
        }

        return CreateErrorResponse(id, -32601, "Method not found");
    }

    private static JsonNode CreateErrorResponse(JsonNode? id, int code, string message)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message
            },
            ["id"] = id
        };
    }
}

/// <summary>
/// JSON-RPC exception.
/// </summary>
internal class JsonRpcException : Exception
{
    public int Code { get; }

    public JsonRpcException(int code, string message) : base(message)
    {
        Code = code;
    }
}