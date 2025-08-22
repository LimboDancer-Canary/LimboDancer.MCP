using Microsoft.Extensions.Hosting;

namespace LimboDancer.MCP.McpServer.Http.Chat;

public class ChatOrchestratorCleanupService : IHostedService
{
    private readonly InMemoryChatOrchestrator _orchestrator;

    public ChatOrchestratorCleanupService(IChatOrchestrator orchestrator)
    {
        _orchestrator = orchestrator as InMemoryChatOrchestrator
                        ?? throw new ArgumentException("Expected InMemoryChatOrchestrator", nameof(orchestrator));
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _orchestrator.Dispose();
        return Task.CompletedTask;
    }
}