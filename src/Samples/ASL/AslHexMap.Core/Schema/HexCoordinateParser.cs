namespace AslHexMap.Core.Schema;

/// <summary>
/// Handles parsing and validation of hex coordinate strings.
/// </summary>
public class HexCoordinateParser
{
    private readonly HexIdValidator _validator;
    private readonly ColumnLetterConverter _columnConverter;

    public HexCoordinateParser()
    {
        _validator = new HexIdValidator();
        _columnConverter = new ColumnLetterConverter();
    }

    /// <summary>
    /// Parses a hex ID string into column and row coordinates.
    /// </summary>
    /// <param name="hexId">The hex ID to parse (e.g., "A1", "12AA15", "Z99")</param>
    /// <returns>Zero-based column and row coordinates</returns>
    /// <exception cref="ArgumentException">Thrown when the hex ID format is invalid</exception>
    public (int col, int row) Parse(string hexId)
    {
        var components = _validator.ValidateAndExtract(hexId);
            
        int col = _columnConverter.LettersToIndex(components.ColumnLetters);
        int row = ParseRowNumber(components.RowNumber);

        ValidateCoordinates(col, row, hexId);
            
        return (col, row);
    }

    /// <summary>
    /// Parses row number string and converts to zero-based index.
    /// </summary>
    /// <param name="rowString">Row number as string</param>
    /// <returns>Zero-based row index</returns>
    /// <exception cref="FormatException">Thrown when row number is invalid</exception>
    private static int ParseRowNumber(string rowString)
    {
        if (!int.TryParse(rowString, out int row))
            throw new FormatException($"Invalid row number: '{rowString}'");
            
        return row - 1; // Convert from 1-based to 0-based
    }

    /// <summary>
    /// Validates that the parsed coordinates are valid.
    /// </summary>
    /// <param name="col">Column index</param>
    /// <param name="row">Row index</param>
    /// <param name="originalHexId">Original hex ID for error messages</param>
    /// <exception cref="ArgumentException">Thrown when coordinates are negative</exception>
    private static void ValidateCoordinates(int col, int row, string originalHexId)
    {
        if (col < 0 || row < 0)
            throw new ArgumentException($"Invalid hex id produces negative coordinates: '{originalHexId}'");
    }
}