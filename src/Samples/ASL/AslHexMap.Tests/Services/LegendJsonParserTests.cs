using AslHexMap.Services;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Xunit;
using static AslHexMap.Services.LegendService;

namespace AslHexMap.Tests.Services
{
    /// <summary>
    /// Unit tests demonstrating improved testability of the LegendJsonParser.
    /// </summary>
    public class LegendJsonParserTests
    {
        private readonly LegendJsonParser _parser;

        public LegendJsonParserTests()
        {
            _parser = new LegendJsonParser();
        }

        [Fact]
        public void ParseFromJson_WithNullDocument_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _parser.ParseFromJson(null!));
        }

        [Fact]
        public void ParseFromJson_WithValidJson_ReturnsCorrectModel()
        {
            // Arrange
            var json = """
            {
                "version": "1.0",
                "sections": [
                    {
                        "title": "Base Terrain",
                        "items": [
                            { "token": "base-open", "label": "Open Ground" },
                            { "token": "base-woods", "label": "Woods" }
                        ]
                    }
                ]
            }
            """;

            using var document = JsonDocument.Parse(json);

            // Act
            var result = _parser.ParseFromJson(document);

            // Assert
            Assert.Equal("1.0", result.Version);
            Assert.Single(result.Sections);
            
            var section = result.Sections[0];
            Assert.Equal("Base Terrain", section.Title);
            Assert.Equal(2, section.Items.Count);
            
            Assert.Equal("base-open", section.Items[0].Token);
            Assert.Equal("Open Ground", section.Items[0].Label);
            Assert.Equal("base-woods", section.Items[1].Token);
            Assert.Equal("Woods", section.Items[1].Label);
        }

        [Fact]
        public void ParseFromJson_WithMissingVersion_UsesDefaultVersion()
        {
            // Arrange
            var json = """
            {
                "sections": []
            }
            """;

            using var document = JsonDocument.Parse(json);

            // Act
            var result = _parser.ParseFromJson(document);

            // Assert
            Assert.Equal("1", result.Version);
        }

        [Fact]
        public void ParseFromJson_WithMissingSections_ThrowsJsonException()
        {
            // Arrange
            var json = """
            {
                "version": "1.0"
            }
            """;

            using var document = JsonDocument.Parse(json);

            // Act & Assert
            var exception = Assert.Throws<JsonException>(() => _parser.ParseFromJson(document));
            Assert.Contains("sections", exception.Message);
        }

        [Fact]
        public void ParseFromJson_WithEmptySections_ReturnsEmptyModel()
        {
            // Arrange
            var json = """
            {
                "version": "1.0",
                "sections": []
            }
            """;

            using var document = JsonDocument.Parse(json);

            // Act
            var result = _parser.ParseFromJson(document);

            // Assert
            Assert.Equal("1.0", result.Version);
            Assert.Empty(result.Sections);
        }

        [Fact]
        public void ParseFromJson_WithSectionMissingItems_ReturnsEmptyItems()
        {
            // Arrange
            var json = """
            {
                "version": "1.0",
                "sections": [
                    {
                        "title": "Test Section"
                    }
                ]
            }
            """;

            using var document = JsonDocument.Parse(json);

            // Act
            var result = _parser.ParseFromJson(document);

            // Assert
            var section = result.Sections[0];
            Assert.Equal("Test Section", section.Title);
            Assert.Empty(section.Items);
        }

        [Fact]
        public async Task ParseFromStreamAsync_WithValidStream_ReturnsCorrectModel()
        {
            // Arrange
            var json = """
            {
                "version": "2.0",
                "sections": [
                    {
                        "title": "Building Features",
                        "items": [
                            { "token": "building-wood", "label": "Wood Building" }
                        ]
                    }
                ]
            }
            """;

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Act
            var result = await _parser.ParseFromStreamAsync(stream);

            // Assert
            Assert.Equal("2.0", result.Version);
            Assert.Single(result.Sections);
            Assert.Equal("Building Features", result.Sections[0].Title);
            Assert.Single(result.Sections[0].Items);
            Assert.Equal("building-wood", result.Sections[0].Items[0].Token);
            Assert.Equal("Wood Building", result.Sections[0].Items[0].Label);
        }

        [Fact]
        public async Task ParseFromStreamAsync_WithNullStream_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _parser.ParseFromStreamAsync(null!));
        }
    }
}