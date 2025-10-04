using System.Text.Json.Serialization;

namespace AslHexMap.Core.Schema
{
    /// <summary>
    /// Linear feature through a hex, e.g., a road. Sides accept "N|NE|SE|S|SW|NW" or 0..5.
    /// </summary>
    public class LinearTraversal
    {
        [JsonPropertyName("hexId")] public string HexId { get; set; } = string.Empty;
        [JsonPropertyName("type")] public string Type { get; set; } = "road";
        [JsonPropertyName("subtype")] public string? Subtype { get; set; }

        [JsonPropertyName("enters")]
        [JsonConverter(typeof(SideJsonConverter))]
        public Side? Enters { get; set; }

        [JsonPropertyName("exits")]
        [JsonConverter(typeof(SideJsonConverter))]
        public Side? Exits { get; set; }

        [JsonPropertyName("elevation")] public int? Elevation { get; set; }
    }
}