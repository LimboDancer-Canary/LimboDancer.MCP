using AslHexMap.Services;
using Microsoft.AspNetCore.Hosting;
using Moq;
using System;
using System.IO;
using Xunit;

namespace AslHexMap.Tests.Services
{
    /// <summary>
    /// Unit tests demonstrating improved testability of the refactored FilePathResolver.
    /// </summary>
    public class FilePathResolverTests
    {
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly FilePathResolver _resolver;

        public FilePathResolverTests()
        {
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockEnvironment.Setup(env => env.ContentRootPath).Returns("/app");
            _resolver = new FilePathResolver(_mockEnvironment.Object);
        }

        [Fact]
        public void ResolveLegendFile_WithNullFileName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _resolver.ResolveLegendFile(null!));
        }

        [Fact]
        public void ResolveLegendFile_WithEmptyFileName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _resolver.ResolveLegendFile(""));
        }

        [Fact]
        public void GetSearchPaths_ReturnsExpectedPaths()
        {
            // Arrange
            var fileName = "test.json";

            // Act
            var paths = _resolver.GetSearchPaths(fileName);

            // Assert
            Assert.Contains("/app/Data/test.json", paths);
            Assert.Contains("/app/test.json", paths);
            Assert.Equal(3, paths.Length); // Should include AppContext.BaseDirectory path too
        }

        [Fact]
        public void GetSearchPaths_WithAdditionalPaths_IncludesAllPaths()
        {
            // Arrange
            var fileName = "test.json";
            var additionalPaths = new[] { "custom", "/absolute/path" };

            // Act
            var paths = _resolver.GetSearchPaths(fileName, additionalPaths);

            // Assert
            Assert.Contains("/app/Data/test.json", paths);
            Assert.Contains("/app/test.json", paths);
            Assert.Contains("/app/custom/test.json", paths);
            Assert.Contains("/absolute/path/test.json", paths);
            Assert.Equal(5, paths.Length);
        }

        [Theory]
        [InlineData("test.json")]
        [InlineData("legend.features.v1.json")]
        [InlineData("custom-legend.json")]
        public void GetSearchPaths_WithDifferentFileNames_BuildsCorrectPaths(string fileName)
        {
            // Act
            var paths = _resolver.GetSearchPaths(fileName);

            // Assert
            Assert.All(paths, path => Assert.EndsWith(fileName, path));
            Assert.Contains($"/app/Data/{fileName}", paths);
            Assert.Contains($"/app/{fileName}", paths);
        }
    }
}