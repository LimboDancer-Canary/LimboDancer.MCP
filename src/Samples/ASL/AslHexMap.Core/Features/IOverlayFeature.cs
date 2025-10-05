using System.Text;

namespace AslHexMap.Core.Features;

public interface IOverlayFeature
{
    /// Token for legend (e.g., "building-wood", "feature-stairwell").
    string Token { get; }

    /// Render onto an already-drawn hex (cx,cy is hex center; size is hex size).
    void Render(StringBuilder sb, double cx, double cy, double size, FeatureContext ctx);
}