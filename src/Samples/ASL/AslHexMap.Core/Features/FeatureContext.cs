using System;

namespace AslHexMap.Core.Features
{
    /// <summary>
    /// Rendering-time context shared by features that occupy the same hex/group.
    /// </summary>
    public sealed class FeatureContext
    {
        /// <summary>Board coordinates for the current hex.</summary>
        public (int col, int row) Coord { get; init; }

        /// <summary>
        /// Optional grouping identifier: features with the same GroupId
        /// are considered part of the same composite (e.g., rowhouses).
        /// </summary>
        public string? GroupId { get; init; }

        // ---- New: cooperative badge hints ----

        /// <summary>
        /// When true, a Stairwell feature will draw a circular badge that also
        /// carries the building level number. The BuildingFootprint should then
        /// suppress its own rectangular badge.
        /// </summary>
        public bool UseCircularStairwellBadge { get; init; }

        /// <summary>
        /// If provided, the level to show inside the circular stairwell badge.
        /// Typically sourced from the BuildingFootprint.Levels.
        /// </summary>
        public int? StairwellBadgeLevel { get; init; }
    }
}