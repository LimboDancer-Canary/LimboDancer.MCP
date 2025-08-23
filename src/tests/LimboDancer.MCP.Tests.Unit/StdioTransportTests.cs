// File: LimboDancer.MCP.Tests/StdioTransportTests.cs
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using FluentAssertions;

namespace LimboDancer.MCP.Tests.Integration;

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
    public async Task StdioTransport_ProcessesInitializeRequest()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<McpServer>()
            .BuildServiceProvider();

        var mcpServer = serviceProvider.GetRequiredService<McpServer>();
        var transport = new StdioTransport(mcpServer, NullLogger<StdioTransport>.Instance);

        // Write initialize request
        await _inputWriter.WriteLineAsync(@"{""jsonrpc"":""2.0"",""id"":1,""method"":""initialize"",""params"":{}}");
        await _inputWriter.FlushAsync();
        _inputStream.Position = 0;

        // Act
        var readTask = transport.RunAsync();
        await Task.Delay(100); // Give it time to process

        // Read response
        _outputStream.Position = 0;
        var response = await _outputReader.ReadLineAsync();

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("protocolVersion");
        response.Should().Contain("2024-11-01");
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetIn(_originalIn);
        _inputStream.Dispose();
        _outputStream.Dispose();
    }
}
