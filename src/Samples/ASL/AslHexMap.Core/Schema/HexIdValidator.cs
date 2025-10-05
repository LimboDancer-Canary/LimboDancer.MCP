using System.Text.RegularExpressions;

namespace AslHexMap.Core.Schema;

/// <summary>
/// Validates hex ID format and extracts components using regex.
/// </summary>
public class HexIdValidator
{
    // Optional leading digits (board no.), then letters, then digits.
    private static readonly Regex HexIdRegex = new(@"^\s*(\d+)?([A-Za-z]+)(\d+)\s*$", RegexOptions.Compiled);

    /// <summary>
    /// Validates hex ID format and extracts its components.
    /// </summary>
    /// <param name="hexId">The hex ID string to validate</param>
    /// <returns>Extracted components from the hex ID</returns>
    /// <exception cref="ArgumentException">Thrown when hex ID format is invalid</exception>
    public HexIdComponents ValidateAndExtract(string hexId)
    {
        var match = HexIdRegex.Match(hexId ?? string.Empty);
            
        if (!match.Success)
            throw new ArgumentException($"Invalid hex id format: '{hexId}'");

        var boardNumber = match.Groups[1].Success ? match.Groups[1].Value : null;
        var columnLetters = match.Groups[2].Value.ToUpperInvariant();
        var rowNumber = match.Groups[3].Value;

        return new HexIdComponents(boardNumber, columnLetters, rowNumber);
    }
}