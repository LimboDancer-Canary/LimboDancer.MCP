// hex-geom.js
// Flat-top hex geometry + indexing (N = 0).
// Works for both the viewer and visualizer.

/** Default side order, clockwise, with N=0 */
export const SIDE_ORDER = ["N", "NE", "SE", "S", "SW", "NW"];

/** Angles (radians) for the outward normal of each side (flat-top) */
export const EDGE_ANGLE = {
  N: 3 * Math.PI / 2,
  NE: 11 * Math.PI / 6,
  SE: Math.PI / 6,
  S: Math.PI / 2,
  SW: 5 * Math.PI / 6,
  NW: 7 * Math.PI / 6,
};

/** Convert side name to index in a given order (defaults to SIDE_ORDER) */
export function idxOf(name, order = SIDE_ORDER) {
  return order.indexOf(name);
}

/** Convert index to side name in a given order (supports neg/overflow) */
export function nameOf(index, order = SIDE_ORDER) {
  return order[((index % 6) + 6) % 6];
}

/** Opposite side index (3 away) */
export function oppositeIndex(i) {
  return (i + 3) % 6;
}

/** Vertex position at unit radius (flat-top), scaled by `size` */
export function getVertexPosition(i, size) {
  const a = (Math.PI / 3) * i;               // flat-top
  return { x: size * Math.cos(a), y: size * Math.sin(a) };
}

/** Midpoint of hexside `edgeIndex` at radius `size` (apothem) */
export function getEdgeMidpoint(edgeIndex, size) {
  const v1 = getVertexPosition(edgeIndex, size);
  const v2 = getVertexPosition((edgeIndex + 1) % 6, size);
  return { x: (v1.x + v2.x) / 2, y: (v2.y + v1.y) / 2 };
}

/** Interpolate between two points */
export function lerpPoint(p1, p2, t) {
  return { x: p1.x + (p2.x - p1.x) * t, y: p1.y + (p2.y - p1.y) * t };
}

/** Points string for a hex polygon centered at (cx,cy) with radius `size` */
export function hexPolygonPoints(cx, cy, size) {
  const pts = [];
  for (let i = 0; i < 6; i++) {
    const v = getVertexPosition(i, size);
    pts.push(`${(cx + v.x).toFixed(3)},${(cy + v.y).toFixed(3)}`);
  }
  return pts.join(" ");
}

/** Outward normal angle (deg) for hexside i (SVG Y+ down) */
export function edgeNormalAngleDeg(i, size) {
  const m = getEdgeMidpoint(i, size);
  return (Math.atan2(m.y, m.x) * 180) / Math.PI;
}
