using System.Text.Json;
using AslHexMap.Core.Features;
using Moq;
using Xunit;

namespace AslHexMap.Core.Tests.Features
{
    public class FeatureFactoryTests
    {
        [Fact]
        public void CreateFromJsonArray_ValidArray_CreatesFeatures()
        {
            // Arrange
            var mockRegistry = new Mock<IFeatureRegistry>();
            var mockFeature = new Mock<IOverlayFeature>();
            
            mockRegistry.Setup(r => r.TryCreate(It.IsAny<JsonElement>(), out It.Ref<IOverlayFeature?>.IsAny))
                .Returns((JsonElement element, out IOverlayFeature? feature) =>
                {
                    feature = mockFeature.Object;
                    return true;
                });

            var factory = new FeatureFactory(mockRegistry.Object);
            
            var jsonArray = JsonSerializer.Deserialize<JsonElement>("""
                [
                    { "type": "test-feature-1" },
                    { "type": "test-feature-2" }
                ]
                """);

            // Act
            var features = factory.CreateFromJsonArray(jsonArray);

            // Assert
            Assert.Equal(2, features.Count);
            Assert.All(features, f => Assert.Same(mockFeature.Object, f));
            
            // Verify the registry was called for each element
            mockRegistry.Verify(r => r.TryCreate(It.IsAny<JsonElement>(), out It.Ref<IOverlayFeature?>.IsAny), 
                Times.Exactly(2));
        }

        [Fact]
        public void CreateFromJsonArray_EmptyArray_ReturnsEmptyList()
        {
            // Arrange
            var mockRegistry = new Mock<IFeatureRegistry>();
            var factory = new FeatureFactory(mockRegistry.Object);
            
            var jsonArray = JsonSerializer.Deserialize<JsonElement>("[]");

            // Act
            var features = factory.CreateFromJsonArray(jsonArray);

            // Assert
            Assert.Empty(features);
            
            // Verify registry was not called
            mockRegistry.Verify(r => r.TryCreate(It.IsAny<JsonElement>(), out It.Ref<IOverlayFeature?>.IsAny), 
                Times.Never);
        }

        [Fact]
        public void CreateFromJsonArray_NotArray_ReturnsEmptyList()
        {
            // Arrange
            var mockRegistry = new Mock<IFeatureRegistry>();
            var factory = new FeatureFactory(mockRegistry.Object);
            
            var notArray = JsonSerializer.Deserialize<JsonElement>("""{ "type": "not-array" }""");

            // Act
            var features = factory.CreateFromJsonArray(notArray);

            // Assert
            Assert.Empty(features);
        }

        [Fact]
        public void CreateFromJsonArray_RegistryFailsToCreate_SkipsFeature()
        {
            // Arrange
            var mockRegistry = new Mock<IFeatureRegistry>();
            var mockFeature = new Mock<IOverlayFeature>();
            
            // Setup registry to succeed for first call, fail for second
            var callCount = 0;
            mockRegistry.Setup(r => r.TryCreate(It.IsAny<JsonElement>(), out It.Ref<IOverlayFeature?>.IsAny))
                .Returns((JsonElement element, out IOverlayFeature? feature) =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        feature = mockFeature.Object;
                        return true;
                    }
                    else
                    {
                        feature = null;
                        return false;
                    }
                });

            var factory = new FeatureFactory(mockRegistry.Object);
            
            var jsonArray = JsonSerializer.Deserialize<JsonElement>("""
                [
                    { "type": "valid-feature" },
                    { "type": "invalid-feature" }
                ]
                """);

            // Act
            var features = factory.CreateFromJsonArray(jsonArray);

            // Assert
            Assert.Single(features);
            Assert.Same(mockFeature.Object, features[0]);
        }

        [Fact]
        public void HasBuildingFootprint_WithBuildingFootprint_ReturnsTrue()
        {
            // Arrange
            var mockRegistry = new Mock<IFeatureRegistry>();
            var factory = new FeatureFactory(mockRegistry.Object);
            
            var buildingFootprint = new BuildingFootprint(BuildingMaterial.Wood, FootprintKind.SingleHex);
            var features = new List<IOverlayFeature> { buildingFootprint };

            // Act
            var result = factory.HasBuildingFootprint(features);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasBuildingFootprint_WithoutBuildingFootprint_ReturnsFalse()
        {
            // Arrange
            var mockRegistry = new Mock<IFeatureRegistry>();
            var factory = new FeatureFactory(mockRegistry.Object);
            
            var stairwell = Stairwell.Up();
            var features = new List<IOverlayFeature> { stairwell };

            // Act
            var result = factory.HasBuildingFootprint(features);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Constructor_NullRegistry_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FeatureFactory(null!));
        }
    }
}