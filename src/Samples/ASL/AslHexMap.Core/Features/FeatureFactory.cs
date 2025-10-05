using AslHexMap.Core.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace AslHexMap.Core.Features
{
    /// <summary>
    /// Factory for creating overlay features from various sources.
    /// </summary>
    public class FeatureFactory
    {
        /// <summary>
        /// Creates typed features from JSON array overrides.
        /// </summary>
        /// <param name="overrides">JSON element containing array of feature definitions</param>
        /// <returns>List of created overlay features</returns>
        public List<IOverlayFeature> CreateFromJsonArray(JsonElement overrides)
        {
            var features = new List<IOverlayFeature>();

            if (overrides.ValueKind != JsonValueKind.Array)
                return features;

            foreach (var element in overrides.EnumerateArray())
            {
                if (FeatureRegistry.TryCreate(element, out var feature) && feature is not null)
                {
                    features.Add(feature);
                }
            }

            return features;
        }

        /// <summary>
        /// Checks if any of the provided features is a building footprint.
        /// </summary>
        /// <param name="features">Features to check</param>
        /// <returns>True if a building footprint is present</returns>
        public bool HasBuildingFootprint(IEnumerable<IOverlayFeature> features)
        {
            return features.Any(f => f is BuildingFootprint);
        }
    }
}