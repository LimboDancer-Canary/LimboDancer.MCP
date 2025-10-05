using System.Text.Json;

namespace AslHexMap.Services;

/// <summary>
/// Generic service for loading and deserializing JSON files of any type.
/// </summary>
/// <typeparam name="T">The type to deserialize the JSON content into</typeparam>
public class JsonFileLoader<T> where T : class
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of JsonFileLoader with default JSON options.
    /// </summary>
    public JsonFileLoader() : this(CreateDefaultOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of JsonFileLoader with custom JSON options.
    /// </summary>
    /// <param name="options">JSON serialization options to use</param>
    public JsonFileLoader(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Loads and deserializes a JSON file from the specified path.
    /// </summary>
    /// <param name="path">Full path to the JSON file</param>
    /// <returns>Deserialized object or null if file doesn't exist</returns>
    /// <exception cref="ArgumentException">Thrown when path is null or empty</exception>
    /// <exception cref="JsonException">Thrown when JSON is malformed</exception>
    public async Task<T?> LoadAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (!File.Exists(path))
            return null;

        try
        {
            await using var fileStream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(fileStream, _options);
        }
        catch (JsonException)
        {
            // Re-throw JSON exceptions as they provide useful information
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load JSON file from '{path}'", ex);
        }
    }

    /// <summary>
    /// Loads and deserializes JSON content from a stream.
    /// </summary>
    /// <param name="stream">Stream containing JSON content</param>
    /// <returns>Deserialized object</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null</exception>
    /// <exception cref="JsonException">Thrown when JSON is malformed</exception>
    public async Task<T?> LoadFromStreamAsync(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        return await JsonSerializer.DeserializeAsync<T>(stream, _options);
    }

    /// <summary>
    /// Creates default JSON serialization options optimized for configuration files.
    /// </summary>
    /// <returns>Configured JsonSerializerOptions</returns>
    private static JsonSerializerOptions CreateDefaultOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}