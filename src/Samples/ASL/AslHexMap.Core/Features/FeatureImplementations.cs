using AslHexMap.Core.Geometry;     // HexGeom
using AslHexMap.Core.Layout;       // Side
using System;
using System.Linq;                 // For FlushSides.First()
using System.Text;
using System.Text.Json;
using AslHexMap.Core.Schema;

namespace AslHexMap.Core.Features
{
    public enum FootprintKind { Center, Span, Side }
    public enum FootprintOrientation { W_E, NE_SW, NW_SE }
    public enum BuildingMaterial { Wood, Stone }

    public sealed class BuildingFootprint : IOverlayFeature
    {
        public BuildingMaterial Material { get; init; }
        public FootprintKind Footprint { get; init; }
        public FootprintOrientation Orientation { get; init; } = FootprintOrientation.W_E;
        /// <summary>Thickness as a multiple of the Hex-Lab baseline height; clamped to [0.3, 0.9] if set.</summary>
        public double? Depth { get; init; }
        public string? GroupId { get; init; }
        /// <summary>For Side footprints: which hexside to flush. If omitted, we pick an “east-ish” default for the axis.</summary>
        public HashSet<Side>? FlushSides { get; init; }

        public string Token => Material == BuildingMaterial.Wood ? "building-wood" : "building-stone2";

        public void Render(StringBuilder sb, double cx, double cy, double size, FeatureContext ctx)
        {
            // Geometry ratios derived from Hex Lab look
            double ap = size * Math.Cos(Math.PI / 6.0);      // apothem (center -> edge)
            double labH = size * (20.0 / 30.0);              // baseline rect height for size
            double h = (Depth.HasValue ? Math.Clamp(Depth.Value, 0.3, 0.9) : 1.0) * labH;

            double w = Footprint switch
            {
                FootprintKind.Center => size * 1.0,          // Hex Lab: width ~ size
                FootprintKind.Span => 2 * ap - size * 0.02,// touches two opposite sides (tiny inset to avoid overdraw)
                FootprintKind.Side => ap,                  // stub from center to one side
                _ => size
            };

            double deg = Orientation switch
            {
                FootprintOrientation.W_E => 0,
                FootprintOrientation.NE_SW => 60,
                FootprintOrientation.NW_SE => 120,
                _ => 0
            };

            // For Side footprints, move the center along the axis so one end flushes the chosen side
            if (Footprint == FootprintKind.Side)
            {
                // Choose default flush side per axis (only NE/NW/SW are used; there is no E/W in Side enum)
                Side side = Orientation switch
                {
                    FootprintOrientation.W_E => Side.NE,   // “east-ish”
                    FootprintOrientation.NE_SW => Side.NE,   // positive axis end
                    FootprintOrientation.NW_SE => Side.NW,   // positive axis end
                    _ => Side.NE
                };
                if (FlushSides is { Count: > 0 })
                    side = FlushSides.First();

                var axis = UnitAlongAxis(deg);
                int sign = AxisSignForSide(Orientation, side);
                double off = (ap - (w / 2.0)) * sign;        // shift so far end lands on the side line
                cx += axis.x * off;
                cy += axis.y * off;
            }

            // Local rect
            double x = cx - w / 2.0;
            double y = cy - h / 2.0;

            sb.Append($"<g transform=\"rotate({deg:0.###} {cx:0.###} {cy:0.###})\">");

            // Body
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

            sb.Append("</g>");
        }

        private static (double x, double y) UnitAlongAxis(double deg)
        {
            double a = deg * Math.PI / 180.0;
            return (Math.Cos(a), Math.Sin(a));
        }

        // Map the “positive” direction of each axis to an allowed Side value (no E/W in enum).
        // W_E axis: NE/SE are “east-ish” (+), NW/SW are “west-ish” (-); N/S map to nearest “east-ish” (+).
        private static int AxisSignForSide(FootprintOrientation axis, Side side) => axis switch
        {
            FootprintOrientation.W_E => (side == Side.NW || side == Side.SW) ? -1 : +1,
            FootprintOrientation.NE_SW => (side == Side.SW) ? -1 : +1,   // -axis toward SW, +axis toward NE
            FootprintOrientation.NW_SE => (side == Side.NW) ? -1 : +1,   // -axis toward NW, +axis toward SE
            _ => +1
        };

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

            return new BuildingFootprint
            {
                Material = mat,
                Footprint = kind,
                Orientation = ori,
                Depth = depth,
                GroupId = groupId,
                FlushSides = flush
            };
        }
    }

    public sealed class Stairwell : IOverlayFeature
    {
        public bool Present { get; init; } = true;
        public string Token => "feature-stairwell";

        public void Render(StringBuilder sb, double cx, double cy, double size, FeatureContext ctx)
        {
            if (!Present) return;
            double bw = size * (10.0 / 30.0), bh = size * (10.0 / 30.0);
            double bx = cx - bw / 2.0, by = cy - bh / 2.0;
            double sw = Math.Max(0.3, size * 0.012);
            sb.Append($"<rect x=\"{bx:0.###}\" y=\"{by:0.###}\" width=\"{bw:0.###}\" height=\"{bh:0.###}\" fill=\"#fff\" stroke=\"#333\" stroke-width=\"{sw:0.###}\"/>");
        }

        public static Stairwell FromJson(JsonElement el) =>
            new() { Present = !el.TryGetProperty("present", out var p) || p.ValueKind != JsonValueKind.False };
    }

    public sealed class RowhouseEdge : IOverlayFeature
    {
        public IReadOnlyList<Side> Edges { get; init; } = Array.Empty<Side>();
        public string Token => "feature-rowhouse-edge";

        public void Render(StringBuilder sb, double cx, double cy, double size, FeatureContext ctx)
        {
            var verts = HexGeom.PointsFlatTop(cx, cy, size);
            double w = size * 0.08;
            for (int i = 0; i < 6; i++)
            {
                var side = (Side)i;
                if (!Edges.Contains(side)) continue;
                var a = verts[i];
                var b = verts[(i + 1) % 6];
                sb.Append($"<line x1=\"{a.x:0.###}\" y1=\"{a.y:0.###}\" x2=\"{b.x:0.###}\" y2=\"{b.y:0.###}\" stroke=\"#000\" stroke-width=\"{w:0.###}\" stroke-linecap=\"round\"/>");
            }
        }

        public static RowhouseEdge FromJson(JsonElement el)
        {
            var edges = new List<Side>();
            if (el.TryGetProperty("edges", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var x in arr.EnumerateArray())
                {
                    if (x.ValueKind == JsonValueKind.String && Enum.TryParse<Side>(x.GetString(), true, out var s)) edges.Add(s);
                    else if (x.ValueKind == JsonValueKind.Number) edges.Add((Side)x.GetInt32());
                }
            }
            return new RowhouseEdge { Edges = edges };
        }
    }
}
