using AslHexMap.Core.Schema;

namespace AslHexMap.Core.Rendering;

/// <summary>Small shared helpers.</summary>
public static class Util
{
    public static Func<double, double, (double, double)> MakeShifter(double minX, double minY, double margin)
        => (x, y) => (x - minX + margin, y - minY + margin);

    public static string IndexToLetters(int index)
    {
        index += 1;
        var s = "";
        while (index > 0)
        {
            int rem = (index - 1) % 26;
            s = (char)('A' + rem) + s;
            index = (index - 1) / 26;
        }
        return s;
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