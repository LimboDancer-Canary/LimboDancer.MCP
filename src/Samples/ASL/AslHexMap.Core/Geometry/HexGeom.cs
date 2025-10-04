using System;
using System.Globalization;
using System.Text;
using AslHexMap.Core.Schema; // for Side

namespace AslHexMap.Core.Geometry
{
    public static class HexGeom
    {
        /// <summary>Flat-top hex vertices, clockwise, starting at angle 0° (East).</summary>
        public static (double x, double y)[] PointsFlatTop(double cx, double cy, double size)
        {
            var pts = new (double x, double y)[6];
            for (int i = 0; i < 6; i++)
            {
                var angleDeg = 60 * i; // 0,60,120,180,240,300
                var angleRad = Math.PI / 180 * angleDeg;
                pts[i] = (cx + size * Math.Cos(angleRad), cy + size * Math.Sin(angleRad));
            }
            return pts;
        }

        public static string ToSvgPoints((double x, double y)[] pts)
        {
            var sb = new StringBuilder();
            var inv = CultureInfo.InvariantCulture;
            for (int i = 0; i < pts.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(pts[i].x.ToString("0.###", inv));
                sb.Append(',');
                sb.Append(pts[i].y.ToString("0.###", inv));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Midpoint of a hex side for flat-top orientation (0=N,1=NE,2=SE,3=S,4=SW,5=NW).
        /// </summary>
        public static (double x, double y) EdgeMidpointFlatTop(double cx, double cy, double size, Side side)
        {
            var p = PointsFlatTop(cx, cy, size);
            // Map side -> (v1,v2) index in our points array [E(0), NE(1), NW(2), W(3), SW(4), SE(5)]
            (int a, int b) = side switch
            {
                Side.N => (1, 2), // NE-NW
                Side.NE => (0, 1), // E-NE
                Side.SE => (5, 0), // SE-E
                Side.S => (4, 5), // SW-SE
                Side.SW => (3, 4), // W-SW
                Side.NW => (2, 3), // NW-W
                _ => (1, 2)
            };
            return ((p[a].x + p[b].x) * 0.5, (p[a].y + p[b].y) * 0.5);
        }

        /// <summary>Label anchor near the NW interior of the hex.</summary>
        public static (double x, double y) LabelAnchorNW(double cx, double cy, double size)
        {
            var p = PointsFlatTop(cx, cy, size);
            var nw = p[2]; // NW vertex
            // Move inwards ~25% toward center so the label sits inside the hex
            var x = cx + (nw.x - cx) * 0.75;
            var y = cy + (nw.y - cy) * 0.75;
            return (x, y);
        }
    }
}
