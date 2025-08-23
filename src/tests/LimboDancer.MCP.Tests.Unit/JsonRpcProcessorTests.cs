// File: LimboDancer.MCP.Tests/JsonRpcProcessorTests.cs
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.MCP.McpServer.Transport;
using Xunit;

namespace LimboDancer.MCP.Tests.Unit;

public class JsonRpcProcessorTests
{
    private readonly JsonRpcProcessor _processor;

    public JsonRpcProcessorTests()
    {
        _processor = new JsonRpcProcessor();
    }

    [Fact]
    public async Task ProcessAsync_ValidMethodCall_ReturnsResult()
    {
        // Arrange
        var expectedResult = new JsonObject { ["data"] = "test" };
        _processor.RegisterMethod("test.method", async (param) => expectedResult);

        var request = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = "test.method",
            ["params"] = new JsonObject()
        };

        // Act
        var response = await _processor.ProcessAsync(request);

        // Assert
        response.Should().NotBeNull();
        response!["result"].Should().BeEquivalentTo(expectedResult);
        response["id"]!.GetValue<int>().Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_InvalidMethod_ReturnsError()
    {
        // Arrange
        var request = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = "unknown.method"
        };

        // Act
        var response = await _processor.ProcessAsync(request);

        // Assert
        response.Should().NotBeNull();
        response!["error"].Should().NotBeNull();
        response["error"]!["code"]!.GetValue<int>().Should().Be(-32601); // Method not found
    }

    [Fact]
    public async Task ProcessAsync_Notification_ReturnsNull()
    {
        // Arrange
        var notificationReceived = false;
        _processor.RegisterNotification("test.notification", async (param) =>
        {
            notificationReceived = true;
        });

        var request = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = "test.notification",
            ["params"] = new JsonObject()
        };

        // Act
        var response = await _processor.ProcessAsync(request);

        // Assert
        response.Should().BeNull();
        notificationReceived.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAsync_MethodThrowsException_ReturnsInternalError()
    {
        // Arrange
        _processor.RegisterMethod("failing.method", async (param) =>
        {
            throw new InvalidOperationException("Test error");
        });

        var request = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = "failing.method"
        };

        // Act
        var response = await _processor.ProcessAsync(request);

        // Assert
        response.Should().NotBeNull();
        response!["error"].Should().NotBeNull();
        response["error"]!["code"]!.GetValue<int>().Should().Be(-32603); // Internal error
        response["error"]!["data"]!.GetValue<string>().Should().Contain("Test error");
    }
}