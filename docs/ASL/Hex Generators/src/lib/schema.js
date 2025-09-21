// lib/schema.js
// JSON schema helpers & normalization for ASL board data.

/* eslint-disable unicorn/number-literal-case */
import { SIDE_ORDER } from './hex-geom.js';

//////////////////////////
// Map / template access //
//////////////////////////

/**
 * Resolve clockwise side order from JSON (falls back to N,NE,SE,S,SW,NW).
 * @param {any} data
 * @param {string[]} [fallback=SIDE_ORDER]
 * @returns {string[]} orderClockwise
 */
export function orderFromJSON(data, fallback = SIDE_ORDER) {
  return data?.map?.renderHints?.sideIndexing?.orderClockwise || fallback;
}

/**
 * Get map size as numbers (NaN if absent).
 * @param {any} data
 * @returns {{w:number,h:number}}
 */
export function mapSize(data) {
  const w = Number(data?.map?.dimensions?.width);
  const h = Number(data?.map?.dimensions?.height);
  return { w, h };
}

/**
 * Get default template id + object.
 * @param {any} data
 * @returns {{id:string|undefined, template:any}}
 */
export function defaultTemplate(data) {
  const id = data?.map?.defaultTemplateId;
  const template = id ? data?.hexTemplates?.[id] : undefined;
  return { id, template };
}

/////////////////////////////
// Hex ID parse/formatting //
/////////////////////////////

/**
 * Convert column letters (A, B, ... Z, AA, AB, ...) to zero-based index.
 * @param {string} letters
 * @returns {number}
 */
export function colLettersToIndex(letters) {
  let n = 0;
  const s = String(letters).trim().toUpperCase();
  for (let i = 0; i < s.length; i++) {
    const c = s.charCodeAt(i);
    if (c < 65 || c > 90) return NaN;
    n = n * 26 + (c - 64); // A->1 ... Z->26
  }
  return n - 1; // zero-based
}

/**
 * Convert zero-based column index to letters (A..Z, AA..).
 * @param {number} idx
 * @returns {string}
 */
export function letters(idx) {
  let n = Math.floor(idx);
  if (!Number.isFinite(n) || n < 0) return '';
  let s = '';
  // Excel-style base-26 without zero
  while (n >= 0) {
    s = String.fromCharCode(65 + (n % 26)) + s;
    n = Math.floor(n / 26) - 1;
  }
  return s;
}

/**
 * Parse a hex ID like "1A1" or "A1" → { c, r } (zero-based).
 * Leading board prefix "1" is optional and ignored.
 * @param {string} id
 * @returns {{c:number,r:number}|null}
 */
export function parseCoord(id) {
  const m = String(id).trim().toUpperCase().match(/^1?([A-Z]+)(\d+)$/);
  if (!m) return null;
  const c = colLettersToIndex(m[1]);
  const r = parseInt(m[2], 10) - 1;
  if (!Number.isFinite(c) || !Number.isFinite(r) || r < 0) return null;
  return { c, r };
}

/**
 * Build a hex ID like "1A1" from zero-based coords.
 * @param {number} c - column index (0-based)
 * @param {number} r - row index (0-based)
 * @param {string} [boardPrefix='1'] - leading board id/prefix
 * @returns {string}
 */
export function cid(c, r, boardPrefix = '1') {
  return `${boardPrefix}${letters(c)}${r + 1}`;
}

//////////////////////////////
// Edge / linear feature IO //
//////////////////////////////

/**
 * Normalize an edge indicator from JSON to a side name in `order`.
 * Accepts: "N","NE","SE","S","SW","NW" (case-insensitive) OR a number/index.
 * Numeric strings are allowed (e.g., "2").
 * Returns null if not recognized.
 * @param {string|number|null|undefined} edge
 * @param {string[]} order
 * @returns {string|null}
 */
export function edgeName(edge, order = SIDE_ORDER) {
  if (edge === null || edge === undefined || edge === '') return null;

  if (typeof edge === 'string') {
    const s = edge.trim().toUpperCase();
    // Named side?
    if (SIDE_ORDER.includes(s)) return s;
    // Numeric string?
    const n = Number(s);
    if (Number.isFinite(n)) {
      const i = ((n % 6) + 6) % 6;
      return order[i];
    }
    return null;
  }

  if (typeof edge === 'number') {
    const i = ((edge % 6) + 6) % 6;
    return order[i];
  }

  return null;
}
