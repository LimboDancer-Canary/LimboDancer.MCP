using AslHexMap.Core.Geometry;     // HexGeom
using AslHexMap.Core.Layout;       // Side
using System;
using System.Linq;                 // For FlushSides.First()
using System.Text;
using System.Text.Json;
using AslHexMap.Core.Schema;

namespace AslHexMap.Core.Features
{
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
