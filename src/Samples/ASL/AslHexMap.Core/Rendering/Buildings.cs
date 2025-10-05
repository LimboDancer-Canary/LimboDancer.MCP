using System.Text;
using System.Text.Json;
using AslHexMap.Core.Schema;

namespace AslHexMap.Core.Rendering;

/// <summary>Building overlays (Hex Lab footprint style for now).</summary>
public static class Buildings
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
            usage?.Buildings.Add("stone");
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