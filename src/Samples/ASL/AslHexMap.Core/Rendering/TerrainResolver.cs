using System.Text.Json;
using AslHexMap.Core.Schema;

namespace AslHexMap.Core.Rendering;

/// <summary>
/// Handles terrain resolution logic by separating template resolution, 
/// override processing, and ground cover determination.
/// </summary>
public class TerrainResolver
{
    /// <summary>
    /// Resolves base terrain from hex data, templates, and overrides.
    /// </summary>
    /// <param name="hex">The hex to resolve terrain for</param>
    /// <param name="fallback">Default terrain if no specific terrain is found</param>
    /// <param name="templates">Available templates dictionary</param>
    /// <returns>Resolved base terrain identifier</returns>
    public string ResolveBaseTerrain(IndividualHex? hex, string fallback, IDictionary<string, HexTemplate> templates)
    {
        if (hex == null)
            return fallback;

        // Start with fallback, then apply template, then overrides
        string baseTerrain = fallback;

        // 1. Try to resolve from template
        var templateTerrain = ResolveFromTemplate(hex, templates);
        if (!string.IsNullOrEmpty(templateTerrain))
            baseTerrain = templateTerrain;

        // 2. Apply per-hex overrides (these take precedence)
        var overrideTerrain = ResolveFromOverrides(hex.Overrides);
        if (!string.IsNullOrEmpty(overrideTerrain))
            baseTerrain = overrideTerrain;

        return baseTerrain;
    }

    /// <summary>
    /// Resolves base terrain from a hex template.
    /// </summary>
    /// <param name="hex">The hex with potential template reference</param>
    /// <param name="templates">Available templates dictionary</param>
    /// <returns>Base terrain from template, or empty string if not found</returns>
    public string ResolveFromTemplate(IndividualHex hex, IDictionary<string, HexTemplate> templates)
    {
        if (string.IsNullOrWhiteSpace(hex.TemplateId) || 
            !templates.TryGetValue(hex.TemplateId!, out var template))
        {
            return string.Empty;
        }

        var baseTerrain = TerrainStyle.NormalizeBase(template.BaseTerrain);

        // Check for ground cover in template overlays
        var groundCover = ResolveGroundCover(template.Overlays);
        if (!string.IsNullOrEmpty(groundCover))
            baseTerrain = groundCover;

        return baseTerrain;
    }

    /// <summary>
    /// Resolves base terrain from hex overrides.
    /// </summary>
    /// <param name="overrides">JSON element containing override data</param>
    /// <returns>Base terrain from overrides, or empty string if not found</returns>
    public string ResolveFromOverrides(JsonElement? overrides)
    {
        if (!overrides.HasValue || overrides.Value.ValueKind != JsonValueKind.Object)
            return string.Empty;

        var overrideElement = overrides.Value;

        // Check for explicit base terrain override
        if (TryGetStringProperty(overrideElement, "baseTerrain", out var baseTerrain))
            return TerrainStyle.NormalizeBase(baseTerrain);

        // Check for ground cover override
        if (TryGetStringProperty(overrideElement, "groundCover", out var groundCover))
            return TerrainStyle.NormalizeBase(groundCover);

        // Check for grain field (special case)
        if (HasGrainProperty(overrideElement))
            return "grain";

        return string.Empty;
    }

    /// <summary>
    /// Resolves ground cover from overlays (can be template overlays or other overlay sources).
    /// </summary>
    /// <param name="overlays">Dictionary of overlay data</param>
    /// <returns>Ground cover terrain type, or empty string if not found</returns>
    public string ResolveGroundCover(IDictionary<string, object>? overlays)
    {
        if (overlays == null)
            return string.Empty;

        // Check for explicit ground cover
        if (overlays.TryGetValue("groundCover", out var groundCover) && groundCover is string groundCoverString)
            return TerrainStyle.NormalizeBase(groundCoverString);

        // Check for grain field (special case)
        if (overlays.ContainsKey("grain"))
            return "grain";

        return string.Empty;
    }

    /// <summary>
    /// Resolves ground cover from JSON overlays.
    /// </summary>
    /// <param name="overlays">JSON element containing overlay data</param>
    /// <returns>Ground cover terrain type, or empty string if not found</returns>
    public string ResolveGroundCover(JsonElement? overlays)
    {
        if (!overlays.HasValue || overlays.Value.ValueKind != JsonValueKind.Object)
            return string.Empty;

        var overlayElement = overlays.Value;

        // Check for explicit ground cover
        if (TryGetStringProperty(overlayElement, "groundCover", out var groundCover))
            return TerrainStyle.NormalizeBase(groundCover);

        // Check for grain field (special case)
        if (overlayElement.TryGetProperty("grain", out var _))
            return "grain";

        return string.Empty;
    }

    /// <summary>
    /// Helper method to safely get a string property from a JSON element.
    /// </summary>
    /// <param name="element">The JSON element to search</param>
    /// <param name="propertyName">Name of the property to find</param>
    /// <param name="value">The extracted string value if found</param>
    /// <returns>True if property was found and is a string, false otherwise</returns>
    private static bool TryGetStringProperty(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        
        if (element.TryGetProperty(propertyName, out var property) && 
            property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString() ?? string.Empty;
            return !string.IsNullOrEmpty(value);
        }

        return false;
    }

    /// <summary>
    /// Helper method to check if the element has a grain property (boolean true or any string).
    /// </summary>
    /// <param name="element">The JSON element to check</param>
    /// <returns>True if grain property exists and indicates grain terrain</returns>
    private static bool HasGrainProperty(JsonElement element)
    {
        if (!element.TryGetProperty("grain", out var grainProperty))
            return false;

        return grainProperty.ValueKind == JsonValueKind.True || 
               grainProperty.ValueKind == JsonValueKind.String;
    }
}