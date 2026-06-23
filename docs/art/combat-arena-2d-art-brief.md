# DeadManZone — Combat Arena 2D Art Brief

**Project:** DeadManZone · **Mode:** TopTroops2D (`CombatArena2D.unity`)  
**Date:** 2026-06-23 · **Branch:** `2dcombatreworkv2`  
**Tracker:** [`combat-arena-2d-asset-tracker.csv`](combat-arena-2d-asset-tracker.csv)  
**Design spec:** [`docs/superpowers/specs/2026-06-23-combat-arena-2d-design.md`](../superpowers/specs/2026-06-23-combat-arena-2d-design.md)

---

## One-page summary

Convert combat **presentation** to a colorful Top Troops–style board: square dirt grid, fixed oblique camera (player **left**, enemy **right**), sprite units with Y-sort depth, arced projectiles, and punchy 2D VFX. **Gameplay/sim unchanged** — art drops into existing Unity fields.

| Deliverable | Count (P0) | Folder |
|-------------|------------|--------|
| Environment / grid | 6 PNGs | `Assets/_Project/Art/Combat2D/Environment/` |
| Role silhouettes (fallback) | 5 PNGs | `Assets/_Project/Art/Combat2D/Units/Silhouettes/` |
| VFX sheets | 3 sheets | `Assets/_Project/Art/Combat2D/VFX/` |
| Building sprites | 5–8 PNGs | `Assets/_Project/Art/Combat2D/Buildings/` |
| Unit combat sprites | 21 urgent + 25 upgrade | `Assets/_Project/Art/Combat2D/Units/` |

**46 pieces** mapped in CSV. **21 have no shop icon** → silhouette-only today → **P0 art**. **25 have icons** → usable at combat scale if ≥48px; dedicated `combatArenaSprite` still recommended.

---

## Art direction

| Topic | Target |
|-------|--------|
| **Reference feel** | Top Troops / mobile autobattler — readable at phone scale |
| **Style** | Cartoon military, bold shapes, optional 2–3px outline |
| **Camera** | Orthographic ~52° pitch; cells must read **square**, not squashed |
| **Board palette** | Warm dirt browns; zone tints (player / neutral / enemy) applied in code |
| **Faction color** | Prefer **neutral grayscale sprites** + multiply tint in Unity (`categoryTint` + side tint). Full-color per faction is P2. |
| **Orientation** | Units face **±X** (left/right); default pose faces **right** (enemy side) |

---

## Technical spec (Unity import)

| Setting | Value |
|---------|-------|
| Format | PNG, straight alpha |
| Pixels Per Unit | **64** (128px sprite ≈ 2 world units) |
| Unit pivot | **Bottom-center** `(0.5, 0.1–0.2)` — feet on cell floor |
| Building pivot | Bottom-center on footprint |
| Filter mode | Bilinear (Point if pixel-art style chosen) |
| Compression | ASTC / BC7 at build; source lossless PNG in repo |
| Atlases | Max 4: Grid, Units, Buildings, VFX (see CSV `atlas` column) |

### Sprite assignment (per piece)

1. **`combatArenaSprite`** on `PieceDefinitionSO` — **preferred** combat art  
2. **`icon`** — auto-used if ≥48×48 px (`CombatUnitSpriteResolver`)  
3. **Role silhouette** — procedural fallback until art lands  

**Assign field:** Inspector → Piece Definition → **Combat Arena Sprite**

---

## Asset categories

### A. Environment (P0 — do first)

| File | Size | Notes |
|------|------|-------|
| `combat2d_grid_cell_light.png` | 64×64 | Checker light dirt |
| `combat2d_grid_cell_dark.png` | 64×64 | Checker dark dirt |
| `combat2d_grid_backdrop.png` | 256×256 tile | Gap fill between cells |
| `combat2d_sky_gradient.png` | 1024×512 | Blue sky → warm horizon |
| `combat2d_shadow_unit.png` | 64×32 | Soft ellipse, ~35% alpha |
| `combat2d_shadow_building.png` | 96×48 | Wider structure shadow |

**Code hook:** `CombatArena2DBattlefieldView`

---

### B. Role silhouettes — shared fallback (P0)

Used when no `combatArenaSprite` and no combat-scale icon.

| File | Role mapping | Size |
|------|--------------|------|
| `combat2d_silhouette_assault.png` | assault, defender | 128×128 |
| `combat2d_silhouette_ranged.png` | sniper, support | 128×128 |
| `combat2d_silhouette_artillery.png` | artillery | 128×128 |
| `combat2d_silhouette_vehicle.png` | tank, vehicle primary | 160×128 |
| `combat2d_silhouette_generic.png` | utility, HQ, unknown | 128×128 |

**Code hook:** `CombatArena2DPlaceholderSprites` / `CombatUnitSpriteResolver.MapRole`

---

### C. VFX (P0 minimum, P1 polish)

| File | Frames | Size | Trigger |
|------|--------|------|---------|
| `combat2d_vfx_impact_rifle.png` | 4 | 64×64 each | Rifle hit |
| `combat2d_vfx_explosion_small.png` | 4 | 128×128 each | Grenade / cannon |
| `combat2d_vfx_death_puff.png` | 4 | 48×48 each | Unit destroyed |
| `combat2d_vfx_tracer_rifle.png` | 1 | 16×8 | Arc head — optional (lines OK for MVP) |
| `combat2d_vfx_tracer_cannon.png` | 1 | 24×16 | Vehicle/building shots |
| `combat2d_vfx_muzzle_rifle.png` | 4 | 32×32 each | P2 |
| `combat2d_vfx_muzzle_cannon.png` | 4 | 48×48 each | P2 |

**Attack profiles in code:** InfantryRifle, InfantryGrenade, InfantryMelee, VehicleCannon, BuildingArtillery

**Code hook:** `CombatArena2DVfx`

---

### D. Buildings (P0 core set)

Structures use the **same sprite resolver** as units (`CombatArena2DBuildingVisual`).

| Shared archetype | Pieces using it | Priority |
|------------------|-----------------|----------|
| `building_supply` | supply_depot, neutral_supply_depot | P0 |
| `building_field_gun` | field_gun_nest, neutral_field_gun | P0 |
| `building_hq_iron` | ironmarch_hq | P0 |
| `building_hq_dust` | dust_hq | P0 |
| `building_hq_echo` | echo_hq | P0 |
| `building_crimson_artillery` | crimson_artillery | P0 |
| `building_workshop` | field_workshop | P1 |
| `building_radio` | radio_array, signal_relay | P1 |
| `building_command` | command_bunker | P1 |

**Size:** 128×128 default; HQ / emplacements **160×160**; wide guns **144×128**.

---

### E. Unit combat sprites (46 pieces)

See CSV for per-piece rows. Summary by priority:

#### P0 — No shop icon (21 pieces) — must ship unique or archetype art

| Faction | Pieces |
|---------|--------|
| **iron_vanguard** | ironmarch_rifle |
| **crimson_legion** | crimson_elite, crimson_tank, crimson_artillery |
| **dust_scourge** | sand_raider, scrap_rig, toxin_launcher, dust_hq |
| **cartel_of_echoes** | phantom_agent, resonance_cannon, echo_hq, signal_relay |
| **ash_wraiths** | wraith_stalker, wraith_phantom, wraith_bombard |
| **neutral** | armored_sapper, command_bunker, gas_drone, mortar_crew, trench_raider, weak_conscript |

#### P1 — Has icon; upgrade to dedicated combat sprite

All remaining pieces with `has_shop_icon=yes` in CSV (25 pieces). Verify icon ≥48px in Unity; if smaller, treat as P0.

#### Shared archetypes (save production time)

| Archetype | Share across |
|-----------|--------------|
| `archetype_rifle` | rifle_squad, ironmarch_rifle, conscript_rifleman, shock_trooper, trench_raider |
| `archetype_marksman` | marksman_squad, ironmarch_sniper |
| `archetype_mortar` | mortar_crew, ironmarch_mortar, neutral_mortar_team |
| `archetype_tank` | ironmarch_heavy_tank, armored_transport, diesel_walker, mobile_cannon |

Faction variants = **palette swap** on shared archetype when schedule is tight.

---

## Squad / manpower visuals

Pieces with `manpowerCost > 1` duplicate the sprite up to **5** soldiers with small formation offsets (no extra art required if silhouette reads at 0.92× scale).

High-manpower examples: rifle_squad (10), mg_team (12), ironmarch_heavy_tank (14).

---

## Production order (recommended)

```
1. Grid atlas + shadows + sky          → battlefield reads immediately
2. Five role silhouettes               → all 46 pieces playable
3. Impact + explosion + death VFX      → combat juice
4. Three faction HQs + supply + field gun
5. P0 no-icon faction showcase units
6. Dedicated combat sprites for shop icons (P1)
7. Tracer / muzzle polish (P2)
```

---

## Handoff checklist (artist → dev)

- [ ] PNGs named per CSV `recommended_filename` column  
- [ ] Placed under `Assets/_Project/Art/Combat2D/` subfolders  
- [ ] Unity: Texture Type = **Sprite (2D and UI)**, PPU = **64**  
- [ ] Slice sprite sheets; set pivots bottom-center  
- [ ] Pack atlases per CSV `atlas` column  
- [ ] Assign `combatArenaSprite` on each `PieceDefinitionSO` (path in CSV)  
- [ ] Play-test in **CombatArena2D** scene with `visualMode: TopTroops2D`  
- [ ] Update CSV `status` column: `missing` → `in_progress` → `done`  

---

## Counts at a glance

| Tier | Unique PNGs (approx) | Notes |
|------|----------------------|-------|
| **P0** | ~25 | Environment + silhouettes + core VFX + 21 no-icon pieces (some shared archetypes) |
| **P1** | +25 | Icon-holding pieces upgraded to combat sprites |
| **P2** | +6 | Muzzle flashes, miss dust, faction full-color variants |

---

## References in codebase

| System | Path |
|--------|------|
| Sprite resolution | `Assets/_Project/Presentation/Combat/Arena/CombatUnitSpriteResolver.cs` |
| Unit visuals | `Assets/_Project/Presentation/Combat/Arena/CombatUnitVisual2D.cs` |
| Buildings | `Assets/_Project/Presentation/Combat/Arena/CombatArena2DBuildingVisual.cs` |
| Battlefield | `Assets/_Project/Presentation/Combat/Arena/CombatArena2DBattlefieldView.cs` |
| VFX | `Assets/_Project/Presentation/Combat/Arena/CombatArena2DVfx.cs` |
| Config | `Assets/_Project/Data/Resources/DeadManZone/CombatArenaConfig.asset` |
| Piece data | `Assets/_Project/Data/Resources/DeadManZone/Pieces/*.asset` |

---

*Print this page + keep the CSV open in Excel/Sheets for status tracking.*
