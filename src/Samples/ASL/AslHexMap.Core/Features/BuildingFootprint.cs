using AslHexMap.Core.Layout;   // Side enum
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using AslHexMap.Core.Schema;

namespace AslHexMap.Core.Features
{
    /// <summary>
    /// Hex-Lab style rectangular building overlay drawn on top of the base terrain.
    /// </summary>
    public sealed class BuildingFootprint : IOverlayFeature
    {
        // Supplied by JSON (see FromJson)
        public BuildingMaterial Material { get; init; }
        public FootprintKind Footprint { get; init; }
        public FootprintOrientation Orientation { get; init; } = FootprintOrientation.W_E;
        /// <summary>Thickness as a multiple of the Hex-Lab baseline height; clamped to [0.3, 0.9] if set.</summary>
        public double? Depth { get; init; }
        public string? GroupId { get; init; }
        /// <summary>For Side footprints: which hexside to flush against. If omitted, we pick an “east-ish” default for the chosen axis.</summary>
        public HashSet<Side>? FlushSides { get; init; }
        /// <summary>Building levels (1, 2, 3…). If provided, we draw a level badge (unless a stairwell circle will host it).</summary>
        public int? Levels { get; init; }

        /// <summary>Legend token used by the UI/legend service.</summary>
        public string Token
        {
            get
            {
                if (Material == BuildingMaterial.Wood)
                    return "building-wood";

                if (Material == BuildingMaterial.Stone)
                    return "building-stone"; // normalized for legend usage

                return "building";
            }
        }

        public void Render(StringBuilder sb, double cx, double cy, double size, FeatureContext ctx)
        {
            // --- geometry ratios derived from the Hex Lab look ---
            double ap = size * Math.Cos(Math.PI / 6.0);      // apothem (center -> edge)
            double labH = size * (20.0 / 30.0);              // baseline rect height for this hex size
            double h = (Depth.HasValue ? Math.Clamp(Depth.Value, 0.3, 0.9) : 1.0) * labH;

            double w = Footprint switch
            {
                FootprintKind.Center => size * 1.0,          // approx hex width for center block
                FootprintKind.Span => 2 * ap - size * 0.02,  // touches opposite sides (tiny inset to avoid overdraw)
                FootprintKind.Side => ap,                    // stub from center to a chosen side
                _ => size
            };

            double deg = Orientation switch
            {
                FootprintOrientation.W_E => 0,
                FootprintOrientation.NE_SW => 60,
                FootprintOrientation.NW_SE => 120,
                _ => 0
            };

            // For Side footprints, shift center along the axis so one end flushes the chosen side
            if (Footprint == FootprintKind.Side)
            {
                // Choose default “flush” side per axis; can be overridden by FlushSides.
                Side side = Orientation switch
                {
                    FootprintOrientation.W_E => Side.NE,     // “east-ish”
                    FootprintOrientation.NE_SW => Side.NE,    // positive axis end
                    FootprintOrientation.NW_SE => Side.NW,    // positive axis end
                    _ => Side.NE
                };
                if (FlushSides is { Count: > 0 })
                    side = FlushSides.First();

                var axis = UnitAlongAxis(deg);
                int sign = AxisSignForSide(Orientation, side);
                double off = (ap - (w / 2.0)) * sign;        // move so far end lands on the side line
                cx += axis.x * off;
                cy += axis.y * off;
            }

            // Local rect for the body
            double x = cx - w / 2.0;
            double y = cy - h / 2.0;

            sb.Append($"<g transform=\"rotate({deg:0.###} {cx:0.###} {cy:0.###})\">");

            // Body fill + outline
            double strokeW = Math.Max(0.4, size * 0.016);
            string fill = (Material == BuildingMaterial.Wood) ? "#8b6914" : "#8b7d6b";
            sb.Append($"<rect x=\"{x:0.###}\" y=\"{y:0.###}\" width=\"{w:0.###}\" height=\"{h:0.###}\" fill=\"{fill}\" stroke=\"#333\" stroke-width=\"{strokeW:0.###}\"/>");

            // Texture accents
            if (Material == BuildingMaterial.Wood)
            {
                var dark = "#654b0e";
                var lineW = Math.Max(0.3, size * 0.010);
                double y20 = y + h * 0.20, y50 = y + h * 0.50, y80 = y + h * 0.80, x2 = x + w;
                sb.Append($"<line x1=\"{x:0.###}\" y1=\"{y20:0.###}\" x2=\"{x2:0.###}\" y2=\"{y20:0.###}\" stroke=\"{dark}\" stroke-width=\"{lineW:0.###}\"/>");
                sb.Append($"<line x1=\"{x:0.###}\" y1=\"{y50:0.###}\" x2=\"{x2:0.###}\" y2=\"{y50:0.###}\" stroke=\"{dark}\" stroke-width=\"{lineW:0.###}\"/>");
                sb.Append($"<line x1=\"{x:0.###}\" y1=\"{y80:0.###}\" x2=\"{x2:0.###}\" y2=\"{y80:0.###}\" stroke=\"{dark}\" stroke-width=\"{lineW:0.###}\"/>");
            }
            else
            {
                var dark = "#5c5248";
                double y30 = y + h * 0.30, y60 = y + h * 0.60, x2 = x + w;
                sb.Append($"<line x1=\"{x:0.###}\" y1=\"{y30:0.###}\" x2=\"{x2:0.###}\" y2=\"{y30:0.###}\" stroke=\"{dark}\" stroke-width=\"{strokeW:0.###}\"/>");
                sb.Append($"<line x1=\"{x:0.###}\" y1=\"{y60:0.###}\" x2=\"{x2:0.###}\" y2=\"{y60:0.###}\" stroke=\"{dark}\" stroke-width=\"{strokeW:0.###}\"/>");
                // soft shadow for mass
                double off = size * (4.0 / 30.0);
                sb.Append($"<rect x=\"{(x + off):0.###}\" y=\"{(y + off):0.###}\" width=\"{w:0.###}\" height=\"{h:0.###}\" fill=\"{dark}\" opacity=\"0.15\"/>");
            }

            // === Level badge logic ===
            // If the stairwell badge will host the number, suppress the rectangle here.
            bool levelsProvided = Levels is int L0 && L0 >= 1;
            bool letStairwellOwnBadge = ctx.UseCircularStairwellBadge && ctx.StairwellBadgeLevel.HasValue;

            if (levelsProvided && !letStairwellOwnBadge)
            {
                int L = Math.Clamp(Levels!.Value, 1, 9); // keep glyph readable
                double bw = size * (10.0 / 30.0), bh = size * (8.0 / 30.0);
                double bx = cx - bw / 2.0, by = cy - bh / 2.0;
                double bsw = Math.Max(0.3, size * 0.010);
                // card
                sb.Append($"<rect x=\"{bx:0.###}\" y=\"{by:0.###}\" width=\"{bw:0.###}\" height=\"{bh:0.###}\" fill=\"#fff\" stroke=\"#333\" stroke-width=\"{bsw:0.###}\"/>");
                // number
                double fontPx = size * 0.20;
                sb.Append($"<text x=\"{cx:0.###}\" y=\"{(cy + fontPx * 0.10):0.###}\" text-anchor=\"middle\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"{fontPx:0.###}\" font-weight=\"700\" fill=\"#333\">{L}</text>");
            }

            sb.Append("</g>");
        }

        // --- helpers ---

        private static (double x, double y) UnitAlongAxis(double deg)
        {
            double a = deg * Math.PI / 180.0;
            return (Math.Cos(a), Math.Sin(a));
        }

        private static int AxisSignForSide(FootprintOrientation axis, Side side) => axis switch
        {
            FootprintOrientation.W_E => (side == Side.NW || side == Side.SW) ? -1 : +1, // NW/SW are “west-ish”
            FootprintOrientation.NE_SW => (side == Side.SW) ? -1 : +1,                    // -axis toward SW, +axis toward NE
            FootprintOrientation.NW_SE => (side == Side.NW) ? -1 : +1,                    // -axis toward NW, +axis toward SE
            _ => +1
        };

        // --- JSON loader ---

        public static BuildingFootprint FromJson(JsonElement el)
        {
            var mat = (el.TryGetProperty("material", out var m) && m.GetString()?.Trim().ToLowerInvariant() == "stone")
                ? BuildingMaterial.Stone : BuildingMaterial.Wood;

            var kind = FootprintKind.Center;
            if (el.TryGetProperty("footprint", out var f) && f.ValueKind == JsonValueKind.String)
                Enum.TryParse(f.GetString(), true, out kind);

            var ori = FootprintOrientation.W_E;
            if (el.TryGetProperty("orientation", out var o) && o.ValueKind == JsonValueKind.String)
                Enum.TryParse(o.GetString(), true, out ori);

            double? depth = null;
            if (el.TryGetProperty("depth", out var d) && d.ValueKind == JsonValueKind.Number)
                depth = d.GetDouble();

            string? groupId = null;
            if (el.TryGetProperty("groupId", out var g) && g.ValueKind == JsonValueKind.String)
                groupId = g.GetString();

            HashSet<Side>? flush = null;
            if (el.TryGetProperty("flushSides", out var fs) && fs.ValueKind == JsonValueKind.Array)
            {
                flush = new();
                foreach (var x in fs.EnumerateArray())
                {
                    if (x.ValueKind == JsonValueKind.String && Enum.TryParse<Side>(x.GetString(), true, out var s)) flush.Add(s);
                    else if (x.ValueKind == JsonValueKind.Number) flush.Add((Side)x.GetInt32());
                }
            }

            int? levels = null;
            if (el.TryGetProperty("levels", out var lv))
            {
                if (lv.ValueKind == JsonValueKind.Number) levels = lv.GetInt32();
                else if (lv.ValueKind == JsonValueKind.String && int.TryParse(lv.GetString(), out var n)) levels = n;
            }

            return new BuildingFootprint
            {
                Material = mat,
                Footprint = kind,
                Orientation = ori,
                Depth = depth,
                GroupId = groupId,
                FlushSides = flush,
                Levels = levels
            };
        }
    }
}
