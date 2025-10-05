using System.Text.Json;
using AslHexMap.Core.Schema;

namespace AslHexMap.Core.Features
{
    public static class FeatureMacroExpander
    {
        public static Dictionary<(int col, int row), List<IOverlayFeature>> BuildFeatureMap(BoardData board)
        {
            var result = new Dictionary<(int col, int row), List<IOverlayFeature>>();
            var templates = board.HexTemplates ?? new Dictionary<string, HexTemplate>(StringComparer.OrdinalIgnoreCase);
            var list = board.Map?.IndividualHexes ?? new List<IndividualHex>();

            foreach (var h in list)
            {
                (int col, int row) key;
                try { key = BoardCoord.Parse(h.HexId); } catch { continue; }

                var features = new List<IOverlayFeature>();

                // new-style typed features
                if (h.Overrides is { } ovArr && ovArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in ovArr.EnumerateArray())
                        if (FeatureRegistry.TryCreate(item, out var f) && f is not null) features.Add(f);
                }

                // legacy macro expansion
                BuildingSpec? bspec = null;
                if (!string.IsNullOrWhiteSpace(h.TemplateId) &&
                    templates.TryGetValue(h.TemplateId!, out var tpl) &&
                    tpl.Building is not null)
                {
                    bspec = tpl.Building;
                }
                if (bspec is null && h.Overrides.HasValue && h.Overrides.Value.ValueKind == JsonValueKind.Object)
                {
                    var obj = h.Overrides.Value;
                    if (obj.TryGetProperty("building", out var b) && b.ValueKind == JsonValueKind.Object)
                    {
                        var spec = new BuildingSpec();
                        if (b.TryGetProperty("type", out var tEl) && tEl.ValueKind == JsonValueKind.String) spec.Type = tEl.GetString();
                        if (b.TryGetProperty("levels", out var lEl) && lEl.ValueKind == JsonValueKind.Number) spec.Levels = lEl.GetInt32();
                        bspec = spec;
                    }
                }

                if (bspec is not null)
                {
                    var mat = (bspec.Type ?? "").Equals("stone", StringComparison.OrdinalIgnoreCase)
                        ? BuildingMaterial.Stone : BuildingMaterial.Wood;

                    features.Add(new BuildingFootprint { Material = mat, Footprint = FootprintKind.Center });
                }

                if (features.Count > 0)
                    result[key] = features;
            }

            return result;
        }
    }
}
