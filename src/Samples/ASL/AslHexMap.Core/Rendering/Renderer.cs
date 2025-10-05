using AslHexMap.Core.Features;
using AslHexMap.Core.Geometry;
using AslHexMap.Core.Layout;
using AslHexMap.Core.Schema;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AslHexMap.Core.Rendering
{
    /// <summary>
    /// Facade for board rendering. 
    /// </summary>
    public static class Renderer
    {
        // ============================
        // Public API (unchanged)
        // ============================

        /// <summary>Known-good test SVG to validate pipeline.</summary>
        public static string RenderTestSvg()
        {
            const int w = 220, h = 120;
            var sb = Svg.Start(w, h, $"0 0 {w} {h}", label: "test");
            Svg.Background(sb, "#ffeb3b");
            Svg.Frame(sb, w, h, "#000");
            Svg.Text(sb, 10, 24, "TEST SVG", 16, "#000");
            Svg.End(sb);
            return sb.ToString();
        }

        /// <summary>Draw one flat-top hex centered in a small canvas.</summary>
        public static string RenderSingleHex(string baseTerrain = "grain", double size = 60)
        {
            const int width = 320;
            const int height = 260;
            
            var canvas = CanvasSetup.CreateCenteredCanvas(width, height, $"{baseTerrain} hex");
            var (cx, cy) = (width * 0.5, height * 0.5);

            SingleHexRenderer.RenderHexWithLabel(canvas.StringBuilder, cx, cy, size, baseTerrain);
            
            Svg.End(canvas.StringBuilder);
            return canvas.StringBuilder.ToString();
        }

        /// <summary>Axial grid (q,r) — useful for geometry checks.</summary>
        public static string RenderHexGrid(int cols, int rows, double size, string baseTerrain = "grain")
        {
            var canvas = CanvasSetup.CreateGridCanvas(cols, rows, size, HexLayout.GridExtentsFlat, 
                $"{cols}x{rows} hex grid ({baseTerrain})");
            
            GridRenderer.RenderAxialGrid(canvas.StringBuilder, cols, rows, size, baseTerrain, canvas.Shifter);
            
            Svg.End(canvas.StringBuilder);
            return canvas.StringBuilder.ToString();
        }

        /// <summary>Offset (odd-Q) rectangular board — ASL style.</summary>
        public static string RenderOffsetGrid(int cols, int rows, double size, string baseTerrain = "grain")
        {
            var canvas = CanvasSetup.CreateGridCanvas(cols, rows, size, HexLayout.OffsetRectExtentsFlat,
                $"{cols}x{rows} offset grid ({baseTerrain})");
            
            GridRenderer.RenderOffsetGrid(canvas.StringBuilder, cols, rows, size, baseTerrain, canvas.Shifter);
            
            Svg.End(canvas.StringBuilder);
            return canvas.StringBuilder.ToString();
        }

        /// <summary>Render a board from JSON (bases, labels, roads, overlays).</summary>
        public static string RenderBoardBase(
            BoardData data,
            double size = 36,
            bool showLabels = true,
            bool showRoads = true,
            LegendRenderer.LegendUsage? usage = null,
            bool useFeatureOverlays = true)
        {
            usage ??= new LegendRenderer.LegendUsage(); // ensure not null

            var boardRenderer = new BoardRenderer(data, size, showLabels, showRoads, useFeatureOverlays, usage);
            var context = boardRenderer.PrepareRenderingContext();
            
            var canvas = CreateBoardCanvas(context.Cols, context.Rows, size);
            
            // Render in passes
            boardRenderer.RenderBaseTerrain(canvas.StringBuilder, context, canvas.Shifter);
            boardRenderer.RenderRoads(canvas.StringBuilder, context, canvas.Shifter);
            boardRenderer.RenderOverlaysAndLabels(canvas.StringBuilder, context, canvas.Shifter);

            Svg.End(canvas.StringBuilder);
            return canvas.StringBuilder.ToString();
        }

        public static string RenderLegendIcon(string token, double size = 14)
        {
            var canvas = CanvasSetup.CreateLegendCanvas(size, $"legend {token}");
            
            // Hex center
            var (cx0, cy0) = HexLayout.OffsetOddQToPixelFlat(0, 0, size);
            var (cx, cy) = canvas.Shifter(cx0, cy0);

            LegendIconRenderer.RenderIconContent(canvas.StringBuilder, token, cx, cy, size);

            Svg.End(canvas.StringBuilder);
            return canvas.StringBuilder.ToString();
        }

        // ============================
        // Private helper methods
        // ============================

        private static CanvasSetup.CanvasConfig CreateBoardCanvas(int cols, int rows, double size)
        {
            var inv = CultureInfo.InvariantCulture;
            var (minX, minY, maxX, maxY) = HexLayout.OffsetRectExtentsFlat(cols, rows, size);
            const double margin = 16.0;
            double width = (maxX - minX) + 2 * margin;
            double height = (maxY - minY) + 2 * margin;
            var shift = Util.MakeShifter(minX, minY, margin);

            var sb = Svg.Start(
                width, height,
                $"0 0 {width.ToString("0.##", inv)} {height.ToString("0.##", inv)}",
                $"{cols}x{rows} board");

            Svg.Background(sb, "#ffffff");
            Svg.Frame(sb, width, height);
            Svg.Defs(sb); // patterns/brushes used by terrain

            return new CanvasSetup.CanvasConfig(sb, shift);
        }
    }
}
