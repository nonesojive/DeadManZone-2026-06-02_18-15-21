> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a playable Unity vertical slice: faction select → 5-fight linear gauntlet with zoned grid loadout, 3-lane shop, 3-phase deterministic combat with between-phase commands, and mid-run save/resume.

**Architecture:** Pure C# `DeadManZone.Core` sim (no Unity refs) handles board, shop, combat, commands, and save schema. Thin `DeadManZone.Game` orchestrates run flow. `DeadManZone.Presentation` replays combat events and handles UI. ScriptableObjects in `DeadManZone.Data` bootstrap content into Core structs at runtime.

**Tech Stack:** Unity 2022.3 LTS (or newer LTS), C# 10+, Unity Test Framework (Edit Mode + Play Mode), Newtonsoft.Json (Unity NuGet or `com.unity.nuget.newtonsoft-json`), TextMeshPro, Unity UI (uGUI).

---

## File Map

| Path | Responsibility |
|------|----------------|
| `Assets/_Project/Core/DeadManZone.Core.asmdef` | Core assembly, no Unity engine refs |
| `Assets/_Project/Core/Common/Rng.cs` | Seeded deterministic RNG |
| `Assets/_Project/Core/Common/GridCoord.cs` | Grid coordinate struct |
| `Assets/_Project/Core/Common/Tags.cs` | Tag constants |
| `Assets/_Project/Core/Board/ZoneType.cs` | Rear, Support, Front enum |
| `Assets/_Project/Core/Board/PieceShape.cs` | Shape cells relative to anchor |
| `Assets/_Project/Core/Board/PieceDefinition.cs` | Data-only piece template |
| `Assets/_Project/Core/Board/PlacedPiece.cs` | Instance on board |
| `Assets/_Project/Core/Board/BoardLayout.cs` | Zone masks + special tile masks |
| `Assets/_Project/Core/Board/BoardState.cs` | Grid occupancy, placement, adjacency |
| `Assets/_Project/Core/Shop/ShopOffer.cs` | Single shop listing |
| `Assets/_Project/Core/Shop/ShopLane.cs` | Lane id enum |
| `Assets/_Project/Core/Shop/ShopState.cs` | Current offers, freeze, reroll count |
| `Assets/_Project/Core/Shop/ShopGenerator.cs` | Pool rolls + building modifiers |
| `Assets/_Project/Core/Combat/CombatPhase.cs` | Phase enum |
| `Assets/_Project/Core/Combat/CombatEvent.cs` | Event log entry |
| `Assets/_Project/Core/Combat/CombatantState.cs` | Runtime HP, cooldowns per piece |
| `Assets/_Project/Core/Combat/StanceType.cs` | Stance enum |
| `Assets/_Project/Core/Combat/PhaseCommand.cs` | Player command payload |
| `Assets/_Project/Core/Combat/CommandProcessor.cs` | Validate/apply commands |
| `Assets/_Project/Core/Combat/CombatResolver.cs` | 3-phase sim loop |
| `Assets/_Project/Core/Run/RunPhase.cs` | Build, Combat, Aftermath enum |
| `Assets/_Project/Core/Run/RunState.cs` | Full run snapshot |
| `Assets/_Project/Core/Run/RunSaveSerializer.cs` | JSON serialize/deserialize |
| `Assets/_Project/Core/Content/ContentRegistry.cs` | Id → definition lookup |
| `Assets/_Project/Core.Tests/EditMode/*.cs` | Core unit tests |
| `Assets/_Project/Data/ScriptableObjects/*.cs` | PieceDefinitionSO, FactionSO, EnemyTemplateSO |
| `Assets/_Project/Data/ContentDatabase.cs` | SO → Core registry bootstrap |
| `Assets/_Project/Game/RunManager.cs` | Gauntlet flow, auto-save hooks |
| `Assets/_Project/Game/SaveManager.cs` | Read/write persistent save file |
| `Assets/_Project/Presentation/Board/BoardView.cs` | Grid rendering + drag-drop |
| `Assets/_Project/Presentation/Shop/ShopView.cs` | 3-lane shop UI |
| `Assets/_Project/Presentation/Combat/CombatDirector.cs` | Event log playback |
| `Assets/_Project/Presentation/Combat/PhaseCommandPanel.cs` | Between-phase UI |
| `Assets/_Project/Scenes/MainMenu.unity` | Continue / New Run |
| `Assets/_Project/Scenes/Run.unity` | Board + shop + combat overlay |

---

## Phase 0 — Unity Project Bootstrap

### Task 0: Create Unity project and folder structure

**Files:**
- Create: Unity project at repo root (2D or 3D template — either works; UI is uGUI)
- Create: folder tree under `Assets/_Project/` per file map above
- Create: `Assets/_Project/Core/DeadManZone.Core.asmdef`
- Create: `Assets/_Project/Core.Tests/DeadManZone.Core.Tests.asmdef`
- Create: `Assets/_Project/Game/DeadManZone.Game.asmdef`
- Create: `Assets/_Project/Presentation/DeadManZone.Presentation.asmdef`
- Create: `Assets/_Project/Data/DeadManZone.Data.asmdef`

- [ ] **Step 1: Create Unity project**

Create project named `DeadManZone` in the repo root via Unity Hub (2022.3 LTS recommended).

- [ ] **Step 2: Add assembly definitions**

`Assets/_Project/Core/DeadManZone.Core.asmdef`:

```json
{
  "name": "DeadManZone.Core",
  "rootNamespace": "DeadManZone.Core",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "autoReferenced": true
}
```

`Assets/_Project/Core.Tests/DeadManZone.Core.Tests.asmdef`:

```json
{
  "name": "DeadManZone.Core.Tests",
  "rootNamespace": "DeadManZone.Core.Tests",
  "references": [
    "DeadManZone.Core",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": ["Editor"],
  "optionalUnityReferences": ["TestAssemblies"]
}
```

`Assets/_Project/Game/DeadManZone.Game.asmdef` — references: `DeadManZone.Core`, `DeadManZone.Data`

`Assets/_Project/Presentation/DeadManZone.Presentation.asmdef` — references: `DeadManZone.Core`, `DeadManZone.Game`, `DeadManZone.Data`

`Assets/_Project/Data/DeadManZone.Data.asmdef` — references: `DeadManZone.Core`

- [ ] **Step 3: Add Newtonsoft.Json**

Window → Package Manager → Add package by name: `com.unity.nuget.newtonsoft-json`

- [ ] **Step 4: Enable Test Framework**

Verify `com.unity.test-framework` is installed (included by default).

- [ ] **Step 5: Commit**

```bash
git add Assets/ ProjectSettings/ Packages/
git commit -m "chore: bootstrap Unity project and assembly layout"
```

---

## Phase 1 — Core Foundation

### Task 1: Deterministic RNG

**Files:**
- Create: `Assets/_Project/Core/Common/Rng.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/RngTests.cs`

- [ ] **Step 1: Write the failing test**

`Assets/_Project/Core.Tests/EditMode/RngTests.cs`:

```csharp
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public class RngTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var a = new Rng(12345);
            var b = new Rng(12345);
            Assert.AreEqual(a.NextInt(0, 100), b.NextInt(0, 100));
            Assert.AreEqual(a.NextInt(0, 100), b.NextInt(0, 100));
        }

        [Test]
        public void NextInt_RespectsBounds()
        {
            var rng = new Rng(1);
            for (int i = 0; i < 100; i++)
            {
                int v = rng.NextInt(2, 5);
                Assert.That(v, Is.InRange(2, 4));
            }
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Unity: Window → General → Test Runner → EditMode → Run `RngTests`
Expected: FAIL — `Rng` not found

- [ ] **Step 3: Implement Rng**

`Assets/_Project/Core/Common/Rng.cs`:

```csharp
namespace DeadManZone.Core.Common
{
    /// <summary>Deterministic Xorshift32 — same seed, same sequence across platforms.</summary>
    public sealed class Rng
    {
        private uint _state;

        public Rng(int seed)
        {
            _state = seed == 0 ? 1u : (uint)seed;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new System.ArgumentOutOfRangeException(nameof(maxExclusive));

            uint range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        public float NextFloat()
        {
            return NextUInt() / (float)uint.MaxValue;
        }

        private uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }
    }
}
```

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Core/Common/Rng.cs Assets/_Project/Core.Tests/EditMode/RngTests.cs
git commit -m "feat(core): add deterministic RNG"
```

---

### Task 2: Grid coordinates and piece shapes

**Files:**
- Create: `Assets/_Project/Core/Common/GridCoord.cs`
- Create: `Assets/_Project/Core/Board/PieceShape.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/PieceShapeTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public class PieceShapeTests
    {
        [Test]
        public void GetCells_ReturnsAnchorPlusOffsets()
        {
            var shape = new PieceShape(new[] { new GridCoord(0, 0), new GridCoord(1, 0) });
            var cells = shape.GetCells(new GridCoord(2, 3)).ToList();
            CollectionAssert.AreEquivalent(
                new[] { new GridCoord(2, 3), new GridCoord(3, 3) },
                cells);
        }
    }
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement**

`Assets/_Project/Core/Common/GridCoord.cs`:

```csharp
namespace DeadManZone.Core.Common
{
    public readonly struct GridCoord
    {
        public int X { get; }
        public int Y { get; }

        public GridCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj) =>
            obj is GridCoord other && X == other.X && Y == other.Y;

        public override int GetHashCode() => (X * 397) ^ Y;
    }
}
```

`Assets/_Project/Core/Board/PieceShape.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Board
{
    public sealed class PieceShape
    {
        private readonly GridCoord[] _cells;

        public PieceShape(IEnumerable<GridCoord> cells)
        {
            _cells = cells.ToArray();
        }

        public IEnumerable<GridCoord> GetCells(GridCoord anchor)
        {
            foreach (var c in _cells)
                yield return new GridCoord(anchor.X + c.X, anchor.Y + c.Y);
        }
    }
}
```

- [ ] **Step 4: Run — expect PASS**

- [ ] **Step 5: Commit**

```bash
git commit -am "feat(core): add grid coords and piece shapes"
```

---

### Task 3: Board layout, zones, and placement validation

**Files:**
- Create: `Assets/_Project/Core/Board/ZoneType.cs`
- Create: `Assets/_Project/Core/Board/PieceDefinition.cs`
- Create: `Assets/_Project/Core/Board/PlacedPiece.cs`
- Create: `Assets/_Project/Core/Board/BoardLayout.cs`
- Create: `Assets/_Project/Core/Board/BoardState.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/BoardStateTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public class BoardStateTests
    {
        private static BoardLayout DefaultLayout() =>
            BoardLayout.CreateStandard(width: 8, height: 6, rearRows: 2, supportRows: 2, specialTiles: new[]
            {
                new GridCoord(1, 2), new GridCoord(4, 2)
            });

        [Test]
        public void CannotPlaceUnitInRearZone()
        {
            var layout = DefaultLayout();
            var board = new BoardState(layout);
            var rifle = TestPieces.RifleSquad();
            var result = board.TryPlace(rifle, new GridCoord(0, 0));
            Assert.IsFalse(result.Success);
            Assert.That(result.Reason, Does.Contain("zone"));
        }

        [Test]
        public void BuildingOnSpecialTile_FlagsSpecialBonus()
        {
            var layout = DefaultLayout();
            var board = new BoardState(layout);
            var bunker = TestPieces.CommandBunker();
            Assert.IsTrue(board.TryPlace(bunker, new GridCoord(1, 2)).Success);
            Assert.IsTrue(board.IsOnSpecialTile(bunker.InstanceId));
        }
    }
}
```

Also create `Assets/_Project/Core.Tests/EditMode/TestPieces.cs` with minimal `PieceDefinition` factories used by tests.

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement board types**

`ZoneType.cs`:

```csharp
namespace DeadManZone.Core.Board
{
    public enum ZoneType { Rear, Support, Front }

    public enum PieceCategory { Building, Unit, Hybrid }
}
```

`PieceDefinition.cs` (keep under 80 lines):

```csharp
using System.Collections.Generic;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Board
{
    public sealed class PieceDefinition
    {
        public string Id { get; init; }
        public string DisplayName { get; init; }
        public PieceCategory Category { get; init; }
        public PieceShape Shape { get; init; }
        public IReadOnlyList<string> Tags { get; init; }
        public int MaxHp { get; init; }
        public int BaseDamage { get; init; }
        public int CooldownTicks { get; init; }
        public int GoldCost { get; init; }
        public int RequisitionCost { get; init; }
        public ShopModifierFlags ShopModifiers { get; init; }
        public CommandActionFlags CommandActions { get; init; }
    }

    [System.Flags]
    public enum ShopModifierFlags
    {
        None = 0,
        ExtraGeneralSlot = 1 << 0,
        GoldDiscount10 = 1 << 1,
        EnemyTagPreview = 1 << 2,
        GuaranteeEngineerOffer = 1 << 3,
    }

    [System.Flags]
    public enum CommandActionFlags
    {
        None = 0,
        ChangeStance = 1 << 0,
        SpendRequisitionBuff = 1 << 1,
        CallStrike = 1 << 2,
    }
}
```

`PlacedPiece.cs`:

```csharp
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Board
{
    public sealed class PlacedPiece
    {
        public string InstanceId { get; init; }
        public PieceDefinition Definition { get; init; }
        public GridCoord Anchor { get; init; }
    }
}
```

`BoardLayout.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Board
{
    public sealed class BoardLayout
    {
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<GridCoord> SpecialTiles { get; }

        private readonly ZoneType[,] _zones;

        private BoardLayout(int width, int height, ZoneType[,] zones, List<GridCoord> specialTiles)
        {
            Width = width;
            Height = height;
            _zones = zones;
            SpecialTiles = specialTiles;
        }

        public static BoardLayout CreateStandard(int width, int height, int rearRows, int supportRows, GridCoord[] specialTiles)
        {
            var zones = new ZoneType[width, height];
            int frontRows = height - rearRows - supportRows;
            for (int y = 0; y < height; y++)
            {
                ZoneType zone = y < rearRows ? ZoneType.Rear
                    : y < rearRows + supportRows ? ZoneType.Support
                    : ZoneType.Front;
                for (int x = 0; x < width; x++)
                    zones[x, y] = zone;
            }
            return new BoardLayout(width, height, zones, specialTiles.ToList());
        }

        public ZoneType GetZone(GridCoord c) => _zones[c.X, c.Y];

        public bool IsSpecialTile(GridCoord c) =>
            SpecialTiles.Any(t => t.X == c.X && t.Y == c.Y);
    }
}
```

`BoardState.cs` (placement + adjacency — split adjacency to `BoardAdjacency.cs` if file exceeds ~200 lines):

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Board
{
    public readonly struct PlacementResult
    {
        public bool Success { get; init; }
        public string Reason { get; init; }
    }

    public sealed class BoardState
    {
        private readonly Dictionary<string, PlacedPiece> _pieces = new();
        private readonly HashSet<GridCoord> _occupied = new();

        public BoardLayout Layout { get; }
        public IReadOnlyCollection<PlacedPiece> Pieces => _pieces.Values;

        public BoardState(BoardLayout layout) => Layout = layout;

        public PlacementResult TryPlace(PieceDefinition def, GridCoord anchor, string instanceId = null)
        {
            instanceId ??= Guid.NewGuid().ToString("N");
            foreach (var cell in def.Shape.GetCells(anchor))
            {
                if (cell.X < 0 || cell.Y < 0 || cell.X >= Layout.Width || cell.Y >= Layout.Height)
                    return new PlacementResult { Success = false, Reason = "Out of bounds" };

                if (_occupied.Contains(cell))
                    return new PlacementResult { Success = false, Reason = "Cell occupied" };

                if (!IsCategoryAllowed(def.Category, Layout.GetZone(cell)))
                    return new PlacementResult { Success = false, Reason = "Invalid zone for category" };
            }

            foreach (var cell in def.Shape.GetCells(anchor))
                _occupied.Add(cell);

            _pieces[instanceId] = new PlacedPiece
            {
                InstanceId = instanceId,
                Definition = def,
                Anchor = anchor
            };
            return new PlacementResult { Success = true };
        }

        public bool IsOnSpecialTile(string instanceId)
        {
            var piece = _pieces[instanceId];
            return piece.Definition.Shape.GetCells(piece.Anchor)
                .Any(c => Layout.IsSpecialTile(c));
        }

        private static bool IsCategoryAllowed(PieceCategory cat, ZoneType zone) =>
            zone switch
            {
                ZoneType.Rear => cat is PieceCategory.Building or PieceCategory.Hybrid,
                ZoneType.Front => cat is PieceCategory.Unit or PieceCategory.Hybrid,
                ZoneType.Support => true,
                _ => false
            };

        public IEnumerable<string> GetAdjacentInstanceIds(string instanceId) =>
            BoardAdjacency.GetTouchingPairs(_pieces.Values)
                .Where(p => p.A == instanceId || p.B == instanceId)
                .Select(p => p.A == instanceId ? p.B : p.A);
    }
}
```

Fix `IsOnSpecialTile` — reference `piece.Definition` not `def`. Add `BoardAdjacency.cs` with edge-touch detection.

- [ ] **Step 4: Fix compile errors, run tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git commit -am "feat(core): board zones and placement validation"
```

---

## Phase 2 — Shop System

### Task 4: Shop generation with building modifiers

**Files:**
- Create: `Assets/_Project/Core/Shop/ShopLane.cs`
- Create: `Assets/_Project/Core/Shop/ShopOffer.cs`
- Create: `Assets/_Project/Core/Shop/ShopState.cs`
- Create: `Assets/_Project/Core/Shop/ShopGenerator.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/ShopGeneratorTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Test]
public void SupplyDepot_AppliesGoldDiscount()
{
    var board = BuildBoardWithSupplyDepot();
    var registry = TestContentRegistry.Create();
    var gen = new ShopGenerator(registry);
    var shop = gen.Generate(board, factionId: "iron_vanguard", round: 2, seed: 999);

    Assert.That(shop.Modifiers.GoldDiscountPercent, Is.GreaterThanOrEqualTo(10));
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement ShopGenerator**

Key logic in `ShopGenerator.cs`:

```csharp
public ShopState Generate(BoardState board, string factionId, int round, int seed)
{
    var rng = new Rng(seed);
    var modifiers = ComputeModifiers(board);
    var offers = new List<ShopOffer>();

    RollLane(ShopLane.General, 5, modifiers, rng, offers, round);
    RollLane(ShopLane.Engineers, 4, modifiers, rng, offers, round);
    RollLane(ShopLane.Requisition, 4, modifiers, rng, offers, round);

    return new ShopState { Offers = offers, Modifiers = modifiers, Seed = seed };
}

private ShopModifiers ComputeModifiers(BoardState board)
{
    int discount = 0;
    int extraGeneralSlots = 0;
    bool preview = false;
    bool guaranteeEngineer = false;

    foreach (var piece in board.Pieces)
    {
        var flags = piece.Definition.ShopModifiers;
        if (flags.HasFlag(ShopModifierFlags.GoldDiscount10)) discount += 10;
        if (flags.HasFlag(ShopModifierFlags.ExtraGeneralSlot)) extraGeneralSlots += 1;
        if (flags.HasFlag(ShopModifierFlags.EnemyTagPreview)) preview = true;
        if (flags.HasFlag(ShopModifierFlags.GuaranteeEngineerOffer)) guaranteeEngineer = true;
    }
    discount = System.Math.Min(discount, 25);
    return new ShopModifiers { GoldDiscountPercent = discount, ExtraGeneralSlots = extraGeneralSlots, /* ... */ };
}
```

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git commit -am "feat(core): shop generation with building modifiers"
```

---

## Phase 3 — Combat Simulation

### Task 5: Combat event log and combatant state

**Files:**
- Create: `Assets/_Project/Core/Combat/CombatPhase.cs`
- Create: `Assets/_Project/Core/Combat/CombatEvent.cs`
- Create: `Assets/_Project/Core/Combat/CombatantState.cs`
- Create: `Assets/_Project/Core/Combat/StanceType.cs`

- [ ] **Step 1: Write failing test for event log append**

```csharp
[Test]
public void EventLog_Append_IncrementsTick()
{
    var log = new CombatEventLog();
    log.Append(CombatPhase.Deployment, 0, "a1", "move", null, 0);
    Assert.AreEqual(1, log.Events.Count);
}
```

- [ ] **Step 2–5: Implement, test, commit**

`CombatEvent.cs`:

```csharp
namespace DeadManZone.Core.Combat
{
    public enum CombatPhase { Deployment = 1, Grind = 2, FinalPush = 3 }

    public sealed class CombatEvent
    {
        public CombatPhase Phase { get; init; }
        public int Tick { get; init; }
        public string ActorId { get; init; }
        public string ActionType { get; init; }
        public string TargetId { get; init; }
        public int Value { get; init; }
    }

    public sealed class CombatEventLog
    {
        public List<CombatEvent> Events { get; } = new();

        public void Append(CombatPhase phase, int tick, string actorId, string actionType, string targetId, int value) =>
            Events.Add(new CombatEvent { Phase = phase, Tick = tick, ActorId = actorId, ActionType = actionType, TargetId = targetId, Value = value });
    }
}
```

---

### Task 6: Combat resolver — three phases

**Files:**
- Create: `Assets/_Project/Core/Combat/CombatResolver.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/CombatResolverTests.cs`

- [ ] **Step 1: Write determinism test**

```csharp
[Test]
public void SameSeedAndBoards_IdenticalEventLog()
{
    var resolver = new CombatResolver(TestContentRegistry.Create());
    var boardA = TestBoards.StandardPlayer();
    var boardB = TestBoards.StandardEnemy();
    var log1 = resolver.Resolve(boardA, boardB, seed: 42, commands: System.Array.Empty<PhaseCommand>());
    var log2 = resolver.Resolve(boardA, boardB, seed: 42, commands: System.Array.Empty<PhaseCommand>());
    Assert.AreEqual(log1.Events.Count, log2.Events.Count);
    for (int i = 0; i < log1.Events.Count; i++)
    {
        Assert.AreEqual(log1.Events[i].ActionType, log2.Events[i].ActionType);
        Assert.AreEqual(log1.Events[i].Value, log2.Events[i].Value);
    }
}
```

- [ ] **Step 2: Implement minimal CombatResolver**

Phase loop skeleton:

```csharp
public CombatResult Resolve(BoardState player, BoardState enemy, int seed, IReadOnlyList<PhaseCommand> commands)
{
    var rng = new Rng(seed);
    var log = new CombatEventLog();
    var playerUnits = SpawnCombatants(player);
    var enemyUnits = SpawnCombatants(enemy);
    var stances = new StanceState();

    RunPhase(CombatPhase.Deployment, playerUnits, enemyUnits, stances, rng, log, damageScale: 0.2f);
    ApplyCommands(commands.Where(c => c.AfterPhase == CombatPhase.Deployment), /* ... */);

    RunPhase(CombatPhase.Grind, playerUnits, enemyUnits, stances, rng, log, damageScale: 1.0f);
    ApplyCommands(commands.Where(c => c.AfterPhase == CombatPhase.Grind), /* ... */);

    RunPhase(CombatPhase.FinalPush, playerUnits, enemyUnits, stances, rng, log, damageScale: 1.3f);

    return new CombatResult
    {
        EventLog = log,
        PlayerWon = TotalHp(enemyUnits) <= 0 || TotalHp(playerUnits) > 0 && TotalHp(enemyUnits) <= 0
    };
}
```

Each tick: move toward targets in Deployment, apply adjacency buffs, fire on cooldown, log events.

- [ ] **Step 3: Run determinism test — expect PASS**

- [ ] **Step 4: Commit**

```bash
git commit -am "feat(core): three-phase combat resolver with event log"
```

---

### Task 7: Between-phase commands

**Files:**
- Create: `Assets/_Project/Core/Combat/PhaseCommand.cs`
- Create: `Assets/_Project/Core/Combat/CommandProcessor.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/CommandProcessorTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
[Test]
public void ChangeStance_RequiresCommandBuilding()
{
    var board = TestBoards.WithCommandBunker();
    var processor = new CommandProcessor();
    var available = processor.GetAvailableCommands(board, requisition: 2, CombatPhase.Deployment);
    Assert.That(available.Any(c => c.Type == CommandType.ChangeStance));
}
```

- [ ] **Step 2: Implement CommandProcessor**

```csharp
public IReadOnlyList<AvailableCommand> GetAvailableCommands(BoardState board, int requisition, CombatPhase completedPhase)
{
    var list = new List<AvailableCommand>();
    bool hasIntactCommand = board.Pieces.Any(p => p.Definition.CommandActions.HasFlag(CommandActionFlags.ChangeStance));
    bool bonusFromSpecial = /* command piece on special tile */;

    if (hasIntactCommand)
        list.Add(new AvailableCommand { Type = CommandType.ChangeStance, /* stance options */ });
    // ... requisition buff, call strike
    return list;
}

public CommandResult TryApply(PhaseCommand cmd, BoardState board, ref int requisition, StanceState stances)
{
    if (cmd.Type == CommandType.SpendRequisitionBuff && requisition < cmd.Cost)
        return CommandResult.Fail("Insufficient requisition");
    // apply
    return CommandResult.Ok();
}
```

- [ ] **Step 3: Run tests — expect PASS**

- [ ] **Step 4: Commit**

```bash
git commit -am "feat(core): between-phase command processor"
```

---

## Phase 4 — Run State & Save/Load

### Task 8: RunState and JSON serialization

**Files:**
- Create: `Assets/_Project/Core/Run/RunPhase.cs`
- Create: `Assets/_Project/Core/Run/RunState.cs`
- Create: `Assets/_Project/Core/Run/RunSaveSerializer.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/RunSaveSerializerTests.cs`

- [ ] **Step 1: Write round-trip test**

```csharp
[Test]
public void SerializeDeserialize_PreservesGoldAndFightIndex()
{
    var state = new RunState
    {
        FightIndex = 3,
        Gold = 120,
        Requisition = 4,
        RunSeed = 777,
        FactionId = "iron_vanguard",
        Phase = RunPhase.Build
    };
    var json = RunSaveSerializer.ToJson(state);
    var loaded = RunSaveSerializer.FromJson(json);
    Assert.AreEqual(3, loaded.FightIndex);
    Assert.AreEqual(120, loaded.Gold);
    Assert.AreEqual(RunPhase.Build, loaded.Phase);
}
```

- [ ] **Step 2: Implement RunState**

Include: `BoardState` snapshot (list of placed piece records), `BenchPieceIds[]`, `ShopState`, `FrozenOfferId`, mid-combat fields (`CombatSeed`, `PlayerBoard`, `EnemyBoard`, `CompletedCombatPhase`, `PendingCommands`, `EventLogJson`).

- [ ] **Step 3: Implement RunSaveSerializer using Newtonsoft.Json**

- [ ] **Step 4: Run round-trip + mid-combat round-trip tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git commit -am "feat(core): run state serialization for save/resume"
```

---

### Task 9: SaveManager (Unity)

**Files:**
- Create: `Assets/_Project/Game/SaveManager.cs`

- [ ] **Step 1: Implement SaveManager**

```csharp
using System.IO;
using DeadManZone.Core.Run;
using UnityEngine;

namespace DeadManZone.Game
{
    public static class SaveManager
    {
        private const string FileName = "run_save.json";
        private static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasSave() => File.Exists(Path);

        public static void Save(RunState state)
        {
            var json = RunSaveSerializer.ToJson(state);
            File.WriteAllText(Path, json);
        }

        public static RunState Load()
        {
            if (!HasSave()) return null;
            try
            {
                return RunSaveSerializer.FromJson(File.ReadAllText(Path));
            }
            catch
            {
                return null; // RunManager shows corrupt save dialog
            }
        }

        public static void DeleteSave()
        {
            if (HasSave()) File.Delete(Path);
        }
    }
}
```

- [ ] **Step 2: Hook Application.quitting in RunManager for auto-save**

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(game): persistent save manager"
```

---

## Phase 5 — Content Pipeline

### Task 10: ScriptableObject definitions and ContentDatabase

**Files:**
- Create: `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs`
- Create: `Assets/_Project/Data/ScriptableObjects/FactionSO.cs`
- Create: `Assets/_Project/Data/ScriptableObjects/EnemyTemplateSO.cs`
- Create: `Assets/_Project/Data/ContentDatabase.cs`
- Create: `Assets/_Project/Core/Content/ContentRegistry.cs`

- [ ] **Step 1: Create PieceDefinitionSO**

```csharp
using DeadManZone.Core.Board;
using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Piece Definition")]
    public class PieceDefinitionSO : ScriptableObject
    {
        public string id;
        public string displayName;
        public PieceCategory category;
        public Vector2Int[] shapeCells;
        public string[] tags;
        public int maxHp, baseDamage, cooldownTicks;
        public int goldCost, requisitionCost;
        public ShopModifierFlags shopModifiers;
        public CommandActionFlags commandActions;

        public PieceDefinition ToCore()
        {
            var cells = new System.Collections.Generic.List<GridCoord>();
            foreach (var v in shapeCells)
                cells.Add(new GridCoord(v.x, v.y));
            return new PieceDefinition
            {
                Id = id,
                DisplayName = displayName,
                Category = category,
                Shape = new PieceShape(cells),
                Tags = tags,
                MaxHp = maxHp,
                BaseDamage = baseDamage,
                CooldownTicks = cooldownTicks,
                GoldCost = goldCost,
                RequisitionCost = requisitionCost,
                ShopModifiers = shopModifiers,
                CommandActions = commandActions
            };
        }
    }
}
```

- [ ] **Step 2: Create ContentDatabase that loads all SOs from Resources/Data**

- [ ] **Step 3: Author 15–20 piece assets** (see Task 15 checklist)

- [ ] **Step 4: Commit**

```bash
git commit -am "feat(data): scriptableObject content pipeline"
```

---

## Phase 6 — Game Orchestration

### Task 11: RunManager — gauntlet flow

**Files:**
- Create: `Assets/_Project/Game/RunManager.cs`

- [ ] **Step 1: Implement RunManager state machine**

States: `MainMenu`, `FactionSelect`, `Build`, `Combat`, `Aftermath`, `Victory`, `Defeat`

Key methods:

```csharp
public void StartNewRun(string factionId)
{
    _state = RunState.CreateNew(factionId, startingGold: 100, startingRequisition: 2, seed: Environment.TickCount);
    _state.Shop = _shopGenerator.Generate(_state.Board, factionId, round: 1, seed: _state.RunSeed);
    SaveManager.Save(_state);
}

public void FinalizeBoardAndStartCombat()
{
    _state.Phase = RunPhase.Combat;
    _state.CombatSeed = _state.RunSeed + _state.FightIndex * 1000;
    _state.EnemyBoard = _enemyTemplates[_state.FightIndex - 1].BuildBoard();
    SaveManager.Save(_state);
    // notify CombatDirector
}

public void OnCombatComplete(CombatResult result)
{
    if (result.PlayerWon)
    {
        _state.Gold += _rewards[_state.FightIndex].Gold;
        _state.Requisition += _rewards[_state.FightIndex].Requisition;
        _state.FightIndex++;
        if (_state.FightIndex > 5) { _state.Phase = RunPhase.Victory; SaveManager.DeleteSave(); return; }
        _state.Phase = RunPhase.Build;
        _state.Shop = _shopGenerator.Generate(/* ... */);
    }
    else _state.Phase = RunPhase.Defeat;
    SaveManager.Save(_state);
}
```

- [ ] **Step 2: Auto-save triggers on shop buy, command submit, pause**

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(game): run manager gauntlet flow"
```

---

## Phase 7 — Presentation (MVP UI)

### Task 12: Main menu with Continue / New Run

**Files:**
- Create: `Assets/_Project/Scenes/MainMenu.unity`
- Create: `Assets/_Project/Presentation/MainMenuController.cs`

- [ ] **Step 1: Scene with two buttons**

Continue visible only if `SaveManager.HasSave()`. Continue loads save → Run scene. New Run → Faction select panel (single faction OK for MVP).

- [ ] **Step 2: Play Mode test: Continue button hidden when no save**

- [ ] **Step 3: Commit**

---

### Task 13: BoardView — grid placement

**Files:**
- Create: `Assets/_Project/Presentation/Board/BoardView.cs`
- Create: `Assets/_Project/Presentation/Board/BoardTileView.cs`
- Create: `Assets/_Project/Presentation/Board/PieceDragHandler.cs`

- [ ] **Step 1: Render 8×6 grid with zone color tints (rear/support/front) and special tile overlay**

- [ ] **Step 2: Drag piece from bench/shop → grid; call `BoardState.TryPlace`; red highlight on invalid**

- [ ] **Step 3: Sell button on selected piece (50% gold refund)**

- [ ] **Step 4: Commit**

---

### Task 14: ShopView — three lanes

**Files:**
- Create: `Assets/_Project/Presentation/Shop/ShopView.cs`

- [ ] **Step 1: Display General / Engineers / Requisition lanes from `ShopState`**

- [ ] **Step 2: Buy deducts gold/requisition, adds to bench**

- [ ] **Step 3: Reroll one lane (gold cost += 1 each use per round)**

- [ ] **Step 4: Freeze one item across rounds**

- [ ] **Step 5: Show building modifier tooltips (discount %, extra slot, enemy tag preview)**

- [ ] **Step 6: Commit**

---

### Task 15: CombatDirector + PhaseCommandPanel

**Files:**
- Create: `Assets/_Project/Presentation/Combat/CombatDirector.cs`
- Create: `Assets/_Project/Presentation/Combat/PhaseCommandPanel.cs`

- [ ] **Step 1: CombatDirector reads `CombatEventLog` and plays events with configurable tick delay**

Map `ActionType`: `move`, `damage`, `buff`, `cooldown_fire` to simple animations (sprite lunge, damage number, flash).

- [ ] **Step 2: Pause after Deployment and Grind phases; show PhaseCommandPanel**

- [ ] **Step 3: Panel lists `CommandProcessor.GetAvailableCommands`; on submit, append to command list and resume sim**

- [ ] **Step 4: On load mid-combat, replay existing event log then pause at correct phase**

- [ ] **Step 5: Commit**

```bash
git commit -am "feat(presentation): combat replay and phase commands"
```

---

## Phase 8 — Vertical Slice Content

### Task 16: Author Iron Vanguard content

**Files:**
- Create: `Assets/_Project/Data/Resources/Pieces/*.asset` (15–20)
- Create: `Assets/_Project/Data/Resources/Enemies/Fight1.asset` … `Fight5.asset`
- Create: `Assets/_Project/Data/Resources/Factions/IronVanguard.asset`

- [ ] **Step 1: Create faction SO** — special tiles at (1,2), (4,2), (6,2); Vanguard tag bonus

- [ ] **Step 2: Create pieces per design spec**

Minimum set:
- Buildings: command_bunker, supply_depot, field_gun_nest, radio_array, field_workshop
- Units: rifle_squad, mg_team, trench_raider, diesel_walker, mortar_crew
- Hybrid/rare: mobile_artillery, gas_drone, armored_sapper

- [ ] **Step 3: Create 5 enemy templates** with escalating boards

- [ ] **Step 4: Headless sim test — player starter board vs Fight1 — player can win**

- [ ] **Step 5: Commit**

```bash
git commit -am "content: iron vanguard vertical slice pieces and enemies"
```

---

## Phase 9 — Integration & QA

### Task 17: Run scene wiring and Save & Exit

**Files:**
- Create: `Assets/_Project/Scenes/Run.unity`
- Modify: `Assets/_Project/Game/RunManager.cs`

- [ ] **Step 1: Single Run scene hosts BoardView + ShopView + Combat overlay**

- [ ] **Step 2: Run hub shows fight #, gold, requisition, "Save & Exit" button**

- [ ] **Step 3: Full loop Play Mode test**

1. New Run → place pieces → fight 1 → win
2. Save mid-build before fight 2 → exit play mode → Continue → same board/gold
3. Save mid-combat after phase 1 → reload → combat resumes at command window

- [ ] **Step 4: Commit**

```bash
git commit -am "feat: wire vertical slice run scene and save/resume"
```

---

### Task 18: Determinism regression suite

**Files:**
- Create: `Assets/_Project/Core.Tests/EditMode/VerticalSliceRegressionTests.cs`

- [ ] **Step 1: Fixed-seed full combat test for all 5 enemy templates**

- [ ] **Step 2: Save/load round-trip during each RunPhase enum value**

- [ ] **Step 3: Document test run in README snippet at repo root**

```bash
git commit -am "test: vertical slice regression suite"
```

---

## Spec Coverage Checklist

| Spec requirement | Task |
|------------------|------|
| Hybrid grid + zones | Task 3 |
| Faction special overlapping tiles | Task 3, 16 |
| 3-lane shop + building modifiers | Task 4, 14 |
| Gold + requisition economy | Task 4, 11, 14 |
| 3-phase combat (Total War tempo) | Task 6 |
| Between-phase commands from loadout | Task 7, 15 |
| Deterministic core sim | Task 1, 6, 18 |
| Mid-run save/resume any point | Task 8, 9, 17 |
| Linear 5-fight gauntlet | Task 11, 16 |
| Iron Vanguard vertical slice | Task 16 |
| Economy vs combat tension | Task 4, 16 (content tuning) |
| Main menu Continue/New Run | Task 12 |

---

## Implementation Order Summary

1. Tasks 0–1: Project + RNG  
2. Tasks 2–3: Board  
3. Task 4: Shop  
4. Tasks 5–7: Combat + commands  
5. Tasks 8–9: Save  
6. Task 10: Content pipeline  
7. Task 11: RunManager  
8. Tasks 12–15: UI  
9. Tasks 16–18: Content + integration  

**Estimated effort:** 2–4 weeks solo, depending on art scope (placeholder rectangles acceptable for MVP).
