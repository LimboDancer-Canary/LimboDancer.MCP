using AslHexMap.Core.Features;
using System;
using System.Text;

namespace AslHexMap.Core.Rendering
{
    /// <summary>
    /// Handles rendering of legend icons for different terrain and feature types.
    /// </summary>
    public static class LegendIconRenderer
    {
        /// <summary>
        /// Renders a legend icon based on the token type.
        /// </summary>
        public static void RenderIconContent(StringBuilder sb, string token, double cx, double cy, double size)
        {
            // Base terrain (default to open)
            string baseTerrain = ExtractBaseTerrain(token);
            
            // Draw the base hex
            Hexes.DrawBaseHex(sb, cx, cy, size, baseTerrain);

            // Overlay features (when token refers to a feature)
            RenderFeatureOverlay(sb, token, cx, cy, size);
        }

        private static string ExtractBaseTerrain(string token)
        {
            if (token.StartsWith("base-", StringComparison.OrdinalIgnoreCase))
                return token.Substring(5);
            return "open";
        }

        private static void RenderFeatureOverlay(StringBuilder sb, string token, double cx, double cy, double size)
        {
            var ctx = new FeatureContext { Coord = (0, 0) };

            switch (token.ToLowerInvariant())
            {
                case "building-wood":
                    RenderWoodBuilding(sb, cx, cy, size, ctx);
                    break;
                case "building-stone":
                    RenderStoneBuilding(sb, cx, cy, size, ctx);
                    break;
                case "feature-stairwell":
                    RenderStairwell(sb, cx, cy, size, ctx);
                    break;
                case "feature-rowhouse-edge":
                    RenderRowhouseEdge(sb, cx, cy, size, ctx);
                    break;
            }
        }

        private static void RenderWoodBuilding(StringBuilder sb, double cx, double cy, double size, FeatureContext ctx)
        {
            new BuildingFootprint
            {
                Material = BuildingMaterial.Wood,
                Footprint = FootprintKind.Center
            }.Render(sb, cx, cy, size, ctx);
        }

        private static void RenderStoneBuilding(StringBuilder sb, double cx, double cy, double size, FeatureContext ctx)
        {
            new BuildingFootprint
            {
                Material = BuildingMaterial.Stone,
                Footprint = FootprintKind.Center
            }.Render(sb, cx, cy, size, ctx);
        }

        private static void RenderStairwell(StringBuilder sb, double cx, double cy, double size, FeatureContext ctx)
        {
            new Stairwell { Present = true }.Render(sb, cx, cy, size, ctx);
        }

        private static void RenderRowhouseEdge(StringBuilder sb, double cx, double cy, double size, FeatureContext ctx)
        {
            // Show a single thick facade on the "east-ish" side to communicate the concept
            new RowhouseEdge { Edges = new[] { Schema.Side.NE } }.Render(sb, cx, cy, size, ctx);
        }
    }
}