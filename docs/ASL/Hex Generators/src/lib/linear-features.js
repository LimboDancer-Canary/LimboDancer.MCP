// lib/linear-features.js
// Linear features (roads/streams/rails) that "stitch" across hex borders.
// Works with flat-top hexes (N=0 side indexing: ["N","NE","SE","S","SW","NW"]).
//
// IMPORTANT: All geometry here is in LOCAL hex coordinates (center at 0,0).
// When rendering, wrap the path(s) in a <g transform="translate(cx,cy)">…</g>.

/* eslint-disable no-mixed-operators */

import { EDGE_ANGLE, SIDE_ORDER, idxOf, apothem } from './hex-geom.js';

/////////////////////////
// Small math helpers  //
/////////////////////////

function normAngle(a) {
  while (a <= -Math.PI) a += 2 * Math.PI;
  while (a >   Math.PI) a -= 2 * Math.PI;
  return a;
}

function midAngle(a, b) {
  const d = normAngle(b - a);
  return a + d / 2;
}

function polarVec(theta, r) {
  return { x: r * Math.cos(theta), y: r * Math.sin(theta) };
}

function scaleVec(p, k) {
  return { x: p.x * k, y: p.y * k };
}

/////////////////////////////
// Edge midpoint utilities //
/////////////////////////////

/**
 * Local vector from hex center to the midpoint of a given side.
 * @param {"N"|"NE"|"SE"|"S"|"SW"|"NW"} side
 * @param {number} size - hex radius
 * @returns {{x:number,y:number}}
 */
function edgeVec(side, size) {
  const a = EDGE_ANGLE[side];
  const r = apothem(size); // center → side distance
  return polarVec(a, r);
}

/**
 * Convenience: compute enter/exit midpoints given side names.
 * Returned points are in LOCAL hex coordinates.
 * @param {"N"|"NE"|"SE"|"S"|"SW"|"NW"} enterName
 * @param {"N"|"NE"|"SE"|"S"|"SW"|"NW"|null|undefined} exitName
 * @param {number} size - hex radius
 * @returns {{enter:{x:number,y:number}, exit?:{x:number,y:number}}}
 */
export function midpointsFromSides(enterName, exitName, size) {
  const enter = edgeVec(enterName, size);
  const exit  = exitName ? edgeVec(exitName, size) : undefined;
  return { enter, exit };
}

/////////////////////////////
// Curvy road path builder //
/////////////////////////////

/**
 * Build an SVG path `d` for a through-hex linear feature (e.g., road).
 * The path is created in LOCAL hex coordinates. For seamless stitching across
 * adjacent hexes, it slightly overhangs past the apothem.
 *
 * Rules:
 *  - Adjacent edges (turn): curve toward the angular bisector.
 *  - Two apart: curve via the center (soft S).
 *  - Opposite edges: subtle perpendicular offset through center.
 *  - Dead-end (no exit): curve from entry toward center and cap at (0,0).
 *
 * @param {{enter:{x:number,y:number}, exit?:{x:number,y:number}}} midpoints
 * @param {"N"|"NE"|"SE"|"S"|"SW"|"NW"} enterName
 * @param {"N"|"NE"|"SE"|"S"|"SW"|"NW"|null|undefined} exitName
 * @param {number} size - hex radius
 * @param {string[]} [order=SIDE_ORDER] - side ordering (clockwise)
 * @returns {string} SVG path data
 */
export function roadPath(midpoints, enterName, exitName, size, order = SIDE_ORDER) {
  const pIn  = midpoints.enter;
  const pOut = midpoints.exit ?? null;

  // Slight overhang to guarantee seam continuity across borders
  const tIn = 1.02;
  const tOut = 1.02;

  const aIn = EDGE_ANGLE[enterName];
  const P1 = scaleVec(pIn, tIn);
  const P2 = pOut ? scaleVec(pOut, tOut) : { x: 0, y: 0 };

  // Dead-end: bend inward and cap at center
  if (!exitName) {
    const inward = { x: -Math.cos(aIn), y: -Math.sin(aIn) };
    const C = { x: P1.x + inward.x * size * 0.55, y: P1.y + inward.y * size * 0.55 };
    return `M ${P1.x} ${P1.y} Q ${C.x} ${C.y} 0 0`;
  }

  const aOut = EDGE_ANGLE[exitName];

  // Relative turn distance (0..5) in the chosen order
  const di = ((idxOf(exitName, order) - idxOf(enterName, order)) % 6 + 6) % 6;

  if (di === 1 || di === 5) {
    // Adjacent edges: bend toward the angular bisector
    const mid = midAngle(aIn, aOut);
    const C = polarVec(mid, size * 0.35);
    return `M ${P1.x} ${P1.y} Q ${C.x} ${C.y} ${P2.x} ${P2.y}`;
  } else if (di === 2 || di === 4) {
    // Two-apart: pass through the center for a gentle S-curve
    return `M ${P1.x} ${P1.y} Q 0 0 ${P2.x} ${P2.y}`;
  } else {
    // Opposite (straight across): add a small perpendicular offset at center
    const vx = P2.x - P1.x, vy = P2.y - P1.y;
    const C = { x: -vy * 0.15, y: vx * 0.15 };
    return `M ${P1.x} ${P1.y} Q ${C.x} ${C.y} ${P2.x} ${P2.y}`;
  }
}

/* Future extension:
 * - export function streamPath(...) with different control distances & style
 * - export function railPath(...), etc.
 */
