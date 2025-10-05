using System;
using System.Globalization;
using System.Text;

namespace AslHexMap.Core.Rendering
{
    /// <summary>
    /// Helper class for setting up SVG canvas with proper dimensions and coordinate transformations.
    /// </summary>
    public static class CanvasSetup
    {
        private const double DefaultMargin = 16.0;

        /// <summary>
        /// Represents a canvas configuration with StringBuilder and coordinate shifter.
        /// </summary>
        public record CanvasConfig(StringBuilder StringBuilder, Func<double, double, (double, double)> Shifter);

        /// <summary>
        /// Creates a centered canvas for single hex rendering.
        /// </summary>
        public static CanvasConfig CreateCenteredCanvas(int width, int height, string label)
        {
            var sb = Svg.Start(width, height, $"0 0 {width} {height}", label);
            Svg.Background(sb, "#ffffff");
            Svg.Frame(sb, width, height);
            Svg.Defs(sb);

            // Identity shifter for centered canvas
            var shifter = new Func<double, double, (double, double)>((x, y) => (x, y));
            return new CanvasConfig(sb, shifter);
        }

        /// <summary>
        /// Creates a grid canvas with appropriate dimensions and coordinate transformation.
        /// </summary>
        public static CanvasConfig CreateGridCanvas(
            int cols, int rows, double size,
            Func<int, int, double, (double, double, double, double)> extentsFunc,
            string label,
            double margin = DefaultMargin)
        {
            var inv = CultureInfo.InvariantCulture;
            var (minX, minY, maxX, maxY) = extentsFunc(cols, rows, size);
            
            double width = (maxX - minX) + 2 * margin;
            double height = (maxY - minY) + 2 * margin;
            var shifter = Util.MakeShifter(minX, minY, margin);

            var sb = Svg.Start(width, height, 
                $"0 0 {width.ToString("0.##", inv)} {height.ToString("0.##", inv)}", 
                label);
            
            Svg.Background(sb, "#ffffff");
            Svg.Frame(sb, width, height);
            Svg.Defs(sb);

            return new CanvasConfig(sb, shifter);
        }

        /// <summary>
        /// Creates a legend icon canvas for small SVG icons.
        /// </summary>
        public static CanvasConfig CreateLegendCanvas(double size, string label)
        {
            var inv = CultureInfo.InvariantCulture;
            
            // Single-hex canvas extents
            var (minX, minY, maxX, maxY) = Layout.HexLayout.OffsetRectExtentsFlat(1, 1, size);
            double margin = 2.0;
            double width = (maxX - minX) + 2 * margin;
            double height = (maxY - minY) + 2 * margin;

            // Simple shifter for tiny SVG
            double dx = -minX + margin, dy = -minY + margin;
            var shifter = new Func<double, double, (double, double)>((x, y) => (x + dx, y + dy));

            var sb = Svg.Start(width, height, 
                $"0 0 {width.ToString("0.##", inv)} {height.ToString("0.##", inv)}", 
                label);
            
            // No background/frame for legend icons
            Svg.Defs(sb);

            return new CanvasConfig(sb, shifter);
        }
    }
}