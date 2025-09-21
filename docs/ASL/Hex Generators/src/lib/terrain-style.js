// lib/terrain-style.js
// Map game terrain → visual style (colors, pattern ids, label color).
// Also includes a tiny usage tracker to feed the dynamic legend.

////////////////////
// Design tokens  //
////////////////////

export const COLORS = {
  openBase:  '#90a955',  // olive base under most non-woods
  woodsBase: '#6f8f3d',  // darker green base under woods
  labelDark: '#111',
  labelLight:'#fff',
};

/////////////////////////////
// Terrain normalization   //
/////////////////////////////

/**
 * Normalize a template's base terrain to canonical keys we style against.
 * "OpenGround" / "Open" → "open"
 */
export function normalizeBase(template) {
  const base = (template?.baseTerrain || '').toLowerCase();
  if (base === 'openground' || base === 'open') return 'open';
  return base || 'open';
}

/////////////////////////////
// Colors & pattern ids    //
/////////////////////////////

/** Solid fill color laid under the pattern for readability. */
export function baseFillColor(base) {
  return base === 'woods' ? COLORS.woodsBase : COLORS.openBase;
}

/**
 * Pattern id for base terrain (matches defs in lib/terrain-defs.js, flavor "v39").
 * open → openGroundPattern (with reduced opacity in renderer)
 */
export function patternIdForBase(base) {
  switch (base) {
    case 'woods':   return 'woodsPattern';
    case 'orchard': return 'orchardPattern';
    case 'brush':   return 'brushPattern';
    case 'grain':   return 'grainPattern';
    case 'marsh':   return 'marshPattern';
    case 'sand':    return 'sandPattern';
    case 'scrub':   return 'scrubPattern';
    case 'open':
    default:        return 'openGroundPattern';
  }
}

/**
 * Pattern id for buildings (matches defs in lib/terrain-defs.js, flavor "viz").
 * {type:'stone', levels:2} → "stone2"; {type:'stone', levels:1} → "stone1"; {type:'wooden'} → "wood"
 */
export function patternIdForBuilding(building) {
  if (!building) return null;
  if (building.type === 'stone' && building.levels === 2) return 'stone2';
  if (building.type === 'stone' && building.levels === 1) return 'stone1';
  if (building.type === 'wooden') return 'wood';
  return null;
}

/** Label color chosen for contrast over the base fill/pattern. */
export function labelColor(base) {
  return base === 'woods' ? COLORS.labelLight : COLORS.labelDark;
}

/////////////////////////////
// Legend usage collection //
/////////////////////////////

/**
 * Track which terrains/buildings appear so the legend can list only used items.
 * Expects `used` to be `{ bases: Set<string>, buildings: Set<string> }`
 */
export function trackUsage(used, template) {
  const base = normalizeBase(template);
  const bpid = patternIdForBuilding(template?.building);
  if (bpid) used.buildings.add(bpid);
  else used.bases.add(base);
}
