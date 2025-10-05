using AslHexMap.Core.Layout;
using System.Text;

namespace AslHexMap.Core.Rendering
{
    /// <summary>
    /// Specialized renderer for single hex displays.
    /// </summary>
    public static class SingleHexRenderer
    {
        /// <summary>
        /// Renders a single hex with center dot and label.
        /// </summary>
        public static void RenderHexWithLabel(StringBuilder sb, double cx, double cy, double size, string baseTerrain)
        {
            // underpaint + pattern
            Hexes.DrawBaseHex(sb, cx, cy, size, baseTerrain, strokeWidth: 1.25, underpaintOpacity: 0.25);

            // center dot + label
            Svg.Circle(sb, cx, cy, 3, "#f00");
            Svg.Text(sb, cx, cy + size + 24, baseTerrain, 14, "#222", anchor: "middle");
        }
    }
}