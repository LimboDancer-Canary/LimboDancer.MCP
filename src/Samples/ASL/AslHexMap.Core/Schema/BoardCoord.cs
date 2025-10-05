using System;

namespace AslHexMap.Core.Schema
{
    /// <summary>
    /// Main coordinate parsing class that orchestrates validation and conversion.
    /// </summary>
    public static class BoardCoord
    {
        private static readonly HexCoordinateParser _parser = new();

        public static (int col, int row) Parse(string coord)
        {
            return _parser.Parse(coord);
        }
    }
}