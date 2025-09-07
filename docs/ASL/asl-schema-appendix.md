# Appendix: Complete Schema Reference

## Introduction

This appendix contains the complete, unabridged schemas from all ASL terrain system project documents. These schemas represent the full technical specification for implementing ASL's terrain system in ASP.NET Core 9 with Blazor Server.

## Table of Contents

1. [Complete ASL Detailed Hex Schema](#1-complete-asl-detailed-hex-schema)
2. [ASL Hex Examples with Linear Traversals](#2-asl-hex-examples-with-linear-traversals)
3. [Complete ASL Scene System](#3-complete-asl-scene-system)
4. [ASL Scene Coordinate System](#4-asl-scene-coordinate-system)
5. [Complete ASL Map Schema](#5-complete-asl-map-schema)
6. [Linear Traversal Analysis](#6-linear-traversal-analysis)

---

## 1. Complete ASL Detailed Hex Schema

This schema captures every possible property a hex can have in ASL. Each field maps directly to specific game mechanics and rules.

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "ASL Hex Definition",
  "description": "Complete definition of a single ASL hex with all possible properties",
  "type": "object",
  "required": ["id", "baseTerrain", "elevation"],
  "properties": {
    "id": {
      "type": "string",
      "pattern": "^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$",
      "description": "Hex template GUID"
    },
    "baseTerrain": {
      "type": "string",
      "description": "Primary terrain type",
      "enum": [
        "openGround", "shellholes", "woods", "lightWoods", "brush",
        "orchard", "vineyard", "grain", "marsh", "mudflat", "crag",
        "graveyard", "gully", "stream", "canal", "river", "pond",
        "lake", "ocean", "valley", "rubble", "debris", "villageTerrain"
      ]
    },
    "elevation": {
      "type": "integer",
      "description": "Base elevation (-3 to +4 typically)"
    },
    "hexsides": {
      "type": "array",
      "description": "Features on each hexside (0-5, clockwise from north)",
      "minItems": 6,
      "maxItems": 6,
      "items": {
        "$ref": "#/definitions/hexsideDefinition"
      }
    },
    "building": {
      "$ref": "#/definitions/buildingProperties"
    },
    "fortifications": {
      "type": "array",
      "items": {
        "$ref": "#/definitions/fortificationDefinition"
      }
    },
    "overlays": {
      "type": "array",
      "description": "Terrain modifications",
      "items": {
        "$ref": "#/definitions/overlayDefinition"
      }
    },
    "water": {
      "$ref": "#/definitions/waterProperties"
    },
    "seasonal": {
      "$ref": "#/definitions/seasonalProperties"
    },
    "conditions": {
      "$ref": "#/definitions/hexConditions"
    },
    "los": {
      "$ref": "#/definitions/losProperties"
    },
    "movement": {
      "$ref": "#/definitions/movementCosts"
    },
    "combat": {
      "$ref": "#/definitions/combatProperties"
    },
    "linearTraversals": {
      "type": "array",
      "items": {
        "$ref": "#/definitions/linearTraversal"
      }
    },
    "intersections": {
      "type": "array",
      "items": {
        "$ref": "#/definitions/featureIntersection"
      }
    }
  },
  "definitions": {
    "hexsideDefinition": {
      "type": "object",
      "properties": {
        "side": {
          "type": "integer",
          "minimum": 0,
          "maximum": 5
        },
        "terrain": {
          "type": "array",
          "items": {
            "type": "string",
            "enum": [
              "clear", "wall", "hedge", "bocage", "cliff",
              "hillsideWall", "hillsideHedge", "cactusHedge",
              "road", "railroad", "path", "ford", "bridge"
            ]
          }
        },
        "elevation": {
          "type": "string",
          "enum": ["ground", "sunken", "elevated"],
          "description": "For roads/railroads"
        },
        "attributes": {
          "type": "object",
          "properties": {
            "breached": {"type": "boolean"},
            "gate": {"type": "boolean"},
            "roadblocked": {"type": "boolean"},
            "destroyed": {"type": "boolean"}
          }
        },
        "connectsTo": {
          "type": "string",
          "description": "Adjacent hex ID"
        }
      }
    },
    "buildingProperties": {
      "type": "object",
      "properties": {
        "type": {
          "type": "string",
          "enum": ["wooden", "stone", "factory", "marketplace", "rowhouse", "church"]
        },
        "levels": {
          "type": "integer",
          "minimum": 1,
          "maximum": 4
        },
        "currentLevels": {
          "type": "integer",
          "description": "After rubble/damage"
        },
        "fortress": {"type": "boolean"},
        "hasStairwell": {"type": "boolean"},
        "hasCellar": {"type": "boolean"},
        "rooftop": {"type": "boolean"},
        "internalWalls": {
          "type": "array",
          "items": {"type": "integer"}
        },
        "multiHex": {
          "type": "array",
          "description": "Other hexes of same building",
          "items": {"type": "string"}
        }
      }
    },
    "fortificationDefinition": {
      "type": "object",
      "required": ["type"],
      "properties": {
        "type": {
          "type": "string",
          "enum": [
            "foxhole", "trench", "wire", "minefield",
            "roadblock", "pillbox", "bunker", "sangar",
            "tetrahedron", "dragonTeeth", "antiTankDitch"
          ]
        },
        "ca": {
          "type": "integer",
          "description": "Covered arc facing"
        },
        "strength": {
          "type": "string",
          "enum": ["light", "medium", "heavy"]
        }
      }
    },
    "overlayDefinition": {
      "type": "object",
      "properties": {
        "type": {
          "type": "string",
          "enum": ["woods", "building", "rubble", "road", "bridge", "hill"]
        },
        "id": {
          "type": "string",
          "description": "Overlay identifier (e.g., 'Wd1', 'RB2')"
        }
      }
    },
    "waterProperties": {
      "type": "object",
      "properties": {
        "depth": {
          "type": "string",
          "enum": ["dry", "shallow", "fordable", "deep", "flooded"]
        },
        "current": {
          "type": "object",
          "properties": {
            "force": {
              "type": "string",
              "enum": ["none", "slow", "moderate", "heavy"]
            },
            "direction": {"type": "integer"}
          }
        },
        "frozen": {"type": "boolean"},
        "hexsidePonds": {
          "type": "array",
          "items": {"type": "integer"}
        }
      }
    },
    "seasonalProperties": {
      "type": "object",
      "properties": {
        "grainInSeason": {"type": "boolean"},
        "orchardInSeason": {"type": "boolean"},
        "weather": {
          "type": "string",
          "enum": ["clear", "rain", "snow", "mud", "frost"]
        }
      }
    },
    "linearTraversal": {
      "type": "object",
      "required": ["type", "enters", "exits"],
      "properties": {
        "type": {
          "type": "string",
          "enum": ["road", "railroad", "stream", "path", "trail"]
        },
        "subtype": {
          "type": "string",
          "enum": ["paved", "dirt", "sunken", "elevated"],
          "description": "Road/railroad subtypes"
        },
        "enters": {
          "type": "integer",
          "minimum": 0,
          "maximum": 5,
          "description": "Hexside where feature enters"
        },
        "exits": {
          "type": ["integer", "null"],
          "minimum": 0,
          "maximum": 5,
          "description": "Hexside where feature exits (null if terminates)"
        },
        "elevation": {
          "type": "string",
          "enum": ["ground", "sunken", "elevated", "depression"],
          "default": "ground"
        },
        "attributes": {
          "type": "object",
          "properties": {
            "width": {"type": "string", "enum": ["narrow", "normal", "wide"]},
            "depth": {"type": "string", "enum": ["shallow", "deep", "flooded"]},
            "current": {
              "type": "object",
              "properties": {
                "force": {"type": "string"},
                "direction": {"type": "integer"}
              }
            }
          }
        }
      }
    },
    "featureIntersection": {
      "type": "object",
      "required": ["features", "intersectionType"],
      "properties": {
        "features": {
          "type": "array",
          "description": "Indices of linearTraversals that intersect",
          "minItems": 2,
          "items": {"type": "integer"}
        },
        "intersectionType": {
          "type": "string",
          "enum": ["bridge", "ford", "culvert", "crossing", "confluence"]
        },
        "elevation": {
          "type": "string",
          "description": "Which feature is on top",
          "enum": ["feature0", "feature1", "same"]
        },
        "attributes": {
          "type": "object",
          "properties": {
            "bridgeType": {"type": "string", "enum": ["wooden", "stone", "pontoon"]},
            "capacity": {"type": "string", "enum": ["foot", "vehicle", "heavy"]},
            "destroyed": {"type": "boolean"}
          }
        }
      }
    },
    "hexConditions": {
      "type": "object",
      "properties": {
        "fire": {
          "type": "string",
          "enum": ["none", "smoke", "flame", "blaze"]
        },
        "rubbled": {"type": "boolean"},
        "debris": {"type": "boolean"},
        "shellholes": {"type": "integer"},
        "controlled": {
          "type": "string",
          "enum": ["attacker", "defender", "contested", "neutral"]
        }
      }
    },
    "losProperties": {
      "type": "object",
      "properties": {
        "obstacle": {
          "type": "number",
          "description": "Height in levels (0 = no obstacle)"
        },
        "hindrance": {
          "type": "integer",
          "description": "Hindrance DRM"
        },
        "blind": {"type": "boolean"},
        "crestLines": {
          "type": "array",
          "items": {"type": "integer"}
        }
      }
    },
    "movementCosts": {
      "type": "object",
      "properties": {
        "infantry": {"type": "number"},
        "cavalry": {"type": "number"},
        "vehicle": {
          "type": "object",
          "properties": {
            "tracked": {"type": "number"},
            "halfTracked": {"type": "number"},
            "wheeled": {"type": "number"},
            "truckMP": {"type": "number"}
          }
        },
        "motorcycle": {"type": "number"}
      }
    },
    "combatProperties": {
      "type": "object",
      "properties": {
        "tem": {"type": "integer"},
        "hindrance": {"type": "integer"},
        "airBurst": {"type": "integer"},
        "hullDown": {"type": "boolean"},
        "wallAdvantage": {"type": "boolean"}
      }
    }
  }
}
```

---

## 2. ASL Hex Examples with Linear Traversals

These examples demonstrate how the hex schema handles complex real-world terrain configurations.

```json
{
  "examples": [
    {
      "id": "H5",
      "baseTerrain": "openGround",
      "elevation": 0,
      "linearTraversals": [
        {
          "type": "road",
          "subtype": "paved",
          "enters": 3,
          "exits": 0,
          "elevation": "ground"
        },
        {
          "type": "stream",
          "enters": 4,
          "exits": 1,
          "elevation": "depression",
          "attributes": {
            "depth": "deep",
            "current": {"force": "moderate", "direction": 1}
          }
        }
      ],
      "intersections": [
        {
          "features": [0, 1],
          "intersectionType": "bridge",
          "elevation": "feature0",
          "attributes": {
            "bridgeType": "stone",
            "capacity": "heavy"
          }
        }
      ]
    },
    {
      "id": "K3",
      "baseTerrain": "openGround", 
      "elevation": 0,
      "linearTraversals": [
        {
          "type": "road",
          "subtype": "dirt",
          "enters": 2,
          "exits": 5,
          "elevation": "ground"
        },
        {
          "type": "stream",
          "enters": 3,
          "exits": 0,
          "elevation": "depression",
          "attributes": {"depth": "shallow"}
        }
      ],
      "intersections": [
        {
          "features": [0, 1],
          "intersectionType": "ford",
          "elevation": "same"
        }
      ]
    },
    {
      "id": "M7",
      "baseTerrain": "openGround",
      "elevation": 0,
      "linearTraversals": [
        {
          "type": "railroad",
          "subtype": "elevated",
          "enters": 1,
          "exits": 4,
          "elevation": "elevated"
        },
        {
          "type": "road",
          "subtype": "paved",
          "enters": 2,
          "exits": 5,
          "elevation": "ground"
        },
        {
          "type": "stream",
          "enters": 3,
          "exits": 0,
          "elevation": "depression",
          "attributes": {"depth": "deep"}
        }
      ],
      "intersections": [
        {
          "features": [0, 1],
          "intersectionType": "crossing",
          "elevation": "feature0"
        },
        {
          "features": [1, 2],
          "intersectionType": "bridge",
          "elevation": "feature1",
          "attributes": {
            "bridgeType": "wooden",
            "capacity": "vehicle"
          }
        },
        {
          "features": [0, 2],
          "intersectionType": "culvert",
          "elevation": "feature0"
        }
      ]
    },
    {
      "id": "P4",
      "baseTerrain": "woods",
      "elevation": 1,
      "linearTraversals": [
        {
          "type": "path",
          "enters": 0,
          "exits": 3,
          "elevation": "ground"
        },
        {
          "type": "stream",
          "enters": 1,
          "exits": 4,
          "elevation": "depression",
          "attributes": {"depth": "shallow"}
        }
      ],
      "intersections": [],
      "notes": "Path and stream don't intersect - they traverse different parts of the hex"
    },
    {
      "id": "T8",
      "baseTerrain": "openGround",
      "elevation": 0,
      "linearTraversals": [
        {
          "type": "road",
          "subtype": "sunken",
          "enters": 0,
          "exits": null,
          "elevation": "sunken",
          "notes": "Road terminates at building"
        }
      ],
      "building": {
        "type": "stone",
        "levels": 2
      }
    }
  ]
}
```

---

## 3. Complete ASL Scene System

This file contains scene definitions that compose hex templates into reusable tactical arrangements.

```json
{
  "sceneDefinitions": {
    "scene_village_square": {
      "sceneId": "d4e5f6a7-1b2c-3d4e-5f6a-7b8c9d0e1f2a",
      "name": "Village Square",
      "size": {"width": 5, "height": 4},
      "tags": ["urban", "defensive", "crossroads"],
      "hexes": [
        {"offset": {"x": 0, "y": 0}, "templateId": "stone-building-2L", "relativeRotation": 0},
        {"offset": {"x": 1, "y": 0}, "templateId": "stone-building-2L", "relativeRotation": 0},
        {"offset": {"x": 2, "y": 0}, "templateId": "open-ground", "relativeRotation": 0},
        {"offset": {"x": 3, "y": 0}, "templateId": "wooden-building-1L", "relativeRotation": 0},
        {"offset": {"x": 0, "y": 1}, "templateId": "stone-building-2L", "relativeRotation": 0},
        {"offset": {"x": 1, "y": 1}, "templateId": "open-ground", "relativeRotation": 0},
        {"offset": {"x": 2, "y": 1}, "templateId": "open-ground-fountain", "relativeRotation": 0},
        {"offset": {"x": 3, "y": 1}, "templateId": "open-ground", "relativeRotation": 0},
        {"offset": {"x": 2, "y": 2}, "templateId": "open-ground", "relativeRotation": 0}
      ],
      "connections": {
        "roads": [
          {"from": {"x": 1, "y": 1}, "to": {"x": 1, "y": 0}, "type": "paved"},
          {"from": {"x": 1, "y": 1}, "to": {"x": 2, "y": 1}, "type": "paved"},
          {"from": {"x": 2, "y": 1}, "to": {"x": 3, "y": 1}, "type": "paved"},
          {"from": {"x": 2, "y": 1}, "to": {"x": 2, "y": 2}, "type": "paved"}
        ],
        "walls": [
          {"hexes": [{"x": 0, "y": 0}, {"x": 1, "y": 0}], "side": 1}
        ]
      },
      "anchors": {
        "north": {"x": 2, "y": 0},
        "east": {"x": 4, "y": 1},
        "south": {"x": 2, "y": 3},
        "west": {"x": 0, "y": 1}
      }
    },
    "scene_river_crossing": {
      "sceneId": "e5f6g7b8-2c3d-4e5f-6g7b-8c9d0e1f2b3c",
      "name": "Fortified River Crossing",
      "size": {"width": 7, "height": 3},
      "hexes": [
        {"offset": {"x": 0, "y": 1}, "templateId": "river-deep", "relativeRotation": 0},
        {"offset": {"x": 1, "y": 1}, "templateId": "river-deep", "relativeRotation": 0},
        {"offset": {"x": 2, "y": 1}, "templateId": "river-deep-bridge", "relativeRotation": 0},
        {"offset": {"x": 3, "y": 1}, "templateId": "river-deep", "relativeRotation": 0},
        {"offset": {"x": 4, "y": 1}, "templateId": "river-deep", "relativeRotation": 0},
        {"offset": {"x": 2, "y": 0}, "templateId": "open-ground", "relativeRotation": 0},
        {"offset": {"x": 2, "y": 2}, "templateId": "open-ground", "relativeRotation": 0},
        {"offset": {"x": 1, "y": 0}, "templateId": "stone-building-1L", "relativeRotation": 0},
        {"offset": {"x": 3, "y": 2}, "templateId": "stone-building-1L", "relativeRotation": 0}
      ],
      "fortifications": [
        {"hex": {"x": 1, "y": 0}, "type": "pillbox", "ca": 3},
        {"hex": {"x": 3, "y": 2}, "type": "pillbox", "ca": 0}
      ]
    },
    "scene_hilltop_woods": {
      "sceneId": "f6g7h8c9-3d4e-5f6g-7h8c-9d0e1f2c3d4e",
      "name": "Defensive Hilltop",
      "size": {"width": 4, "height": 4},
      "baseElevation": 1,
      "hexes": [
        {"offset": {"x": 1, "y": 1}, "templateId": "woods", "elevationDelta": 1},
        {"offset": {"x": 2, "y": 1}, "templateId": "woods", "elevationDelta": 1},
        {"offset": {"x": 1, "y": 2}, "templateId": "woods", "elevationDelta": 1},
        {"offset": {"x": 2, "y": 2}, "templateId": "woods", "elevationDelta": 1},
        {"offset": {"x": 0, "y": 1}, "templateId": "open-ground", "elevationDelta": 0},
        {"offset": {"x": 3, "y": 1}, "templateId": "open-ground", "elevationDelta": 0},
        {"offset": {"x": 1, "y": 0}, "templateId": "open-ground", "elevationDelta": 0},
        {"offset": {"x": 1, "y": 3}, "templateId": "open-ground", "elevationDelta": 0}
      ],
      "properties": {
        "defensive": true,
        "losAdvantage": "central"
      }
    }
  },
  "sceneDeployment": {
    "mapId": "custom_battle_01",
    "deployments": [
      {
        "sceneId": "d4e5f6a7-1b2c-3d4e-5f6a-7b8c9d0e1f2a",
        "anchorHex": "M7",
        "anchorPoint": {"x": 2, "y": 2},
        "rotation": 0,
        "modifications": {
          "fortifications": [
            {"hex": {"x": 1, "y": 1}, "add": "wire"}
          ]
        }
      },
      {
        "sceneId": "e5f6g7b8-2c3d-4e5f-6g7b-8c9d0e1f2b3c",
        "anchorHex": "J3",
        "anchorPoint": {"x": 2, "y": 1},
        "rotation": 0
      },
      {
        "sceneId": "f6g7h8c9-3d4e-5f6g-7h8c-9d0e1f2c3d4e",
        "anchorHex": "P2",
        "rotation": 2,
        "elevationAdjust": 1
      }
    ]
  },
  "sceneRules": {
    "placement": {
      "overlap": "prohibited",
      "edgeBuffer": 1,
      "elevationConstraints": true
    },
    "validation": {
      "waterContinuity": true,
      "roadConnectivity": true,
      "buildingAlignment": true
    }
  },
  "sceneLibraries": {
    "terrain": ["village_square", "river_crossing", "hilltop_woods"],
    "campaign": ["stalingrad_factory", "normandy_hedgerow", "pacific_bunker"],
    "special": ["airfield_complex", "rail_junction", "fortified_farm"]
  }
}
```

---

## 4. ASL Scene Coordinate System

This markdown explains how scenes use internal coordinates that are translated to map positions during deployment.

```markdown
# ASL Scene Coordinate System

## Internal Scene Coordinates

Scenes use **internal coordinates** independent of map placement:
- Origin: (0,0) at top-left
- Range: (0,0) to (width-1, height-1)
- No board references until deployment

## Example: 5×5 Village Scene

### Internal Structure
```
(0,0) (1,0) (2,0) (3,0) (4,0)
(0,1) (1,1) (2,1) (3,1) (4,1)
(0,2) (1,2) CENTER (3,2) (4,2)
(0,3) (1,3) (2,3) (3,3) (4,3)
(0,4) (1,4) (2,4) (3,4) (4,4)
```

### Scene Definition
```json
{
  "sceneId": "village_001",
  "size": {"width": 5, "height": 5},
  "centerHex": {"x": 2, "y": 2},
  "hexes": [
    {"offset": {"x": 2, "y": 2}, "templateId": "fountain"},
    {"offset": {"x": 0, "y": 0}, "templateId": "building"}
  ]
}
```

## Deployment Translation

### Deployment Specification
```json
{
  "sceneId": "village_001",
  "anchorHex": "M7",
  "anchorPoint": {"x": 2, "y": 2},
  "rotation": 0
}
```

### Translation Formula
- Scene internal (2,2) → Map hex M7
- Scene internal (0,0) → Map hex K5
- Scene internal (4,4) → Map hex O9

### Rotation Before Translation
When rotation ≠ 0:
1. Rotate internal coordinates around center
2. Then translate to map coordinates

## Key Concepts

**Scene Space**: Abstract 0-based grid
**Map Space**: Board hex coordinates (A1, B2, etc.)
**Anchor Point**: Which scene hex aligns with anchorHex
**Translation**: anchorHex - anchorPoint = offset for all hexes
```

---

## 5. Complete ASL Map Schema

This schema defines how scenes and individual hexes are deployed to create complete battlefields.

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "ASL Map Definition",
  "description": "Schema for ASL maps with deployed scenes and hex templates",
  "type": "object",
  "required": ["mapId", "dimensions", "hexes"],
  "properties": {
    "mapId": {
      "type": "string",
      "description": "Unique map identifier"
    },
    "name": {
      "type": "string",
      "description": "Map name"
    },
    "dimensions": {
      "type": "object",
      "required": ["width", "height"],
      "properties": {
        "width": {"type": "integer"},
        "height": {"type": "integer"}
      }
    },
    "hexes": {
      "type": "object",
      "description": "Map hexes by coordinate",
      "patternProperties": {
        "^[A-Z]{1,2}[0-9]{1,2}$": {
          "$ref": "#/definitions/deployedHex"
        }
      }
    },
    "deploymentHistory": {
      "type": "array",
      "description": "Record of all deployments",
      "items": {
        "$ref": "#/definitions/deploymentRecord"
      }
    }
  },
  "definitions": {
    "deployedHex": {
      "type": "object",
      "required": ["templateId", "terrain", "elevation"],
      "properties": {
        "templateId": {
          "type": "string",
          "description": "Hex template GUID"
        },
        "terrain": {
          "type": "string"
        },
        "elevation": {
          "type": "integer"
        },
        "source": {
          "$ref": "#/definitions/hexSource"
        },
        "hexsides": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/hexsideState"
          }
        },
        "features": {
          "type": "array"
        },
        "overlays": {
          "type": "array"
        }
      }
    },
    "hexSource": {
      "type": "object",
      "description": "Origin of this hex",
      "properties": {
        "type": {
          "type": "string",
          "enum": ["scene", "template", "manual"]
        },
        "sceneId": {
          "type": "string",
          "description": "Source scene ID if from scene"
        },
        "sceneOffset": {
          "type": "object",
          "properties": {
            "x": {"type": "integer"},
            "y": {"type": "integer"}
          }
        },
        "deploymentId": {
          "type": "string",
          "description": "Which deployment created this hex"
        }
      }
    },
    "hexsideState": {
      "type": "object",
      "properties": {
        "side": {
          "type": "integer",
          "minimum": 0,
          "maximum": 5
        },
        "terrain": {
          "type": "array",
          "items": {"type": "string"}
        },
        "connectsTo": {
          "type": "string",
          "pattern": "^[A-Z]{1,2}[0-9]{1,2}$"
        }
      }
    },
    "deploymentRecord": {
      "type": "object",
      "required": ["deploymentId", "timestamp", "type"],
      "properties": {
        "deploymentId": {
          "type": "string"
        },
        "timestamp": {
          "type": "string",
          "format": "date-time"
        },
        "type": {
          "type": "string",
          "enum": ["scene", "hexTemplate"]
        },
        "sceneDeployment": {
          "$ref": "#/definitions/sceneDeploymentRecord"
        },
        "hexDeployment": {
          "$ref": "#/definitions/hexDeploymentRecord"
        }
      }
    },
    "sceneDeploymentRecord": {
      "type": "object",
      "required": ["sceneId", "anchorHex", "rotation"],
      "properties": {
        "sceneId": {
          "type": "string"
        },
        "anchorHex": {
          "type": "string"
        },
        "anchorPoint": {
          "type": "object",
          "properties": {
            "x": {"type": "integer"},
            "y": {"type": "integer"}
          }
        },
        "rotation": {
          "type": "integer"
        },
        "affectedHexes": {
          "type": "array",
          "items": {"type": "string"}
        }
      }
    },
    "hexDeploymentRecord": {
      "type": "object",
      "required": ["templateId", "targetHex"],
      "properties": {
        "templateId": {
          "type": "string"
        },
        "targetHex": {
          "type": "string"
        }
      }
    }
  }
}
```

---

## 6. Linear Traversal Analysis

This companion document explains how the linear traversal system works and its impact on gameplay.

```markdown
# Linear Traversal Schema Analysis

## Schema Capabilities

The updated schema handles linear features through hexes with three key components:

### 1. Linear Traversals
Defines features that cross through a hex:
- **Entry/Exit**: Specific hexsides (0-5)
- **Type**: road, railroad, stream, path
- **Elevation**: ground, sunken, elevated, depression
- **Attributes**: width, depth, current

### 2. Intersections
Where features meet within a hex:
- **Bridge**: Road/rail over water
- **Ford**: Road crosses shallow water
- **Culvert**: Water under elevated feature
- **Crossing**: Features at different elevations
- **Confluence**: Waters merge

### 3. Complex Examples

**Simple Bridge (H5)**:
- Road enters side 3, exits side 0
- Stream enters side 4, exits side 1
- Stone bridge carries road over stream

**Multi-Feature (M7)**:
- Elevated railroad (1→4)
- Ground road (2→5)
- Stream (3→0)
- Three intersections: RR/road crossing, road/stream bridge, RR/stream culvert

## Validation Rules

1. **Connectivity**: Exit of one hex must match enter of adjacent
2. **Intersection Logic**: Crossing features must have intersection defined
3. **Elevation Consistency**: Bridges require elevation difference
4. **Water Flow**: Stream direction must be consistent

## Movement/LOS Impact

- Roads provide movement bonus only along their path
- Bridges allow crossing without water penalties
- Fords still incur water movement costs
- Elevated features block LOS perpendicular to path

This schema fully supports ASL's complex linear terrain interactions.
```

---

## Summary

These schemas form a complete technical specification for implementing ASL's terrain system. Each component serves a specific purpose:

1. **Hex Templates** provide reusable terrain definitions with all possible properties
2. **Scenes** compose templates into tactical arrangements with internal logic
3. **Maps** deploy scenes and hexes to create specific battlefields
4. **The Coordinate System** enables flexible placement through mathematical transformations

The three-tier architecture with property inheritance creates a flexible system that captures ASL's complexity while remaining maintainable and extensible. The linear traversal system handles unique features like roads and streams that cross through hexes, while the comprehensive property definitions capture every terrain aspect that affects gameplay.

This appendix provides all technical details needed to implement the system in ASP.NET Core 9 with Blazor Server, ensuring perfect fidelity to ASL's game mechanics.