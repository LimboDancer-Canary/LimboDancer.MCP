// lib/linear-features.js
// Linear features (roads/paths/rails/streams) that "stitch" across hex borders.
// Works with flat-top hexes and 0=N side indexing: ["N","NE","SE","S","SW","NW"].
//
// IMPORTANT: All geometry here is LOCAL to the hex (center at 0,0).
// When rendering, wrap the path(s) in a <g transform="translate(cx,cy)">…</g>.

/* eslint-disable no-mixed-operators */

import { EDGE_ANGLE, SIDE_ORDER, idxOf, apothem } from './hex-geom.js';
import { edgeName } from './schema.js';

/////////////////////////
// Shared "model" bits //
/////////////////////////

/** Canonical kinds for traversals. */
export const LinearKinds = Object.freeze({
  road:   'road',
  path:   'path',
  rail:   'rail',
  stream: 'stream',
});

/**
 * Minimal registry (extensible) for feature defaults/styling tokens.
 * Renderers can consult this without hard-coding.
 */
export const LinearRegistry = {
  [LinearKinds.road]:   { strokeBase: '#666', strokeTop: '#c8c8c8', widthBase: 4, widthTop: 2.4 },
  [LinearKinds.path]:   { strokeBase: '#6b6b6b', strokeTop: '#dedede', widthBase: 2.5, widthTop: 1.3, dashed: '4 3' },
  [LinearKinds.rail]:   { strokeBase: '#2e2e2e', strokeTop: '#f6f6f6', widthBase: 3.6, widthTop: 1.0, sleepersEvery: 10 },
  [LinearKinds.stream]: { strokeBase: '#2a6faa', strokeTop: '#7fb3e0', widthBase: 3.6, widthTop: 2.0 },
};

/**
 * Normalize a traversal-like object → { kind, enters, exits } where
 * enters/exits are SIDE NAMES per `order` (numbers or strings accepted).
 *
 * Supported shapes:
 *  - { type:'road'|'path'|'rail'|'stream', enters:<name|index>, exits:<name|index|null> }
 *  - { kind:'road', ... } (alias for type)
 *  - { enters, exits } (kind defaults to 'road')
 *  - legacy:
 *      template.linearFeature = { entryEdge, exitEdge }
 *      template.road          = { entryEdge, exitEdge }
 *
 * @param {any} obj
 * @param {string[]} [order=SIDE_ORDER]
 * @returns {{ kind:string, enters:string|null, exits:string|null }}
 */
export function normalizeTraversal(obj, order = SIDE_ORDER) {
  const kind = (obj?.kind || obj?.type || LinearKinds.road).toString().toLowerCase();
  // Accept various field names
  const entersRaw = obj?.enters ?? obj?.entryEdge;
  const exitsRaw  = obj?.exits  ?? obj?.exitEdge;

  const enters = edgeName(entersRaw, order);
  const exits  = edgeName(exitsRaw,  order);

  return { kind, enters, exits };
}

/**
 * Convenience to build a stable key for a traversal inside a hex.
 * @param {{kind:string, enters:string|null, exits:string|null}} t
 */
export function traversalKey(t) {
  return `${t.kind}:${t.enters ?? '∅'}>${t.exits ?? '∅'}`;
}

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
  const enter = enterName ? edgeVec(enterName, size) : undefined;
  const exit  = exitName ? edgeVec(exitName, size) : undefined;
  return { enter, exit };
}

/**
 * Like midpointsFromSides but from a traversal-like object.
 * @param {{enters:any, exits:any}} traversal
 * @param {number} size
 * @param {string[]} [order=SIDE_ORDER]
 * @returns {{enter:{x:number,y:number}|undefined, exit?:{x:number,y:number}|undefined, entersName:string|null, exitsName:string|null}}
 */
export function midpointsFromTraversal(traversal, size, order = SIDE_ORDER) {
  const t = normalizeTraversal(traversal, order);
  const m = midpointsFromSides(t.enters, t.exits, size);
  return { ...m, entersName: t.enters, exitsName: t.exits };
}

/////////////////////////////
// Curvy road path builder //
/////////////////////////////

/**
 * Build an SVG path `d` for a through-hex linear feature (e.g., road).
 * Path is LOCAL to the hex. For seamless stitching across borders,
 * it slightly overhangs past the apothem.
 *
 * Rules:
 *  - Adjacent edges (turn): curve toward the angular bisector.
 *  - Two apart: curve via the center (soft S).
 *  - Opposite edges: subtle perpendicular offset through center.
 *  - Dead-end (no exit): curve from entry toward center and cap at (0,0).
 *
 * @param {{enter?:{x:number,y:number}, exit?:{x:number,y:number}}} midpoints
 * @param {"N"|"NE"|"SE"|"S"|"SW"|"NW"|null} enterName
 * @param {"N"|"NE"|"SE"|"S"|"SW"|"NW"|null} exitName
 * @param {number} size - hex radius
 * @param {string[]} [order=SIDE_ORDER] - side ordering (clockwise)
 * @returns {string} SVG path data
 */
export function roadPath(midpoints, enterName, exitName, size, order = SIDE_ORDER) {
  const pIn  = midpoints.enter;
  const pOut = midpoints.exit ?? null;

  if (!pIn) return ''; // nothing to draw

  // Slight overhang to guarantee seam continuity across borders
  const tIn = 1.02;
  const tOut = 1.02;

  const aIn = enterName ? EDGE_ANGLE[enterName] : 0;
  const P1 = scaleVec(pIn, tIn);
  const P2 = pOut ? scaleVec(pOut, tOut) : { x: 0, y: 0 };

  // Dead-end: bend inward and cap at center
  if (!exitName || !pOut) {
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

/* Future extension stubs (same signature as roadPath):
export function pathPath(...)   {}
export function railPath(...)   {}
export function streamPath(...) {}
*/
