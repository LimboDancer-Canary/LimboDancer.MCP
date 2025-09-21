// HexLab.js
// Matches asl-hex-lab.html (uses #hexSvg, .terrain-item, #showGrid/#showCoords/#showCenter)

import { ensureLayers, text } from './lib/svg-scene.js';
import { hexPolygonPoints } from './lib/hex-geom.js';
import { renderHex } from './lib/render.js';
import { SIDE_ORDER } from './lib/hex-geom.js';

// Small defs injector via window hook from svg-scene.js
function injectDefs(svg) {
  if (window?.ASL?.render?.defs?.createPatternDefs) {
    window.ASL.render.defs.createPatternDefs(svg, { flavors: ['v39', 'viz'] });
  }
}

function $(sel) { return document.querySelector(sel); }
function $all(sel) { return Array.from(document.querySelectorAll(sel)); }

// Map sidebar data-terrain → canonical base/building
const TERRAIN_MAP = {
  // Natural
  openGround: { base: 'open' },
  woods:      { base: 'woods' },
  lightWoods: { base: 'woods' },   // alias
  brush:      { base: 'brush' },
  orchard:    { base: 'orchard' },
  vineyard:   { base: 'orchard' }, // placeholder alias
  grain:      { base: 'grain' },
  marsh:      { base: 'marsh' },
  // Others shown in UI but not implemented yet will fall back to 'open'
  mudflat:    { base: 'open' },
  crag:       { base: 'open' },
  graveyard:  { base: 'open' },

  // Water features (show as open for now – streams/rivers are linear, coming later)
  stream: { base: 'open' },
  river:  { base: 'open' },
  canal:  { base: 'open' },
  pond:   { base: 'open' },
  lake:   { base: 'open' },
  ocean:  { base: 'open' },

  // Depressions / damaged / special (placeholders)
  gully:   { base: 'open' },
  valley:  { base: 'open' },
  shellholes: { base: 'open' },
  rubble:     { base: 'open' },
  debris:     { base: 'open' },
  runway:     { base: 'open' },
  villageTerrain:   { base: 'open' },
  preparedFireZone: { base: 'open' },

  // Building overlays (map to building patterns)
  'building-wooden-1': { base: 'open', building: { type: 'wood',  levels: 1 } },
  'building-wooden-2': { base: 'open', building: { type: 'wood',  levels: 2 } },
  'building-stone-1':  { base: 'open', building: { type: 'stone', levels: 1 } },
  'building-stone-2':  { base: 'open', building: { type: 'stone', levels: 2 } },
  'building-factory':  { base: 'open', building: { type: 'stone', levels: 1 } }, // placeholder
  'building-marketplace': { base: 'open', building: { type: 'stone', levels: 1 } },
  'building-rowhouse': { base: 'open', building: { type: 'stone', levels: 1 } },
  'building-church':   { base: 'open', building: { type: 'stone', levels: 2 } },
};

function labelForKey(key) {
  const el = document.querySelector(`.terrain-item[data-terrain="${key}"]`);
  return el ? el.textContent.trim() : key;
}

function parseViewBox(svg) {
  const vb = (svg.getAttribute('viewBox') || '0 0 60 52').split(/\s+/).map(Number);
  const [x, y, w, h] = vb.length === 4 ? vb : [0, 0, 60, 52];
  return { x, y, w, h };
}

function clearLayers(svg) {
  // Remove any previous layers and default content
  svg.querySelectorAll('g[id^="layer-"]').forEach(n => n.remove());
  const defaultGroup = $('#hexContent');
  if (defaultGroup) defaultGroup.remove();
  // Remove existing <defs>, we’ll re-inject fresh
  svg.querySelectorAll('defs').forEach(d => d.remove());
}

function drawCenterDot(layer, cx, cy) {
  const c = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
  c.setAttribute('cx', cx);
  c.setAttribute('cy', cy);
  c.setAttribute('r', 1.6);
  c.setAttribute('fill', '#d00');
  layer.appendChild(c);
}

function renderSelected(key) {
  const svg = $('#hexSvg');
  if (!svg) return;

  clearLayers(svg);
  injectDefs(svg);

  const { w, h } = parseViewBox(svg);
  const center = { x: w / 2, y: h / 2 };
  const size = w / 2; // matches initial polygon: width ~ 2*size

  const layers = ensureLayers(svg, ['terrain', 'roads', 'labels', 'legend']);

  // Terrain template from mapping (fallback to open)
  const pick = TERRAIN_MAP[key] || { base: 'open' };
  const template = {
    baseTerrain: pick.base || 'open',
    building: pick.building || null,
    linearTraversals: [],
  };

  // Grid/coords toggles from page
  const showGrid = $('#showGrid')?.checked ?? true;
  const showCoords = $('#showCoords')?.checked ?? false;
  const showCenter = $('#showCenter')?.checked ?? true;

  const used = { bases: new Set(), buildings: new Set() };
  renderHex(layers, center, size, template, 'A1', { showGrid, showLabels: showCoords }, used);

  if (showCenter) drawCenterDot(layers.labels, center.x, center.y);

  // Update header (no TypeScript non-null)
  const nameEl = $('#terrainName');
  if (nameEl) nameEl.textContent = labelForKey(key) || 'Terrain';
  const implemented = ['open','woods','orchard','brush','grain','marsh'].includes(template.baseTerrain) || template.building;
  const descEl = $('#terrainDescription');
  if (descEl) descEl.textContent = implemented
    ? 'Rendered with shared lib (base + pattern; buildings use viz patterns).'
    : 'Not yet implemented in renderer — shown as Open pending feature support.';
}

function main() {
  // Sidebar clicks
  $all('.terrain-item').forEach(item => {
    item.addEventListener('click', () => {
      $all('.terrain-item').forEach(n => n.classList.remove('active'));
      item.classList.add('active');
      renderSelected(item.getAttribute('data-terrain'));
    });
  });

  // Controls
  ['#showGrid', '#showCoords', '#showCenter'].forEach(id => {
    const el = $(id);
    if (el) el.addEventListener('change', () => {
      const active = document.querySelector('.terrain-item.active');
      const key = active?.getAttribute('data-terrain') || 'openGround';
      renderSelected(key);
    });
  });

  // Initial selection
  const first = document.querySelector('.terrain-item[data-terrain="openGround"]');
  if (first) {
    first.classList.add('active');
    renderSelected('openGround');
  }
}

document.addEventListener('DOMContentLoaded', main);
