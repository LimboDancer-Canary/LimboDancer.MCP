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

    public sealed class Stairwell : IOverlayFeature
    {
        public bool Present { get; init; }
        public string Token => "feature-stairwell";

        public void Render(StringBuilder sb, double cx, double cy, double size, FeatureContext ctx)
        {
            if (!Present) return;

            // If a building level was provided for this hex/group, render a circular badge
            // at the stairwell location (center for now) and draw the level inside it.
            if (ctx.StairwellBadgeLevel is int lvl && lvl >= 1)
            {
                lvl = Math.Clamp(lvl, 1, 9);
                double r = size * 0.18;
                double sw = Math.Max(0.35, size * 0.012);

                // white circle marker
                sb.Append($"<circle cx=\"{cx:0.###}\" cy=\"{cy:0.###}\" r=\"{r:0.###}\" fill=\"#fff\" stroke=\"#333\" stroke-width=\"{sw:0.###}\"/>");

                // level digit
                double fontPx = size * 0.20;
                sb.Append($"<text x=\"{cx:0.###}\" y=\"{(cy + fontPx * 0.10):0.###}\" text-anchor=\"middle\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"{fontPx:0.###}\" font-weight=\"700\" fill=\"#333\">{lvl}</text>");
                return;
            }

            // Fallback: simple small “S” circle if no level was provided by the building
            {
                double r = size * 0.16;
                double sw = Math.Max(0.35, size * 0.012);
                sb.Append($"<circle cx=\"{cx:0.###}\" cy=\"{cy:0.###}\" r=\"{r:0.###}\" fill=\"#fff\" stroke=\"#333\" stroke-width=\"{sw:0.###}\"/>");
                double fontPx = size * 0.18;
                sb.Append($"<text x=\"{cx:0.###}\" y=\"{(cy + fontPx * 0.10):0.###}\" text-anchor=\"middle\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"{fontPx:0.###}\" font-weight=\"700\" fill=\"#333\">S</text>");
            }
        }

        public static Stairwell FromJson(JsonElement el)
        {
            // Default: present = true (so a bare "type":"stairwell" works)
            bool present = true;

            if (el.ValueKind == JsonValueKind.Object &&
                el.TryGetProperty("present", out var p))
            {
                if (p.ValueKind == JsonValueKind.True || p.ValueKind == JsonValueKind.False)
                    present = p.GetBoolean();
                else if (p.ValueKind == JsonValueKind.String &&
                         bool.TryParse(p.GetString(), out var b))
                    present = b;
            }

            return new Stairwell { Present = present };
        }

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
