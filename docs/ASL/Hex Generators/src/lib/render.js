// lib/render.js
// High-level renderer for the ASL visualizer.
// Consumes JSON board data and paints an SVG into a container.
//
// Usage:
//   import { draw } from './lib/render.js'
//   draw(containerEl, data, { showRoads:true, showLabels:true, showGrid:true })
//
// Dependencies are split into small, testable modules.

import { hexPolygonPoints } from './hex-geom.js';
import { hexPos, boardCanvasSize, getHexSize } from './layout.js';
import { ensureSvg, ensureLayers, polygon, text } from './svg-scene.js';
import {
  orderFromJSON, mapSize, defaultTemplate, parseCoord, cid
} from './schema.js';
import {
  normalizeBase, baseFillColor, patternIdForBase, patternIdForBuilding,
  labelColor, trackUsage
} from './terrain-style.js';
import { drawLegend } from './legend.js';
import {
  drawRoadsForTemplate, drawRoadsForTraversal
} from './roads-renderer.js';

//////////////////////
// Render settings  //
//////////////////////

const GRID = {
  stroke: 'rgba(60,60,60,0.45)',
  width: 0.8,
};

const LABEL = {
  dy: 4,
  size: 10,
  weight: 600,
};

//////////////////////
// Public API       //
//////////////////////

/**
 * Draw the board into an SVG appended to `container`.
 *
 * @param {HTMLElement} container
 * @param {any} data - ASL board JSON
 * @param {{showRoads?:boolean, showLabels?:boolean, showGrid?:boolean}} [opts]
 */
export function draw(container, data, opts = {}) {
  const showRoads  = opts.showRoads  ?? true;
  const showLabels = opts.showLabels ?? true;
  const showGrid   = opts.showGrid   ?? true;

  // 1) Validate schema basics and compute canvas
  const { w: W, h: H } = mapSize(data);
  if (!Number.isFinite(W) || !Number.isFinite(H)) {
    throw new Error('Missing or invalid map.dimensions width/height');
  }

  const { template: defaultT } = defaultTemplate(data);
  if (!defaultT) {
    throw new Error('defaultTemplateId not found in hexTemplates');
  }

  const order = orderFromJSON(data);
  const size  = getHexSize();

  const { width: svgW, height: svgH } = boardCanvasSize(W, H, { size });

  // 2) Prepare SVG + layers (terrain, roads, labels, legend)
  const svg = ensureSvg(container, svgW, svgH, { withDefs: true, defsFlavors: ['v39', 'viz'] });
  const layers = ensureLayers(svg, ['terrain', 'roads', 'labels', 'legend']);

  // Track used terrain types/buildings for the legend
  const used = { bases: new Set(), buildings: new Set() };

  // 3) Paint full grid with default template
  for (let r = 0; r < H; r++) {
    for (let c = 0; c < W; c++) {
      const hexId = cid(c, r);
      const center = hexPos(c, r, size);
      paintHex(layers, center, size, defaultT, hexId, { showGrid, showLabels }, used);
    }
  }

  // 4) Object-form overrides (data.hexes)
  const hexMap = data.hexes || {};
  for (const hexId of Object.keys(hexMap)) {
    const cr = parseCoord(hexId);
    if (!cr) continue;

    const center = hexPos(cr.c, cr.r, size);
    const t = hexMap[hexId] || defaultT;
    paintHex(layers, center, size, t, hexId, { showGrid, showLabels }, used);

    if (showRoads) {
      drawRoadsForTemplate(layers.roads, center, t, size, order);
    }
  }

  // 5) Array-form overrides (map.individualHexes)
  const overrides = data.map?.individualHexes || [];
  for (const h of overrides) {
    if (!h?.hexId) continue;
    const cr = parseCoord(h.hexId);
    if (!cr) continue;

    const center = hexPos(cr.c, cr.r, size);
    const baseT = data.hexTemplates?.[h.templateId] || defaultT;
    const merged = Object.assign(
      {},
      baseT,
      h.overrides && !Array.isArray(h.overrides) ? h.overrides : {}
    );

    paintHex(layers, center, size, merged, h.hexId, { showGrid, showLabels }, used);

    if (showRoads) {
      // New format
      for (const lt of (merged.linearTraversals || [])) {
        drawRoadsForTraversal(layers.roads, center, lt, size, order);
      }
      // Legacy overrides array
      if (Array.isArray(h.overrides)) {
        for (const o of h.overrides) {
          if ((o.type || '').toLowerCase() === 'road') {
            drawRoadsForTraversal(layers.roads, center, o, size, order);
          }
        }
      }
    }
  }

  // 6) Legend (after we know what's present)
  // Title above board center for context, then legend block on the right.
  text(svg, svgW / 2, 40, 'ASL Board — Generated from JSON', {
    'text-anchor': 'middle',
    'font-size': 12,
    fill: '#6b7280',
  });
  drawLegend(layers.legend, svgW - 220, 70, used, { title: 'Map Legend' });
}

//////////////////////
// Internal helpers //
//////////////////////

/**
 * Draw a composite hex: solid base + pattern overlay + label (optional).
 * Tracks usage for the legend.
 *
 * @param {{terrain:SVGGElement, roads:SVGGElement, labels:SVGGElement}} layers
 * @param {{x:number,y:number}} center
 * @param {number} size
 * @param {any} template
 * @param {string} hexId
 * @param {{showGrid:boolean, showLabels:boolean}} flags
 * @param {{bases:Set<string>, buildings:Set<string>}} used
 */
function paintHex(layers, center, size, template, hexId, flags, used) {
  const base = normalizeBase(template);
  const pts  = hexPolygonPoints(center.x, center.y, size);

  // 1) Base solid fill (olive/woods) with optional grid stroke
  polygon(layers.terrain, pts, {
    fill: baseFillColor(base),
    stroke: flags.showGrid ? GRID.stroke : 'none',
    'stroke-width': flags.showGrid ? GRID.width : 0,
    'data-hex': hexId,
  });

  // 2) Pattern overlay: either building (viz set) or base terrain (v39 set)
  const bpid = patternIdForBuilding(template?.building);
  const pid  = bpid || patternIdForBase(base);
  const opacity = pid === 'openGroundPattern' ? 0.35 : 1.0;

  polygon(layers.terrain, pts, {
    fill: `url(#${pid})`,
    'fill-opacity': opacity,
  });

  // 3) Track for legend
  trackUsage(used, template);

  // 4) Label
  if (flags.showLabels) {
    text(layers.labels, center.x, center.y + LABEL.dy, hexId.replace(/^1/, ''), {
      'text-anchor': 'middle',
      fill: labelColor(base),
      'font-size': LABEL.size,
      'font-weight': LABEL.weight,
    });
  }
}
