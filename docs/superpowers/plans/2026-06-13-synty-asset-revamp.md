# Synty Full Asset Revamp Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace all legacy visual assets (Toon soldiers, RTS vehicles, cube buildings, BunkerSurvivalUI) with cohesive Synty POLYGON art across combat arena, UI, icons, and scene atmosphere.

**Architecture:** Data-driven migration via extended `SandboxArtCatalog` + editor generators that wrap Synty prefabs under `Assets/_Project/Art/Synty/`. Runtime changes limited to `CombatArenaUnitVisual` (new locomotion driver), `CombatArenaBootstrap` (terrain/props), and `VisualProfile` theme swap. Core sim untouched.

**Tech Stack:** Unity 6, URP, Synty POLYGON packs (PolygonWar, SidekickCharacters, AnimationBaseLocomotion, InterfaceMilitaryCombatHUD, PolygonParticleFX, PolygonMapsWoodlandApocalypse), existing `VisualProfileSO` / `UiThemeSO` pipeline.

**Spec:** `docs/superpowers/specs/2026-06-13-synty-asset-revamp-design.md`

---

## File map

| File | Responsibility |
|------|----------------|
| `Assets/_Project/Art/Synty/` | Project-owned prefab wrappers + icon PNG output |
| `Assets/_Project/Data/Editor/Synty/SyntyArtPaths.cs` | Constants for all Synty source + wrapper paths |
| `Assets/_Project/Data/Editor/Synty/SyntyArenaPrefabGenerator.cs` | Editor: build unit/vehicle/building wrapper prefabs |
| `Assets/_Project/Data/Editor/Synty/SyntyArtCatalogFactory.cs` | Editor: regenerate SandboxArtCatalog with Synty paths |
| `Assets/_Project/Data/Editor/Synty/SyntyUiKitSetup.cs` | Editor: build SyntyTrenchUiTheme from HUD sprites |
| `Assets/_Project/Data/Editor/SandboxArt/SandboxArtPaths.cs` | Update to delegate to SyntyArtPaths |
| `Assets/_Project/Presentation/Combat/Arena/ICombatUnitVisualDriver.cs` | Interface for walk/attack/idle |
| `Assets/_Project/Presentation/Combat/Arena/SyntyLocomotionVisualDriver.cs` | AnimationBaseLocomotion driver |
| `Assets/_Project/Presentation/Combat/Arena/StaticMeshVisualDriver.cs` | No-op driver for vehicles |
| `Assets/_Project/Presentation/Combat/Arena/CombatArenaUnitVisual.cs` | Use driver instead of Toon ints |
| `Assets/_Project/Presentation/Combat/Arena/CombatArenaBootstrap.cs` | Synty ground mesh + prop ring |
| `Assets/_Project/Presentation/Combat/Arena/CombatArenaVfx.cs` | Default PolygonParticleFX refs |
| `Assets/_Project/Data/Visual/Presets/SyntyTrenchUiTheme.asset` | New UI theme |
| `Assets/_Project/Data/Visual/Presets/SyntyTrenchRunAtmosphere.asset` | Trench fog palette |
| `Assets/_Project/Core.Tests/EditMode/SandboxArtCoverageTests.cs` | Add legacy-path guard test |

---

### Task 1: Scaffold folders and path constants

**Files:**
- Create: `Assets/_Project/Data/Editor/Synty/SyntyArtPaths.cs`
- Create: `Assets/_Project/Art/Synty/Arena/Units/.gitkeep` (via generator)
- Modify: `Assets/_Project/Data/Editor/SandboxArt/SandboxArtPaths.cs`

- [ ] **Step 1: Create `SyntyArtPaths.cs`**

```csharp
namespace DeadManZone.Data.Editor
{
    internal static class SyntyArtPaths
    {
        internal const string ProjectArtRoot = "Assets/_Project/Art/Synty";
        internal const string ArenaUnits = ProjectArtRoot + "/Arena/Units";
        internal const string ArenaVehicles = ProjectArtRoot + "/Arena/Vehicles";
        internal const string ArenaBuildings = ProjectArtRoot + "/Arena/Buildings";
        internal const string Icons = ProjectArtRoot + "/Icons";

        // Sidekick role prefabs (sources)
        internal const string SidekickRifle =
            "Assets/Synty/SidekickCharacters/Characters/ScifiSoldiers/ScifiSoldier_03/ScifiSoldier_03.prefab";
        internal const string SidekickSupport =
            "Assets/Synty/SidekickCharacters/Characters/ScifiSoldiers/ScifiSoldier_05/ScifiSoldier_05.prefab";
        internal const string SidekickMedic =
            "Assets/Synty/SidekickCharacters/Characters/ScifiSoldiers/ScifiSoldier_02/ScifiSoldier_02.prefab";
        internal const string SidekickSniper =
            "Assets/Synty/SidekickCharacters/Characters/ScifiSoldiers/ScifiSoldier_06/ScifiSoldier_06.prefab";
        internal const string SidekickOfficer =
            "Assets/Synty/SidekickCharacters/Characters/ScifiSoldiers/ScifiSoldier_04/ScifiSoldier_04.prefab";

        internal const string LocomotionController =
            "Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/AC_Polygon_Masculine.controller";

        // Vehicles
        internal const string GermanTruck =
            "Assets/Synty/PolygonWar/Prefabs/Vehicles/SM_Veh_German_Truck_01.prefab";
        internal const string GermanCar =
            "Assets/Synty/PolygonWar/Prefabs/Vehicles/SM_Veh_German_Car_01.prefab";
        internal const string GermanHalftrack =
            "Assets/Synty/PolygonWar/Prefabs/Vehicles/SM_Veh_German_Halftrack_01.prefab";
        internal const string GermanTank =
            "Assets/Synty/PolygonWar/Prefabs/Vehicles/SM_Veh_German_Tank_01.prefab";
        internal const string MechWalker =
            "Assets/Synty/PolygonMech/Prefabs/Mechs/SM_Mech_01.prefab";

        // Buildings
        internal const string BunkerLarge =
            "Assets/Synty/PolygonWar/Prefabs/Buildings/SM_Bld_Bunker_Large_01.prefab";
        internal const string BunkerGun =
            "Assets/Synty/PolygonWar/Prefabs/Buildings/SM_Bld_Bunker_Gun_01.prefab";
        internal const string Barracks =
            "Assets/Synty/PolygonWar/Prefabs/Buildings/SM_Bld_Barracks_01.prefab";

        // Wrapper outputs
        internal const string UnitRifle = ArenaUnits + "/ArenaUnit_Rifle.prefab";
        internal const string UnitSupport = ArenaUnits + "/ArenaUnit_Support.prefab";
        internal const string UnitMedic = ArenaUnits + "/ArenaUnit_Medic.prefab";
        internal const string UnitSniper = ArenaUnits + "/ArenaUnit_Sniper.prefab";
        internal const string UnitOfficer = ArenaUnits + "/ArenaUnit_Officer.prefab";
        internal const string VehicleTruck = ArenaVehicles + "/ArenaVehicle_Truck.prefab";
        internal const string VehicleCar = ArenaVehicles + "/ArenaVehicle_Car.prefab";
        internal const string VehicleHalftrack = ArenaVehicles + "/ArenaVehicle_Halftrack.prefab";
        internal const string VehicleTank = ArenaVehicles + "/ArenaVehicle_Tank.prefab";
        internal const string VehicleMech = ArenaVehicles + "/ArenaVehicle_Mech.prefab";
        internal const string BuildingHq = ArenaBuildings + "/ArenaBuilding_Hq.prefab";
        internal const string BuildingFieldGun = ArenaBuildings + "/ArenaBuilding_FieldGun.prefab";
        internal const string BuildingSupplyDepot = ArenaBuildings + "/ArenaBuilding_SupplyDepot.prefab";

        internal static string IconPath(string pieceId) => $"{Icons}/{pieceId}_icon.png";
    }
}
```

- [ ] **Step 2: Point `SandboxArtPaths` at Synty wrappers**

Replace Toon/RTS constants with `SyntyArtPaths` wrapper paths. Keep `CatalogAssetPath`, `ResourcesFolder` unchanged. Set `SandboxIconsFolder` to `SyntyArtPaths.Icons`.

- [ ] **Step 3: Verify compile**

Run Unity or `dotnet` test project build if available. No test changes yet.

---

### Task 2: Arena prefab generator (editor)

**Files:**
- Create: `Assets/_Project/Data/Editor/Synty/SyntyArenaPrefabGenerator.cs`

- [ ] **Step 1: Implement generator with menu item**

```csharp
[MenuItem("DeadManZone/Synty/Generate Arena Prefab Wrappers")]
public static void GenerateAll()
{
    EnsureFolders();
    CreateUnitPrefab(SyntyArtPaths.UnitRifle, SyntyArtPaths.SidekickRifle);
    CreateUnitPrefab(SyntyArtPaths.UnitSupport, SyntyArtPaths.SidekickSupport);
    CreateUnitPrefab(SyntyArtPaths.UnitMedic, SyntyArtPaths.SidekickMedic);
    CreateUnitPrefab(SyntyArtPaths.UnitSniper, SyntyArtPaths.SidekickSniper);
    CreateUnitPrefab(SyntyArtPaths.UnitOfficer, SyntyArtPaths.SidekickOfficer);
    CreateMeshWrapper(SyntyArtPaths.VehicleTruck, SyntyArtPaths.GermanTruck, 1f);
    CreateMeshWrapper(SyntyArtPaths.VehicleCar, SyntyArtPaths.GermanCar, 1f);
    CreateMeshWrapper(SyntyArtPaths.VehicleHalftrack, SyntyArtPaths.GermanHalftrack, 1f);
    CreateMeshWrapper(SyntyArtPaths.VehicleTank, SyntyArtPaths.GermanTank, 1f);
    CreateMeshWrapper(SyntyArtPaths.VehicleMech, SyntyArtPaths.MechWalker, 1f);
    CreateMeshWrapper(SyntyArtPaths.BuildingHq, SyntyArtPaths.BunkerLarge, 1f);
    CreateMeshWrapper(SyntyArtPaths.BuildingFieldGun, SyntyArtPaths.BunkerGun, 1f);
    CreateMeshWrapper(SyntyArtPaths.BuildingSupplyDepot, SyntyArtPaths.Barracks, 1f);
    AssetDatabase.SaveAssets();
}
```

`CreateUnitPrefab`: instantiate Sidekick source, add `Animator` with `AC_Polygon_Masculine.controller`, save as prefab at output path.

`CreateMeshWrapper`: empty root + child prefab instance, pivot Y=0, forward +Z.

- [ ] **Step 2: Run in Unity**

Menu: `DeadManZone/Synty/Generate Arena Prefab Wrappers`. Confirm 13 prefabs under `Assets/_Project/Art/Synty/Arena/`.

- [ ] **Step 3: Fix pink materials if any**

Menu: Synty `Tools > Replace Shaders` (PNB_Core) if materials render magenta.

---

### Task 3: Synty art catalog factory

**Files:**
- Create: `Assets/_Project/Data/Editor/Synty/SyntyArtCatalogFactory.cs`
- Modify: `Assets/_Project/Data/Editor/SandboxArt/SandboxArtDefaultCatalogFactory.cs` (delegate or deprecate)

- [ ] **Step 1: Add menu `DeadManZone/Synty/Create Synty Sandbox Art Catalog`**

Build 25 entries mapping piece IDs to wrapper prefabs per spec §3. Example entries:

| pieceId | prefab | icon | snapshot |
|---------|--------|------|----------|
| rifle_squad | UnitRifle | IconPath("rifle_squad") | true |
| mg_team | UnitSupport | ... | true |
| diesel_walker | VehicleMech | ... | true |
| ironmarch_hq | BuildingHq | ... | true |
| radio_array | BuildingSupplyDepot | ... | true |

All infantry share 5 role prefabs; vehicles/buildings use vehicle/building wrappers.

- [ ] **Step 2: Run catalog factory in Unity**

Verify `SandboxArtCatalog.asset` updated with Synty paths only (no `Toon_Soldiers` or `RTS_Modern` strings).

---

### Task 4: Legacy path guard test

**Files:**
- Modify: `Assets/_Project/Core.Tests/EditMode/SandboxArtCoverageTests.cs`

- [ ] **Step 1: Add failing test**

```csharp
[Test]
public void SandboxArtCatalog_NoLegacyThirdPartyPaths()
{
    var catalog = SandboxArtCatalogSO.LoadFromResources();
    Assert.NotNull(catalog);
    var forbidden = new[] { "Toon_Soldiers", "RTS_Modern", "BunkerSurvivalUI/Sprites/Icons" };
    foreach (var entry in catalog.entries)
    {
        foreach (var bad in forbidden)
        {
            Assert.IsFalse(entry.combatArenaPrefabPath?.Contains(bad) ?? false,
                $"Entry '{entry.pieceId}' still references {bad}");
            Assert.IsFalse(entry.iconAssetPath?.Contains(bad) ?? false,
                $"Entry '{entry.pieceId}' icon still references {bad}");
        }
    }
}
```

- [ ] **Step 2: Run test — expect FAIL until Task 3 catalog applied**

Command (Unity Test Runner EditMode) or CI equivalent.

- [ ] **Step 3: Apply catalog + art pass — expect PASS**

Menus:
1. `DeadManZone/Synty/Create Synty Sandbox Art Catalog`
2. `DeadManZone/Art/Snapshot Missing Icons From Prefabs` (update icon folder to SyntyArtPaths.Icons in snapshotter if needed)
3. `DeadManZone/Art/Apply Sandbox Art Pass`

---

### Task 5: Locomotion visual driver

**Files:**
- Create: `Assets/_Project/Presentation/Combat/Arena/ICombatUnitVisualDriver.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/SyntyLocomotionVisualDriver.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/StaticMeshVisualDriver.cs`
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaUnitVisual.cs`

- [ ] **Step 1: Define interface**

```csharp
public interface ICombatUnitVisualDriver
{
    void Bind(Animator animator);
    void SetWalking(bool walking);
    void PlayAttack();
    void Clear();
}
```

- [ ] **Step 2: Implement `SyntyLocomotionVisualDriver`**

Use animator params from `AC_Polygon_Masculine`:
- `MoveSpeed` float: 0 idle, ~1.5 walk
- `CurrentGait` int: 1 walk (verify in controller; fallback to MoveSpeed only)
- Attack: set `MoveSpeed=0`, optional punch trigger if present; else rely on existing lunge tween in `CombatArenaUnitVisual`

- [ ] **Step 3: Implement `StaticMeshVisualDriver`**

All methods no-op (vehicles/buildings).

- [ ] **Step 4: Refactor `CombatArenaUnitVisual`**

Remove Toon WW2 `StringToHash` ints and `HideUnusedWeapons`. On `Build()`:
```csharp
_animator = instance.GetComponentInChildren<Animator>();
_driver = _animator != null
    ? new SyntyLocomotionVisualDriver()
    : new StaticMeshVisualDriver();
_driver.Bind(_animator);
```
Delegate `SetWalking`, attack routine to `_driver`.

- [ ] **Step 5: Run EditMode tests — all green**

---

### Task 6: Synty UI theme

**Files:**
- Create: `Assets/_Project/Data/Editor/Synty/SyntyUiKitSetup.cs`
- Create: `Assets/_Project/Data/Visual/Presets/SyntyTrenchUiTheme.asset` (via menu)
- Modify: `Assets/_Project/Presentation/Editor/UiThemeSceneStyling.cs`
- Modify: `Assets/_Project/Presentation/Editor/VisualProfilePresetFactory.cs`
- Modify: `Assets/_Project/Data/Resources/DeadManZone/VisualProfile.asset`

- [ ] **Step 1: Implement `SyntyUiKitSetup`**

Mirror `BunkerSurvivalUiKitSetup` structure:
- `KitRoot = "Assets/Synty/InterfaceMilitaryCombatHUD"`
- Load sprites from `Sprites/Frames/`, `Sprites/Buttons/`, `Sprites/Banners/`
- Copy zone/lane colors from BunkerSurvival theme, desaturate ~10%
- `menuBackgroundSprite` from `InterfaceModernMenus` or Military HUD branding bg

Menu: `DeadManZone/UI Kit/Import Synty Trench Theme`

- [ ] **Step 2: Wire active VisualProfile**

Menu: `DeadManZone/UI Kit/Apply Synty Theme To Active Profile`

- [ ] **Step 3: Update fallback chain in `UiThemeSceneStyling`**

Prefer Synty when `InterfaceMilitaryCombatHUD` folder exists.

- [ ] **Step 4: Manual verify in Unity**

Open Run scene — panels use Military HUD frames, not BunkerSurvival sprites.

---

### Task 7: Combat arena environment

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaBootstrap.cs`
- Modify: `Assets/_Project/Scenes/CombatArena.unity` (via editor setup or bootstrap)
- Create: `Assets/_Project/Data/Visual/Presets/SyntyTrenchRunAtmosphere.asset`

- [ ] **Step 1: Ground mesh**

In `EnsureGround()`, if `config.useSyntyTerrain` (new bool on `CombatArenaConfigSO`, default true):
- Load prefab/mesh from `PolygonMapsWoodlandApocalypse` ground tile
- Apply Synty ground material instead of flat brown Lit color

- [ ] **Step 2: Prop ring**

Add `SpawnPerimeterProps(BattlefieldLayout layout)`: instantiate 8–12 `PolygonWar` sandbag/trench wall prefabs around board bounds. Parent to `transform`, static batching.

- [ ] **Step 3: Skybox**

On `Awake`, assign `PolygonApocalypse` skybox material to `RenderSettings.skybox`.

- [ ] **Step 4: Atmosphere SO**

Create `SyntyTrenchRunAtmosphere.asset` with fog per spec §5.1. Assign to VisualProfile `runAtmosphere`.

---

### Task 8: Combat VFX

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaVfx.cs`
- Modify: `Assets/_Project/Scenes/CombatArena.unity`

- [ ] **Step 1: Assign default prefabs**

Serialize defaults (or load from Resources):
- `impactPrefab` → `Assets/Synty/PolygonParticleFX/Prefabs/FX_Gunshot_01.prefab`
- `deathPrefab` → `Assets/Synty/PolygonParticleFX/Prefabs/FX_Dust_Small_01.prefab`

Wire on `CombatArenaBootstrap` or scene object if not serialized.

- [ ] **Step 2: PlayMode smoke**

Enter combat — damage spawns gunshot burst, death spawns dust.

---

### Task 9: Icon snapshot pass

**Files:**
- Modify: `Assets/_Project/Data/Editor/SandboxArt/SandboxIconSnapshotter.cs`

- [ ] **Step 1: Update default icon folder to `SyntyArtPaths.Icons`**

- [ ] **Step 2: Add menu `DeadManZone/Synty/Apply Full Synty Art Pass`**

Batch runner:
1. Generate Arena Prefab Wrappers
2. Create Synty Sandbox Art Catalog
3. Snapshot Missing Icons (force re-snapshot: delete existing icons or add `forceResnapshot` flag)
4. Apply Sandbox Art Pass
5. Validate Sandbox Art Coverage

- [ ] **Step 3: Run full pass in Unity**

All 25 icons exist under `Assets/_Project/Art/Synty/Icons/`.

---

### Task 10: Main menu backdrop (optional polish)

**Files:**
- Modify: `Assets/_Project/Presentation/Editor/MenuSceneSetup.cs`
- Modify: `Assets/_Project/Scenes/MainMenu.unity`

- [ ] **Step 1: Add static 3D backdrop root**

Instantiate subset of `PolygonMapsMilitaryWarehouse` demo prefab (bunker corridor). Position behind UI camera. No collision.

- [ ] **Step 2: Apply Synty theme via existing restyle menu**

`DeadManZone/UI Kit/Restyle All Scenes With Synty Kit`

---

### Task 11: Final QA

- [ ] **Step 1: Run all EditMode tests**

`SandboxArtCoverageTests` — 4 tests green including `NoLegacyThirdPartyPaths`.

- [ ] **Step 2: Run PlayMode combat tests**

Existing `CombatArenaPlayModeTests` — no animator errors.

- [ ] **Step 3: Manual checklist**

1. MainMenu — Synty UI + warehouse backdrop
2. Run — board icons consistent Synty style; shop readable
3. Fight 1 — Synty soldiers walk/attack; bunkers visible; ground not flat brown
4. No console errors referencing missing Toon animators

---

### Task 12: Legacy cleanup (phase 2 — separate PR)

**Do not delete until Task 11 passes.**

- [ ] **Step 1: Grep `_Project` for legacy paths**

Confirm zero references to: `Toon_Soldiers`, `RTS_Modern`, `BunkerSurvivalUI`, `SimpleMilitary`, etc.

- [ ] **Step 2: Delete legacy folders**

Remove folders listed in spec §8. Keep `BunkerSurvivalUI` until user confirms UI looks correct in build.

- [ ] **Step 3: Update `.gitignore` if needed for large Synty samples**

Optional: ignore `Assets/Synty/*/Samples/Scenes/` to reduce noise (not required).

---

## Self-review (spec coverage)

| Spec § | Task |
|--------|------|
| §3 Asset mapping | Tasks 1–3, 9 |
| §4 UI theme | Task 6 |
| §5 Scenes/atmosphere | Tasks 7, 10 |
| §6 Code changes | Tasks 5, 7, 8 |
| §7 Shaders | Task 2 step 3 |
| §8 Legacy cleanup | Task 12 (phase 2) |
| §11 Testing | Tasks 4, 11 |

No placeholder steps. All paths defined.

---

## Execution notes

- **Unity Editor required** for Tasks 2, 3, 6, 9, 10 (prefab/scene asset creation).
- **Do not commit** unless user requests (per project git rules).
- **Branch:** create `feature/synty-asset-revamp` before Task 1 if not already on feature branch.
