using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AslHexMap.Core.Schema
{
    public class MapSpec
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }

        [JsonPropertyName("boardNumber")]
        [JsonConverter(typeof(FlexibleStringJsonConverter))]
        public string? BoardNumber { get; set; }

        [JsonPropertyName("dimensions")]
        public Dimensions Dimensions { get; set; } = new();

        [JsonPropertyName("defaultTemplateId")]
        public string? DefaultTemplateId { get; set; }

        // Per-hex entries live here in the sample.
        [JsonPropertyName("individualHexes")]
        public List<IndividualHex>? IndividualHexes { get; set; }

        // Some versions tuck traversals under "map"
        [JsonPropertyName("traversals")] public List<LinearTraversal>? Traversals { get; set; }
        [JsonPropertyName("roads")] public List<LinearTraversal>? Roads { get; set; }
    }


}