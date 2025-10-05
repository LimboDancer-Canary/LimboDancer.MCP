using System.Text.Json;
using static AslHexMap.Services.LegendService;

namespace AslHexMap.Services
{
    /// <summary>
    /// Responsible for parsing JSON documents into LegendModel objects.
    /// </summary>
    public class LegendJsonParser
    {
        /// <summary>
        /// Parses a JsonDocument into a LegendModel.
        /// </summary>
        /// <param name="document">The JSON document to parse</param>
        /// <returns>A LegendModel containing the parsed data</returns>
        /// <exception cref="ArgumentNullException">Thrown when document is null</exception>
        /// <exception cref="JsonException">Thrown when the JSON structure is invalid</exception>
        public LegendModel ParseFromJson(JsonDocument document)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var root = document.RootElement;
            
            var version = ExtractVersion(root);
            var sections = ExtractSections(root);

            return new LegendModel(version, sections);
        }

        /// <summary>
        /// Parses a JsonDocument from a stream into a LegendModel.
        /// </summary>
        /// <param name="stream">The stream containing JSON data</param>
        /// <returns>A LegendModel containing the parsed data</returns>
        public async Task<LegendModel> ParseFromStreamAsync(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            using var document = await JsonDocument.ParseAsync(stream);
            return ParseFromJson(document);
        }

        /// <summary>
        /// Extracts the version from the root JSON element.
        /// </summary>
        /// <param name="root">The root JSON element</param>
        /// <returns>Version string, defaults to "1" if not found</returns>
        private static string ExtractVersion(JsonElement root)
        {
            return root.TryGetProperty("version", out var versionElement) && versionElement.ValueKind == JsonValueKind.String
                ? versionElement.GetString() ?? "1"
                : "1";
        }

        /// <summary>
        /// Extracts legend sections from the root JSON element.
        /// </summary>
        /// <param name="root">The root JSON element</param>
        /// <returns>List of legend sections</returns>
        /// <exception cref="JsonException">Thrown when sections property is missing or invalid</exception>
        private static List<LegendSection> ExtractSections(JsonElement root)
        {
            if (!root.TryGetProperty("sections", out var sectionsElement) || sectionsElement.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException("Legend JSON must contain a 'sections' array property");
            }

            var sections = new List<LegendSection>();

            foreach (var sectionElement in sectionsElement.EnumerateArray())
            {
                var section = ParseSection(sectionElement);
                sections.Add(section);
            }

            return sections;
        }

        /// <summary>
        /// Parses a single legend section from JSON.
        /// </summary>
        /// <param name="sectionElement">The JSON element representing a section</param>
        /// <returns>A LegendSection object</returns>
        private static LegendSection ParseSection(JsonElement sectionElement)
        {
            var title = ExtractSectionTitle(sectionElement);
            var items = ExtractSectionItems(sectionElement);

            return new LegendSection(title, items);
        }

        /// <summary>
        /// Extracts the title from a section JSON element.
        /// </summary>
        /// <param name="sectionElement">The section JSON element</param>
        /// <returns>Section title, empty string if not found</returns>
        private static string ExtractSectionTitle(JsonElement sectionElement)
        {
            return sectionElement.TryGetProperty("title", out var titleElement) && titleElement.ValueKind == JsonValueKind.String
                ? titleElement.GetString() ?? ""
                : "";
        }

        /// <summary>
        /// Extracts items from a section JSON element.
        /// </summary>
        /// <param name="sectionElement">The section JSON element</param>
        /// <returns>List of legend items</returns>
        private static List<LegendItem> ExtractSectionItems(JsonElement sectionElement)
        {
            var items = new List<LegendItem>();

            if (!sectionElement.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
            {
                return items; // Return empty list if items property is missing or not an array
            }

            foreach (var itemElement in itemsElement.EnumerateArray())
            {
                var item = ParseItem(itemElement);
                items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// Parses a single legend item from JSON.
        /// </summary>
        /// <param name="itemElement">The JSON element representing an item</param>
        /// <returns>A LegendItem object</returns>
        private static LegendItem ParseItem(JsonElement itemElement)
        {
            var token = itemElement.TryGetProperty("token", out var tokenElement) && tokenElement.ValueKind == JsonValueKind.String
                ? tokenElement.GetString() ?? ""
                : "";

            var label = itemElement.TryGetProperty("label", out var labelElement) && labelElement.ValueKind == JsonValueKind.String
                ? labelElement.GetString() ?? ""
                : "";

            return new LegendItem(token, label);
        }
    }
}