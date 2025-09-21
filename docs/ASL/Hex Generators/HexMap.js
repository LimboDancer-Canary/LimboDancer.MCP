// HexMap.js
// ASL Board Visualizer logic (flat-top, N=0) as an ES module.

import { hexPolygonPoints, SIDE_ORDER } from './hex-geom.js';
import { midpointsFromSides, roadPath } from './linear-features.js';
import { buildTerrainDefs } from './terrain-defs.js';

// ---------- Config ----------
const HEX_SIZE = 30;
const HEX_H = Math.sqrt(3) * HEX_SIZE;
const ORDER_DEFAULT = SIDE_ORDER.slice();

const COLORS = {
  openBase:  '#90a955',
  woodsBase: '#6f8f3d',
  gridStroke: 'rgba(60,60,60,0.45)',
  gridWidth: 0.8,
  labelDark: '#111',
  labelLight:'#fff'
};

const stage = document.getElementById('stage');
const msg = document.getElementById('msg');

const showError = (t)=>{ msg.textContent=t; msg.style.display='block'; };
const hideError  = ()=>{ msg.style.display='none'; };

// ---------- helpers ----------
function orderFromJSON(data){
  return data?.map?.renderHints?.sideIndexing?.orderClockwise || ORDER_DEFAULT;
}
function colLettersToIndex(letters){ let c=0; for(let i=0;i<letters.length;i++) c=c*26+(letters.charCodeAt(i)-64); return c-1; }
function parseCoord(id){
  const m=String(id).trim().match(/^1?([A-Z]+)(\d+)$/);
  if(!m) return null;
  return { c: colLettersToIndex(m[1]), r: parseInt(m[2],10)-1 };
}
function letters(c){ return c<26?String.fromCharCode(65+c):('A'+String.fromCharCode(65+(c-26))); }
function cid(c,r){ return '1'+letters(c)+(r+1); }
function hexPos(c,r){ return { x: c*HEX_SIZE*1.5 + 80, y: r*HEX_H + (c%2)*(HEX_H*0.5) + 80 }; }
function edgeName(edge, order){
  if (edge === null || edge === undefined || edge === '') return null;
  if (typeof edge === 'string') {
    const s = edge.trim().toUpperCase();
    if (ORDER_DEFAULT.includes(s)) return s;
    const n = Number(s);
    if (Number.isFinite(n)) return order[((n%6)+6)%6];
    return null;
  }
  if (typeof edge === 'number') return order[((edge%6)+6)%6];
  return null;
}

// ---------- SVG + defs ----------
function ensureSvg(width, height){
  stage.innerHTML = '';
  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  svg.setAttribute('viewBox', `0 0 ${width} ${height}`);
  svg.setAttribute('xmlns','http://www.w3.org/2000/svg');
  svg.insertAdjacentHTML('afterbegin',
    buildTerrainDefs({ flavor: 'v39' }) + buildTerrainDefs({ flavor: 'viz' })
  );
  stage.appendChild(svg);
  return svg;
}

// ---------- terrain → pattern mapping ----------
function normalizeBase(t){
  const base = (t?.baseTerrain || '').toLowerCase();
  if (base === 'openground' || base === 'open') return 'open';
  return base || 'open';
}
function baseFillColor(base){
  return (base === 'woods') ? COLORS.woodsBase : COLORS.openBase;
}
function patternIdForBase(base){
  switch(base){
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
function patternIdForBuilding(b){
  if(!b) return null;
  if (b.type==='stone' && b.levels===2) return 'stone2';
  if (b.type==='stone' && b.levels===1) return 'stone1';
  if (b.type==='wooden')                return 'wood';
  return null;
}
function labelColor(base){ return (base==='woods') ? COLORS.labelLight : COLORS.labelDark; }

// ---------- drawing helpers ----------
function drawHexComposite(svg, center, size, template, hexId, showGrid, showLabels, used){
  const base = normalizeBase(template);
  const points = hexPolygonPoints(center.x, center.y, size);

  // base solid
  svg.insertAdjacentHTML('beforeend',
    `<polygon data-hex="${hexId}" points="${points}" fill="${baseFillColor(base)}" stroke="${showGrid ? COLORS.gridStroke : 'none'}" stroke-width="${showGrid?COLORS.gridWidth:0}"/>`
  );

  // overlay pattern
  const bpid = patternIdForBuilding(template?.building);
  const pid  = bpid || patternIdForBase(base);
  const opacity = (pid === 'openGroundPattern') ? 0.35 : 1.0;
  svg.insertAdjacentHTML('beforeend',
    `<polygon points="${points}" fill="url(#${pid})" fill-opacity="${opacity}" stroke="none"/>`
  );

  // usage tracking
  if (bpid) { used.buildings.add(bpid); }
  else      { used.bases.add(base); }

  if (showLabels){
    svg.insertAdjacentHTML('beforeend',
      `<text x="${center.x}" y="${center.y+4}" text-anchor="middle" fill="${labelColor(base)}" font-size="10" font-weight="600">${hexId.replace(/^1/,'')}</text>`
    );
  }
}

function drawLegend(svg, svgW, used){
  const legendX = svgW - 220;
  const legendY = 70;
  const itemH   = 28;

  svg.insertAdjacentHTML('beforeend', `<text x="${svgW/2}" y="40" text-anchor="middle" fill="#6b7280" font-size="12">ASL Board — Generated from JSON</text>`);
  svg.insertAdjacentHTML('beforeend', `<text x="${legendX}" y="${legendY-16}" font-size="16" font-weight="600">Map Legend</text>`);

  function legendHex(x,y, base, pid, label){
    const miniPts = hexPolygonPoints(x, y, 14);
    svg.insertAdjacentHTML('beforeend', `<polygon points="${miniPts}" fill="${baseFillColor(base)}" stroke="#333" stroke-width="0.7"/>`);
    const op = (pid==='openGroundPattern') ? 0.35 : 1.0;
    svg.insertAdjacentHTML('beforeend', `<polygon points="${miniPts}" fill="url(#${pid})" fill-opacity="${op}"/>`);
    svg.insertAdjacentHTML('beforeend', `<text x="${x+26}" y="${y+4}" font-size="12">${label}</text>`);
  }

  const BASE_ORDER   = ['open','woods','orchard','brush','grain','marsh','sand','scrub'];
  const LABELS_BASE  = { open:'Open Ground', woods:'Woods', orchard:'Orchard', brush:'Brush', grain:'Grain', marsh:'Marsh', sand:'Sand', scrub:'Scrub' };
  const BUILD_ORDER  = ['stone2','stone1','wood'];
  const LABELS_BUILD = { stone2:'Stone Building (2 levels)', stone1:'Stone Building (1 level)', wood:'Wooden Building' };

  let row = 0;
  for(const base of BASE_ORDER){
    if(used.bases.has(base)){
      legendHex(legendX, legendY + row*itemH, base, patternIdForBase(base), LABELS_BASE[base]); row++;
    }
  }
  for(const bid of BUILD_ORDER){
    if(used.buildings.has(bid)){
      legendHex(legendX, legendY + row*itemH, 'open', bid, LABELS_BUILD[bid]); row++;
    }
  }
}

// draw one road using LOCAL coords, then translate to center
function drawRoad(svg, center, entryName, exitName, order){
  const mps = midpointsFromSides(entryName, exitName, HEX_SIZE); // local coords
  const d   = roadPath({ enter: mps.enter, exit: mps.exit || undefined }, entryName, exitName, HEX_SIZE, order);
  const g = document.createElementNS('http://www.w3.org/2000/svg','g');
  g.setAttribute('transform', `translate(${center.x},${center.y})`);
  g.innerHTML =
    `<path d="${d}" fill="none" stroke="#666" stroke-width="4" stroke-linecap="round" stroke-linejoin="round" opacity=".85"></path>
     <path d="${d}" fill="none" stroke="#c8c8c8" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"></path>`;
  svg.appendChild(g);
}

// ---------- Render ----------
function draw(data){
  try{
    hideError();

    const showRoads = document.getElementById('toggle-roads').checked;
    const showLabels= document.getElementById('toggle-labels').checked;
    const showGrid  = document.getElementById('toggle-grid').checked;

    const W=+data?.map?.dimensions?.width, H=+data?.map?.dimensions?.height;
    if(!Number.isFinite(W)||!Number.isFinite(H)) throw new Error("Missing map.dimensions width/height.");

    const defaultId=data?.map?.defaultTemplateId;
    const defaultT=data?.hexTemplates?.[defaultId];
    if(!defaultT) throw new Error("defaultTemplateId not found in hexTemplates.");

    const order = orderFromJSON(data);

    const padX=160, padY=120;
    const svgW = Math.max(padX + W*HEX_SIZE*1.6, 900);
    const svgH = Math.max(padY + H*HEX_H + HEX_H, 600);
    const svg = ensureSvg(svgW, svgH);

    const used = { bases:new Set(), buildings:new Set() };

    // 1) draw grid (default template)
    for (let r = 0; r < H; r++) {
      for (let c = 0; c < W; c++) {
        const hexId = cid(c,r);
        const center = hexPos(c, r);
        drawHexComposite(svg, center, HEX_SIZE, defaultT, hexId, showGrid, showLabels, used);
      }
    }

    // 2) per-hex map (data.hexes)
    const hexMap = data.hexes || {};
    for (const hexId of Object.keys(hexMap)) {
      const cr = parseCoord(hexId); if(!cr) continue;
      const center = hexPos(cr.c, cr.r);
      const t = hexMap[hexId] || defaultT;
      drawHexComposite(svg, center, HEX_SIZE, t, hexId, showGrid, showLabels, used);

      if (showRoads) {
        const lf = t.linearFeature || t.road || null;
        if (lf && (lf.entryEdge!==undefined)) {
          const entryName = edgeName(lf.entryEdge, order);
          const exitName  = edgeName(lf.exitEdge,  order);
          if (entryName){ drawRoad(svg, center, entryName, exitName, order); }
        }
      }
    }

    // 3) individualHexes (overrides &/or linearTraversals)
    const overrides = data.map?.individualHexes || [];
    for (const h of overrides){
      if(!h?.hexId) continue;
      const cr = parseCoord(h.hexId); if(!cr) continue;
      const center = hexPos(cr.c, cr.r);
      const baseT = data.hexTemplates?.[h.templateId] || defaultT;
      const merged = Object.assign({}, baseT, h.overrides && !Array.isArray(h.overrides) ? h.overrides : {});
      drawHexComposite(svg, center, HEX_SIZE, merged, h.hexId, showGrid, showLabels, used);

      if (showRoads){
        const lts = merged.linearTraversals || [];
        for(const lt of lts){
          const entryName = edgeName(lt.enters, order);
          const exitName  = edgeName(lt.exits,  order);
          if (entryName){ drawRoad(svg, center, entryName, exitName, order); }
        }
        if (Array.isArray(h.overrides)){
          for(const o of h.overrides){
            if((o.type||'').toLowerCase()!=='road') continue;
            const entryName = edgeName(o.enters, order);
            const exitName  = edgeName(o.exits,  order);
            if (entryName){ drawRoad(svg, center, entryName, exitName, order); }
          }
        }
      }
    }

    // 4) legend (after we know what’s present)
    drawLegend(svg, svgW, used);

  }catch(e){
    showError(e.message || String(e));
    console.error('[VIS] draw failed', e);
  }
}
window.draw = draw; // optional: keep for quick testing from console

// ---------- UI wiring ----------
document.getElementById('btn-render').addEventListener('click', ()=>{
  try{
    const text = document.getElementById('json').value.trim();
    if(!text){ showError('No JSON provided.'); return; }
    draw(JSON.parse(text));
  }catch(e){
    showError('Invalid JSON: '+e.message);
  }
});

document.getElementById('btn-clear').addEventListener('click', ()=>{
  document.getElementById('json').value='';
  stage.innerHTML='<div id="stage">Load or paste JSON, then click <b>Render JSON</b>.</div>';
});

document.getElementById('btn-load').addEventListener('click', ()=>{
  const fileInput = document.getElementById('file');
  if (!fileInput.files || !fileInput.files[0]){ showError('Choose a .json file first.'); return; }
  const reader = new FileReader();
  reader.onload = () => { document.getElementById('json').value = reader.result; };
  reader.readAsText(fileInput.files[0]);
});

['toggle-roads','toggle-labels','toggle-grid'].forEach(id=>{
  document.getElementById(id).addEventListener('change', ()=>{
    const t = document.getElementById('json').value.trim();
    if(t){ try{ draw(JSON.parse(t)); }catch{} }
  });
});

export { draw };
