using System;
using System.Diagnostics.CodeAnalysis;

namespace LimboDancer.MCP.Ontology.Runtime
{
    /// <summary>
    /// Represents the hierarchical partition scope across the platform.
    /// All public APIs and persisted ontology artifacts MUST be scoped by TenantScope.
    /// </summary>
    public readonly record struct TenantScope
    {
        public string TenantId { get; }
        public string PackageId { get; }
        public string ChannelId { get; }

        public TenantScope(string tenantId, string packageId, string channelId)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(packageId)) throw new ArgumentException("PackageId is required.", nameof(packageId));
            if (string.IsNullOrWhiteSpace(channelId)) throw new ArgumentException("ChannelId is required.", nameof(channelId));

            TenantId = tenantId.Trim();
            PackageId = packageId.Trim();
            ChannelId = channelId.Trim();
        }

        /// <summary>
        /// Returns the hierarchical partition key (HPK) used in storage layers.
        /// </summary>
        public string PartitionKey => $"{TenantId}::{PackageId}::{ChannelId}";

        public override string ToString() => PartitionKey;

        public static bool TryParse(string value, out TenantScope scope)
        {
            scope = default;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var parts = value.Split(new[] { "::" }, StringSplitOptions.None);
            if (parts.Length != 3) return false;
            scope = new TenantScope(parts[0], parts[1], parts[2]);
            return true;
        }

        public void EnsureComplete()
        {
            // Constructor already enforces non-empty. This method exists for clarity at call sites.
        }

        public void EnsureSame([NotNull] TenantScope other)
        {
            if (!Equals(other))
            {
                throw new TenantScopeException($"Cross-scope operation denied. Expected {this}, got {other}.");
            }
        }

        public TenantScope WithTenant(string tenantId) => new(tenantId, PackageId, ChannelId);
        public TenantScope WithPackage(string packageId) => new(TenantId, packageId, ChannelId);
        public TenantScope WithChannel(string channelId) => new(TenantId, PackageId, channelId);
    }
}