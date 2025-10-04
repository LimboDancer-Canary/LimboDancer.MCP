using System.Text.Json.Serialization;

namespace AslHexMap.Core.Schema
{
    /// <summary>Building overlay as expressed by the JSON templates.</summary>
    public sealed class BuildingSpec
    {
        // "stone", "wood", "wooden", etc.
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        // Building levels/floors (1, 2, ...)
        [JsonPropertyName("levels")]
        public int? Levels { get; set; }
    }
}