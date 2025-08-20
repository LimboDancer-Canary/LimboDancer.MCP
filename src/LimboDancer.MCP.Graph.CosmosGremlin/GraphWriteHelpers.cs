using System.Text;

namespace LimboDancer.MCP.Graph.CosmosGremlin;

internal static class GraphWriteHelpers
{
    /// <summary>Safely builds a Gremlin snippet for setting properties from a dictionary (stringifies values).</summary>
    public static string BuildPropAssignments(IDictionary<string, object?> props)
    {
        var sb = new StringBuilder();
        foreach (var kv in props)
        {
            var key = kv.Key.Replace("'", ""); // defensive
            var val = kv.Value switch
            {
                null => "",
                bool b => b ? "true" : "false",
                DateTime dt => dt.ToUniversalTime().ToString("O"),
                DateTimeOffset dto => dto.ToUniversalTime().ToString("O"),
                Guid g => g.ToString("N"),
                _ => kv.Value!.ToString()!
            };
            // Cosmos Gremlin property values are strings most of the time – send as strings to be safe
            sb.Append($".property('{key}','{Escape(val)}')");
        }
        return sb.ToString();

        static string Escape(string s) => s.Replace("\\", "\\\\").Replace("'", "\\'");
    }
}