using System.Text.Json;

namespace AslHexMap.Core.Features;

/// <summary>
/// Interface for registering and creating overlay features from JSON specifications.
/// </summary>
public interface IFeatureRegistry
{
    /// <summary>
    /// Registers a factory function for creating features of a specific type.
    /// </summary>
    /// <typeparam name="T">Type of feature to register</typeparam>
    /// <param name="type">String identifier for the feature type</param>
    /// <param name="factory">Factory function that creates the feature from JSON</param>
    void Register<T>(string type, Func<JsonElement, T> factory) where T : IOverlayFeature;
    
    /// <summary>
    /// Attempts to create a feature from a JSON element.
    /// </summary>
    /// <param name="element">JSON element containing feature specification</param>
    /// <param name="feature">Created feature instance, or null if creation failed</param>
    /// <returns>True if feature was successfully created</returns>
    bool TryCreate(JsonElement element, out IOverlayFeature? feature);
}

/// <summary>
/// Registry for overlay feature factories, allowing dynamic feature creation from JSON.
/// </summary>
public class FeatureRegistry : IFeatureRegistry
{
    // type -> factory(JsonElement spec) -> IOverlayFeature
    private readonly Dictionary<string, Func<JsonElement, IOverlayFeature>> _factories;

    /// <summary>
    /// Initializes a new instance of FeatureRegistry with default feature registrations.
    /// </summary>
    public FeatureRegistry()
    {
        _factories = new Dictionary<string, Func<JsonElement, IOverlayFeature>>(StringComparer.OrdinalIgnoreCase);
        RegisterDefaultFeatures();
    }

    /// <summary>
    /// Initializes a new instance of FeatureRegistry without default registrations.
    /// Useful for testing or custom configurations.
    /// </summary>
    /// <param name="registerDefaults">Whether to register default features</param>
    public FeatureRegistry(bool registerDefaults)
    {
        _factories = new Dictionary<string, Func<JsonElement, IOverlayFeature>>(StringComparer.OrdinalIgnoreCase);
        
        if (registerDefaults)
        {
            RegisterDefaultFeatures();
        }
    }

    /// <inheritdoc />
    public void Register<T>(string type, Func<JsonElement, T> factory) where T : IOverlayFeature
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Feature type cannot be null or empty", nameof(type));
        
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        _factories[type] = element => factory(element);
    }

    /// <inheritdoc />
    public bool TryCreate(JsonElement element, out IOverlayFeature? feature)
    {
        feature = null;
        
        if (!element.TryGetProperty("type", out var typeProperty) || 
            typeProperty.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var type = typeProperty.GetString();
        if (string.IsNullOrEmpty(type))
            return false;

        if (_factories.TryGetValue(type, out var factory))
        {
            try
            {
                feature = factory(element);
                return feature != null;
            }
            catch
            {
                // Factory threw an exception, treat as failed creation
                feature = null;
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Registers the default set of overlay features.
    /// </summary>
    private void RegisterDefaultFeatures()
    {
        Register("building-footprint", BuildingFootprint.FromJson);
        Register("stairwell", Stairwell.FromJson);
        Register("rowhouse-edge", RowhouseEdge.FromJson);
    }
}