using System;
using System.Collections.Generic;

namespace AslHexMap.Core.Layout
{
    /// <summary>
    /// Flat-top hex layout helpers.
    /// - Axial helpers (for math checks)
    /// - Offset (odd-Q) helpers for ASL-style rectangular boards
    /// </summary>
    public static class HexLayout
    {
        private static readonly double RT3 = Math.Sqrt(3.0);

        // ---------- AXIAL ----------
        public static (double x, double y) AxialToPixelFlat(int q, int r, double size)
        {
            var x = size * (1.5 * q);
            var y = size * (RT3 * r + (RT3 / 2.0) * q);
            return (x, y);
        }

        public static (double minX, double minY, double maxX, double maxY)
            GridExtentsFlat(int cols, int rows, double size)
        {
            if (cols <= 0 || rows <= 0) return (0, 0, 0, 0);

            var apothem = (RT3 / 2.0) * size;
            double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
            double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;

            for (int q = 0; q < cols; q++)
            {
                for (int r = 0; r < rows; r++)
                {
                    var (x, y) = AxialToPixelFlat(q, r, size);
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            // include full hex bounds
            minX -= size; maxX += size;
            minY -= apothem; maxY += apothem;
            return (minX, minY, maxX, maxY);
        }

        /// <summary>
        /// Axial rectangle iterator: (q,r) where q∈[0..cols-1], r∈[0..rows-1].
        /// </summary>
        public static IEnumerable<(int, int)> AxialRect(int cols, int rows)
        {
            for (int q = 0; q < cols; q++)
                for (int r = 0; r < rows; r++)
                    yield return (q, r);
        }

        // ---------- OFFSET (odd-Q) — ASL-style ----------
        /// <summary>
        /// Odd-Q offset (flat-top): columns vertical; odd columns shifted down by apothem.
        /// </summary>
        public static (double x, double y) OffsetOddQToPixelFlat(int col, int row, double size)
        {
            double x = size * (1.5 * col);
            double y = size * RT3 * (row + ((col & 1) == 1 ? 0.5 : 0.0));
            return (x, y);
        }

        /// <summary>
        /// Extents for a rectangular offset grid (cols x rows), including radius/apothem margins.
        /// </summary>
        public static (double minX, double minY, double maxX, double maxY)
            OffsetRectExtentsFlat(int cols, int rows, double size)
        {
            if (cols <= 0 || rows <= 0) return (0, 0, 0, 0);

            var apothem = (RT3 / 2.0) * size;
            double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
            double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;

            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    var (x, y) = OffsetOddQToPixelFlat(c, r, size);
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            minX -= size; maxX += size;
            minY -= apothem; maxY += apothem;
            return (minX, minY, maxX, maxY);
        }

        /// <summary>
        /// Offset rectangle iterator: (col,row) where col∈[0..cols-1], row∈[0..rows-1].
        /// </summary>
        public static IEnumerable<(int, int)> OffsetRect(int cols, int rows)
        {
            for (int c = 0; c < cols; c++)
                for (int r = 0; r < rows; r++)
                    yield return (c, r);
        }
    }
}
