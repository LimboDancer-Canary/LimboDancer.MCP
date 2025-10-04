using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AslHexMap.Core.Schema
{
    /// <summary>
    /// Reads either a JSON string or number and returns a C# string.
    /// Writes as a JSON string.
    /// </summary>
    public sealed class FlexibleStringJsonConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.TryGetInt64(out var i)
                    ? i.ToString(CultureInfo.InvariantCulture)
                    : reader.GetDouble().ToString(CultureInfo.InvariantCulture),
                JsonTokenType.Null => null,
                _ => throw new JsonException($"Expected string or number but got {reader.TokenType}.")
            };
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
            => writer.WriteStringValue(value);
    }
}