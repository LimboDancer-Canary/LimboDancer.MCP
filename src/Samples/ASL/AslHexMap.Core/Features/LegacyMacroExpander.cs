using AslHexMap.Core.Schema;
using System;
using System.Text.Json;

namespace AslHexMap.Core.Features
{
    /// <summary>
    /// Handles legacy macro expansion for building specifications.
    /// </summary>
    public class LegacyMacroExpander
    {
        /// <summary>
        /// Creates a BuildingFootprint from a BuildingSpec using legacy rules.
        /// </summary>
        /// <param name="spec">The building specification</param>
        /// <returns>A BuildingFootprint or null if spec is invalid</returns>
        public BuildingFootprint? CreateBuildingFromSpec(BuildingSpec? spec)
        {
            if (spec is null)
                return null;

            var material = DetermineBuildingMaterial(spec.Type);

            return new BuildingFootprint
            {
                Material = material,
                Footprint = FootprintKind.Center,
                Levels = spec.Levels
            };
        }

        /// <summary>
        /// Extracts building specification from JSON object overrides.
        /// </summary>
        /// <param name="overrides">JSON element containing object overrides</param>
        /// <returns>Building specification or null if not found</returns>
        public BuildingSpec? ExtractBuildingSpecFromOverrides(JsonElement overrides)
        {
            if (overrides.ValueKind != JsonValueKind.Object)
                return null;

            if (!overrides.TryGetProperty("building", out var buildingElement) || 
                buildingElement.ValueKind != JsonValueKind.Object)
                return null;

            var spec = new BuildingSpec();

            if (buildingElement.TryGetProperty("type", out var typeElement) && 
                typeElement.ValueKind == JsonValueKind.String)
            {
                spec.Type = typeElement.GetString();
            }

            if (buildingElement.TryGetProperty("levels", out var levelsElement) && 
                levelsElement.ValueKind == JsonValueKind.Number)
            {
                spec.Levels = levelsElement.GetInt32();
            }

            return spec;
        }

        /// <summary>
        /// Determines building material from building type string.
        /// </summary>
        /// <param name="buildingType">The building type string</param>
        /// <returns>Appropriate building material</returns>
        private static BuildingMaterial DetermineBuildingMaterial(string? buildingType)
        {
            return (buildingType ?? "").Equals("stone", StringComparison.OrdinalIgnoreCase)
                ? BuildingMaterial.Stone
                : BuildingMaterial.Wood;
        }
    }
}