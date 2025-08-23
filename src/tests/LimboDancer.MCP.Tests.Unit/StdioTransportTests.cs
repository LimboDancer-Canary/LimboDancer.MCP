// File: LimboDancer.MCP.Tests/StdioTransportTests.cs
using FluentAssertions;
using LimboDancer.MCP.McpServer;
using LimboDancer.MCP.McpServer.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;
using System.Text;
using Xunit;

namespace LimboDancer.MCP.Tests.Unit;

public class StdioTransportTests : IDisposable
{
    private readonly MemoryStream _inputStream;
    private readonly MemoryStream _outputStream;
    private readonly StreamWriter _inputWriter;
    private readonly StreamReader _outputReader;
    private readonly TextWriter _originalOut;
    private readonly TextReader _originalIn;

    public StdioTransportTests()
    {
        _inputStream = new MemoryStream();
        _outputStream = new MemoryStream();
        _inputWriter = new StreamWriter(_inputStream);
        _outputReader = new StreamReader(_outputStream);

        _originalOut = Console.Out;
        _originalIn = Console.In;

        Console.SetOut(new StreamWriter(_outputStream) { AutoFlush = true });
        Console.SetIn(new StreamReader(_inputStream));
    }

    [Fact]
    public async Task StdioTransport_ProcessesDiscoverRequest()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<McpServer.McpServer>(provider =>
                new McpServer.McpServer(provider, provider.GetRequiredService<ILogger<McpServer.McpServer>>()))
            .BuildServiceProvider();

        var mcpServer = serviceProvider.GetRequiredService<McpServer.McpServer>();
        var transport = new StdioTransport(mcpServer, NullLogger<StdioTransport>.Instance);

        // Write discover request
        await _inputWriter.WriteLineAsync(@"{""jsonrpc"":""2.0"",""id"":1,""method"":""discover"",""params"":{}}");
        await _inputWriter.FlushAsync();
        _inputStream.Position = 0;

        // Act
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(500)); // Timeout after 500ms

        try
        {
            await transport.RunAsync();
        }
        catch (OperationCanceledException)
        {
            // Expected - we'll cancel the operation
        }

        // Read response
        _outputStream.Position = 0;
        var response = await _outputReader.ReadLineAsync();

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("tools");
        response.Should().Contain("jsonrpc");
    }

    [Fact]
    public async Task StdioTransport_ProcessesExecuteRequest()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<McpServer.McpServer>(provider =>
                new McpServer.McpServer(provider, provider.GetRequiredService<ILogger<McpServer.McpServer>>()))
            .BuildServiceProvider();

        var mcpServer = serviceProvider.GetRequiredService<McpServer.McpServer>();
        var transport = new StdioTransport(mcpServer, NullLogger<StdioTransport>.Instance);

        // Write execute request (this will likely fail since we don't have tools registered, but should get a proper JSON-RPC response)
        await _inputWriter.WriteLineAsync(@"{""jsonrpc"":""2.0"",""id"":2,""method"":""execute"",""params"":{""name"":""test_tool"",""arguments"":{}}}");
        await _inputWriter.FlushAsync();
        _inputStream.Position = 0;

        // Act
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(500)); // Timeout after 500ms

        try
        {
            await transport.RunAsync();
        }
        catch (OperationCanceledException)
        {
            // Expected - we'll cancel the operation
        }

        // Read response
        _outputStream.Position = 0;
        var response = await _outputReader.ReadLineAsync();

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("jsonrpc");
        // Should contain either result or error
        response.Should().Match(r => r.Contains("result") || r.Contains("error"));
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetIn(_originalIn);
        _inputWriter?.Dispose();
        _outputReader?.Dispose();
        _inputStream?.Dispose();
        _outputStream?.Dispose();
    }
}