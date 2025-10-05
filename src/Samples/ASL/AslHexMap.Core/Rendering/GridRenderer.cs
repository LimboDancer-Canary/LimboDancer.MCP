using AslHexMap.Core.Layout;
using System;
using System.Text;

namespace AslHexMap.Core.Rendering
{
    /// <summary>
    /// Specialized renderer for hex grid displays.
    /// </summary>
    public static class GridRenderer
    {
        /// <summary>
        /// Renders an axial grid with hexes and center dots.
        /// </summary>
        public static void RenderAxialGrid(StringBuilder sb, int cols, int rows, double size, string baseTerrain, 
            Func<double, double, (double, double)> shifter)
        {
            foreach (var (q, r) in HexLayout.AxialRect(cols, rows))
            {
                var (cx, cy) = HexLayout.AxialToPixelFlat(q, r, size);
                (cx, cy) = shifter(cx, cy);
                Hexes.DrawBaseHex(sb, cx, cy, size, baseTerrain, strokeWidth: 1.0, underpaintOpacity: 0.15);
                Svg.Circle(sb, cx, cy, 1.6, "#d33");
            }
        }

        /// <summary>
        /// Renders an offset grid with hexes and center dots.
        /// </summary>
        public static void RenderOffsetGrid(StringBuilder sb, int cols, int rows, double size, string baseTerrain,
            Func<double, double, (double, double)> shifter)
        {
            foreach (var (col, row) in HexLayout.OffsetRect(cols, rows))
            {
                var (cx, cy) = HexLayout.OffsetOddQToPixelFlat(col, row, size);
                (cx, cy) = shifter(cx, cy);
                Hexes.DrawBaseHex(sb, cx, cy, size, baseTerrain, strokeWidth: 1.0, underpaintOpacity: 0.15);
                Svg.Circle(sb, cx, cy, 1.6, "#d33");
            }
        }
    }
}