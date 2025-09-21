// HexMap.js
// Matches asl-visualizer.html (uses #stage, #json, #file, #btn-load/#btn-render/#btn-clear,
// and toggles #toggle-roads/#toggle-labels/#toggle-grid)

import { draw } from './lib/render.js';

const el = (s) => document.querySelector(s);

function readTextAreaJson() {
  const t = el('#json');
  if (!t) return null;
  const raw = t.value.trim();
  if (!raw) return null;
  return JSON.parse(raw);
}

async function readFileInputJson() {
  const inp = el('#file');
  if (!inp || !inp.files || !inp.files[0]) return null;
  const txt = await inp.files[0].text();
  return JSON.parse(txt);
}

function flags() {
  return {
    showRoads:  el('#toggle-roads')?.checked ?? true,
    showLabels: el('#toggle-labels')?.checked ?? true,
    showGrid:   el('#toggle-grid')?.checked ?? true,
  };
}

function renderBoard(data) {
  const stage = el('#stage');
  if (!stage) throw new Error('#stage container not found');
  draw(stage, data, flags());
}

function showError(msg) {
  const m = el('#msg');
  if (!m) return;
  m.textContent = msg;
  m.style.display = msg ? 'block' : 'none';
}

function wireUI() {
  const btnLoad   = el('#btn-load');
  const btnRender = el('#btn-render');
  const btnClear  = el('#btn-clear');

  if (btnLoad) {
    btnLoad.addEventListener('click', async () => {
      try {
        const data = await readFileInputJson();
        if (!data) return;
        el('#json').value = JSON.stringify(data, null, 2);
        renderBoard(data);
        showError('');
      } catch (e) {
        console.error(e);
        showError('Failed to load JSON file.');
      }
    });
  }

  if (btnRender) {
    btnRender.addEventListener('click', () => {
      try {
        const data = readTextAreaJson();
        if (!data) {
          showError('Paste JSON in the textarea or load a file.');
          return;
        }
        renderBoard(data);
        showError('');
      } catch (e) {
        console.error(e);
        showError('Invalid JSON. Fix the contents and try again.');
      }
    });
  }

  if (btnClear) {
    btnClear.addEventListener('click', () => {
      const stage = el('#stage');
      if (stage) stage.innerHTML = 'Load or paste JSON, then click <b>Render JSON</b>.';
      showError('');
    });
  }

  // Re-render when toggles change (if something is already displayed)
  ;['#toggle-roads', '#toggle-labels', '#toggle-grid'].forEach(id => {
    const cb = el(id);
    if (cb) cb.addEventListener('change', () => {
      try {
        const data = readTextAreaJson();
        if (data) renderBoard(data);
      } catch (_) { /* ignore */ }
    });
  });
}

document.addEventListener('DOMContentLoaded', wireUI);
