namespace LimboDancer.MCP.McpServer.Resilience;

/// <summary>
/// Exception that represents a transient failure that can be retried.
/// </summary>
public class TransientException : Exception
{
    public TransientException(string message) : base(message) { }
    public TransientException(string message, Exception innerException) : base(message, innerException) { }
}