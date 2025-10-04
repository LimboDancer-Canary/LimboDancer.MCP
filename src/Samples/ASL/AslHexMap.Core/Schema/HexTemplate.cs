using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AslHexMap.Core.Schema
{
    /// <summary>
    /// A reusable template describing how a hex looks/behaves (base terrain, overlays, etc.).
    /// </summary>
    public class HexTemplate
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Base terrain key (e.g., "Open", "Woods", "Grain", "Building", ...).
        /// Renderer maps this to fill/pattern using TerrainStyle/TerrainDefs.
        /// </summary>
        [JsonPropertyName("baseTerrain")]
        public string BaseTerrain { get; set; } = "Open";

        /// <summary>
        /// Optional overlays/features bundled into the template (e.g., building levels).
        /// </summary>
        [JsonPropertyName("overlays")]
        public Dictionary<string, object>? Overlays { get; set; }

        /// <summary>
        /// Optional linear features defined at the template level (e.g., a road segment through the hex).
        /// </summary>
        [JsonPropertyName("linearFeatures")]
        public List<LinearTraversal>? LinearFeatures { get; set; }

        [JsonPropertyName("building")]
        public BuildingSpec? Building { get; set; }

    }
}