using System.Text.Json;
using System.Text.Json.Serialization;

namespace AslHexMap.Core.Schema
{
    /// <summary>
    /// Flat-top hex side order. 0 = North.
    /// SIDE_ORDER = ["N","NE","SE","S","SW","NW"]
    /// </summary>
    [JsonConverter(typeof(SideJsonConverter))]
    public enum Side
    {
        N = 0,
        NE = 1,
        SE = 2,
        S = 3,
        SW = 4,
        NW = 5
    }

    public static class SideHelpers
    {
        public static readonly string[] Names = { "N", "NE", "SE", "S", "SW", "NW" };

        public static bool TryParse(string text, out Side side)
        {
            side = default;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var t = text.Trim().ToUpperInvariant();
            for (int i = 0; i < Names.Length; i++)
            {
                if (Names[i] == t) { side = (Side)i; return true; }
            }
            return false;
        }

        public static string ToName(this Side side) => Names[(int)side];
    }

    /// <summary>
    /// Accepts either string names ("N","NE","SE","S","SW","NW") or integers (0..5).
    /// Writes canonical string names.
    /// </summary>
    public sealed class SideJsonConverter : JsonConverter<Side>
    {
        public override Side Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (SideHelpers.TryParse(s!, out var side)) return side;
                throw new JsonException($"Invalid side name: {s}");
            }
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out var i) && i >= 0 && i <= 5) return (Side)i;
                throw new JsonException("Side integer must be in [0..5].");
            }
            throw new JsonException("Side must be string or integer.");
        }

        public override void Write(Utf8JsonWriter writer, Side value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToName());
    }
}
