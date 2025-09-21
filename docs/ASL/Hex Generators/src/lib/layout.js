// lib/layout.js
// Layout helpers for flat-top hex boards (SVG Y+ downward).
// Coordinates: column-major with odd-column vertical offsets.
// Matches HexMap.js expectations (N = 0, flat-top).

//////////////////////
// Sizing constants //
//////////////////////

/** Default hex radius (px). */
let HEX_SIZE = 30;

/** Get current hex radius. */
export function getHexSize() { return HEX_SIZE; }

/** Set hex radius (px). Updates dependent helpers that accept default size. */
export function setHexSize(px) {
  const n = Number(px);
  if (Number.isFinite(n) && n > 0) HEX_SIZE = n;
}

/** Height of a flat-top hex for a given radius. */
export function hexHeight(size = HEX_SIZE) {
  return Math.sqrt(3) * size;
}

/** Apothem (center → side distance) for a given radius. */
export function hexApothem(size = HEX_SIZE) {
  return size * Math.cos(Math.PI / 6); // cos(30°)
}

/////////////////////////
// Grid→pixel mapping  //
/////////////////////////

/**
 * Pixel position (center) for a hex at column c, row r.
 * Uses "odd-q" style vertical offset: odd columns shifted down by 0.5 hex height.
 *
 * @param {number} c - column index (0-based)
 * @param {number} r - row index (0-based)
 * @param {number} [size=HEX_SIZE] - hex radius
 * @param {number} [originX=80] - left padding (px)
 * @param {number} [originY=80] - top padding (px)
 * @returns {{x:number,y:number}}
 */
export function hexPos(c, r, size = HEX_SIZE, originX = 80, originY = 80) {
  const H = hexHeight(size);
  return {
    x: c * size * 1.5 + originX,
    y: r * H + (c % 2) * (H * 0.5) + originY,
  };
}

////////////////////////////
// Canvas size / viewBox  //
////////////////////////////

/**
 * Compute a comfortable SVG canvas size for a board of W×H hexes.
 * Adds padding around the content so labels/legend don’t clip.
 *
 * @param {number} W - number of columns
 * @param {number} H - number of rows
 * @param {object} [opts]
 * @param {number} [opts.size=HEX_SIZE]
 * @param {{x:number,y:number}} [opts.pad={x:160,y:120}]
 * @param {number} [opts.minW=900] - minimum canvas width (px)
 * @param {number} [opts.minH=600] - minimum canvas height (px)
 * @returns {{width:number,height:number}}
 */
export function boardCanvasSize(
  W,
  H,
  { size = HEX_SIZE, pad = { x: 160, y: 120 }, minW = 900, minH = 600 } = {}
) {
  const Hh = hexHeight(size);
  // Width: each new column adds 1.5*size horizontally; add a little extra room.
  const width = Math.max(pad.x + W * size * 1.6, minW);
  // Height: H rows plus half a hex height margin at the bottom.
  const height = Math.max(pad.y + H * Hh + Hh, minH);
  return { width, height };
}
