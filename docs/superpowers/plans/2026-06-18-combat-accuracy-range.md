# Combat Accuracy & Range Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add four-band attack range (Melee 1 · Short 3 · Medium 5 · Long 8) and hybrid accuracy (full hit / graze 33% / clean miss) to the deterministic combat sim, with log + presentation hooks.

**Architecture:** Extend `CombatRange` and `AttackRangeTier`; add pure-C# `CombatAccuracyResolver` (+ config/defaults/outcome types) called from `TickCombatRun.ResolveAttacks`; log `damage` / `graze` / `miss`; stub `AccuracyModifierCollector` for future tactics/abilities. TDD in `Assets/_Project/Core.Tests/EditMode/`.

**Tech Stack:** Unity 6, C# Core (`DeadManZone.Core`), NUnit EditMode, existing `Rng`

**Spec:** `docs/superpowers/specs/2026-06-18-combat-accuracy-range-design.md`

---

## File map

| File | Action |
|------|--------|
| `Assets/_Project/Core/Board/CombatStatEnums.cs` | Add `Melee` to `AttackRangeTier` |
| `Assets/_Project/Core/Combat/CombatRange.cs` | Four cell values |
| `Assets/_Project/Core/Combat/CombatAccuracyConfig.cs` | **Create** — tunable constants |
| `Assets/_Project/Core/Combat/CombatAccuracyDefaults.cs` | **Create** — base accuracy lookup |
| `Assets/_Project/Core/Combat/CombatAttackOutcome.cs` | **Create** — outcome kind + struct |
| `Assets/_Project/Core/Combat/CombatAccuracyResolver.cs` | **Create** — falloff, graze band, roll |
| `Assets/_Project/Core/Combat/AccuracyModifierCollector.cs` | **Create** — v1 stub (returns 0) |
| `Assets/_Project/Core/Board/PieceDefinition.cs` | Add `int? AccuracyOverride` |
| `Assets/_Project/Core/Combat/TickCombatRun.cs` | Wire resolver in `ResolveAttacks` |
| `Assets/_Project/Core/Combat/ArmyHealthReplayTracker.cs` | Handle `graze` |
| `Assets/_Project/Core/Combat/CombatLogFormatter.cs` | Format `graze` / `miss` |
| `Assets/_Project/Presentation/Combat/Arena/CombatArenaPresenter.cs` | Visual `graze` / `miss` |
| `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs` | `accuracyOverride` field + `ToCore()` |
| `Assets/_Project/Data/UnitCreation/UnitCreationDraft.cs` | Optional override |
| `Assets/_Project/Data/Editor/UnitCreatorFormSections.cs` | Inspector field |
| `Assets/_Project/Data/Editor/AttackRangeTierMigration.cs` | **Create** — one-time SO enum migration |
| `Assets/_Project/Core.Tests/EditMode/TestPieces.cs` | `accuracyOverride` on `With()` |
| `Assets/_Project/Core.Tests/EditMode/CombatRangeTests.cs` | Four-tier assertions |
| `Assets/_Project/Core.Tests/EditMode/CombatAccuracyResolverTests.cs` | **Create** |
| `Assets/_Project/Core.Tests/EditMode/CombatAccuracyIntegrationTests.cs` | **Create** |
| `Assets/_Project/Core.Tests/EditMode/CombatLogFormatterTests.cs` | **Create** or extend if exists |
| `Assets/_Project/Core.Tests/EditMode/RoleEngagementTests.cs` | Update Long=8 expectations |
| `Assets/_Project/Core.Tests/EditMode/CombatMovementRangeGateTests.cs` | Melee vs Short naming |
| Other tests referencing old tier semantics | Fix compile + assertions |

**Unity test command (filtered):**

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.0.XXf1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform editmode `
  -testFilter "DeadManZone.Core.Tests.EditMode.CombatAccuracyResolverTests" `
  -testResults "TestResults-EditMode.xml" -quit
```

Replace `6000.0.XXf1` with installed editor version from Unity Hub.

---

### Task 1: Four-tier `AttackRangeTier` + `CombatRange`

**Files:**
- Modify: `Assets/_Project/Core/Board/CombatStatEnums.cs`
- Modify: `Assets/_Project/Core/Combat/CombatRange.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/CombatRangeTests.cs`

- [ ] **Step 1: Write failing range tests**

Add to `CombatRangeTests.cs`:

```csharp
[Test]
public void GetRangeCells_MeleeShortMediumLong()
{
    Assert.AreEqual(1, CombatRange.GetRangeCells(AttackRangeTier.Melee));
    Assert.AreEqual(3, CombatRange.GetRangeCells(AttackRangeTier.Short));
    Assert.AreEqual(5, CombatRange.GetRangeCells(AttackRangeTier.Medium));
    Assert.AreEqual(8, CombatRange.GetRangeCells(AttackRangeTier.Long));
}

[Test]
public void IsInRange_RespectsMeleeVersusShort()
{
    var from = new GridCoord(0, 0);
    Assert.IsTrue(CombatRange.IsInRange(from, new GridCoord(1, 0), AttackRangeTier.Melee));
    Assert.IsFalse(CombatRange.IsInRange(from, new GridCoord(2, 0), AttackRangeTier.Melee));
    Assert.IsTrue(CombatRange.IsInRange(from, new GridCoord(2, 0), AttackRangeTier.Short));
}
```

Update existing `SelectTarget_SkipsOutOfRangeEnemies` to use `AttackRangeTier.Melee` instead of `Short` for 1-cell range.

- [ ] **Step 2: Run tests — expect FAIL** (enum missing `Melee`, wrong cell counts)

- [ ] **Step 3: Implement enum + range table**

`CombatStatEnums.cs`:

```csharp
public enum AttackRangeTier
{
    Melee,
    Short,
    Medium,
    Long
}
```

`CombatRange.cs`:

```csharp
public static int GetRangeCells(AttackRangeTier tier) => tier switch
{
    AttackRangeTier.Melee => 1,
    AttackRangeTier.Short => 3,
    AttackRangeTier.Medium => 5,
    AttackRangeTier.Long => 8,
    _ => 5
};
```

- [ ] **Step 4: Run `CombatRangeTests` — PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Core/Board/CombatStatEnums.cs Assets/_Project/Core/Combat/CombatRange.cs Assets/_Project/Core.Tests/EditMode/CombatRangeTests.cs
git commit -m "feat(combat): add four-band attack range tiers"
```

---

### Task 2: Accuracy config + defaults

**Files:**
- Create: `Assets/_Project/Core/Combat/CombatAccuracyConfig.cs`
- Create: `Assets/_Project/Core/Combat/CombatAccuracyDefaults.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/CombatAccuracyDefaultsTests.cs`

- [ ] **Step 1: Write failing defaults tests**

```csharp
[Test]
public void BallisticInfantry_DefaultIs78()
{
    var def = TestPieces.With(TestPieces.RifleSquad(), attackType: AttackType.Ballistic);
    Assert.AreEqual(78, CombatAccuracyDefaults.GetBaseAccuracy(def));
}

[Test]
public void SniperRole_OverridesBallisticDefault()
{
    var def = TestPieces.CreateUnit("s", combatRole: GameTagIds.Sniper, primary: GameTagIds.Infantry);
    def = TestPieces.With(def, attackType: AttackType.Ballistic);
    Assert.AreEqual(88, CombatAccuracyDefaults.GetBaseAccuracy(def));
}

[Test]
public void AccuracyOverride_WinsOverTable()
{
    var def = new PieceDefinition
    {
        Id = "x", DisplayName = "x", Shape = TestPieces.RifleSquad().Shape,
        AttackType = AttackType.Ballistic, AccuracyOverride = 95
    };
    Assert.AreEqual(95, CombatAccuracyDefaults.GetBaseAccuracy(def));
}
```

- [ ] **Step 2: Run — FAIL** (types missing)

- [ ] **Step 3: Implement**

`CombatAccuracyConfig.cs`:

```csharp
namespace DeadManZone.Core.Combat
{
    public static class CombatAccuracyConfig
    {
        public const float InnerRangeFraction = 0.6f;
        public const float AccuracyFloorFraction = 0.5f;
        public const int AbsoluteAccuracyFloor = 40;
        public const int GrazeBandBaseline = 12;
        public const int GrazeBandAtPointBlank = 2;
        public const float GrazeBandMaxMultiplier = 2f;
        public const float GrazeDamageFraction = 0.33f;
    }
}
```

`CombatAccuracyDefaults.cs`:

```csharp
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public static class CombatAccuracyDefaults
    {
        public static int GetBaseAccuracy(PieceDefinition piece)
        {
            if (piece == null)
                return 78;

            if (piece.AccuracyOverride.HasValue)
                return Clamp(piece.AccuracyOverride.Value);

            int fromType = piece.AttackType switch
            {
                AttackType.Melee => 92,
                AttackType.Piercing => 80,
                AttackType.Explosive => 72,
                AttackType.Shredding => 68,
                AttackType.Gas => 75,
                _ => 78
            };

            if (piece.CombatRole == GameTagIds.Sniper)
                return 88;
            if (piece.CombatRole == GameTagIds.Artillery)
                return System.Math.Max(fromType, 72);

            return fromType;
        }

        private static int Clamp(int value) =>
            System.Math.Clamp(value, 0, 100);
    }
}
```

Add to `PieceDefinition.cs`:

```csharp
public int? AccuracyOverride { get; init; }
```

- [ ] **Step 4: Run defaults tests — PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(combat): add accuracy config and default lookup"
```

---

### Task 3: `CombatAccuracyResolver` (TDD core)

**Files:**
- Create: `Assets/_Project/Core/Combat/CombatAttackOutcome.cs`
- Create: `Assets/_Project/Core/Combat/CombatAccuracyResolver.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/CombatAccuracyResolverTests.cs`

- [ ] **Step 1: Write failing resolver tests**

```csharp
[Test]
public void InnerRange_KeepsFullAccuracy()
{
    // base 80, max range 8, distance 4 (50% — inside inner 60%)
    int effective = CombatAccuracyResolver.GetEffectiveAccuracy(80, distance: 4, maxRange: 8);
    Assert.AreEqual(80, effective);
}

[Test]
public void MaxRange_ReducesAccuracyTowardFloor()
{
    int effective = CombatAccuracyResolver.GetEffectiveAccuracy(80, distance: 8, maxRange: 8);
    Assert.AreEqual(40, effective); // max(40, round(80*0.5))
}

[Test]
public void GrazeBand_WiderAtMaxRange()
{
    int near = CombatAccuracyResolver.GetGrazeBand(distance: 1, maxRange: 8);
    int far = CombatAccuracyResolver.GetGrazeBand(distance: 8, maxRange: 8);
    Assert.AreEqual(2, near);
    Assert.AreEqual(24, far);
}

[Test]
public void ResolveOutcome_HitGrazeMiss()
{
    int full = 30;
    var hit = CombatAccuracyResolver.ResolveOutcome(80, grazeBand: 10, roll: 50, fullDamage: full);
    var graze = CombatAccuracyResolver.ResolveOutcome(80, grazeBand: 10, roll: 85, fullDamage: full);
    var miss = CombatAccuracyResolver.ResolveOutcome(80, grazeBand: 10, roll: 96, fullDamage: full);

    Assert.AreEqual(CombatAttackOutcomeKind.Hit, hit.Kind);
    Assert.AreEqual(30, hit.Damage);
    Assert.AreEqual(CombatAttackOutcomeKind.Graze, graze.Kind);
    Assert.AreEqual(10, graze.Damage); // max(1, round(30*0.33))
    Assert.AreEqual(CombatAttackOutcomeKind.Miss, miss.Kind);
    Assert.AreEqual(0, miss.Damage);
}
```

- [ ] **Step 2: Run — FAIL**

- [ ] **Step 3: Implement**

`CombatAttackOutcome.cs`:

```csharp
namespace DeadManZone.Core.Combat
{
    public enum CombatAttackOutcomeKind { Hit, Graze, Miss }

    public sealed class CombatAttackOutcome
    {
        public CombatAttackOutcomeKind Kind { get; init; }
        public int Damage { get; init; }
        public int Roll { get; init; }
        public int EffectiveAccuracy { get; init; }
        public int GrazeBand { get; init; }
    }
}
```

`CombatAccuracyResolver.cs` (key methods):

```csharp
public static int GetEffectiveAccuracy(int baseAccuracy, int distance, int maxRange)
{
    if (maxRange <= 0)
        return System.Math.Clamp(baseAccuracy, 0, 100);

    float innerEdge = CombatAccuracyConfig.InnerRangeFraction * maxRange;
    if (distance <= innerEdge)
        return System.Math.Clamp(baseAccuracy, 0, 100);

    int floor = System.Math.Max(
        CombatAccuracyConfig.AbsoluteAccuracyFloor,
        (int)System.Math.Round(baseAccuracy * CombatAccuracyConfig.AccuracyFloorFraction));

    float t = (distance - innerEdge) / System.Math.Max(1f, maxRange - innerEdge);
    int effective = baseAccuracy - (int)System.Math.Round((baseAccuracy - floor) * t);
    return System.Math.Clamp(effective, 0, 100);
}

public static int GetGrazeBand(int distance, int maxRange)
{
    if (maxRange <= 1)
        return CombatAccuracyConfig.GrazeBandAtPointBlank;

    float t = (distance - 1f) / (maxRange - 1f);
    t = System.Math.Clamp(t, 0f, 1f);
    float band = CombatAccuracyConfig.GrazeBandAtPointBlank
        + t * (CombatAccuracyConfig.GrazeBandBaseline * CombatAccuracyConfig.GrazeBandMaxMultiplier
               - CombatAccuracyConfig.GrazeBandAtPointBlank);
    return (int)System.Math.Round(band);
}

public static CombatAttackOutcome ResolveOutcome(int effectiveAccuracy, int grazeBand, int roll, int fullDamage)
{
    if (roll <= effectiveAccuracy)
        return Outcome(CombatAttackOutcomeKind.Hit, fullDamage, roll, effectiveAccuracy, grazeBand);

    if (roll <= effectiveAccuracy + grazeBand)
    {
        int grazeDamage = System.Math.Max(1, (int)System.Math.Round(
            fullDamage * CombatAccuracyConfig.GrazeDamageFraction));
        return Outcome(CombatAttackOutcomeKind.Graze, grazeDamage, roll, effectiveAccuracy, grazeBand);
    }

    return Outcome(CombatAttackOutcomeKind.Miss, 0, roll, effectiveAccuracy, grazeBand);
}

public static CombatAttackOutcome Resolve(
    Rng rng,
    PieceDefinition attacker,
    PieceDefinition defender,
    int distance,
    int accuracyModifier,
    int flatDamageBonus,
    int defenderArmorBuffSteps)
{
    int maxRange = CombatRange.GetRangeCells(attacker.AttackRange);
    int baseAccuracy = CombatAccuracyDefaults.GetBaseAccuracy(attacker) + accuracyModifier;
    int effective = GetEffectiveAccuracy(baseAccuracy, distance, maxRange);
    int grazeBand = GetGrazeBand(distance, maxRange);
    int roll = rng.NextInt(1, 101);
    int fullDamage = CombatDamageResolver.ComputeDamage(
        attacker, defender, 1f, defenderArmorBuffSteps, flatDamageBonus);
    return ResolveOutcome(effective, grazeBand, roll, fullDamage);
}
```

- [ ] **Step 4: Run `CombatAccuracyResolverTests` — PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(combat): add accuracy resolver with hit graze miss"
```

---

### Task 4: Modifier collector stub

**Files:**
- Create: `Assets/_Project/Core/Combat/AccuracyModifierCollector.cs`

- [ ] **Step 1: Add stub**

```csharp
namespace DeadManZone.Core.Combat
{
    public static class AccuracyModifierCollector
    {
        // ponytail: v1 returns 0; tactics/abilities plug in here later
        public static int Collect(
            CombatantState attacker,
            CombatantState target,
            TacticType tactic) => 0;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git commit -m "feat(combat): stub accuracy modifier collector"
```

---

### Task 5: Wire `TickCombatRun.ResolveAttacks`

**Files:**
- Modify: `Assets/_Project/Core/Combat/TickCombatRun.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/CombatAccuracyIntegrationTests.cs`

- [ ] **Step 1: Write failing integration test**

Use minimal 1v1 board fixture (copy pattern from existing `TickCombatRun` tests). Seed chosen so log contains at least one `miss` or `graze` at Long range over 30 ticks:

```csharp
[Test]
public void ResolveAttacks_LogsGrazeOrMiss_AtLongRange()
{
    // player rifle at (0,5) vs enemy at (8,5) — Long range edge
    // seed 4242 — verify log ActionTypes include graze or miss (not only damage)
    var log = RunFight(seed: 4242, playerRange: AttackRangeTier.Long, enemyAtX: 8);
    Assert.IsTrue(log.Events.Exists(e => e.ActionType == "graze" || e.ActionType == "miss"));
}
```

Tune seed/enemy distance while implementing if 4242 doesn't produce mixed outcomes.

- [ ] **Step 2: Run — FAIL** (no graze/miss events yet)

- [ ] **Step 3: Replace damage block in `ResolveAttacks`**

```csharp
int distance = CombatRange.Manhattan(actor.AnchorPosition, target.AnchorPosition);
int accuracyMod = AccuracyModifierCollector.Collect(actor, target, tactic);
var outcome = CombatAccuracyResolver.Resolve(
    _rng,
    actor.Definition,
    target.Definition,
    distance,
    accuracyMod,
    actor.DamageBonus + damageBuff,
    target.ArmorBuffSteps);

string actionType = outcome.Kind switch
{
    CombatAttackOutcomeKind.Hit => "damage",
    CombatAttackOutcomeKind.Graze => "graze",
    _ => "miss"
};

_log.Append(segment, GlobalTick, actor.InstanceId, actionType, target.InstanceId, outcome.Damage);

if (outcome.Damage > 0)
{
    target.CurrentHp -= outcome.Damage;
    actor.DamageDealtThisFight += outcome.Damage;
    target.DamageTakenThisFight += outcome.Damage;
    if (!target.IsAlive)
        LogDestroyed(segment, target.InstanceId, actor.InstanceId);
}

actor.CooldownRemaining = CombatAttackSpeed.GetEffectiveCooldown(
    actor.Definition.CooldownTicks,
    actor.Definition.AttackSpeed);
```

Note: cooldown **always** set — hit, graze, and miss.

- [ ] **Step 4: Run integration test — PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(combat): apply accuracy rolls in tick combat"
```

---

### Task 6: Event log consumers

**Files:**
- Modify: `Assets/_Project/Core/Combat/ArmyHealthReplayTracker.cs`
- Modify: `Assets/_Project/Core/Combat/CombatLogFormatter.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/CombatLogFormatterTests.cs`

- [ ] **Step 1: Write formatter tests**

```csharp
[Test]
public void Format_Graze_IncludesGrazeLabel()
{
    var line = CombatLogFormatter.Format(new CombatEvent
    {
        Segment = 0, Tick = 1, ActorId = "a", ActionType = "graze", TargetId = "b", Value = 7
    });
    StringAssert.Contains("graze", line);
    StringAssert.Contains("7", line);
}

[Test]
public void Format_Miss_ShowsMissed()
{
    var line = CombatLogFormatter.Format(new CombatEvent
    {
        Segment = 0, Tick = 1, ActorId = "a", ActionType = "miss", TargetId = "b", Value = 0
    });
    StringAssert.Contains("miss", line, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 2: Run — FAIL**

- [ ] **Step 3: Implement**

`ArmyHealthReplayTracker` — add `case "graze":` alongside `"damage"`.

`CombatLogFormatter` — add cases:

```csharp
"graze" =>
    $"[{phase} t{tick}] {Label(combatEvent.ActorId)} → {Label(combatEvent.TargetId)}: {combatEvent.Value} graze",
"miss" =>
    $"[{phase} t{tick}] {Label(combatEvent.ActorId)} → {Label(combatEvent.TargetId)}: missed",
```

- [ ] **Step 4: Run formatter tests — PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(combat): format graze and miss combat log events"
```

---

### Task 7: Presentation hooks

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaPresenter.cs`

- [ ] **Step 1: Extend `ApplyEventVisual` switch**

```csharp
case "graze":
    PlayDamageEvent(combatEvent, grazeImpactScale: 0.6f);
    break;
case "miss":
    PlayMissEvent(combatEvent);
    break;
```

- [ ] **Step 2: Add `PlayMissEvent`**

Muzzle/tracer from attacker toward target world position; **skip** impact callback/VFX. Reuse `PlayAttackMuzzleVfx` path from `PlayDamageEvent` without calling impact.

- [ ] **Step 3: Optional `grazeImpactScale` on impact VFX** — scale particle size or pass smaller damage to existing impact (minimal change).

- [ ] **Step 4: Manual smoke in Play mode** — long-range fight shows tracers without impact on miss.

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(combat): present graze and miss in arena replay"
```

---

### Task 8: Data layer + Unit Creator

**Files:**
- Modify: `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs`
- Modify: `Assets/_Project/Data/UnitCreation/UnitCreationDraft.cs`
- Modify: `Assets/_Project/Data/Editor/UnitCreatorFormSections.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/TestPieces.cs`

- [ ] **Step 1: Add fields**

`PieceDefinitionSO`:

```csharp
[Tooltip("Optional 0-100. Leave at 0 to use attack type / role defaults.")]
public int accuracyOverride;
```

In `ToCore()`: `AccuracyOverride = accuracyOverride <= 0 ? null : accuracyOverride`

`TestPieces.With(...)` add `int? accuracyOverride = null` parameter.

- [ ] **Step 2: Unit Creator** — optional int field under Combat Stats.

- [ ] **Step 3: Commit**

```bash
git commit -m "feat(data): optional accuracy override on piece definitions"
```

---

### Task 9: Content migration (AttackRange enum reorder)

**Files:**
- Create: `Assets/_Project/Data/Editor/AttackRangeTierMigration.cs`
- Modify: `Assets/_Project/Data/Editor/DemoPieceFactory.cs` (manual Long bumps where needed)

- [ ] **Step 1: Editor menu** `DeadManZone/Migrate Attack Range Tiers`

For each `PieceDefinitionSO` in project:

| Serialized old index | Old name | New enum value |
|--------------------|----------|----------------|
| 0 | Short | Melee |
| 1 | Medium | Short |
| 2 | Long | Medium |

Then second pass: if `combatRole` is `sniper` or `artillery`, set `attackRange = Long`.

- [ ] **Step 2: Run menu item in Unity** — verify demo pieces in Inspector.

- [ ] **Step 3: Update `DemoPieceFactory` hardcoded tiers** — artillery/sniper → `Long`; former Short(1) → `Melee`; rifle squads → `Short`.

- [ ] **Step 4: Commit migrated assets + script**

```bash
git commit -m "chore(content): migrate attack range tiers to four-band model"
```

---

### Task 10: Fix downstream tests

**Files:**
- Modify: `RoleEngagementTests.cs` — Artillery goal: enemy X=12, Long=8 → `goalX = min(12-8, 7) = 4` (was 6 with range 6)
- Modify: `CombatMovementRangeGateTests.cs` — rename `AttackRangeTier.Short` (1 cell) → `Melee`
- Modify: `CombatRoleTargetingTests.cs`, `PieceDefinitionCombatStatsTests.cs`, any compile errors from enum insert

- [ ] **Step 1: Fix compile errors** (enum value shift breaks serialized tests using implicit ordinals — tests use names, OK)

- [ ] **Step 2: Update assertions** to new cell math

- [ ] **Step 3: Run full EditMode suite**

```powershell
& "<UnityEditorPath>\Unity.exe" -batchmode -nographics `
  -projectPath "<repo-root>" `
  -runTests -testPlatform editmode `
  -testResults "TestResults-EditMode.xml" -quit
```

Expected: all green

- [ ] **Step 4: Commit**

```bash
git commit -m "test(combat): update range and accuracy test fixtures"
```

---

### Task 11: GDD appendix (optional doc-only)

**Files:**
- Modify: `docs/DeadManZone-Game-Design-Document.md` — Appendix B range table + short accuracy section

- [ ] **Step 1: Update range table to Melee 1 / Short 3 / Medium 5 / Long 8**

- [ ] **Step 2: Add accuracy subsection** referencing hit/graze/miss

- [ ] **Step 3: Commit**

```bash
git commit -m "docs: document four-band range and accuracy outcomes"
```

---

## Spec coverage self-review

| Spec requirement | Task |
|------------------|------|
| Four range tiers | Task 1 |
| Enum migration | Task 9 |
| Hybrid accuracy defaults + override | Task 2 |
| Falloff + graze band math | Task 3 |
| Hit / graze 33% / miss + cooldown | Task 3, 5 |
| Modifier stub | Task 4 |
| Event log types | Task 5, 6 |
| Presentation | Task 7 |
| Deterministic Rng | Task 3, 5 |
| EditMode tests | Tasks 1–3, 5–6, 10 |
| Abilities unchanged | No task (explicit out of scope) |
| GDD update | Task 11 |

No placeholders remain. Type names consistent across tasks.

---

## Verification gate

Before marking complete:

1. Full EditMode test run green
2. `ReadLints` clean on all edited C# files
3. Manual Play mode: sandbox fight shows mix of hits, grazes, misses at long range
4. Migration run on demo content — artillery at Long 8, riflemen at Short 3
