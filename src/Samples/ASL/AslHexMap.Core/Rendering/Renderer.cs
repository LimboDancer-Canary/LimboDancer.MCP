using AslHexMap.Core.Geometry;
using AslHexMap.Core.Layout;
using AslHexMap.Core.Schema;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AslHexMap.Core.Rendering
{
    public static class Renderer
    {
        // Local building colors (mirrors Hex Lab look)
        private const string WoodFill = "#8b6914";
        private const string WoodFillDark = "#654b0e";
        private const string StoneFill = "#8b7d6b";
        private const string StoneFillDark = "#5c5248";

        /// <summary>Known-good test SVG to validate pipeline.</summary>
        public static string RenderTestSvg()
        {
            const int w = 220, h = 120;
            var sb = new StringBuilder();
            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{w}\" height=\"{h}\" viewBox=\"0 0 {w} {h}\">");
            sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"#ffeb3b\"/>");
            sb.Append("<rect x=\"0.5\" y=\"0.5\" width=\"219\" height=\"119\" fill=\"none\" stroke=\"#000\" stroke-width=\"1\"/>");
            sb.Append("<text x=\"10\" y=\"24\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"16\" fill=\"#000\">TEST SVG</text>");
            sb.Append("</svg>");
            return sb.ToString();
        }

        /// <summary>Draw one flat-top hex centered in a small canvas.</summary>
        public static string RenderSingleHex(string baseTerrain = "grain", double size = 60)
        {
            const int width = 320;
            const int height = 260;
            double cx = width * 0.5;
            double cy = height * 0.5;

            var pts = HexGeom.PointsFlatTop(cx, cy, size);
            var pointsAttr = HexGeom.ToSvgPoints(pts);

            var sb = new StringBuilder();
            sb.Append(
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" " +
                $"viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"{baseTerrain} hex\">");

            // Background + frame
            sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");
            sb.Append($"<rect x=\"0.5\" y=\"0.5\" width=\"{width - 1}\" height=\"{height - 1}\" fill=\"none\" stroke=\"#888\" stroke-dasharray=\"4 3\"/>");

            // Underpaint + pattern
            var underpaint = TerrainStyle.Colors.ForBase(baseTerrain);
            sb.Append($"<polygon points=\"{pointsAttr}\" fill=\"{underpaint}\" opacity=\"0.25\"/>");

            sb.Append(TerrainDefs.BuildTerrainDefs("v39"));
            var patternId = TerrainStyle.PatternIdForBase(baseTerrain);
            sb.Append($"<polygon points=\"{pointsAttr}\" fill=\"url(#{patternId})\" stroke=\"#333\" stroke-width=\"1.25\"/>");

            // Center dot + label
            sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"3\" fill=\"#f00\"/>");
            sb.Append($"<text x=\"{cx}\" y=\"{cy + size + 24}\" text-anchor=\"middle\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"14\" fill=\"#222\">{baseTerrain}</text>");

            sb.Append("</svg>");
            return sb.ToString();
        }

        /// <summary>Axial grid (q,r) — useful for geometry checks.</summary>
        public static string RenderHexGrid(int cols, int rows, double size, string baseTerrain = "grain")
        {
            var inv = CultureInfo.InvariantCulture;

            var (minX, minY, maxX, maxY) = HexLayout.GridExtentsFlat(cols, rows, size);
            const double margin = 16.0;
            double width = (maxX - minX) + 2 * margin;
            double height = (maxY - minY) + 2 * margin;

            (double x, double y) Shift(double x, double y) => (x - minX + margin, y - minY + margin);

            var sb = new StringBuilder();
            sb.Append(
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width.ToString("0.##", inv)}\" height=\"{height.ToString("0.##", inv)}\" " +
                $"viewBox=\"0 0 {width.ToString("0.##", inv)} {height.ToString("0.##", inv)}\" role=\"img\" aria-label=\"{cols}x{rows} hex grid ({baseTerrain})\">");

            sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");
            sb.Append("<rect x=\"0.5\" y=\"0.5\" width=\"99%\" height=\"99%\" fill=\"none\" stroke=\"#ccc\"/>");

            sb.Append(TerrainDefs.BuildTerrainDefs("v39"));
            var patternId = TerrainStyle.PatternIdForBase(baseTerrain);
            var underpaint = TerrainStyle.Colors.ForBase(baseTerrain);

            foreach (var qr in HexLayout.AxialRect(cols, rows))
            {
                var q = qr.Item1;
                var r = qr.Item2;

                var (cx, cy) = HexLayout.AxialToPixelFlat(q, r, size);
                (cx, cy) = Shift(cx, cy);

                var pts = HexGeom.PointsFlatTop(cx, cy, size);
                var points = HexGeom.ToSvgPoints(pts);

                sb.Append($"<polygon points=\"{points}\" fill=\"{underpaint}\" opacity=\"0.15\"/>");
                sb.Append($"<polygon points=\"{points}\" fill=\"url(#{patternId})\" stroke=\"#333\" stroke-width=\"1\"/>");
                sb.Append($"<circle cx=\"{cx.ToString("0.###", inv)}\" cy=\"{cy.ToString("0.###", inv)}\" r=\"1.6\" fill=\"#d33\"/>");
            }

            sb.Append("</svg>");
            return sb.ToString();
        }

        /// <summary>Offset (odd-Q) rectangular board — ASL style.</summary>
        public static string RenderOffsetGrid(int cols, int rows, double size, string baseTerrain = "grain")
        {
            var inv = CultureInfo.InvariantCulture;

            var (minX, minY, maxX, maxY) = HexLayout.OffsetRectExtentsFlat(cols, rows, size);
            const double margin = 16.0;
            double width = (maxX - minX) + 2 * margin;
            double height = (maxY - minY) + 2 * margin;

            (double x, double y) Shift(double x, double y) => (x - minX + margin, y - minY + margin);

            var sb = new StringBuilder();
            sb.Append(
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width.ToString("0.##", inv)}\" height=\"{height.ToString("0.##", inv)}\" " +
                $"viewBox=\"0 0 {width.ToString("0.##", inv)} {height.ToString("0.##", inv)}\" role=\"img\" aria-label=\"{cols}x{rows} offset grid ({baseTerrain})\">");

            sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");
            sb.Append("<rect x=\"0.5\" y=\"0.5\" width=\"99%\" height=\"99%\" fill=\"none\" stroke=\"#ccc\"/>");

            sb.Append(TerrainDefs.BuildTerrainDefs("v39"));
            var patternId = TerrainStyle.PatternIdForBase(baseTerrain);
            var underpaint = TerrainStyle.Colors.ForBase(baseTerrain);

            foreach (var cr in HexLayout.OffsetRect(cols, rows))
            {
                var col = cr.Item1;
                var row = cr.Item2;

                var (cx, cy) = HexLayout.OffsetOddQToPixelFlat(col, row, size);
                (cx, cy) = Shift(cx, cy);

                var pts = HexGeom.PointsFlatTop(cx, cy, size);
                var points = HexGeom.ToSvgPoints(pts);

                sb.Append($"<polygon points=\"{points}\" fill=\"{underpaint}\" opacity=\"0.15\"/>");
                sb.Append($"<polygon points=\"{points}\" fill=\"url(#{patternId})\" stroke=\"#333\" stroke-width=\"1\"/>");
                sb.Append($"<circle cx=\"{cx.ToString("0.###", inv)}\" cy=\"{cy.ToString("0.###", inv)}\" r=\"1.6\" fill=\"#d33\"/>");
            }

            sb.Append("</svg>");
            return sb.ToString();
        }

        /// <summary>
        /// Render a board from JSON (bases, labels, roads, Hex Lab building footprints).
        /// </summary>
        public static string RenderBoardBase(
            BoardData data,
            double size = 36,
            bool showLabels = true,
            bool showRoads = true,
            LegendRenderer.LegendUsage? usage = null)
        {
            var inv = CultureInfo.InvariantCulture;

            int cols = data.Map?.Dimensions?.Width ?? 0;
            int rows = data.Map?.Dimensions?.Height ?? 0;

            var (minX, minY, maxX, maxY) = HexLayout.OffsetRectExtentsFlat(cols, rows, size);
            const double margin = 16.0;
            double width = (maxX - minX) + 2 * margin;
            double height = (maxY - minY) + 2 * margin;

            (double x, double y) Shift(double x, double y) => (x - minX + margin, y - minY + margin);

            // Templates are a dict in our sample.
            var templates = data.HexTemplates ?? new Dictionary<string, HexTemplate>(StringComparer.OrdinalIgnoreCase);

            string defaultTplId = data.Map?.DefaultTemplateId ?? string.Empty;
            templates.TryGetValue(defaultTplId, out var defaultTpl);
            string defaultBase = TerrainStyle.NormalizeBase(defaultTpl?.BaseTerrain ?? "open");

            // Per-hex & road collection
            var perHex = new Dictionary<(int col, int row), IndividualHex>();
            var roadItems = new List<(int col, int row, Side? enters, Side? exits)>();

            // Parse Side from JSON (number 0..5 or string "N|NE|SE|S|SW|NW")
            static Side? ParseSide(JsonElement el)
            {
                if (el.ValueKind == JsonValueKind.Null) return null;
                if (el.ValueKind == JsonValueKind.Number) return (Side)el.GetInt32();
                if (el.ValueKind == JsonValueKind.String)
                {
                    var s = el.GetString()?.Trim().ToUpperInvariant();
                    return s switch
                    {
                        "N" => Side.N,
                        "NE" => Side.NE,
                        "SE" => Side.SE,
                        "S" => Side.S,
                        "SW" => Side.SW,
                        "NW" => Side.NW,
                        "0" => Side.N,
                        "1" => Side.NE,
                        "2" => Side.SE,
                        "3" => Side.S,
                        "4" => Side.SW,
                        "5" => Side.NW,
                        _ => null
                    };
                }
                return null;
            }

            var list = data.Map?.IndividualHexes;
            if (list != null)
            {
                foreach (var h in list)
                {
                    (int col, int row) k;
                    try { k = BoardCoord.Parse(h.HexId); } catch { continue; }

                    perHex[k] = h;

                    // roads in overrides arrays
                    if (h.Overrides.HasValue && h.Overrides.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in h.Overrides.Value.EnumerateArray())
                        {
                            if (item.ValueKind != JsonValueKind.Object) continue;
                            if (!item.TryGetProperty("type", out var t) || t.ValueKind != JsonValueKind.String) continue;
                            if (!string.Equals(t.GetString(), "road", StringComparison.OrdinalIgnoreCase)) continue;

                            item.TryGetProperty("enters", out var eIn);
                            item.TryGetProperty("exits", out var eOut);
                            var enters = ParseSide(eIn);
                            var exits = ParseSide(eOut);

                            if (enters is not null || exits is not null)
                                roadItems.Add((k.col, k.row, enters, exits));
                        }
                    }
                }
            }

            var sb = new StringBuilder();
            sb.Append(
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width.ToString("0.##", inv)}\" height=\"{height.ToString("0.##", inv)}\" " +
                $"viewBox=\"0 0 {width.ToString("0.##", inv)} {height.ToString("0.##", inv)}\" role=\"img\" aria-label=\"{cols}x{rows} board\">");

            sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");
            sb.Append("<rect x=\"0.5\" y=\"0.5\" width=\"99%\" height=\"99%\" fill=\"none\" stroke=\"#ccc\"/>");

            // Defs once
            sb.Append(TerrainDefs.BuildTerrainDefs("v39"));

            // --- Hex fills, labels, buildings ---
            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    IndividualHex? cellHex = null;
                    perHex.TryGetValue((col, row), out cellHex);

                    string baseTerrain = defaultBase;

                    if (cellHex != null)
                    {
                        // TemplateId -> base terrain
                        if (!string.IsNullOrWhiteSpace(cellHex.TemplateId) &&
                            templates.TryGetValue(cellHex.TemplateId!, out var tpl2))
                        {
                            baseTerrain = TerrainStyle.NormalizeBase(tpl2.BaseTerrain);

                            // Heuristic: some templates might express base as overlay fields
                            if (tpl2.Overlays != null)
                            {
                                if (tpl2.Overlays.TryGetValue("groundCover", out var gc) && gc is string gcs)
                                    baseTerrain = TerrainStyle.NormalizeBase(gcs);
                                else if (tpl2.Overlays.ContainsKey("grain"))
                                    baseTerrain = "grain";
                            }
                        }

                        // Overrides object could also override base
                        if (cellHex.Overrides.HasValue && cellHex.Overrides.Value.ValueKind == JsonValueKind.Object)
                        {
                            var ov = cellHex.Overrides.Value;
                            if (ov.TryGetProperty("baseTerrain", out var bt) && bt.ValueKind == JsonValueKind.String)
                                baseTerrain = TerrainStyle.NormalizeBase(bt.GetString());
                            if (ov.TryGetProperty("groundCover", out var gc2) && gc2.ValueKind == JsonValueKind.String)
                                baseTerrain = TerrainStyle.NormalizeBase(gc2.GetString());
                            if (ov.TryGetProperty("grain", out var gr) &&
                                (gr.ValueKind == JsonValueKind.True || gr.ValueKind == JsonValueKind.String))
                                baseTerrain = "grain";
                        }
                    }

                    usage?.Bases.Add(TerrainStyle.NormalizeBase(baseTerrain));

                    var (cx, cy) = HexLayout.OffsetOddQToPixelFlat(col, row, size);
                    (cx, cy) = Shift(cx, cy);

                    var pts = HexGeom.PointsFlatTop(cx, cy, size);
                    var points = HexGeom.ToSvgPoints(pts);

                    var underpaint = TerrainStyle.Colors.ForBase(baseTerrain);
                    var patternId = TerrainStyle.PatternIdForBase(baseTerrain);

                    sb.Append($"<polygon points=\"{points}\" fill=\"{underpaint}\" opacity=\"0.15\"/>");
                    sb.Append($"<polygon points=\"{points}\" fill=\"url(#{patternId})\" stroke=\"#333\" stroke-width=\"1\"/>");

                    // --- Building overlay (Hex Lab footprint) ---
                    BuildingSpec? bspec = null;

                    if (cellHex != null &&
                        !string.IsNullOrWhiteSpace(cellHex.TemplateId) &&
                        templates.TryGetValue(cellHex.TemplateId!, out var tplB))
                    {
                        bspec = tplB.Building;
                    }

                    if (bspec is null && cellHex?.Overrides is { } ovEl && ovEl.ValueKind == JsonValueKind.Object)
                    {
                        if (ovEl.TryGetProperty("building", out var b) && b.ValueKind == JsonValueKind.Object)
                        {
                            var spec = new BuildingSpec();
                            if (b.TryGetProperty("type", out var tEl) && tEl.ValueKind == JsonValueKind.String)
                                spec.Type = tEl.GetString();
                            if (b.TryGetProperty("levels", out var lEl) && lEl.ValueKind == JsonValueKind.Number)
                                spec.Levels = lEl.GetInt32();
                            bspec = spec;
                        }
                    }

                    if (bspec is not null)
                    {
                        var t = (bspec.Type ?? "").Trim().ToLowerInvariant();
                        if (t == "wooden") t = "wood";
                        int levels = bspec.Levels.GetValueOrDefault(1);

                        if (t == "wood" && levels <= 1)
                        {
                            usage?.Buildings.Add("wood");
                            AppendWoodBuilding1(sb, cx, cy, size);
                        }
                        else if (t == "stone" && levels >= 2)
                        {
                            usage?.Buildings.Add("stone2");
                            AppendStoneBuilding2(sb, cx, cy, size);
                        }
                    }

                    if (showLabels)
                    {
                        var (lx, ly) = HexGeom.LabelAnchorNW(cx, cy, size);
                        var label = $"{IndexToLetters(col)}{row + 1}";
                        sb.Append($"<text x=\"{lx.ToString("0.###", inv)}\" y=\"{(ly + 1).ToString("0.###", inv)}\" " +
                                  $"font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"10\" fill=\"#1a1a1a\">{label}</text>");
                    }
                }
            }

            // --- Roads ---
            if (showRoads)
            {
                // Also include any global traversals if present
                foreach (var t in data.AllTraversals)
                {
                    if (!string.Equals(t.Type, "road", StringComparison.OrdinalIgnoreCase)) continue;
                    if (t.Enters is null && t.Exits is null) continue;

                    (int col, int row) k;
                    try { k = BoardCoord.Parse(t.HexId); } catch { continue; }
                    roadItems.Add((k.col, k.row, t.Enters, t.Exits));
                }

                // local helpers (Hex Lab-like)
                static double AngleForSide(Side s) => s switch
                {
                    Side.N => 3 * Math.PI / 2,
                    Side.NE => 11 * Math.PI / 6,
                    Side.SE => Math.PI / 6,
                    Side.S => Math.PI / 2,
                    Side.SW => 5 * Math.PI / 6,
                    Side.NW => 7 * Math.PI / 6,
                    _ => 0
                };

                static (double, double) Polar(double a, double r) => (r * Math.Cos(a), r * Math.Sin(a));
                static (double, double) Scale((double, double) p, double t) => (p.Item1 * t, p.Item2 * t);

                static double MidAngle(double a, double b)
                {
                    double da = ((b - a + Math.PI * 3) % (Math.PI * 2)) - Math.PI;
                    return a + da / 2.0;
                }

                string BuildRoadPath(double cx, double cy, double size, Side enters, Side? exits)
                {
                    double ap = size * Math.Cos(Math.PI / 6.0); // apothem
                    double aIn = AngleForSide(enters);

                    var P1 = Scale(Polar(aIn, ap), 1.02); // slight overhang
                    double P1x = cx + P1.Item1, P1y = cy + P1.Item2;

                    if (exits is null)
                    {
                        var inward = (-Math.Cos(aIn), -Math.Sin(aIn));
                        var Cx = cx + (P1.Item1 + inward.Item1 * size * 0.55);
                        var Cy = cy + (P1.Item2 + inward.Item2 * size * 0.55);
                        return $"M {P1x:0.###} {P1y:0.###} Q {Cx:0.###} {Cy:0.###} {cx:0.###} {cy:0.###}";
                    }

                    double aOut = AngleForSide(exits.Value);
                    var P2 = Scale(Polar(aOut, ap), 1.02);
                    double P2x = cx + P2.Item1, P2y = cy + P2.Item2;

                    int di = (((int)exits.Value - (int)enters) % 6 + 6) % 6;

                    if (di == 1 || di == 5)
                    {
                        double mid = MidAngle(aIn, aOut);
                        var C = Polar(mid, size * 0.35);
                        return $"M {P1x:0.###} {P1y:0.###} Q {cx + C.Item1:0.###} {cy + C.Item2:0.###} {P2x:0.###} {P2y:0.###}";
                    }
                    else if (di == 2 || di == 4)
                    {
                        return $"M {P1x:0.###} {P1y:0.###} Q {cx:0.###} {cy:0.###} {P2x:0.###} {P2y:0.###}";
                    }
                    else
                    {
                        double vx = P2.Item1 - P1.Item1, vy = P2.Item2 - P1.Item2;
                        var C = (-vy * 0.15, vx * 0.15);
                        return $"M {P1x:0.###} {P1y:0.###} Q {cx + C.Item1:0.###} {cy + C.Item2:0.###} {P2x:0.###} {P2y:0.###}";
                    }
                }

                foreach (var seg in roadItems)
                {
                    if (!seg.enters.HasValue && !seg.exits.HasValue) continue;
                    var (cx, cy) = HexLayout.OffsetOddQToPixelFlat(seg.col, seg.row, size);
                    (cx, cy) = (cx - minX + margin, cy - minY + margin);

                    Side enters = seg.enters ?? seg.exits!.Value;
                    Side? exits = seg.enters.HasValue ? seg.exits : null;

                    string d = BuildRoadPath(cx, cy, size, enters, exits);

                    // Outer body
                    sb.Append($"<path d=\"{d}\" fill=\"none\" stroke=\"#666\" stroke-width=\"{(size * 0.20).ToString("0.###", inv)}\" " +
                              $"stroke-linecap=\"round\" stroke-linejoin=\"round\" opacity=\"0.85\" />");

                    // Inner highlight
                    sb.Append($"<path d=\"{d}\" fill=\"none\" stroke=\"#c8c8c8\" stroke-width=\"{(size * 0.12).ToString("0.###", inv)}\" " +
                              $"stroke-linecap=\"round\" stroke-linejoin=\"round\" />");
                }
            }

            sb.Append("</svg>");
            return sb.ToString();

            static string IndexToLetters(int index)
            {
                index += 1;
                var s = "";
                while (index > 0)
                {
                    int rem = (index - 1) % 26;
                    s = (char)('A' + rem) + s;
                    index = (index - 1) / 26;
                }
                return s;
            }
        }

        // ===== Hex Lab building footprints =====

        private static void AppendWoodBuilding1(StringBuilder sb, double cx, double cy, double size)
        {
            // Proportions from Hex Lab (HEX_SIZE=30 -> rect=30x20)
            double w = size * 1.0;
            double h = size * (20.0 / 30.0);
            double x = cx - w / 2.0;
            double y = cy - h / 2.0;

            double strokeW = Math.Max(0.4, size * 0.016);
            double lineW = Math.Max(0.3, size * 0.010);

            sb.Append($"<rect x=\"{x:0.###}\" y=\"{y:0.###}\" width=\"{w:0.###}\" height=\"{h:0.###}\" " +
                      $"fill=\"{WoodFill}\" stroke=\"#333\" stroke-width=\"{strokeW:0.###}\"/>");

            double y20 = y + h * 0.20, y50 = y + h * 0.50, y80 = y + h * 0.80;
            double x2 = x + w;
            sb.Append($"<line x1=\"{x:0.###}\" y1=\"{y20:0.###}\" x2=\"{x2:0.###}\" y2=\"{y20:0.###}\" stroke=\"{WoodFillDark}\" stroke-width=\"{lineW:0.###}\"/>");
            sb.Append($"<line x1=\"{x:0.###}\" y1=\"{y50:0.###}\" x2=\"{x2:0.###}\" y2=\"{y50:0.###}\" stroke=\"{WoodFillDark}\" stroke-width=\"{lineW:0.###}\"/>");
            sb.Append($"<line x1=\"{x:0.###}\" y1=\"{y80:0.###}\" x2=\"{x2:0.###}\" y2=\"{y80:0.###}\" stroke=\"{WoodFillDark}\" stroke-width=\"{lineW:0.###}\"/>");

            double fontPx = size * 0.20;
            sb.Append($"<text x=\"{cx:0.###}\" y=\"{(cy + (fontPx * 0.05)):0.###}\" text-anchor=\"middle\" " +
                      $"font-size=\"{fontPx:0.###}\" font-weight=\"bold\" fill=\"#fff\">1</text>");
        }

        private static void AppendStoneBuilding2(StringBuilder sb, double cx, double cy, double size)
        {
            // Proportions from Hex Lab (HEX_SIZE=30 -> rect=30x20, shadow offset 4)
            double w = size * 1.0;
            double h = size * (20.0 / 30.0);
            double x = cx - w / 2.0;
            double y = cy - h / 2.0;

            double strokeW = Math.Max(0.4, size * 0.016);
            double off = size * (4.0 / 30.0);

            // Shadow
            sb.Append($"<rect x=\"{(x + off):0.###}\" y=\"{(y + off):0.###}\" width=\"{w:0.###}\" height=\"{h:0.###}\" " +
                      $"fill=\"{StoneFillDark}\" opacity=\"0.5\"/>");

            // Main block
            sb.Append($"<rect x=\"{x:0.###}\" y=\"{y:0.###}\" width=\"{w:0.###}\" height=\"{h:0.###}\" " +
                      $"fill=\"{StoneFill}\" stroke=\"#333\" stroke-width=\"{strokeW:0.###}\"/>");

            double y30 = y + h * 0.30, y60 = y + h * 0.60, x2 = x + w;
            sb.Append($"<line x1=\"{x:0.###}\" y1=\"{y30:0.###}\" x2=\"{x2:0.###}\" y2=\"{y30:0.###}\" stroke=\"{StoneFillDark}\" stroke-width=\"{strokeW:0.###}\"/>");
            sb.Append($"<line x1=\"{x:0.###}\" y1=\"{y60:0.###}\" x2=\"{x2:0.###}\" y2=\"{y60:0.###}\" stroke=\"{StoneFillDark}\" stroke-width=\"{strokeW:0.###}\"/>");

            // Level badge
            double bw = size * (10.0 / 30.0), bh = size * (8.0 / 30.0);
            double bx = cx - bw / 2.0, by = cy - bh / 2.0;
            double bsw = Math.Max(0.3, size * 0.010);
            sb.Append($"<rect x=\"{bx:0.###}\" y=\"{by:0.###}\" width=\"{bw:0.###}\" height=\"{bh:0.###}\" fill=\"#fff\" stroke=\"#333\" stroke-width=\"{bsw:0.###}\"/>");

            double fontPx = size * 0.20;
            sb.Append($"<text x=\"{cx:0.###}\" y=\"{(cy + (fontPx * 0.10)):0.###}\" text-anchor=\"middle\" font-size=\"{fontPx:0.###}\" font-weight=\"bold\" fill=\"#333\">2</text>");
        }
    }
}
