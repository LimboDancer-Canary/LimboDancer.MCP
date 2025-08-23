using System.Text.Json;
using System.Text.Json.Nodes;

namespace LimboDancer.MCP.McpServer.Transport;

/// <summary>
/// Simple JSON-RPC processor for handling method calls.
/// </summary>
public class JsonRpcProcessor
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