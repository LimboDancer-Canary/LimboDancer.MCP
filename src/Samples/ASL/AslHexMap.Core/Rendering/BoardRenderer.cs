using AslHexMap.Core.Features;
using AslHexMap.Core.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AslHexMap.Core.Rendering
{
    /// <summary>
    /// Handles the complex board rendering logic including templates, features, and overlays.
    /// </summary>
    public class BoardRenderer
    {
        private readonly BoardData _data;
        private readonly double _size;
        private readonly bool _showLabels;
        private readonly bool _showRoads;
        private readonly bool _useFeatureOverlays;
        private readonly LegendRenderer.LegendUsage _usage;

        public BoardRenderer(BoardData data, double size, bool showLabels, bool showRoads, 
            bool useFeatureOverlays, LegendRenderer.LegendUsage usage)
        {
            _data = data;
            _size = size;
            _showLabels = showLabels;
            _showRoads = showRoads;
            _useFeatureOverlays = useFeatureOverlays;
            _usage = usage;
        }

        /// <summary>
        /// Prepares the rendering context with templates, defaults, and feature maps.
        /// </summary>
        public BoardRenderingContext PrepareRenderingContext()
        {
            int cols = _data.Map?.Dimensions?.Width ?? 0;
            int rows = _data.Map?.Dimensions?.Height ?? 0;

            // Templates & defaults (for base terrain resolution)
            var templates = _data.HexTemplates ?? new Dictionary<string, HexTemplate>(StringComparer.OrdinalIgnoreCase);
            string defaultTplId = _data.Map?.DefaultTemplateId ?? string.Empty;
            templates.TryGetValue(defaultTplId, out var defaultTpl);
            string defaultBase = TerrainStyle.NormalizeBase(defaultTpl?.BaseTerrain ?? "open");

            // Index and roads
            var perHex = Util.IndexPerHex(_data);
            var roadItems = Roads.CollectRoads(perHex, _data);

            // Feature map (optional new pipeline)
            var featureMap = _useFeatureOverlays
                ? FeatureMacroExpander.BuildFeatureMap(_data)
                : new Dictionary<(int col, int row), List<IOverlayFeature>>();

            return new BoardRenderingContext(cols, rows, templates, defaultBase, perHex, roadItems, featureMap);
        }

        /// <summary>
        /// Renders the base terrain layer for all hexes.
        /// </summary>
        public void RenderBaseTerrain(StringBuilder sb, BoardRenderingContext context, 
            Func<double, double, (double, double)> shifter)
        {
            for (int col = 0; col < context.Cols; col++)
            {
                for (int row = 0; row < context.Rows; row++)
                {
                    context.PerHex.TryGetValue((col, row), out var hex);

                    // Resolve base terrain (template + overrides)
                    string baseTerrain = Hexes.ResolveBaseTerrain(hex, context.DefaultBase, context.Templates);
                    _usage.Bases.Add(TerrainStyle.NormalizeBase(baseTerrain));

                    var (cx, cy) = Layout.HexLayout.OffsetOddQToPixelFlat(col, row, _size);
                    (cx, cy) = shifter(cx, cy);

                    Hexes.DrawBaseHex(sb, cx, cy, _size, baseTerrain);
                }
            }
        }

        /// <summary>
        /// Renders roads if enabled.
        /// </summary>
        public void RenderRoads(StringBuilder sb, BoardRenderingContext context, 
            Func<double, double, (double, double)> shifter)
        {
            if (_showRoads)
                Roads.RenderRoads(sb, context.RoadItems, _size, shifter);
        }

        /// <summary>
        /// Renders overlays (buildings, features) and labels.
        /// </summary>
        public void RenderOverlaysAndLabels(StringBuilder sb, BoardRenderingContext context, 
            Func<double, double, (double, double)> shifter)
        {
            for (int col = 0; col < context.Cols; col++)
            {
                for (int row = 0; row < context.Rows; row++)
                {
                    var (cx, cy) = Layout.HexLayout.OffsetOddQToPixelFlat(col, row, _size);
                    (cx, cy) = shifter(cx, cy);

                    RenderHexFeatures(sb, context, col, row, cx, cy);
                    RenderHexLabel(sb, col, row, cx, cy);
                }
            }
        }

        private void RenderHexFeatures(StringBuilder sb, BoardRenderingContext context, int col, int row, double cx, double cy)
        {
            if (!_useFeatureOverlays || !context.FeatureMap.TryGetValue((col, row), out var feats) || feats is null)
                return;

            var featureContext = CreateFeatureContext(feats, col, row);

            foreach (var feature in feats)
            {
                UpdateLegendUsage(feature);
                feature.Render(sb, cx, cy, _size, featureContext);
            }
        }

        private FeatureContext CreateFeatureContext(List<IOverlayFeature> feats, int col, int row)
        {
            // detect building + stairwell to coordinate one unified badge
            var building = feats.OfType<BuildingFootprint>().FirstOrDefault();
            bool hasStairwell = feats.OfType<Stairwell>().Any(s => s.Present);
            int? levelForBadge = building?.Levels;

            return new FeatureContext
            {
                Coord = (col, row),
                GroupId = building?.GroupId,
                UseCircularStairwellBadge = hasStairwell && levelForBadge.HasValue,
                StairwellBadgeLevel = levelForBadge
            };
        }

        private void UpdateLegendUsage(IOverlayFeature feature)
        {
            var token = feature.Token?.ToLowerInvariant() ?? "";
            if (token.Contains("building-wood")) _usage?.Buildings.Add("wood");
            else if (token.Contains("building-stone")) _usage?.Buildings.Add("stone");
        }

        private void RenderHexLabel(StringBuilder sb, int col, int row, double cx, double cy)
        {
            if (!_showLabels) return;

            var (lx, ly) = Geometry.HexGeom.LabelAnchorNW(cx, cy, _size);
            var label = $"{Util.IndexToLetters(col)}{row + 1}";
            Svg.Text(sb, lx, ly + 1, label, 10, "#1a1a1a");
        }
    }

    /// <summary>
    /// Contains all the prepared data needed for board rendering.
    /// </summary>
    public record BoardRenderingContext(
        int Cols,
        int Rows,
        Dictionary<string, HexTemplate> Templates,
        string DefaultBase,
        Dictionary<(int col, int row), IndividualHex> PerHex,
        List<(int col, int row, Side? enters, Side? exits)> RoadItems,
        Dictionary<(int col, int row), List<IOverlayFeature>> FeatureMap);
}