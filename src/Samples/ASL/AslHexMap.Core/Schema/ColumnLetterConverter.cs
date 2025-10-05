namespace AslHexMap.Core.Schema;

/// <summary>
/// Handles conversion between column letters and indices.
/// </summary>
public class ColumnLetterConverter
{
    /// <summary>
    /// Converts column letters to zero-based column index.
    /// A=0, B=1, ... Z=25, AA=26, AB=27, etc.
    /// </summary>
    /// <param name="letters">Column letters (e.g., "A", "AA", "AB")</param>
    /// <returns>Zero-based column index</returns>
    /// <exception cref="ArgumentException">Thrown when letters string is null or empty</exception>
    public int LettersToIndex(string letters)
    {
        if (string.IsNullOrEmpty(letters))
            throw new ArgumentException("Column letters cannot be null or empty", nameof(letters));

        int index = 0;
        foreach (char c in letters)
        {
            if (c < 'A' || c > 'Z')
                throw new ArgumentException($"Invalid column letter: '{c}'", nameof(letters));
                
            index = index * 26 + (c - 'A' + 1);
        }
            
        return index - 1; // Convert from 1-based to 0-based
    }

    /// <summary>
    /// Converts zero-based column index to column letters.
    /// 0=A, 1=B, ... 25=Z, 26=AA, 27=AB, etc.
    /// </summary>
    /// <param name="index">Zero-based column index</param>
    /// <returns>Column letters representation</returns>
    /// <exception cref="ArgumentException">Thrown when index is negative</exception>
    public string IndexToLetters(int index)
    {
        if (index < 0)
            throw new ArgumentException("Column index cannot be negative", nameof(index));

        index += 1; // Convert from 0-based to 1-based for calculation
            
        var letters = string.Empty;
        while (index > 0)
        {
            int remainder = (index - 1) % 26;
            letters = (char)('A' + remainder) + letters;
            index = (index - 1) / 26;
        }
            
        return letters;
    }
}