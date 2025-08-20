// File: /src/LimboDancer.MCP.McpServer/Tools/ToolSchemas.cs
// Purpose: Single source of truth for JSON-LD @context used across tool schemas.

namespace LimboDancer.MCP.McpServer.Tools
{
    public static class ToolSchemas
    {
        /// <summary>
        /// Common JSON-LD @context for tools. Use by embedding into schema JSON.
        /// </summary>
        public const string JsonLdContext = /* language=json */ """
        {
          "kg": "https://example.org/kg#",
          "ldm": "https://example.org/ldm#",
          "schema": "http://schema.org/",
          "xsd": "http://www.w3.org/2001/XMLSchema#",
          "sessionId": "@id",
          "subjectVertexId": "@id",
          "subjectIds": { "@container": "@set", "@id": "@id" }
        }
        """;
    }
}
