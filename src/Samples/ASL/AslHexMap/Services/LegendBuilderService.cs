using AslHexMap.Core.Rendering;

namespace AslHexMap.Services;

/// <summary>
/// Service responsible for building legend view models from usage data.
/// </summary>
public class LegendBuilderService
{
    private readonly LegendService _legendService;

    public LegendBuilderService(LegendService legendService)
    {
        _legendService = legendService ?? throw new ArgumentNullException(nameof(legendService));
    }

    /// <summary>
    /// Builds a legend view model from legend usage data.
    /// </summary>
    /// <param name="usage">Usage data indicating which legend items to include</param>
    /// <returns>Legend view model with categorized items</returns>
    public async Task<LegendViewModel> BuildLegendAsync(LegendRenderer.LegendUsage usage)
    {
        if (usage == null)
            throw new ArgumentNullException(nameof(usage));

        var tokens = CollectTokensFromUsage(usage);
        
        var model = await _legendService.LoadAsync();
        var labelMap = await _legendService.LabelsForAsync(tokens);

        var baseTerrain = ExtractBaseTerrain(model, labelMap);
        var buildingFeatures = ExtractBuildingFeatures(model, labelMap);

        return new LegendViewModel(baseTerrain, buildingFeatures);
    }

    /// <summary>
    /// Collects all relevant tokens from the usage data.
    /// </summary>
    /// <param name="usage">Usage data to extract tokens from</param>
    /// <returns>Set of tokens for legend lookup</returns>
    private static HashSet<string> CollectTokensFromUsage(LegendRenderer.LegendUsage usage)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add base terrain tokens
        foreach (var baseTerrain in usage.Bases)
            tokens.Add($"base-{baseTerrain}");

        // Add building feature tokens (bridge to new legend tokens)
        foreach (var building in usage.Buildings)
        {
            if (building.Equals("wood", StringComparison.OrdinalIgnoreCase))
                tokens.Add("building-wood");
            else if (building.Equals("stone", StringComparison.OrdinalIgnoreCase))
                tokens.Add("building-stone");
        }

        return tokens;
    }

    /// <summary>
    /// Extracts base terrain legend items from the model.
    /// </summary>
    /// <param name="model">Legend model containing all sections</param>
    /// <param name="labelMap">Map of tokens to labels</param>
    /// <returns>List of base terrain legend items</returns>
    private static List<LegendItemViewModel> ExtractBaseTerrain(
        LegendService.LegendModel model, 
        Dictionary<string, string> labelMap)
    {
        var baseTerrainSection = model.Sections
            .FirstOrDefault(s => s.Title.Equals("Base Terrain", StringComparison.OrdinalIgnoreCase));

        if (baseTerrainSection == null)
            return new List<LegendItemViewModel>();

        return baseTerrainSection.Items
            .Where(item => labelMap.ContainsKey(item.Token))
            .Select(item => new LegendItemViewModel(item.Token, labelMap[item.Token]))
            .ToList();
    }

    /// <summary>
    /// Extracts building feature legend items from the model.
    /// </summary>
    /// <param name="model">Legend model containing all sections</param>
    /// <param name="labelMap">Map of tokens to labels</param>
    /// <returns>List of building feature legend items</returns>
    private static List<LegendItemViewModel> ExtractBuildingFeatures(
        LegendService.LegendModel model, 
        Dictionary<string, string> labelMap)
    {
        var buildingSection = model.Sections
            .FirstOrDefault(s => s.Title.Equals("Building Features", StringComparison.OrdinalIgnoreCase));

        if (buildingSection == null)
            return new List<LegendItemViewModel>();

        return buildingSection.Items
            .Where(item => labelMap.ContainsKey(item.Token))
            .Select(item => new LegendItemViewModel(item.Token, labelMap[item.Token]))
            .ToList();
    }
}

/// <summary>
/// View model representing a complete legend with categorized items.
/// </summary>
/// <param name="BaseTerrain">Base terrain legend items</param>
/// <param name="BuildingFeatures">Building feature legend items</param>
public record LegendViewModel(
    IReadOnlyList<LegendItemViewModel> BaseTerrain,
    IReadOnlyList<LegendItemViewModel> BuildingFeatures)
{
    /// <summary>
    /// Gets whether the legend has any items to display.
    /// </summary>
    public bool HasItems => BaseTerrain.Count > 0 || BuildingFeatures.Count > 0;
}

/// <summary>
/// View model representing a single legend item.
/// </summary>
/// <param name="Token">Token identifier for the legend item</param>
/// <param name="Label">Display label for the legend item</param>
public record LegendItemViewModel(string Token, string Label);