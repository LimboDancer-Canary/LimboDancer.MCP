using System.Globalization;
using System.Text;
using System.Text.Json;
using AslHexMap.Core.Layout;
using AslHexMap.Core.Schema;

namespace AslHexMap.Core.Rendering;

/// <summary>Road collection/parsing and drawing (curved paths).</summary>
public static class Roads
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