using System;

namespace LimboDancer.MCP.Graph.CosmosGremlin
{
    /// <summary>
    /// Represents an effect in the knowledge graph. 
    /// Effects describe the outcome of an action or relation.
    /// </summary>
    public sealed class Effect
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// The tenant that owns this effect.
        /// </summary>
        public Guid TenantId { get; init; }

        /// <summary>
        /// Label or type of the effect.
        /// </summary>
        public string Label { get; init; } = string.Empty;

        /// <summary>
        /// Free-form payload for the effect.
        /// </summary>
        public string Payload { get; init; } = string.Empty;

        /// <summary>
        /// UTC timestamp of creation.
        /// </summary>
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    }
}