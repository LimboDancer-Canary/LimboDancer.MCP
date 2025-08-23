// File: LimboDancer.MCP.Tests/E2E/McpServerE2ETests.cs
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LimboDancer.MCP.Tests.Unit;
using Xunit;

namespace LimboDancer.MCP.Tests.E2E;

public class McpServerE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public McpServerE2ETests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Override services for testing
                services.AddAuthentication("Test")
                    .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });
            });
        });

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
    }

    [Fact]
    public async Task Initialize_ReturnsProtocolInfo()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/mcp/initialize", new { });

        // Assert
        response.Should().BeSuccessful();
        var content = await response.Content.ReadFromJsonAsync<dynamic>();
        content.Should().NotBeNull();
        content.protocolVersion.Should().Be("2024-11-01");
    }

    [Fact]
    public async Task ListTools_ReturnsAvailableTools()
    {
        // Act
        var response = await _client.GetAsync("/api/mcp/tools");

        // Assert
        response.Should().BeSuccessful();
        var content = await response.Content.ReadFromJsonAsync<dynamic>();
        content.Should().NotBeNull();
        content.tools.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteTool_ValidInput_ReturnsResult()
    {
        // Arrange
        var toolInput = new
        {
            sessionId = Guid.NewGuid().ToString(),
            limit = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/mcp/tools/history_get", toolInput);

        // Assert
        response.Should().BeSuccessful();
        var content = await response.Content.ReadFromJsonAsync<dynamic>();
        content.Should().NotBeNull();
        content.toolResult.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteTool_InvalidTool_Returns404()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/mcp/tools/invalid_tool", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Manifest_ReturnsCompleteManifest()
    {
        // Act
        var response = await _client.GetAsync("/api/mcp/manifest");

        // Assert
        response.Should().BeSuccessful();
        var content = await response.Content.ReadFromJsonAsync<dynamic>();
        content.Should().NotBeNull();
        content.name.Should().Be("LimboDancer.MCP");
        content.tools.Should().NotBeNull();
        content.server.Should().NotBeNull();
        content.ontology.Should().NotBeNull();
    }
}
