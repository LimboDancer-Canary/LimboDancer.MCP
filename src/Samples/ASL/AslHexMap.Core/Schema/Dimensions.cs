using System.Text.Json.Serialization;

namespace AslHexMap.Core.Schema
{
    public class Dimensions
    {
        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }
}