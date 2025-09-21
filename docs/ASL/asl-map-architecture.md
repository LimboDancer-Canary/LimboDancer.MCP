# ASL Map, Scene, Hex Definition and Architecture

## Introduction: Understanding the Design Philosophy

Advanced Squad Leader represents one of the most complex tactical wargaming systems ever created, with terrain mechanics that profoundly impact every aspect of gameplay. Our architecture for ASP.NET Core 9 with Blazor Server must capture this complexity while remaining flexible enough to handle ASL's extensive exception-based rule system.

The fundamental challenge lies in how ASL treats terrain. Unlike simpler games where a woods hex is just "woods," ASL distinguishes between different types of woods (regular, pine, forest), considers their interaction with other terrain features (paths, roads, streams), and applies dozens of conditional modifiers based on unit type, weather, and tactical situation. A single hex might contain woods with a road running through it, crossed by a stream with a bridge, all at different elevation levels with distinct effects on movement and combat.

This document explains how our three-tier architecture (Hex Templates, Scenes, and Maps) elegantly handles this complexity while maintaining the flexibility needed for ASL's vast array of scenarios.

## The Three-Tier Architecture: Why This Approach?

### Understanding the Need for Multiple Layers

Consider a typical ASL scenario setup. You might need to place a village on board 3, add some rubble to represent previous fighting, overlay a frozen stream, and designate certain buildings as fortified. Traditional approaches would require either modifying the base board (losing reusability) or creating entirely new boards for each variation (explosion of data).

Our three-tier system mirrors how ASL players actually think about terrain:

**Hex Templates** represent the fundamental building blocks - a stone building is always a stone building with certain inherent properties. According to ASL rule 23.3, stone buildings provide a +3 TEM (Terrain Effects Modifier), while wooden buildings provide +2. These base properties rarely change.

**Scenes** represent tactical situations that appear repeatedly across scenarios - a village square, a river crossing, a fortified hill position. These arrangements have their own internal logic. A village square typically has buildings arranged around a central open area with connecting roads. By defining these as reusable scenes, we can quickly construct complex tactical situations.

**Maps** represent the final deployed state for a specific scenario. This is where scenes are placed, individual hexes might be modified (perhaps a building is already rubbled), and the complete tactical puzzle comes together.

### Property Inheritance and Override Hierarchy

The magic of this system lies in how properties flow through the tiers. Each tier inherits properties from the previous tier and can selectively override them. This mirrors how ASL scenarios actually work.

Consider this real gameplay example from the rulebook: A stone building (defined in our Hex Template) normally has 2 levels. But in a particular scene representing a factory complex, we might override this to 3 levels to represent industrial buildings. When deployed to a map for a Stalingrad scenario, we might further override it to show the upper level has been destroyed by artillery, leaving only 1 functional level.

The inheritance chain works as follows:
```
Hex Template (base) → Scene (overrides hex) → Map (overrides scene/hex)
```

This is not just a technical convenience - it directly models how ASL scenarios are designed. The base terrain is modified by SSR (Special Scenario Rules), which our override system handles elegantly.

## Hex Template Schema: Capturing ASL's Terrain Complexity

### Base Terrain and Its Implications

Every hex in ASL starts with a base terrain type. According to ASL rules, these fundamental terrain types each have specific game effects:

**Open Ground** (ASL rule 1.1) provides no inherent protection and allows normal movement. However, units moving in Open Ground are subject to FFMO (First Fire Movement in Open) penalties, making them extremely vulnerable. Our hex template captures this with a baseTerrain property.

**Woods** (ASL rule 13) are a one-level obstacle to LOS, provide +1 TEM, and cost 2 MF (Movement Factors) for infantry to enter. But woods interact differently with other features - a road through woods negates the movement penalty along the road, while woods containing a stream create entirely different tactical considerations.

**Buildings** vary dramatically based on construction type and size. ASL rules 23.21-23.24 define precise mechanics for single-story houses, two-story houses, multi-story buildings, and even third-level structures. Our schema captures not just the building type but also critical properties like:

- Number of levels (affects LOS and stacking)
- Presence of stairwells (controls vertical movement between levels)
- Construction type (stone vs wooden affects TEM and collapse probability)
- Multi-hex designation (large buildings spanning multiple hexes)

### The Innovation of Linear Traversals

One of ASL's most complex terrain concepts involves linear features that cross through hexes. Traditional hex-based games struggle with this, but ASL's detailed rules require precise tracking of how roads, streams, and railroads traverse each hex.

Consider ASL rule 3.1 regarding roads: "Infantry movement along a road costs 1 MF per hex instead of the normal terrain cost." This seems simple until you realize a road might enter one hexside and exit another, and only movement along that specific path gets the benefit. Our linearTraversals array captures this perfectly:

```json
"linearTraversals": [
  {
    "type": "road",
    "subtype": "paved",
    "enters": 3,
    "exits": 0,
    "elevation": "ground"
  }
]
```

This indicates a paved road enters through hexside 3 and exits through hexside 0. Only units moving from hexside 3 to 0 (or vice versa) gain the road movement benefit. Units entering from hexside 2 must pay full terrain costs.

### Complex Intersections and Their Game Effects

Where linear features meet, ASL's complexity truly shines. A road crossing a stream might have a bridge (allowing vehicles to cross), a ford (requiring bog checks), or no crossing at all (forcing dismounted movement). Rule B6 covers bridges in detail, while B20.8 handles fords. Our intersection system captures these critical tactical features:

```json
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
```

This intersection indicates the first linear traversal (index 0, the road) crosses over the second (index 1, the stream) via a stone bridge capable of supporting heavy vehicles. The tactical implications are profound - the bridge becomes a chokepoint that might be destroyed, blocked, or heavily defended.

### Elevation and Line of Sight Complexity

ASL's LOS rules (Section A6) form the heart of tactical play. Higher elevation provides advantages, but the devil is in the details. Rule 10.23 defines "Blind Hexes" - areas that can't be seen due to intervening terrain despite height advantages. Our pre-computed LOS data captures these nuances:

```json
"los": {
  "P5": {
    "clear": false,
    "blockedBy": ["L4", "M4", "N4"],
    "blockingTerrain": ["Woods", "Building"]
  }
}
```

This pre-computation transforms runtime LOS checking from complex calculations involving elevation differences, obstacle heights, and blind hex determinations into simple lookups. When a player asks "Can I see from K3 to P5?" the answer is instantaneous.

## Scene System: Modeling Tactical Situations

### Why Scenes Matter in ASL

ASL scenarios often feature recurring tactical situations. A village appears in dozens of scenarios, but manually placing each building, road, and wall would be tedious and error-prone. Scenes solve this by pre-defining these common arrangements.

More importantly, scenes maintain their own internal logic. In a village scene, buildings cluster around a central square, roads connect logically, and walls might protect certain approaches. This internal coherence makes scenarios more realistic and playable.

### Internal Coordinate System

Scenes use an internal coordinate system starting at (0,0) in the top-left corner. This deliberate design choice provides several benefits:

**Rotation becomes mathematical rather than manual.** When deploying a scene rotated 90 degrees, we can apply a simple transformation matrix rather than manually adjusting each hex.

**Scenes remain board-agnostic.** The same village scene can be deployed to any board at any location, making it truly reusable.

**Relative positioning stays consistent.** If a machine gun position overlooks the village square in the scene definition, this tactical relationship persists regardless of where or how the scene is deployed.

### Scene Deployment and Anchor Points

The deployment system uses an anchor point concept that ASL players intuitively understand. When setting up a scenario, players often think "place the village with its center at hex M7." Our deployment specification captures this exactly:

```json
{
  "sceneId": "village_001",
  "anchorHex": "M7",
  "anchorPoint": {"x": 2, "y": 2},
  "rotation": 0
}
```

This places the scene's internal coordinate (2,2) - typically its center - at map hex M7. All other hexes in the scene maintain their relative positions. If the scene is 5x5 hexes, the corners automatically align to the correct map positions through simple offset calculations.

### Managing Connections and Relationships

Scenes must maintain internal connectivity. Roads should connect logically, walls should form defensive lines, and buildings should relate tactically. Our connection system ensures these relationships persist through deployment:

Roads that connect within a scene remain connected when deployed. Walls maintain their defensive integrity. Multi-hex buildings deploy as unified structures. This automatic maintenance of tactical relationships dramatically simplifies scenario setup while ensuring playable, logical terrain arrangements.

## Map Schema: The Final Battlefield

### From Template to Battlefield

Maps represent the culmination of our terrain system - the actual battlefield where games are played. Each hex on a map traces its lineage through our three-tier system, but represents a specific, concrete tactical situation.

When a hex is deployed to a map, it gains context. That generic "stone building" from the hex template, modified by the "village scene" to have specific connections and relationships, now sits at a specific location (say, K7) with specific tactical implications. Units approaching from J7 might have cover from the building, while those at L8 face open ground.

### Source Tracking and Modification History

Our source tracking system serves multiple purposes beyond mere bookkeeping. By tracking where each hex originated (which template, which scene, which deployment), we enable:

**Scenario reconstruction** - Understanding how a complex battlefield was assembled helps in creating variations or fixing issues.

**Selective updates** - If we improve a scene definition, we can update all maps using that scene while preserving map-specific overrides.

**debugging and validation** - When a hex seems wrong, we can trace its complete history through all three tiers.

### Dynamic Battlefield Evolution

ASL battles are dynamic. Buildings collapse into rubble (rule 24), fires spread (rule 25), and fortifications are constructed (rule 23). Our override system at the map level elegantly handles these changes:

A building that starts intact might be reduced to rubble by heavy artillery. Rather than replacing the entire hex definition, we simply override the relevant properties:

```json
"overrides": {
  "building": {
    "currentLevels": 0,
    "rubbled": true
  },
  "baseTerrain": "rubble"
}
```

The original building information remains (important for victory conditions and terrain type determination), while the current tactical situation reflects the destruction.

## Line of Sight: The Heart of Tactical Combat

### Understanding ASL's LOS Complexity

Line of Sight in ASL involves numerous factors that interact in complex ways. Elevation provides advantages but creates blind hexes. Obstacles block LOS unless the viewer or target has sufficient height advantage. Different terrain types create different obstacle heights. Weather conditions modify visibility. The complexity is staggering.

Rule A6.1 states the basic principle: "A unit may see, and therefore fire at, another unit only if a Line of Sight (LOS) can be traced from the center of the firing unit's hex to the center of the target unit's hex." Simple in concept, fiendishly complex in application.

### Pre-computation Strategy

Rather than calculating LOS at runtime (expensive and error-prone), we pre-compute all possible LOS relationships. This transforms the complex rules of Section A6 into simple lookups:

For each hex pair, we determine:
- Whether LOS exists
- What terrain blocks it (if any)
- Which intermediate hexes create the blocking
- Any special conditions (like blind hexes)

This pre-computation incorporates all the complex rules: obstacle heights (woods are 1 level, buildings vary), elevation advantages (rule 10.1), blind hexes (rule 10.23), and special cases (cliffs, ridges, etc.).

### Tactical Implications of LOS Data

Having instant LOS information transforms gameplay. Players can quickly evaluate positions, plan movement routes, and identify key terrain. The system can highlight dead zones, suggest optimal firing positions, and warn about exposed movement paths.

Consider this tactical scenario: Infantry needs to cross from building K3 to building P5. Our system instantly shows that direct movement is impossible due to LOS from enemy positions at M4. But it also reveals that moving K3→K4→L5→M5→N5→O5→P5 maintains concealment the entire way, using woods and building shadows for cover.

## Implementation Patterns for Blazor Server

### State Management Philosophy

Our Blazor Server implementation maintains game state centrally, with clients receiving updates via SignalR. This architectural choice aligns perfectly with ASL's information transparency - all players see the same battlefield and can verify LOS relationships.

The GameState class maintains the current battlefield configuration, tracking not just where units are but the complete terrain state including all overrides and modifications. This enables rich queries like "Show me all buildings with LOS to hex K5" or "Find all covered routes from my position to the objective."

### Component Design for Complex Terrain

Each terrain type requires careful component design to capture its visual and functional aspects. A building component must show:
- Construction type (color coding: gray for stone, brown for wood)
- Number of levels (visual height representation)
- Current damage state (intact, damaged, rubbled)
- Fortification status
- Any units present at each level

These components update reactively as the game state changes. When artillery reduces a building to rubble, the component immediately reflects this, updating both visual representation and tactical properties.

### Performance Through Intelligence

While pre-computation provides instant LOS queries, we must still handle dynamic battlefield changes efficiently. Our approach uses several optimization strategies:

**Incremental updates** - When terrain changes (building rubbled, smoke placed), we only recalculate affected LOS relationships rather than the entire matrix.

**Lazy loading** - Detailed hex information loads on demand. The initial map shows basic terrain, with full details loading as players interact with specific areas.

**Viewport optimization** - In large scenarios, we only fully render hexes within the current viewport, maintaining performance even on massive campaign maps.

## Validation and Rules Enforcement

### Maintaining ASL's Internal Consistency

ASL's rules contain numerous interdependencies that our validation system must enforce. For example:

When placing a bridge (overlay), the system must verify:
- A linear water feature exists in the hex
- The bridge connects to valid road/path hexsides
- The crossing makes tactical sense (not bridging to nowhere)

Building placement must ensure multi-hex buildings remain contiguous and properly connected. Fortifications must have valid covered arcs. These validations prevent illegal configurations while remaining flexible enough for ASL's edge cases.

### Scenario-Specific Overrides

ASL scenarios often bend or break normal rules through Special Scenario Rules (SSR). Our validation system must be flexible enough to accommodate these while still preventing obvious errors. A scenario might declare "all buildings are fortified" or "treat all woods as pine woods." Our override system at each tier naturally handles these modifications while maintaining base rule integrity.

## Real-World Example: Village Defense Scenario

### Setting the Scene

Let's walk through how our system handles a typical ASL scenario: defending a village against attacking forces. The scenario uses Board 3 with a village scene deployed around hex M7, representing a French village in 1944.

The village scene includes:
- A central square with a fountain (light cover)
- Stone buildings (2-level) forming the perimeter
- Connecting roads allowing rapid movement
- Stone walls providing additional defensive positions

### Deployment Process

First, we deploy the village scene to the map:
```json
{
  "sceneId": "village_square_1944",
  "anchorHex": "M7",
  "anchorPoint": {"x": 3, "y": 3},
  "rotation": 0
}
```

This places the village center at M7. The system automatically positions all buildings, roads, and walls relative to this anchor point.

### Scenario-Specific Modifications

The scenario SSR states "The church at L6 has been damaged by preliminary bombardment - treat upper level as rubbled." Our map override system handles this elegantly:

```json
"L6": {
  "overrides": {
    "building": {
      "currentLevels": 1,
      "upperLevelRubbled": true
    }
  }
}
```

The scenario also adds fortifications: "The defender may set up two fortified building locations." Players select buildings K6 and N7, and the system applies the fortified status, automatically updating TEM calculations and noting the restricted covered arcs.

### Tactical Analysis

With the battlefield prepared, our LOS system reveals the tactical situation:

The church tower at L6, despite damage, still provides excellent LOS along the main road approaching from the south. Buildings K6 and M8 create interlocking fields of fire covering the eastern approach. However, the woods at J8-K9 create a dangerous blind spot that attackers might exploit.

The defender realizes they must either position units to cover this blind spot or accept it as a calculated risk. The road network allows rapid redeployment but also provides high-speed avenues for enemy vehicles. Each tactical decision emerges naturally from the terrain configuration our system has created.

## Integration with ASL Rules Engine

### Bridging Terrain and Rules

While this document focuses on terrain representation, our architecture seamlessly integrates with the broader ASL rules engine. When a unit attempts to move from K7 to L7, the system:

1. Checks the terrain in both hexes (building to open ground)
2. Calculates movement cost (2 MF to exit building)
3. Verifies LOS from all enemy positions (triggering defensive fire opportunities)
4. Applies terrain modifiers (FFMO for movement in open ground)
5. Resolves any triggered events (defensive fire, residual firepower)

This integration happens transparently, with terrain data enriching the rules engine's decision-making.

### Supporting Complex Queries

Our architecture enables sophisticated queries that combine terrain and tactical considerations:

"Find all hexes where I can place a machine gun to cover both the bridge at G5 and the road junction at K7" - The system identifies hexes with LOS to both locations, sufficient elevation for good fields of fire, and appropriate terrain for MG placement.

"Show me all positions where enemy infantry could approach building M7 while maintaining concealment" - Combining movement rules with LOS data reveals covered approach routes.

"What's the optimal path for my tank to reach N9 while minimizing exposure to the AT gun at J4?" - Pathfinding algorithms use our terrain and LOS data to suggest tactically sound routes.

## Future Extensibility

### Accommodating New Terrain Types

ASL continues to evolve, with new modules introducing terrain types like rice paddies, jungle, desert, and urban ruins. Our architecture accommodates these additions naturally:

New hex templates define the base properties of exotic terrain. Scenes capture how this terrain typically appears (jungle clearings, desert villages). Maps deploy these new elements alongside familiar terrain. The override system handles special cases and scenario-specific modifications.

### Historical and Hypothetical Scenarios

Our scene system particularly shines when creating historical scenarios. A "Stalingrad Factory Complex" scene can be meticulously crafted once, then reused across multiple scenarios with appropriate modifications. Hypothetical scenarios benefit similarly - what if the Germans had reached Moscow? Create appropriate urban scenes and deploy them to generate endless tactical variations.

### Campaign Game Support

ASL's campaign games, where terrain changes persist across multiple scenarios, find natural support in our architecture. Building damage accumulates, fortifications are constructed, and the battlefield evolves. Our override system tracks these changes while maintaining the base terrain for reference.

## Conclusion: Elegant Complexity

Our three-tier architecture achieves something remarkable: it captures ASL's tremendous terrain complexity while remaining intuitive and maintainable. By separating concerns across Hex Templates (what terrain is), Scenes (how terrain arranges tactically), and Maps (where terrain deploys specifically), we create a system that mirrors how ASL players and designers think about battlefield terrain.

The property inheritance system provides flexibility without chaos. Pre-computed LOS data delivers performance without sacrificing accuracy. Scene composition accelerates scenario creation while maintaining tactical coherence. Most importantly, every design decision traces back to specific ASL rules and gameplay requirements.

This architecture doesn't just store terrain data - it understands terrain's role in ASL's tactical ecosystem. Whether tracking a single squad's desperate dash across a fire-swept street or orchestrating a massive combined-arms assault, our system provides the detailed terrain information that makes ASL's tactical decisions meaningful.

The battlefield is ready. Let the game begin.

---

# Appendix A: Complete Schema Reference

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
    "description": {
    "type": "string",
    "description": "Human-readable description of the hex template"
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

---

# Appendix B: Complete ASL Terrain Reference

## ASL Terrain Types - Official Order of Presentation

This appendix contains all 37 terrain types as specified in the ASL Rulebook Chapter B. Each terrain type includes its official designation, key properties, and implementation details for our terrain system.

### Terrain Type Index

1. **Open Ground**
   - Base terrain: `openGround`
   - LOS: No obstacle/hindrance
   - TEM: -1 (FFMO applies)
   - Movement: 1 MF infantry, variable vehicle
   - Special: Subject to FFMO unless Height Advantage

2. **Shellholes**
   - Base terrain: `shellholes`
   - LOS: No obstacle
   - TEM: +1 (if entered at 2 MF)
   - Movement: 1-2 MF infantry
   - Special: Treated as Open Ground if entered at 1 MF

3. **Roads**
   - Linear feature: `road` (paved/dirt subtypes)
   - LOS: No effect
   - TEM: DOT (depends on other terrain)
   - Movement: 1 MF along road, ½ MP vehicles
   - Special: FFMO applies if using road movement rate

4. **Sunken Road**
   - Linear feature: `road` with `sunken` elevation
   - LOS: Depression
   - TEM: -1 (FFMO)
   - Movement: 2 MF entry
   - Special: Provides crest status

5. **Elevated Road**
   - Linear feature: `road` with `elevated` elevation
   - LOS: Level One elevation
   - TEM: -1 (FFMO)
   - Movement: 2 MF entry
   - Special: Height Advantage may negate FFMO

6. **Bridges**
   - Intersection type: `bridge`
   - LOS: Hindrance
   - TEM: -1/+1 (depends on LOS direction)
   - Movement: Use road rate
   - Special: Can be destroyed, capacity limits

7. **Runways**
   - Base terrain: `runway`
   - LOS: No effect
   - TEM: -1 (in any Fire Phase)
   - Movement: 1 MF infantry
   - Special: TEM NA vs armor

8. **Sewers & Tunnels**
   - Special overlay: `sewer`
   - LOS: Only to adjacent sewer hex
   - TEM: -2 or NA
   - Movement: ALL MF
   - Special: Underground movement system

9. **Walls & Hedges**
   - Hexside feature: `wall`, `hedge`, `bocage`
   - LOS: Half-level (wall/hedge) or Level One (bocage)
   - TEM: +2 (wall), +1 (hedge), +2/+1 (bocage)
   - Movement: 1 + COT
   - Special: Includes variants (hillside, cactus)

10. **Hills**
    - Defined by: `elevation` property (1-4)
    - LOS: Creates blind hexes
    - TEM: +1 Height Advantage (if no other TEM)
    - Movement: Double MF if ascending
    - Special: Crest lines affect combat

11. **Cliffs**
    - Hexside feature: `cliff`
    - LOS: Blocks completely
    - TEM: -2 vs climber
    - Movement: CLIMB only
    - Special: Requires climbing checks

12. **Brush**
    - Base terrain: `brush`
    - LOS: Hindrance
    - TEM: 0
    - Movement: 2 MF infantry
    - Special: Becomes Open Ground in Deep Snow

13. **Woods**
    - Base terrain: `woods`
    - LOS: Level One obstacle
    - TEM: +1 / -1 (Air Bursts)
    - Movement: 2 MF, vehicles bog
    - Special: Pine woods variant

14. **Orchard**
    - Base terrain: `orchard`
    - LOS: Level One (Apr-Oct) or Hindrance
    - TEM: 0
    - Movement: 1 MF
    - Special: Seasonal, includes Cactus Patch, Olive Grove

15. **Grain**
    - Base terrain: `grain`
    - LOS: Hindrance (Jun-Sep)
    - TEM: 0
    - Movement: 1½ MF (Apr-Sep)
    - Special: Highly seasonal

16. **Marsh**
    - Base terrain: `marsh`
    - LOS: Hindrance to same level
    - TEM: 0 (HE FP halved)
    - Movement: ALL MF, vehicles prohibited
    - Special: Includes Mudflat variant

17. **Crag**
    - Base terrain: `crag`
    - LOS: Not an obstacle
    - TEM: +1
    - Movement: 2 MF infantry
    - Special: No fortifications allowed

18. **Graveyard**
    - Base terrain: `graveyard`
    - LOS: Not an obstacle
    - TEM: +1
    - Movement: 1 MF
    - Special: Wall/hedge hexsides

19. **Gullies**
    - Base terrain: `gully`
    - LOS: Depression (two-level)
    - TEM: -1 (Crest) / 0 (in gully)
    - Movement: 2 MF + COT
    - Special: Complex crest rules

20. **Streams & Crest Status**
    - Base terrain/Linear: `stream`
    - LOS: Depression
    - TEM: -1 (Crest)
    - Movement: Varies by depth
    - Special: Shallow/Deep/Flooded states

21. **Water Obstacles**
    - Base terrain: `river`, `canal`, `pond`, `lake`, `ocean`
    - LOS: No effect
    - TEM: -1
    - Movement: Prohibited (except boats/swimming)
    - Special: Freezing possible

22. **Valley**
    - Base terrain: `valley`
    - LOS: Two-level depression
    - TEM: -1 unless entrenched
    - Movement: Standard
    - Special: Rare terrain type

23. **Buildings**
    - Building property: `wooden`, `stone`, etc.
    - LOS: Obstacle (height = levels)
    - TEM: +1 wooden, +2 stone, +3 stone ground
    - Movement: 2 MF
    - Special: Multi-hex, factories, marketplaces

24. **Rubble**
    - Base terrain: `rubble`
    - LOS: Hindrance
    - TEM: +1
    - Movement: 2 MF + COT
    - Special: From destroyed buildings

25. **Fire**
    - Hex condition: `fire` (smoke/flame/blaze)
    - LOS: Hindrance/Obstacle
    - TEM: NA
    - Movement: Prohibited in Blaze
    - Special: Spreads via kindling numbers

26. **Wire**
    - Fortification: `wire`
    - LOS: No effect
    - TEM: 0
    - Movement: +1 MF
    - Special: Can be breached

27. **Entrenchments**
    - Fortification: `entrenchment`
    - LOS: No effect
    - TEM: +2 to +4
    - Movement: 1 MF to enter
    - Special: Foxholes, trenches

28. **Minefields**
    - Hidden overlay: `minefield`
    - LOS: No effect
    - TEM: NA
    - Movement: Attack triggered
    - Special: Various types (AP, AT)

29. **Roadblocks**
    - Fortification: `roadblock`
    - LOS: Hindrance
    - TEM: +1
    - Movement: Blocks vehicles
    - Special: Placed on roads

30. **Pillboxes**
    - Fortification: `pillbox`
    - LOS: Obstacle
    - TEM: +3 to +5
    - Movement: Via entrance only
    - Special: Multiple CA types

31. **Village Terrain**
    - Area designation: `villageTerrain`
    - LOS: Per component terrain
    - TEM: Per component terrain
    - Movement: Per component terrain
    - Special: Combines multiple terrain types

32. **Railroads**
    - Linear feature: `railroad`
    - LOS: Embanked = hindrance
    - TEM: +1 if embanked
    - Movement: 1 MF along RR
    - Special: RR crossing rules

33. **Stream-Hex Terrain**
    - Hex terrain: Combined types
    - LOS: Per dominant terrain
    - TEM: Per dominant terrain
    - Movement: Per dominant terrain
    - Special: Fordable in places

34. **Towers**
    - Building subtype: `tower`
    - LOS: High vantage point
    - TEM: Variable
    - Movement: Stairwell required
    - Special: Extended LOS range

35. **Light Woods**
    - Base terrain: `lightWoods`
    - LOS: Hindrance only
    - TEM: +1
    - Movement: 1.5 MF
    - Special: Seasonal effects

36. **Prepared Fire Zone**
    - Overlay designation: `preparedFireZone`
    - LOS: Cleared
    - TEM: -1
    - Movement: As Open Ground
    - Special: Scenario specific

37. **Debris**
    - Base terrain: `debris`
    - LOS: Hindrance
    - TEM: 0
    - Movement: +1 MF/MP
    - Special: From destruction

## Implementation Notes

### Schema Coverage
Our current schema fully supports:
- All base terrain types through the `baseTerrain` enum
- Linear features (roads, streams, RR) via `linearTraversals`
- Hexside features (walls, hedges, cliffs) via `hexsides`
- Buildings through the `building` property
- Fortifications through the `fortifications` array
- Overlays and conditions through respective properties

### Key Design Decisions
1. **Terrain Precedence**: When multiple terrain types exist in a hex, the dominant type determines base properties
2. **Linear Features**: Roads, streams, and railroads use the traversal system to track entry/exit hexsides
3. **Elevation**: Stored as integer levels, with special handling for depressions and crest status
4. **Seasonal Effects**: Handled through the `seasonal` property for grain, orchards, and streams
5. **Combat Modifiers**: TEM and hindrance values pre-computed and stored

### ASL Rules Integration
Each terrain type references specific ASL rule sections:
- Open Ground: B1
- Shellholes: B2
- Roads: B3
- Buildings: B23
- And so forth...

This allows direct correlation between our implementation and the official rules, ensuring accuracy and enabling rule lookups during gameplay.  
  
  
---

# Appendix C: Building Terrain Type - Schema and Design Implications

## Critical Design Principle: Buildings Are NOT Base Terrain

### ASL Rule B.1 SYMBOLOGY
*"In some cases a hex will contain more than one terrain type with neither dominant over the other, in which case the terrain effects of both types are cumulative."*

The rulebook explicitly states that **"2I9 is a combination building-woods hex"**, confirming that buildings exist **on top of** other terrain types rather than replacing them.

## Building Schema Design

### Core Building Properties

```json
{
  "buildingProperties": {
    "type": "object",
    "properties": {
      "type": {
        "type": "string",
        "enum": ["wooden", "stone", "factory", "marketplace", "rowhouse", "church"],
        "description": "Building construction type affects TEM and special rules"
      },
      "levels": {
        "type": "integer",
        "minimum": 1,
        "maximum": 4,
        "description": "Number of building levels (floors)"
      },
      "currentLevels": {
        "type": "integer",
        "description": "Actual levels after damage/rubble"
      },
      "hasStairwell": {
        "type": "boolean",
        "description": "Inherent stairwell if no printed stairwells"
      },
      "stairwells": {
        "type": "array",
        "description": "Printed stairwell positions for multi-level access",
        "items": {
          "type": "object",
          "properties": {
            "id": {"type": "string"},
            "position": {
              "type": "object",
              "properties": {
                "x": {"type": "number", "description": "Relative X (0-1)"},
                "y": {"type": "number", "description": "Relative Y (0-1)"}
              }
            },
            "accessibleLevels": {
              "type": "array",
              "items": {"type": "integer"},
              "description": "Which levels this stairwell connects"
            }
          }
        }
      },
      "hasCellar": {
        "type": "boolean",
        "default": true,
        "description": "Per B23.41 - AFVs can fall through"
      },
      "rooftop": {
        "type": "boolean",
        "default": false,
        "description": "Per B23.8 - Only by SSR, no TEM"
      },
      "fortified": {
        "type": "boolean",
        "default": false,
        "description": "Per B23.9 - +1 additional TEM"
      },
      "internalWalls": {
        "type": "array",
        "description": "For rowhouses - black bars between sections",
        "items": {
          "type": "object",
          "properties": {
            "hexside": {
              "type": "integer",
              "minimum": 0,
              "maximum": 5
            },
            "breachable": {
              "type": "boolean",
              "default": true
            },
            "breached": {
              "type": "boolean",
              "default": false,
              "description": "Has this wall been breached by DC?"
            }
          }
        }
      },
      "connections": {
        "type": "array",
        "description": "Multi-hex building connections",
        "items": {
          "type": "object",
          "required": ["hexside", "toHex"],
          "properties": {
            "hexside": {
              "type": "integer",
              "minimum": 0,
              "maximum": 5,
              "description": "Which hexside has the connection"
            },
            "toHex": {
              "type": "string",
              "description": "Adjacent hex coordinate (e.g., 'K5')"
            },
            "connectionType": {
              "type": "string",
              "enum": ["wall", "passage", "covered", "open"],
              "description": "Type of building connection"
            },
            "width": {
              "type": "number",
              "description": "Width as fraction of hexside (0-1)",
              "default": 0.5
            },
            "offset": {
              "type": "number",
              "description": "Position along hexside (0-1)",
              "default": 0.5
            }
          }
        }
      },
      "multiHexId": {
        "type": "string",
        "description": "Shared ID for all hexes of the same building"
      }
    }
  }
}
```

## Building Type Specifications

### 1. **Standard Buildings**

#### Wooden Buildings (Brown)
- **TEM**: +2
- **Fire**: Kindle on DR ≥ 7, Spread on DR ≥ 8
- **Levels**: 1-2 typical
- **Visual**: Brown color (#8b6914)

#### Stone Buildings (Gray)
- **TEM**: +3 (ground level), +4 (upper levels vs Indirect Fire)
- **Fire**: Kindle on DR ≥ 8, Spread on DR ≥ 9
- **Levels**: 1-4 possible
- **Visual**: Gray color (#8b7d6b)

### 2. **Special Building Types**

#### Factory (B23.74)
- **Height**: 1½ levels (no stairwell) or 2½ levels (with stairwell)
- **Occupancy**: All units at ground level only
- **LOS**: Special rules - hindrance within building
- **TEM**: +3 normal, +1 when firing through same building
- **Movement**: 1 MF between Factory hexes
- **Special Features**:
  - Vehicular-sized entrances (stairwell hexes)
  - No cellars
  - Rooftop access points

#### Marketplace (B23.73)
- **Ground Level**: No obstacle (open ground)
- **Upper Level**: Overhang configuration
- **Access**: External staircase only
- **Special**: Units can move through ground level as Open Ground

#### Rowhouse (B23.71)
- **Walls**: Black bars block LOS between sections
- **Movement**: Special bypass rules at ground level (3 MF)
- **Breach**: Can breach walls with DC
- **Heights**: Can have variable heights (1 and 2 levels)
- **Control**: Each section counts as separate building for rout

#### Church
- **Steeple**: Special LOS vantage point
- **Construction**: Usually stone
- **Special**: Often SSR-defined features

### 3. **Building Features by Level**

#### Ground Level
- Normal building TEM applies
- Entry cost: 2 MF
- May contain cellars (AFV bog/fall risk)
- Fortification possible (+1 TEM)

#### Upper Levels (1-3)
- Accessed via stairwells (1 MF between levels)
- +1 TEM vs Indirect Fire per level above ground
- Multi-hex buildings connect at same level

#### Rooftops (B23.8)
- Only exist by SSR
- No building TEM (treated as Open Ground)
- +1 Height Advantage only
- Cannot be fortified
- All multi-level buildings have inherent rooftop access

## Hex Design Implications

### Visual Layering
```svg
<!-- Example: Woods hex with Stone Building -->
<g id="hex-2I9">
  <!-- Layer 1: Base terrain -->
  <polygon points="..." fill="url(#woods-pattern)"/>
  
  <!-- Layer 2: Building footprint -->
  <rect x="18" y="16" width="24" height="20" 
        fill="#8b7d6b" stroke="#5c5248" opacity="0.9"/>
  
  <!-- Layer 3: Building features -->
  <circle cx="30" cy="26" r="1.5" fill="white" 
          stroke="black" stroke-width="0.3"/> <!-- Stairwell -->
  
  <!-- Layer 4: Level indicator (optional) -->
  <text x="38" y="20" font-size="3" fill="#fff">2</text>
</g>
```

### Multi-Hex Building Connections
```svg
<!-- Building segment with connection indicator -->
<g id="building-segment">
  <!-- Main building area -->
  <rect x="15" y="16" width="30" height="20" fill="#8b7d6b"/>
  
  <!-- Connection extension to hex edge -->
  <rect x="45" y="18" width="15" height="16" 
        fill="#8b7d6b" stroke="none"/>
  
  <!-- Visual indicator of connection -->
  <line x1="45" y1="18" x2="45" y2="34" 
        stroke="#5c5248" stroke-width="0.5" stroke-dasharray="2,1"/>
</g>
```

### Rowhouse Black Bars
```svg
<!-- Rowhouse with internal walls -->
<g id="rowhouse">
  <!-- Building sections -->
  <rect x="15" y="16" width="30" height="20" fill="#8b7d6b"/>
  
  <!-- Black bar (internal wall) -->
  <line x1="30" y1="16" x2="30" y2="36" 
        stroke="black" stroke-width="2"/>
  
  <!-- Breach indicator (if breached) -->
  <circle cx="30" cy="26" r="3" fill="white" stroke="red" 
          stroke-width="1" opacity="0.8"/>
</g>
```

## Implementation Guidelines

### 1. **Terrain Combination Logic**
- Buildings modify but don't replace base terrain
- Movement costs use higher of building (2 MF) or terrain
- TEM uses building value
- Special terrain effects may still apply (e.g., bog in marsh)

### 2. **Stairwell Representation**
- White circle/square symbol within building
- Position indicates access point
- Multiple stairwells possible in large buildings
- Factory stairwells indicate vehicular entrances

### 3. **Multi-Hex Building Rules**
- All segments share same `multiHexId`
- Connections must be reciprocal between hexes
- Building control requires controlling majority of hexes
- Rubbling affects only specific hexes unless specified

### 4. **Visual Hierarchy**
1. Base terrain visible at edges/gaps
2. Building footprint (semi-transparent or with gaps)
3. Building type indicators (color, pattern)
4. Feature symbols (stairwells, level numbers)
5. Connection indicators at hex edges

## Common Building Configurations

### Village Center
- Multiple connected stone buildings
- Central marketplace (potentially)
- Various building heights (1-2 levels)
- Roads between buildings

### Factory Complex
- Large multi-hex factory
- All hexes at same height
- Multiple vehicular entrances
- Potential railroad connections

### Rowhouse Block
- Linear arrangement
- Black bars between sections
- Consistent height along block
- Limited access points

### Fortified Position
- Stone buildings with fortification
- Strategic stairwell placement
- Commanding height (2+ levels)
- Limited approaches

## Rules References
- Building basics: B23
- Cellars: B23.41
- Rooftops: B23.8
- Fortified Buildings: B23.9
- Factory: B23.74
- Marketplace: B23.73
- Rowhouse: B23.71
- Multi-hex buildings: B23.721-722

This comprehensive reference ensures accurate implementation of ASL's complex building rules within our terrain system.