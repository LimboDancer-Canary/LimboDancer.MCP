using System.Text.Json;
using System.Text.Json.Serialization;

namespace AslHexMap.Core.Schema
{
    public class IndividualHex
    {
        [JsonPropertyName("hexId")] public string HexId { get; set; } = string.Empty;
        [JsonPropertyName("templateId")] public string? TemplateId { get; set; }

        /// <summary>
        /// Some files use [] (array) or {} (object). Use JsonElement? to tolerate both.
        /// </summary>
        [JsonPropertyName("overrides")] public JsonElement? Overrides { get; set; }
    }
}