using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AslHexMap.Core.Schema;

namespace AslHexMap.Core.Features
{
    public static class FeatureMacroExpander
    {
        public static Dictionary<(int col, int row), List<IOverlayFeature>> BuildFeatureMap(BoardData data)
        {
            var map = new Dictionary<(int col, int row), List<IOverlayFeature>>();
            if (data?.Map is null) return map;

            var templates = data.HexTemplates ?? new Dictionary<string, HexTemplate>(StringComparer.OrdinalIgnoreCase);
            var hexes = data.Map.IndividualHexes;
            if (hexes is null) return map;

            foreach (var h in hexes)
            {
                (int col, int row) key;
                try { key = BoardCoord.Parse(h.HexId); }
                catch { continue; }

                var bucket = new List<IOverlayFeature>();

                // 1) Typed features from per-hex overrides ARRAY (e.g., road, building-footprint, etc.)
                if (h.Overrides.HasValue && h.Overrides.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in h.Overrides.Value.EnumerateArray())
                    {
                        if (FeatureRegistry.TryCreate(el, out var f) && f is not null)
                            bucket.Add(f);
                    }
                }

                // Avoid double-drawing: if a typed BuildingFootprint already exists, skip legacy macro
                bool hasTypedFootprint = bucket.Any(f => f is BuildingFootprint);

                // 2) Legacy macro expansion (template/overrides "building" → BuildingFootprint),
                //    only when no typed footprint was provided.
                if (!hasTypedFootprint)
                {
                    BuildingSpec? bspec = null;

                    // from template
                    if (!string.IsNullOrWhiteSpace(h.TemplateId) &&
                        templates.TryGetValue(h.TemplateId!, out var tpl) &&
                        tpl.Building is not null)
                    {
                        bspec = tpl.Building;
                    }

                    // from overrides OBJECT
                    if (bspec is null && h.Overrides.HasValue && h.Overrides.Value.ValueKind == JsonValueKind.Object)
                    {
                        var obj = h.Overrides.Value;
                        if (obj.TryGetProperty("building", out var b) && b.ValueKind == JsonValueKind.Object)
                        {
                            var spec = new BuildingSpec();
                            if (b.TryGetProperty("type", out var tEl) && tEl.ValueKind == JsonValueKind.String)
                                spec.Type = tEl.GetString();
                            if (b.TryGetProperty("levels", out var lEl) && lEl.ValueKind == JsonValueKind.Number)
                                spec.Levels = lEl.GetInt32();
                            bspec = spec;
                        }
                    }

                    if (bspec is not null)
                    {
                        var mat = (bspec.Type ?? "").Equals("stone", StringComparison.OrdinalIgnoreCase)
                            ? BuildingMaterial.Stone
                            : BuildingMaterial.Wood;

                        bucket.Add(new BuildingFootprint
                        {
                            Material = mat,
                            Footprint = FootprintKind.Center,
                            Levels = bspec.Levels
                        });
                    }
                }

                if (bucket.Count > 0)
                    map[key] = bucket;
            }

            return map;
        }
    }
}
