using System.Text.Json;

namespace AslHexMap.Services;

public sealed class LegendService
{
    public sealed record LegendItem(string Token, string Label);
    public sealed record LegendSection(string Title, List<LegendItem> Items);
    public sealed record LegendModel(string Version, List<LegendSection> Sections);

    private readonly IWebHostEnvironment _env;
    private LegendModel? _cache;

    public LegendService(IWebHostEnvironment env) => _env = env;

    public async Task<LegendModel> LoadAsync(string fileName = "legend.features.v1.json")
    {
        if (_cache is not null) return _cache;

        // Try project root (ContentRoot), then ./Data under it, then the app base directory (bin) + /Data
        var candidates = new[]
        {
            Path.Combine(_env.ContentRootPath, "Data", fileName),
            Path.Combine(_env.ContentRootPath, fileName),
            Path.Combine(AppContext.BaseDirectory, "Data", fileName)
        };

        var path = candidates.FirstOrDefault(File.Exists);
        if (path is null)
        {
            throw new FileNotFoundException(
                $"Legend file '{fileName}' not found. Looked in: {string.Join(" | ", candidates)}");
        }

        using var s = File.OpenRead(path);
        using var doc = await JsonDocument.ParseAsync(s);
        var root = doc.RootElement;

        var sections = new List<LegendSection>();
        foreach (var sec in root.GetProperty("sections").EnumerateArray())
        {
            var title = sec.GetProperty("title").GetString() ?? "";
            var items = new List<LegendItem>();
            foreach (var it in sec.GetProperty("items").EnumerateArray())
                items.Add(new LegendItem(it.GetProperty("token").GetString() ?? "", it.GetProperty("label").GetString() ?? ""));
            sections.Add(new LegendSection(title, items));
        }

        _cache = new LegendModel(root.GetProperty("version").GetString() ?? "1", sections);
        return _cache;
    }


    /// Return label lookup for just the tokens we used.
    public async Task<Dictionary<string, string>> LabelsForAsync(IEnumerable<string> tokens)
    {
        var model = await LoadAsync();
        var want = new HashSet<string>(tokens);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sec in model.Sections)
            foreach (var it in sec.Items)
                if (want.Contains(it.Token)) map[it.Token] = it.Label;
        return map;
    }
}
