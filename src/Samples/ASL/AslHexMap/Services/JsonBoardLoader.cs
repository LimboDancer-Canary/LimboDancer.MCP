using System.Text.Json;
using AslHexMap.Core.Schema;
using Microsoft.AspNetCore.Hosting;

namespace AslHexMap.Services
{
    public sealed class JsonBoardLoader
    {
        private readonly IWebHostEnvironment _env;
        private readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public JsonBoardLoader(IWebHostEnvironment env) => _env = env;

        public async Task<BoardData?> LoadSampleAsync(string fileName = "asl_board_features_demo.json")
        {
            var path = Path.Combine(_env.ContentRootPath, "Data", fileName);
            if (!File.Exists(path)) return null;
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<BoardData>(fs, _opts);
        }

        public async Task<BoardData?> LoadAsync(string fullOrRelativePath)
        {
            var path = Path.IsPathRooted(fullOrRelativePath)
                ? fullOrRelativePath
                : Path.Combine(_env.ContentRootPath, fullOrRelativePath);
            if (!File.Exists(path)) return null;
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<BoardData>(fs, _opts);
        }
    }
}