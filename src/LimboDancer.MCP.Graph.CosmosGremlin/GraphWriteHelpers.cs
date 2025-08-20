using System;
using System.Collections.Generic;

namespace LimboDancer.MCP.Graph.CosmosGremlin
{
    public static class GraphWriteHelpers
    {
        public const string TenantPropertyName = "tenant_id";
        public const string IdPropertyName = "id";
        private const string VertexIdSeparator = "::";

        public static string ToVertexId(string tenantId, string localId)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(localId)) throw new ArgumentException("LocalId is required.", nameof(localId));
            return $"{tenantId}{VertexIdSeparator}{localId}";
        }

        public static string GetLocalId(string tenantAwareVertexId, string expectedTenantId)
        {
            if (!TryParseTenantFromVertexId(tenantAwareVertexId, out var tenant, out var local))
            {
                throw new ArgumentException("Invalid vertex id format.", nameof(tenantAwareVertexId));
            }

            if (!string.Equals(tenant, expectedTenantId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Vertex id does not belong to the expected tenant.");
            }

            return local!;
        }

        public static bool TryParseTenantFromVertexId(string vertexId, out string tenantId, out string localId)
        {
            tenantId = "";
            localId = "";
            if (string.IsNullOrWhiteSpace(vertexId)) return false;
            var idx = vertexId.IndexOf(VertexIdSeparator, StringComparison.Ordinal);
            if (idx <= 0) return false;
            tenantId = vertexId.Substring(0, idx);
            localId = vertexId[(idx + VertexIdSeparator.Length)..];
            return !string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(localId);
        }

        public static void EnsureTenantMatches(string tenantId, string vertexId)
        {
            if (!TryParseTenantFromVertexId(vertexId, out var t, out _))
            {
                throw new ArgumentException("Vertex id is malformed.", nameof(vertexId));
            }

            if (!string.Equals(t, tenantId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Cross-tenant access detected.");
            }
        }

        public static IDictionary<string, object> WithTenantProperty(IDictionary<string, object>? properties, string tenantId)
        {
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    if (string.Equals(kvp.Key, IdPropertyName, StringComparison.Ordinal)) continue;
                    if (string.Equals(kvp.Key, TenantPropertyName, StringComparison.Ordinal)) continue;
                    result[kvp.Key] = kvp.Value;
                }
            }

            result[TenantPropertyName] = tenantId;
            return result;
        }

        public static void ValidateLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Label is required.", nameof(label));
            }
        }

        public static void ValidatePropertyKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Property key is required.", nameof(key));
            if (string.Equals(key, IdPropertyName, StringComparison.Ordinal) ||
                string.Equals(key, TenantPropertyName, StringComparison.Ordinal))
            {
                throw new ArgumentException($"Property key '{key}' is reserved.");
            }
        }
    }
}