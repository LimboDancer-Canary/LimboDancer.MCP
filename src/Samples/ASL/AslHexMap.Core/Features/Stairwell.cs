using System.Text;
using System.Text.Json;

namespace AslHexMap.Core.Features;

public sealed class Stairwell : IOverlayFeature
{
    public bool Present { get; init; }
    public string Token => "feature-stairwell";

    public void Render(StringBuilder sb, double cx, double cy, double size, FeatureContext ctx)
    {
        if (!Present) return;

        // If a building level was provided for this hex/group, render a circular badge
        // at the stairwell location (center for now) and draw the level inside it.
        if (ctx.StairwellBadgeLevel is int lvl && lvl >= 1)
        {
            lvl = Math.Clamp(lvl, 1, 9);
            double r = size * 0.18;
            double sw = Math.Max(0.35, size * 0.012);

            // white circle marker
            sb.Append($"<circle cx=\"{cx:0.###}\" cy=\"{cy:0.###}\" r=\"{r:0.###}\" fill=\"#fff\" stroke=\"#333\" stroke-width=\"{sw:0.###}\"/>");

            // level digit
            double fontPx = size * 0.20;
            sb.Append($"<text x=\"{cx:0.###}\" y=\"{(cy + fontPx * 0.10):0.###}\" text-anchor=\"middle\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"{fontPx:0.###}\" font-weight=\"700\" fill=\"#333\">{lvl}</text>");
            return;
        }

        // Fallback: simple small “S” circle if no level was provided by the building
        {
            double r = size * 0.16;
            double sw = Math.Max(0.35, size * 0.012);
            sb.Append($"<circle cx=\"{cx:0.###}\" cy=\"{cy:0.###}\" r=\"{r:0.###}\" fill=\"#fff\" stroke=\"#333\" stroke-width=\"{sw:0.###}\"/>");
            double fontPx = size * 0.18;
            sb.Append($"<text x=\"{cx:0.###}\" y=\"{(cy + fontPx * 0.10):0.###}\" text-anchor=\"middle\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"{fontPx:0.###}\" font-weight=\"700\" fill=\"#333\">S</text>");
        }
    }

    public static Stairwell FromJson(JsonElement el)
    {
        // Default: present = true (so a bare "type":"stairwell" works)
        bool present = true;

        if (el.ValueKind == JsonValueKind.Object &&
            el.TryGetProperty("present", out var p))
        {
            if (p.ValueKind == JsonValueKind.True || p.ValueKind == JsonValueKind.False)
                present = p.GetBoolean();
            else if (p.ValueKind == JsonValueKind.String &&
                     bool.TryParse(p.GetString(), out var b))
                present = b;
        }

        return new Stairwell { Present = present };
    }

}