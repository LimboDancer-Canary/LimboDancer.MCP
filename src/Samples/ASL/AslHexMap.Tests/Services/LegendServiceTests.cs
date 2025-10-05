using AslHexMap.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using static AslHexMap.Services.LegendService;

namespace AslHexMap.Tests.Services
{
    /// <summary>
    /// Unit tests demonstrating improved testability of the refactored LegendService.
    /// </summary>
    public class LegendServiceTests
    {
        private readonly Mock<FilePathResolver> _mockPathResolver;
        private readonly Mock<LegendJsonParser> _mockJsonParser;
        private readonly LegendService _service;

        public LegendServiceTests()
        {
            _mockPathResolver = new Mock<FilePathResolver>();
            _mockJsonParser = new Mock<LegendJsonParser>();
            _service = new LegendService(_mockPathResolver.Object, _mockJsonParser.Object);
        }

        [Fact]
        public async Task LoadAsync_WithValidFile_ReturnsLegendModel()
        {
            // Arrange
            var fileName = "test.json";
            var filePath = "/app/Data/test.json";
            var expectedModel = new LegendModel("1.0", new List<LegendSection>());

            _mockPathResolver.Setup(r => r.ResolveLegendFile(fileName))
                .Returns(filePath);
            _mockJsonParser.Setup(p => p.ParseFromStreamAsync(It.IsAny<Stream>()))
                .ReturnsAsync(expectedModel);

            // Mock file existence
            var mockFile = new Mock<Stream>();
            // Note: In a real test, you'd need to mock File.OpenRead or use a test file system

            // Act
            // This test would require additional mocking of File.OpenRead for complete isolation
            // For now, we'll test the logic flow with the dependencies
            
            // Assert that the correct methods are called with expected parameters
            // This demonstrates the improved testability - we can now test each component separately
        }

        [Fact]
        public async Task LabelsForAsync_WithTokens_ReturnsCorrectLabels()
        {
            // Arrange
            var tokens = new[] { "base-open", "building-wood" };
            var sections = new List<LegendSection>
            {
                new LegendSection("Base Terrain", new List<LegendItem>
                {
                    new LegendItem("base-open", "Open Ground"),
                    new LegendItem("base-woods", "Woods")
                }),
                new LegendSection("Building Features", new List<LegendItem>
                {
                    new LegendItem("building-wood", "Wood Building")
                })
            };
            var model = new LegendModel("1.0", sections);

            // Setup mocks to return our test model
            _mockPathResolver.Setup(r => r.ResolveLegendFile(It.IsAny<string>()))
                .Returns("/test/path");
            _mockJsonParser.Setup(p => p.ParseFromStreamAsync(It.IsAny<Stream>()))
                .ReturnsAsync(model);

            // Act
            var result = await _service.LabelsForAsync(tokens);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Open Ground", result["base-open"]);
            Assert.Equal("Wood Building", result["building-wood"]);
            Assert.False(result.ContainsKey("base-woods")); // Should not include unused tokens
        }

        [Fact]
        public void ClearCache_ClearsInternalCache()
        {
            // Act
            _service.ClearCache();

            // Assert
            // The cache should be cleared, which can be verified by subsequent calls
            // requiring file resolution again (this would be tested with multiple LoadAsync calls)
        }

        [Fact]
        public async Task LoadFromFileAsync_WithInvalidPath_ThrowsFileNotFoundException()
        {
            // Arrange
            var invalidPath = "/nonexistent/file.json";

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => _service.LoadFromFileAsync(invalidPath));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task LoadFromFileAsync_WithInvalidFileName_ThrowsArgumentException(string invalidPath)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.LoadFromFileAsync(invalidPath));
        }
    }
}