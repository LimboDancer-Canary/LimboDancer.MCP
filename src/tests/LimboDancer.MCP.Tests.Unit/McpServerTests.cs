// File: LimboDancer.MCP.Tests/McpServerTests.cs
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Text.Json;
using Xunit;

namespace LimboDancer.MCP.Tests.Unit;

public class McpServerTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly McpServer _mcpServer;

    public McpServerTests()
    {
        var services = new ServiceCollection();

        // Mock tools
        var mockHistoryTool = new Mock<HistoryGetTool>(null, null);
        services.AddSingleton(mockHistoryTool.Object);

        services.AddLogging(builder => builder.AddConsole());

        _serviceProvider = services.BuildServiceProvider();
        _mcpServer = new McpServer(_serviceProvider, _serviceProvider.GetRequiredService<ILogger<McpServer>>());
    }

    [Fact]
    public void GetTools_ReturnsAllRegisteredTools()
    {
        // Act
        var tools = _mcpServer.GetTools();

        // Assert
        tools.Should().NotBeEmpty();
        tools.Should().Contain(t => t.Name == "history_get");
        tools.Should().Contain(t => t.Name == "history_append");
        tools.Should().Contain(t => t.Name == "graph_query");
        tools.Should().Contain(t => t.Name == "memory_search");
    }

    [Fact]
    public async Task ExecuteToolAsync_UnknownTool_ReturnsError()
    {
        // Act
        var result = await _mcpServer.ExecuteToolAsync("unknown_tool", default(JsonElement));

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content.Should().HaveCount(1);
        result.Content[0].AsTextContent()?.Text.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteToolAsync_ValidTool_ExecutesSuccessfully()
    {
        // Arrange
        var mockTool = new Mock<HistoryGetTool>(null, null);
        mockTool.Setup(t => t.ExecuteAsync(It.IsAny<JsonElement>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HistoryGetOutput { SessionId = "test", Messages = new() });

        var services = new ServiceCollection();
        services.AddSingleton(mockTool.Object);
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var server = new McpServer(serviceProvider, serviceProvider.GetRequiredService<ILogger<McpServer>>());

        var input = JsonSerializer.SerializeToElement(new { sessionId = "test", limit = 10 });

        // Act
        var result = await server.ExecuteToolAsync("history_get", input);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        mockTool.Verify(t => t.ExecuteAsync(It.IsAny<JsonElement>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
