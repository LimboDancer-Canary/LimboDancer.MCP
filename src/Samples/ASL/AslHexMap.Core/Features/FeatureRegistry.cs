using System.Text.Json;

namespace AslHexMap.Core.Features;

public static class FeatureRegistry
{
    // type -> factory(JsonElement spec) -> IOverlayFeature
    private static readonly Dictionary<string, Func<JsonElement, IOverlayFeature>> _map =
        new(StringComparer.OrdinalIgnoreCase);

    static FeatureRegistry()
    {
        Register("building-footprint", json => BuildingFootprint.FromJson(json));
        Register("stairwell", json => Stairwell.FromJson(json));
        Register("rowhouse-edge", json => RowhouseEdge.FromJson(json));
    }

    public static void Register(string type, Func<JsonElement, IOverlayFeature> factory) => _map[type] = factory;

    public static bool TryCreate(JsonElement el, out IOverlayFeature? feature)
    {
        feature = null;
        if (!el.TryGetProperty("type", out var t) || t.ValueKind != JsonValueKind.String) return false;
        if (_map.TryGetValue(t.GetString()!, out var f))
        {
            feature = f(el);
            return true;
        }
        return false;
    }
    
}