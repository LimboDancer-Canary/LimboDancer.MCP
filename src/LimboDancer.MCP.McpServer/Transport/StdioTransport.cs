using Microsoft.Extensions.Logging;
using ModelContextProtocol.Core.Protocol;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;

namespace LimboDancer.MCP.McpServer.Transport;

/// <summary>
/// Handles MCP communication over stdio (standard input/output).
/// </summary>
public class StdioTransport : IDisposable
{
    private readonly McpServer _mcpServer;
    private readonly ILogger<StdioTransport> _logger;
    private readonly JsonRpcProcessor _jsonRpc;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _readTask;
    private readonly Task _writeTask;
    private readonly Channel<string> _outputChannel;

    public StdioTransport(McpServer mcpServer, ILogger<StdioTransport> logger)
    {
        _mcpServer = mcpServer ?? throw new ArgumentNullException(nameof(mcpServer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonRpc = new JsonRpcProcessor();
        _outputChannel = Channel.CreateUnbounded<string>();

        RegisterHandlers();

        // Start read and write loops
        _readTask = Task.Run(ReadLoopAsync);
        _writeTask = Task.Run(WriteLoopAsync);
    }

    private void RegisterHandlers()
    {
        // Handle initialize request
        _jsonRpc.RegisterMethod("initialize", async (JsonNode? @params) =>
        {
            _logger.LogInformation("Received initialize request");

            return new JsonObject
            {
                ["protocolVersion"] = "2024-11-01",
                ["capabilities"] = new JsonObject
                {
                    ["tools"] = new JsonObject { }
                },
                ["serverInfo"] = new JsonObject
                {
                    ["name"] = "LimboDancer.MCP",
                    ["version"] = "1.0.0"
                }
            };
        });

        // Handle list tools request
        _jsonRpc.RegisterMethod("tools/list", async (JsonNode? @params) =>
        {
            _logger.LogInformation("Received tools/list request");

            var tools = _mcpServer.GetTools();
            var toolsArray = new JsonArray();

            foreach (var tool in tools)
            {
                toolsArray.Add(new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["inputSchema"] = JsonSerializer.SerializeToNode(tool.InputSchema)
                });
            }

            return new JsonObject { ["tools"] = toolsArray };
        });

        // Handle tool execution
        _jsonRpc.RegisterMethod("tools/call", async (JsonNode? @params) =>
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
        }
    }

    private async Task WriteLoopAsync()
    {
        try
        {
            using var writer = new StreamWriter(Console.OpenStandardOutput(), Encoding.UTF8)
            {
                AutoFlush = true
            };

            await foreach (var message in _outputChannel.Reader.ReadAllAsync(_cancellationTokenSource.Token))
            {
                await writer.WriteLineAsync(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in write loop");
        }
    }

    private async Task ProcessRequestAsync(JsonNode request)
    {
        try
        {
            var response = await _jsonRpc.ProcessAsync(request);
            if (response != null)
            {
                var responseJson = response.ToJsonString(new JsonWriterOptions { Indented = false });
                await _outputChannel.Writer.WriteAsync(responseJson);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request");

            // Send error response
            var errorResponse = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = request["id"]?.GetValue<object>(),
                ["error"] = new JsonObject
                {
                    ["code"] = -32603,
                    ["message"] = "Internal error",
                    ["data"] = ex.Message
                }
            };

            await _outputChannel.Writer.WriteAsync(
                errorResponse.ToJsonString(new JsonWriterOptions { Indented = false }));
        }
    }

    public async Task RunAsync()
    {
        await Task.WhenAny(_readTask, _writeTask);
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _outputChannel.Writer.TryComplete();

        try
        {
            _readTask.Wait(TimeSpan.FromSeconds(5));
            _writeTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // Ignore timeout exceptions
        }

        _cancellationTokenSource.Dispose();
    }
}

/// <summary>
/// Simple JSON-RPC processor for handling method calls.
/// </summary>
internal class JsonRpcProcessor
{
    private readonly Dictionary<string, Func<JsonNode?, Task<JsonNode?>>> _methods = new();
    private readonly Dictionary<string, Func<JsonNode?, Task>> _notifications = new();

    public void RegisterMethod(string method, Func<JsonNode?, Task<JsonNode?>> handler)
    {
        _methods[method] = handler;
    }

    public void RegisterNotification(string method, Func<JsonNode?, Task> handler)
    {
        _notifications[method] = handler;
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
                    ["id"] = id.DeepClone(),
                    ["result"] = result
                };
            }
            catch (JsonRpcException ex)
            {
                return CreateErrorResponse(id, ex.Code, ex.Message, ex.Data);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(id, -32603, "Internal error", ex.Message);
            }
        }

        return CreateErrorResponse(id, -32601, "Method not found");
    }

    private JsonNode CreateErrorResponse(JsonNode? id, int code, string message, object? data = null)
    {
        var error = new JsonObject
        {
            ["code"] = code,
            ["message"] = message
        };

        if (data != null)
        {
            error["data"] = JsonSerializer.SerializeToNode(data);
        }

        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["error"] = error
        };
    }
}

internal class JsonRpcException : Exception
{
    public int Code { get; }
    public object? Data { get; }

    public JsonRpcException(int code, string message, object? data = null) : base(message)
    {
        Code = code;
        Data = data;
    }
}