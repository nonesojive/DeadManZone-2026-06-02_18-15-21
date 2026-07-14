> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# Iron Vanguard Premium Combat Slice — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a screenshot-ready Iron Vanguard combat vertical slice with fully animated units, trench-ring environment, Synty Apocalypse HUD, and evidence-based visual scorecard on branch `prettycombat`.

**Architecture:** Replay-driven presentation only — extend existing `CombatArenaPresenter` stack with a fixed slice layout builder, config preset, animation/VFX/audio asset bootstraps, and PlayMode tests. No combat sim changes.

**Tech Stack:** Unity 6000.3.x, URP, Synty POLYGON assets, Kevin Iglesias combat clips, Unity Test Framework, existing `CombatArena*` ScriptableObjects.

**Spec:** `docs/superpowers/specs/2026-06-15-iron-vanguard-combat-slice-design.md`

---

## File map

| File | Responsibility |
|------|----------------|
| `Assets/_Project/Presentation/Combat/Arena/CombatSliceLayouts.cs` | **Create** — builds player/enemy boards for Iron Vanguard skirmish |
| `Assets/_Project/Presentation/Combat/Arena/CombatSliceConstants.cs` | **Create** — seed `424242`, piece ID constants |
| `Assets/_Project/Presentation/Editor/CombatSliceLauncher.cs` | **Create** — editor menu to bootstrap assets + open slice |
| `Assets/_Project/Tests.PlayMode/CombatArenaTestBoards.cs` | **Modify** — add `BuildIronVanguardSkirmish` delegating to layouts |
| `Assets/_Project/Core.Tests/EditMode/IronVanguardSliceLayoutTests.cs` | **Create** — layout placement EditMode tests |
| `Assets/_Project/Tests.PlayMode/IronVanguardSlicePlayModeTests.cs` | **Create** — animator + replay integration tests |
| `Assets/_Project/Data/Resources/DeadManZone/CombatArenaConfig.asset` | **Modify** — enable `spawnPerimeterProps`, tune padding/fog |
| `Assets/_Project/Art/Synty/Animation/AC_CombatArena_Infantry.controller` | **Modify** — idle/walk/shoot/death states wired |
| `Assets/_Project/Data/Editor/CombatArenaCombatAnimatorBootstrap.cs` | **Modify** — rebuild controller from animation set clips |
| `docs/combat/prettycombat-visual-scorecard.md` | **Modify** — expand to 10-row rubric from spec |
| `docs/superpowers/specs/2026-06-15-iron-vanguard-combat-slice-design.md` | **Modify** — fix tank ID to `ironmarch_heavy_tank` |

**Note:** Design spec says `ironmarch_tank`; catalog piece ID is `ironmarch_heavy_tank`. Use catalog ID everywhere.

---

## Task 1: Slice layout builder

**Files:**
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatSliceConstants.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatSliceLayouts.cs`
- Modify: `Assets/_Project/Tests.PlayMode/CombatArenaTestBoards.cs`
- Test: `Assets/_Project/Core.Tests/EditMode/IronVanguardSliceLayoutTests.cs`

- [ ] **Step 1: Write the failing EditMode test**

Create `Assets/_Project/Core.Tests/EditMode/IronVanguardSliceLayoutTests.cs`:

```csharp
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class IronVanguardSliceLayoutTests
    {
        [Test]
        public void IronVanguardSkirmish_BuildsBattlefieldWithExpectedCombatants()
        {
            var database = ContentDatabase.Load();
            Assert.NotNull(database, "Run DeadManZone/Generate Vertical Slice Content first.");

            var battlefield = CombatSliceLayouts.BuildIronVanguardSkirmish(database);
            Assert.NotNull(battlefield);

            int combatants = 0;
            foreach (var cell in battlefield.Cells)
            {
                if (cell?.Definition == null)
                    continue;
                if (PieceTagQueries.HasTag(cell.Definition, GameTagIds.Combatant))
                    combatants++;
            }

            // 2 player rifles + 1 tank + 2 enemy rifles = 5 combatants
            // (HQ + field gun are buildings, not Combatant-tagged infantry)
            Assert.GreaterOrEqual(combatants, 5,
                "Slice should field at least five combatant-tagged units.");
        }

        [Test]
        public void IronVanguardSkirmish_AllPlacementsSucceed()
        {
            var database = ContentDatabase.Load();
            Assert.NotNull(database);

            Assert.DoesNotThrow(() => CombatSliceLayouts.BuildIronVanguardSkirmish(database));
        }
    }
}
```

Add `DeadManZone.Presentation` reference to `DeadManZone.Core.Tests.asmdef` if missing.

- [ ] **Step 2: Run test to verify it fails**

Run: Unity Test Runner → EditMode → `IronVanguardSliceLayoutTests`  
Expected: FAIL — `CombatSliceLayouts` not found

- [ ] **Step 3: Implement layout builder**

Create `Assets/_Project/Presentation/Combat/Arena/CombatSliceConstants.cs`:

```csharp
namespace DeadManZone.Presentation.Combat.Arena
{
    public static class CombatSliceConstants
    {
        public const int IronVanguardSkirmishSeed = 424242;

        public const string PlayerHq = "ironmarch_hq";
        public const string PlayerRifle = "ironmarch_rifle";
        public const string PlayerTank = "ironmarch_heavy_tank";
        public const string PlayerFieldGun = "field_gun_nest";

        public const string EnemyHq = "ironmarch_hq";
        public const string EnemyRifle = "ironmarch_rifle";
    }
}
```

Create `Assets/_Project/Presentation/Combat/Arena/CombatSliceLayouts.cs`:

```csharp
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Data;

namespace DeadManZone.Presentation.Combat.Arena
{
    public static class CombatSliceLayouts
    {
        public static BattlefieldState BuildIronVanguardSkirmish(ContentDatabase database)
        {
            var faction = database.GetFaction("iron_vanguard");
            if (faction == null)
                return null;

            var player = new BoardState(faction.CreateBoardLayout());
            Place(player, database, CombatSliceConstants.PlayerHq, new GridCoord(0, 4), "hq_player");
            Place(player, database, CombatSliceConstants.PlayerRifle, new GridCoord(2, 3), "rifle_1");
            Place(player, database, CombatSliceConstants.PlayerRifle, new GridCoord(2, 5), "rifle_2");
            Place(player, database, CombatSliceConstants.PlayerTank, new GridCoord(4, 4), "tank_1");
            Place(player, database, CombatSliceConstants.PlayerFieldGun, new GridCoord(3, 2), "field_gun_1");

            var enemy = new BoardState(faction.CreateBoardLayout());
            Place(enemy, database, CombatSliceConstants.EnemyHq, new GridCoord(0, 4), "enemy_hq");
            Place(enemy, database, CombatSliceConstants.EnemyRifle, new GridCoord(2, 3), "enemy_rifle_1");
            Place(enemy, database, CombatSliceConstants.EnemyRifle, new GridCoord(2, 5), "enemy_rifle_2");

            return BattlefieldState.FromBoards(player, enemy);
        }

        private static void Place(
            BoardState board,
            ContentDatabase database,
            string pieceId,
            GridCoord anchor,
            string instanceId)
        {
            var piece = database.Pieces.First(p => p.id == pieceId).ToCore();
            var result = board.TryPlace(piece, anchor, instanceId);
            if (!result.Success)
                throw new System.InvalidOperationException(
                    $"Slice placement failed: {pieceId} at {anchor} — {result.Reason}");
        }
    }
}
```

Update `CombatArenaTestBoards.cs`:

```csharp
public static BattlefieldState BuildIronVanguardSkirmish(ContentDatabase database) =>
    CombatSliceLayouts.BuildIronVanguardSkirmish(database);
```

- [ ] **Step 4: Run tests**

Expected: `IronVanguardSliceLayoutTests` PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Presentation/Combat/Arena/CombatSliceConstants.cs \
        Assets/_Project/Presentation/Combat/Arena/CombatSliceLayouts.cs \
        Assets/_Project/Tests.PlayMode/CombatArenaTestBoards.cs \
        Assets/_Project/Core.Tests/EditMode/IronVanguardSliceLayoutTests.cs \
        Assets/_Project/Core.Tests/DeadManZone.Core.Tests.asmdef
git commit -m "feat(combat): add Iron Vanguard skirmish slice layout builder"
```

---

## Task 2: Environment preset (trench ring)

**Files:**
- Modify: `Assets/_Project/Data/Resources/DeadManZone/CombatArenaConfig.asset`
- Create: `Assets/_Project/Presentation/Editor/CombatSliceEnvironmentBootstrap.cs`
- Test: existing `CombatArenaPlayModeTests` (manual visual check)

- [ ] **Step 1: Create editor bootstrap**

Create `Assets/_Project/Presentation/Editor/CombatSliceEnvironmentBootstrap.cs`:

```csharp
#if UNITY_EDITOR
using DeadManZone.Data;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    public static class CombatSliceEnvironmentBootstrap
    {
        private const string ConfigPath = "Assets/_Project/Data/Resources/DeadManZone/CombatArenaConfig.asset";

        [MenuItem("DeadManZone/Combat Arena/Apply Iron Vanguard Slice Environment")]
        public static void ApplySliceEnvironment()
        {
            var config = AssetDatabase.LoadAssetAtPath<CombatArenaConfigSO>(ConfigPath);
            if (config == null)
            {
                Debug.LogError($"Missing config at {ConfigPath}");
                return;
            }

            config.spawnPerimeterProps = true;
            config.groundPadding = 1.4f;
            config.enableArenaFog = true;
            config.fogDensity = 0.024f;
            config.useSyntySkybox = false;
            config.useFlatTexturedGround = true;

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("Iron Vanguard slice environment preset applied to CombatArenaConfig.");
        }
    }
}
#endif
```

- [ ] **Step 2: Run menu in Unity**

`DeadManZone → Combat Arena → Apply Iron Vanguard Slice Environment`

- [ ] **Step 3: Visual verify**

Play Mode → load arena with slice board → confirm 8 bunker walls ring the field, fog hides horizon.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Presentation/Editor/CombatSliceEnvironmentBootstrap.cs \
        Assets/_Project/Data/Resources/DeadManZone/CombatArenaConfig.asset
git commit -m "feat(combat): enable trench ring environment preset for slice"
```

---

## Task 3: Animation controller completion

**Files:**
- Modify: `Assets/_Project/Data/Editor/CombatArenaCombatAnimatorBootstrap.cs`
- Modify: `Assets/_Project/Art/Synty/Animation/AC_CombatArena_Infantry.controller`
- Test: `Assets/_Project/Tests.PlayMode/IronVanguardSlicePlayModeTests.cs` (partial)

- [ ] **Step 1: Write failing PlayMode test skeleton**

Create `Assets/_Project/Tests.PlayMode/IronVanguardSlicePlayModeTests.cs`:

```csharp
using System.Collections;
using System.Linq;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeadManZone.PlayMode.Tests
{
    public sealed class IronVanguardSlicePlayModeTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
            CombatArenaSession.ResetForTests();
        }

        [UnityTest]
        public IEnumerator SliceBoard_SpawnsRifleWithAnimator()
        {
            var database = ContentDatabase.Load();
            if (database == null)
            {
                Assert.Ignore("ContentDatabase missing.");
                yield break;
            }

            _root = new GameObject("SliceTestRoot");
            var loader = _root.AddComponent<CombatArenaSceneLoader>();
            yield return loader.LoadAsync();

            var presenterGo = new GameObject("Presenter");
            var presenter = presenterGo.AddComponent<CombatArenaPresenter>();
            yield return null;

            presenter.InitializeArena(CombatSliceLayouts.BuildIronVanguardSkirmish(database));

            var rifle = presenter.GetActiveActors().FirstOrDefault(a => a.InstanceId == "rifle_1");
            Assert.NotNull(rifle, "Player rifle should spawn.");
            var animator = rifle.GetComponentInChildren<Animator>();
            Assert.NotNull(animator, "Rifle should have an Animator.");
            Assert.IsNotNull(animator.runtimeAnimatorController, "Animator needs AC_CombatArena_Infantry.");

            yield return loader.UnloadAsync();
        }
    }
}
```

- [ ] **Step 2: Run test — expect PASS if prefabs already have animators**

If FAIL: ensure `ArenaUnit_Rifle.prefab` includes Sidekick model with Animator + `AC_CombatArena_Infantry`.

- [ ] **Step 3: Extend animator bootstrap**

In `CombatArenaCombatAnimatorBootstrap.cs`, ensure menu rebuilds `AC_CombatArena_Infantry` with states:
- **Idle** (default)
- **Walk** — bool `IsWalking` true
- **Shoot** — trigger `Shoot` → plays `CombatArenaAnimationSet.rifleShoot`
- **GrenadeThrow** — trigger `GrenadeThrow`
- **Death** — trigger `Death` → random death clip

Run: `DeadManZone → Combat Arena → Create Or Refresh Animation Set`  
Then: `DeadManZone → Combat Arena → Rebuild Combat Infantry Animator` (add menu if missing)

- [ ] **Step 4: Add move/attack PlayMode assertion**

Extend test with a synthetic `CombatDirector` replay of one `move` event; assert `IsWalking` or `MoveSpeed > 0` on rifle animator during move.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Tests.PlayMode/IronVanguardSlicePlayModeTests.cs \
        Assets/_Project/Data/Editor/CombatArenaCombatAnimatorBootstrap.cs \
        Assets/_Project/Art/Synty/Animation/AC_CombatArena_Infantry.controller
git commit -m "feat(combat): wire infantry animator for slice idle/walk/shoot/death"
```

---

## Task 4: Asset bootstrap bundle + dev launcher

**Files:**
- Create: `Assets/_Project/Presentation/Editor/CombatSliceLauncher.cs`

- [ ] **Step 1: Create one-shot bootstrap menu**

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    public static class CombatSliceLauncher
    {
        [MenuItem("DeadManZone/Combat Arena/Launch Iron Vanguard Slice (Bootstrap All)")]
        public static void BootstrapAll()
        {
            ApocalypseCombatHudSetup.ImportApocalypseCombatHud();
            CombatArenaVfxSetBootstrap.CreateOrRefresh();
            CombatArenaAnimationSetBootstrap.CreateOrRefresh();
            CombatSliceEnvironmentBootstrap.ApplySliceEnvironment();
            Debug.Log(
                "Iron Vanguard slice assets bootstrapped.\n" +
                "Next: open Run scene, begin combat with slice layout, capture screenshots.");
        }
    }
}
#endif
```

Add `using DeadManZone.Data.Editor;` for VFX/animation bootstrap classes.

- [ ] **Step 2: Run menu once in Unity**

Confirm no console errors; verify Resources assets populated.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Presentation/Editor/CombatSliceLauncher.cs
git commit -m "chore(combat): add Iron Vanguard slice bootstrap menu"
```

---

## Task 5: Integration hardening (already started on prettycombat)

**Files:**
- Verify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaAudioPresenter.cs`
- Verify: `Assets/_Project/Presentation/Combat/Arena/ArmyHealthBarView.cs`
- Verify: `Assets/_Project/Presentation/Combat/Arena/CombatHealthBarUiFactory.cs`

- [ ] **Step 1: Confirm audio uses PlayClipAtPoint only**

`CombatArenaAudioPresenter.PlayAt` must NOT assign `_source.transform.position` on the combat panel hierarchy.

- [ ] **Step 2: Confirm health bar fill uses fillRect, Slider disabled**

`ArmyHealthBarView.BindSlider` sets `fillSlider.enabled = false`; `SetFractionImmediate` sets `fillRect.anchorMax.x`.

- [ ] **Step 3: PlayMode regression**

Run: `ArmyHealthBarPlayModeTests`, `CombatArenaSpectaclePlayModeTests`, `CombatArenaReplayPlayModeTests`  
Expected: all PASS

- [ ] **Step 4: Commit if any fixes needed**

```bash
git commit -m "fix(combat): harden HUD fill and audio isolation for slice"
```

---

## Task 6: Visual scorecard + evidence

**Files:**
- Modify: `docs/combat/prettycombat-visual-scorecard.md`
- Use: `Assets/_Project/Presentation/Editor/CombatPrettyPassScreenshotCapture.cs`

- [ ] **Step 1: Update scorecard to 10-row rubric from spec §8**

Copy rubric table from `docs/superpowers/specs/2026-06-15-iron-vanguard-combat-slice-design.md` §8 into `docs/combat/prettycombat-visual-scorecard.md`. Add slice-specific encounter description and tank ID correction.

- [ ] **Step 2: Manual Play Mode evidence capture**

1. Run `DeadManZone → Combat Arena → Launch Iron Vanguard Slice (Bootstrap All)`
2. Open Run scene → enter combat with slice board
3. Play through first shot + first death
4. Run `DeadManZone → Combat Arena → Pretty Combat Pass — Capture Screenshot` ×3
5. Fill rubric scores in scorecard doc

- [ ] **Step 3: Commit docs + screenshots**

```bash
git add docs/combat/prettycombat-visual-scorecard.md \
        Assets/_Project/Art/QA/CombatPrettyPass/*.png
git commit -m "docs(combat): Iron Vanguard slice visual scorecard with evidence"
```

---

## Task 7: Director verification gate

- [ ] **Step 1: Run full test suite**

Unity Test Runner → EditMode + PlayMode  
Expected: all combat tests green including new slice tests

- [ ] **Step 2: Profiler spot-check**

Play Mode → slice combat → Profiler  
Target: ≥55 FPS @ 1080p; no GC.Alloc spike in `ArmyHealthBarPresenter.HandleReplayEvent`

- [ ] **Step 3: Build smoke test (optional)**

File → Build Settings → build Windows player  
Confirm `CombatHudAssets.asset` and `CombatArenaConfig` refs resolve (no pink materials)

- [ ] **Step 4: Mark slice complete in scorecard**

All 10 rubric rows ≥4/5 with screenshot paths filled in

---

## Spec coverage checklist

| Spec § | Task |
|--------|------|
| §2 Success criteria 1–4 | Task 1, 3 |
| §2 Success criteria 5–7 | Task 2, 3 |
| §2 Success criteria 8–9 | Task 5 (prettycombat branch) |
| §2 Success criteria 10–12 | Task 6, 7 |
| §4 Encounter | Task 1 |
| §5 Animation | Task 3 |
| §6 Environment | Task 2 |
| §7 HUD/VFX/Audio | Task 4, 5 |
| §8 Scorecard | Task 6 |
| §9 Testing | Tasks 1, 3, 7 |

---

## Plan complete

Saved to `docs/superpowers/plans/2026-06-15-iron-vanguard-combat-slice.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — fresh subagent per task, review between tasks, fast iteration  
2. **Inline Execution** — execute tasks in this session with checkpoints

Which approach do you want?
