// HexLab.js
// Terrain Hex Laboratory (ES modules) — uses shared lib/* utilities.

import { hexPolygonPoints } from './lib/hex-geom.js';
import { buildTerrainDefs } from './lib/terrain-defs.js';

const HEX_SIZE = 30;
const CENTER_X = 30;
const CENTER_Y = 26;

// Precomputed flat-top hex points (same math as visualizer)
const FLAT_HEX_POINTS = hexPolygonPoints(CENTER_X, CENTER_Y, HEX_SIZE);

const stageSvg   = () => document.getElementById('hexSvg');
const stageDefs  = () => document.getElementById('svgDefs');
const stageGroup = () => document.getElementById('hexContent');

function ensureDefs() {
  const svg = stageSvg();
  const defs = stageDefs();
  // Replace any prior defs with our shared libraries:
  defs.innerHTML =
    buildTerrainDefs({ flavor: 'viewer' }) + // woods-pattern, light-woods-pattern, brush-pattern, grain-pattern
    buildTerrainDefs({ flavor: 'viz' }) +    // building fills: stone1, stone2, wood
    buildTerrainDefs({ flavor: 'v39' });     // openGroundPattern, orchardPattern, marshPattern, etc.
}

function centerDot(showCenter) {
  return showCenter
    ? `<circle cx="${CENTER_X}" cy="${CENTER_Y}" r="1" fill="#fff" stroke="#333" stroke-width="0.3"/>`
    : '';
}

function coordsLabel(showCoords, fill = '#333', y = 30) {
  return showCoords
    ? `<text x="${CENTER_X}" y="${y}" text-anchor="middle" font-size="4" fill="${fill}">E5</text>`
    : '';
}

function gridStroke(showGrid) {
  return showGrid ? '#333' : 'none';
}

//
// Terrain renderers (return an HTML snippet that goes into #hexContent)
// Keep names consistent with the sidebar data-terrain values.
//
const terrainCatalog = {
  openGround: {
    name: 'Open Ground',
    description: 'Clear terrain with no obstacles or cover',
    render: (showGrid, showCoords, showCenter) => `
      <polygon points="${FLAT_HEX_POINTS}"
               fill="var(--open-ground)"
               stroke="${gridStroke(showGrid)}"
               stroke-width="0.5"/>
      ${centerDot(showCenter)}
      ${coordsLabel(showCoords, '#333', 30)}
    `,
  },

  woods: {
    name: 'Woods',
    description: 'Dense forest terrain providing concealment and cover',
    render: (showGrid, showCoords, showCenter) => `
      <polygon points="${FLAT_HEX_POINTS}"
               fill="url(#woods-pattern)"
               stroke="${gridStroke(showGrid)}"
               stroke-width="0.5"/>
      ${centerDot(showCenter)}
      ${coordsLabel(showCoords, '#fff', 30)}
    `,
  },

  lightWoods: {
    name: 'Light Woods',
    description: 'Sparse forest terrain with limited concealment',
    render: (showGrid, showCoords, showCenter) => `
      <polygon points="${FLAT_HEX_POINTS}"
               fill="url(#light-woods-pattern)"
               stroke="${gridStroke(showGrid)}"
               stroke-width="0.5"/>
      ${centerDot(showCenter)}
      ${coordsLabel(showCoords, '#fff', 30)}
    `,
  },

  brush: {
    name: 'Brush',
    description: 'Light vegetation providing hindrance but limited cover',
    render: (showGrid, showCoords, showCenter) => `
      <polygon points="${FLAT_HEX_POINTS}"
               fill="url(#brush-pattern)"
               stroke="${gridStroke(showGrid)}"
               stroke-width="0.5"/>
      ${centerDot(showCenter)}
      ${coordsLabel(showCoords, '#333', 30)}
    `,
  },

  orchard: {
    name: 'Orchard',
    description: 'Cultivated trees with patterned hindrance',
    render: (showGrid, showCoords, showCenter) => `
      <polygon points="${FLAT_HEX_POINTS}"
               fill="url(#orchardPattern)"
               stroke="${gridStroke(showGrid)}"
               stroke-width="0.5"/>
      ${centerDot(showCenter)}
      ${coordsLabel(showCoords, '#333', 30)}
    `,
  },

  grain: {
    name: 'Grain',
    description: 'Agricultural grain fields, seasonal hindrance',
    render: (showGrid, showCoords, showCenter) => `
      <polygon points="${FLAT_HEX_POINTS}"
               fill="url(#grain-pattern)"
               stroke="${gridStroke(showGrid)}"
               stroke-width="0.5"/>
      ${centerDot(showCenter)}
      ${coordsLabel(showCoords, '#333', 30)}
    `,
  },

  marsh: {
    name: 'Marsh',
    description: 'Wet ground with reeds and shallow pools',
    render: (showGrid, showCoords, showCenter) => `
      <polygon points="${FLAT_HEX_POINTS}"
               fill="url(#marshPattern)"
               stroke="${gridStroke(showGrid)}"
               stroke-width="0.5"/>
      ${centerDot(showCenter)}
      ${coordsLabel(showCoords, '#333', 30)}
    `,
  },

  // Water features (simple shapes; patterns not required)
  stream: {
    name: 'Stream',
    description: 'Narrow water course running through depression',
    render: (showGrid, showCoords, showCenter) => `
      <polygon points="${FLAT_HEX_POINTS}"
               fill="var(--open-ground)"
               stroke="${gridStroke(showGrid)}"
               stroke-width="0.5"/>
      <path d="M 45,0 Q 30,26 15,52" fill="none" stroke="var(--stream-blue)" stroke-width="4"/>
      <path d="M 45,0 Q 30,26 15,52" fill="none" stroke="#87ceeb" stroke-width="2"/>
      ${centerDot(showCenter)}
      ${coordsLabel(showCoords, '#333', 30)}
    `,
  },

  pond: {
    name: 'Pond',
    description: 'Small body of water',
    render: (showGrid, showCoords, showCenter) => `
      <polygon points="${FLAT_HEX_POINTS}"
               fill="var(--water-blue)"
               stroke="${gridStroke(showGrid)}"
               stroke-width="0.5"/>
      <ellipse cx="${CENTER_X}" cy="${CENTER_Y}" rx="20" ry="15"
               fill="none" stroke="var(--water-shallow)" stroke-width="0.5" opacity="0.5"/>
      <ellipse cx="${CENTER_X}" cy="${CENTER_Y}" rx="12" ry="8"
               fill="none" stroke="var(--water-shallow)" stroke-width="0.5" opacity="0.3"/>
      ${centerDot(showCenter)}
      ${coordsLabel(showCoords, '#fff', 30)}
    `,
  },

  rubble: {
    name: 'Rubble',
    description: 'Destroyed building debris providing cover and hindrance',
    render: (showGrid, showCoords, showCenter) => {
      const svg = stageSvg();
      // Lazy-inject a rubble pattern if missing (kept here for lab experimentation)
      if (!svg.querySelector('#rubble-pattern')) {
        stageDefs().insertAdjacentHTML('beforeend', `
          <pattern id="rubble-pattern" width="12" height="12" patternUnits="userSpaceOnUse">
            <rect width="12" height="12" fill="var(--rubble)"/>
            <rect x="1" y="1" width="4" height="3" fill="#7a6651" transform="rotate(15 3 2.5)"/>
            <rect x="7" y="2" width="3" height="4" fill="#8b7d6b" transform="rotate(-20 8.5 4)"/>
            <rect x="2" y="7" width="5" height="2" fill="#6b5d4f" transform="rotate(25 4.5 8)"/>
            <rect x="8" y="8" width="2" height="3" fill="#5c5248"/>
          </pattern>
        `);
      }
      return `
        <polygon points="${FLAT_HEX_POINTS}"
                 fill="url(#rubble-pattern)"
                 stroke="${gridStroke(showGrid)}"
                 stroke-width="0.5"/>
        ${centerDot(showCenter)}
        ${coordsLabel(showCoords, '#333', 30)}
      `;
    },
  },

  // Building overlays: draw footprint on open ground with a level indicator
  'building-wooden-1': {
    name: 'Wooden Building (1 Level)',
    description: 'Single-story wooden building on open ground',
    render: (showGrid, showCoords, showCenter) => `
      <polygon points="${FLAT_HEX_POINTS}"
               fill="var(--open-ground)"
               stroke="${gridStroke(showGrid)}"
               stroke-width="0.5"/>
      <rect x="15" y="16" width="30" height="20" fill="var(--building-wood)" stroke="#333" stroke-width="0.5"/>
      <line x1="15" y1="20" x2="45" y2="20" stroke="var(--building-wood-dark)" stroke-width="0.3"/>
      <line x1="15" y1="26" x2="45" y2="26" stroke="var(--building-wood-dark)" stroke-width="0.3"/>
      <line x1="15" y1="32" x2="45" y2="32" stroke="var(--building-wood-dark)" stroke-width="0.3"/>
      <text x="${CENTER_X}" y="${CENTER_Y+1}" text-anchor="middle" font-size="6" font-weight="bold" fill="#fff">1</text>
      ${centerDot(showCenter)}
      ${coordsLabel(showCoords, '#333', 44)}
    `,
  },

  'building-stone-2': {
    name: 'Stone Building (2 Levels)',
    description: 'Two-story stone building on open ground',
    render: (showGrid, showCoords, showCenter) => `
      <polygon points="${FLAT_HEX_POINTS}"
               fill="var(--open-ground)"
               stroke="${gridStroke(showGrid)}"
               stroke-width="0.5"/>
      <rect x="17" y="18" width="30" height="20" fill="var(--building-stone-dark)" opacity="0.5"/>
      <rect x="13" y="14" width="30" height="20" fill="var(--building-stone)" stroke="#333" stroke-width="0.5"/>
      <line x1="13" y1="20" x2="43" y2="20" stroke="var(--building-stone-dark)" stroke-width="0.5"/>
      <line x1="13" y1="26" x2="43" y2="26" stroke="var(--building-stone-dark)" stroke-width="0.5"/>
      <rect x="${CENTER_X-5}" y="${CENTER_Y-4}" width="10" height="8" fill="#fff" stroke="#333" stroke-width="0.3"/>
      <text x="${CENTER_X}" y="${CENTER_Y+2}" text-anchor="middle" font-size="6" font-weight="bold" fill="#333">2</text>
      ${centerDot(showCenter)}
      ${coordsLabel(showCoords, '#333', 44)}
    `,
  },

  // Default (not yet implemented)
  default: {
    render: (showGrid, showCoords, showCenter, name) => `
      <polygon points="${FLAT_HEX_POINTS}"
               fill="#e0e0e0"
               stroke="${gridStroke(showGrid)}"
               stroke-width="0.5"/>
      <text x="${CENTER_X}" y="${CENTER_Y-2}" text-anchor="middle" font-size="5" fill="#666">${name}</text>
      <text x="${CENTER_X}" y="${CENTER_Y+4}" text-anchor="middle" font-size="3" fill="#999">(Not implemented)</text>
      ${centerDot(showCenter)}
      ${coordsLabel(showCoords, '#333', 36)}
    `,
  },
};

function renderTerrain(terrainType) {
  ensureDefs(); // make sure pattern sets are present (viewer/viz/v39)
  const content = stageGroup();

  const showGrid   = document.getElementById('showGrid').checked;
  const showCoords = document.getElementById('showCoords').checked;
  const showCenter = document.getElementById('showCenter').checked;

  const entry = terrainCatalog[terrainType] || terrainCatalog.default;
  const html  = entry.render(showGrid, showCoords, showCenter, terrainType);

  content.innerHTML = html;

  // Update header
  document.getElementById('terrainName').textContent =
    entry.name || terrainType;
  document.getElementById('terrainDescription').textContent =
    entry.description || 'Custom terrain type';
}

function wireUI() {
  // Sidebar interactions
  document.querySelectorAll('.terrain-item').forEach(item => {
    item.addEventListener('click', () => {
      document.querySelectorAll('.terrain-item').forEach(el => el.classList.remove('active'));
      item.classList.add('active');
      renderTerrain(item.getAttribute('data-terrain'));
    });
  });

  // Controls
  ['showGrid', 'showCoords', 'showCenter'].forEach(id => {
    const el = document.getElementById(id);
    el.addEventListener('change', () => {
      const active = document.querySelector('.terrain-item.active');
      if (active) renderTerrain(active.getAttribute('data-terrain'));
    });
  });
}

document.addEventListener('DOMContentLoaded', () => {
  wireUI();
  // Initial defs injection so patterns exist before first selection
  ensureDefs();
});
