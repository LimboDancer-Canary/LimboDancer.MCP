using Polly;

namespace LimboDancer.MCP.McpServer.Resilience;

/// <summary>
/// Resilience policies for MCP tool execution.
/// </summary>
public interface IResiliencePolicies
{
    IAsyncPolicy<T> GetToolExecutionPolicy<T>(string toolName);
    IAsyncPolicy<HttpResponseMessage> GetHttpPolicy();
}