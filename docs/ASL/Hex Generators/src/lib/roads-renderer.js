// lib/roads-renderer.js
// Curvy, stitched road rendering for flat-top hexes (N=0).
// Geometry is LOCAL to the hex (center at 0,0). Callers must translate
// to the hex center before drawing (we do that via gTranslate here).

import { midpointsFromSides, roadPath } from './linear-features.js';
import { gTranslate, path } from './svg-scene.js';
import { edgeName } from './schema.js';

/** Road stroke styling tokens (feel free to tweak). */
export const ROAD_STYLE = {
  baseStroke: '#666',
  baseWidth: 4,
  baseOpacity: 0.85,
  topStroke: '#c8c8c8',
  topWidth: 2.4,
  linecap: 'round',
  linejoin: 'round',
};

/**
 * Draw a single road segment inside one hex.
 * Coordinates are LOCAL to the hex (we translate a <g> to the hex center).
 *
 * @param {SVGSVGElement} svg
 * @param {{x:number,y:number}} centerPx - hex center in PIXELS
 * @param {"N"|"NE"|"SE"|"S"|"SW"|"NW"} entryName
 * @param {"N"|"NE"|"SE"|"S"|"SW"|"NW"|null|undefined} exitName
 * @param {number} size - hex radius (px)
 * @param {string[]} order - side order (clockwise)
 * @param {Partial<typeof ROAD_STYLE>} [style]
 * @returns {SVGGElement} group containing the two strokes
 */
export function drawRoad(svg, centerPx, entryName, exitName, size, order, style = {}) {
  const s = { ...ROAD_STYLE, ...style };
  const mps = midpointsFromSides(entryName, exitName, size); // LOCAL coords
  const d = roadPath({ enter: mps.enter, exit: mps.exit || undefined }, entryName, exitName, size, order);

  const g = gTranslate(svg, centerPx.x, centerPx.y);

  // Base stroke (darker, thicker)
  path(g, d, {
    fill: 'none',
    stroke: s.baseStroke,
    'stroke-width': s.baseWidth,
    'stroke-linecap': s.linecap,
    'stroke-linejoin': s.linejoin,
    opacity: s.baseOpacity,
  });

  // Top stroke (lighter, thinner) for beveled look
  path(g, d, {
    fill: 'none',
    stroke: s.topStroke,
    'stroke-width': s.topWidth,
    'stroke-linecap': s.linecap,
    'stroke-linejoin': s.linejoin,
  });

  return g;
}

/**
 * Draw roads described directly on a template (legacy keys).
 * Supports either `template.linearFeature` OR `template.road`.
 *
 * @param {SVGSVGElement} svg
 * @param {{x:number,y:number}} centerPx
 * @param {any} template
 * @param {number} size
 * @param {string[]} order
 * @param {Partial<typeof ROAD_STYLE>} [style]
 */
export function drawRoadsForTemplate(svg, centerPx, template, size, order, style) {
  const lf = template?.linearFeature || template?.road || null;
  if (!lf) return;

  // Accepts entryEdge/exitEdge as names or indices/strings
  const entryName = edgeName(lf.entryEdge, order);
  const exitName  = edgeName(lf.exitEdge,  order);

  if (entryName) drawRoad(svg, centerPx, entryName, exitName, size, order, style);
}

/**
 * Draw a road for a traversal-like object (new format or legacy override).
 * Supports fields: { enters, exits } (names or indices).
 *
 * @param {SVGSVGElement} svg
 * @param {{x:number,y:number}} centerPx
 * @param {{enters:any, exits:any}} traversal
 * @param {number} size
 * @param {string[]} order
 * @param {Partial<typeof ROAD_STYLE>} [style]
 */
export function drawRoadsForTraversal(svg, centerPx, traversal, size, order, style) {
  const entryName = edgeName(traversal?.enters, order);
  const exitName  = edgeName(traversal?.exits,  order);
  if (entryName) drawRoad(svg, centerPx, entryName, exitName, size, order, style);
}

/**
 * Optional registry for future feature types (streams, rails, etc.).
 * Consumers can extend/replace entries without changing the renderer.
 */
export const LinearRenderers = {
  road: drawRoad,
  // stream: (svg, centerPx, entryName, exitName, size, order, style) => {...},
  // rail:   ...
};
