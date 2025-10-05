using AslHexMap.Core.Features;
using AslHexMap.Core.Schema;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace AslHexMap.Core.Tests.Features
{
    /// <summary>
    /// Example unit tests demonstrating improved testability of the refactored feature building system.
    /// </summary>
    public class FeatureMapBuilderTests
    {
        [Fact]
        public void TemplateResolver_ResolveTemplate_ReturnsCorrectTemplate()
        {
            // Arrange
            var resolver = new TemplateResolver();
            var templates = new Dictionary<string, HexTemplate>(StringComparer.OrdinalIgnoreCase)
            {
                ["wood"] = new HexTemplate { Id = "wood", BaseTerrain = "open" }
            };
            var hex = new IndividualHex { HexId = "A1", TemplateId = "wood" };

            // Act
            var result = resolver.ResolveTemplate(hex, templates);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("wood", result.Id);
            Assert.Equal("open", result.BaseTerrain);
        }

        [Fact]
        public void TemplateResolver_ResolveTemplate_WithInvalidTemplateId_ReturnsNull()
        {
            // Arrange
            var resolver = new TemplateResolver();
            var templates = new Dictionary<string, HexTemplate>(StringComparer.OrdinalIgnoreCase);
            var hex = new IndividualHex { HexId = "A1", TemplateId = "nonexistent" };

            // Act
            var result = resolver.ResolveTemplate(hex, templates);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void LegacyMacroExpander_CreateBuildingFromSpec_CreatesCorrectFootprint()
        {
            // Arrange
            var expander = new LegacyMacroExpander();
            var spec = new BuildingSpec { Type = "stone", Levels = 2 };

            // Act
            var result = expander.CreateBuildingFromSpec(spec);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(BuildingMaterial.Stone, result.Material);
            Assert.Equal(FootprintKind.Center, result.Footprint);
            Assert.Equal(2, result.Levels);
        }

        [Fact]
        public void LegacyMacroExpander_CreateBuildingFromSpec_WithWoodType_CreatesWoodBuilding()
        {
            // Arrange
            var expander = new LegacyMacroExpander();
            var spec = new BuildingSpec { Type = "wood", Levels = 1 };

            // Act
            var result = expander.CreateBuildingFromSpec(spec);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(BuildingMaterial.Wood, result.Material);
        }

        [Fact]
        public void FeatureFactory_HasBuildingFootprint_WithBuildingFootprint_ReturnsTrue()
        {
            // Arrange
            var factory = new FeatureFactory();
            var features = new List<IOverlayFeature>
            {
                new BuildingFootprint { Material = BuildingMaterial.Wood, Footprint = FootprintKind.Center }
            };

            // Act
            var result = factory.HasBuildingFootprint(features);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void FeatureFactory_HasBuildingFootprint_WithoutBuildingFootprint_ReturnsFalse()
        {
            // Arrange
            var factory = new FeatureFactory();
            var features = new List<IOverlayFeature>
            {
                new Stairwell { Present = true }
            };

            // Act
            var result = factory.HasBuildingFootprint(features);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void FeatureMapBuilder_BuildFeatureMap_WithEmptyData_ReturnsEmptyMap()
        {
            // Arrange
            var builder = FeatureMacroExpander.CreateBuilder();
            var data = new BoardData { Map = null };

            // Act
            var result = builder.BuildFeatureMap(data);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}