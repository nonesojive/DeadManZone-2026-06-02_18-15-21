> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — Synty Full Asset Revamp Design

**Date:** 2026-06-13  
**Status:** Approved (2026-06-13)  
**Supersedes:** `2026-06-13-sandbox-art-asset-pass-design.md` (temporary Toon/RTS/BunkerSurvival placeholders)  
**Builds on:** `2026-06-10-deadmanzone-combat-arena-presentation-design.md`, `2026-06-06-deadmanzone-visual-studio-design.md`, `2026-05-31-deadmanzone-autobattler-design.md`  
**Goal:** Migrate all visual layers to a cohesive Synty POLYGON aesthetic — combat arena, UI, board icons, scene atmosphere, and VFX — then retire legacy third-party art dependencies.

---

## 1. Summary

The user subscribed to **Syntypass** and imported 32 Synty packages. The project still renders combat with **Toon_Soldiers**, **RTS modern vehicles**, **Unity cube buildings**, **BunkerSurvivalUI**, and mixed custom PNG icons. This design replaces those layers with Synty assets while preserving existing architecture (`VisualProfileSO`, `UiThemeSO`, `SandboxArtCatalog`, combat arena presenter).

| Layer | Current | Target |
|-------|---------|--------|
| Combat infantry | Toon WW2 German (custom animator ints) | SidekickCharacters + AnimationBaseLocomotion |
| Combat vehicles | RTS_Modern_Combat_Vehicle_Pack | PolygonWar tanks/trucks + PolygonMech walker |
| Arena buildings | Colored cubes | PolygonWar bunkers, gun nests, supply crates |
| Arena terrain | Runtime brown Plane | PolygonMapsWoodlandApocalypse ground tile + trench props |
| Combat VFX | Generic/null particles | PolygonParticleFX gunshot, dust, explosion, blood |
| UI chrome | BunkerSurvivalUI kit | InterfaceMilitaryCombatHUD + InterfaceModernMenus |
| Board/shop icons | Mixed Grok + BunkerSurvival + snapshots | Re-snapshot all 25 sandbox pieces from Synty prefabs |
| Main menu backdrop | Static PNG / default sky | PolygonMapsMilitaryWarehouse interior preset |
| Atmosphere | Default Unity ambient | PolygonApocalypse skybox + trench fog palette |

**Art direction:** Grimdark retro-futurist WW1 trench — brass, mud, gas-lamp amber accents. Synty low-poly readability at top-down / isometric camera angles. Iron Vanguard faction uses **German-adjacent** PolygonWar props without literal historical branding (matches existing IronMarch naming).

---

## 2. Success criteria

| # | Criterion |
|---|-----------|
| 1 | All **25 sandbox roster** pieces have non-null `icon` sourced from Synty prefab snapshots or PolygonIcons |
| 2 | All units/hybrids have `combatArenaPrefab` pointing under `Assets/_Project/Art/Synty/` (not Toon/RTS paths) |
| 3 | All buildings use Synty mesh prefabs (not Unity primitives) for arena presentation |
| 4 | `SandboxArtCoverageTests` and `CombatArenaPlayModeTests` pass |
| 5 | Active `VisualProfile` uses new `SyntyTrenchUiTheme` (no BunkerSurvivalUI sprite references in runtime theme) |
| 6 | Combat arena ground uses Synty terrain material; at least 4 perimeter trench/debris props placed via `CombatArenaBootstrap` |
| 7 | `CombatArenaUnitVisual` drives Sidekick/AnimationBaseLocomotion (no Toon WW2 animator int hashes) |
| 8 | Legacy folders listed in §8 are removable without breaking `_Project` references |
| 9 | Editor menu **Apply Synty Art Pass** reproduces full assignment from catalog |

---

## 3. Synty asset mapping (sandbox roster)

All new project-owned prefabs live under:

```
Assets/_Project/Art/Synty/
  Arena/
    Units/          ← Sidekick prefab variants (infantry roles)
    Vehicles/       ← PolygonWar / PolygonMech wrappers
    Buildings/      ← PolygonWar building prefab wrappers
  Icons/            ← Snapshot PNG output
  UI/               ← Theme asset references only (sprites stay in Assets/Synty/)
```

### 3.1 Infantry (SidekickCharacters)

PolygonWar ships modular attachments only — no rigged soldier prefabs. Use **SidekickCharacters** with **AnimationBaseLocomotion** sample controller (`AC_Polygon_Masculine` or project copy).

| Piece role | Sidekick base prefab | Notes |
|------------|---------------------|-------|
| Rifle / conscript / defender | `HumanSpecies_01` or `ScifiSoldier_03` | Retro-futurist coat reads as trench infantry |
| MG / mortar / grenade support | `ScifiSoldier_05` | Heavier silhouette, backpack attachment |
| Medic / engineer | `ScifiSoldier_02` | Lighter gear |
| Sniper / marksman | `ScifiSoldier_06` | Scoped weapon prop from PolygonMilitary attachments |
| Officer / breacher / shock trooper | `ScifiSoldier_04` | Officer cap from PolygonWar attachments optional |

Create **5 role prefabs** under `Arena/Units/` (not 15 unique meshes). Multiple piece IDs share the same prefab with scale/tint variance via existing `combatArenaModelScale` on `PieceDefinitionSO`.

**Animator contract:** Replace Toon-specific ints (`status_k98`, etc.) with AnimationBaseLocomotion parameters:
- `MoveSpeed` (float) or `IsWalking` (bool)
- `Attack` (trigger) for attack lunge
- Idle state default on spawn

`CombatArenaUnitVisual` gets a small strategy interface or enum (`LegacyToon` removed after migration).

### 3.2 Vehicles

| Piece | Synty source prefab | Scale |
|-------|---------------------|-------|
| armored_transport | `PolygonWar/.../SM_Veh_German_Truck_01` | 0.85 |
| mobile_cannon | `PolygonWar/.../SM_Veh_German_Car_01` + towed `SM_Wep_Artillery_01` child | 0.9 |
| diesel_walker | `PolygonMech/.../SM_Mech_01` (or smallest walker variant) | 0.75 |
| mobile_artillery | `PolygonWar/.../SM_Veh_German_Halftrack_01` | 0.85 |
| ironmarch_heavy_tank | `PolygonWar/.../SM_Veh_German_Tank_01` | 0.9 |

Vehicles are **static mesh** in arena (no vehicle animator v1). Attack animation = unit lunge or muzzle flash VFX only.

### 3.3 Buildings

Replace cube prefabs with wrapped Synty meshes:

| Piece | Synty source | Wrapper prefab |
|-------|--------------|----------------|
| ironmarch_hq | `SM_Bld_Bunker_Large_01` | `ArenaBuilding_Hq.prefab` |
| field_gun_nest / neutral_field_gun | `SM_Bld_Bunker_Gun_01` | `ArenaBuilding_FieldGun.prefab` |
| supply_depot / field_workshop / neutral_supply_depot | `SM_Bld_Barracks_01` or crate stack from PolygonWar Props | `ArenaBuilding_SupplyDepot.prefab` |
| radio_array | `SM_Prop_Radio_01` (PolygonMilitary or PolygonWar props) | New `ArenaBuilding_Radio.prefab` |

Building prefabs: empty root + child mesh prefab instance, normalized pivot at ground, consistent forward = +Z.

### 3.4 Icons

Run **Snapshot Missing Icons From Prefabs** for all catalog entries with `snapshotIconFromPrefab = true`. Building icons snapshot the new arena building prefabs. Neutral Grok icons (`conscript_rifleman`, etc.) **re-snapshot** from Sidekick prefabs for style consistency.

Output folder: `Assets/_Project/Art/Synty/Icons/`

PolygonIcons pack supplies fallback UI glyphs (ammo, wrench, radio) where snapshot quality is poor.

---

## 4. UI theme migration

### 4.1 New theme asset

Create `SyntyTrenchUiTheme.asset` at `Assets/_Project/Data/Visual/Presets/`:

| UiThemeSO field | Synty source |
|-----------------|--------------|
| `panelSprite`, `cardSprite`, `modalFrameSprite` | InterfaceMilitaryCombatHUD `Sprites/Frames/` |
| `buttonNormal/Highlighted/Pressed` | InterfaceMilitaryCombatHUD `Sprites/Buttons/` |
| `bannerSprite` | InterfaceMilitaryCombatHUD `Sprites/Banners/` |
| `accentButtonSprite`, `dangerButtonSprite` | Military HUD accent variants |
| `menuBackgroundSprite` | InterfaceModernMenus main menu plate OR Military warehouse render |
| `runBackgroundSprite` | PolygonApocalypse sky gradient capture OR InterfaceMilitaryCombatHUD branding bg |
| `combatBackgroundSprite` | Dark scrim (procedural color; no sprite required) |
| Color palette | Muted olive/brass from Military HUD sample scenes |

Keep **semantic colors** from `BunkerSurvivalUiTheme` (zone tints, lane tints, sell zone) — only swap sprite sources and slightly desaturate to match Synty albedo.

### 4.2 Editor tooling

Add `SyntyUiKitSetup.cs` mirroring `BunkerSurvivalUiKitSetup`:

| Menu | Action |
|------|--------|
| `DeadManZone/UI Kit/Import Synty Trench Theme` | Build/update `SyntyTrenchUiTheme` |
| `DeadManZone/UI Kit/Apply Synty Theme To Active Profile` | Wire runtime VisualProfile |
| `DeadManZone/UI Kit/Restyle All Scenes With Synty Kit` | Calls existing `UiThemeSceneRefresher` |

Update `UiThemeSceneStyling` fallback chain: Synty theme → BunkerSurvival (deprecated) → hardcoded defaults.

### 4.3 Fonts

Use InterfaceMilitaryCombatHUD bundled **Exo 2.0 SDF** for TMP labels where scenes currently use Liberation Sans. Apply via `UiThemeSO` extension field or scene bootstrap — do not fork every Text component manually.

---

## 5. Scene & atmosphere

### 5.1 VisualProfile update

Update `DeadManZoneDefaultVisualProfile` / runtime `VisualProfile.asset`:

- `uiTheme` → `SyntyTrenchUiTheme`
- `runAtmosphere` → new `SyntyTrenchRunAtmosphere` (exponential fog, `{r:0.45, g:0.42, b:0.38}`, density 0.025)
- `mainMenuAtmosphere` → warehouse interior trilight (dim, cool)
- `postProcessProfile` → optional: copy URP volume from `PolygonApocalypseWasteland` demo scene

### 5.2 MainMenu.unity

- Add non-interactive 3D backdrop root: instantiate `PolygonMapsMilitaryWarehouse` demo prefab subset (bunker corridor, dim point lights)
- Camera: static pose, no player control
- UI canvas unchanged structurally; theme refresh applies Synty sprites

### 5.3 Run.unity (build phase)

- Keep flat UI board (no gameplay change)
- Optional: subtle parallax 3D trench backdrop behind shop panel (disabled if perf > 2ms GPU on target hardware)
- `ShopBackgroundBootstrap` — re-enable controlled backdrop using rendered warehouse PNG or low-poly prefab instance with depth-of-field scrim

### 5.4 CombatArena.unity

Extend `CombatArenaBootstrap`:

1. **Ground:** Replace primitive Plane with `PolygonMapsWoodlandApocalypse` ground mesh tile (scaled to board via existing `FitGroundToLayout`)
2. **Props:** Spawn ring of `PolygonWar` trench wall / sandbag prefabs around board perimeter (pooled, static)
3. **Sky:** Assign `PolygonApocalypse` skybox material to RenderSettings
4. **VFX defaults:** Wire `CombatArenaVfx` impact → `FX_Gunshot_01`, death → `FX_Dust_Small_01` + `FX_BloodSplat_Small_01`

---

## 6. Code changes

| File | Change |
|------|--------|
| `SandboxArtPaths.cs` | Replace Toon/RTS/BunkerSurvival paths with `Assets/_Project/Art/Synty/...` constants |
| `SandboxArtDefaultCatalogFactory.cs` | Regenerate catalog entries for Synty paths; all icons snapshot=true where prefab exists |
| `CombatArenaUnitVisual.cs` | Add `SyntyLocomotionVisualDriver` — AnimationBaseLocomotion params; remove WW2 hashes |
| `CombatArenaBootstrap.cs` | Synty ground + prop spawn; material assignment |
| `CombatArenaVfx.cs` | Serialize default PolygonParticleFX prefab refs |
| `SyntyUiKitSetup.cs` | New editor — theme creation |
| `VisualProfilePresetFactory.cs` | Prefer Synty theme when folder exists |
| `SandboxArtCoverageTests.cs` | Assert no path contains `Toon_Soldiers` or `RTS_Modern` |

**No Core/sim changes.** Save schema unchanged.

### 6.1 Animator driver (new file)

`Assets/_Project/Presentation/Combat/Arena/SyntyLocomotionVisualDriver.cs` (~80 lines):

- Implements `ICombatUnitVisualDriver`
- Sets `MoveSpeed` on walk start/stop
- Fires `Attack` trigger; coroutine waits animation event or fixed 0.5s
- Static mesh vehicles: no-op driver

---

## 7. Shader & render pipeline

Project uses **URP** (evidenced by Synty URP demo assets and `Universal Render Pipeline/Lit` in bootstrap).

**One-time editor step:** Run Synty `PNB_Core/Scripts/Editor/ReplaceShaders` or Synty Package Helper shader graph setup if pink materials appear after import.

Verify:
- All `_Project/Art/Synty` prefabs use Synty POLYGON shader variants
- No legacy Standard shader on arena units (except intentional placeholders during migration)

---

## 8. Legacy asset cleanup

After pass validation, these folders have **zero `_Project` references** and may be deleted from the repo (or moved to `Assets/_Archive/`):

| Folder | Reason |
|--------|--------|
| `Toon_Soldiers` | Replaced by Sidekick |
| `RTS_Modern_Combat_Vehicle_Pack_Free` | Replaced by PolygonWar |
| `WW2_German_soilders` | Unused duplicate |
| `LowPolySoldiers_demo` | Unused |
| `Toon_RTS_demo` | Unused |
| `SimpleMilitary`, `SimpleApocalypse`, `SimpleApocalypseInteriors`, `SimpleFX`, `SimpleBuildings`, `SimpleProps`, `SimpleItems`, `SimpleIcons` | Superseded by Synty |
| `PostApocalypseGunsDemo` | Superseded |
| `BunkerSurvivalUI` | Retained until Synty theme verified; delete in cleanup phase 2 |

**Keep:** `TextMesh Pro`, `Plugins`, `_Project`, `Synty`, project-specific art under `_Project/Art/`.

Document cleanup in PR description; do not delete in same commit as migration (two-phase for easy rollback).

---

## 9. Recommended Synty downloads (optional)

Already sufficient for this pass. Optional additions if available on Syntypass:

| Pack | Benefit |
|------|---------|
| None required | Current 32-pack import covers all mapping |
| Future: dedicated **POLYGON WW1 Characters** if Synty releases rigged presets | Would simplify infantry vs Sidekick assembly |

**Do not import** additional city/sci-fi/heist packs — increases repo size without visual gain for trench theme.

---

## 10. Implementation phases

| Phase | Work | Est. |
|-------|------|------|
| **P0** | Shader verify, folder scaffold, `SandboxArtPaths` + catalog rewrite | 1 session |
| **P1** | Build arena prefab wrappers (units, vehicles, buildings) | 1 session |
| **P2** | `SyntyLocomotionVisualDriver` + update `CombatArenaUnitVisual` | 1 session |
| **P3** | Icon snapshot pass + Apply catalog + fix coverage tests | 1 session |
| **P4** | `SyntyTrenchUiTheme` + scene restyle (MainMenu, Run) | 1 session |
| **P5** | Combat arena terrain, props, VFX, atmosphere SOs | 1 session |
| **P6** | PlayMode QA, legacy folder cleanup PR | 1 session |

Each phase ends with green EditMode tests; P6 adds PlayMode combat smoke test.

---

## 11. Testing

| Test | Validates |
|------|-----------|
| `SandboxArtCoverageTests` | Icons + prefabs for 25 roster |
| `SandboxArtCoverageTests.NoLegacyPrefabPaths` | New — no Toon/RTS paths |
| `CombatArenaPlayModeTests` | Units spawn, walk, attack without animator errors |
| Manual | MainMenu backdrop visible; Run shop readable with new theme; fight 1 arena shows Synty ground + bunkers |

---

## 12. Risks & mitigations

| Risk | Mitigation |
|------|------------|
| Sidekick soldiers look sci-fi, not WW1 | Attach PolygonWar helmets/packs; desaturate materials; accept stylized Iron Vanguard retro-futurism |
| AnimationBaseLocomotion mismatch | Fall back to static pose + lunge tween for v1 if animator setup blocks |
| Repo size (+Synty already large) | Delete legacy folders in phase 6; no new Synty downloads |
| Pink shaders after upgrade | Run Synty ReplaceShaders menu once |
| Icon snapshot inconsistency | Single snapshot camera rig in `SandboxIconSnapshotter` with fixed yaw/pitch |

---

## 13. Out of scope

- Enemy faction art (Dust/Cartel/Crimson/Ash) — remain null prefabs until faction expansion
- Board **cellSprites** terrain tiles — future pass using PolygonWar floor textures
- Addressables / LOD groups for Synty meshes
- Sidekick Character Creator custom builds per piece (use 5 role prefabs only)
- Async PvP or new gameplay systems

---

## 14. Approval checklist

- [x] User confirms Sidekick sci-fi soldiers read acceptably as Iron Vanguard trench troops
- [x] User confirms UI migration away from BunkerSurvivalUI
- [x] User approves two-phase legacy deletion (migrate first, delete second)
