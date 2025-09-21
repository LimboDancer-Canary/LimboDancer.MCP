// lib/svg-scene.js
// Minimal SVG scene helpers for the ASL visualizer/viewer.
// - ensureSvg(container, width, height, options)
// - g(svg, attrs), gTranslate(svg, x, y, attrs)
// - polygon(svg, points, attrs), path(svg, d, attrs), text(svg, x, y, str, attrs)
// - ensureLayers(svg, ['terrain','roads','labels','legend'])
//
// Defaults match the current visualizer (injects v39 + viz terrain defs).

import { buildTerrainDefs } from './terrain-defs.js';

export const SVG_NS = 'http://www.w3.org/2000/svg';

/**
 * Create/replace an <svg> in the container with a given viewBox.
 * By default injects both v39 and viz terrain <defs>.
 *
 * @param {HTMLElement} container
 * @param {number} width
 * @param {number} height
 * @param {object} [opts]
 * @param {boolean} [opts.clear=true]
 * @param {boolean} [opts.withDefs=true]
 * @param {string[]} [opts.defsFlavors=['v39','viz']]
 * @param {boolean} [opts.setXmlns=true]
 * @returns {SVGSVGElement}
 */
export function ensureSvg(
  container,
  width,
  height,
  { clear = true, withDefs = true, defsFlavors = ['v39', 'viz'], setXmlns = true } = {}
) {
  if (clear) container.innerHTML = '';
  const svg = create('svg');
  setAttrs(svg, { viewBox: `0 0 ${width} ${height}` });
  if (setXmlns) setAttrs(svg, { xmlns: SVG_NS });

  if (withDefs) {
    const defsHtml = defsFlavors.map(f => buildTerrainDefs({ flavor: f })).join('');
    svg.insertAdjacentHTML('afterbegin', defsHtml);
  }

  container.appendChild(svg);
  return svg;
}

/** Create an SVG element with namespace. */
export function create(tag) {
  return document.createElementNS(SVG_NS, tag);
}

/** Set multiple attributes on an element. */
export function setAttrs(el, attrs = {}) {
  for (const [k, v] of Object.entries(attrs)) {
    if (v !== undefined && v !== null) el.setAttribute(k, String(v));
  }
  return el;
}

/** Append a <g> to svg. */
export function g(svg, attrs = {}) {
  const el = create('g');
  setAttrs(el, attrs);
  svg.appendChild(el);
  return el;
}

/** Append a translated <g> to svg. */
export function gTranslate(svg, x, y, attrs = {}) {
  const el = g(svg, { transform: `translate(${x},${y})`, ...attrs });
  return el;
}

/**
 * Ensure named layer groups exist (e.g., 'terrain','roads','labels','legend').
 * Returns a map of { name: <g> }.
 */
export function ensureLayers(svg, names = []) {
  const out = {};
  for (const name of names) {
    const id = `layer-${name}`;
    let el = svg.querySelector(`g#${id}`);
    if (!el) {
      el = create('g');
      el.id = id;
      svg.appendChild(el);
    }
    out[name] = el;
  }
  return out;
}

/**
 * Append a <polygon>. `points` may be a string or an array of "x,y" pairs.
 * Returns the created element.
 */
export function polygon(svg, points, attrs = {}) {
  const el = create('polygon');
  el.setAttribute('points', Array.isArray(points) ? points.join(' ') : String(points));
  setAttrs(el, attrs);
  svg.appendChild(el);
  return el;
}

/** Append a <path> with `d`. Returns the created element. */
export function path(svg, d, attrs = {}) {
  const el = create('path');
  setAttrs(el, { d, ...attrs });
  svg.appendChild(el);
  return el;
}

/** Append a <text>. Returns the created element. */
export function text(svg, x, y, str, attrs = {}) {
  const el = create('text');
  el.textContent = String(str);
  setAttrs(el, { x, y, ...attrs });
  svg.appendChild(el);
  return el;
}

/**
 * Insert the terrain <defs> (patterns/filters) into an existing SVG once.
 * Mirrors the injection that ensureSvg() does, but for pre-existing <svg>.
 * @param {SVGSVGElement} svg
 * @param {{flavors?:string[]}} [opts]
 */
export function createPatternDefs(svg, { flavors = ['v39','viz'] } = {}) {
  if (!svg) return null;
  const defsHtml = (flavors || []).map(f => buildTerrainDefs({ flavor: f })).join('');
  if (!defsHtml) return null;
  svg.insertAdjacentHTML('afterbegin', defsHtml);
  const defsList = svg.querySelectorAll('defs');
  return defsList.length ? defsList[defsList.length - 1] : null;
}

// Browser-global convenience for Hex Lab
if (typeof window !== 'undefined') {
  window.ASL = window.ASL || {};
  window.ASL.render = window.ASL.render || {};
  window.ASL.render.defs = window.ASL.render.defs || {};
  window.ASL.render.defs.createPatternDefs = createPatternDefs;
}
