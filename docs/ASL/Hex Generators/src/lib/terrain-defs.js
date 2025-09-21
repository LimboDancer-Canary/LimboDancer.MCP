// lib/terrain-defs.js
// SVG <defs> for terrain patterns & optional hillshade filter.
// API:
//   buildTerrainDefs({ flavor: "v39" | "viewer" | "viz" })
//   buildHillshadeFilter()


export const CANON = {
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
  ],
};


/**
 * Pick which ID style to emit:
 * - "v39"   → ids like woodsPattern, brushPattern, openGroundPattern...
 * - "viewer"→ ids like woods-pattern, brush-pattern, grain-pattern...
 * - "viz"   → ids used by the visualizer: woods, stone1, stone2, wood
 */
export function buildTerrainDefs({ flavor = "v39" } = {}) {
  if (flavor === "viz") {
    // Minimal set used by the visualizer (can be extended)
    return `<defs>
      <pattern id="woods" width="20" height="20" patternUnits="userSpaceOnUse">
        <rect width="20" height="20" fill="#2d5016"/>
        <circle cx="5" cy="5" r="3" fill="#1a3009" opacity="0.6"/>
        <circle cx="15" cy="10" r="2.5" fill="#1a3009" opacity="0.5"/>
        <circle cx="8" cy="15" r="3.5" fill="#1a3009" opacity="0.7"/>
      </pattern>
      <pattern id="stone2" width="10" height="10" patternUnits="userSpaceOnUse">
        <rect width="10" height="10" fill="#8b7d6b"/>
        <rect x="0" y="0" width="10" height="2" fill="#5c5248"/>
        <rect x="0" y="5" width="10" height="2" fill="#5c5248"/>
      </pattern>
      <pattern id="stone1" width="10" height="10" patternUnits="userSpaceOnUse">
        <rect width="10" height="10" fill="#a0937f"/>
        <line x1="0" y1="5" x2="10" y2="5" stroke="#7a6e5c" stroke-width="1"/>
      </pattern>
      <pattern id="wood" width="8" height="8" patternUnits="userSpaceOnUse">
        <rect width="8" height="8" fill="#8b6914"/>
        <line x1="0" y1="0" x2="0" y2="8" stroke="#654b0e" stroke-width="0.5"/>
        <line x1="4" y1="0" x2="4" y2="8" stroke="#654b0e" stroke-width="0.5"/>
      </pattern>
    </defs>`;
  }

  if (flavor === "viewer") {
    // Matches ids the viewer already adds (…-pattern)
    return `<defs>
      <pattern id="woods-pattern" width="20" height="20" patternUnits="userSpaceOnUse">
        <rect width="20" height="20" fill="#2d5016"/>
        <circle cx="5" cy="5" r="3" fill="#1a3009" opacity="0.6"/>
        <circle cx="15" cy="10" r="2.5" fill="#1a3009" opacity="0.5"/>
        <circle cx="8" cy="15" r="3.5" fill="#1a3009" opacity="0.7"/>
      </pattern>
      <pattern id="light-woods-pattern" width="25" height="25" patternUnits="userSpaceOnUse">
        <rect width="25" height="25" fill="#4a6b2e"/>
        <circle cx="8" cy="8" r="2.5" fill="#2d5016" opacity="0.5"/>
        <circle cx="18" cy="18" r="2"   fill="#2d5016" opacity="0.4"/>
      </pattern>
      <pattern id="brush-pattern" width="15" height="15" patternUnits="userSpaceOnUse">
        <rect width="15" height="15" fill="#6b7a4f"/>
        <path d="M3,12 Q5,8 7,12 M9,10 Q11,6 13,10" stroke="#4a5c2e" stroke-width="1.5" fill="none" opacity="0.6"/>
        <circle cx="5" cy="5" r="1" fill="#4a5c2e" opacity="0.4"/>
      </pattern>
      <pattern id="grain-pattern" width="10" height="20" patternUnits="userSpaceOnUse">
        <rect width="10" height="20" fill="#daa520"/>
        <path d="M2,0 L2,20 M5,0 L5,20 M8,0 L8,20" stroke="#b8960a" stroke-width="0.5"/>
        <circle cx="2" cy="5" r="1" fill="#8b7500"/>
        <circle cx="5" cy="8" r="1" fill="#8b7500"/>
        <circle cx="8" cy="6" r="1" fill="#8b7500"/>
      </pattern>
    </defs>`;
  }

  // Default "v39" (…Pattern ids)
  return `<defs>
    <pattern id="woodsPattern" x="0" y="0" width="20" height="20" patternUnits="userSpaceOnUse">
      <circle cx="5" cy="5" r="3" fill="#2d5016" opacity="0.8"/>
      <circle cx="5" cy="3" r="2" fill="#3d6b20" opacity="0.8"/>
      <circle cx="15" cy="12" r="3" fill="#2d5016" opacity="0.8"/>
      <circle cx="15" cy="10" r="2" fill="#3d6b20" opacity="0.8"/>
      <circle cx="8" cy="18" r="3" fill="#2d5016" opacity="0.8"/>
      <circle cx="8" cy="16" r="2" fill="#3d6b20" opacity="0.8"/>
    </pattern>
    <pattern id="orchardPattern" x="0" y="0" width="15" height="15" patternUnits="userSpaceOnUse">
      <circle cx="7.5" cy="7.5" r="2" fill="#2d5016" opacity="0.6"/>
      <circle cx="7.5" cy="7.5" r="1" fill="#3d6b20" opacity="0.8"/>
    </pattern>
    <pattern id="brushPattern" x="0" y="0" width="16" height="16" patternUnits="userSpaceOnUse">
      <ellipse cx="4" cy="8" rx="3" ry="2" fill="#6b8e23" opacity="0.6"/>
      <ellipse cx="12" cy="4" rx="2" ry="3" fill="#6b8e23" opacity="0.6"/>
      <ellipse cx="10" cy="12" rx="3" ry="2" fill="#6b8e23" opacity="0.6"/>
    </pattern>
    <pattern id="grainPattern" x="0" y="0" width="6" height="10" patternUnits="userSpaceOnUse">
      <line x1="1" y1="0" x2="1" y2="10" stroke="#daa520" stroke-width="0.5" opacity="0.8"/>
      <line x1="3" y1="0" x2="3" y2="10" stroke="#b8960c" stroke-width="0.5" opacity="0.8"/>
      <line x1="5" y1="0" x2="5" y2="10" stroke="#daa520" stroke-width="0.5" opacity="0.8"/>
      <circle cx="1" cy="2" r="0.5" fill="#daa520" opacity="0.8"/>
      <circle cx="3" cy="4" r="0.5" fill="#b8960c" opacity="0.8"/>
      <circle cx="5" cy="3" r="0.5" fill="#daa520" opacity="0.8"/>
    </pattern>
    <pattern id="marshPattern" x="0" y="0" width="20" height="20" patternUnits="userSpaceOnUse">
      <path d="M5,10 Q5,15 3,15 T3,10" fill="none" stroke="#4a7c59" stroke-width="1" opacity="0.6"/>
      <path d="M8,5 Q8,10 6,10 T6,5" fill="none" stroke="#4a7c59" stroke-width="1" opacity="0.6"/>
      <path d="M15,12 Q15,17 13,17 T13,12" fill="none" stroke="#4a7c59" stroke-width="1" opacity="0.6"/>
      <path d="M18,2 Q18,7 16,7 T16,2" fill="none" stroke="#4a7c59" stroke-width="1" opacity="0.6"/>
      <ellipse cx="10" cy="12" rx="2" ry="1" fill="#5f9ea0" opacity="0.3"/>
    </pattern>
    <pattern id="openGroundPattern" x="0" y="0" width="30" height="30" patternUnits="userSpaceOnUse">
      <circle cx="10" cy="10" r="0.5" fill="#c4b5a0" opacity="0.3"/>
      <circle cx="20" cy="20" r="0.5" fill="#c4b5a0" opacity="0.3"/>
      <circle cx="5"  cy="25" r="0.5" fill="#c4b5a0" opacity="0.3"/>
      <circle cx="25" cy="5"  r="0.5" fill="#c4b5a0" opacity="0.3"/>
    </pattern>
    <pattern id="sandPattern" x="0" y="0" width="10" height="10" patternUnits="userSpaceOnUse">
      <circle cx="2" cy="2" r="0.3" fill="#d2b48c" opacity="0.4"/>
      <circle cx="5" cy="4" r="0.3" fill="#d2b48c" opacity="0.4"/>
      <circle cx="8" cy="3" r="0.3" fill="#d2b48c" opacity="0.4"/>
      <circle cx="3" cy="7" r="0.3" fill="#d2b48c" opacity="0.4"/>
      <circle cx="7" cy="8" r="0.3" fill="#d2b48c" opacity="0.4"/>
    </pattern>
    <pattern id="scrubPattern" x="0" y="0" width="15" height="15" patternUnits="userSpaceOnUse">
      <circle cx="5" cy="5" r="1" fill="#a0826d" opacity="0.5"/>
      <circle cx="10" cy="10" r="1" fill="#a0826d" opacity="0.5"/>
      <path d="M3,10 L5,8 L7,10" fill="none" stroke="#a0826d" stroke-width="0.5" opacity="0.6"/>
      <path d="M8,5 L10,3 L12,5" fill="none" stroke="#a0826d" stroke-width="0.5" opacity="0.6"/>
    </pattern>
  </defs>`;
}

/** Optional: subtle lighting (unused by default) */
export function buildHillshadeFilter() {
  return `<defs>
    <filter id="hillshade" x="-40%" y="-40%" width="180%" height="180%" color-interpolation-filters="sRGB">
      <feGaussianBlur in="SourceAlpha" stdDeviation="1" result="blur"/>
      <feDiffuseLighting in="blur" surfaceScale="2" lighting-color="#ffffff" result="light">
        <feDistantLight azimuth="315" elevation="35"/>
      </feDiffuseLighting>
      <feComposite in="light" in2="SourceGraphic" operator="in" result="litPaint"/>
      <feBlend in="SourceGraphic" in2="litPaint" mode="multiply"/>
    </filter>
  </defs>`;
}
