namespace AslHexMap.Core.Schema;

/// <summary>
/// Represents the extracted components of a hex ID.
/// </summary>
/// <param name="BoardNumber">Optional board number prefix</param>
/// <param name="ColumnLetters">Column letters (A, B, AA, etc.)</param>
/// <param name="RowNumber">Row number as string</param>
public record HexIdComponents(string? BoardNumber, string ColumnLetters, string RowNumber);