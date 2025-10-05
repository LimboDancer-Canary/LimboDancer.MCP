using System.Text;
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
        var resolver = new TerrainResolver();
        return resolver.ResolveBaseTerrain(hex, fallback, templates);
    }
}