using System.Text;
using System.Text.Json;
using AslHexMap.Core.Geometry;
using AslHexMap.Core.Schema;

namespace AslHexMap.Core.Rendering;

/// <summary>Base hex drawing + terrain resolution.</summary>
public static class Hexes
{
    public static void DrawBaseHex(StringBuilder sb, double cx, double cy, double size,
        string baseTerrain, double strokeWidth = 1.0, double underpaintOpacity = 0.15)
    {
        var pts = HexGeom.PointsFlatTop(cx, cy, size);
        var points = HexGeom.ToSvgPoints(pts);

        var underpaint = TerrainStyle.Colors.ForBase(baseTerrain);
        var patternId = TerrainStyle.PatternIdForBase(baseTerrain);

        Svg.Polygon(sb, points, fill: underpaint, opacity: underpaintOpacity);
        Svg.Polygon(sb, points, fill: $"url(#{patternId})", stroke: "#333", strokeWidth: strokeWidth);
    }

    public static string ResolveBaseTerrain(IndividualHex? hex, string fallback,
        IDictionary<string, HexTemplate> templates)
    {
        string baseTerrain = fallback;

        if (hex != null)
        {
            if (!string.IsNullOrWhiteSpace(hex.TemplateId) &&
                templates.TryGetValue(hex.TemplateId!, out var tpl))
            {
                baseTerrain = TerrainStyle.NormalizeBase(tpl.BaseTerrain);

                // some templates may carry ground cover in Overlays
                if (tpl.Overlays != null)
                {
                    if (tpl.Overlays.TryGetValue("groundCover", out var gc) && gc is string gcs)
                        baseTerrain = TerrainStyle.NormalizeBase(gcs);
                    else if (tpl.Overlays.ContainsKey("grain"))
                        baseTerrain = "grain";
                }
            }

            // per-hex overrides
            if (hex.Overrides.HasValue && hex.Overrides.Value.ValueKind == JsonValueKind.Object)
            {
                var ov = hex.Overrides.Value;
                if (ov.TryGetProperty("baseTerrain", out var bt) && bt.ValueKind == JsonValueKind.String)
                    baseTerrain = TerrainStyle.NormalizeBase(bt.GetString());
                if (ov.TryGetProperty("groundCover", out var gc2) && gc2.ValueKind == JsonValueKind.String)
                    baseTerrain = TerrainStyle.NormalizeBase(gc2.GetString());
                if (ov.TryGetProperty("grain", out var gr) &&
                    (gr.ValueKind == JsonValueKind.True || gr.ValueKind == JsonValueKind.String))
                    baseTerrain = "grain";
            }
        }

        return baseTerrain;
    }
}