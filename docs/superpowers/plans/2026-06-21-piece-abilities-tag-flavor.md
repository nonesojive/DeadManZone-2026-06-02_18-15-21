# Piece Abilities & Tag Flavor Split — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make synergy/ability/flavor tags display-only (Critical Mass counting unchanged) and drive adjacency/fight-start combat effects only through explicit per-piece abilities (hybrid catalog + inline one-offs).

**Architecture:** Add Unity-free `PieceAbilityDefinition` + `PieceAbilityEngine` in Core (mirrors today's `SynergyEngine` snapshot shape for minimal combat/UI churn). Data layer gets `AbilityDefinitionSO` catalog and `PieceAbilityInlineEntry` on `PieceDefinitionSO`. `SynergyRuleCatalog` tag lookup is removed after tests and content migration. `GrantedAbility` / `CommandActions` stay untouched in Phase 1.

**Tech Stack:** Unity 6 (6000.3.x), C#, Edit Mode tests (`DeadManZone.Core.Tests`, `DeadManZone.Presentation.Tests`), ScriptableObjects under `Assets/_Project/Data/`.

**Spec reference:** `docs/superpowers/specs/2026-06-21-piece-abilities-tag-flavor-design.md`

**Branch:** `critmass+synergyworkv1` (or `piece-abilities-v1` worktree)

---

## File map

| Path | Responsibility |
|------|----------------|
| `Assets/_Project/Core/Tags/PieceAbilityTrigger.cs` | `AdjacentAura`, `FightStart` (Phase 1); reserve `Pause`, `Command` for Phase 2 |
| `Assets/_Project/Core/Tags/PieceAbilityDefinition.cs` | Pure ability row: id, description, trigger, neighbor filter, stat, mod, magnitude |
| `Assets/_Project/Core/Combat/PieceAbilityEngine.cs` | Evaluate fight-start auras from piece ability lists; apply to combatants |
| `Assets/_Project/Core/Board/PieceDefinition.cs` | Add `Abilities` list resolved at build time |
| `Assets/_Project/Data/ScriptableObjects/AbilityDefinitionSO.cs` | Catalog ability asset |
| `Assets/_Project/Data/ScriptableObjects/PieceAbilityInlineEntry.cs` | Serializable inline ability (same fields as core definition) |
| `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs` | `catalogAbilities[]`, `customAbilities[]`; merge in `ToCore()` |
| `Assets/_Project/Data/Editor/PieceAbilityCatalogGenerator.cs` | Menu: seed catalog from old `SynergyRuleCatalog` + trait registry copy |
| `Assets/_Project/Data/Editor/PieceAbilityContentMigrator.cs` | Menu: assign catalog abilities to pieces that had implicit tag effects |
| `Assets/_Project/Core.Tests/EditMode/PieceAbilityEngineTests.cs` | Port `SynergyEngineTests` scenarios using abilities, not synergy tags |
| `Assets/_Project/Core.Tests/EditMode/TestPieces.cs` | `WithAbilities(...)`, `CreateUnit(..., abilities: ...)` helpers |
| `Assets/_Project/Core/Tags/PieceCardViewModelBuilder.cs` | Ability lines from piece abilities; remove tag-implied synergy bonus display |
| `Assets/_Project/Core/Tags/PieceCardTooltipFormatter.cs` | `BuildAbilityLines(IReadOnlyList<PieceAbilityDefinition>)` |
| `Assets/_Project/Core/Tags/BuffStripEvaluator.cs` | Show active **ability** auras on buff strip, not synergy tag ids |
| `Assets/_Project/Presentation/Board/BoardSynergyOverlay.cs` | Link lines from ability snapshot (`SourceAbilityId`) |
| **Delete after migration:** `Assets/_Project/Core/Tags/SynergyRuleCatalog.cs` | Tag-driven rules replaced by catalog |
| **Replace call sites:** all `SynergyEngine` usages | → `PieceAbilityEngine` (same snapshot type names initially) |

---

## Phase 1 — Core ability model

### Task 1: Ability definition types

**Files:**
- Create: `Assets/_Project/Core/Tags/PieceAbilityTrigger.cs`
- Create: `Assets/_Project/Core/Tags/PieceAbilityDefinition.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/PieceAbilityDefinitionTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class PieceAbilityDefinitionTests
    {
        [Test]
        public void Definition_StoresIdAndDescription()
        {
            var def = new PieceAbilityDefinition
            {
                Id = "adjacent_infantry_armor_plus_one",
                CardDescription = "Adjacent infantry gain +1 armor.",
                Trigger = PieceAbilityTrigger.AdjacentAura,
                NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
                Stat = SynergyStat.ArmorType,
                ModType = SynergyModType.Flat,
                Magnitude = 1
            };

            Assert.AreEqual("adjacent_infantry_armor_plus_one", def.Id);
            Assert.AreEqual(PieceAbilityTrigger.AdjacentAura, def.Trigger);
            Assert.AreEqual(1, def.Magnitude);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run (filtered Edit Mode):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform editmode `
  -testFilter "DeadManZone.Core.Tests.EditMode.PieceAbilityDefinitionTests" `
  -testResults "TestResults-PieceAbility.xml" -quit
```

Expected: FAIL — types not found

- [ ] **Step 3: Write minimal implementation**

`PieceAbilityTrigger.cs`:

```csharp
namespace DeadManZone.Core.Tags
{
    public enum PieceAbilityTrigger
    {
        AdjacentAura = 0,
        FightStart = 1
        // Pause, Command — Phase 2
    }
}
```

`PieceAbilityDefinition.cs`:

```csharp
namespace DeadManZone.Core.Tags
{
    public readonly struct PieceAbilityDefinition
    {
        public string Id { get; init; }
        public string CardDescription { get; init; }
        public PieceAbilityTrigger Trigger { get; init; }
        public NeighborFilter NeighborFilter { get; init; }
        public SynergyStat Stat { get; init; }
        public SynergyModType ModType { get; init; }
        public int Magnitude { get; init; }
    }
}
```

- [ ] **Step 4: Run test — PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Core/Tags/PieceAbilityTrigger.cs Assets/_Project/Core/Tags/PieceAbilityDefinition.cs Assets/_Project/Core.Tests/EditMode/PieceAbilityDefinitionTests.cs
git commit -m "feat(tags): add core piece ability definition types"
```

---

### Task 2: PieceDefinition carries resolved abilities

**Files:**
- Modify: `Assets/_Project/Core/Board/PieceDefinition.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/TestPieces.cs`
- Test: `Assets/_Project/Core.Tests/EditMode/PieceDefinitionAbilityTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Test]
public void PieceDefinition_ExposesAbilitiesList()
{
    var abilities = new[]
    {
        new PieceAbilityDefinition
        {
            Id = "inspiring_move",
            Trigger = PieceAbilityTrigger.AdjacentAura,
            Stat = SynergyStat.MoveChargePercent,
            ModType = SynergyModType.Flat,
            Magnitude = 5
        }
    };
    var piece = TestPieces.With(TestPieces.RifleSquad(), abilities: abilities);
    Assert.AreEqual(1, piece.Abilities.Count);
    Assert.AreEqual("inspiring_move", piece.Abilities[0].Id);
}
```

- [ ] **Step 2: Run — FAIL** (`Abilities` missing)

- [ ] **Step 3: Implement**

Add to `PieceDefinition.cs`:

```csharp
public IReadOnlyList<PieceAbilityDefinition> Abilities { get; init; }
    = Array.Empty<PieceAbilityDefinition>();
```

Add `TestPieces.With(..., IReadOnlyList<PieceAbilityDefinition> abilities = null)` copying abilities onto definition.

- [ ] **Step 4: Run — PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(board): add Abilities list to PieceDefinition"
```

---

### Task 3: PieceAbilityEngine (TDD port of SynergyEngine)

**Files:**
- Create: `Assets/_Project/Core/Combat/PieceAbilityEngine.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/PieceAbilityEngineTests.cs`
- Reference: `Assets/_Project/Core/Combat/SynergyEngine.cs` (copy adjacency loop; source = piece abilities not synergy tags)

- [ ] **Step 1: Write failing tests** (port all cases from `SynergyEngineTests.cs`)

Key behavior changes vs old tests:

| Old test | New setup |
|----------|-----------|
| `MedicAdjacentInfantry_GrantsArmorBuff` | Medic **tag only** → **zero** bonus. Same test with `abilities: [medicArmorAura]` → +1 armor step |
| `CommandAdjacentArtillery_GrantsDamageBonus` | Ability on command piece, not Command synergy tag |
| `InspiringAdjacentAny_GrantsMoveCharge` | Ability on source, magnitude 5 move charge |
| `PieceWithNoSynergyTags_ProducesZeroBonuses` | Piece with tags but **no abilities** → zero |

Example new test:

```csharp
[Test]
public void MedicTagAlone_DoesNotGrantArmorBuff()
{
    var medic = TestPieces.CreateUnit("medic", synergyTags: new[] { GameTagIds.Medic });
    var infantry = TestPieces.CreateUnit("infantry", primary: GameTagIds.Infantry);
    var board = CreateAdjacentBoard(medic, "medic_1", infantry, "infantry_1");

    var snapshot = PieceAbilityEngine.EvaluateFightStart(board);
    Assert.IsTrue(snapshot.TryGet("infantry_1", out var result));
    Assert.AreEqual(0, result.ArmorBuffSteps);
}

[Test]
public void MedicArmorAbility_AdjacentInfantry_GrantsArmorBuff()
{
    var aura = new PieceAbilityDefinition
    {
        Id = "adjacent_infantry_armor_plus_one",
        Trigger = PieceAbilityTrigger.AdjacentAura,
        NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
        Stat = SynergyStat.ArmorType,
        ModType = SynergyModType.Flat,
        Magnitude = 1
    };
    var medic = TestPieces.With(
        TestPieces.CreateUnit("medic", synergyTags: new[] { GameTagIds.Medic }),
        abilities: new[] { aura });
    // ... assert ArmorBuffSteps == 1
}
```

- [ ] **Step 2: Run — FAIL**

- [ ] **Step 3: Implement `PieceAbilityEngine`**

Reuse snapshot structs from `SynergyEngine` initially (same `SynergyResult`, `SynergyLink` with `SourceAbilityId` replacing `SourceTagId`, or add property alias). Loop:

```csharp
foreach (var source in board.Pieces)
{
    foreach (var ability in source.Definition.Abilities)
    {
        if (ability.Trigger != PieceAbilityTrigger.AdjacentAura)
            continue;
        // same adjacency + NeighborFilter + stat apply as SynergyEngine.ApplyRule
    }
}
```

Extract shared adjacency helper from `SynergyEngine` if needed (single file `BoardAdjacency.cs` or private duplicate — ponytail: duplicate first, extract if >1 caller).

- [ ] **Step 4: Run `PieceAbilityEngineTests` — PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(combat): add PieceAbilityEngine for per-piece auras"
```

---

### Task 4: Wire combat to PieceAbilityEngine

**Files:**
- Modify: `Assets/_Project/Core/Combat/TickCombatRun.cs`
- Modify: `Assets/_Project/Presentation/Board/BoardView.cs`
- Modify: `Assets/_Project/Core/Combat/ArmyStrengthCalculator.cs`
- Modify: `Assets/_Project/Presentation/Board/BoardSynergyOverlay.cs`
- Modify: `Assets/_Project/Core/Tags/BuffStripEvaluator.cs`

- [ ] **Step 1: Write regression test** in `MechanicsSandboxChecklistTests.cs` or new `CombatIntegrationAbilityTests.cs` asserting fight start uses abilities path.

- [ ] **Step 2: Replace `SynergyEngine` calls with `PieceAbilityEngine`** at all grep sites (keep `ApplyToCombatants` signature).

- [ ] **Step 3: Run full Edit Mode suite**

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform editmode `
  -testResults "TestResults-EditMode.xml" -quit
```

Expected: failures until content migration (Task 7) — **temporarily** keep `SynergyEngine` delegating to ability engine OR migrate test pieces in same task.

- [ ] **Step 4: Delete `SynergyEngine.cs` and `SynergyRuleCatalog.cs`** once all tests green.

- [ ] **Step 5: Commit**

```bash
git commit -m "refactor(combat): replace SynergyEngine with PieceAbilityEngine"
```

---

## Phase 2 — Data layer & catalog

### Task 5: AbilityDefinitionSO + piece authoring fields

**Files:**
- Create: `Assets/_Project/Data/ScriptableObjects/AbilityDefinitionSO.cs`
- Create: `Assets/_Project/Data/ScriptableObjects/PieceAbilityInlineEntry.cs`
- Modify: `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs`
- Create: `Assets/_Project/Data/Resources/DeadManZone/Abilities/` folder

- [ ] **Step 1: Write Edit Mode test** `AbilityDefinitionSOTests.cs` (Data.Tests if exists, else load in generator test only)

- [ ] **Step 2: Implement SO + inline entry**

`AbilityDefinitionSO`:

```csharp
[CreateAssetMenu(menuName = "DeadManZone/Ability Definition")]
public sealed class AbilityDefinitionSO : ScriptableObject
{
    public string id;
    [TextArea] public string cardDescription;
    public PieceAbilityTrigger trigger;
    public NeighborFilter neighborFilter;
    public SynergyStat stat;
    public SynergyModType modType;
    public int magnitude;

    public PieceAbilityDefinition ToCore() => new()
    {
        Id = id,
        CardDescription = cardDescription,
        Trigger = trigger,
        NeighborFilter = neighborFilter,
        Stat = stat,
        ModType = modType,
        Magnitude = magnitude
    };
}
```

`PieceDefinitionSO` additions:

```csharp
[Header("Abilities")]
public AbilityDefinitionSO[] catalogAbilities = Array.Empty<AbilityDefinitionSO>();
public PieceAbilityInlineEntry[] customAbilities = Array.Empty<PieceAbilityInlineEntry>();
```

Update `ToCore()`:

```csharp
Abilities = ResolveAbilities(),

// private method merges catalog + inline ToCore()
```

- [ ] **Step 3: Commit**

```bash
git commit -m "feat(data): ability catalog SO and piece ability authoring"
```

---

### Task 6: Default ability catalog generator

**Files:**
- Create: `Assets/_Project/Data/Editor/PieceAbilityCatalogGenerator.cs`
- Output: `Assets/_Project/Data/Resources/DeadManZone/Abilities/*.asset`

- [ ] **Step 1: Implement menu `DeadManZone/Generate Piece Ability Catalog`**

Seed at minimum (from current `SynergyRuleCatalog`):

| Asset id | Maps from |
|----------|-----------|
| `adjacent_infantry_armor_plus_one` | Medic → Infantry armor +1 |
| `adjacent_artillery_damage_plus_two` | Command → Artillery damage +2 |
| `adjacent_stealth_damage_plus_one` | Echo → Stealth ability tag neighbor |
| `adjacent_allies_move_charge_plus_five` | Inspiring → any neighbor move +5 |

Use `SynergyTraitRegistry` strings for `cardDescription` where helpful.

- [ ] **Step 2: Run menu in Unity; verify assets import**

- [ ] **Step 3: Commit generated assets + generator**

```bash
git commit -m "feat(data): generate default piece ability catalog"
```

---

### Task 7: Content migration — assign abilities to existing pieces

**Files:**
- Create: `Assets/_Project/Data/Editor/PieceAbilityContentMigrator.cs`

- [ ] **Step 1: Implement menu `DeadManZone/Migrate Piece Tag-Implied Abilities`**

Logic (suggest, don't auto-blind-assign without log):

```
For each PieceDefinitionSO:
  if has synergy tag Medic and no catalogAbilities → suggest adjacent_infantry_armor_plus_one
  if has Inspiring → suggest adjacent_allies_move_charge_plus_five
  if has Command → suggest adjacent_artillery_damage_plus_two
  ...
Write summary to console; optional [Apply] flag in menu
```

**ponytail:** v1 migrator auto-assigns for demo content only (`Assets/_Project/Data/.../Pieces/`), logs skipped legendaries.

- [ ] **Step 2: Run migrator; re-run Edit Mode tests — all green**

- [ ] **Step 3: Commit**

```bash
git commit -m "content: assign catalog abilities to demo pieces"
```

---

## Phase 3 — UI & presentation

### Task 8: Unit card abilities section

**Files:**
- Modify: `Assets/_Project/Core/Tags/PieceCardViewModelBuilder.cs`
- Modify: `Assets/_Project/Core/Tags/PieceCardViewModel.cs` (add `AbilityLines`)
- Modify: `Assets/_Project/Core/Tags/PieceCardTooltipFormatter.cs`
- Modify: `Assets/_Project/Presentation/UI/PieceCardView.Binding.cs` (if abilities UI slot exists)
- Test: `Assets/_Project/Core.Tests/EditMode/PieceCardViewModelBuilderTests.cs`

- [ ] **Step 1: Failing test**

```csharp
[Test]
public void Build_WithCatalogAbility_IncludesAbilityLine()
{
    var aura = new PieceAbilityDefinition
    {
        Id = "adjacent_allies_move_plus_one",
        CardDescription = "Adjacent allies gain +1 move step."
    };
    var piece = TestPieces.With(TestPieces.RifleSquad(), abilities: new[] { aura });
    var vm = PieceCardViewModelBuilder.Build(piece);
    Assert.That(vm.AbilityLines, Does.Contain("Adjacent allies gain +1 move step."));
}
```

- [ ] **Step 2: Implement `BuildAbilityLines`** — one line per ability `CardDescription`; append `GrantedAbility` text from existing formatter until Phase 2.

- [ ] **Step 3: Remove card display of tag-implied synergy bonuses** (`SynergyDamageBonus` etc. from tag engine — keep if showing **resolved aura on hovered piece** from snapshot).

- [ ] **Step 4: Tests PASS; commit**

```bash
git commit -m "feat(ui): unit card abilities section from piece ability list"
```

---

### Task 9: Buff strip & board overlay

**Files:**
- Modify: `Assets/_Project/Core/Tags/BuffStripEvaluator.cs`
- Modify: `Assets/_Project/Presentation/Board/BoardSynergyOverlay.cs`

- [ ] **Step 1: Update `AppendActiveSynergyTags`** → `AppendActiveAbilityAuras`: iterate ability links from snapshot, display ability id/description — **not** `piece.Definition.SynergyTags`.

- [ ] **Step 2: Overlay links use `SourceAbilityId`**

- [ ] **Step 3: Manual Play mode check** — hover card, buff strip, board links still readable.

- [ ] **Step 4: Commit**

```bash
git commit -m "feat(ui): buff strip and overlay driven by piece abilities"
```

---

## Phase 4 — Verification & cleanup

### Task 10: Full verification gate

- [ ] Run full Edit Mode suite (expect 440+ tests pass)
- [ ] Run filtered: `PieceAbilityEngineTests`, `CriticalMassEngineTests`, `PieceCardViewModelBuilderTests`
- [ ] `ReadLints` on all touched C# files
- [ ] Update spec status to **Implemented (Phase 1)** in design doc header
- [ ] Commit any lint fixes

```bash
git commit -m "chore: piece abilities phase 1 verification"
```

---

## Spec coverage checklist

| Spec section | Task |
|--------------|------|
| Tags flavor-only | Task 3 tests prove tag-alone gives zero; Task 4 removes SynergyRuleCatalog |
| Hybrid catalog + inline | Tasks 5–7 |
| Critical Mass unchanged | Task 10 regression on `CriticalMassEngineTests` |
| PieceAbilityEngine replaces SynergyEngine | Tasks 3–4 |
| Card abilities section | Task 8 |
| Board overlay from abilities | Task 9 |
| Editor tooling | Tasks 6–7 |
| Phase 2 GrantedAbility migration | **Out of scope** — documented in spec |

---

## Risks during execution

1. **Tests fail mid-migration** — keep `SynergyEngine` as one-line delegate to `PieceAbilityEngine` until Task 7 completes.
2. **Demo pieces behave differently** — migrator must assign abilities matching old tag rules for vertical slice content.
3. **Buff strip noise** — ability auras and CM entries may duplicate concepts; use ability display name distinct from CM tag id.

---

## Success criteria (from spec)

- [ ] Medic tag alone does not buff adjacent infantry
- [ ] Same piece with medic aura **ability** does buff
- [ ] Critical Mass still counts tags and applies tier bonuses
- [ ] Unit card lists explicit ability descriptions
- [ ] Edit Mode tests green
