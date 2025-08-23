namespace LimboDancer.MCP.Ontology.Runtime
{
    /// <summary>
    /// Exception thrown when ontology validation fails.
    /// </summary>
    public sealed class OntologyValidationException : Exception
    {
        public TenantScope Scope { get; }

        public OntologyValidationException(TenantScope scope, string message)
            : base(message)
        {
            Scope = scope;
        }

        public OntologyValidationException(TenantScope scope, string message, Exception innerException)
            : base(message, innerException)
        {
            Scope = scope;
        }

        public OntologyValidationException(string message)
            : base(message)
        {
            Scope = default;
        }

        public OntologyValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
            Scope = default;
        }
    }
}