// linear-features.js
// Road/stream/path/rail curves that "stitch" neatly across hex edges.
// Curvy road geometry (ported from v1) + edge midpoints
// Works with flat-top, N=0 edge order ["N","NE","SE","S","SW","NW"]

import { EDGE_ANGLE, SIDE_ORDER, idxOf } from "./hex-geom.js";

/** Utility functions */
function norm(a) { while (a <= -Math.PI) a += 2 * Math.PI; while (a > Math.PI) a -= 2 * Math.PI; return a; }
function midAng(a, b) { const d = norm(b - a); return a + d / 2; }
function rot(theta, r) { return { x: r * Math.cos(theta), y: r * Math.sin(theta) }; }
function scale(p, k) { return { x: p.x * k, y: p.y * k }; }

/** edge midpoint vector relative to center */
function edgeVec(side, size) {
  const apothem = size * Math.cos(Math.PI / 6);
  const a = EDGE_ANGLE[side];
  return { x: apothem * Math.cos(a), y: apothem * Math.sin(a) };
}

/**
 * Build an SVG path for a through-hex linear feature.
 * Curvy road path (Bezier-ish via SVG Q curves), with tiny overhangs to stitch across hex borders
 * @param {{enter:{x,y}, exit?:{x,y}}} midpoints  midpoints at APOTHEM distance from center
 * @param {string} enterName one of "N","NE","SE","S","SW","NW"
 * @param {string|null} exitName same, or null/undefined for termination in hex
 * @param {number} size hex radius (for control distances)
 * @param {string[]} order side order array (default SIDE_ORDER)
 * @returns {string} SVG path "d" attribute
 */
export function roadPath(midpoints, enterName, exitName, size, order = SIDE_ORDER) {
  const mp = midpoints;
  const pIn = mp.enter, pOut = mp.exit ?? null;
  const t1 = 1.02, t2 = 1.02;             // slight overlap across edges
  const aIn = EDGE_ANGLE[enterName];
  const P1 = scale(pIn, t1);
  const P2 = pOut ? scale(pOut, t2) : { x: 0, y: 0 };

  if (!exitName) {
    // cap inside the hex
    const inward = { x: -Math.cos(aIn), y: -Math.sin(aIn) };
    const C = { x: P1.x + inward.x * size * 0.55, y: P1.y + inward.y * size * 0.55 };
    return `M ${P1.x} ${P1.y} Q ${C.x} ${C.y} 0 0`;
  }

  const aOut = EDGE_ANGLE[exitName];
  const di = ((idxOf(exitName, order) - idxOf(enterName, order)) % 6 + 6) % 6;

  if (di === 1 || di === 5) {
    // adjacent edges → bend toward mid-angle
    const mid = midAng(aIn, aOut);
    const C = rot(mid, size * 0.35);
    return `M ${P1.x} ${P1.y} Q ${C.x} ${C.y} ${P2.x} ${P2.y}`;
  } else if (di === 2 || di === 4) {
    // two apart → go via center
    return `M ${P1.x} ${P1.y} Q 0 0 ${P2.x} ${P2.y}`;
  } else {
    // opposite → subtle perpendicular through center
    const vx = P2.x - P1.x, vy = P2.y - P1.y;
    const C = { x: -vy * 0.15, y: vx * 0.15 };
    return `M ${P1.x} ${P1.y} Q ${C.x} ${C.y} ${P2.x} ${P2.y}`;
  }
}

/** Midpoints from side names and hex size (radius). */
export function midpointsFromSides(enterName, exitName, size) {
  const enter = edgeVec(enterName, size);
  const exit = exitName ? edgeVec(exitName, size) : null;
  return { enter, exit };
}
