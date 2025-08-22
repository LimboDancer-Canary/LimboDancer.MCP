namespace LimboDancer.MCP.Ontology.Runtime;

/// <summary>
/// Exception thrown when an operation violates tenant scope boundaries.
/// </summary>
public class TenantScopeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantScopeException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TenantScopeException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantScopeException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public TenantScopeException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantScopeException"/> class.
    /// </summary>
    public TenantScopeException() : base("A tenant scope violation occurred.")
    {
    }
}