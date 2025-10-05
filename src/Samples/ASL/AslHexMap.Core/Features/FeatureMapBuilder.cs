using AslHexMap.Core.Schema;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AslHexMap.Core.Features
{
    /// <summary>
    /// Main feature map builder that orchestrates the creation of overlay features
    /// using specialized helper classes for different responsibilities.
    /// </summary>
    public class FeatureMapBuilder
    {
        private readonly TemplateResolver _templateResolver;
        private readonly FeatureFactory _featureFactory;
        private readonly LegacyMacroExpander _legacyExpander;

        /// <summary>
        /// Initializes a new instance of FeatureMapBuilder with its dependencies.
        /// </summary>
        public FeatureMapBuilder(
            TemplateResolver templateResolver,
            FeatureFactory featureFactory,
            LegacyMacroExpander legacyExpander)
        {
            _templateResolver = templateResolver ?? throw new ArgumentNullException(nameof(templateResolver));
            _featureFactory = featureFactory ?? throw new ArgumentNullException(nameof(featureFactory));
            _legacyExpander = legacyExpander ?? throw new ArgumentNullException(nameof(legacyExpander));
        }

        /// <summary>
        /// Builds a feature map from board data.
        /// </summary>
        /// <param name="data">The board data to process</param>
        /// <returns>Dictionary mapping hex coordinates to lists of overlay features</returns>
        public Dictionary<(int col, int row), List<IOverlayFeature>> BuildFeatureMap(BoardData data)
        {
            var map = new Dictionary<(int col, int row), List<IOverlayFeature>>();
            
            if (data?.Map is null) 
                return map;

            var templates = data.HexTemplates ?? new Dictionary<string, HexTemplate>(StringComparer.OrdinalIgnoreCase);
            var hexes = data.Map.IndividualHexes;
            
            if (hexes is null) 
                return map;

            foreach (var hex in hexes)
            {
                var coordinates = ParseHexCoordinates(hex.HexId);
                if (!coordinates.HasValue)
                    continue;

                var features = ProcessHexFeatures(hex, templates);
                
                if (features.Count > 0)
                    map[coordinates.Value] = features;
            }

            return map;
        }

        /// <summary>
        /// Processes all features for a single hex.
        /// </summary>
        /// <param name="hex">The hex to process</param>
        /// <param name="templates">Available templates</param>
        /// <returns>List of overlay features for the hex</returns>
        private List<IOverlayFeature> ProcessHexFeatures(IndividualHex hex, Dictionary<string, HexTemplate> templates)
        {
            var features = new List<IOverlayFeature>();

            // 1) Process typed features from JSON array overrides
            ProcessTypedFeatures(hex, features);

            // 2) Process legacy building macros if no typed building footprint exists
            ProcessLegacyBuildingMacros(hex, templates, features);

            return features;
        }

        /// <summary>
        /// Processes typed features from JSON array overrides.
        /// </summary>
        /// <param name="hex">The hex to process</param>
        /// <param name="features">Feature list to add to</param>
        private void ProcessTypedFeatures(IndividualHex hex, List<IOverlayFeature> features)
        {
            if (!hex.Overrides.HasValue || hex.Overrides.Value.ValueKind != JsonValueKind.Array)
                return;

            var typedFeatures = _featureFactory.CreateFromJsonArray(hex.Overrides.Value);
            features.AddRange(typedFeatures);
        }

        /// <summary>
        /// Processes legacy building macros if no typed building footprint already exists.
        /// </summary>
        /// <param name="hex">The hex to process</param>
        /// <param name="templates">Available templates</param>
        /// <param name="features">Current feature list to check and add to</param>
        private void ProcessLegacyBuildingMacros(IndividualHex hex, Dictionary<string, HexTemplate> templates, List<IOverlayFeature> features)
        {
            // Skip if we already have a typed building footprint
            if (_featureFactory.HasBuildingFootprint(features))
                return;

            var buildingSpec = ResolveBuildingSpec(hex, templates);
            var buildingFootprint = _legacyExpander.CreateBuildingFromSpec(buildingSpec);
            
            if (buildingFootprint is not null)
                features.Add(buildingFootprint);
        }

        /// <summary>
        /// Resolves building specification from template or overrides.
        /// </summary>
        /// <param name="hex">The hex to resolve for</param>
        /// <param name="templates">Available templates</param>
        /// <returns>Building specification or null if not found</returns>
        private BuildingSpec? ResolveBuildingSpec(IndividualHex hex, Dictionary<string, HexTemplate> templates)
        {
            // Try template first
            var template = _templateResolver.ResolveTemplate(hex, templates);
            var buildingSpec = _templateResolver.ExtractBuildingSpec(template);
            
            if (buildingSpec is not null)
                return buildingSpec;

            // Fall back to overrides
            if (hex.Overrides.HasValue)
                return _legacyExpander.ExtractBuildingSpecFromOverrides(hex.Overrides.Value);

            return null;
        }

        /// <summary>
        /// Safely parses hex coordinates from hex ID string.
        /// </summary>
        /// <param name="hexId">The hex ID to parse</param>
        /// <returns>Parsed coordinates or null if parsing fails</returns>
        private static (int col, int row)? ParseHexCoordinates(string hexId)
        {
            try
            {
                return BoardCoord.Parse(hexId);
            }
            catch
            {
                return null;
            }
        }
    }
}