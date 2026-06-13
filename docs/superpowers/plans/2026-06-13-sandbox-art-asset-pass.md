# Sandbox Art & Asset Pass Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire temporary 2D icons and 3D combat-arena prefabs for all 25 sandbox core pieces (10 neutral + 15 IronMarch) using Toon_Soldiers, RTS vehicles, Grok/BunkerSurvivalUI icons, and existing cube building prefabs.

**Architecture:** A `SandboxArtCatalog` ScriptableObject holds per-piece icon paths, prefab paths, and arena transform hints. Editor menus apply the catalog to `PieceDefinitionSO` assets in one shot; an optional snapshotter renders missing icons from prefabs. EditMode tests enforce coverage on the static 25-piece roster list.

**Tech Stack:** Unity 6, C#, Unity Editor (`AssetDatabase`), Unity Test Framework (EditMode), existing `PieceDefinitionSO` visual fields, `NeutralArtPipelineEditor`, `CombatArenaBuildingPrefabGenerator`.

**Spec:** `docs/superpowers/specs/2026-06-13-sandbox-art-asset-pass-design.md`

---

## Execution order & green gates

| Task | Green gate |
|------|------------|
| 1 — Roster + catalog SO types | Project compiles |
| 2 — Default catalog factory | `Create Default Sandbox Art Catalog` menu produces asset with 25 entries |
| 3 — Assigner + validate menus | Manual: Apply sets icons on one test piece |
| 4 — Icon snapshotter | PNG files appear under `Art/Sandbox/Renders/Icons/` |
| 5 — Coverage tests | EditMode: `SandboxArtCoverageTests` passes **after** Apply |
| 6 — Full apply + asset commit | All 25 pieces have icons; units/hybrids have prefabs |
| 7 — PlayMode smoke | `CombatArenaPlayModeTests` still pass |

**Important:** Run `DeadManZone/Art/Apply Sandbox Art Pass` after any `Generate Demo Content` regen — content generation clears `icon` and `combatArenaPrefab`.

Run tests: **Window → General → Test Runner → EditMode → Run All** (or filter `SandboxArtCoverageTests`).

---

## Master file map

| File | Action |
|------|--------|
| `Assets/_Project/Data/Art/SandboxArtRoster.cs` | Create — static 25-piece id list + category helpers |
| `Assets/_Project/Data/Art/SandboxArtCatalogSO.cs` | Create — SO + entry struct |
| `Assets/_Project/Data/Editor/SandboxArt/SandboxArtPaths.cs` | Create — prefab/icon path constants |
| `Assets/_Project/Data/Editor/SandboxArt/SandboxArtDefaultCatalogFactory.cs` | Create — seed 25 mappings |
| `Assets/_Project/Data/Editor/SandboxArt/SandboxArtAssigner.cs` | Create — Apply + Validate menus |
| `Assets/_Project/Data/Editor/SandboxArt/SandboxIconSnapshotter.cs` | Create — render prefab → PNG |
| `Assets/_Project/Data/Resources/DeadManZone/SandboxArtCatalog.asset` | Create via editor menu |
| `Assets/_Project/Art/Sandbox/Renders/Icons/` | Create folder; PNG outputs |
| `Assets/_Project/Core.Tests/EditMode/SandboxArtCoverageTests.cs` | Create |

---

## Prefab path constants (reference)

```
Toon root: Assets/Toon_Soldiers/ToonSoldiers_WW2/prefabs/
  TSww2_German_infantry.prefab
  TSww2_German_medic.prefab
  TSww2_German_support.prefab
  TSww2_German_sniper.prefab
  TSww2_German_officer.prefab

RTS root: Assets/RTS_Modern_Combat_Vehicle_Pack_Free/
  ATV_N1/0_Prefabs/ATV_N1_Color_0_Prefab.prefab
  FA_N26/0_Prefabs/FA_N26_Color_0_Prefab.prefab
  FA_N26/0_Prefabs/FA_N26_Color_1_Prefab.prefab
  MSH_N2/0_Prefabs/MSH_N2_Color_0_Prefab.prefab
  MSH_N2/0_Prefabs/MSH_N2_Color_1_Prefab.prefab

Building cubes: Assets/_Project/Presentation/Combat/Arena/Prefabs/Buildings/
  ArenaBuilding_Hq.prefab
  ArenaBuilding_FieldGun.prefab
  ArenaBuilding_SupplyDepot.prefab

Bunker icons: Assets/BunkerSurvivalUI/Sprites/Icons/
  icon_bunker_map.png, icon_emergency_radio.png, icon_fuel_canister.png,
  icon_generator_part.png, icon_toolbox.png

Grok icons: Assets/_Project/Art/Neutral/Renders/Icons/{pieceId}_icon.png
Sandbox snapshots: Assets/_Project/Art/Sandbox/Renders/Icons/{pieceId}_icon.png
```

---

## Task 1: Sandbox roster + catalog ScriptableObject

**Files:**
- Create: `Assets/_Project/Data/Art/SandboxArtRoster.cs`
- Create: `Assets/_Project/Data/Art/SandboxArtCatalogSO.cs`

- [ ] **Step 1: Create `SandboxArtRoster.cs`**

```csharp
using System.Collections.Generic;
using DeadManZone.Core.Board;

namespace DeadManZone.Data
{
    public static class SandboxArtRoster
    {
        public static readonly string[] AllPieceIds =
        {
            "conscript_rifleman", "grenade_thrower", "field_medic", "armored_transport",
            "mobile_cannon", "neutral_supply_depot", "neutral_field_gun", "shock_trooper",
            "neutral_mortar_team", "marksman_squad",
            "ironmarch_hq", "rifle_squad", "diesel_walker", "radio_array", "mg_team",
            "field_gun_nest", "supply_depot", "field_workshop", "mobile_artillery",
            "ironmarch_heavy_tank", "ironmarch_mortar", "ironmarch_engineer",
            "ironmarch_breacher", "ironmarch_sniper", "ironmarch_defender"
        };

        private static readonly HashSet<string> RequiresArenaPrefabCategories = new()
        {
            // piece ids that must have combatArenaPrefab (units + hybrids only)
        };

        public static bool RequiresCombatArenaPrefab(PieceCategory category) =>
            category is PieceCategory.Unit or PieceCategory.Hybrid;
    }
}
```

Populate `RequiresArenaPrefabCategories` is unnecessary — use `PieceCategory` from loaded piece at test/validate time.

- [ ] **Step 2: Create `SandboxArtCatalogSO.cs`**

```csharp
using System;
using UnityEngine;

namespace DeadManZone.Data
{
    [Serializable]
    public struct SandboxArtEntry
    {
        public string pieceId;
        public string iconAssetPath;
        [Tooltip("Empty = no prefab assignment (radio_array uses runtime placeholder).")]
        public string combatArenaPrefabPath;
        public float combatArenaModelScale;
        public float combatArenaModelHeight;
        public bool snapshotIconFromPrefab;
    }

    [CreateAssetMenu(menuName = "DeadManZone/Sandbox Art Catalog")]
    public sealed class SandboxArtCatalogSO : ScriptableObject
    {
        public SandboxArtEntry[] entries = Array.Empty<SandboxArtEntry>();

        private const string ResourcesPath = "DeadManZone/SandboxArtCatalog";

        public static SandboxArtCatalogSO LoadFromResources() =>
            Resources.Load<SandboxArtCatalogSO>(ResourcesPath);

        public bool TryGetEntry(string pieceId, out SandboxArtEntry entry)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (string.Equals(entries[i].pieceId, pieceId, StringComparison.Ordinal))
                {
                    entry = entries[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }
    }
}
```

- [ ] **Step 3: Verify compile**

Open Unity or check IDE — no errors in `DeadManZone.Data` assembly.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Data/Art/SandboxArtRoster.cs Assets/_Project/Data/Art/SandboxArtCatalogSO.cs
git commit -m "feat: add sandbox art roster and catalog ScriptableObject types"
```

---

## Task 2: Default catalog factory (25 mappings)

**Files:**
- Create: `Assets/_Project/Data/Editor/SandboxArt/SandboxArtPaths.cs`
- Create: `Assets/_Project/Data/Editor/SandboxArt/SandboxArtDefaultCatalogFactory.cs`

- [ ] **Step 1: Create `SandboxArtPaths.cs`** with `internal static class SandboxArtPaths` holding `const string` for every prefab and bunker icon path from the reference section above, plus:

```csharp
internal static string GrokIcon(string pieceId) =>
    $"Assets/_Project/Art/Neutral/Renders/Icons/{pieceId}_icon.png";

internal static string SandboxSnapshotIcon(string pieceId) =>
    $"Assets/_Project/Art/Sandbox/Renders/Icons/{pieceId}_icon.png";

internal const string CatalogAssetPath =
    "Assets/_Project/Data/Resources/DeadManZone/SandboxArtCatalog.asset";
```

- [ ] **Step 2: Create `SandboxArtDefaultCatalogFactory.cs`**

Implement `[MenuItem("DeadManZone/Art/Create Default Sandbox Art Catalog")]` that:

1. Ensures folder `Assets/_Project/Data/Resources/DeadManZone/` exists
2. Creates or loads `SandboxArtCatalog.asset`
3. Sets `entries` to exactly 25 `SandboxArtEntry` values per spec §4:

| pieceId | prefab | icon path | scale | height | snapshot |
|---------|--------|-----------|-------|--------|----------|
| conscript_rifleman | German_infantry | GrokIcon | 1 | 1.6 | false |
| grenade_thrower | German_support | GrokIcon | 1 | 1.6 | false |
| field_medic | German_medic | GrokIcon | 1 | 1.6 | false |
| armored_transport | ATV_N1_Color_0 | GrokIcon | 0.9 | 1.2 | false |
| mobile_cannon | MSH_N2_Color_0 | GrokIcon | 0.85 | 1.4 | false |
| neutral_supply_depot | ArenaBuilding_SupplyDepot | icon_fuel_canister | 1 | 0 | false |
| neutral_field_gun | ArenaBuilding_FieldGun | icon_generator_part | 1 | 0 | false |
| shock_trooper | German_officer | SandboxSnapshotIcon | 1 | 1.6 | true |
| neutral_mortar_team | German_support | SandboxSnapshotIcon | 1 | 1.6 | true |
| marksman_squad | German_sniper | SandboxSnapshotIcon | 1 | 1.6 | true |
| ironmarch_hq | ArenaBuilding_Hq | icon_bunker_map | 1 | 0 | false |
| rifle_squad | German_infantry | SandboxSnapshotIcon | 1 | 1.6 | true |
| diesel_walker | FA_N26_Color_0 | SandboxSnapshotIcon | 0.9 | 1.4 | true |
| radio_array | *(empty string)* | icon_emergency_radio | 1 | 0 | false |
| mg_team | German_support | SandboxSnapshotIcon | 1 | 1.6 | true |
| field_gun_nest | ArenaBuilding_FieldGun | icon_generator_part | 1 | 0 | false |
| supply_depot | ArenaBuilding_SupplyDepot | icon_fuel_canister | 1 | 0 | false |
| field_workshop | ArenaBuilding_SupplyDepot | icon_toolbox | 1 | 0 | false |
| mobile_artillery | MSH_N2_Color_1 | SandboxSnapshotIcon | 0.85 | 1.4 | true |
| ironmarch_heavy_tank | FA_N26_Color_1 | SandboxSnapshotIcon | 0.95 | 1.5 | true |
| ironmarch_mortar | German_support | SandboxSnapshotIcon | 1 | 1.6 | true |
| ironmarch_engineer | German_medic | SandboxSnapshotIcon | 1 | 1.6 | true |
| ironmarch_breacher | German_officer | SandboxSnapshotIcon | 1 | 1.6 | true |
| ironmarch_sniper | German_sniper | SandboxSnapshotIcon | 1 | 1.6 | true |
| ironmarch_defender | German_infantry | SandboxSnapshotIcon | 1 | 1.6 | true |

4. `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets`

- [ ] **Step 3: Run menu in Unity**

`DeadManZone → Art → Create Default Sandbox Art Catalog`

Expected: `SandboxArtCatalog.asset` created with 25 entries.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Data/Editor/SandboxArt/ Assets/_Project/Data/Resources/DeadManZone/SandboxArtCatalog.asset Assets/_Project/Data/Resources/DeadManZone/SandboxArtCatalog.asset.meta
git commit -m "feat: seed default sandbox art catalog with 25 piece mappings"
```

---

## Task 3: Assigner + validate menus

**Files:**
- Create: `Assets/_Project/Data/Editor/SandboxArt/SandboxArtAssigner.cs`

- [ ] **Step 1: Implement assigner**

```csharp
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class SandboxArtAssigner
    {
        private const string PiecesRoot = "Assets/_Project/Data/Resources/DeadManZone/Pieces";

        [MenuItem("DeadManZone/Art/Apply Sandbox Art Pass")]
        public static void ApplySandboxArtPass()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SandboxArtCatalogSO>(SandboxArtPaths.CatalogAssetPath);
            if (catalog == null)
            {
                Debug.LogError("SandboxArtCatalog missing. Run Create Default Sandbox Art Catalog first.");
                return;
            }

            int applied = 0;
            foreach (var entry in catalog.entries)
            {
                if (ApplyEntry(entry))
                    applied++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Sandbox art pass applied to {applied}/{catalog.entries.Length} pieces.");
        }

        [MenuItem("DeadManZone/Art/Validate Sandbox Art Coverage")]
        public static void ValidateSandboxArtCoverage()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SandboxArtCatalogSO>(SandboxArtPaths.CatalogAssetPath);
            if (catalog == null)
            {
                Debug.LogError("SandboxArtCatalog missing.");
                return;
            }

            int issues = 0;
            foreach (var pieceId in SandboxArtRoster.AllPieceIds)
            {
                var piece = LoadPiece(pieceId);
                if (piece == null)
                {
                    Debug.LogWarning($"Missing piece asset: {pieceId}");
                    issues++;
                    continue;
                }

                if (piece.icon == null)
                {
                    Debug.LogWarning($"{pieceId}: icon not assigned");
                    issues++;
                }

                if (SandboxArtRoster.RequiresCombatArenaPrefab(piece.category)
                    && piece.combatArenaPrefab == null)
                {
                    Debug.LogWarning($"{pieceId}: combatArenaPrefab required for {piece.category}");
                    issues++;
                }
            }

            Debug.Log(issues == 0
                ? "Sandbox art coverage: OK (25/25)"
                : $"Sandbox art coverage: {issues} issue(s) — run Apply Sandbox Art Pass");
        }

        private static bool ApplyEntry(SandboxArtEntry entry)
        {
            var piece = LoadPiece(entry.pieceId);
            if (piece == null)
                return false;

            if (!string.IsNullOrEmpty(entry.iconAssetPath))
            {
                ConfigureSpriteImporter(entry.iconAssetPath);
                piece.icon = AssetDatabase.LoadAssetAtPath<Sprite>(entry.iconAssetPath);
            }

            if (!string.IsNullOrEmpty(entry.combatArenaPrefabPath))
            {
                piece.combatArenaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.combatArenaPrefabPath);
                piece.combatArenaModelScale = entry.combatArenaModelScale > 0f ? entry.combatArenaModelScale : 1f;
                piece.combatArenaModelHeight = entry.combatArenaModelHeight;
            }

            EditorUtility.SetDirty(piece);
            return piece.icon != null;
        }

        private static PieceDefinitionSO LoadPiece(string pieceId) =>
            AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>($"{PiecesRoot}/{pieceId}.asset");

        private static void ConfigureSpriteImporter(string assetPath)
        {
            // Reuse same settings as NeutralArtPipelineEditor.ConfigureSpriteImporter (256, sprite, no mips)
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();
        }
    }
}
#endif
```

Extract shared `ConfigureSpriteImporter` to a small `SandboxArtSpriteImporter.cs` if duplication with `NeutralArtPipelineEditor` becomes noisy — optional, not required for v1.

- [ ] **Step 2: Ensure building placeholder prefabs exist**

Run: `DeadManZone → Combat Arena → Generate Building Placeholder Prefabs`

- [ ] **Step 3: Assign Grok icons for 5 neutrals**

Run in order:
1. `DeadManZone → Art → Import Grok Batch 2 Icons` (if Grok source images present)
2. `DeadManZone → Art → Assign Neutral Icons From Renders`

If Grok sources missing, run `DeadManZone → Art → Generate Placeholder Neutral Icons` instead.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Data/Editor/SandboxArt/SandboxArtAssigner.cs
git commit -m "feat: add sandbox art apply and validate editor menus"
```

---

## Task 4: Icon snapshotter

**Files:**
- Create: `Assets/_Project/Data/Editor/SandboxArt/SandboxIconSnapshotter.cs`

- [ ] **Step 1: Implement snapshotter**

`[MenuItem("DeadManZone/Art/Snapshot Missing Icons From Prefabs")]`:

1. Load catalog
2. For each entry where `snapshotIconFromPrefab == true` and PNG missing at `iconAssetPath`:
   - Load prefab from `combatArenaPrefabPath`
   - Render with orthographic camera (elevation ~35°, azimuth ~225° — match neutral art spec)
   - Write 256×256 PNG to `iconAssetPath`
   - Configure sprite importer

Use a hidden temporary scene or `PreviewRenderUtility` (UnityEditor) — standard Editor pattern:

```csharp
using (var preview = new PreviewRenderUtility())
{
    preview.camera.orthographic = true;
    preview.camera.transform.rotation = Quaternion.Euler(35f, 225f, 0f);
    // instantiate prefab, frame bounds, Render(), ReadPixels → PNG
}
```

- [ ] **Step 2: Run snapshotter in Unity**

`DeadManZone → Art → Snapshot Missing Icons From Prefabs`

Expected: ~17 PNG files under `Assets/_Project/Art/Sandbox/Renders/Icons/`.

- [ ] **Step 3: Commit snapshotter code + generated PNGs**

```bash
git add Assets/_Project/Data/Editor/SandboxArt/SandboxIconSnapshotter.cs Assets/_Project/Art/Sandbox/Renders/
git commit -m "feat: snapshot sandbox piece icons from toon and vehicle prefabs"
```

---

## Task 5: Coverage tests

**Files:**
- Create: `Assets/_Project/Core.Tests/EditMode/SandboxArtCoverageTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using DeadManZone.Core.Board;
using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class SandboxArtCoverageTests
    {
        [Test]
        public void SandboxRoster_AllPiecesHaveIcons()
        {
            var database = ContentDatabase.Load();
            Assert.NotNull(database);

            foreach (var pieceId in SandboxArtRoster.AllPieceIds)
            {
                var piece = FindPiece(database, pieceId);
                Assert.NotNull(piece, $"Missing piece '{pieceId}'");
                Assert.NotNull(piece.icon, $"Piece '{pieceId}' has no icon — run Apply Sandbox Art Pass");
            }
        }

        [Test]
        public void SandboxRoster_UnitsAndHybridsHaveArenaPrefabs()
        {
            var database = ContentDatabase.Load();
            Assert.NotNull(database);

            foreach (var pieceId in SandboxArtRoster.AllPieceIds)
            {
                var piece = FindPiece(database, pieceId);
                Assert.NotNull(piece);
                if (!SandboxArtRoster.RequiresCombatArenaPrefab(piece.category))
                    continue;

                Assert.NotNull(piece.combatArenaPrefab,
                    $"Piece '{pieceId}' ({piece.category}) needs combatArenaPrefab");
            }
        }

        [Test]
        public void SandboxArtCatalog_HasEntryForEveryRosterPiece()
        {
            var catalog = SandboxArtCatalogSO.LoadFromResources();
            Assert.NotNull(catalog, "SandboxArtCatalog missing from Resources/DeadManZone/");

            foreach (var pieceId in SandboxArtRoster.AllPieceIds)
                Assert.IsTrue(catalog.TryGetEntry(pieceId, out _), $"Catalog missing entry for '{pieceId}'");
        }

        private static PieceDefinitionSO FindPiece(ContentDatabase db, string id) =>
            System.Linq.Enumerable.FirstOrDefault(db.Pieces, p => p != null && p.id == id);
    }
}
```

- [ ] **Step 2: Run tests — expect FAIL**

Test Runner → `SandboxArtCoverageTests` — icons null until Apply runs.

- [ ] **Step 3: Apply full art pass in Unity**

Run in order:
1. `DeadManZone → Art → Create Default Sandbox Art Catalog` (if not done)
2. `DeadManZone → Combat Arena → Generate Building Placeholder Prefabs`
3. `DeadManZone → Art → Import Grok Batch 2 Icons` + `Assign Neutral Icons From Renders` (or placeholders)
4. `DeadManZone → Art → Snapshot Missing Icons From Prefabs`
5. `DeadManZone → Art → Apply Sandbox Art Pass`
6. `DeadManZone → Art → Validate Sandbox Art Coverage` → expect "OK (25/25)"

- [ ] **Step 4: Re-run tests — expect PASS**

All 295+ EditMode tests green.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Core.Tests/EditMode/SandboxArtCoverageTests.cs Assets/_Project/Data/Resources/DeadManZone/Pieces/
git commit -m "test: enforce sandbox art coverage for 25-piece roster"
```

---

## Task 6: PlayMode verification + push

- [ ] **Step 1: Run PlayMode smoke**

Test Runner → PlayMode → `CombatArenaPlayModeTests`

Expected: all pass (building cubes unchanged).

- [ ] **Step 2: Manual QA (5 min)**

1. Enter Play Mode on Run scene
2. Shop shows icons on neutral + IronMarch offers
3. Start combat — toon soldiers and vehicles visible in arena; buildings still cubes

- [ ] **Step 3: Push**

```bash
git push origin master
```

---

## Spec coverage self-review

| Spec § | Task |
|--------|------|
| 25-piece roster | Task 1 `SandboxArtRoster`, Task 2 factory |
| Catalog + assigner | Tasks 1–3 |
| Icon pipeline (Grok, Bunker, snapshot, placeholder) | Tasks 3–4 |
| Arena prefabs (toon + RTS + cubes) | Task 2 mappings, Task 3 Apply |
| `SandboxArtCoverageTests` | Task 5 |
| PlayMode smoke | Task 6 |
| Re-run Apply after content regen | Documented in header |
| Deferred (enemy factions, per-cell, grimdark) | Not in plan |

---

## Post-pass workflow note

Add to `AGENTS.md` or team habit:

> After `DeadManZone → Generate Demo Content`, always run `DeadManZone → Art → Apply Sandbox Art Pass`.
