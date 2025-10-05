using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AslHexMap.Core.Schema;

namespace AslHexMap.Core.Features
{
    /// <summary>
    /// Static facade for feature map building, maintaining backward compatibility.
    /// Internally uses the new modular architecture for better testability and maintainability.
    /// </summary>
    public static class FeatureMacroExpander
    {
        private static readonly Lazy<FeatureMapBuilder> _builder = new(() =>
        {
            var featureRegistry = new FeatureRegistry();
            var templateResolver = new TemplateResolver();
            var featureFactory = new FeatureFactory(featureRegistry);
            var legacyExpander = new LegacyMacroExpander();
            return new FeatureMapBuilder(templateResolver, featureFactory, legacyExpander);
        });

        /// <summary>
        /// Builds a feature map from board data using the new modular architecture.
        /// This method maintains the original public API for backward compatibility.
        /// </summary>
        /// <param name="data">The board data to process</param>
        /// <returns>Dictionary mapping hex coordinates to lists of overlay features</returns>
        public static Dictionary<(int col, int row), List<IOverlayFeature>> BuildFeatureMap(BoardData data)
        {
            return _builder.Value.BuildFeatureMap(data);
        }

        /// <summary>
        /// Creates a new FeatureMapBuilder instance with default dependencies.
        /// Useful for testing or when you need to customize the building process.
        /// </summary>
        /// <returns>A new FeatureMapBuilder instance</returns>
        public static FeatureMapBuilder CreateBuilder()
        {
            var featureRegistry = new FeatureRegistry();
            var templateResolver = new TemplateResolver();
            var featureFactory = new FeatureFactory(featureRegistry);
            var legacyExpander = new LegacyMacroExpander();
            return new FeatureMapBuilder(templateResolver, featureFactory, legacyExpander);
        }

        /// <summary>
        /// Creates a FeatureMapBuilder with custom dependencies.
        /// Useful for testing with mocked dependencies.
        /// </summary>
        /// <param name="featureRegistry">Custom feature registry</param>
        /// <param name="templateResolver">Custom template resolver</param>
        /// <param name="featureFactory">Custom feature factory</param>
        /// <param name="legacyExpander">Custom legacy macro expander</param>
        /// <returns>A new FeatureMapBuilder instance</returns>
        public static FeatureMapBuilder CreateBuilder(
            IFeatureRegistry featureRegistry,
            TemplateResolver templateResolver,
            FeatureFactory featureFactory,
            LegacyMacroExpander legacyExpander)
        {
            return new FeatureMapBuilder(templateResolver, featureFactory, legacyExpander);
        }

        /// <summary>
        /// Creates a FeatureMapBuilder with custom dependencies.
        /// Useful for testing with mocked dependencies.
        /// </summary>
        /// <param name="templateResolver">Custom template resolver</param>
        /// <param name="featureFactory">Custom feature factory</param>
        /// <param name="legacyExpander">Custom legacy macro expander</param>
        /// <returns>A new FeatureMapBuilder instance</returns>
        public static FeatureMapBuilder CreateBuilder(
            TemplateResolver templateResolver,
            FeatureFactory featureFactory,
            LegacyMacroExpander legacyExpander)
        {
            return new FeatureMapBuilder(templateResolver, featureFactory, legacyExpander);
        }
    }
}
