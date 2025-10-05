using AslHexMap.Core.Features;
using AslHexMap.Core.Geometry;
using AslHexMap.Core.Layout;
using AslHexMap.Core.Schema;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AslHexMap.Core.Rendering
{
    /// <summary>
    /// Facade for board rendering. Public surface stays the same, but the implementation
    /// is split into focused sub-components: Svg, Hexes, Buildings, Roads, and Util.
    /// </summary>
    public static class Renderer
    {
        // ============================
        // Public API (unchanged)
        // ============================

        /// <summary>Known-good test SVG to validate pipeline.</summary>
        public static string RenderTestSvg()
        {
            const int w = 220, h = 120;
            var sb = Svg.Start(w, h, $"0 0 {w} {h}", label: "test");
            Svg.Background(sb, "#ffeb3b");
            Svg.Frame(sb, w, h, "#000");
            Svg.Text(sb, 10, 24, "TEST SVG", 16, "#000");
            Svg.End(sb);
            return sb.ToString();
        }

        /// <summary>Draw one flat-top hex centered in a small canvas.</summary>
        public static string RenderSingleHex(string baseTerrain = "grain", double size = 60)
        {
            const int width = 320;
            const int height = 260;
            double cx = width * 0.5;
            double cy = height * 0.5;

            var sb = Svg.Start(width, height, $"0 0 {width} {height}", $"{baseTerrain} hex");
            Svg.Background(sb, "#ffffff");
            Svg.Frame(sb, width, height);

            // defs once
            Svg.Defs(sb);

            // underpaint + pattern
            Hexes.DrawBaseHex(sb, cx, cy, size, baseTerrain, strokeWidth: 1.25, underpaintOpacity: 0.25);

            // center dot + label
            Svg.Circle(sb, cx, cy, 3, "#f00");
            Svg.Text(sb, cx, cy + size + 24, baseTerrain, 14, "#222", anchor: "middle");

            Svg.End(sb);
            return sb.ToString();
        }

        /// <summary>Axial grid (q,r) — useful for geometry checks.</summary>
        public static string RenderHexGrid(int cols, int rows, double size, string baseTerrain = "grain")
        {
            var inv = CultureInfo.InvariantCulture;
            var (minX, minY, maxX, maxY) = HexLayout.GridExtentsFlat(cols, rows, size);
            const double margin = 16.0;
            double width = (maxX - minX) + 2 * margin;
            double height = (maxY - minY) + 2 * margin;
            var shift = Util.MakeShifter(minX, minY, margin);

            var sb = Svg.Start(width, height, $"0 0 {width.ToString("0.##", inv)} {height.ToString("0.##", inv)}",
                               $"{cols}x{rows} hex grid ({baseTerrain})");
            Svg.Background(sb, "#ffffff");
            Svg.Frame(sb, width, height);
            Svg.Defs(sb);

            foreach (var (q, r) in HexLayout.AxialRect(cols, rows))
            {
                var (cx, cy) = HexLayout.AxialToPixelFlat(q, r, size);
                (cx, cy) = shift(cx, cy);
                Hexes.DrawBaseHex(sb, cx, cy, size, baseTerrain, strokeWidth: 1.0, underpaintOpacity: 0.15);
                Svg.Circle(sb, cx, cy, 1.6, "#d33");
            }

            Svg.End(sb);
            return sb.ToString();
        }

        /// <summary>Offset (odd-Q) rectangular board — ASL style.</summary>
        public static string RenderOffsetGrid(int cols, int rows, double size, string baseTerrain = "grain")
        {
            var inv = CultureInfo.InvariantCulture;
            var (minX, minY, maxX, maxY) = HexLayout.OffsetRectExtentsFlat(cols, rows, size);
            const double margin = 16.0;
            double width = (maxX - minX) + 2 * margin;
            double height = (maxY - minY) + 2 * margin;
            var shift = Util.MakeShifter(minX, minY, margin);

            var sb = Svg.Start(width, height, $"0 0 {width.ToString("0.##", inv)} {height.ToString("0.##", inv)}",
                               $"{cols}x{rows} offset grid ({baseTerrain})");
            Svg.Background(sb, "#ffffff");
            Svg.Frame(sb, width, height);
            Svg.Defs(sb);

            foreach (var (col, row) in HexLayout.OffsetRect(cols, rows))
            {
                var (cx, cy) = HexLayout.OffsetOddQToPixelFlat(col, row, size);
                (cx, cy) = shift(cx, cy);
                Hexes.DrawBaseHex(sb, cx, cy, size, baseTerrain, strokeWidth: 1.0, underpaintOpacity: 0.15);
                Svg.Circle(sb, cx, cy, 1.6, "#d33");
            }

            Svg.End(sb);
            return sb.ToString();
        }

        /// <summary>Render a board from JSON (bases, labels, roads, overlays).</summary>
        public static string RenderBoardBase(
            BoardData data,
            double size = 36,
            bool showLabels = true,
            bool showRoads = true,
            LegendRenderer.LegendUsage? usage = null,
            bool useFeatureOverlays = true)
        {
            usage ??= new LegendRenderer.LegendUsage(); // ensure not null

            var inv = CultureInfo.InvariantCulture;

            int cols = data.Map?.Dimensions?.Width ?? 0;
            int rows = data.Map?.Dimensions?.Height ?? 0;

            var (minX, minY, maxX, maxY) = HexLayout.OffsetRectExtentsFlat(cols, rows, size);
            const double margin = 16.0;
            double width = (maxX - minX) + 2 * margin;
            double height = (maxY - minY) + 2 * margin;
            var shift = Util.MakeShifter(minX, minY, margin);

            var sb = Svg.Start(
                width, height,
                $"0 0 {width.ToString("0.##", inv)} {height.ToString("0.##", inv)}",
                $"{cols}x{rows} board");

            Svg.Background(sb, "#ffffff");
            Svg.Frame(sb, width, height);
            Svg.Defs(sb); // patterns/brushes used by terrain

            // Templates & defaults (for base terrain resolution)
            var templates = data.HexTemplates ?? new Dictionary<string, HexTemplate>(StringComparer.OrdinalIgnoreCase);
            string defaultTplId = data.Map?.DefaultTemplateId ?? string.Empty;
            templates.TryGetValue(defaultTplId, out var defaultTpl);
            string defaultBase = TerrainStyle.NormalizeBase(defaultTpl?.BaseTerrain ?? "open");

            // Index and roads
            var perHex = Util.IndexPerHex(data);
            var roadItems = Roads.CollectRoads(perHex, data);

            // Feature map (optional new pipeline)
            var featureMap = useFeatureOverlays
                ? AslHexMap.Core.Features.FeatureMacroExpander.BuildFeatureMap(data)
                : new Dictionary<(int col, int row), List<AslHexMap.Core.Features.IOverlayFeature>>();

            // ---------- PASS 1: draw BASES only ----------
            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    perHex.TryGetValue((col, row), out var hex);

                    // Resolve base terrain (template + overrides)
                    string baseTerrain = Hexes.ResolveBaseTerrain(hex, defaultBase, templates);
                    usage.Bases.Add(TerrainStyle.NormalizeBase(baseTerrain));

                    var (cx, cy) = HexLayout.OffsetOddQToPixelFlat(col, row, size);
                    (cx, cy) = shift(cx, cy);

                    Hexes.DrawBaseHex(sb, cx, cy, size, baseTerrain);
                }
            }

            // ---------- ROADS (under overlays) ----------
            if (showRoads)
                Roads.RenderRoads(sb, roadItems, size, shift);

            // ---------- PASS 2: overlays (buildings etc.) + labels ----------
            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    var (cx, cy) = HexLayout.OffsetOddQToPixelFlat(col, row, size);
                    (cx, cy) = shift(cx, cy);

                    if (useFeatureOverlays && featureMap.TryGetValue((col, row), out var feats) && feats is not null)
                    {
                        // detect building + stairwell to coordinate one unified badge
                        var building = feats.OfType<AslHexMap.Core.Features.BuildingFootprint>().FirstOrDefault();
                        bool hasStairwell = feats.OfType<AslHexMap.Core.Features.Stairwell>().Any(s => s.Present);
                        int? levelForBadge = building?.Levels;

                        var ctx = new AslHexMap.Core.Features.FeatureContext
                        {
                            Coord = (col, row),
                            GroupId = building?.GroupId,
                            UseCircularStairwellBadge = hasStairwell && levelForBadge.HasValue,
                            StairwellBadgeLevel = levelForBadge
                        };

                        foreach (var f in feats)
                        {
                            // legend usage
                            var t = f.Token?.ToLowerInvariant() ?? "";
                            if (t.Contains("building-wood")) usage?.Buildings.Add("wood");
                            else if (t.Contains("building-stone")) usage?.Buildings.Add("stone");

                            f.Render(sb, cx, cy, size, ctx);
                        }
                    }

                    // Labels
                    if (showLabels)
                    {
                        var (lx, ly) = HexGeom.LabelAnchorNW(cx, cy, size);
                        var label = $"{Util.IndexToLetters(col)}{row + 1}";
                        Svg.Text(sb, lx, ly + 1, label, 10, "#1a1a1a");
                    }
                }
            }

            Svg.End(sb);
            return sb.ToString();
        }


        public static string RenderLegendIcon(string token, double size = 14)
        {
            var inv = CultureInfo.InvariantCulture;

            // Single-hex canvas extents
            var (minX, minY, maxX, maxY) = HexLayout.OffsetRectExtentsFlat(1, 1, size);
            double margin = 2.0;
            double width = (maxX - minX) + 2 * margin;
            double height = (maxY - minY) + 2 * margin;

            // Simple shifter (same as Util.MakeShifter but inline for this tiny SVG)
            double dx = -minX + margin, dy = -minY + margin;
            (double sx, double sy) Shift(double x, double y) => (x + dx, y + dy);

            var sb = Svg.Start(
                width, height,
                $"0 0 {width.ToString("0.##", inv)} {height.ToString("0.##", inv)}",
                $"legend {token}");

            // No background/frame for legend icons
            Svg.Defs(sb); // patterns for woods/orchard/brush, etc.

            // Hex center
            var (cx0, cy0) = HexLayout.OffsetOddQToPixelFlat(0, 0, size);
            var (cx, cy) = Shift(cx0, cy0);

            // Base terrain (default to open)
            string baseTerrain = "open";
            if (token.StartsWith("base-", StringComparison.OrdinalIgnoreCase))
                baseTerrain = token.Substring(5);

            // Draw the base hex
            Hexes.DrawBaseHex(sb, cx, cy, size, baseTerrain);

            // Overlay features (when token refers to a feature)
            var ctx = new Features.FeatureContext { Coord = (0, 0) };

            if (token.Equals("building-wood", StringComparison.OrdinalIgnoreCase))
            {
                new BuildingFootprint
                {
                    Material = BuildingMaterial.Wood,
                    Footprint = FootprintKind.Center
                }.Render(sb, cx, cy, size, ctx);
            }
            else if (token.Equals("building-stone", StringComparison.OrdinalIgnoreCase))
            {
                new BuildingFootprint
                {
                    Material = BuildingMaterial.Stone,
                    Footprint = FootprintKind.Center
                }.Render(sb, cx, cy, size, ctx);
            }
            else if (token.Equals("feature-stairwell", StringComparison.OrdinalIgnoreCase))
            {
                new Stairwell { Present = true }.Render(sb, cx, cy, size, ctx);
            }
            else if (token.Equals("feature-rowhouse-edge", StringComparison.OrdinalIgnoreCase))
            {
                // Show a single thick facade on the “east-ish” side to communicate the concept
                new RowhouseEdge { Edges = new[] { Side.NE } }.Render(sb, cx, cy, size, ctx);
            }

            Svg.End(sb);
            return sb.ToString();
        }

    }
}
