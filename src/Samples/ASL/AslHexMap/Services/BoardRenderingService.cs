using AslHexMap.Core.Schema;
using AslHexMap.Core.Rendering;

namespace AslHexMap.Services;

/// <summary>
/// Service responsible for board rendering operations and managing render options.
/// </summary>
public class BoardRenderingService
{
    /// <summary>
    /// Renders a board with the specified options.
    /// </summary>
    /// <param name="board">The board data to render</param>
    /// <param name="options">Rendering options and settings</param>
    /// <returns>Rendering result containing SVG and usage information</returns>
    public Task<RenderResult> RenderBoardAsync(BoardData board, RenderOptions options)
    {
        if (board == null)
            throw new ArgumentNullException(nameof(board));

        var usage = new LegendRenderer.LegendUsage();

        var svg = Renderer.RenderBoardBase(
            board,
            size: options.HexSize,
            showLabels: options.ShowLabels,
            showRoads: options.ShowRoads,
            usage: usage,
            useFeatureOverlays: options.UseFeatureOverlays);

        var result = new RenderResult(svg, usage);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Validates render options and returns any validation errors.
    /// </summary>
    /// <param name="options">Options to validate</param>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> ValidateRenderOptions(RenderOptions options)
    {
        var errors = new List<string>();

        if (options.HexSize < 18 || options.HexSize > 72)
            errors.Add("Hex size must be between 18 and 72");

        return errors;
    }
}

/// <summary>
/// Represents options for board rendering.
/// </summary>
public class RenderOptions
{
    /// <summary>
    /// Size of hexagons in pixels.
    /// </summary>
    public double HexSize { get; set; } = 36;

    /// <summary>
    /// Whether to show hex coordinate labels.
    /// </summary>
    public bool ShowLabels { get; set; } = true;

    /// <summary>
    /// Whether to render roads.
    /// </summary>
    public bool ShowRoads { get; set; } = true;

    /// <summary>
    /// Whether to use feature overlay system.
    /// </summary>
    public bool UseFeatureOverlays { get; set; } = true;
}

/// <summary>
/// Represents the result of a board rendering operation.
/// </summary>
/// <param name="Svg">Generated SVG markup</param>
/// <param name="Usage">Legend usage information for building legends</param>
public record RenderResult(string Svg, LegendRenderer.LegendUsage Usage);