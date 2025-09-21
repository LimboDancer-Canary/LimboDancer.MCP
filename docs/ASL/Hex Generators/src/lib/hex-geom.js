// lib/hex-geom.js
// Flat-top hex geometry utilities (N = 0 side indexing).
// Compatible with the visualizer, viewer, and generator.
//
// Coordinate system: SVG-style (x right, y down).
// Hex orientation: flat-top. Side names are clockwise: ["N","NE","SE","S","SW","NW"].

//////////////////////////
// Constants & Indexing //
//////////////////////////

/** Default side order, clockwise, with N=0. */
export const SIDE_ORDER = ["N", "NE", "SE", "S", "SW", "NW"];

/**
 * Outward-normal angles (radians) at each hex side midpoint (local hex coords).
 * Values are chosen for flat-top orientation with SVG's Y+ downward.
 */
export const EDGE_ANGLE = {
  N: 3 * Math.PI / 2,   // up
  NE: 11 * Math.PI / 6, // up-right
  SE: Math.PI / 6,      // down-right
  S: Math.PI / 2,       // down
  SW: 5 * Math.PI / 6,  // down-left
  NW: 7 * Math.PI / 6,  // up-left
};

/**
 * Get index of a side name inside a given order (default SIDE_ORDER).
 * @param {string} name - One of "N","NE","SE","S","SW","NW".
 * @param {string[]} [order=SIDE_ORDER]
 * @returns {number} index or -1 if not present
 */
export function idxOf(name, order = SIDE_ORDER) {
  return order.indexOf(name);
}

/**
 * Normalize an index to [0..5] and return the side name from the given order.
 * @param {number} index - Can be negative or overflow.
 * @param {string[]} [order=SIDE_ORDER]
 * @returns {string} side name
 */
export function nameOf(index, order = SIDE_ORDER) {
  return order[((index % 6) + 6) % 6];
}

/**
 * Opposite side index (3 steps away).
 * @param {number} i
 * @returns {number}
 */
export function oppositeIndex(i) {
  return (i + 3) % 6;
}

//////////////////////////
// Vertex/Edge Geometry //
//////////////////////////

/**
 * Get vertex position (local) at unit radius, scaled by `size`.
 * Vertex 0 is at angle 0 (to the right), then 60° steps (flat-top).
 * @param {number} i - vertex index [0..5]
 * @param {number} size - hex radius
 * @returns {{x:number,y:number}}
 */
export function getVertexPosition(i, size) {
  const a = (Math.PI / 3) * i; // 0,60,120,...
  return { x: size * Math.cos(a), y: size * Math.sin(a) };
}

/**
 * Get the midpoint of hex side `edgeIndex` (local), at radius `size`.
 * Computed as midpoint between vertices i and (i+1).
 * @param {number} edgeIndex - [0..5]
 * @param {number} size - hex radius
 * @returns {{x:number,y:number}}
 */
export function getEdgeMidpoint(edgeIndex, size) {
  const v1 = getVertexPosition(edgeIndex, size);
  const v2 = getVertexPosition((edgeIndex + 1) % 6, size);
  return { x: (v1.x + v2.x) / 2, y: (v1.y + v2.y) / 2 };
}

/**
 * Apothem (center to side distance) for a flat-top hex of radius `size`.
 * @param {number} size
 * @returns {number}
 */
export function apothem(size) {
  return size * Math.cos(Math.PI / 6);
}

/**
 * Linear interpolation between two points.
 * @param {{x:number,y:number}} p1
 * @param {{x:number,y:number}} p2
 * @param {number} t - [0..1]
 * @returns {{x:number,y:number}}
 */
export function lerpPoint(p1, p2, t) {
  return { x: p1.x + (p2.x - p1.x) * t, y: p1.y + (p2.y - p1.y) * t };
}

/////////////////////
// Drawing Helpers //
/////////////////////

/**
 * Points string for a hex polygon centered at (cx,cy) with radius `size`.
 * Suitable for the SVG <polygon points="..."> attribute.
 * @param {number} cx
 * @param {number} cy
 * @param {number} size
 * @returns {string}
 */
export function hexPolygonPoints(cx, cy, size) {
  const pts = [];
  for (let i = 0; i < 6; i++) {
    const v = getVertexPosition(i, size);
    pts.push(`${(cx + v.x).toFixed(3)},${(cy + v.y).toFixed(3)}`);
  }
  return pts.join(" ");
}

/** Alias requested by apps: identical to hexPolygonPoints(...) */
export function getHexPoints(cx, cy, size) {
  return hexPolygonPoints(cx, cy, size);
}

/**
 * Outward normal angle (degrees) for side i (local coords).
 * Useful for label/marker orientation if needed.
 * @param {number} i - side index [0..5]
 * @param {number} size - hex radius (unused here; included for parity)
 * @returns {number} angle in degrees
 */
export function edgeNormalAngleDeg(i, size) { // size kept for API symmetry
  const m = getEdgeMidpoint(i, size || 1);
  return (Math.atan2(m.y, m.x) * 180) / Math.PI;
}
