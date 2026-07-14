> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

> **Superseded (2026-07-01):** Draw fight rewards via `FightRewardTable` removed. See `2026-07-01-build-hud-economy-design.md`.

# DeadManZone Combat & Units Demo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the [2026-06-04 combat & units demo spec](../specs/2026-06-04-deadmanzone-combat-units-demo-design.md): retimed combat (5s/30s/5s + gas-until-win), Tactics + 3 Abilities at pauses, core combat stat enums with RPS damage, permanent faction HQ, 8-piece weighted shop pool, and standard battle report.

**Architecture:** Extend `DeadManZone.Core` with focused modules (`CombatDamageResolver`, `TacticState`, `CombatAbilityExecutor`, `BattleReportBuilder`, `ShopPoolFilter`, `CombatPacingConfig`) while keeping `TickCombatRun` as orchestrator. Map enums through `PieceDefinitionSO` → `PieceDefinition`. Presentation catches up after Core Edit Mode tests pass.

**Tech Stack:** Unity 6 (`6000.3.8f1`), C#, Unity Test Framework (Edit Mode + Play Mode), existing asmdefs under `Assets/_Project/`.

**Spec reference:** `docs/superpowers/specs/2026-06-04-deadmanzone-combat-units-demo-design.md`

**Recommended workspace:** Feature branch off `master` (commit `e0b88ed` or later).

---

## File map (new & modified)

| Path | Responsibility |
|------|----------------|
| `Assets/_Project/Core/Board/CombatStatEnums.cs` | AttackSpeed, AttackRange, MovementSpeed, ArmorType, AttackType, GrantedAbility |
| `Assets/_Project/Core/Board/PieceDefinition.cs` | New combat stat fields + FactionId |
| `Assets/_Project/Core/Combat/CombatPacingConfig.cs` | Segment tick budgets (50/300/50, gas uncapped) |
| `Assets/_Project/Core/Combat/CombatDamageResolver.cs` | RPS-lite damage + armor buffs |
| `Assets/_Project/Core/Combat/TacticType.cs` | Replaces `StanceType` |
| `Assets/_Project/Core/Combat/TacticState.cs` | Replaces `StanceState` |
| `Assets/_Project/Core/Combat/TacticTargeting.cs` | Replaces `CombatTargeting` |
| `Assets/_Project/Core/Combat/CombatAbilityExecutor.cs` | Grenade Lob, Shield Allies, Cannon Blast |
| `Assets/_Project/Core/Combat/TacticPauseValidator.cs` | Tactic + ability + Authority validation |
| `Assets/_Project/Core/Combat/BattleReportBuilder.cs` | Top-3 dealt/taken aggregation |
| `Assets/_Project/Core/Combat/CombatantState.cs` | Damage tracking, armor buffs, effective cooldown |
| `Assets/_Project/Core/Combat/TickCombatRun.cs` | Pacing, tactics, abilities, damage resolver, gas-until-win |
| `Assets/_Project/Core/Combat/PhaseCommand.cs` | Tactic + ability command shape |
| `Assets/_Project/Core/Combat/CommandProcessor.cs` | Delegate to validator + ability executor |
| `Assets/_Project/Core/Shop/ShopPoolFilter.cs` | Fight-index faction weighting |
| `Assets/_Project/Core/Shop/ShopGenerator.cs` | Use ShopPoolFilter when rolling |
| `Assets/_Project/Core/Common/GameTags.cs` | Add `Command`, `Neutral`, `Vanguard` constants |
| `Assets/_Project/Core/Board/BoardState.cs` | Reject relocate/remove of HQ-tagged pieces |
| `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs` | Serialize new enums |
| `Assets/_Project/Data/ScriptableObjects/FactionSO.cs` | hqPieceId, hqSpawnAnchor, hqSpawnRotation |
| `Assets/_Project/Game/RunOrchestrator.cs` | HQ spawn on new run; sell/move guards |
| `Assets/_Project/Game/FightRewardTable.cs` | Draw reward (~50% supplies) |
| `Assets/_Project/Presentation/Combat/TacticPausePanel.cs` | New pause UI (or refactor PhaseCommandPanel) |
| `Assets/_Project/Presentation/Combat/BattleReportPresenter.cs` | Aftermath report screen |
| `Assets/_Project/Presentation/Board/BoardView.cs` | Disable drag for HQ instances |
| `Assets/_Project/Data/Resources/DeadManZone/Pieces/*.asset` | 5 new neutrals + rebalance 3 IV + hq |
| `Assets/_Project/Core.Tests/EditMode/*` | New/updated tests per task |

---

## Phase 1 — Combat stat enums & PieceDefinition

### Task 1: Combat stat enums

**Files:**
- Create: `Assets/_Project/Core/Board/CombatStatEnums.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/CombatStatEnumsTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using DeadManZone.Core.Board;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatStatEnumsTests
    {
        [Test]
        public void GrantedAbility_IncludesDemoAbilities()
        {
            Assert.AreEqual(0, (int)GrantedAbility.None);
            Assert.AreEqual(1, (int)GrantedAbility.GrenadeLob);
            Assert.AreEqual(2, (int)GrantedAbility.ShieldAllies);
            Assert.AreEqual(3, (int)GrantedAbility.CannonBlast);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `Unity.exe -batchmode -nographics -projectPath "<repo>" -runTests -testPlatform editmode -testResults TestResults.xml -quit`  
Filter: `CombatStatEnumsTests`  
Expected: FAIL — type not found

- [ ] **Step 3: Write minimal implementation**

Create `Assets/_Project/Core/Board/CombatStatEnums.cs`:

```csharp
namespace DeadManZone.Core.Board
{
    public enum AttackSpeedTier { Slow, Medium, Fast }
    public enum AttackRangeTier { Short, Medium, Long }
    public enum MovementSpeedTier { None, Low, Medium, High }
    public enum ArmorType { None, Light, Medium, Heavy }
    public enum AttackType { None, Ballistic, Explosive, Piercing }
    public enum GrantedAbility { None, GrenadeLob, ShieldAllies, CannonBlast }
}
```

- [ ] **Step 4: Run test to verify it passes**

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Core/Board/CombatStatEnums.cs Assets/_Project/Core.Tests/EditMode/CombatStatEnumsTests.cs
git commit -m "feat: add combat stat enums for demo unit model"
```

---

### Task 2: Extend PieceDefinition and PieceDefinitionSO

**Files:**
- Modify: `Assets/_Project/Core/Board/PieceDefinition.cs`
- Modify: `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/TestPieces.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/PieceDefinitionCombatStatsTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using DeadManZone.Core.Board;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class PieceDefinitionCombatStatsTests
    {
        [Test]
        public void PieceDefinition_DefaultsToMediumBaseline()
        {
            var piece = TestPieces.RifleSquad();
            Assert.AreEqual(AttackSpeedTier.Medium, piece.AttackSpeed);
            Assert.AreEqual(AttackRangeTier.Medium, piece.AttackRange);
            Assert.AreEqual(MovementSpeedTier.Medium, piece.MovementSpeed);
            Assert.AreEqual(ArmorType.Light, piece.ArmorType);
            Assert.AreEqual(AttackType.Ballistic, piece.AttackType);
            Assert.AreEqual(GrantedAbility.None, piece.GrantedAbility);
            Assert.AreEqual("neutral", piece.FactionId);
        }
    }
}
```

- [ ] **Step 2: Run test — expect FAIL**

- [ ] **Step 3: Add properties to PieceDefinition**

```csharp
public AttackSpeedTier AttackSpeed { get; init; } = AttackSpeedTier.Medium;
public AttackRangeTier AttackRange { get; init; } = AttackRangeTier.Medium;
public MovementSpeedTier MovementSpeed { get; init; } = MovementSpeedTier.Medium;
public ArmorType ArmorType { get; init; } = ArmorType.Light;
public AttackType AttackType { get; init; } = AttackType.Ballistic;
public GrantedAbility GrantedAbility { get; init; } = GrantedAbility.None;
public string FactionId { get; init; } = "neutral";
```

Update `PieceDefinitionSO.ToCore()` to map the new serialized fields (defaults match above).

Update `TestPieces.RifleSquad()` to set `FactionId = "iron_vanguard"` and explicit enum values.

- [ ] **Step 4: Run test — expect PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat: extend PieceDefinition with combat stat fields"
```

---

## Phase 2 — CombatDamageResolver & range gating

### Task 3: CombatDamageResolver (RPS lite)

**Files:**
- Create: `Assets/_Project/Core/Combat/CombatDamageResolver.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/CombatDamageResolverTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatDamageResolverTests
    {
        [Test]
        public void Ballistic_BonusVsLightArmor()
        {
            var atk = TestPieces.RifleSquad() with { BaseDamage = 100, AttackType = AttackType.Ballistic };
            var def = TestPieces.RifleSquad() with { ArmorType = ArmorType.Light };
            int dmg = CombatDamageResolver.ComputeDamage(atk, def, damageScale: 1f, armorBuffSteps: 0);
            Assert.AreEqual(125, dmg);
        }

        [Test]
        public void Piercing_BonusVsHeavyArmor()
        {
            var atk = TestPieces.RifleSquad() with { BaseDamage = 100, AttackType = AttackType.Piercing };
            var def = TestPieces.RifleSquad() with { ArmorType = ArmorType.Heavy };
            int dmg = CombatDamageResolver.ComputeDamage(atk, def, damageScale: 1f, armorBuffSteps: 0);
            Assert.AreEqual(95, dmg); // 100 * 0.70 * 1.35 = 94.5 → 95
        }

        [Test]
        public void ShieldAllies_BumpsArmorOneTier()
        {
            var atk = TestPieces.RifleSquad() with { BaseDamage = 100, AttackType = AttackType.Ballistic };
            var def = TestPieces.RifleSquad() with { ArmorType = ArmorType.Light };
            int dmg = CombatDamageResolver.ComputeDamage(atk, def, damageScale: 1f, armorBuffSteps: 1);
            Assert.Less(dmg, 125); // Light→Medium removes ballistic bonus
        }
    }
}
```

Note: use explicit `PieceDefinition` construction in tests if `with` unavailable (project uses init-only sealed class — build helper `TestPieces.With(...)` instead).

- [ ] **Step 2: Run tests — expect FAIL**

- [ ] **Step 3: Implement CombatDamageResolver**

```csharp
namespace DeadManZone.Core.Combat
{
    public static class CombatDamageResolver
    {
        public static int ComputeDamage(
            PieceDefinition attacker,
            PieceDefinition defender,
            float damageScale,
            int armorBuffSteps,
            int flatBonus = 0)
        {
            float baseDmg = (attacker.BaseDamage + flatBonus) * damageScale;
            var armor = StepArmor(defender.ArmorType, armorBuffSteps);
            float afterArmor = baseDmg * BaselineArmorMultiplier(armor);
            float typeMult = AttackTypeMultiplier(attacker.AttackType, armor, defender.Tags);
            return System.Math.Max(1, (int)(afterArmor * typeMult));
        }

        private static ArmorType StepArmor(ArmorType baseArmor, int steps) { /* Light→Medium→Heavy cap Heavy */ }
        private static float BaselineArmorMultiplier(ArmorType armor) => armor switch
        {
            ArmorType.Medium => 0.85f,
            ArmorType.Heavy => 0.70f,
            _ => 1.0f
        };
        private static float AttackTypeMultiplier(AttackType atk, ArmorType armor, IReadOnlyList<string> tags) { /* spec table */ }
    }
}
```

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit**

---

### Task 4: Attack range gating in CombatTargeting

**Files:**
- Modify: `Assets/_Project/Core/Combat/CombatTargeting.cs` (later renamed `TacticTargeting.cs` in Task 6)
- Modify: `Assets/_Project/Core/Combat/CombatantState.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/CombatRangeTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Test]
public void SelectTarget_SkipsOutOfRangeEnemies()
{
    var attacker = new CombatantState
    {
        InstanceId = "a1",
        Definition = TestPieces.RifleSquad() with { AttackRange = AttackRangeTier.Short },
        Position = new GridCoord(0, 0),
        CurrentHp = 10
    };
    var near = new CombatantState { InstanceId = "e1", Definition = TestPieces.RifleSquad(), Position = new GridCoord(1, 0), CurrentHp = 10 };
    var far = new CombatantState { InstanceId = "e2", Definition = TestPieces.RifleSquad(), Position = new GridCoord(5, 0), CurrentHp = 3 };
    var target = CombatTargeting.SelectTarget(attacker, new[] { far, near }, StanceType.FocusWeakest);
    Assert.AreEqual("e1", target.InstanceId);
}
```

- [ ] **Step 2–4: Implement Manhattan range filter (Short=1, Medium=3, Long=6) before stance sort**

- [ ] **Step 5: Wire `TickCombatRun.ResolveAttacks` to use `CombatDamageResolver` and track `DamageDealtThisFight` / `DamageTakenThisFight` on combatants**

- [ ] **Step 6: Commit**

---

## Phase 3 — Combat pacing (5s / 30s / 5s + gas until win)

### Task 5: CombatPacingConfig and segment restructure

**Files:**
- Create: `Assets/_Project/Core/Combat/CombatPacingConfig.cs`
- Modify: `Assets/_Project/Core/Combat/CombatSegment.cs`
- Modify: `Assets/_Project/Core/Combat/TickCombatRun.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/CombatResolverTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Test]
public void TickCombatRun_GasSegmentRunsUntilWinner_NoFixedCap()
{
    // Setup stalemate boards with high HP, seed locked
    var run = TickCombatRun.Start(playerBoard, enemyBoard, seed: 42, authority: 5);
    run.Continue(empty); // through pause 1
    run.Continue(empty); // through pause 2
    // After FinalPush brief segment, gas ticks until IsFightOver
    Assert.IsTrue(run.Log.Entries.Any(e => e.ActionType == "gas_damage"));
    Assert.IsTrue(run.IsFightOver);
}
```

- [ ] **Step 2: Update CombatPacingConfig**

```csharp
public static class CombatPacingConfig
{
    public const int TicksPerSecond = 10;
    public const int OpeningTicks = 50;
    public const int MainFightTicks = 300;
    public const int BriefPushTicks = 50;
    public const int MaxGasTicks = 10_000; // safety cap for sim; presentation still "until win"
}
```

- [ ] **Step 3: Change TickCombatRun flow**

Replace single `GasFinal` budget with:
1. `RunSegment(Opening, …, OpeningTicks, 0.2f)`
2. pause
3. `RunSegment(MainFight, …, MainFightTicks, 1.0f)`
4. pause
5. `RunSegment(BriefPush, …, BriefPushTicks, 1.0f)` — **no gas**
6. `RunGasUntilEnd()` — loop ticks applying gas + move/attack until `TryEndFight()` or `MaxGasTicks`

- [ ] **Step 4: Update existing CombatResolverTests segment expectations**

- [ ] **Step 5: Run Edit Mode combat tests — all PASS**

- [ ] **Step 6: Commit**

```bash
git commit -m "feat: retime combat segments to 5s/30s/5s plus gas until win"
```

---

## Phase 4 — Tactics system

### Task 6: Rename stances to tactics

**Files:**
- Create: `Assets/_Project/Core/Combat/TacticType.cs`
- Create: `Assets/_Project/Core/Combat/TacticState.cs`
- Create: `Assets/_Project/Core/Combat/TacticTargeting.cs`
- Modify: `Assets/_Project/Core/Combat/TickCombatRun.cs`
- Modify: `Assets/_Project/Core/Combat/PhaseCommand.cs`
- Delete or obsolete: `StanceType.cs`, `StanceState.cs` (grep-replace all references)

- [ ] **Step 1: Write failing test**

```csharp
[Test]
public void TacticType_MapsLegacyStances()
{
    Assert.AreEqual(TacticType.DisciplinedFire, TacticType.DisciplinedFire);
    Assert.AreEqual(4, System.Enum.GetNames(typeof(TacticType)).Length);
}
```

Enum values:
```csharp
public enum TacticType
{
    DisciplinedFire,  // was FocusWeakest
    Advance,          // was AllOutAssault + forward bias
    StandGround,      // was HoldTheLine
    ProtectSupport    // was SupportPriority
}
```

- [ ] **Step 2: Implement TacticTargeting with same sort keys as old CombatTargeting**

- [ ] **Step 3: Add `TacticPauseValidator`**

```csharp
public sealed class TacticPauseValidator
{
    public bool CanContinue(
        TacticType selected,
        TacticType previous,
        bool hqAlive,
        bool hasCommandPiece,
        CombatPhase pauseAfterPhase,
        ref int authority,
        out string reason)
    {
        if (!hqAlive && selected == TacticType.DisciplinedFire) { reason = "HQ destroyed"; return false; }
        if (selected == TacticType.ProtectSupport && !hasCommandPiece) { reason = "No Command piece"; return false; }
        // Apply base costs + pause-2 switch surcharge per spec table
    }
}
```

- [ ] **Step 4: Update PhaseCommand to carry `TacticType SelectedTactic`**

- [ ] **Step 5: Grep replace `StanceType` → `TacticType` across Core, Game, Presentation, Tests**

- [ ] **Step 6: Run all Edit Mode tests — fix breakages**

- [ ] **Step 7: Commit**

---

## Phase 5 — Demo abilities

### Task 7: CombatAbilityExecutor

**Files:**
- Create: `Assets/_Project/Core/Combat/CombatAbilityExecutor.cs`
- Modify: `Assets/_Project/Core/Combat/CommandProcessor.cs`
- Modify: `Assets/_Project/Core/Combat/PhaseCommand.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/CombatAbilityExecutorTests.cs`

- [ ] **Step 1: Write failing tests for each ability**

```csharp
[Test]
public void GrenadeLob_DealsExplosiveAoE()
{
    // board with grenade thrower alive + clustered enemies
    var log = new CombatEventLog();
    CombatAbilityExecutor.Execute(GrantedAbility.GrenadeLob, sourceId, board, playerCombatants, enemyCombatants, log, CombatPhase.Grind, targetCell: new GridCoord(10, 5));
    Assert.IsTrue(log.Entries.Any(e => e.ActionType == "grenade_lob"));
}

[Test]
public void CannonBlast_OnlyValidOnGrindPause()
{
    // Pause 1 submission rejects CannonBlast with reason
}
```

- [ ] **Step 2: Implement executor**

- Grenade Lob: 30 explosive damage, 2×2 area, uses `CombatDamageResolver` with Explosive
- Shield Allies: set `ArmorBuffSteps = 1` on adjacent infantry combatants for next segment
- Cannon Blast: 50 primary + 25 splash adjacent to target; Pause 2 only

- [ ] **Step 3: Extend PhaseCommand with `List<GrantedAbility> SelectedAbilities` and per-ability target cell optional**

- [ ] **Step 4: CommandProcessor batch: validate all abilities (alive source with matching GrantedAbility, Authority, pause phase), deduct Authority atomically, execute at segment start in TickCombatRun.ApplyCommands**

- [ ] **Step 5: Run tests — PASS**

- [ ] **Step 6: Commit**

---

## Phase 6 — HQ spawn & immovable rules

### Task 8: FactionSO HQ config

**Files:**
- Modify: `Assets/_Project/Data/ScriptableObjects/FactionSO.cs`
- Modify: `Assets/_Project/Data/Resources/DeadManZone/Factions/*.asset` (Iron Vanguard values)
- Create: `Assets/_Project/Core.Tests/EditMode/HqSpawnTests.cs`

- [ ] **Step 1: Add fields to FactionSO**

```csharp
public string hqPieceId = "hq_command";
public Vector2Int hqSpawnAnchor = new(0, 4);
public int hqSpawnRotation = 0; // PieceRotation enum int
```

- [ ] **Step 2: Write failing test**

```csharp
[Test]
public void StartNewRun_PlacesHqAtFactionAnchor()
{
    var orchestrator = TestRunOrchestrator.CreateWithIronVanguard();
    orchestrator.StartNewRun("iron_vanguard", runSeed: 1);
    var board = orchestrator.GetPlayerBoard();
    var hq = board.Pieces.Single(p => p.Definition.Tags.Contains(GameTags.Hq));
    Assert.AreEqual(new GridCoord(0, 4), hq.Anchor);
}
```

- [ ] **Step 3: Implement in RunOrchestrator.StartNewRun**

After `CreateEmptyBoardSnapshot`, load HQ piece from registry and `TryPlace` at faction anchor with fixed instance id `"hq_player"`.

- [ ] **Step 4: Run test — PASS**

- [ ] **Step 5: Commit**

---

### Task 9: Block HQ move and sell

**Files:**
- Modify: `Assets/_Project/Core/Board/BoardState.cs`
- Modify: `Assets/_Project/Game/RunOrchestrator.cs` (`TryMovePlacedPiece`, sell method)
- Modify: `Assets/_Project/Core.Tests/EditMode/RunOrchestratorTests.cs`
- Modify: `Assets/_Project/Presentation/Board/BoardView.cs`

- [ ] **Step 1: Write failing tests**

```csharp
[Test]
public void TryMovePlacedPiece_RejectsHq()
{
    // setup run with HQ
    Assert.IsFalse(_orchestrator.TryMovePlacedPiece("hq_player", new GridCoord(2, 2)));
}

[Test]
public void TrySellPiece_RejectsHq()
{
    Assert.IsFalse(_orchestrator.TrySellPiece("hq_player"));
}
```

- [ ] **Step 2: Guard in BoardState.TryRelocate and TryRemove — if piece has `GameTags.Hq`, return fail**

- [ ] **Step 3: BoardView — skip drag start for HQ-tagged instances**

- [ ] **Step 4: Run tests — PASS**

- [ ] **Step 5: Commit**

---

## Phase 7 — Shop pool filter & demo content

### Task 10: ShopPoolFilter

**Files:**
- Create: `Assets/_Project/Core/Shop/ShopPoolFilter.cs`
- Modify: `Assets/_Project/Core/Shop/ShopGenerator.cs`
- Modify: `Assets/_Project/Core/Content/ContentRegistry.cs` (optional `GetPool(lane, factionId)`)
- Create: `Assets/_Project/Core.Tests/EditMode/ShopPoolFilterTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Test]
public void Fight1_HeavilyFavorsNeutral()
{
    var rng = new Rng(123);
    int neutral = 0;
    for (int i = 0; i < 100; i++)
    {
        var piece = ShopPoolFilter.PickWeighted(pool, fightIndex: 1, rng);
        if (piece.FactionId == "neutral") neutral++;
    }
    Assert.Greater(neutral, 70);
}
```

- [ ] **Step 2: Implement weight table from spec (85/15, 55/45, 25/75)**

- [ ] **Step 3: ShopGenerator.RollLane passes `round` fight index into filter**

- [ ] **Step 4: Commit**

---

### Task 11: Author demo piece assets

**Files:**
- Create: `Assets/_Project/Data/Resources/DeadManZone/Pieces/conscript_rifleman.asset`
- Create: `.../grenade_thrower.asset`
- Create: `.../field_medic.asset`
- Create: `.../armored_transport.asset`
- Create: `.../mobile_cannon.asset`
- Modify: `.../rifle_squad.asset`, `diesel_walker.asset`, `radio_array.asset`, `hq_command.asset`
- Modify: `Assets/_Project/Data/ContentDatabase.cs` or loader to register only demo pool pieces for player shop

- [ ] **Step 1: Create five neutral ScriptableObjects with stats from spec Section 5**

- [ ] **Step 2: Rebalance three IV pieces; set `radio_array` tags to include `Command`, `GrantedAbility = None`**

- [ ] **Step 3: Set `hq_command` MaxHp ~200, MovementSpeed None, tags `HQ`, `building`**

- [ ] **Step 4: Register pieces in ContentDatabase with correct ShopLane; exclude non-demo pieces from player shop pool (enemies may still use legacy templates until Task 12)**

- [ ] **Step 5: Manual verify in Unity — shop shows demo pieces**

- [ ] **Step 6: Commit**

```bash
git commit -m "content: add demo neutral roster and rebalance Iron Vanguard pieces"
```

---

### Task 12: Rebalance enemy fight templates (light pass)

**Files:**
- Modify: `Assets/_Project/Data/Resources/DeadManZone/Enemies/fight_*.asset`

- [ ] **Step 1: Ensure each fight template includes enemy HQ at fixed rear anchor**

- [ ] **Step 2: Replace enemy units with demo pool ids where sensible (fights 1–3 neutral-heavy)**

- [ ] **Step 3: Smoke test fights 1, 5, 10 in editor**

- [ ] **Step 4: Commit**

---

## Phase 8 — Battle report & draw outcome

### Task 13: BattleReportBuilder

**Files:**
- Create: `Assets/_Project/Core/Combat/BattleReportBuilder.cs`
- Create: `Assets/_Project/Core/Combat/BattleReport.cs`
- Modify: `Assets/_Project/Game/RunOrchestrator.cs` (`CompleteCombat`)
- Modify: `Assets/_Project/Game/FightRewardTable.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/BattleReportBuilderTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Test]
public void BuildReport_ReturnsTop3DamageDealt()
{
    var combatants = /* setup with tracked damage */;
    var report = BattleReportBuilder.Build(combatants, playerWon: true, suppliesEarned: 10, moraleDelta: 0);
    Assert.AreEqual(3, report.TopDamageDealt.Count);
}
```

- [ ] **Step 2: Implement BattleReport DTO and builder sorting by DamageDealtThisFight / DamageTakenThisFight**

- [ ] **Step 3: Add draw detection in CombatWinChecker or TickCombatRun — both sides zero combatants same tick**

- [ ] **Step 4: FightRewardTable.GetReward(fightIndex, isDraw: true)` returns 50% supplies; MoraleCalculator skips loss on draw**

- [ ] **Step 5: Store last `BattleReport` on RunState or return through orchestrator for UI**

- [ ] **Step 6: Commit**

---

### Task 14: BattleReportPresenter

**Files:**
- Create: `Assets/_Project/Presentation/Combat/BattleReportPresenter.cs`
- Modify: `Assets/_Project/Presentation/Combat/CombatFlowPresenter.cs`
- Modify: Unity scene / prefab for report panel

- [ ] **Step 1: Create UI panel with outcome, manpower refund, supplies, morale delta, two top-3 lists, Continue button**

- [ ] **Step 2: Wire Continue → shop build phase (skip separate board-income screen per spec)**

- [ ] **Step 3: Play Mode test or manual checklist — report appears after fight**

- [ ] **Step 4: Commit**

---

## Phase 9 — Presentation: pause UI & combat loading

### Task 15: TacticPausePanel

**Files:**
- Create: `Assets/_Project/Presentation/Combat/TacticPausePanel.cs`
- Modify: `Assets/_Project/Presentation/Combat/CombatFlowPresenter.cs`
- Modify: `Assets/_Project/Game/RunManager.cs` (expose available tactics + abilities)
- Obsolete: `PhaseCommandPanel.cs` usage (keep file until migrated)

- [ ] **Step 1: Build UI — tactic radio (4 options, gray Disciplined Fire if HQ dead), ability cards for unlocked granted abilities**

- [ ] **Step 2: Live Authority cost total; disable Continue until `TacticPauseValidator.CanContinue`**

- [ ] **Step 3: Submit builds `PhaseCommand` list with selected tactic + abilities**

- [ ] **Step 4: Play Mode test `TacticPausePanelPlayModeTests` — panel gates Continue correctly**

- [ ] **Step 5: Commit**

---

### Task 16: Combat loading placeholder

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/CombatFlowPresenter.cs`

- [ ] **Step 1: Show overlay "Entering combat…" for ~1s while battlefield builds**

- [ ] **Step 2: Hide overlay when CombatDirector starts replay**

- [ ] **Step 3: Commit**

---

## Phase 10 — Integration & regression

### Task 17: Save/resume with tactics mid-pause

**Files:**
- Modify: `Assets/_Project/Core/Run/RunSaveSerializer.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/RunSaveSerializerTests.cs`

- [ ] **Step 1: Extend combat save blob with `SelectedTactic`, pending abilities if pause unsubmitted**

- [ ] **Step 2: Test: save at AwaitingCommand → reload → identical outcome after same commands**

- [ ] **Step 3: Commit**

---

### Task 18: Full regression pass

- [ ] **Step 1: Run all Edit Mode tests**

```bash
Unity.exe -batchmode -nographics -projectPath "<repo>" -runTests -testPlatform editmode -testResults TestResults-EditMode.xml -quit
```

Expected: 0 failures

- [ ] **Step 2: Run Play Mode tests**

- [ ] **Step 3: Manual demo checklist**

  - New run → HQ visible at rear, cannot drag
  - Shop fight 1 → mostly neutral pieces
  - Fight 5 → mix of neutral + IV
  - Combat pauses at ~5s and ~35s wall time
  - Grenade Lob available with Grenade Thrower on board
  - Cannon Blast only on pause 2 with Mobile Cannon
  - Gas ends stalemate
  - Battle report shows top 3 lists
  - Save mid-pause → resume identical

- [ ] **Step 4: Final commit if any fixes**

```bash
git commit -m "test: combat units demo integration fixes"
```

---

## Spec coverage checklist

| Spec section | Task(s) |
|--------------|---------|
| Segment pacing 5/30/5 + gas | Task 5 |
| Tactics + switch surcharge | Task 6 |
| 3 abilities | Task 7 |
| Unit stat enums + tags | Tasks 1–2, 4 |
| RPS damage | Task 3 |
| HQ spawn/immovable | Tasks 8–9 |
| 8-piece roster + weighting | Tasks 10–12 |
| Battle report standard | Tasks 13–14 |
| Draw outcome | Task 13 |
| Presentation pause + loading | Tasks 15–16 |
| Save mid-pause | Task 17 |

---

## Success criteria (from spec)

- [ ] Tactic selection at both pauses in a full fight
- [ ] Demo abilities fire when source piece alive
- [ ] RPS damage verifiable in tests
- [ ] HQ auto-spawn, no move/sell
- [ ] Battle report top 3 dealt/taken
- [ ] Save mid-pause → identical outcome
- [ ] Deterministic replay for same seed + commands
