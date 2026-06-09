# Tag Vocabulary Rework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Swap demo Synergy registry tags for Attack Type registry entries, expand the `AttackType` enum, update combat matchups, show attack type on piece cards, and clear legacy synergy content — without removing `SynergyEngine` or related infrastructure.

**Architecture:** Add `AttackTypeProfileCatalog` as the single source for matchup multipliers and tooltips. Keep `PieceDefinition.AttackType` enum as the canonical authoring field. Register seven `TagCategory.AttackType` entries in `TagRegistry` keyed by lowercase enum names. Clear demo synergy/trait/critical-mass rules and strip `synergyTags` from piece assets. Replace runtime checks that used removed `command`/`supply` tag IDs with `CommandActionFlags` checks.

**Tech Stack:** Unity 6, C#, Unity Test Framework (Edit Mode), asmdefs under `Assets/_Project/`.

**Spec reference:** `docs/superpowers/specs/2026-06-09-tag-vocabulary-rework-design.md`

---

## File map

| Path | Action | Responsibility |
|------|--------|----------------|
| `Assets/_Project/Core/Board/CombatStatEnums.cs` | Modify | Add `Shredding`, `Fire`, `Melee`, `Gas` to `AttackType` |
| `Assets/_Project/Core/Tags/AttackTypeProfile.cs` | Create | Profile data + matchup evaluation |
| `Assets/_Project/Core/Tags/AttackTypeProfileCatalog.cs` | Create | Seven attack type profiles |
| `Assets/_Project/Core/Tags/AttackTypeTags.cs` | Create | Enum ↔ tag ID helpers |
| `Assets/_Project/Core/Tags/TagCategory.cs` | Modify | Add `AttackType` |
| `Assets/_Project/Core/Tags/GameTagIds.cs` | Modify | Remove synergy constants; add attack type ID constants |
| `Assets/_Project/Core/Tags/TagRegistry.cs` | Modify | Remove synergy entries; add attack type entries from catalog |
| `Assets/_Project/Core/Combat/CombatDamageResolver.cs` | Modify | Use `AttackTypeProfileCatalog` for multipliers |
| `Assets/_Project/Core/Tags/PieceTagQueries.cs` | Modify | Add attack type identity chip |
| `Assets/_Project/Presentation/Board/PieceHoverCard.cs` | Modify | Hide stats-row attack type text |
| `Assets/_Project/Core/Tags/TagPickerCatalog.cs` | Modify | Expose `AttackTypeTags` list |
| `Assets/_Project/Core/Tags/SynergyRuleCatalog.cs` | Modify | Empty demo rules array |
| `Assets/_Project/Core/Tags/SynergyTraitRegistry.cs` | Modify | Empty trait catalog |
| `Assets/_Project/Core/Tags/CriticalMassRuleCatalog.cs` | Modify | Remove vanguard synergy rule |
| `Assets/_Project/Core/Run/AuthorityCalculator.cs` | Modify | Count `ChangeStance` flag instead of `command` tag |
| `Assets/_Project/Core/Combat/CommandProcessor.cs` | Modify | Same `ChangeStance` check for tactic validation |
| `Assets/_Project/Game/RunOrchestrator.cs` | Modify | Same `ChangeStance` check for `HasCommandPiece` |
| `Assets/_Project/Core/Combat/PhasedCombatRun.cs` | Modify | Remove legacy supply adjacency bonus |
| `Assets/_Project/Data/Editor/TagContentMigrator.cs` | Modify | Drop synergy mappings; add clear action |
| `Assets/_Project/Data/Resources/DeadManZone/Pieces/radio_array.asset` | Modify | Add `commandActions: ChangeStance` (authority source after tag removal) |
| `Assets/_Project/Core.Tests/EditMode/AttackTypeProfileCatalogTests.cs` | Create | Profile + enum mapping tests |
| `Assets/_Project/Core.Tests/EditMode/TagRegistryTests.cs` | Modify | Attack type + zero synergy assertions |
| `Assets/_Project/Core.Tests/EditMode/CombatDamageResolverTests.cs` | Modify | New matchup tests |
| `Assets/_Project/Core.Tests/EditMode/PieceTagQueriesTests.cs` | Modify | Attack type chip test |
| `Assets/_Project/Core.Tests/EditMode/PieceCardViewModelBuilderTests.cs` | Modify | Update overflow test data |
| `Assets/_Project/Core.Tests/EditMode/SynergyEngineTests.cs` | Modify | Empty-catalog behavior test |
| `Assets/_Project/Core.Tests/EditMode/DemoPieceTagMigrationTests.cs` | Modify | Assert empty synergy tags |
| `Assets/_Project/Core.Tests/EditMode/AuthorityCalculatorTests.cs` | Modify | Use `ChangeStance` on test pieces |
| `Assets/_Project/Core.Tests/EditMode/TestPieces.cs` | Modify | Remove old synergy tag fixtures |

**Test command (full EditMode suite):**

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.0.32f1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform editmode -testResults TestResults-EditMode.xml -quit
```

Adjust Unity path to your installed editor if different. Filter a single fixture:

```powershell
-runTests -testPlatform editmode -testFilter "DeadManZone.Core.Tests.EditMode.CombatDamageResolverTests"
```

---

## Task 1: Attack type profile catalog

**Files:**
- Create: `Assets/_Project/Core/Tags/AttackTypeProfile.cs`
- Create: `Assets/_Project/Core/Tags/AttackTypeProfileCatalog.cs`
- Create: `Assets/_Project/Core/Tags/AttackTypeTags.cs`
- Modify: `Assets/_Project/Core/Board/CombatStatEnums.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/AttackTypeProfileCatalogTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class AttackTypeProfileCatalogTests
    {
        [Test]
        public void Catalog_ContainsSevenAttackTypes()
        {
            Assert.AreEqual(7, AttackTypeProfileCatalog.All.Count);
        }

        [Test]
        public void ToTagId_MapsBallisticEnum()
        {
            Assert.AreEqual(GameTagIds.Ballistic, AttackTypeTags.ToTagId(AttackType.Ballistic));
        }

        [Test]
        public void Ballistic_Tooltip_MentionsMediumAndHeavy()
        {
            var profile = AttackTypeProfileCatalog.Get(AttackType.Ballistic);
            Assert.That(profile.Tooltip, Does.Contain("Medium"));
            Assert.That(profile.Tooltip, Does.Contain("Heavy"));
        }

        [Test]
        public void Enum_IncludesNewValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(AttackType), AttackType.Shredding));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AttackType), AttackType.Fire));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AttackType), AttackType.Melee));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AttackType), AttackType.Gas));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `-testFilter "DeadManZone.Core.Tests.EditMode.AttackTypeProfileCatalogTests"`  
Expected: FAIL — types not found

- [ ] **Step 3: Expand enum**

In `CombatStatEnums.cs`:

```csharp
public enum AttackType
{
    None,
    Ballistic,
    Explosive,
    Piercing,
    Shredding,
    Fire,
    Melee,
    Gas
}
```

- [ ] **Step 4: Implement profile types**

`AttackTypeProfile.cs`:

```csharp
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Tags
{
    public sealed class AttackTypeProfile
    {
        public AttackType AttackType { get; init; }
        public string TagId { get; init; }
        public string DisplayName { get; init; }
        public string Tooltip { get; init; }
        public float StrongMultiplier { get; init; } = 1.25f;
        public float WeakMultiplier { get; init; } = 0.85f;
        public float NeutralMultiplier { get; init; } = 1.0f;
        public ArmorType? StrongArmor { get; init; }
        public ArmorType? WeakArmor { get; init; }
        public string StrongPrimaryTagId { get; init; }
        public string WeakPrimaryTagId { get; init; }
        public bool StrongVsStructures { get; init; }
    }
}
```

`AttackTypeTags.cs`:

```csharp
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Tags
{
    public static class AttackTypeTags
    {
        public static string ToTagId(AttackType attackType)
        {
            if (attackType == AttackType.None)
                return null;

            return attackType.ToString().ToLowerInvariant();
        }

        public static bool TryFromTagId(string tagId, out AttackType attackType)
        {
            attackType = AttackType.None;
            if (string.IsNullOrWhiteSpace(tagId))
                return false;

            foreach (AttackType value in System.Enum.GetValues(typeof(AttackType)))
            {
                if (value == AttackType.None)
                    continue;

                if (string.Equals(ToTagId(value), tagId.Trim(), System.StringComparison.OrdinalIgnoreCase))
                {
                    attackType = value;
                    return true;
                }
            }

            return false;
        }
    }
}
```

`AttackTypeProfileCatalog.cs`:

```csharp
using System.Collections.Generic;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Tags
{
    public static class AttackTypeProfileCatalog
    {
        private static readonly AttackTypeProfile[] Profiles =
        {
            Profile(AttackType.Ballistic, "Ballistic", "Strong vs Medium armor, weak vs Heavy",
                strongArmor: ArmorType.Medium, weakArmor: ArmorType.Heavy, strongMultiplier: 1.25f),
            Profile(AttackType.Piercing, "Piercing", "Strong vs Heavy armor, weak vs Light",
                strongArmor: ArmorType.Heavy, weakArmor: ArmorType.Light, strongMultiplier: 1.35f),
            Profile(AttackType.Shredding, "Shredding", "Strong vs Light armor, weak vs Medium",
                strongArmor: ArmorType.Light, weakArmor: ArmorType.Medium, strongMultiplier: 1.25f),
            Profile(AttackType.Explosive, "Explosive", "Strong vs Heavy armor and structures",
                strongArmor: ArmorType.Heavy, strongVsStructures: true, strongMultiplier: 1.30f),
            Profile(AttackType.Fire, "Fire", "Applies burn status"),
            Profile(AttackType.Melee, "Melee", "Close-quarters attack (matchups TBD)"),
            Profile(AttackType.Gas, "Gas", "Strong vs Infantry, weak vs buildings",
                strongPrimaryTagId: GameTagIds.Infantry, weakPrimaryTagId: GameTagIds.Building, strongMultiplier: 1.25f)
        };

        public static IReadOnlyList<AttackTypeProfile> All { get; } = Profiles;

        public static AttackTypeProfile Get(AttackType attackType)
        {
            for (int i = 0; i < Profiles.Length; i++)
            {
                if (Profiles[i].AttackType == attackType)
                    return Profiles[i];
            }

            return null;
        }

        private static AttackTypeProfile Profile(
            AttackType attackType,
            string displayName,
            string tooltip,
            ArmorType? strongArmor = null,
            ArmorType? weakArmor = null,
            string strongPrimaryTagId = null,
            string weakPrimaryTagId = null,
            bool strongVsStructures = false,
            float strongMultiplier = 1.25f,
            float weakMultiplier = 0.85f)
        {
            return new AttackTypeProfile
            {
                AttackType = attackType,
                TagId = AttackTypeTags.ToTagId(attackType),
                DisplayName = displayName,
                Tooltip = tooltip,
                StrongArmor = strongArmor,
                WeakArmor = weakArmor,
                StrongPrimaryTagId = strongPrimaryTagId,
                WeakPrimaryTagId = weakPrimaryTagId,
                StrongVsStructures = strongVsStructures,
                StrongMultiplier = strongMultiplier,
                WeakMultiplier = weakMultiplier
            };
        }
    }
}
```

Note: Gas weak vs buildings applies to both `building` and `structure` primaries — handle in resolver (Task 3).

- [ ] **Step 5: Run tests — expect PASS**

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Core/Board/CombatStatEnums.cs Assets/_Project/Core/Tags/AttackTypeProfile.cs Assets/_Project/Core/Tags/AttackTypeProfileCatalog.cs Assets/_Project/Core/Tags/AttackTypeTags.cs Assets/_Project/Core.Tests/EditMode/AttackTypeProfileCatalogTests.cs
git commit -m "feat(tags): add attack type profile catalog and enum expansion"
```

---

## Task 2: Tag registry swap

**Files:**
- Modify: `Assets/_Project/Core/Tags/TagCategory.cs`
- Modify: `Assets/_Project/Core/Tags/GameTagIds.cs`
- Modify: `Assets/_Project/Core/Tags/TagRegistry.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/TagRegistryTests.cs`

- [ ] **Step 1: Write failing registry tests**

Add to `TagRegistryTests.cs`:

```csharp
[Test]
public void Registry_ContainsBallisticAttackTypeTag()
{
    var tag = TagRegistry.Get(GameTagIds.Ballistic);
    Assert.AreEqual(TagCategory.AttackType, tag.Category);
    Assert.AreEqual("Ballistic", tag.DisplayName);
}

[Test]
public void Registry_HasNoSynergyTags()
{
    Assert.AreEqual(0, TagRegistry.GetByCategory(TagCategory.Synergy).Count);
}

[Test]
public void Registry_HasSevenAttackTypeTags()
{
    Assert.AreEqual(7, TagRegistry.GetByCategory(TagCategory.AttackType).Count);
}
```

- [ ] **Step 2: Run tests — expect FAIL**

- [ ] **Step 3: Update `TagCategory`**

```csharp
public enum TagCategory
{
    Primary,
    CombatRole,
    Faction,
    System,
    Synergy,
    AttackType
}
```

- [ ] **Step 4: Update `GameTagIds`**

Remove the `// Synergy` region (`Supply` through `Gas`). Add:

```csharp
// Attack type
public const string Ballistic = "ballistic";
public const string Piercing = "piercing";
public const string Shredding = "shredding";
public const string Explosive = "explosive";
public const string Fire = "fire";
public const string Melee = "melee";
public const string Gas = "gas";
```

- [ ] **Step 5: Rewrite synergy section of `TagRegistry`**

Remove lines registering `Supply`…`Gas`. Add a loop registering attack types from catalog:

```csharp
// Attack type — built from AttackTypeProfileCatalog
... RegisterAttackTypes();

private static void RegisterAttackTypes()
{
    foreach (var profile in AttackTypeProfileCatalog.All)
    {
        Catalog[profile.TagId] = Create(
            profile.TagId,
            profile.DisplayName,
            TagCategory.AttackType,
            profile.Tooltip,
            displayPriority: 62);
    }
}
```

Because `TagRegistry` uses a static dictionary initializer, inline the seven entries instead if a helper complicates the static ctor:

```csharp
[GameTagIds.Ballistic] = CreateFromProfile(AttackType.Ballistic),
// ... etc
```

With helper:

```csharp
private static TagDefinition CreateFromProfile(AttackType attackType)
{
    var profile = AttackTypeProfileCatalog.Get(attackType);
    return Create(profile.TagId, profile.DisplayName, TagCategory.AttackType, profile.Tooltip, 62);
}
```

- [ ] **Step 6: Run TagRegistryTests — expect PASS**

- [ ] **Step 7: Commit**

```bash
git commit -m "feat(tags): swap synergy registry entries for attack type tags"
```

---

## Task 3: Combat damage resolver

**Files:**
- Modify: `Assets/_Project/Core/Combat/CombatDamageResolver.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/CombatDamageResolverTests.cs`

- [ ] **Step 1: Replace resolver tests**

Replace `Ballistic_BonusVsLightArmor` with:

```csharp
[Test]
public void Ballistic_StrongVsMediumArmor()
{
    var attacker = TestPieces.With(TestPieces.RifleSquad(), baseDamage: 100, attackType: AttackType.Ballistic);
    var defender = TestPieces.With(TestPieces.RifleSquad(), armorType: ArmorType.Medium);
    int damage = CombatDamageResolver.ComputeDamage(attacker, defender, 1f, 0);
    Assert.AreEqual(106, damage); // 100 * 0.85 medium armor * 1.25 ballistic
}

[Test]
public void Ballistic_WeakVsHeavyArmor()
{
    var attacker = TestPieces.With(TestPieces.RifleSquad(), baseDamage: 100, attackType: AttackType.Ballistic);
    var defender = TestPieces.With(TestPieces.RifleSquad(), armorType: ArmorType.Heavy);
    int damage = CombatDamageResolver.ComputeDamage(attacker, defender, 1f, 0);
    Assert.AreEqual(59, damage); // 100 * 0.70 heavy * 0.85 weak
}

[Test]
public void Piercing_StrongVsHeavy_WeakVsLight()
{
    var vsHeavy = CombatDamageResolver.ComputeDamage(
        TestPieces.With(TestPieces.RifleSquad(), baseDamage: 100, attackType: AttackType.Piercing),
        TestPieces.With(TestPieces.RifleSquad(), armorType: ArmorType.Heavy), 1f, 0);
    var vsLight = CombatDamageResolver.ComputeDamage(
        TestPieces.With(TestPieces.RifleSquad(), baseDamage: 100, attackType: AttackType.Piercing),
        TestPieces.With(TestPieces.RifleSquad(), armorType: ArmorType.Light), 1f, 0);
    Assert.AreEqual(94, vsHeavy);
    Assert.AreEqual(85, vsLight);
}

[Test]
public void Shredding_StrongVsLight_WeakVsMedium()
{
    var vsLight = CombatDamageResolver.ComputeDamage(
        TestPieces.With(TestPieces.RifleSquad(), baseDamage: 100, attackType: AttackType.Shredding),
        TestPieces.With(TestPieces.RifleSquad(), armorType: ArmorType.Light), 1f, 0);
    var vsMedium = CombatDamageResolver.ComputeDamage(
        TestPieces.With(TestPieces.RifleSquad(), baseDamage: 100, attackType: AttackType.Shredding),
        TestPieces.With(TestPieces.RifleSquad(), armorType: ArmorType.Medium), 1f, 0);
    Assert.AreEqual(125, vsLight);
    Assert.AreEqual(72, vsMedium);
}

[Test]
public void Explosive_StrongVsStructurePrimary()
{
    var attacker = TestPieces.With(TestPieces.RifleSquad(), baseDamage: 100, attackType: AttackType.Explosive);
    var structure = TestPieces.CreateUnit("nest", primary: GameTagIds.Structure, systemTag: GameTagIds.Combatant);
    structure = TestPieces.With(structure, armorType: ArmorType.Light);
    int damage = CombatDamageResolver.ComputeDamage(attacker, structure, 1f, 0);
    Assert.AreEqual(130, damage);
}

[Test]
public void Gas_StrongVsInfantry_WeakVsBuilding()
{
    var attacker = TestPieces.With(TestPieces.RifleSquad(), baseDamage: 100, attackType: AttackType.Gas);
    var infantry = TestPieces.CreateUnit("inf", primary: GameTagIds.Infantry, systemTag: GameTagIds.Combatant);
    var building = TestPieces.CreateUnit("depot", primary: GameTagIds.Building, systemTag: GameTagIds.NonCombatant);
    Assert.AreEqual(125, CombatDamageResolver.ComputeDamage(attacker, infantry, 1f, 0));
    Assert.AreEqual(85, CombatDamageResolver.ComputeDamage(attacker, building, 1f, 0));
}
```

Keep `ShieldAllies_BumpsArmorOneTier` unchanged.

- [ ] **Step 2: Run tests — expect FAIL**

- [ ] **Step 3: Implement profile-driven multiplier**

Replace `AttackTypeMultiplier` body:

```csharp
public static float AttackTypeMultiplier(
    AttackType attackType,
    ArmorType armor,
    PieceDefinition defender)
{
    var profile = AttackTypeProfileCatalog.Get(attackType);
    if (profile == null)
        return 1f;

    bool isStructure = PieceTagQueries.HasPrimaryTag(defender, GameTagIds.Building)
        || PieceTagQueries.HasPrimaryTag(defender, GameTagIds.Structure);

    if (profile.StrongArmor.HasValue && armor == profile.StrongArmor.Value)
        return profile.StrongMultiplier;

    if (profile.WeakArmor.HasValue && armor == profile.WeakArmor.Value)
        return profile.WeakMultiplier;

    if (profile.StrongVsStructures && isStructure)
        return profile.StrongMultiplier;

    if (!string.IsNullOrWhiteSpace(profile.StrongPrimaryTagId)
        && PieceTagQueries.HasPrimaryTag(defender, profile.StrongPrimaryTagId))
        return profile.StrongMultiplier;

    if (!string.IsNullOrWhiteSpace(profile.WeakPrimaryTagId)
        && (PieceTagQueries.HasPrimaryTag(defender, profile.WeakPrimaryTagId)
            || (profile.WeakPrimaryTagId == GameTagIds.Building
                && PieceTagQueries.HasPrimaryTag(defender, GameTagIds.Structure))))
        return profile.WeakMultiplier;

    return profile.NeutralMultiplier;
}
```

- [ ] **Step 4: Run CombatDamageResolverTests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(combat): drive attack type matchups from profile catalog"
```

---

## Task 4: Player card attack type chip

**Files:**
- Modify: `Assets/_Project/Core/Tags/PieceTagQueries.cs`
- Modify: `Assets/_Project/Presentation/Board/PieceHoverCard.cs`
- Modify: `Assets/_Project/Core/Tags/TagPickerCatalog.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/PieceTagQueriesTests.cs`

- [ ] **Step 1: Add failing test**

In `PieceTagQueriesTests.cs`:

```csharp
[Test]
public void GetPlayerVisibleTags_IncludesAttackTypeChip()
{
    var piece = new PieceDefinition
    {
        Primary = GameTagIds.Infantry,
        CombatRole = GameTagIds.Assault,
        SystemTag = GameTagIds.Combatant,
        AttackType = AttackType.Piercing,
        FactionId = "neutral"
    };

    var result = PieceTagQueries.GetPlayerVisibleTags(piece, maxOptionalChips: 4);
    Assert.IsTrue(result.IdentityTags.Any(t => t.Id == GameTagIds.Piercing));
    Assert.IsFalse(result.IdentityTags.Any(t => t.Id == GameTagIds.Combatant));
}
```

Add `using System.Linq;` and `using DeadManZone.Core.Board;`.

- [ ] **Step 2: Run test — expect FAIL**

- [ ] **Step 3: Add chip in `GetPlayerVisibleTags`**

After `AddFactionTag(identityTags, seen, piece.FactionId);` insert:

```csharp
AddAttackTypeTag(identityTags, seen, piece.AttackType);
```

Add method:

```csharp
private static void AddAttackTypeTag(List<TagDefinition> destination, HashSet<string> seen, AttackType attackType)
{
    if (attackType == AttackType.None)
        return;

    string tagId = AttackTypeTags.ToTagId(attackType);
    AddVisibleTag(destination, seen, tagId, TagCategory.AttackType, 62, "Attack type identity tag.");
}
```

Add `using DeadManZone.Core.Board;` if missing.

- [ ] **Step 4: Hide stats-row attack type text in `PieceHoverCard.Bind`**

Replace:

```csharp
SetText(attackTypeText, $"Attack Type: {FormatAttackType(model.AttackType)}");
```

With:

```csharp
if (attackTypeText != null)
    attackTypeText.gameObject.SetActive(false);
```

- [ ] **Step 5: Expose attack types in picker**

In `TagPickerCatalog.cs`:

```csharp
public static IReadOnlyList<TagDefinition> AttackTypeTags => TagRegistry.GetByCategory(TagCategory.AttackType);
```

- [ ] **Step 6: Run PieceTagQueriesTests — expect PASS**

- [ ] **Step 7: Commit**

```bash
git commit -m "feat(ui): show attack type as identity chip on piece card"
```

---

## Task 5: Clear demo synergy data

**Files:**
- Modify: `Assets/_Project/Core/Tags/SynergyRuleCatalog.cs`
- Modify: `Assets/_Project/Core/Tags/SynergyTraitRegistry.cs`
- Modify: `Assets/_Project/Core/Tags/CriticalMassRuleCatalog.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/SynergyEngineTests.cs`

- [ ] **Step 1: Empty synergy rule catalog**

```csharp
private static readonly SynergyEffectDefinition[] Rules = System.Array.Empty<SynergyEffectDefinition>();
```

- [ ] **Step 2: Empty trait registry**

```csharp
private static readonly IReadOnlyDictionary<string, SynergyTraitThresholds> Catalog =
    new Dictionary<string, SynergyTraitThresholds>(StringComparer.OrdinalIgnoreCase);
```

- [ ] **Step 3: Remove vanguard rule from critical mass**

Delete the `GameTagIds.Vanguard` entry from `DemoRules` array (keep infantry, vehicle, artillery, assault rules).

- [ ] **Step 4: Update synergy engine tests**

Replace both tests with:

```csharp
[Test]
public void EmptyRuleCatalog_ProducesZeroBonuses()
{
    var rifle = TestPieces.CreateUnit("rifle", primary: GameTagIds.Infantry, systemTag: GameTagIds.Combatant);
    var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
    var board = new BoardState(layout);
    board.TryPlace(rifle, TestBoards.SupportLineAnchor(0), "rifle_1");

    var snapshot = SynergyEngine.EvaluateFightStart(board);
    Assert.IsFalse(snapshot.TryGet("rifle_1", out var result) && result.DamageBonus > 0);
}
```

- [ ] **Step 5: Run SynergyEngineTests + CriticalMassRulesTests — expect PASS**

- [ ] **Step 6: Commit**

```bash
git commit -m "chore(tags): clear demo synergy rules and trait thresholds"
```

---

## Task 6: Retire Command/Supply tag runtime checks

**Files:**
- Modify: `Assets/_Project/Core/Run/AuthorityCalculator.cs`
- Modify: `Assets/_Project/Core/Combat/CommandProcessor.cs`
- Modify: `Assets/_Project/Game/RunOrchestrator.cs`
- Modify: `Assets/_Project/Core/Combat/PhasedCombatRun.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/AuthorityCalculatorTests.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/TestPieces.cs`
- Modify: `Assets/_Project/Data/Resources/DeadManZone/Pieces/radio_array.asset`

- [ ] **Step 1: Add shared helper** (inline in each file or extract to `CommandPieceQueries.cs`):

```csharp
private static bool HasCommandPiece(BoardState board) =>
    board?.Pieces != null && board.Pieces.Any(p =>
        p.Definition.CommandActions.HasFlag(CommandActionFlags.ChangeStance));
```

- [ ] **Step 2: Update `AuthorityCalculator`**

```csharp
pool += board.Pieces.Count(p =>
    p.Definition.CommandActions.HasFlag(CommandActionFlags.ChangeStance));
```

Remove `using DeadManZone.Core.Tags;` if no longer needed.

- [ ] **Step 3: Update `CommandProcessor` line 83** — replace `GameTagIds.Command` check with `ChangeStance` flag check.

- [ ] **Step 4: Update `RunOrchestrator` `HasCommandPiece`** — same flag check.

- [ ] **Step 5: Remove legacy supply adjacency in `PhasedCombatRun`**

Delete calls to `ApplyAdjacencyBonuses` at fight start (lines ~68–69) and delete the private method entirely. Fight-start synergy bonuses remain the job of `SynergyEngine` when rules exist again.

- [ ] **Step 6: Fix `TestPieces.CommandBunker`**

Ensure it has `CommandActions = CommandActionFlags.ChangeStance` (already present). Remove `Tags = new[] { GameTagIds.Command }`.

- [ ] **Step 7: Fix `AuthorityCalculatorTests` radio fixture**

```csharp
var radio = new PieceDefinition
{
    Id = "radio_array",
    DisplayName = "Radio Array",
    Category = PieceCategory.Building,
    Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
    CommandActions = CommandActionFlags.ChangeStance,
    MaxHp = 12
};
```

- [ ] **Step 8: Set `radio_array.asset` `commandActions` to `ChangeStance` (enum value 1)**

- [ ] **Step 9: Run AuthorityCalculatorTests — expect PASS**

- [ ] **Step 10: Commit**

```bash
git commit -m "refactor: replace command/supply tag checks with command action flags"
```

---

## Task 7: Update remaining tests and fixtures

**Files:**
- Modify: `Assets/_Project/Core.Tests/EditMode/PieceTagQueriesTests.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/PieceCardViewModelBuilderTests.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/DemoPieceTagMigrationTests.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/TestPieces.cs`

- [ ] **Step 1: Fix `PieceTagQueriesTests`**

Remove vanguard synergy assertion test or replace with:

```csharp
SynergyTags = System.Array.Empty<string>()
// Assert no synergy tags present
Assert.IsFalse(PieceTagQueries.HasSynergyTag(piece, "vanguard"));
```

- [ ] **Step 2: Fix `PieceCardViewModelBuilderTests`**

Replace removed synergy tag IDs in `SynergyTags` with a single custom ability tag only:

```csharp
AbilityTags = new[] { "flamethrower" },
SynergyTags = System.Array.Empty<string>()
```

Adjust overflow assertions: `OptionalTags.Count` should be 1, `OverflowCount` 0. Add:

```csharp
Assert.IsTrue(model.IdentityTags.Any(t => t.Id == GameTagIds.Ballistic));
```

- [ ] **Step 3: Fix `DemoPieceTagMigrationTests`**

```csharp
Assert.IsEmpty(rifleSo.synergyTags);
Assert.IsFalse(PieceTagQueries.HasSynergyTag(rifle, GameTagIds.Vanguard));
```

Remove reference to `GameTagIds.Vanguard` if constant deleted — use string `"vanguard"` in negative assertion or assert empty synergy list only.

- [ ] **Step 4: Clean `TestPieces` helpers**

Remove or rewrite `SupplyDepot`, `FieldWorkshop`, `GasDrone`, `CommandBunker` legacy tag arrays that reference removed IDs. Use `CommandActions` / `AttackType.Gas` instead where tests need those concepts.

- [ ] **Step 5: Run full EditMode suite — fix compile errors from removed `GameTagIds.*` synergy constants**

Search: `GameTagIds\.(Supply|Medic|Command|Echo|Stealth|Vanguard|Mechanical|Gas)`

- [ ] **Step 6: Commit**

```bash
git commit -m "test: update fixtures for tag vocabulary rework"
```

---

## Task 8: Content migration

**Files:**
- Modify: `Assets/_Project/Data/Editor/TagContentMigrator.cs`
- Modify: all piece `.asset` files under `Assets/_Project/Data/Resources/DeadManZone/Pieces/` that have non-empty `synergyTags`

- [ ] **Step 1: Simplify `PieceTagMapping`**

Remove synergy tag parameters from all `PieceMappings` entries — only primary, combatRole, systemTag remain.

- [ ] **Step 2: Remove old synergy strings from `KnownLegacyTags`**

Remove: `Supply`, `Medic`, `Command`, `Echo`, `Stealth`, `Vanguard`, `Mechanical`, `Gas`.

- [ ] **Step 3: Add menu action**

```csharp
[MenuItem("DeadManZone/Clear Legacy Synergy Tags")]
public static void ClearLegacySynergyTags()
{
    string[] guids = AssetDatabase.FindAssets("t:PieceDefinitionSO", new[] { PieceRoot });
    int cleared = 0;
    foreach (string guid in guids)
    {
        var piece = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
        if (piece == null || piece.synergyTags == null || piece.synergyTags.Length == 0)
            continue;

        piece.synergyTags = System.Array.Empty<string>();
        EditorUtility.SetDirty(piece);
        cleared++;
    }

    if (cleared > 0)
        AssetDatabase.SaveAssets();

    Debug.Log($"[TagContentMigrator] Cleared synergy tags on {cleared} pieces.");
}
```

- [ ] **Step 4: Run in Unity editor** — DeadManZone → Clear Legacy Synergy Tags

Or hand-edit YAML: set `synergyTags: []` on these 14 assets:  
`rifle_squad`, `diesel_walker`, `radio_array`, `supply_depot`, `field_workshop`, `mobile_artillery`, `field_medic`, `sand_raider`, `toxin_launcher`, `phantom_agent`, `signal_relay`, `resonance_cannon`, `wraith_stalker`, `wraith_phantom`, `wraith_bombard`.

- [ ] **Step 5: Run DemoPieceTagMigrationTests — expect PASS**

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Data/Editor/TagContentMigrator.cs Assets/_Project/Data/Resources/DeadManZone/Pieces/
git commit -m "chore(content): clear legacy synergy tags from demo pieces"
```

---

## Task 9: Full regression

- [ ] **Step 1: Run full EditMode suite**

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.0.32f1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform editmode -testResults TestResults-EditMode.xml -quit
```

Expected: all tests pass; zero failures referencing removed synergy constants.

- [ ] **Step 2: Manual smoke check in Unity**

1. Enter Play Mode on build screen.  
2. Hover a combat piece — identity chips show Primary, Role, Faction, **Attack Type**; no `"Attack Type: …"` text line.  
3. Synergy side panel shows no demo traits.  
4. Place 3 infantry — critical mass damage bonus still applies.

- [ ] **Step 3: Final commit if any fixups needed**

```bash
git commit -m "chore: tag vocabulary rework regression pass"
```

---

## Spec coverage checklist

| Spec requirement | Task |
|------------------|------|
| Remove 8 synergy registry entries | Task 2 |
| Add 7 attack type registry entries | Task 2 |
| Expand AttackType enum | Task 1 |
| AttackTypeProfileCatalog shared by resolver + tooltips | Task 1, 3 |
| New combat matchups | Task 3 |
| Attack type identity chip | Task 4 |
| Remove stats-row attack type text | Task 4 |
| Clear SynergyRuleCatalog | Task 5 |
| Clear SynergyTraitRegistry | Task 5 |
| Remove vanguard critical mass rule | Task 5 |
| Strip piece synergyTags | Task 8 |
| Undecided tags not implemented | N/A (spec appendix only) |
| Fire/Melee neutral multipliers | Task 1 profile + Task 3 |
| Replace Command/Supply runtime tag checks | Task 6 |

---

## Execution handoff

Plan complete and saved to `docs/superpowers/plans/2026-06-09-tag-vocabulary-rework.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — fresh subagent per task, review between tasks, fast iteration  
2. **Inline Execution** — execute tasks in this session with checkpoints

Which approach do you want?
