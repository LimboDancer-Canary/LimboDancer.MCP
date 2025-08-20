using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.MCP.Ontology.Runtime;

namespace LimboDancer.MCP.Ontology.JsonLd
{
    /// <summary>
    /// Builds a JSON-LD @context object based on the entities and properties available in a scoped channel.
    /// </summary>
    public static class JsonLdContextBuilder
    {
        public static JsonObject BuildContext(TenantScope scope, string baseNamespace, IReadOnlyDictionary<string, string>? extraPrefixes = null)
        {
            if (!baseNamespace.EndsWith("#") && !baseNamespace.EndsWith("/")) baseNamespace += "#";

            var ctx = new JsonObject
            {
                ["@vocab"] = baseNamespace
            };

            // Prefixes
            var prefixes = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ldm"] = baseNamespace,
                ["xsd"] = "http://www.w3.org/2001/XMLSchema#",
                ["rdf"] = "http://www.w3.org/1999/02/22-rdf-syntax-ns#",
                ["rdfs"] = "http://www.w3.org/2000/01/rdf-schema#",
                ["owl"] = "http://www.w3.org/2002/07/owl#"
            };

            if (extraPrefixes is not null)
            {
                foreach (var kv in extraPrefixes)
                    prefixes[kv.Key] = kv.Value;
            }

            foreach (var kv in prefixes)
            {
                ctx[kv.Key] = kv.Value;
                Curie.RegisterPrefix(kv.Key, kv.Value);
            }

            // Note: Terms for entities/properties are implicitly derived from @vocab; no explicit per-term mapping is required here.
            return ctx;
        }

        public static string BuildContextJson(TenantScope scope, string baseNamespace, IReadOnlyDictionary<string, string>? extraPrefixes = null)
        {
            var ctx = BuildContext(scope, baseNamespace, extraPrefixes);
            var root = new JsonObject { ["@context"] = ctx };
            return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }
    }
}