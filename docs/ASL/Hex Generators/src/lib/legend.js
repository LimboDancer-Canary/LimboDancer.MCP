// lib/legend.js
// Dynamic legend that lists only terrains/buildings actually present on the board.
// Usage:
//   drawLegend(svg, x, y, used, { title: 'Map Legend' })
//
// `used` is expected to be:
//   { bases: Set<string>, buildings: Set<string> }

import { hexPolygonPoints } from './hex-geom.js';
import { baseFillColor, patternIdForBase } from './terrain-style.js';
import { polygon, text } from './svg-scene.js';

// Stable ordering for legend rows
export const BASE_ORDER = ['open','woods','orchard','brush','grain','marsh','sand','scrub'];
export const BASE_LABEL = {
  open:'Open Ground',
  woods:'Woods',
  orchard:'Orchard',
  brush:'Brush',
  grain:'Grain',
  marsh:'Marsh',
  sand:'Sand',
  scrub:'Scrub',
};

export const BUILD_ORDER = ['stone2','stone1','wood'];
export const BUILD_LABEL = {
  stone2:'Stone Building (2 levels)',
  stone1:'Stone Building (1 level)',
  wood:'Wooden Building',
};

/**
 * Draw the legend block.
 * @param {SVGSVGElement} svg
 * @param {number} x - left anchor for the legend icon (mini hex center)
 * @param {number} y - top baseline for first row
 * @param {{bases:Set<string>, buildings:Set<string>}} used
 * @param {{title?:string, titleOffset?:number, rowHeight?:number, iconSize?:number, labelDx?:number}} [opts]
 */
export function drawLegend(
  svg,
  x,
  y,
  used,
  { title = 'Map Legend', titleOffset = -16, rowHeight = 28, iconSize = 14, labelDx = 26 } = {}
) {
  // Header
  text(svg, x, y + titleOffset, title, { 'font-size': 16, 'font-weight': 600 });

  let row = 0;
  const addRow = (baseKey, patternId, label) => {
    const cy = y + row * rowHeight;
    // mini-hex points
    const pts = hexPolygonPoints(x, cy, iconSize);

    // base color hex
    polygon(svg, pts, {
      fill: baseFillColor(baseKey),
      stroke: '#333',
      'stroke-width': 0.7,
    });

    // overlay pattern (reduced opacity for open ground for readability)
    const op = patternId === 'openGroundPattern' ? 0.35 : 1.0;
    polygon(svg, pts, {
      fill: `url(#${patternId})`,
      'fill-opacity': op,
    });

    // label
    text(svg, x + labelDx, cy + 4, label, { 'font-size': 12 });

    row++;
  };

  // Base terrains present
  for (const base of BASE_ORDER) {
    if (used.bases?.has(base)) {
      addRow(base, patternIdForBase(base), BASE_LABEL[base] || base);
    }
  }

  // Buildings present (render on open base)
  for (const bid of BUILD_ORDER) {
    if (used.buildings?.has(bid)) {
      addRow('open', bid, BUILD_LABEL[bid] || bid);
    }
  }
}
