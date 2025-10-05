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
            usage ??= new LegendUsage();

            var config = new LegendConfig(hexSize, 16, 18, 8, 12, 12);
            var calculator = new LegendLayoutCalculator();
            var layout = calculator.CalculateLayout(usage, config);
            
            var builder = new LegendSvgBuilder();
            return builder.BuildSvg(layout, config, title);
        }
    }

    public record LegendConfig(
        double HexSize, 
        double LeftPad, 
        double TopPad, 
        double RowGap,
        double SectionGap,
        double IconTextGap);

    public record LegendItem(string Id, string Label, LegendItemType Type, BuildingMaterial? Material = null);

    public enum LegendItemType
    {
        Base,
        Building
    }

    public record LegendLayout(
        IReadOnlyList<LegendItem> Items,
        double TotalHeight,
        double TotalWidth,
        IReadOnlyList<LegendItemPosition> ItemPositions);

    public record LegendItemPosition(double X, double Y, double IconX, double IconY, double TextX, double TextY);

    public class LegendLayoutCalculator
    {
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

        public LegendLayout CalculateLayout(LegendRenderer.LegendUsage usage, LegendConfig config)
        {
            var items = new List<LegendItem>();
            var positions = new List<LegendItemPosition>();

            var buildingIds = new HashSet<string>(
                usage.Buildings.Select(NormalizeBuildingId),
                StringComparer.OrdinalIgnoreCase);

            var baseItems = BaseOrder.Where(b => usage.Bases.Contains(b.id))
                .Select(b => new LegendItem(b.id, b.label, LegendItemType.Base))
                .ToArray();

            var bldItems = BuildingOrder.Where(b => buildingIds.Contains(b.id))
                .Select(b => new LegendItem(b.id, b.label, LegendItemType.Building, b.mat))
                .ToArray();

            items.AddRange(baseItems);
            items.AddRange(bldItems);

            if (items.Count == 0)
            {
                return new LegendLayout(items, 1, 1, positions);
            }

            double rowH = config.HexSize * 2 + config.RowGap;
            double y = config.TopPad;

            // Calculate positions for base items
            foreach (var item in baseItems)
            {
                double iconX = config.LeftPad + config.HexSize;
                double iconY = y + config.HexSize;
                double textX = iconX + config.HexSize + config.IconTextGap;
                double textY = iconY + 4;

                positions.Add(new LegendItemPosition(config.LeftPad, y, iconX, iconY, textX, textY));
                y += rowH;
            }

            // Add section gap if both sections exist
            if (baseItems.Length > 0 && bldItems.Length > 0)
                y += config.SectionGap;

            // Calculate positions for building items
            foreach (var item in bldItems)
            {
                double iconX = config.LeftPad + config.HexSize;
                double iconY = y + config.HexSize;
                double textX = iconX + config.HexSize + config.IconTextGap;
                double textY = iconY + 4;

                positions.Add(new LegendItemPosition(config.LeftPad, y, iconX, iconY, textX, textY));
                y += rowH;
            }

            double totalHeight = y + config.TopPad;
            double totalWidth = 280; // Fixed width for now

            return new LegendLayout(items, totalHeight, totalWidth, positions);
        }

        private static string NormalizeBuildingId(string id)
        {
            var s = (id ?? "").Trim().ToLowerInvariant();
            if (s.StartsWith("stone")) return "stone";
            if (s.StartsWith("wood")) return "wood";
            return s;
        }
    }

    public class LegendSvgBuilder
    {
        public string BuildSvg(LegendLayout layout, LegendConfig config, string title = "Legend")
        {
            if (layout.Items.Count == 0)
            {
                return "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1\" height=\"1\"></svg>";
            }

            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();

            // SVG root element
            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{layout.TotalWidth}\" height=\"{layout.TotalHeight}\" viewBox=\"0 0 {layout.TotalWidth} {layout.TotalHeight}\" role=\"img\">");
            
            // Background
            sb.Append("<rect width=\"100%\" height=\"100%\" rx=\"8\" ry=\"8\" fill=\"#fff\" stroke=\"#ddd\"/>");
            
            // Title
            sb.Append($"<text x=\"{config.LeftPad}\" y=\"{(config.TopPad - 6):0.##}\" font-family=\"Segoe UI, Arial\" font-size=\"18\" font-weight=\"600\">{title}</text>");
            
            // Terrain definitions
            sb.Append(TerrainDefs.BuildTerrainDefs("v39"));

            // Render items
            for (int i = 0; i < layout.Items.Count; i++)
            {
                var item = layout.Items[i];
                var position = layout.ItemPositions[i];

                RenderLegendItem(sb, item, position, config);
            }

            sb.Append("</svg>");
            return sb.ToString();
        }

        private static void RenderLegendItem(StringBuilder sb, LegendItem item, LegendItemPosition position, LegendConfig config)
        {
            var pts = HexGeom.PointsFlatTop(position.IconX, position.IconY, config.HexSize);
            var points = HexGeom.ToSvgPoints(pts);

            if (item.Type == LegendItemType.Base)
            {
                var patternId = TerrainStyle.PatternIdForBase(item.Id);
                sb.Append($"<polygon points=\"{points}\" fill=\"url(#{patternId})\" stroke=\"#333\" stroke-width=\"0.9\"/>");
            }
            else if (item.Type == LegendItemType.Building && item.Material.HasValue)
            {
                sb.Append($"<polygon points=\"{points}\" fill=\"#f9f9f9\" stroke=\"#333\" stroke-width=\"0.9\"/>");
                
                var ctx = new FeatureContext { Coord = (0, 0) };
                new BuildingFootprint { Material = item.Material.Value, Footprint = FootprintKind.Center }
                    .Render(sb, position.IconX, position.IconY, config.HexSize, ctx);
            }

            // Render text label
            sb.Append($"<text x=\"{position.TextX}\" y=\"{position.TextY}\" font-family=\"Segoe UI, Arial\" font-size=\"14\">{item.Label}</text>");
        }
    }
}
