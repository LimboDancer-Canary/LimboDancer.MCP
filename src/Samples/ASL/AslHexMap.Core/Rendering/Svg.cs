using System.Globalization;
using System.Text;

namespace AslHexMap.Core.Rendering;

/// <summary>SVG helpers (start/frame/defs/primitives).</summary>
public static class Svg
{
    public static StringBuilder Start(double width, double height, string viewBox, string? label = null)
    {
        var sb = new StringBuilder();
        sb.Append(
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width:0.##}\" height=\"{height:0.##}\" " +
            $"viewBox=\"{viewBox}\" role=\"img\" aria-label=\"{(label ?? "svg")}\">");
        return sb;
    }

    public static void End(StringBuilder sb) => sb.Append("</svg>");

    public static void Background(StringBuilder sb, string fill) =>
        sb.Append($"<rect width=\"100%\" height=\"100%\" fill=\"{fill}\"/>");

    public static void Frame(StringBuilder sb, double w, double h, string stroke = "#ccc")
    {
        sb.Append($"<rect x=\"0.5\" y=\"0.5\" width=\"99%\" height=\"99%\" fill=\"none\" stroke=\"{stroke}\"/>");
    }

    public static void Defs(StringBuilder sb) => sb.Append(TerrainDefs.BuildTerrainDefs("v39"));

    public static void Polygon(StringBuilder sb, string points, string? fill = null, string? stroke = null,
        double strokeWidth = 1.0, double? opacity = null)
    {
        sb.Append("<polygon points=\"").Append(points).Append("\"");
        if (!string.IsNullOrEmpty(fill)) sb.Append(" fill=\"").Append(fill).Append("\"");
        if (!string.IsNullOrEmpty(stroke)) sb.Append(" stroke=\"").Append(stroke).Append("\" stroke-width=\"").Append(strokeWidth.ToString("0.###", CultureInfo.InvariantCulture)).Append("\"");
        if (opacity.HasValue) sb.Append(" opacity=\"").Append(opacity.Value.ToString("0.###", CultureInfo.InvariantCulture)).Append("\"");
        sb.Append("/>");
    }

    public static void Circle(StringBuilder sb, double cx, double cy, double r, string fill) =>
        sb.Append($"<circle cx=\"{cx:0.###}\" cy=\"{cy:0.###}\" r=\"{r:0.###}\" fill=\"{fill}\"/>");

    public static void Text(StringBuilder sb, double x, double y, string text, double px, string fill,
        string anchor = "start", string family = "Segoe UI, Arial, sans-serif", int weight = 400)
    {
        sb.Append($"<text x=\"{x:0.###}\" y=\"{y:0.###}\" text-anchor=\"{anchor}\" font-family=\"{family}\" font-size=\"{px:0.###}\" font-weight=\"{weight}\" fill=\"{fill}\">{text}</text>");
    }

    public static void Rect(StringBuilder sb, double x, double y, double w, double h,
        string fill, string stroke, double strokeWidth, double? opacity = null)
    {
        sb.Append($"<rect x=\"{x:0.###}\" y=\"{y:0.###}\" width=\"{w:0.###}\" height=\"{h:0.###}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{strokeWidth:0.###}\"");
        if (opacity.HasValue) sb.Append($" opacity=\"{opacity.Value:0.###}\"");
        sb.Append("/>");
    }

    public static void Line(StringBuilder sb, double x1, double y1, double x2, double y2, string stroke, double w) =>
        sb.Append($"<line x1=\"{x1:0.###}\" y1=\"{y1:0.###}\" x2=\"{x2:0.###}\" y2=\"{y2:0.###}\" stroke=\"{stroke}\" stroke-width=\"{w:0.###}\"/>");

    public static void Path(StringBuilder sb, string d, string stroke, double w, double opacity = 1.0)
    {
        sb.Append($"<path d=\"{d}\" fill=\"none\" stroke=\"{stroke}\" stroke-width=\"{w:0.###}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"");
        if (opacity < 1.0) sb.Append($" opacity=\"{opacity:0.###}\"");
        sb.Append(" />");
    }
}