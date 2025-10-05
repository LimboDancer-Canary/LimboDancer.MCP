Here design plan based on the ASL rules pages B23–B30 and the way official boards depict things. 

---

## What a “building” means to draw (rules → visuals)

**A building hex is any hex that contains one or more brown/gray rectangular building depictions**—even if the hex center dot isn’t touched (B23.1). On printed boards, those rectangles:

* are **brown** for *wood* and **gray** for *stone* (TEM hint, B23.33),
* may **touch a hexside** to show connection to a building in the adjacent hex (same building, B23.1),
* sometimes show a **stairwell symbol** (small white square replacing the center dot) for multi-level buildings (B23.26).

Our renderer should therefore treat **buildings as overlays** made up of **footprints** (rectangles/polygons) that can sit centered or **flush to specific hexsides** so adjacent building hexes look visually connected.

---

## Core building types we should support (first pass)

1. **Single-Story House (B23.21)**

   * One hex, **no stairwell**.
   * Draw a **centered rectangle**; slightly smaller than the hex; **wood** (brown) or **stone** (gray).
   * Optional: plank/stone “courses” lines like Hex Lab for texture (subtle).

2. **Two-Story House (B23.22)**

   * Multi-hex building that **contains a stairwell symbol**. Considered a **1½-level** obstacle (rules detail, not visual).
   * Visually: same rectangles but with one hex in the building showing a **white stairwell square** near center (replaces center dot).
   * **Footprints should touch hexsides** where the building continues.

3. **Multi-Story Building (B23.23)**

   * Contains a stairwell; **2½-level** obstacle (again rules detail).
   * Visual is still **rectangles**, usually **stone** in official art. Show the **stairwell square** in at least one hex of the building.
   * If we want an explicit visual cue of “taller”, we can use **slightly larger footprint** + **darker shadow** (optional UI hint).

4. **Rowhouse (B23.71)**

   * Multiple adjacent building hexes are part of **one rowhouse**; official boards use a **thick black bar** across the *shared hexside(s)* between the hexes to indicate rowhouse partitioning.
   * Our renderer: when two adjacent hexes belong to the same **rowhouse group**, draw a **black bar** on that hexside (in both hexes or once in a z-ordered overlay).

5. **Marketplace (B23.73)** – later

   * Ground level **is not a building obstacle**; official depiction uses **white dashed lines** to hint “open under roof.”
   * Not needed for v1, but we should keep a slot in the model for `marketplace: true` and a **dashed treatment**.

6. **Factory (B23.74)** – later

   * Big multi-hex structure, has **printed stairwell** (or SSR). Special LOS/rooftop rules.
   * Visual: large **gray** rectangles spanning hexes, **stairwell**, and maybe **roof access points** markers (small symbols). Save for phase 2.

7. **Rubble (Sec. 24)** – future overlay

   * If a building hex becomes rubble, replace the building footprint with a **rubble pattern** (brown for wood/gray for stone), possibly with a small “Rubble” badge in debug.

---

## Visual primitives we should standardize

We’ll get a lot of mileage if we define a small kit of footprint shapes:

* **Center box**: width ~ 0.75–1.0 × hex width; height ~ 0.55–0.7 × hex width. (Hex Lab uses 30×20 for size 30; that’s a nice baseline.)
* **Span box (flush)**: rectangle that **touches two opposite hexsides** to show continuation (W–E, NE–SW, NW–SE). This is the most common multi-hex depiction.

  * Orientation options for a flat-top hex: `0°` (W–E), `+60°` (NE–SW), `+120°` (NW–SE).
* **Side box**: rectangle **flush to one hexside** (used when a building just reaches to an edge).
* **Connector seam**: when two neighbor hexes are in the same building group and both footprints are **flush to the shared side**, ensure there’s **no gap** (stroke handling or a tiny bridging polygon).
* **Stairwell symbol**: small **white square** at or near center (replaces center dot). Size ≈ `size * 0.33` of our label badge from Hex Lab looks right.
* **Material texture**: optional horizontal **plank lines** (wood) or **stone courses** (stone), subtle opacity; keep them as visual accents, not loud patterns.
* **Rowhouse bar**: a **thick black segment** exactly on the **shared hexside** where rowhouse partition exists.

---

## Data we’ll want in the JSON (incremental)

Let’s keep the per-hex payload small but expressive. Proposal:

```json
{
  "hexId": "1F5",
  "building": {
    "groupId": "B3",          // every contiguous hex of the same building shares this
    "material": "wood|stone",
    "levels": 1|2|3,          // 2 = two-story, 3 = multi-story; int is fine even if rules talk 1½/2½
    "stairwell": true|false,  // whether a printed stairwell square appears in this hex
    "footprint": {
      "type": "center|span|side",
      "orientation": "W_E|NE_SW|NW_SE", // only needed for span/side
      "depth": 0.55,         // thickness as fraction of hex width (defaults per type)
      "flushSides": ["W","E"]  // explicit sides the shape should touch (alternative to orientation)
    },
    "rowhouseEdges": ["E","SE"],   // draw a black bar on these hexsides
    "debugLabel": "2"              // optional debug numeral; production: rely on stairwell only
  }
}
```

Notes:

* If **two adjacent hexes** have the same `groupId` and each footprint **flushes** the **shared side**, they’ll look like one continuous building—no extra connector geometry needed beyond careful stroke order.
* **`levels`**: rules talk “1½ / 2½ obstacles”, but boards don’t print “½”. We can store `levels:2` for “two-story house”, `levels:3` for “multi-story.” If you want exact obstacle height for rules engines later, we can add `obstacle: 1.5 | 2.5` separately.
* **`stairwell`** is important: show the white square when true (B23.26). Many official buildings show the square only in one hex of the group—good enough to signal type.
* **Rowhouse**: you can either flag edges explicitly per hex (`rowhouseEdges`) or mark the building group `rowhouse:true` and let the generator infer edges between group members aligned across a side.

---

## Rendering rules (what we’ll actually draw)

1. Draw base terrain first (Open Ground, etc.).
2. For each hex with a `building`:

   * Choose **fill color** (wood/stone).
   * Compute the **footprint polygon** from `type + orientation + depth (+ flushSides)`.
   * Draw the **footprint** with a thin dark stroke.
   * Add **material lines**:

     * Wood: 3 horizontal plank lines (20% / 50% / 80% of height).
     * Stone: 2 courses (30% / 60%) + an optional **soft shadow** (like Hex Lab) to give mass for multi-level.
   * If `stairwell:true`: draw a **white square** (replacing the center dot).
   * If `debugLabel` present (optional dev mode): draw small badge “1/2/3.” Production: we omit numbers to match boards.
3. **Rowhouse edges**: pass 2 over all **adjacent building pairs**; where flagged, draw the **black bar** centered on the shared hexside.
4. Z-order: buildings above base, rowhouse bars above buildings, then roads (roads typically sit above ground but beneath units; board art varies—happy to tweak).

---

## Where we can start (Phase 1)

* Implement **center** and **span** footprints in three orientations.
* Support **stairwell square**.
* Support **rowhouse bar** on declared edges.
* Keep **wood/stone** with subtle textures (as you already have).
* Keep our **legend**: add a tiny **stairwell** key and a **rowhouse edge** key.

This gets us authentic-looking **single-hex** and **multi-hex** buildings that visually connect and carry the key ASL signals.

---

## What we’ll do next (Phase 2+)

* **Marketplace**: dashed boundary treatment and “open under roof” hint.
* **Factories**: larger spans, printed stairwell, optional rooftop access markers.
* **Footprint variety**: L-shapes/trapezoids (rare but seen); we can extend with a `poly` footprint type (local hex coords) if needed.
* **Rubble transitions** (Sec. 24): overlay rubble texture that replaces footprint while keeping group edges (and maybe leave rowhouse bar very dim beneath).
* **Orientation by angle**: if we need finer angles than the 3 axial orientations, allow degrees and rotate the rectangle; still snapping to touch sides is key for adjacency.

---

## Sanity checks against the pages

* **B23.1** connection across hexsides → we support **flush footprints + shared side** logic.
* **B23.21 / B23.22 / B23.23** 1-, 2-, multi-level + stairwell → covered via `levels` + `stairwell` + footprint scale/shadow.
* **B23.71 Rowhouse** black bar → explicit **rowhouseEdges**.
* **B23.73 Marketplace** dashed lines → scheduled.
* **B23.74 Factory** large multi-hex, stairwell, rooftop points → scheduled.
* **Sec. 24 Rubble** → planned overlay that swaps footprint.

---

If you’re good with this direction, I’ll propose a **minimal JSON schema update** (backward compatible) and then wire the **center/span footprints** + **stairwell** + **rowhouse bar** into the renderer behind a feature flag so we can test incrementally.
