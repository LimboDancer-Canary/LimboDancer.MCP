using AslHexMap.Core.Schema;

namespace AslHexMap.Core.Features;

public sealed class FeatureContext
{
    public (int col, int row) Coord { get; init; }
    /// Hex sides shared with same building group (for flush seams if needed).
    public IReadOnlySet<Side> SharedSides { get; init; } = new HashSet<Side>();
    /// Optional: id of building group (rowhouse/factory…)
    public string? GroupId { get; init; }
}