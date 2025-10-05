using System.Text.Json;

namespace AslHexMap.Services;

public sealed class LegendService
{
    public sealed record LegendItem(string Token, string Label);
    public sealed record LegendSection(string Title, List<LegendItem> Items);
    public sealed record LegendModel(string Version, List<LegendSection> Sections);

    private readonly FilePathResolver _pathResolver;
    private readonly LegendJsonParser _jsonParser;
    private LegendModel? _cache;

    /// <summary>
    /// Initializes a new instance of LegendService with injected dependencies.
    /// </summary>
    /// <param name="pathResolver">File path resolver for locating legend files</param>
    /// <param name="jsonParser">JSON parser for processing legend data</param>
    public LegendService(FilePathResolver pathResolver, LegendJsonParser jsonParser)
    {
        _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
        _jsonParser = jsonParser ?? throw new ArgumentNullException(nameof(jsonParser));
    }

    /// <summary>
    /// Constructor for backward compatibility with IWebHostEnvironment injection.
    /// </summary>
    /// <param name="env">Web host environment</param>
    public LegendService(IWebHostEnvironment env)
        : this(new FilePathResolver(env), new LegendJsonParser())
    {
    }

    /// <summary>
    /// Loads the legend model from the specified file, with caching.
    /// </summary>
    /// <param name="fileName">Name of the legend file to load</param>
    /// <returns>The loaded legend model</returns>
    /// <exception cref="FileNotFoundException">Thrown when the legend file cannot be found</exception>
    public async Task<LegendModel> LoadAsync(string fileName = "legend.features.v1.json")
    {
        if (_cache is not null)
            return _cache;

        var filePath = ResolveLegendFilePath(fileName);
        _cache = await LoadFromFileAsync(filePath);

        return _cache;
    }

    /// <summary>
    /// Returns label lookup for the specified tokens.
    /// </summary>
    /// <param name="tokens">Collection of tokens to look up</param>
    /// <returns>Dictionary mapping tokens to their labels</returns>
    public async Task<Dictionary<string, string>> LabelsForAsync(IEnumerable<string> tokens)
    {
        var model = await LoadAsync();
        var wantedTokens = new HashSet<string>(tokens);
        var labelMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var section in model.Sections)
        {
            foreach (var item in section.Items)
            {
                if (wantedTokens.Contains(item.Token))
                {
                    labelMap[item.Token] = item.Label;
                }
            }
        }

        return labelMap;
    }

    /// <summary>
    /// Clears the cached legend model, forcing a reload on next access.
    /// </summary>
    public void ClearCache()
    {
        _cache = null;
    }

    /// <summary>
    /// Loads legend model directly from a file path without caching.
    /// Useful for testing or loading different legend files.
    /// </summary>
    /// <param name="filePath">Full path to the legend file</param>
    /// <returns>The loaded legend model</returns>
    public async Task<LegendModel> LoadFromFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Legend file not found: {filePath}");

        using var stream = File.OpenRead(filePath);
        return await _jsonParser.ParseFromStreamAsync(stream);
    }

    /// <summary>
    /// Resolves the file path for a legend file using the path resolver.
    /// </summary>
    /// <param name="fileName">Name of the file to resolve</param>
    /// <returns>Full path to the file</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file cannot be found</exception>
    private string ResolveLegendFilePath(string fileName)
    {
        var filePath = _pathResolver.ResolveLegendFile(fileName);

        if (filePath is null)
        {
            var searchPaths = _pathResolver.GetSearchPaths(fileName);
            throw new FileNotFoundException(
                $"Legend file '{fileName}' not found. Looked in: {string.Join(" | ", searchPaths)}");
        }

        return filePath;
    }
}
