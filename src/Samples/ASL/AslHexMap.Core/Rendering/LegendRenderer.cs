// LegendRenderer.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AslHexMap.Core.Features;
using AslHexMap.Core.Geometry;

namespace AslHexMap.Core.Rendering
{
    public static class LegendRenderer
    {
        public sealed class LegendUsage
        {
            public ISet<string> Bases { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public ISet<string> Buildings { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public static LegendUsage CreateUsage() => new();

        private static readonly (string id, string label)[] BaseOrder =
        {
            ("open", "Open Ground"),
            ("woods", "Woods"),
            ("orchard", "Orchard"),
            ("brush", "Brush"),
            ("grain", "Grain"),
            ("marsh", "Marsh"),
            ("sand", "Sand"),
            ("scrub", "Scrub")
        };

        private static readonly (string id, string label, BuildingMaterial mat)[] BuildingOrder =
        {
            ("wood", "Wood Building", BuildingMaterial.Wood),
            ("stone", "Stone Building", BuildingMaterial.Stone)
        };

        private static string NormalizeBuildingId(string id)
        {
            var s = (id ?? "").Trim().ToLowerInvariant();
            if (s.StartsWith("stone")) return "stone";
            if (s.StartsWith("wood")) return "wood";
            return s;
        }

        public static string BuildLegendSvg(LegendUsage? usage, double hexSize = 12, string title = "Legend")
        {
            usage ??= new LegendUsage(); // ensure valid instance

            var inv = CultureInfo.InvariantCulture;

            var buildingIds = new HashSet<string>(
                usage.Buildings.Select(NormalizeBuildingId),
                StringComparer.OrdinalIgnoreCase);

            var baseItems = BaseOrder.Where(b => usage.Bases.Contains(b.id)).ToArray();
            var bldItems = BuildingOrder.Where(b => buildingIds.Contains(b.id)).ToArray();

            if (baseItems.Length == 0 && bldItems.Length == 0)
                return "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1\" height=\"1\"></svg>";

            double leftPad = 16, topPad = 18, rowGap = 8, sectionGap = 12, iconTextGap = 12;
            double rowH = hexSize * 2 + rowGap;
            double y = topPad;

            var sb = new StringBuilder();
            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"280\" height=\"{400}\" viewBox=\"0 0 280 {400}\" role=\"img\">");
            sb.Append("<rect width=\"100%\" height=\"100%\" rx=\"8\" ry=\"8\" fill=\"#fff\" stroke=\"#ddd\"/>");
            sb.Append($"<text x=\"{leftPad}\" y=\"{(topPad - 6):0.##}\" font-family=\"Segoe UI, Arial\" font-size=\"18\" font-weight=\"600\">{title}</text>");
            sb.Append(TerrainDefs.BuildTerrainDefs("v39"));

            foreach (var (id, label) in baseItems)
            {
                double cx = leftPad + hexSize;
                double cy = y + hexSize;
                var pts = HexGeom.PointsFlatTop(cx, cy, hexSize);
                var points = HexGeom.ToSvgPoints(pts);
                var patternId = TerrainStyle.PatternIdForBase(id);
                sb.Append($"<polygon points=\"{points}\" fill=\"url(#{patternId})\" stroke=\"#333\" stroke-width=\"0.9\"/>");
                sb.Append($"<text x=\"{cx + hexSize + iconTextGap}\" y=\"{cy + 4}\" font-family=\"Segoe UI, Arial\" font-size=\"14\">{label}</text>");
                y += rowH;
            }

            if (baseItems.Length > 0 && bldItems.Length > 0)
                y += sectionGap;

            foreach (var (_, label, material) in bldItems)
            {
                double cx = leftPad + hexSize;
                double cy = y + hexSize;
                var pts = HexGeom.PointsFlatTop(cx, cy, hexSize);
                var points = HexGeom.ToSvgPoints(pts);
                sb.Append($"<polygon points=\"{points}\" fill=\"#f9f9f9\" stroke=\"#333\" stroke-width=\"0.9\"/>");

                var ctx = new FeatureContext { Coord = (0, 0) };
                new BuildingFootprint { Material = material, Footprint = FootprintKind.Center }.Render(sb, cx, cy, hexSize, ctx);

                sb.Append($"<text x=\"{cx + hexSize + iconTextGap}\" y=\"{cy + 4}\" font-family=\"Segoe UI, Arial\" font-size=\"14\">{label}</text>");
                y += rowH;
            }

            sb.Append("</svg>");
            return sb.ToString();
        }
    }
}
