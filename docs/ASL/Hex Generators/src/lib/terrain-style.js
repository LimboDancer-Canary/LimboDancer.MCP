// lib/terrain-style.js
// Presentation tokens & helpers for ASL terrain rendering.
// - Distinguishes BASE terrains (ground layer) vs OVERLAYS (on top).
// - Centralizes base underpaint colors and pattern-id selection.
// - Exposes helpers used by renderers and the legend.

///////////////////////////////
// Canonical terrain classes //
///////////////////////////////

// Ground layer (what the hex "is"). These ids are used throughout.
export const BASE_TERRAINS = new Set([
  'open', 'woods', 'orchard', 'brush', 'grain', 'marsh', 'sand', 'scrub',
]);

// Overlay categories (sit on top of the base). More can be added later.
export const OVERLAY_TYPES = {
  // Pattern ids for buildings come from the "viz" <defs>:
  //   'wood', 'stone1' (1 level), 'stone2' (2 levels)
  building: new Set(['wood', 'stone1', 'stone2']),
};

// Convenience checks
export const isBaseTerrain   = (t) => BASE_TERRAINS.has(t);
export const isBuildingLayer = (id) => OVERLAY_TYPES.building.has(id);

///////////////////////////////
// Colors (underpaint fill)  //
///////////////////////////////

export const COLORS = {
  openBase:  '#90a955', // olive
  woodsBase: '#6f8f3d',
  orchardBase: '#7da35f',
  brushBase: '#b6c38a',
  grainBase: '#d9c178', // <-- ensure Grain is golden everywhere
  marshBase: '#9db9a4',
  sandBase:  '#e3cf9e',
  scrubBase: '#9fb389',

  labelDark:  '#111',
  labelLight: '#ffffff',
};

// Solid fill color under the pattern for a given base terrain.
export function baseFillColor(base) {
  switch (base) {
    case 'woods':   return COLORS.woodsBase;
    case 'orchard': return COLORS.orchardBase;
    case 'brush':   return COLORS.brushBase;
    case 'grain':   return COLORS.grainBase;   // key for Grain parity
    case 'marsh':   return COLORS.marshBase;
    case 'sand':    return COLORS.sandBase;
    case 'scrub':   return COLORS.scrubBase;
    default:        return COLORS.openBase;    // 'open' and unknowns
  }
}

// Label color chosen for contrast against the base fill.
export function labelColor(base) {
  // Woods and darker greens benefit from light labels; others go dark.
  switch (base) {
    case 'woods':
      return COLORS.labelLight;
    default:
      return COLORS.labelDark;
  }
}

///////////////////////////////
// Pattern id selection      //
///////////////////////////////

// v39 base terrain patterns (must match ids defined in terrain-defs.js)
const PATTERN_BY_BASE = {
  open:    'openGroundPattern',
  woods:   'woodsPattern',
  orchard: 'orchardPattern',
  brush:   'brushPattern',
  grain:   'grainPattern',
  marsh:   'marshPattern',
  sand:    'sandPattern',
  scrub:   'scrubPattern',
};

export function patternIdForBase(base) {
  return PATTERN_BY_BASE[base] || PATTERN_BY_BASE.open;
}

// Building overlay → viz pattern id.
// Accepts either a string id ('wood'|'stone1'|'stone2')
// or an object like { type:'stone'|'wood', levels:1|2 }.
export function patternIdForBuilding(building) {
  if (!building) return null;

  if (typeof building === 'string') {
    const id = building.toLowerCase();
    return OVERLAY_TYPES.building.has(id) ? id : null;
  }

  const type = (building.type || '').toLowerCase();
  const lvl  = Number(building.levels || building.level || 1);

  if (type === 'wood') return 'wood';
  if (type === 'stone') return lvl >= 2 ? 'stone2' : 'stone1';

  return null;
}

///////////////////////////////
// Normalization & tracking  //
///////////////////////////////

// Convert a template/base string into a canonical base id in BASE_TERRAINS.
export function normalizeBase(templateOrString) {
  const raw = typeof templateOrString === 'string'
    ? templateOrString
    : (templateOrString?.baseTerrain || templateOrString?.base || '');

  const s = String(raw).trim().toLowerCase();

  // Common aliases from UI/JSON
  if (s === 'openground' || s === 'open ground' || s === 'open') return 'open';
  if (s === 'light woods' || s === 'lightwoods' || s === 'woods') return 'woods';
  if (s === 'orchard') return 'orchard';
  if (s === 'brush')   return 'brush';
  if (s === 'grain')   return 'grain';
  if (s === 'marsh')   return 'marsh';
  if (s === 'sand')    return 'sand';
  if (s === 'scrub')   return 'scrub';

  // Fallback to open so we always render something
  return 'open';
}

/**
 * Track which base/overlay patterns were used for legend construction.
 * Mutates the provided `used` set object in-place:
 *   used = { bases: Set<string>, buildings: Set<string> }
 */
export function trackUsage(used, template) {
  if (!used) return;

  const base = normalizeBase(template);
  used.bases?.add(base);

  const bpid = patternIdForBuilding(template?.building);
  if (bpid) used.buildings?.add(bpid);
}
