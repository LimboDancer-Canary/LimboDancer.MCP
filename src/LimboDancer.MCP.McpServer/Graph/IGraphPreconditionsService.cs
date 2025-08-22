using LimboDancer.MCP.McpServer.Tools;

namespace LimboDancer.MCP.McpServer.Graph
{
    public interface IGraphPreconditionsService
    {
        Task<PreconditionsResult> CheckAsync(CheckGraphPreconditionsRequest request, CancellationToken ct = default);
    }
}