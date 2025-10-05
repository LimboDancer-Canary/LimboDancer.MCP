using AslHexMap.Core.Schema;
using Microsoft.AspNetCore.Hosting;

namespace AslHexMap.Services
{
    /// <summary>
    /// Service responsible for loading board data from JSON files.
    /// </summary>
    public sealed class JsonBoardLoader
    {
        private readonly JsonFileLoader<BoardData> _jsonLoader;
        private readonly BoardFilePathResolver _pathResolver;

        /// <summary>
        /// Initializes a new instance of JsonBoardLoader.
        /// </summary>
        /// <param name="env">Web host environment for path resolution</param>
        public JsonBoardLoader(IWebHostEnvironment env)
        {
            if (env == null)
                throw new ArgumentNullException(nameof(env));

            _jsonLoader = new JsonFileLoader<BoardData>();
            _pathResolver = new BoardFilePathResolver(env.ContentRootPath);
        }

        /// <summary>
        /// Initializes a new instance of JsonBoardLoader with custom dependencies.
        /// </summary>
        /// <param name="jsonLoader">JSON file loader for BoardData</param>
        /// <param name="pathResolver">Path resolver for board files</param>
        public JsonBoardLoader(JsonFileLoader<BoardData> jsonLoader, BoardFilePathResolver pathResolver)
        {
            _jsonLoader = jsonLoader ?? throw new ArgumentNullException(nameof(jsonLoader));
            _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
        }

        /// <summary>
        /// Loads a board data file from the default data directory.
        /// </summary>
        /// <param name="fileName">Name of the file in the data directory</param>
        /// <returns>Loaded board data or null if file doesn't exist</returns>
        /// <exception cref="InvalidOperationException">Thrown when file loading fails</exception>
        public async Task<BoardData?> LoadSampleAsync(string fileName = "asl_board_features_demo.json")
        {
            try
            {
                var path = _pathResolver.ResolveDataPath(fileName);
                return await _jsonLoader.LoadAsync(path);
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                throw new InvalidOperationException($"Failed to load sample board '{fileName}'", ex);
            }
        }

        /// <summary>
        /// Loads a board data file from a full or relative path.
        /// </summary>
        /// <param name="fullOrRelativePath">Full path or relative path to the board file</param>
        /// <returns>Loaded board data or null if file doesn't exist</returns>
        /// <exception cref="InvalidOperationException">Thrown when file loading fails</exception>
        public async Task<BoardData?> LoadAsync(string fullOrRelativePath)
        {
            try
            {
                var path = _pathResolver.ResolvePath(fullOrRelativePath);
                return await _jsonLoader.LoadAsync(path);
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                throw new InvalidOperationException($"Failed to load board from '{fullOrRelativePath}'", ex);
            }
        }

        /// <summary>
        /// Attempts to find and load a board file by checking multiple possible locations.
        /// </summary>
        /// <param name="fileName">Name of the file to find and load</param>
        /// <returns>Loaded board data or null if file not found in any location</returns>
        public async Task<BoardData?> FindAndLoadAsync(string fileName)
        {
            var existingPath = _pathResolver.FindExistingFile(fileName);
            
            if (existingPath == null)
                return null;

            return await _jsonLoader.LoadAsync(existingPath);
        }

        /// <summary>
        /// Gets diagnostic information about possible file locations for a given filename.
        /// </summary>
        /// <param name="fileName">Name of the file to get diagnostics for</param>
        /// <returns>Diagnostic information including possible paths and their existence status</returns>
        public BoardLoadDiagnostics GetDiagnostics(string fileName)
        {
            var possiblePaths = _pathResolver.GetPossiblePaths(fileName);
            var pathStatuses = possiblePaths.ToDictionary(
                path => path, 
                path => _pathResolver.ValidatePath(path)
            );

            return new BoardLoadDiagnostics(fileName, pathStatuses);
        }
    }

    /// <summary>
    /// Diagnostic information for board file loading.
    /// </summary>
    /// <param name="FileName">The filename that was searched for</param>
    /// <param name="PathStatuses">Dictionary of possible paths and whether they exist</param>
    public record BoardLoadDiagnostics(
        string FileName, 
        Dictionary<string, bool> PathStatuses)
    {
        /// <summary>
        /// Gets whether any of the possible paths exist.
        /// </summary>
        public bool AnyPathExists => PathStatuses.Values.Any(exists => exists);

        /// <summary>
        /// Gets the first existing path, or null if none exist.
        /// </summary>
        public string? FirstExistingPath => PathStatuses
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .FirstOrDefault();

        /// <summary>
        /// Gets all existing paths.
        /// </summary>
        public IEnumerable<string> ExistingPaths => PathStatuses
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key);

        /// <summary>
        /// Gets all non-existing paths.
        /// </summary>
        public IEnumerable<string> NonExistingPaths => PathStatuses
            .Where(kvp => !kvp.Value)
            .Select(kvp => kvp.Key);
    }
}