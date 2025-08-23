// File: LimboDancer.MCP.Tests/McpServerTests.cs
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Text.Json;
using LimboDancer.MCP.McpServer;
using LimboDancer.MCP.McpServer.Tools;
using Xunit;

namespace LimboDancer.MCP.Tests.Unit;

public class McpServerTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly McpServer.McpServer _mcpServer;

    public McpServerTests()
    {
        var services = new ServiceCollection();

        // Mock tools
        var mockHistoryTool = new Mock<HistoryGetTool>(null, null);
        services.AddSingleton(mockHistoryTool.Object);

        services.AddLogging(builder => builder.AddConsole());

        _serviceProvider = services.BuildServiceProvider();
        _mcpServer = new McpServer.McpServer(_serviceProvider, _serviceProvider.GetRequiredService<ILogger<McpServer.McpServer>>());
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
    public async Task ExecuteToolAsync_UnknownTool_ThrowsException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _mcpServer.ExecuteToolAsync("unknown_tool", default(JsonElement)));

        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteToolAsync_ValidTool_ExecutesSuccessfully()
    {
        // Arrange
        var expectedOutput = new HistoryGetOutput { SessionId = "test", Messages = new() };
        var mockTool = new Mock<HistoryGetTool>(null, null);
        mockTool.Setup(t => t.ExecuteAsync(It.IsAny<JsonElement>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOutput);

        var services = new ServiceCollection();
        services.AddSingleton(mockTool.Object);
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var server = new McpServer.McpServer(serviceProvider, serviceProvider.GetRequiredService<ILogger<McpServer.McpServer>>());

        var input = JsonSerializer.SerializeToElement(new { sessionId = "test", limit = 10 });

        // Act
        var result = await server.ExecuteToolAsync("history_get", input);

        // Assert
        result.Should().NotBeNull();
        result.ValueKind.Should().Be(JsonValueKind.Object);

        // Verify the result contains expected properties
        result.TryGetProperty("sessionId", out var sessionIdProp).Should().BeTrue();
        sessionIdProp.GetString().Should().Be("test");

        result.TryGetProperty("messages", out var messagesProp).Should().BeTrue();
        messagesProp.ValueKind.Should().Be(JsonValueKind.Array);

        mockTool.Verify(t => t.ExecuteAsync(It.IsAny<JsonElement>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteToolAsync_ToolThrowsException_PropagatesException()
    {
        // Arrange
        var mockTool = new Mock<HistoryGetTool>(null, null);
        mockTool.Setup(t => t.ExecuteAsync(It.IsAny<JsonElement>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Test error"));

        var services = new ServiceCollection();
        services.AddSingleton(mockTool.Object);
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var server = new McpServer.McpServer(serviceProvider, serviceProvider.GetRequiredService<ILogger<McpServer.McpServer>>());

        var input = JsonSerializer.SerializeToElement(new { sessionId = "test", limit = 10 });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => server.ExecuteToolAsync("history_get", input));

        exception.Message.Should().Be("Test error");
    }
}