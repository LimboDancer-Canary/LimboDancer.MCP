# ASL Board Hex Map Management

## Overview

ASL (Advanced Squad Leader) relies heavily on standardized hex maps for gameplay. Rules reference specific hex locations (e.g., "LOS from 3K3 to 3P5"), making map data critical for rule interpretation and validation. This document outlines our approach to managing hex map data within the LimboDancer ontology system.

## Key Requirements

### 1. Preserve Board and Hex References
- Rules reference specific boards and hexes: "Board 3, hex K3"
- Examples use exact coordinates: "36AA8 to 36GG2"
- System must understand board numbers and hex coordinate systems

### 2. Pre-computed Line of Sight (LOS)
- LOS calculations are complex and rules-heavy
- Pre-computing LOS between hexes enables instant queries
- Must store blocking information and terrain causes

### 3. Integration Without Special Structures
- Use generic JSON documents, not custom classes
- Enrich GameState with hex properties dynamically
- Maintain clean separation from core ontology

## Board Metadata Structure

From the VASL board metadata, we know:
- Standard geomorphic boards are 33x10 hexes
- Buildings have multiple levels (1-2 for Board 1)
- Terrain types include: Open Ground, Woods, Buildings (Stone/Wooden)
- Each hex has a coordinate (A1 through GG10)

## JSON Document Structure

### Board-Level Structure
```json
{
  "metadata": {
    "type": "board",
    "id": "1",
    "version": "6.9",
    "author": "TR",
    "dimensions": {
      "width": 33,
      "height": 10
    },
    "hasHills": false
  },
  "hexes": {
    "K3": { /* hex data */ },
    "P5": { /* hex data */ }
    // ... all 330 hexes
  }
}
```

### Hex-Level Structure
```json
{
  "K3": {
    "coordinate": "K3",
    "terrain": "Woods",
    "level": 1,
    "features": ["Woods"],
    "los": {
      "A1": { "clear": true },
      "A2": { "clear": true },
      "P5": {
        "clear": false,
        "blockedBy": ["L4", "M4", "N4"],
        "blockingTerrain": ["Woods", "Building"]
      },
      "AA7": {
        "clear": false,
        "blockedBy": ["Y5", "Z6"],
        "blockingTerrain": ["Stone Building, 2 Level"]
      }
      // ... LOS to all relevant hexes
    }
  }
}
```

### Building Classifications (from Board 1)
```json
{
  "buildingTypes": {
    "stoneBuilding2Level": [
      "E4", "F3", "G3", "G4", "F5", "F6", "G6", "H5",
      "J4", "J5", "K4", "K5", "L6", "L7", "M7", "M5",
      // ... 42 total hexes
    ],
    "stoneBuilding1Level": [
      "P7", "P8", "P5", "Q6", "R1", "S1", "S5", "T4",
      // ... 12 total hexes
    ],
    "woodenBuilding1Level": [
      "F1", "G1", "K7", "K8", "Z1", "AA2", "AA9", "BB8"
    ]
  }
}
```

## Integration with Ontology System

### 1. Loading Reference Data
```csharp
public class OntologyQueryEngine 
{
    private readonly Dictionary<string, JsonDocument> _referenceData;
    
    public void LoadBoardData(string boardNumber, string jsonPath) 
    {
        var jsonContent = File.ReadAllText(jsonPath);
        _referenceData[$"Board{boardNumber}"] = JsonDocument.Parse(jsonContent);
    }
}
```

### 2. Enriching GameState
```csharp
private void EnrichStateWithHexData(GameState state, string location) 
{
    // Parse "3K3" → board="3", hex="K3"
    var (boardNum, hexCoord) = ParseLocation(location);
    
    if (_referenceData.TryGetValue($"Board{boardNum}", out var board)) 
    {
        var hex = board.RootElement
            .GetProperty("hexes")
            .GetProperty(hexCoord);
        
        // Add generic properties to state
        state.StateVariables[$"terrain_{location}"] = 
            hex.GetProperty("terrain").GetString();
        state.StateVariables[$"level_{location}"] = 
            hex.GetProperty("level").GetInt32();
    }
}
```

### 3. LOS Queries
```csharp
public bool CheckLOS(string fromLocation, string toLocation) 
{
    var (fromBoard, fromHex) = ParseLocation(fromLocation);
    var (toBoard, toHex) = ParseLocation(toLocation);
    
    // Same board check (cross-board LOS is rare)
    if (fromBoard != toBoard) return false;
    
    var board = _referenceData[$"Board{fromBoard}"];
    var hexData = board.RootElement
        .GetProperty("hexes")
        .GetProperty(fromHex);
    
    if (hexData.TryGetProperty("los", out var los) &&
        los.TryGetProperty(toHex, out var losData)) 
    {
        return losData.GetProperty("clear").GetBoolean();
    }
    
    return false; // No pre-computed LOS data
}
```

### 4. Rule Example Validation
```csharp
public ValidationResult ValidateRuleExample(ExampleNode example) 
{
    // Extract locations from example text
    var locations = ExtractLocations(example.ExampleText);
    
    foreach (var loc in locations) 
    {
        if (!HexExists(loc)) 
        {
            return new ValidationResult 
            {
                IsValid = false,
                Message = $"Example references non-existent hex: {loc}"
            };
        }
    }
    
    // Validate LOS claims in examples
    if (example.ExampleText.Contains("LOS from")) 
    {
        var losResult = ValidateLOSClaim(example);
        if (!losResult.IsValid) return losResult;
    }
    
    return ValidationResult.Success;
}
```

## Benefits of This Approach

### 1. **No Special Structures**
- Hex data is just JSON documents
- No modifications to core ontology classes
- Easy to extend with new properties

### 2. **Performance**
- Pre-computed LOS eliminates complex calculations
- O(1) hex lookups
- Cached JSON documents in memory

### 3. **Maintainability**
- Board data separate from rules
- Easy to add new boards
- Can update LOS calculations independently

### 4. **Query Integration**
```csharp
// User asks: "Can infantry in woods at K3 see building at P5?"

// 1. Parse locations
var locations = ["K3", "P5"];

// 2. Enrich state with hex data
foreach (var loc in locations) {
    EnrichStateWithHexData(state, loc);
}
// State now has: terrain_K3="Woods", terrain_P5="Stone Building"

// 3. Check LOS
var hasLOS = CheckLOS("K3", "P5"); // false

// 4. Find applicable rules
state.StateVariables["has_los"] = hasLOS;
var rules = _graph.GetActiveRules(state);
// Rules check generic conditions like "has_los == false"
```

## Dynamic Terrain and Unit Management

### The Challenge
ASL terrain is **dynamic**, not static:
- **Buildings** → Rubble (permanent)
- **Woods/Forest** → Blazes → Open Ground (progressive)
- **Bridges** → Destroyed (permanent)
- **Any hex** → Smoke (temporary, dissipates)
- **Vehicles** → Wrecks (permanent obstacles)
- **Units** → Constantly moving between hexes

### Solution: Unified Dynamic State Model

We use Option B: Pre-computed base terrain with dynamic overlay.

```csharp
public class HexState 
{
    public string Coordinate { get; set; }
    public string BaseTerrain { get; set; }      // Original from JSON
    public string CurrentTerrain { get; set; }   // After modifications
    public List<Unit> Units { get; set; }        // Dynamic occupants
    public List<Counter> Counters { get; set; }  // Smoke, rubble, wrecks
}

public class Unit 
{
    public string Id { get; set; }
    public string Type { get; set; }  // Infantry, Vehicle, Gun
    public bool BlocksLOS { get; set; }  // Vehicles, wrecks
    public Dictionary<string, object> Attributes { get; set; }
}

public class Counter 
{
    public string Type { get; set; }  // Smoke, Rubble, Blaze, Wreck
    public int? DurationRemaining { get; set; }  // For smoke
    public Dictionary<string, object> Effects { get; set; }
}
```

### Dynamic LOS Calculation

```csharp
public class GameBoard 
{
    private JsonDocument BaseTerrainData { get; set; }  // Pre-computed
    public Dictionary<string, HexState> Hexes { get; set; }
    
    public bool CheckLOS(string from, string to) 
    {
        // 1. Start with pre-computed base LOS
        var baseLOS = GetBaseLOS(from, to);
        
        // 2. Apply terrain modifications
        foreach (var hex in GetLOSPath(from, to)) 
        {
            if (Hexes[hex].CurrentTerrain != Hexes[hex].BaseTerrain) 
            {
                baseLOS = ApplyModificationPattern(baseLOS, hex);
            }
            
            // 3. Check dynamic blockers (units, smoke)
            if (HasLOSBlocker(Hexes[hex])) return false;
        }
        
        return baseLOS;
    }
}
```

### LOS Modification Rules

```json
{
  "losModificationRules": {
    "building_to_rubble": {
      "heightChange": -1,
      "losEffect": "degrades",
      "pattern": "adjacentHexesBecomeVisible"
    },
    "woods_to_blaze": {
      "heightChange": 0,
      "losEffect": "hindrance",
      "hindranceAdded": 2
    },
    "smoke": {
      "hindranceAdded": 3,
      "blockingThreshold": 6,
      "duration": "variable"
    },
    "wreck": {
      "heightEquivalent": 0.5,
      "losEffect": "blocks_if_aligned"
    }
  }
}
```

### Enhanced Query Engine Integration

```csharp
public class OntologyQueryEngine 
{
    private readonly GameBoard _gameBoard;  // Dynamic state
    
    private void EnrichStateWithHexData(GameState state, string location) 
    {
        var hexState = _gameBoard.Hexes[location];
        
        // Include both static and dynamic data
        state.StateVariables[$"terrain_{location}"] = hexState.CurrentTerrain;
        state.StateVariables[$"base_terrain_{location}"] = hexState.BaseTerrain;
        state.StateVariables[$"units_{location}"] = hexState.Units.Count;
        state.StateVariables[$"has_smoke_{location}"] = 
            hexState.Counters.Any(c => c.Type == "Smoke");
        state.StateVariables[$"is_rubbled_{location}"] = 
            hexState.CurrentTerrain == "Rubble";
    }
    
    public bool CheckLOS(string from, string to) 
    {
        return _gameBoard.CheckLOS(from, to);
    }
}
```

### Benefits of Unified Approach

1. **Single State Model** - Units and terrain modifications in one place
2. **Consistent Updates** - Same mechanism for unit movement and terrain changes
3. **Rule Integration** - "Wreck creation" naturally flows from unit destruction
4. **Performance** - Base LOS remains O(1), modifications applied only when needed
5. **Query Simplicity** - GameState has everything needed for rule evaluation

## Future Enhancements

### 1. **Multi-Board Support**
- Handle mega-scenarios with multiple boards
- Cross-board LOS calculations
- Board overlap configurations

### 2. **Elevation Support**
- Hill contours and crest lines
- Multi-level buildings
- Depression hexes

### 3. **Overlay Management**
- Scenario-specific overlays (from metadata)
- Terrain transformations
- Seasonal changes

## Data Generation Strategy

### 1. **Initial Data Creation**
- Parse VASL board images and metadata
- Extract terrain types per hex
- Generate LOS calculations using ASL rules

### 2. **Validation**
- Cross-reference with known examples
- Community validation
- Automated testing against rule examples

### 3. **Distribution**
- JSON files packaged with system
- Version control for updates
- Separate download for board data

## Key Design Insights

### Canonical Rule ID Preservation
The entire ASL community references rules by their exact IDs (A.1, B.23.71, 10.211). Our system NEVER modifies these IDs - they serve as primary keys throughout the ontology graph. This enables players to query using familiar references and validates rule examples against their exact citations.

### Separation of Concerns
- **Ontology Graph**: Stores rules, exceptions, definitions, cross-references
- **JSON Documents**: Contains spatial data (hex terrain, pre-computed LOS)
- **Query Engine**: Bridges both stores, enriching GameState dynamically

### Complete Query Flow
When asking "Can infantry in woods at K3 see building at P5?":

1. **Parse Natural Language** (Claude)
   - Extract locations, unit types, actions
   
2. **Enrich GameState** (Reference Data)
   ```csharp
   state.StateVariables["terrain_K3"] = "Woods"
   state.StateVariables["terrain_P5"] = "Stone Building"
   state.StateVariables["has_los"] = false  // From pre-computed data
   ```

3. **Query Rule Graph** (Ontology)
   - Find LOS rules (A6.1)
   - Traverse exception nodes
   - Check phase-specific modifications
   
4. **Synthesize Response** (Claude)
   - Combine rule requirements with spatial reality
   - Suggest tactical alternatives
   - Reference exact rule citations

### Performance Benefits
Pre-computed LOS eliminates complex runtime calculations involving:
- Terrain height differentials
- Intervening obstacles
- Blind hex calculations
- Multi-level building considerations

This transforms O(n²) LOS algorithms into O(1) lookups.

## Conclusion

This approach provides a clean, efficient way to integrate ASL's spatial requirements into our generic ontology system. By using JSON documents and pre-computed LOS data, we enable sophisticated spatial queries while maintaining the system's generic architecture. The hex data enriches rule queries without requiring special structures, keeping the design elegant and extensible.