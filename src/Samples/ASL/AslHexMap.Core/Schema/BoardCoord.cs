using System;
using System.Text.RegularExpressions;

namespace AslHexMap.Core.Schema
{
    public static class BoardCoord
    {
        // Optional leading digits (board no.), then letters, then digits.
        private static readonly Regex Rx = new(@"^\s*(\d+)?([A-Za-z]+)(\d+)\s*$", RegexOptions.Compiled);

        public static (int col, int row) Parse(string coord)
        {
            var m = Rx.Match(coord ?? string.Empty);
            if (!m.Success) throw new ArgumentException($"Invalid hex id: '{coord}'");

            var colStr = m.Groups[2].Value.ToUpperInvariant();
            var rowStr = m.Groups[3].Value;

            int col = LettersToIndex(colStr);
            int row = int.Parse(rowStr) - 1; // rows labeled from 1

            if (col < 0 || row < 0) throw new ArgumentException($"Invalid hex id: '{coord}'");
            return (col, row);
        }

        // A=0, B=1, ... Z=25, AA=26, ...
        private static int LettersToIndex(string s)
        {
            int n = 0;
            for (int i = 0; i < s.Length; i++)
                n = n * 26 + (s[i] - 'A' + 1);
            return n - 1;
        }
    }
}