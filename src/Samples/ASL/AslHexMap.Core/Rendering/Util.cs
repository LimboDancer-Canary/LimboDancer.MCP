using AslHexMap.Core.Schema;

namespace AslHexMap.Core.Rendering;

/// <summary>Small shared helpers.</summary>
public static class Util
{
    private static readonly ColumnLetterConverter _columnConverter = new();

    public static Func<double, double, (double, double)> MakeShifter(double minX, double minY, double margin)
        => (x, y) => (x - minX + margin, y - minY + margin);

    /// <summary>
    /// Converts zero-based column index to column letters.
    /// </summary>
    /// <param name="index">Zero-based column index</param>
    /// <returns>Column letters representation</returns>
    public static string IndexToLetters(int index)
    {
        return _columnConverter.IndexToLetters(index);
    }

    public static Dictionary<(int col, int row), IndividualHex> IndexPerHex(BoardData data)
    {
        var perHex = new Dictionary<(int col, int row), IndividualHex>();
        var list = data.Map?.IndividualHexes;
        if (list is null) return perHex;

        foreach (var h in list)
        {
            try
            {
                var k = BoardCoord.Parse(h.HexId);
                perHex[(k.col, k.row)] = h;
            }
            catch
            {
                // ignore parse errors
            }
        }
        return perHex;
    }
}