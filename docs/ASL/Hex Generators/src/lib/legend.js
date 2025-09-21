// lib/legend.js
// Dynamic legend driven by canonical terrain keys & pattern ids.
// Usage:
//   drawLegend(svg, x, y, used, { title: 'Map Legend' })
//
// `used` is expected to be:
//   { bases: Set<string>, buildings: Set<string> }
//
// Canonical base keys come from terrain-style normalization
// (e.g., 'open','woods','orchard','brush','grain','marsh','sand','scrub').
// Pattern ids come from terrain-defs.js flavors: "v39" for base, "viz" for buildings.

import { hexPolygonPoints } from './hex-geom.js';
import { baseFillColor, patternIdForBase, labelColor } from './terrain-style.js';
import { polygon, text } from './svg-scene.js';

// Stable ordering so the legend is predictable across apps
const BASE_ORDER = ['open','woods','orchard','brush','grain','marsh','sand','scrub'];

// Canonical, user-facing labels for base terrains (derived from rulebook names)
const BASE_LABEL = {
  open:    'Open Ground',
  woods:   'Woods',
  orchard: 'Orchard',
  brush:   'Brush',
  grain:   'Grain',
  marsh:   'Marsh',
  sand:    'Sand',
  scrub:   'Scrub',
};

// Buildings: ids match terrain-defs.js "viz" flavor patterns (stone2/stone1/wood)
const BUILD_ORDER = ['stone2','stone1','wood'];
const BUILD_LABEL = {
  stone2: 'Stone Building (2 levels)',
  stone1: 'Stone Building (1 level)',
  wood:   'Wooden Building',
};

/**
 * Draw the legend block.
 * @param {SVGSVGElement|SVGGElement} svg
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

    // mini-hex icon
    const pts = hexPolygonPoints(x, cy, iconSize);

    // base solid for readability (tokens from terrain-style)
    polygon(svg, pts, {
      fill: baseFillColor(baseKey),
      stroke: '#333',
      'stroke-width': 0.7,
    });

    // overlay pattern (ids from terrain-defs.js)
    const isOpen = patternId === 'openGroundPattern';
    polygon(svg, pts, {
      fill: `url(#${patternId})`,
      'fill-opacity': isOpen ? 0.35 : 1.0,
    });

    // label
    text(svg, x + labelDx, cy + 4, label, {
      'font-size': 12,
      fill: labelColor(baseKey),
    });

    row++;
  };

  // Base terrains actually present (order-filtered for stability)
  for (const base of BASE_ORDER) {
    if (used?.bases?.has(base)) {
      addRow(base, patternIdForBase(base), BASE_LABEL[base] || base);
    }
  }

  // Buildings actually present (rendered on an 'open' base swatch)
  for (const bid of BUILD_ORDER) {
    if (used?.buildings?.has(bid)) {
      addRow('open', bid, BUILD_LABEL[bid] || bid);
    }
  }
}

export default drawLegend;
