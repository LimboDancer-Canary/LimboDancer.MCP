using Microsoft.AspNetCore.Hosting;

namespace AslHexMap.Services
{
    /// <summary>
    /// Responsible for resolving file paths for legend files across multiple search locations.
    /// </summary>
    public class FilePathResolver
    {
        private readonly IWebHostEnvironment _environment;

        /// <summary>
        /// Initializes a new instance of FilePathResolver.
        /// </summary>
        /// <param name="environment">The web host environment for path resolution</param>
        public FilePathResolver(IWebHostEnvironment environment)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Resolves the full path to a legend file by searching in multiple locations.
        /// </summary>
        /// <param name="fileName">The name of the file to find</param>
        /// <param name="additionalSearchPaths">Additional search paths to include</param>
        /// <returns>The full path to the file if found, otherwise null</returns>
        public string? ResolveLegendFile(string fileName, params string[] additionalSearchPaths)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            var searchPaths = BuildSearchPaths(fileName, additionalSearchPaths);
            return searchPaths.FirstOrDefault(File.Exists);
        }

        /// <summary>
        /// Gets all search paths that would be checked for a given file name.
        /// Useful for diagnostics and error messages.
        /// </summary>
        /// <param name="fileName">The name of the file</param>
        /// <param name="additionalSearchPaths">Additional search paths to include</param>
        /// <returns>Array of all search paths that would be checked</returns>
        public string[] GetSearchPaths(string fileName, params string[] additionalSearchPaths)
        {
            return BuildSearchPaths(fileName, additionalSearchPaths);
        }

        /// <summary>
        /// Builds the complete list of search paths for a file.
        /// </summary>
        /// <param name="fileName">The name of the file</param>
        /// <param name="additionalSearchPaths">Additional search paths to include</param>
        /// <returns>Array of search paths</returns>
        private string[] BuildSearchPaths(string fileName, string[] additionalSearchPaths)
        {
            var defaultPaths = new[]
            {
                Path.Combine(_environment.ContentRootPath, "Data", fileName),
                Path.Combine(_environment.ContentRootPath, fileName),
                Path.Combine(AppContext.BaseDirectory, "Data", fileName)
            };

            if (additionalSearchPaths?.Length > 0)
            {
                var additionalFullPaths = additionalSearchPaths
                    .Select(path => Path.IsPathRooted(path) ? Path.Combine(path, fileName) : Path.Combine(_environment.ContentRootPath, path, fileName))
                    .ToArray();

                return defaultPaths.Concat(additionalFullPaths).ToArray();
            }

            return defaultPaths;
        }
    }
}