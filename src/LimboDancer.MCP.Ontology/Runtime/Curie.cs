using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace LimboDancer.MCP.Ontology.Runtime
{
    /// <summary>
    /// Utility to expand/compact CURIEs to absolute URIs and vice versa.
    /// </summary>
    public static class Curie
    {
        private static readonly ConcurrentDictionary<string, string> PrefixMap = new(StringComparer.Ordinal);

        static Curie()
        {
            // Sensible defaults that callers can override at runtime.
            // ldm is the default platform ontology prefix.
            PrefixMap["ldm"] = "https://ontology.limbodancer.mcp/"; // override per channel via JsonLdContextBuilder if desired
            PrefixMap["rdf"] = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
            PrefixMap["rdfs"] = "http://www.w3.org/2000/01/rdf-schema#";
            PrefixMap["xsd"] = "http://www.w3.org/2001/XMLSchema#";
            PrefixMap["owl"] = "http://www.w3.org/2002/07/owl#";
        }

        public static void RegisterPrefix(string prefix, string baseUri)
        {
            if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("Prefix is required.", nameof(prefix));
            if (string.IsNullOrWhiteSpace(baseUri)) throw new ArgumentException("Base URI is required.", nameof(baseUri));
            if (!baseUri.EndsWith("#") && !baseUri.EndsWith("/")) baseUri += "#";
            PrefixMap[prefix] = baseUri;
        }

        public static IReadOnlyDictionary<string, string> GetPrefixes() => PrefixMap;

        public static string Expand(string curieOrUri)
        {
            if (string.IsNullOrWhiteSpace(curieOrUri)) throw new ArgumentException("Value is required.", nameof(curieOrUri));
            if (Uri.IsWellFormedUriString(curieOrUri, UriKind.Absolute)) return curieOrUri;

            var idx = curieOrUri.IndexOf(':');
            if (idx <= 0) throw new ArgumentException($"Not a CURIE or absolute URI: {curieOrUri}");
            var prefix = curieOrUri[..idx];
            var local = curieOrUri[(idx + 1)..];
            if (!PrefixMap.TryGetValue(prefix, out var baseUri))
                throw new ArgumentException($"Unknown CURIE prefix '{prefix}' in '{curieOrUri}'. Register with Curie.RegisterPrefix().");
            return baseUri + local;
        }

        public static string Compact(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri)) throw new ArgumentException("URI is required.", nameof(uri));
            foreach (var kvp in PrefixMap)
            {
                if (uri.StartsWith(kvp.Value, StringComparison.Ordinal))
                {
                    return $"{kvp.Key}:{uri.AsSpan(kvp.Value.Length)}";
                }
            }
            return uri; // No known prefix; return original
        }
    }
}