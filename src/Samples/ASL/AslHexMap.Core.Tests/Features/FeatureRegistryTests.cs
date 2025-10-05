using System.Text.Json;
using AslHexMap.Core.Features;
using Xunit;

namespace AslHexMap.Core.Tests.Features
{
    public class FeatureRegistryTests
    {
        [Fact]
        public void Constructor_WithDefaults_RegistersDefaultFeatures()
        {
            // Arrange & Act
            var registry = new FeatureRegistry();

            // Assert - Try to create known default features
            var buildingJson = JsonSerializer.Deserialize<JsonElement>("""
                {
                    "type": "building-footprint",
                    "material": "wood",
                    "kind": "single-hex"
                }
                """);

            var stairwellJson = JsonSerializer.Deserialize<JsonElement>("""
                {
                    "type": "stairwell",
                    "location": "up"
                }
                """);

            Assert.True(registry.TryCreate(buildingJson, out var buildingFeature));
            Assert.NotNull(buildingFeature);
            Assert.IsType<BuildingFootprint>(buildingFeature);

            Assert.True(registry.TryCreate(stairwellJson, out var stairwellFeature));
            Assert.NotNull(stairwellFeature);
            Assert.IsType<Stairwell>(stairwellFeature);
        }

        [Fact]
        public void Constructor_WithoutDefaults_DoesNotRegisterFeatures()
        {
            // Arrange & Act
            var registry = new FeatureRegistry(registerDefaults: false);

            // Assert
            var buildingJson = JsonSerializer.Deserialize<JsonElement>("""
                {
                    "type": "building-footprint",
                    "material": "wood",
                    "kind": "single-hex"
                }
                """);

            Assert.False(registry.TryCreate(buildingJson, out var feature));
            Assert.Null(feature);
        }

        [Fact]
        public void Register_CustomFeature_AllowsCreation()
        {
            // Arrange
            var registry = new FeatureRegistry(registerDefaults: false);
            
            // Register a mock feature factory
            registry.Register<TestFeature>("test-feature", json => new TestFeature(json.GetProperty("name").GetString()!));

            var testJson = JsonSerializer.Deserialize<JsonElement>("""
                {
                    "type": "test-feature",
                    "name": "TestName"
                }
                """);

            // Act
            var success = registry.TryCreate(testJson, out var feature);

            // Assert
            Assert.True(success);
            Assert.NotNull(feature);
            Assert.IsType<TestFeature>(feature);
            Assert.Equal("TestName", ((TestFeature)feature).Name);
        }

        [Fact]
        public void TryCreate_MissingTypeProperty_ReturnsFalse()
        {
            // Arrange
            var registry = new FeatureRegistry();
            var invalidJson = JsonSerializer.Deserialize<JsonElement>("""
                {
                    "material": "wood"
                }
                """);

            // Act
            var success = registry.TryCreate(invalidJson, out var feature);

            // Assert
            Assert.False(success);
            Assert.Null(feature);
        }

        [Fact]
        public void TryCreate_UnknownType_ReturnsFalse()
        {
            // Arrange
            var registry = new FeatureRegistry();
            var unknownJson = JsonSerializer.Deserialize<JsonElement>("""
                {
                    "type": "unknown-feature"
                }
                """);

            // Act
            var success = registry.TryCreate(unknownJson, out var feature);

            // Assert
            Assert.False(success);
            Assert.Null(feature);
        }

        [Fact]
        public void Register_NullType_ThrowsArgumentException()
        {
            // Arrange
            var registry = new FeatureRegistry();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                registry.Register<TestFeature>(null!, json => new TestFeature("test")));
        }

        [Fact]
        public void Register_NullFactory_ThrowsArgumentNullException()
        {
            // Arrange
            var registry = new FeatureRegistry();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                registry.Register<TestFeature>("test", null!));
        }

        // Test feature class for testing purposes
        private class TestFeature : IOverlayFeature
        {
            public string Name { get; }
            public string Token => "test-token";

            public TestFeature(string name)
            {
                Name = name;
            }

            public void Render(System.Text.StringBuilder sb, double cx, double cy, double size, FeatureContext ctx)
            {
                // Mock implementation
            }
        }
    }
}