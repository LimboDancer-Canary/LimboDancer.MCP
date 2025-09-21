// HexMap.js
// Thin UI entry that delegates all rendering to lib/render.js

import { draw } from './lib/render.js';

const stage = document.getElementById('stage');
const msg   = document.getElementById('msg');

const showError = (t)=>{ msg.textContent = t; msg.style.display = 'block'; };
const hideError = ()=>{ msg.style.display = 'none'; };

function currentOpts() {
  return {
    showRoads:  document.getElementById('toggle-roads')?.checked ?? true,
    showLabels: document.getElementById('toggle-labels')?.checked ?? true,
    showGrid:   document.getElementById('toggle-grid')?.checked ?? true,
  };
}

function renderFromTextarea() {
  try {
    hideError();
    const text = document.getElementById('json').value.trim();
    if (!text) { showError('No JSON provided.'); return; }
    const data = JSON.parse(text);
    draw(stage, data, currentOpts());
  } catch (e) {
    console.error('[HexMap] render error', e);
    showError('Invalid JSON: ' + (e?.message || e));
  }
}

document.getElementById('btn-render')?.addEventListener('click', renderFromTextarea);

document.getElementById('btn-clear')?.addEventListener('click', ()=>{
  document.getElementById('json').value = '';
  // Reset stage to the placeholder message
  stage.innerHTML = '<div id="stage">Load or paste JSON, then click <b>Render JSON</b>.</div>';
  hideError();
});

document.getElementById('btn-load')?.addEventListener('click', ()=>{
  const fileInput = document.getElementById('file');
  if (!fileInput.files || !fileInput.files[0]) { showError('Choose a .json file first.'); return; }
  const reader = new FileReader();
  reader.onload = () => { document.getElementById('json').value = reader.result; };
  reader.readAsText(fileInput.files[0]);
});

// Live re-render on toggles (if JSON present)
['toggle-roads','toggle-labels','toggle-grid'].forEach(id=>{
  const el = document.getElementById(id);
  if (!el) return;
  el.addEventListener('change', ()=>{
    const t = document.getElementById('json').value.trim();
    if (t) { try { draw(stage, JSON.parse(t), currentOpts()); } catch { /* ignore */ } }
  });
});

// Optional: export for console debugging
export { renderFromTextarea as draw };
