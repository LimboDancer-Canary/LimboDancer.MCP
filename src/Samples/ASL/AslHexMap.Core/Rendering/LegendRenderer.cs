using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AslHexMap.Core.Geometry;

namespace AslHexMap.Core.Rendering
{
    /// <summary>
    /// Builds a terrain legend (base terrains now; building materials optional).
    /// </summary>
    public static class LegendRenderer
    {
        // Track what the board used (bases + overlays)
        public sealed class LegendUsage
        {
            public ISet<string> Bases { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public ISet<string> Buildings { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public static LegendUsage CreateUsage() => new();

        // Friendly labels & deterministic order for bases
        private static readonly (string id, string label)[] BaseOrder =
        {
            ("open",   "Open Ground"),
            ("woods",  "Woods"),
            ("orchard","Orchard"),
            ("brush",  "Brush"),
            ("grain",  "Grain"),
            ("marsh",  "Marsh"),
            ("sand",   "Sand"),
            ("scrub",  "Scrub"),
        };

        // Buildings we know how to render as overlays (pattern ids come from TerrainDefs)
        private static readonly (string pid, string label)[] BuildingOrder =
        {
            ("stone2", "Stone Building (2 levels)"),
            ("stone1", "Stone Building (1 level)"),
            ("wood",   "Wood Building"),
        };

        /// <summary>
        /// Produce a combined legend (Base terrains + Buildings) as a tidy SVG.
        /// If Buildings set is empty, only bases are shown.
        /// </summary>
        public static string BuildLegendSvg(LegendUsage usage, double hexSize = 12, string title = "Map Legend")
        {
            var inv = CultureInfo.InvariantCulture;

            var baseItems = BaseOrder.Where(b => usage.Bases.Contains(b.id)).ToArray();
            var bldItems = BuildingOrder.Where(b => usage.Buildings.Contains(b.pid)).ToArray();

            // If nothing to show, return a tiny blank SVG to avoid layout jumps.
            if (baseItems.Length == 0 && bldItems.Length == 0)
                return "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\" viewBox=\"0 0 10 10\"></svg>";

            // --- Layout constants ---
            double leftPad = 16, rightPad = 16, topPad = 18, bottomPad = 12;
            double rowGap = 8, sectionGap = 12, iconTextGap = 12;
            double rowH = hexSize * 2 + rowGap;
            double textBaselineOffset = 4;
            double textWidth = 180; // rough budget; good enough for our labels
            double iconBand = hexSize * 2;

            // Compute height
            int baseRows = baseItems.Length;
            int bldRows = bldItems.Length;
            double innerHeight = 0;
            if (baseRows > 0) innerHeight += baseRows * rowH;
            if (bldRows > 0)
            {
                if (innerHeight > 0) innerHeight += sectionGap; // gap between sections
                innerHeight += bldRows * rowH;
            }

            double width = leftPad + iconBand + iconTextGap + textWidth + rightPad;
            double height = topPad + innerHeight + bottomPad;

            var sb = new StringBuilder();
            sb.Append(
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" " +
                $"width=\"{width.ToString("0.##", inv)}\" height=\"{height.ToString("0.##", inv)}\" " +
                $"viewBox=\"0 0 {width.ToString("0.##", inv)} {height.ToString("0.##", inv)}\" " +
                $"role=\"img\" aria-label=\"Terrain legend\">");

            // Card background
            sb.Append("<rect width=\"100%\" height=\"100%\" rx=\"10\" ry=\"10\" fill=\"#ffffff\" stroke=\"#dadee5\"/>");

            // Title
            if (!string.IsNullOrWhiteSpace(title))
            {
                sb.Append($"<text x=\"{leftPad}\" y=\"{(topPad - 6).ToString("0.##", inv)}\" " +
                          $"font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"18\" font-weight=\"600\" fill=\"#1b1f23\">{title}</text>");
            }

            // Inject terrain defs once (for fills)
            sb.Append(TerrainDefs.BuildTerrainDefs("v39"));

            // Cursor Y
            double y = topPad;

            // --- Base section ---
            if (baseRows > 0)
            {
                for (int i = 0; i < baseItems.Length; i++)
                {
                    var id = baseItems[i].id;
                    var label = baseItems[i].label;

                    double cx = leftPad + hexSize;
                    double cy = y + hexSize;

                    var pts = HexGeom.PointsFlatTop(cx, cy, hexSize);
                    var points = HexGeom.ToSvgPoints(pts);

                    var underpaint = TerrainStyle.Colors.ForBase(id);
                    var patternId = TerrainStyle.PatternIdForBase(id);

                    sb.Append($"<polygon points=\"{points}\" fill=\"{underpaint}\" opacity=\"0.18\"/>");
                    sb.Append($"<polygon points=\"{points}\" fill=\"url(#{patternId})\" stroke=\"#333\" stroke-width=\"0.9\"/>");

                    double tx = cx + hexSize + iconTextGap;
                    double ty = cy + textBaselineOffset;

                    sb.Append($"<text x=\"{tx.ToString("0.###", inv)}\" y=\"{ty.ToString("0.###", inv)}\" " +
                              $"font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"14\" fill=\"#1b1f23\">{label}</text>");

                    y += rowH;
                }
            }

            // --- Gap between sections ---
            if (bldRows > 0 && baseRows > 0) y += sectionGap;

            // --- Building section ---
            if (bldRows > 0)
            {
                for (int i = 0; i < bldItems.Length; i++)
                {
                    var pid = bldItems[i].pid;   // pattern id (e.g., stone2)
                    var label = bldItems[i].label;

                    double cx = leftPad + hexSize;
                    double cy = y + hexSize;

                    var pts = HexGeom.PointsFlatTop(cx, cy, hexSize);
                    var points = HexGeom.ToSvgPoints(pts);

                    // Buildings: draw the pattern only (no underpaint), so the material stands out
                    sb.Append($"<polygon points=\"{points}\" fill=\"url(#{pid})\" stroke=\"#333\" stroke-width=\"0.9\"/>");

                    double tx = cx + hexSize + iconTextGap;
                    double ty = cy + textBaselineOffset;

                    sb.Append($"<text x=\"{tx.ToString("0.###", inv)}\" y=\"{ty.ToString("0.###", inv)}\" " +
                              $"font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"14\" fill=\"#1b1f23\">{label}</text>");

                    y += rowH;
                }
            }

            sb.Append("</svg>");
            return sb.ToString();
        }
    }
}
