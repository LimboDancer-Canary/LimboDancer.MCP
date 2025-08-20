namespace LimboDancer.MCP.Ontology.Constants
{
    /// <summary>
    /// Minimal platform constants. Prefer data-first definitions; keep this list thin.
    /// </summary>
    public static class Ldm
    {
        // Base namespace template. Replace tokens per scope/channel as needed.
        public const string BaseNamespaceTemplate = "https://ontology.limbodancer.mcp/{tenant}/{package}/{channel}#";

        public static string BaseNamespace(string tenant, string @package, string channel)
            => BaseNamespaceTemplate
                .Replace("{tenant}", tenant)
                .Replace("{package}", @package)
                .Replace("{channel}", channel);
    }
}