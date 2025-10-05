using AslHexMap.Core.Features;
using AslHexMap.Core.Geometry;
using AslHexMap.Core.Layout;
using AslHexMap.Core.Schema;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;            // <-- added
using System.Text;
using System.Text.Json;

namespace AslHexMap.Core.Rendering
{
    /// <summary>
    /// Facade for board rendering. Public surface stays the same, but the implementation
    /// is split into focused sub-components: Svg, Hexes, Buildings, Roads, and Util.
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
            double cx = width * 0.5;
            double cy = height * 0.5;

            var sb = Svg.Start(width, height, $"0 0 {width} {height}", $"{baseTerrain} hex");
            Svg.Background(sb, "#ffffff");
            Svg.Frame(sb, width, height);

            // defs once
            Svg.Defs(sb);

            // underpaint + pattern
            Hexes.DrawBaseHex(sb, cx, cy, size, baseTerrain, strokeWidth: 1.25, underpaintOpacity: 0.25);

            // center dot + label
            Svg.Circle(sb, cx, cy, 3, "#f00");
            Svg.Text(sb, cx, cy + size + 24, baseTerrain, 14, "#222", anchor: "middle");

            Svg.End(sb);
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
            var shift = Util.MakeShifter(minX, minY, margin);

            var sb = Svg.Start(width, height, $"0 0 {width.ToString("0.##", inv)} {height.ToString("0.##", inv)}",
                               $"{cols}x{rows} hex grid ({baseTerrain})");
            Svg.Background(sb, "#ffffff");
            Svg.Frame(sb, width, height);
            Svg.Defs(sb);

            foreach (var (q, r) in HexLayout.AxialRect(cols, rows))
            {
                var (cx, cy) = HexLayout.AxialToPixelFlat(q, r, size);
                (cx, cy) = shift(cx, cy);
                Hexes.DrawBaseHex(sb, cx, cy, size, baseTerrain, strokeWidth: 1.0, underpaintOpacity: 0.15);
                Svg.Circle(sb, cx, cy, 1.6, "#d33");
            }

            Svg.End(sb);
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
            var shift = Util.MakeShifter(minX, minY, margin);

            var sb = Svg.Start(width, height, $"0 0 {width.ToString("0.##", inv)} {height.ToString("0.##", inv)}",
                               $"{cols}x{rows} offset grid ({baseTerrain})");
            Svg.Background(sb, "#ffffff");
            Svg.Frame(sb, width, height);
            Svg.Defs(sb);

            foreach (var (col, row) in HexLayout.OffsetRect(cols, rows))
            {
                var (cx, cy) = HexLayout.OffsetOddQToPixelFlat(col, row, size);
                (cx, cy) = shift(cx, cy);
                Hexes.DrawBaseHex(sb, cx, cy, size, baseTerrain, strokeWidth: 1.0, underpaintOpacity: 0.15);
                Svg.Circle(sb, cx, cy, 1.6, "#d33");
            }

            Svg.End(sb);
            return sb.ToString();
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
            var inv = CultureInfo.InvariantCulture;

            int cols = data.Map?.Dimensions?.Width ?? 0;
            int rows = data.Map?.Dimensions?.Height ?? 0;

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

            // Templates & defaults (for base terrain resolution)
            var templates = data.HexTemplates ?? new Dictionary<string, HexTemplate>(StringComparer.OrdinalIgnoreCase);
            string defaultTplId = data.Map?.DefaultTemplateId ?? string.Empty;
            templates.TryGetValue(defaultTplId, out var defaultTpl);
            string defaultBase = TerrainStyle.NormalizeBase(defaultTpl?.BaseTerrain ?? "open");

            // Index and roads
            var perHex = Util.IndexPerHex(data);
            var roadItems = Roads.CollectRoads(perHex, data);

            // Feature map (optional new pipeline)
            var featureMap = useFeatureOverlays
                ? AslHexMap.Core.Features.FeatureMacroExpander.BuildFeatureMap(data)
                : new Dictionary<(int col, int row), List<AslHexMap.Core.Features.IOverlayFeature>>();

            // ---------- PASS 1: draw BASES only ----------
            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    perHex.TryGetValue((col, row), out var hex);

                    // Resolve base terrain (template + overrides)
                    string baseTerrain = Hexes.ResolveBaseTerrain(hex, defaultBase, templates);
                    usage?.Bases.Add(TerrainStyle.NormalizeBase(baseTerrain));

                    var (cx, cy) = HexLayout.OffsetOddQToPixelFlat(col, row, size);
                    (cx, cy) = shift(cx, cy);

                    Hexes.DrawBaseHex(sb, cx, cy, size, baseTerrain);
                }
            }

            // ---------- ROADS (under overlays) ----------
            if (showRoads)
                Roads.RenderRoads(sb, roadItems, size, shift);

            // ---------- PASS 2: overlays (buildings etc.) + labels ----------
            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    var (cx, cy) = HexLayout.OffsetOddQToPixelFlat(col, row, size);
                    (cx, cy) = shift(cx, cy);

                    // Overlays
                    if (useFeatureOverlays && featureMap.TryGetValue((col, row), out var feats) && feats is not null)
                    {
                        string? groupId = feats.OfType<AslHexMap.Core.Features.BuildingFootprint>()
                                               .FirstOrDefault()?.GroupId;

                        var ctx = new AslHexMap.Core.Features.FeatureContext
                        {
                            Coord = (col, row),
                            GroupId = groupId
                        };

                        foreach (var f in feats)
                        {
                            // Bridge to existing legend usage tokens for buildings
                            if (f.Token.Equals("building-wood", StringComparison.OrdinalIgnoreCase))
                                usage?.Buildings.Add("wood");
                            else if (f.Token.Equals("building-stone2", StringComparison.OrdinalIgnoreCase))
                                usage?.Buildings.Add("stone2");

                            f.Render(sb, cx, cy, size, ctx);
                        }
                    }

                    // Labels
                    if (showLabels)
                    {
                        var (lx, ly) = HexGeom.LabelAnchorNW(cx, cy, size);
                        var label = $"{Util.IndexToLetters(col)}{row + 1}";
                        Svg.Text(sb, lx, ly + 1, label, 10, "#1a1a1a");
                    }
                }
            }

            Svg.End(sb);
            return sb.ToString();
        }

        public static string RenderLegendIcon(string token, double size = 14)
        {
            var inv = CultureInfo.InvariantCulture;

            // Single-hex canvas extents
            var (minX, minY, maxX, maxY) = HexLayout.OffsetRectExtentsFlat(1, 1, size);
            double margin = 2.0;
            double width = (maxX - minX) + 2 * margin;
            double height = (maxY - minY) + 2 * margin;

            // Simple shifter (same as Util.MakeShifter but inline for this tiny SVG)
            double dx = -minX + margin, dy = -minY + margin;
            (double sx, double sy) Shift(double x, double y) => (x + dx, y + dy);

            var sb = Svg.Start(
                width, height,
                $"0 0 {width.ToString("0.##", inv)} {height.ToString("0.##", inv)}",
                $"legend {token}");

            // No background/frame for legend icons
            Svg.Defs(sb); // patterns for woods/orchard/brush, etc.

            // Hex center
            var (cx0, cy0) = HexLayout.OffsetOddQToPixelFlat(0, 0, size);
            var (cx, cy) = Shift(cx0, cy0);

            // Base terrain (default to open)
            string baseTerrain = "open";
            if (token.StartsWith("base-", StringComparison.OrdinalIgnoreCase))
                baseTerrain = token.Substring(5);

            // Draw the base hex
            Hexes.DrawBaseHex(sb, cx, cy, size, baseTerrain);

            // Overlay features (when token refers to a feature)
            var ctx = new Features.FeatureContext { Coord = (0, 0) };

            if (token.Equals("building-wood", StringComparison.OrdinalIgnoreCase))
            {
                new BuildingFootprint
                {
                    Material = BuildingMaterial.Wood,
                    Footprint = FootprintKind.Center
                }.Render(sb, cx, cy, size, ctx);
            }
            else if (token.Equals("building-stone2", StringComparison.OrdinalIgnoreCase))
            {
                new BuildingFootprint
                {
                    Material = BuildingMaterial.Stone,
                    Footprint = FootprintKind.Center
                }.Render(sb, cx, cy, size, ctx);
            }
            else if (token.Equals("feature-stairwell", StringComparison.OrdinalIgnoreCase))
            {
                new Stairwell { Present = true }.Render(sb, cx, cy, size, ctx);
            }
            else if (token.Equals("feature-rowhouse-edge", StringComparison.OrdinalIgnoreCase))
            {
                // Show a single thick facade on the “east-ish” side to communicate the concept
                new RowhouseEdge { Edges = new[] { Side.NE } }.Render(sb, cx, cy, size, ctx);  // <-- changed
            }

            Svg.End(sb);
            return sb.ToString();
        }

        // ============================
        // Sub-components
        // ============================

        /// <summary>SVG helpers (start/frame/defs/primitives).</summary>
        private static class Svg
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

        /// <summary>Base hex drawing + terrain resolution.</summary>
        private static class Hexes
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

        /// <summary>Building overlays (Hex Lab footprint style for now).</summary>
        private static class Buildings
        {
            private const string WoodFill = "#8b6914";
            private const string WoodFillDark = "#654b0e";
            private const string StoneFill = "#8b7d6b";
            private const string StoneFillDark = "#5c5248";

            public static void RenderBuildingOverlay(
                StringBuilder sb,
                IDictionary<string, HexTemplate> templates,
                IndividualHex? cellHex,
                double cx, double cy, double size,
                LegendRenderer.LegendUsage? usage)
            {
                BuildingSpec? spec = GetBuildingSpec(templates, cellHex);
                if (spec is null) return;

                var t = (spec.Type ?? "").Trim().ToLowerInvariant();
                if (t == "wooden") t = "wood";
                int levels = spec.Levels.GetValueOrDefault(1);

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

            private static BuildingSpec? GetBuildingSpec(IDictionary<string, HexTemplate> templates, IndividualHex? cellHex)
            {
                // From template
                if (cellHex != null &&
                    !string.IsNullOrWhiteSpace(cellHex.TemplateId) &&
                    templates.TryGetValue(cellHex.TemplateId!, out var tplB) &&
                    tplB.Building is not null)
                {
                    return tplB.Building;
                }

                // From per-hex overrides: { "building": { "type": "...", "levels": N } }
                if (cellHex?.Overrides is { } ovEl && ovEl.ValueKind == JsonValueKind.Object &&
                    ovEl.TryGetProperty("building", out var b) && b.ValueKind == JsonValueKind.Object)
                {
                    var spec = new BuildingSpec();
                    if (b.TryGetProperty("type", out var tEl) && tEl.ValueKind == JsonValueKind.String)
                        spec.Type = tEl.GetString();
                    if (b.TryGetProperty("levels", out var lEl) && lEl.ValueKind == JsonValueKind.Number)
                        spec.Levels = lEl.GetInt32();
                    return spec;
                }

                return null;
            }

            // Footprints (ported from Hex Lab proportions)

            private static void AppendWoodBuilding1(StringBuilder sb, double cx, double cy, double size)
            {
                double w = size * 1.0;
                double h = size * (20.0 / 30.0);
                double x = cx - w / 2.0;
                double y = cy - h / 2.0;

                double strokeW = Math.Max(0.4, size * 0.016);
                double lineW = Math.Max(0.3, size * 0.010);

                Svg.Rect(sb, x, y, w, h, WoodFill, "#333", strokeW);
                Svg.Line(sb, x, y + h * 0.20, x + w, y + h * 0.20, WoodFillDark, lineW);
                Svg.Line(sb, x, y + h * 0.50, x + w, y + h * 0.50, WoodFillDark, lineW);
                Svg.Line(sb, x, y + h * 0.80, x + w, y + h * 0.80, WoodFillDark, lineW);

                // Level hint (lab style)
                double fontPx = size * 0.20;
                Svg.Text(sb, cx, cy + (fontPx * 0.05), "1", fontPx, "#fff", anchor: "middle", weight: 700);
            }

            private static void AppendStoneBuilding2(StringBuilder sb, double cx, double cy, double size)
            {
                double w = size * 1.0;
                double h = size * (20.0 / 30.0);
                double x = cx - w / 2.0;
                double y = cy - h / 2.0;

                double strokeW = Math.Max(0.4, size * 0.016);
                double off = size * (4.0 / 30.0);

                // Shadow
                Svg.Rect(sb, x + off, y + off, w, h, StoneFillDark, StoneFillDark, 0, opacity: 0.5);

                // Main block
                Svg.Rect(sb, x, y, w, h, StoneFill, "#333", strokeW);

                // Courses
                Svg.Line(sb, x, y + h * 0.30, x + w, y + h * 0.30, StoneFillDark, strokeW);
                Svg.Line(sb, x, y + h * 0.60, x + w, y + h * 0.60, StoneFillDark, strokeW);

                // Level badge
                double bw = size * (10.0 / 30.0), bh = size * (8.0 / 30.0);
                double bx = cx - bw / 2.0, by = cy - bh / 2.0;
                double bsw = Math.Max(0.3, size * 0.010);
                Svg.Rect(sb, bx, by, bw, bh, "#fff", "#333", bsw);
                double fontPx = size * 0.20;
                Svg.Text(sb, cx, cy + (fontPx * 0.10), "2", fontPx, "#333", anchor: "middle", weight: 700);
            }
        }

        /// <summary>Road collection/parsing and drawing (curved paths).</summary>
        private static class Roads
        {
            public static List<(int col, int row, Side? enters, Side? exits)> CollectRoads(
                Dictionary<(int col, int row), IndividualHex> perHex, BoardData data)
            {
                var result = new List<(int col, int row, Side? enters, Side? exits)>();

                // From per-hex overrides arrays
                foreach (var kvp in perHex)
                {
                    var hex = kvp.Value;
                    if (!hex.Overrides.HasValue || hex.Overrides.Value.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var item in hex.Overrides.Value.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object) continue;
                        if (!item.TryGetProperty("type", out var t) || t.ValueKind != JsonValueKind.String) continue;
                        if (!string.Equals(t.GetString(), "road", StringComparison.OrdinalIgnoreCase)) continue;

                        item.TryGetProperty("enters", out var eIn);
                        item.TryGetProperty("exits", out var eOut);
                        var enters = ParseSide(eIn);
                        var exits = ParseSide(eOut);

                        if (enters is not null || exits is not null)
                            result.Add((kvp.Key.col, kvp.Key.row, enters, exits));
                    }
                }

                // From global traversals (if any)
                foreach (var t in data.AllTraversals)
                {
                    if (!string.Equals(t.Type, "road", StringComparison.OrdinalIgnoreCase)) continue;
                    if (t.Enters is null && t.Exits is null) continue;

                    (int col, int row) k;
                    try { k = BoardCoord.Parse(t.HexId); } catch { continue; }
                    result.Add((k.col, k.row, t.Enters, t.Exits));
                }

                return result;
            }

            public static void RenderRoads(
                StringBuilder sb,
                List<(int col, int row, Side? enters, Side? exits)> items,
                double size,
                Func<double, double, (double, double)> shift)
            {
                var inv = CultureInfo.InvariantCulture;

                foreach (var seg in items)
                {
                    if (!seg.enters.HasValue && !seg.exits.HasValue) continue;

                    var (cx, cy) = HexLayout.OffsetOddQToPixelFlat(seg.col, seg.row, size);
                    (cx, cy) = shift(cx, cy);

                    Side enters = seg.enters ?? seg.exits!.Value;
                    Side? exits = seg.enters.HasValue ? seg.exits : null;

                    string d = BuildRoadPath(cx, cy, size, enters, exits);

                    // Outer body
                    Svg.Path(sb, d, "#666", size * 0.20, opacity: 0.85);
                    // Inner highlight
                    Svg.Path(sb, d, "#c8c8c8", size * 0.12);
                }
            }

            // ---- helpers ----

            private static string BuildRoadPath(double cx, double cy, double size, Side enters, Side? exits)
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

            private static double AngleForSide(Side s) => s switch
            {
                Side.N => 3 * Math.PI / 2,
                Side.NE => 11 * Math.PI / 6,
                Side.SE => Math.PI / 6,
                Side.S => Math.PI / 2,
                Side.SW => 5 * Math.PI / 6,
                Side.NW => 7 * Math.PI / 6,
                _ => 0
            };

            private static (double, double) Polar(double a, double r) => (r * Math.Cos(a), r * Math.Sin(a));
            private static (double, double) Scale((double, double) p, double t) => (p.Item1 * t, p.Item2 * t);

            private static double MidAngle(double a, double b)
            {
                double da = ((b - a + Math.PI * 3) % (Math.PI * 2)) - Math.PI;
                return a + da / 2.0;
            }

            private static Side? ParseSide(JsonElement el)
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
                        _ => (Side?)null
                    };
                }
                return null;
            }
        }

        /// <summary>Small shared helpers.</summary>
        private static class Util
        {
            public static Func<double, double, (double, double)> MakeShifter(double minX, double minY, double margin)
                => (x, y) => (x - minX + margin, y - minY + margin);

            public static string IndexToLetters(int index)
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

            public static Dictionary<(int col, int row), IndividualHex> IndexPerHex(BoardData data)
            {
                var perHex = new Dictionary<(int col, int row), IndividualHex>();
                var list = data.Map?.IndividualHexes;
                if (list is null) return perHex;

                foreach (var h in list)
                {
                    try
                    {
                        var k = BoardCoord.Parse(h.HexId);
                        perHex[(k.col, k.row)] = h;
                    }
                    catch
                    {
                        // ignore parse errors
                    }
                }
                return perHex;
            }
        }
    }
}
