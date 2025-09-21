// lib/schema.js
// JSON schema helpers & normalization for ASL board data.

import { SIDE_ORDER } from './hex-geom.js';
// Pull canonical lists (labels, ids) from terrain-defs when available.
// This avoids duplicating terrain/hexside/linear enumerations across apps.
let CANON = null;
try {
  // Optional import – if not present, we’ll use fallbacks below.
  ({ CANON } = await import('./terrain-defs.js'));
} catch (_) {
  /* no-op: fall back to local defaults */
}

/** Return clockwise side order from JSON (falls back to N,NE,SE,S,SW,NW). */
export function orderFromJSON(data, fallback = SIDE_ORDER) {
  const ord = data?.map?.renderHints?.sideIndexing?.orderClockwise;
  return Array.isArray(ord) && ord.length === 6 ? ord.slice() : fallback.slice();
}

/** Map dimensions {w,h} (columns x rows). */
export function mapSize(data) {
  const w = data?.map?.dimensions?.width;
  const h = data?.map?.dimensions?.height;
  return { w: Number(w) || 0, h: Number(h) || 0 };
}

/** Default template lookup (id + template object). */
export function defaultTemplate(data) {
  const id = data?.map?.defaultTemplateId || 'open';
  const t  = data?.hexTemplates?.[id] || null;
  return { id, template: t };
}

/** A1→{c,r} and helpers … (existing exports) **/
export function parseCoord(id) { /* … existing code … */ }
export function cid(c, r, boardPrefix = '1') { /* … existing code … */ }
export function edgeName(edge, order = SIDE_ORDER) { /* … existing code … */ }

/* ------------------------------------------------------------------ */
/*                        UI Option Catalogs                          */
/* ------------------------------------------------------------------ */

// Fallbacks ensure immediate usability if CANON isn’t exported yet.
const FALLBACK = {
  bases: [
    { id: 'open',   label: 'Open Ground' },
    { id: 'woods',  label: 'Woods' },
    { id: 'orchard',label: 'Orchard' },
    { id: 'brush',  label: 'Brush' },
    { id: 'grain',  label: 'Grain' },
    { id: 'marsh',  label: 'Marsh' },
    { id: 'sand',   label: 'Sand' },
    { id: 'scrub',  label: 'Scrub' },
  ],
  hexsides: [
    { id: 'wall',  label: 'Wall' },
    { id: 'hedge', label: 'Hedge' },
  ],
  linear: [
    { id: 'road', label: 'Road', subtypes: ['paved', 'dirt', 'sunken', 'elevated'] },
    // future: { id: 'railroad', label:'Railroad' }, { id:'stream', label:'Stream' }
  ],
};

/** Canonical base terrain list for pickers/legends (id + label). */
export function baseTerrainCatalog() {
  return (CANON?.bases && Array.isArray(CANON.bases) && CANON.bases.length)
    ? CANON.bases
    : FALLBACK.bases;
}

/** Canonical hexside features (e.g., wall/hedge). */
export function hexsideCatalog() {
  return (CANON?.hexsides && Array.isArray(CANON.hexsides) && CANON.hexsides.length)
    ? CANON.hexsides
    : FALLBACK.hexsides;
}

/** Canonical linear feature types (roads, etc.), with optional subtypes. */
export function linearTypeCatalog() {
  return (CANON?.linear && Array.isArray(CANON.linear) && CANON.linear.length)
    ? CANON.linear
    : FALLBACK.linear;
}

/** One-shot bundle the UI can consume to populate selectors. */
export function schemaListsForUI() {
  return {
    baseTerrains: baseTerrainCatalog(),
    hexsides:     hexsideCatalog(),
    linearTypes:  linearTypeCatalog(),
  };
}
