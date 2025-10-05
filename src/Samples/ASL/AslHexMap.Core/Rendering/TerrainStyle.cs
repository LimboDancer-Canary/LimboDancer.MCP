using AslHexMap.Core.Schema;

namespace AslHexMap.Core.Rendering
{
    /// <summary>
    /// Presentation tokens & helpers for ASL terrain rendering.
    /// - Distinguishes BASE terrains (ground layer) vs overlays (buildings).
    /// - Centralizes base underpaint colors and pattern-id selection.
    /// - Handles common synonyms from JSON (e.g., "openGround" → "open").
    /// </summary>
    public static class TerrainStyle
    {
        /////////////////////////////////////////
        // Canonical terrain ids (ground layer) //
        /////////////////////////////////////////

        // Ground/base terrains we currently support.
        public static readonly HashSet<string> BaseTerrains = new(StringComparer.OrdinalIgnoreCase)
        {
            "open", "woods", "orchard", "brush", "grain", "marsh", "sand", "scrub"
        };

        // Overlay categories (drawn above the base).
        public static class OverlayTypes
        {
            // Building “materials” supported by our defs.
            public static readonly HashSet<string> Building = new(StringComparer.OrdinalIgnoreCase)
            {
                "stone","stone1","stone2","wood"
            };
        }

        /////////////////////////////////////////
        // Synonyms / normalization            //
        /////////////////////////////////////////

        // Map common inputs to canonical ids (after NormalizeKey).
        private static readonly Dictionary<string, string> Synonyms = new(StringComparer.OrdinalIgnoreCase)
        {
            // Open Ground variants
            ["openground"] = "open",
            ["open-ground"] = "open",
            ["open_ground"] = "open",
            ["og"] = "open",

            // Add more synonyms here if needed:
            // ["lightwoods"] = "woods",
            // ["orchards"]   = "orchard",
        };

        private static string NormalizeKey(string s)
            => s.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();

        /// <summary>
        /// Normalize any template/base input to a canonical base terrain id. Defaults to "open".
        /// </summary>
        public static string NormalizeBase(string? baseTerrain)
        {
            if (string.IsNullOrWhiteSpace(baseTerrain)) return "open";
            var keyNorm = NormalizeKey(baseTerrain.Trim());

            if (Synonyms.TryGetValue(keyNorm, out var mapped))
                return mapped;

            // If already a canonical id (case-insensitive), return it lowercased.
            return BaseTerrains.Contains(keyNorm) ? keyNorm : "open";
        }

        /////////////////////////////////////////
        // Pattern id selection                //
        /////////////////////////////////////////

        /// <summary>
        /// Return the pattern id to use for a base terrain (for fill="url(#id)").
        /// </summary>
        public static string PatternIdForBase(string? baseTerrain)
        {
            return NormalizeBase(baseTerrain) switch
            {
                "open" => "openPattern",
                "woods" => "woodsPattern",
                "orchard" => "orchardPattern",
                "brush" => "brushPattern",
                "grain" => "grainPattern",
                "marsh" => "marshPattern",
                "sand" => "sandPattern",
                "scrub" => "scrubPattern",
                _ => "openPattern"
            };
        }

        /// <summary>
        /// Return the pattern id for a building overlay given its material/type.
        /// Expected values: stone1, stone2, wood. Returns null if unknown.
        /// </summary>
        public static string? PatternIdForBuilding(string? buildingMaterial)
        {
            if (string.IsNullOrWhiteSpace(buildingMaterial)) return null;
            var key = buildingMaterial.Trim().ToLowerInvariant();
            return OverlayTypes.Building.Contains(key) ? key : null; // ids match defs: "stone1"/"stone2"/"wood"
        }

        public static string? PatternIdForBuilding(BuildingSpec? spec)
        {
            if (spec is null) return null;
            var t = (spec.Type ?? "").Trim().ToLowerInvariant();

            // normalize "wooden" → "wood"
            if (t == "wooden") t = "wood";

            if (t == "wood") return "wood";

            if (t == "stone")
            {
                var levels = spec.Levels.GetValueOrDefault(1);
                return levels >= 2 ? "stone2" : "stone1";
            }

            // future: "rubble", "factory", etc.
            return null;
        }


    /////////////////////////////////////////
    // Colors (underpaint/fallback fills)  //
    /////////////////////////////////////////

    public static class Colors
        {
            public const string OpenBase = "#90a955"; // olive
            public const string WoodsBase = "#6f8f3d";
            public const string OrchardBase = "#7da35f";
            public const string BrushBase = "#8aa15f";
            public const string GrainBase = "#d5c07a";
            public const string MarshBase = "#7aa08a";
            public const string SandBase = "#d8c9a6";
            public const string ScrubBase = "#9aa76f";

            /// <summary>Fallback/underpaint color for a base terrain.</summary>
            public static string ForBase(string baseTerrain) => TerrainStyle.NormalizeBase(baseTerrain) switch
            {
                "open" => OpenBase,
                "woods" => WoodsBase,
                "orchard" => OrchardBase,
                "brush" => BrushBase,
                "grain" => GrainBase,
                "marsh" => MarshBase,
                "sand" => SandBase,
                "scrub" => ScrubBase,
                _ => OpenBase
            };
        }

        /////////////////////////////////////////
        // Legend usage tracking (optional)    //
        /////////////////////////////////////////

        /// <summary>
        /// Track which base/overlay patterns were used, to build a legend later.
        /// Mutates the provided sets in-place.
        /// </summary>
        public static void TrackUsage(
            (ISet<string> Bases, ISet<string> Buildings) used,
            string? baseTerrain,
            string? buildingMaterial)
        {
            used.Bases.Add(NormalizeBase(baseTerrain));
            var bpid = PatternIdForBuilding(buildingMaterial);
            if (bpid is not null) used.Buildings.Add(bpid);
        }
    }
}
