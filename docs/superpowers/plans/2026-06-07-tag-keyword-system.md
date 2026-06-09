# Tag / Keyword System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the [2026-06-07 tag/keyword spec](../specs/2026-06-07-tag-keyword-system-design.md) so one tag vocabulary drives authoring, backend systems, and player piece cards with fight-start synergy and critical-mass snapshots.

**Architecture:** Add a `Tags` module under `DeadManZone.Core` with `TagRegistry`, categorized fields on `PieceDefinition`, and data-driven `SynergyEffect` / `CriticalMassRule` definitions. Keep `AttackType`, `ArmorType`, and `GrantedAbility` as enums. Refactor `SynergyEngine` and `CriticalMassRules` to tag-owned rules. Presentation adds a hover card that reads registry metadata.

**Tech Stack:** Unity 6, C#, Unity Test Framework (Edit Mode), asmdefs under `Assets/_Project/`.

**Spec reference:** `docs/superpowers/specs/2026-06-07-tag-keyword-system-design.md`

**Branch:** `tag-rework`

---

## File map (new & modified)

| Path | Responsibility |
|------|----------------|
| `Assets/_Project/Core/Tags/TagCategory.cs` | Primary, CombatRole, System, Synergy, Ability, Flavor |
| `Assets/_Project/Core/Tags/TagDefinition.cs` | Registry entry (id, display, tooltip, visibility) |
| `Assets/_Project/Core/Tags/GameTagIds.cs` | Canonical snake_case tag ID constants |
| `Assets/_Project/Core/Tags/TagRegistry.cs` | Lookup, validation, demo tag catalog |
| `Assets/_Project/Core/Tags/SynergyModType.cs` | Flat, TierStep, Percent |
| `Assets/_Project/Core/Tags/SynergyDirection.cs` | Inbound, Outbound |
| `Assets/_Project/Core/Tags/NeighborFilter.cs` | Filter adjacent pieces by tag/category/faction |
| `Assets/_Project/Core/Tags/SynergyEffectDefinition.cs` | Per-tag synergy rule data |
| `Assets/_Project/Core/Tags/SynergyRuleCatalog.cs` | Demo synergy rules keyed by source tag |
| `Assets/_Project/Core/Tags/CriticalMassRuleDefinition.cs` | Threshold rule data |
| `Assets/_Project/Core/Tags/CriticalMassRuleCatalog.cs` | Demo critical mass rules |
| `Assets/_Project/Core/Tags/CombatRoleProfile.cs` | Role → AI bias data (v1) |
| `Assets/_Project/Core/Tags/PieceTagQueries.cs` | `HasTag`, `GetAllTags`, display tag selection |
| `Assets/_Project/Core/Board/PieceDefinition.cs` | Categorized tag fields + legacy `Tags` bridge |
| `Assets/_Project/Core/Combat/SynergyEngine.cs` | Tag-owned effects; fight-start snapshot |
| `Assets/_Project/Core/Combat/CriticalMassRules.cs` | Data-driven thresholds; fight-start snapshot |
| `Assets/_Project/Core/Combat/CombatRoleTargeting.cs` | Role-based target bias layered on tactics |
| `Assets/_Project/Core/Board/PrimaryZoneRules.cs` | Primary → allowed zones |
| `Assets/_Project/Core/Board/BoardState.cs` | Call `PrimaryZoneRules` on place/relocate |
| `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs` | Serialize categorized tags; build `PieceDefinition` |
| `Assets/_Project/Data/Editor/TagContentMigrator.cs` | Migrate demo pieces + flat `Tags` |
| `Assets/_Project/Presentation/Board/PieceHoverCard.cs` | Stats row + tag chips + `+N` overflow |
| `Assets/_Project/Core/Tags/PieceCardViewModelBuilder.cs` | Maps `PieceDefinition` + registry → card view model |
| `Assets/_Project/Core.Tests/EditMode/*` | Tests per task |

---

## Phase 1 — Tag registry & taxonomy

### Task 1: Tag category and definition types

**Files:**
- Create: `Assets/_Project/Core/Tags/TagCategory.cs`
- Create: `Assets/_Project/Core/Tags/TagDefinition.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/TagRegistryTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class TagRegistryTests
    {
        [Test]
        public void Registry_ContainsInfantryPrimaryTag()
        {
            var tag = TagRegistry.Get(GameTagIds.Infantry);
            Assert.AreEqual(TagCategory.Primary, tag.Category);
            Assert.IsTrue(tag.PlayerVisible);
            Assert.AreEqual("Infantry", tag.DisplayName);
        }

        [Test]
        public void Registry_UnknownId_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => TagRegistry.Get("not_a_real_tag"));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `Unity.exe -batchmode -nographics -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" -runTests -testPlatform editmode -testFilter "TagRegistryTests" -testResults TestResults-EditMode.xml -quit`  
Expected: FAIL — types not found

- [ ] **Step 3: Write minimal implementation**

Create `TagCategory.cs`:

```csharp
namespace DeadManZone.Core.Tags
{
    public enum TagCategory
    {
        Primary,
        CombatRole,
        System,
        Faction,
        Synergy,
        Ability,
        Flavor
    }
}
```

Create `TagDefinition.cs`:

```csharp
namespace DeadManZone.Core.Tags
{
    public sealed class TagDefinition
    {
        public string Id { get; init; }
        public string DisplayName { get; init; }
        public TagCategory Category { get; init; }
        public bool PlayerVisible { get; init; } = true;
        public string Tooltip { get; init; }
        public int DisplayPriority { get; init; }
    }
}
```

Create `GameTagIds.cs` with demo IDs (snake_case):

```csharp
namespace DeadManZone.Core.Tags
{
    public static class GameTagIds
    {
        // Primary
        public const string Infantry = "infantry";
        public const string Vehicle = "vehicle";
        public const string Building = "building";
        public const string Structure = "structure";

        // Combat role
        public const string Assault = "assault";
        public const string Tank = "tank";
        public const string Artillery = "artillery";
        public const string Support = "support";
        public const string Utility = "utility";
        public const string Headquarters = "headquarters";
        public const string Sniper = "sniper";

        // System
        public const string Combatant = "combatant";
        public const string NonCombatant = "noncombatant";
        public const string Hq = "hq";

        // Synergy (demo)
        public const string Supply = "supply";
        public const string Medic = "medic";
        public const string Command = "command";
        public const string Echo = "echo";
        public const string Stealth = "stealth";
        public const string Vanguard = "vanguard";
        public const string Mechanical = "mechanical";
        public const string Gas = "gas";
    }
}
```

Create `TagRegistry.cs` with static catalog + `Get(string id)` and `TryGet(string id, out TagDefinition tag)`.

- [ ] **Step 4: Run test to verify it passes**

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Core/Tags Assets/_Project/Core.Tests/EditMode/TagRegistryTests.cs
git commit -m "feat(tags): add tag registry and canonical tag IDs"
```

---

### Task 2: Categorized fields on PieceDefinition

**Files:**
- Modify: `Assets/_Project/Core/Board/PieceDefinition.cs`
- Modify: `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs`
- Create: `Assets/_Project/Core/Tags/PieceTagQueries.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/PieceTagQueriesTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class PieceTagQueriesTests
    {
        [Test]
        public void HasTag_MatchesPrimaryAndSynergy()
        {
            var piece = new PieceDefinition
            {
                Id = "test",
                DisplayName = "Test",
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                Primary = GameTagIds.Infantry,
                CombatRole = GameTagIds.Assault,
                SystemTag = GameTagIds.Combatant,
                SynergyTags = new[] { GameTagIds.Vanguard }
            };

            Assert.IsTrue(PieceTagQueries.HasTag(piece, GameTagIds.Infantry));
            Assert.IsTrue(PieceTagQueries.HasTag(piece, GameTagIds.Vanguard));
            Assert.IsFalse(PieceTagQueries.HasTag(piece, GameTagIds.Vehicle));
        }

        [Test]
        public void GetPlayerVisibleTags_ExcludesSystemTags()
        {
            var piece = new PieceDefinition
            {
                Id = "test",
                DisplayName = "Test",
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                Primary = GameTagIds.Infantry,
                CombatRole = GameTagIds.Assault,
                SystemTag = GameTagIds.Combatant,
                FactionId = "neutral",
                SynergyTags = new[] { "a", "b", "c", "d", "e" },
                AbilityTags = new[] { "flamethrower" }
            };

            var visible = PieceTagQueries.GetPlayerVisibleTags(piece, maxOptionalChips: 4);
            Assert.IsFalse(visible.Any(t => t.Id == GameTagIds.Combatant));
            Assert.LessOrEqual(visible.Count(t => t.Category is TagCategory.Synergy or TagCategory.Ability), 4);
        }
    }
}
```

Add `using System.Linq;` to the test file.

- [ ] **Step 2: Run test — expect FAIL**

- [ ] **Step 3: Implement**

Add to `PieceDefinition.cs`:

```csharp
public string Primary { get; init; }
public string CombatRole { get; init; }
public string SystemTag { get; init; }
public IReadOnlyList<string> SynergyTags { get; init; } = System.Array.Empty<string>();
public IReadOnlyList<string> AbilityTags { get; init; } = System.Array.Empty<string>();
```

Keep existing `Tags` temporarily. In `PieceDefinitionSO.ToCore()`:

- Map new serialized string fields
- Build legacy `Tags` via `PieceTagQueries.BuildLegacyTags(data)` for consumers not yet migrated

Implement `PieceTagQueries.cs`:

- `HasTag(PieceDefinition, string)` — checks categorized fields + synergy/ability lists + legacy `Tags` during migration
- `GetAllTagIds(PieceDefinition)`
- `GetPlayerVisibleTags(PieceDefinition, int maxOptionalChips)` — always Primary, CombatRole, Faction display; abilities first then synergy by `DisplayPriority`; `+N` count as metadata on return type or separate `GetOverflowCount`

- [ ] **Step 4: Run test — expect PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(tags): add categorized piece tag fields and query helpers"
```

---

## Phase 2 — Tag-owned synergies (fight-start snapshot)

### Task 3: Synergy effect definitions

**Files:**
- Create: `Assets/_Project/Core/Tags/SynergyModType.cs`
- Create: `Assets/_Project/Core/Tags/SynergyDirection.cs`
- Create: `Assets/_Project/Core/Tags/NeighborFilter.cs`
- Create: `Assets/_Project/Core/Tags/SynergyEffectDefinition.cs`
- Create: `Assets/_Project/Core/Tags/SynergyRuleCatalog.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/SynergyEngineTests.cs`

- [ ] **Step 1: Write failing tests**

Add to `SynergyEngineTests.cs`:

```csharp
[Test]
public void Supply_OutboundAura_BuffsAdjacentNeighbor()
{
    var supply = TestPieces.CreateUnit("supply",
        primary: GameTagIds.Building,
        system: GameTagIds.NonCombatant,
        synergy: new[] { GameTagIds.Supply });
    var rifle = TestPieces.CreateUnit("rifle",
        primary: GameTagIds.Infantry,
        combatRole: GameTagIds.Assault,
        system: GameTagIds.Combatant);

    var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
    var board = new BoardState(layout);
    board.TryPlace(supply, new GridCoord(0, 0), "supply_1");
    board.TryPlace(rifle, new GridCoord(1, 0), "rifle_1");

    var snapshot = SynergyEngine.EvaluateFightStart(board);
    Assert.GreaterOrEqual(snapshot.GetDamageBonus("rifle_1"), 1);
}

[Test]
public void FightStartSnapshot_DoesNotChangeAfterMove()
{
    // Place supply + rifle adjacent, snapshot, simulate combatant position change off board adjacency,
    // assert rifle damage bonus unchanged from snapshot.
}
```

Extend `TestPieces.CreateUnit` with optional `primary`, `combatRole`, `system`, `synergy` parameters.

- [ ] **Step 2: Run tests — expect FAIL**

- [ ] **Step 3: Implement synergy data model**

`SynergyEffectDefinition` fields:

```csharp
public string SourceTagId { get; init; }
public SynergyDirection Direction { get; init; }
public SynergyModType ModType { get; init; }
public SynergyStat Stat { get; init; }  // Damage, AttackRange, MovementSpeed, ArmorType, MoveChargePercent
public int Value { get; init; }
public NeighborFilter Filter { get; init; }
```

`SynergyRuleCatalog.DemoRules` — port four demo rules as outbound/inbound:

| Source tag | Direction | Effect |
|------------|-----------|--------|
| `supply` | Outbound | +1 flat damage to any adjacent |
| `medic` | Outbound | +1 armor tier step to adjacent `infantry` primary |
| `command` | Outbound | +2 flat damage to adjacent `artillery` role |
| `echo` | Outbound | +1 flat damage to adjacent `stealth` synergy |

- [ ] **Step 4: Refactor `SynergyEngine`**

Replace pair-scan logic with:

```csharp
public sealed class FightStartSynergySnapshot
{
    private readonly Dictionary<string, SynergyResult> _byInstanceId = new();
    public SynergyResult GetFor(string instanceId) => _byInstanceId.GetValueOrDefault(instanceId);
    public int GetDamageBonus(string instanceId) => GetFor(instanceId).DamageBonus;
}

public static FightStartSynergySnapshot EvaluateFightStart(BoardState board)
```

Algorithm:

1. For each placed piece, foreach synergy tag on piece, load rules from `SynergyRuleCatalog` for that tag
2. **Outbound:** for each adjacent piece matching filter, add buff to neighbor's snapshot entry
3. **Inbound:** count adjacent matches, add buff to self snapshot entry
4. Return immutable snapshot; `ApplyToCombatants` copies snapshot values onto `CombatantState` once at fight start

`TickCombatRun` must call `EvaluateFightStart` once — verify no second pass after movement.

- [ ] **Step 5: Run tests — expect PASS**

- [ ] **Step 6: Commit**

```bash
git commit -m "feat(tags): tag-owned synergy effects with fight-start snapshot"
```

---

## Phase 3 — Data-driven critical mass (fight-start snapshot)

### Task 4: Critical mass rules catalog

**Files:**
- Create: `Assets/_Project/Core/Tags/CriticalMassRuleDefinition.cs`
- Create: `Assets/_Project/Core/Tags/CriticalMassRuleCatalog.cs`
- Modify: `Assets/_Project/Core/Combat/CriticalMassRules.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/CriticalMassRulesTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Test]
public void ThreeInfantryPrimary_GrantsDamageBonus()
{
    var infantry = TestPieces.CreateUnit("inf",
        primary: GameTagIds.Infantry,
        combatRole: GameTagIds.Assault,
        system: GameTagIds.Combatant);

    var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
    var board = new BoardState(layout);
    board.TryPlace(infantry, new GridCoord(0, 0), "a");
    board.TryPlace(infantry, new GridCoord(1, 0), "b");
    board.TryPlace(infantry, new GridCoord(2, 0), "c");

    var bonus = CriticalMassRules.EvaluateFightStart(board);
    Assert.GreaterOrEqual(bonus.DamageBonus, 2);
}

[Test]
public void FightStartSnapshot_DoesNotChangeAfterDeath()
{
    // Evaluate with 3 infantry, mark one dead (or remove from board count helper),
    // assert EvaluateFightStart snapshot unchanged — mid-fight death must not re-run in combat loop.
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement**

`CriticalMassRuleDefinition`:

```csharp
public string TagId { get; init; }
public TagCategory CountCategory { get; init; }
public int Threshold { get; init; }
public int DamageBonus { get; init; }
public int ArmorShredSteps { get; init; }
public int MoveChargePercentBonus { get; init; }
```

`CriticalMassRuleCatalog.DemoRules` — five rules from spec §5.2.

Refactor `CriticalMassRules.Evaluate` → `EvaluateFightStart(BoardState)` counting via `PieceTagQueries` on Primary / CombatRole / Synergy fields (not legacy PascalCase `Infantry` string).

- [ ] **Step 4: Run — expect PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(tags): data-driven critical mass with fight-start snapshot"
```

---

## Phase 4 — Primary zone rules & combat role targeting

### Task 5: Primary zone placement rules

**Files:**
- Create: `Assets/_Project/Core/Board/PrimaryZoneRules.cs`
- Modify: `Assets/_Project/Core/Board/BoardState.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/PrimaryZoneRulesTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Test]
public void BuildingPrimary_CannotPlaceInFrontZone()
{
    var building = TestPieces.CreateUnit("depot",
        primary: GameTagIds.Building,
        combatRole: GameTagIds.Utility,
        system: GameTagIds.NonCombatant);

    var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
    var board = new BoardState(layout);
    var frontCoord = FindFrontZoneCoord(layout);

    var result = board.TryPlace(building, frontCoord, "depot_1");
    Assert.IsFalse(result.Success);
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement `PrimaryZoneRules`**

```csharp
public static bool IsZoneAllowed(string primaryTag, ZoneType zone) => primaryTag switch
{
    GameTagIds.Building => zone == ZoneType.Rear,
    GameTagIds.Infantry => zone is ZoneType.Front or ZoneType.Support,
    GameTagIds.Vehicle => zone is ZoneType.Front or ZoneType.Support,
    GameTagIds.Structure => true,
    _ => true
};
```

Call from `BoardState.TryPlace` / relocate after footprint check.

- [ ] **Step 4: Run — expect PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(tags): enforce primary tag zone placement rules"
```

---

### Task 6: Combat role targeting bias (v1)

**Files:**
- Create: `Assets/_Project/Core/Tags/CombatRoleProfile.cs`
- Create: `Assets/_Project/Core/Combat/CombatRoleTargeting.cs`
- Modify: `Assets/_Project/Core/Combat/TacticTargeting.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/CombatRoleTargetingTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Test]
public void ArtilleryRole_PrefersFurthestTargetInRange()
{
    var artillery = CreateCombatant(GameTagIds.Artillery, new GridCoord(2, 2), attackRange: AttackRangeTier.Long);
    var near = CreateCombatant(GameTagIds.Assault, new GridCoord(3, 2), hp: 5);
    var far = CreateCombatant(GameTagIds.Assault, new GridCoord(8, 2), hp: 10);

    var target = CombatRoleTargeting.SelectTarget(artillery, new[] { near, far }, TacticType.DisciplinedFire);
    Assert.AreEqual(far.InstanceId, target.InstanceId);
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement**

`CombatRoleProfile` per role from spec §2.2. `CombatRoleTargeting.SelectTarget` applies role ordering **after** tactic narrows in-range set. `TacticTargeting` delegates final pick to `CombatRoleTargeting` when `CombatRole` is set.

- [ ] **Step 4: Run — expect PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(tags): combat role targeting profiles v1"
```

---

## Phase 5 — Content migration

### Task 7: Migrate demo pieces to categorized tags

**Files:**
- Modify: `Assets/_Project/Data/Editor/DemoPieceFactory.cs`
- Create: `Assets/_Project/Data/Editor/TagContentMigrator.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/GameTagsTests.cs` (update or supersede)
- Create: `Assets/_Project/Core.Tests/EditMode/DemoPieceTagMigrationTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Test]
public void RifleSquad_HasCategorizedTags()
{
    var registry = TestContentRegistry.CreateDemo();
    var rifle = registry.GetPiece("rifle_squad");
    Assert.AreEqual(GameTagIds.Infantry, rifle.Primary);
    Assert.AreEqual(GameTagIds.Assault, rifle.CombatRole);
    Assert.AreEqual(GameTagIds.Combatant, rifle.SystemTag);
    Assert.IsTrue(PieceTagQueries.HasTag(rifle, GameTagIds.Vanguard));
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Update `DemoPieceFactory`**

Map each demo piece per spec §5 demo table. Example rifle squad:

```csharp
primary: GameTagIds.Infantry,
combatRole: GameTagIds.Assault,
systemTag: GameTagIds.Combatant,
synergyTags: new[] { GameTagIds.Vanguard },
factionId: "iron_vanguard"
```

Add `TagContentMigrator` menu item: `DeadManZone/Migrate Piece Tags` — reads legacy `tags[]` on SO assets, fills categorized fields using mapping table, logs warnings for unknown strings.

Regenerate demo piece assets via existing factory menu.

- [ ] **Step 4: Run migration tests + full EditMode suite**

Run: `Unity.exe -batchmode -nographics -projectPath "<repo>" -runTests -testPlatform editmode -testResults TestResults-EditMode.xml -quit`  
Expected: PASS (fix any consumers still using `GameKeywords.Infantry` PascalCase in synergy/critical mass tests)

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(tags): migrate demo content to categorized tag fields"
```

---

### Task 8: Bridge legacy GameTags/GameKeywords consumers

**Files:**
- Modify: `Assets/_Project/Core/Combat/CombatWinChecker.cs`
- Modify: `Assets/_Project/Core/Run/AuthorityCalculator.cs`
- Modify: `Assets/_Project/Core/Run/ManpowerCalculator.cs`
- Modify: `Assets/_Project/Core/Combat/CombatMovementRules.cs`
- Modify: `Assets/_Project/Core/Combat/CombatDamageResolver.cs`
- Modify: `Assets/_Project/Core/Combat/CombatantState.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Test]
public void CombatantState_HasTag_UsesCategorizedSystemTag()
{
    var def = TestPieces.CreateUnit("u",
        primary: GameTagIds.Infantry,
        combatRole: GameTagIds.Assault,
        system: GameTagIds.Combatant);
    var combatant = new CombatantState("u", def, new GridCoord(0, 0), 10);
    Assert.IsTrue(combatant.HasTag(GameTagIds.Combatant));
}
```

- [ ] **Step 2: Run — expect FAIL if `CombatantState` still only checks `Definition.Tags`**

- [ ] **Step 3: Update consumers**

Replace `GameTags.Combatant` / `GameTags.Hq` / `GameTags.Command` checks with `PieceTagQueries.HasTag(definition, GameTagIds.*)`.

`CombatDamageResolver`: replace `building`/`structure` string tag check with `PieceTagQueries.HasTag(defender, GameTagIds.Building)` or `Structure`.

Mark `GameTags.cs` and `GameKeywords.cs` `[Obsolete]` pointing to `GameTagIds` — do not delete until all call sites migrated.

- [ ] **Step 4: Run full EditMode tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "refactor(tags): route gameplay queries through PieceTagQueries"
```

---

## Phase 6 — Piece hover card UI

### Task 9: Hover card view model (Core, testable)

**Files:**
- Create: `Assets/_Project/Core/Tags/PieceCardViewModel.cs`
- Create: `Assets/_Project/Core/Tags/PieceCardViewModelBuilder.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/PieceCardViewModelBuilderTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Test]
public void Build_IncludesStatIconsAndIdentityChipsOnly()
{
    var piece = /* flamethrower trooper from spec §6.3 */;
    var model = PieceCardViewModelBuilder.Build(piece);
    Assert.AreEqual(10, model.Hp);
    Assert.AreEqual(AttackType.Ballistic, model.AttackType); // or Fire when added
    Assert.IsTrue(model.IdentityTags.Any(t => t.Id == GameTagIds.Assault));
    Assert.IsFalse(model.IdentityTags.Any(t => t.Id == GameTagIds.Combatant));
    Assert.LessOrEqual(model.OptionalTags.Count, 4);
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement view model builder**

`PieceCardViewModel`:

- `Hp`, `BaseDamage`, movement/attack speed tiers
- `AttackType`, `ArmorType` for icon lookup
- `IdentityTags` (Primary, Role, Faction display from registry)
- `OptionalTags` (ability + synergy, max 4)
- `OverflowCount` for `+N`

- [ ] **Step 4: Run — expect PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(ui): piece hover card view model from tag registry"
```

---

### Task 10: Piece hover card MonoBehaviour

**Files:**
- Create: `Assets/_Project/Presentation/Board/PieceHoverCard.cs`
- Modify: board interaction class that handles piece pointer hover (locate during implementation — search `IPointerEnterHandler` under `Presentation/Board`)

- [ ] **Step 1: Wire hover events to show/hide card**

- [ ] **Step 2: Render stat row with TMP + icon sprites**

- [ ] **Step 3: Render tag chips from `TagDefinition.ChipColor` / `DisplayName`**

- [ ] **Step 4: `+N` chip opens tooltip listing overflow tags**

- [ ] **Step 5: Manual playtest** — hover demo pieces in build scene; verify chip cap and hidden system tags

- [ ] **Step 6: Commit**

```bash
git commit -m "feat(ui): piece hover card with stats and tag chips"
```

---

## Phase 7 — Editor validation (minimal v1)

### Task 11: PieceDefinitionSO OnValidate

**Files:**
- Modify: `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs`

- [ ] **Step 1: Add validation**

```csharp
private void OnValidate()
{
    if (string.IsNullOrEmpty(primary))
        Debug.LogWarning($"[{name}] missing Primary tag", this);
    if (!string.IsNullOrEmpty(primary) && !TagRegistry.TryGet(primary, out _))
        Debug.LogWarning($"[{name}] unknown Primary tag '{primary}'", this);
    // Repeat for combatRole, systemTag; validate synergy/ability entries
}
```

- [ ] **Step 2: Open 2–3 piece assets in Unity — confirm warnings for empty/invalid tags**

- [ ] **Step 3: Commit**

```bash
git commit -m "feat(editor): validate piece tags against registry"
```

---

## Phase 8 — Cleanup & documentation

### Task 12: Remove legacy flat Tags path

**Files:**
- Modify: `Assets/_Project/Core/Board/PieceDefinition.cs`
- Modify: `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs`
- Delete or obsolete: `Assets/_Project/Core/Common/GameTags.cs`, `GameKeywords.cs` (only when zero references)

- [ ] **Step 1: Grep for `GameTags.` and `GameKeywords.` — must be zero**

- [ ] **Step 2: Remove `Tags` list from `PieceDefinition` if all consumers use `PieceTagQueries`**

- [ ] **Step 3: Run full EditMode tests — expect PASS**

- [ ] **Step 4: Update `docs/DeadManZone-Game-Design-Document.md` §5 tags table to reference registry IDs**

- [ ] **Step 5: Commit**

```bash
git commit -m "chore(tags): remove legacy flat tag list and old constants"
```

---

## Spec coverage checklist

| Spec section | Task |
|--------------|------|
| §2 Tag categories | Task 1–2 |
| §3 Tag Registry | Task 1 |
| §4 Synergies (tag-owned, snapshot) | Task 3 |
| §5 Critical mass (snapshot) | Task 4 |
| §6 Piece card | Task 9–10 |
| §7 Authoring UX (picker deferred; validate v1) | Task 11; full picker follow-up |
| §8 System consumers | Task 5–8 |
| §9 Migration | Task 7–8, 12 |
| §11 Testing | Each task |
| §12 Implementation order | Phase order above |

**Deferred (explicit):** unified visual tag picker UI (Task 11 is validate-only), shop tag filtering, percent synergy mods, in-combat synergy UI.

---

## Self-review notes

- Fight-start snapshot enforced in Tasks 3–4 with explicit immutability tests.
- `AttackType` / `ArmorType` remain enums; card uses icons via view model, not word chips.
- `PieceTagQueries` is the single query path before legacy removal in Task 12.
- `TestPieces.CreateUnit` extended in Task 3 — update all tests using old signature.
