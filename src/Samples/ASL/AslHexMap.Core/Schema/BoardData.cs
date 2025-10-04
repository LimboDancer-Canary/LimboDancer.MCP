using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AslHexMap.Core.Schema
{
    /// <summary>Root document for an ASL board JSON.</summary>
    public class BoardData
    {
        [JsonPropertyName("map")]
        public MapSpec Map { get; set; } = new();

        /// <summary>
        /// In the sample, hexTemplates is an OBJECT keyed by template id.
        /// e.g. { "open": { id:"open", baseTerrain:"openGround" }, ... }
        /// </summary>
        [JsonPropertyName("hexTemplates")]
        public Dictionary<string, HexTemplate> HexTemplates { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        // Roads/traversals may appear at the root or under "map" depending on generator.
        [JsonPropertyName("traversals")] public List<LinearTraversal>? Traversals { get; set; }
        [JsonPropertyName("roads")] public List<LinearTraversal>? Roads { get; set; }

        /// <summary>Unified enumeration over any traversal lists present.</summary>
        [JsonIgnore]
        public IEnumerable<LinearTraversal> AllTraversals
        {
            get
            {
                if (Traversals != null) foreach (var t in Traversals) yield return t;
                if (Roads != null) foreach (var t in Roads) yield return t;
                if (Map?.Traversals != null) foreach (var t in Map.Traversals) yield return t;
                if (Map?.Roads != null) foreach (var t in Map.Roads) yield return t;
            }
        }
    }
}