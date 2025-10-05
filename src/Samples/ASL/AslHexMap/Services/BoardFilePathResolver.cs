namespace AslHexMap.Services;

/// <summary>
/// Service responsible for resolving file paths for board-related files.
/// </summary>
public class BoardFilePathResolver
{
    private readonly string _contentRoot;
    private readonly string _defaultDataDirectory;

    /// <summary>
    /// Initializes a new instance of BoardFilePathResolver.
    /// </summary>
    /// <param name="contentRoot">Content root path of the application</param>
    /// <param name="defaultDataDirectory">Default directory name for data files</param>
    public BoardFilePathResolver(string contentRoot, string defaultDataDirectory = "Data")
    {
        _contentRoot = contentRoot ?? throw new ArgumentNullException(nameof(contentRoot));
        _defaultDataDirectory = defaultDataDirectory ?? throw new ArgumentNullException(nameof(defaultDataDirectory));
    }

    /// <summary>
    /// Resolves a file path, handling both absolute and relative paths.
    /// </summary>
    /// <param name="fileName">Name or path of the file to resolve</param>
    /// <returns>Full resolved path to the file</returns>
    /// <exception cref="ArgumentException">Thrown when fileName is null or empty</exception>
    public string ResolvePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

        // If it's already an absolute path, return as-is
        if (Path.IsPathRooted(fileName))
            return fileName;

        // For relative paths, resolve against content root
        return Path.Combine(_contentRoot, fileName);
    }

    /// <summary>
    /// Resolves a file path within the default data directory.
    /// </summary>
    /// <param name="fileName">Name of the file in the data directory</param>
    /// <returns>Full path to the file in the data directory</returns>
    /// <exception cref="ArgumentException">Thrown when fileName is null or empty</exception>
    public string ResolveDataPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

        return Path.Combine(_contentRoot, _defaultDataDirectory, fileName);
    }

    /// <summary>
    /// Gets all possible search paths for a given file name.
    /// Useful for diagnostics and error reporting.
    /// </summary>
    /// <param name="fileName">Name of the file to search for</param>
    /// <returns>Array of possible file paths</returns>
    public string[] GetPossiblePaths(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Array.Empty<string>();

        var paths = new List<string>();

        // If it's absolute, only that path makes sense
        if (Path.IsPathRooted(fileName))
        {
            paths.Add(fileName);
        }
        else
        {
            // Try data directory first
            paths.Add(ResolveDataPath(fileName));
            
            // Then try content root
            paths.Add(Path.Combine(_contentRoot, fileName));
        }

        return paths.ToArray();
    }

    /// <summary>
    /// Finds the first existing file among possible paths.
    /// </summary>
    /// <param name="fileName">Name of the file to find</param>
    /// <returns>Path to the first existing file, or null if none found</returns>
    public string? FindExistingFile(string fileName)
    {
        var possiblePaths = GetPossiblePaths(fileName);
        return possiblePaths.FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// Validates that a resolved path exists and is accessible.
    /// </summary>
    /// <param name="filePath">Path to validate</param>
    /// <returns>True if file exists and is accessible</returns>
    public bool ValidatePath(string filePath)
    {
        try
        {
            return !string.IsNullOrEmpty(filePath) && File.Exists(filePath);
        }
        catch
        {
            return false;
        }
    }
}