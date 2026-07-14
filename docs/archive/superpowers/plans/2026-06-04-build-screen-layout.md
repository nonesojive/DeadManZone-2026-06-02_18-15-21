> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# Build Screen Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the build-phase screen per `docs/superpowers/specs/2026-06-04-build-screen-layout-design.md` — pause menu, unified HUD, 9×10 board with zone chrome, spatial 2×9 reserves, R/Q rotation, shop lock slot preservation.

**Architecture:** Extend Core placement with `PieceRotation` and a fixed-size `ReservesState`; bump save schema to v3 (`ReservesSnapshot`, rotation on board pieces, `SlotIndex` on shop offers). Presentation updates `RunSceneSetup` + drag-drop; Game orchestrator swaps bench APIs for reserves. No save migration from v2 bench list.

**Tech Stack:** Unity uGUI/TMP, pure C# Core (`Assets/_Project/Core`), EditMode tests (`Assets/_Project/Core.Tests`), scene setup via `RunSceneSetup.cs` editor menu.

**Spec:** `docs/superpowers/specs/2026-06-04-build-screen-layout-design.md`

---

## File map

| File | Action |
|------|--------|
| `Core/Board/PieceRotation.cs` | Create |
| `Core/Board/ShapeTransforms.cs` | Create — rotate cell offsets |
| `Core/Board/PieceShape.cs` | Modify — `GetCells(anchor, rotation)` |
| `Core/Board/PlacedPiece.cs` | Modify — add `Rotation` |
| `Core/Board/BoardState.cs` | Modify — rotation on place/relocate/canPlace |
| `Core/Board/ReservesState.cs` | Create — 9×2 placement |
| `Core/Run/PlacedPieceRecord.cs` | Modify — `Rotation` int |
| `Core/Run/ReservesSnapshot.cs` | Create |
| `Core/Run/BoardSnapshot.cs` | Modify — mapper passes rotation |
| `Core/Run/RunState.cs` | Modify — `Reserves`, schema v3, remove `BenchPieceIds` |
| `Core/Shop/ShopOffer.cs` | Modify — `SlotIndex` |
| `Core/Shop/ShopGenerator.cs` | Modify — assign slot indices |
| `Core/Run/ShopOfferRecord.cs` | Modify — `SlotIndex` |
| `Core.Tests/EditMode/TestBoards.cs` | Modify — 9×10, rear 4 / support 3 |
| `Core.Tests/EditMode/BoardStateTests.cs` | Modify + rotation tests |
| `Core.Tests/EditMode/ReservesStateTests.cs` | Create |
| `Core.Tests/EditMode/RunOrchestratorTests.cs` | Modify bench → reserves, slot lock test |
| `Data/ScriptableObjects/FactionSO.cs` | Modify — height 10, rear 4, support 3, special tiles |
| `Game/RunOrchestrator.cs` | Modify — remove `BenchLimit`, reserves helpers |
| `Game/RunOrchestrator.Shop.cs` | Modify — reserves acquire, reroll by slot |
| `Game/RunManager.cs` | Modify — public reserves API |
| `Presentation/Run/PauseMenuView.cs` | Create |
| `Presentation/Run/RunSceneController.cs` | Modify — menu wiring |
| `Presentation/Run/RunHudView.cs` | Modify — combined top-left text |
| `Presentation/Shop/ShopView.cs` | Modify — remove currencies, order by slot |
| `Presentation/Bench/ReservesView.cs` | Create (replace `BenchView` usage) |
| `Presentation/Bench/ReservesTileView.cs` | Create — mirror `BoardTileView` minimal |
| `Presentation/DragDrop/DragPayload.cs` | Modify — `ReservesPiece`, `Rotation` |
| `Presentation/DragDrop/DragDropController.cs` | Modify — R/Q while dragging |
| `Presentation/DragDrop/BenchSlotView.cs` | Delete or obsolete after reserves |
| `Presentation/Editor/RunSceneSetup.cs` | Modify — layout, menu, reserves grid, zone chrome |
| `README.md` | Modify — note save v3 / old saves invalid |

---

## Task 1: Board dimensions (Core constants)

**Files:**
- Modify: `Assets/_Project/Core.Tests/EditMode/TestBoards.cs`
- Modify: `Assets/_Project/Data/ScriptableObjects/FactionSO.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/BoardStateTests.cs`

- [ ] **Step 1: Update test constants**

```csharp
public const int DefaultWidth = 9;
public const int DefaultHeight = 10;
public const int DefaultRearCols = 4;
public const int DefaultSupportCols = 3;
public static GridCoord FrontLineAnchor(int y = 5) => new(7, y); // front zone starts at x=7
```

- [ ] **Step 2: Update `FactionSO` serialized defaults** to match (boardHeight 10, rearCols 4, supportCols 3). Adjust `specialTileCoords` to valid Y range 0–9 (e.g. keep x, set y to 4–5 band).

- [ ] **Step 3: Fix `BoardStateTests` zone assertions** for rear x=0, front x=8:

```csharp
Assert.AreEqual(ZoneType.Rear, layout.GetZone(new GridCoord(0, 5)));
Assert.AreEqual(ZoneType.Front, layout.GetZone(new GridCoord(8, 5)));
```

- [ ] **Step 4: Run EditMode tests**

Run (Unity Test Runner or CLI per README):

`Unity.exe -batchmode -projectPath "<repo>" -runTests -testPlatform editmode -quit`

Expected: board tests PASS (some placement tests may need anchor tweaks).

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Core.Tests/EditMode/TestBoards.cs Assets/_Project/Data/ScriptableObjects/FactionSO.cs Assets/_Project/Core.Tests/EditMode/BoardStateTests.cs
git commit -m "feat(board): 9x10 layout with rear 4 support 3 front 2"
```

---

## Task 2: Piece rotation (Core)

**Files:**
- Create: `Assets/_Project/Core/Board/PieceRotation.cs`
- Create: `Assets/_Project/Core/Board/ShapeTransforms.cs`
- Modify: `Assets/_Project/Core/Board/PieceShape.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/PieceRotationTests.cs`

- [ ] **Step 1: Write failing rotation test**

```csharp
[Test]
public void GetCells_Rotated90_OffsetsSwapped()
{
    var shape = new PieceShape(new[] { new GridCoord(0, 0), new GridCoord(1, 0) }); // 2-wide
    var cells = shape.GetCells(new GridCoord(3, 4), PieceRotation.R90).ToList();
    Assert.Contains(new GridCoord(3, 4), cells);
    Assert.Contains(new GridCoord(3, 5), cells); // second cell stacks vertically
}
```

- [ ] **Step 2: Implement**

`PieceRotation.cs`:

```csharp
namespace DeadManZone.Core.Board
{
    public enum PieceRotation { R0 = 0, R90 = 90, R180 = 180, R270 = 270 }
}
```

`ShapeTransforms.cs` — rotate local `(x,y)` then add anchor:

```csharp
public static GridCoord RotateOffset(GridCoord local, PieceRotation rotation) =>
    rotation switch
    {
        PieceRotation.R0 => local,
        PieceRotation.R90 => new GridCoord(-local.Y, local.X),
        PieceRotation.R180 => new GridCoord(-local.X, -local.Y),
        PieceRotation.R270 => new GridCoord(local.Y, -local.X),
        _ => local
    };
```

`PieceShape.GetCells(GridCoord anchor, PieceRotation rotation = PieceRotation.R0)` loops `_cells`, yields `anchor + RotateOffset(cell, rotation)` (use unrotated local cells from definition).

- [ ] **Step 3: Run test — PASS**

- [ ] **Step 4: Commit**

```bash
git commit -m "feat(core): piece rotation on shape cells"
```

---

## Task 3: BoardState + save records use rotation

**Files:**
- Modify: `Assets/_Project/Core/Board/PlacedPiece.cs`
- Modify: `Assets/_Project/Core/Board/BoardState.cs`
- Modify: `Assets/_Project/Core/Run/PlacedPieceRecord.cs` (inline in `BoardSnapshot.cs`)
- Modify: `Assets/_Project/Core/Run/BoardSnapshot.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/BoardStateTests.cs`

- [ ] **Step 1: Add `Rotation` to `PlacedPiece` and `PlacedPieceRecord` (int or enum serialized as int)**

- [ ] **Step 2: Thread rotation through `CanPlace` / `TryPlace` / `TryRelocate` / `TryRemove` / `IsOnSpecialTile` — all `GetCells` calls pass `piece.Rotation`**

- [ ] **Step 3: Mapper read/write rotation (default 0)**

- [ ] **Step 4: Test — place rifle at front with `R90`, assert occupancy cells match rotated footprint**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(board): store and validate rotation on placement"
```

---

## Task 4: ReservesState (Core)

**Files:**
- Create: `Assets/_Project/Core/Board/ReservesState.cs`
- Create: `Assets/_Project/Core/Run/ReservesSnapshot.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/ReservesStateTests.cs`

- [ ] **Step 1: Failing tests**

```csharp
[Test]
public void TryPlace_TwoPieces_NoOverlap_WhenTheyFit()
{
    var reserves = new ReservesState(width: 9, height: 2);
    Assert.IsTrue(reserves.TryPlace(smallPiece, new GridCoord(0, 0), PieceRotation.R0, "a").Success);
    Assert.IsTrue(reserves.TryPlace(smallPiece, new GridCoord(3, 0), PieceRotation.R0, "b").Success);
}

[Test]
public void TryPlace_ExceedsHeight_Fails()
{
    var reserves = new ReservesState(9, 2);
    var tall = /* piece with cells at y=0 and y=1 relative + anchor y=1 overflows */;
    Assert.IsFalse(reserves.TryPlace(tall, new GridCoord(0, 1), PieceRotation.R0).Success);
}
```

- [ ] **Step 2: Implement `ReservesState`** — copy `BoardState` placement logic without zone checks; fixed 9×2; same rotation API; `TryRemove`, `TryRelocate`, `Pieces` collection.

- [ ] **Step 3: `ReservesSnapshot` + static mapper `FromReserves` / `ToReserves`**

- [ ] **Step 4: Run tests — PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(core): spatial reserves grid 9x2"
```

---

## Task 5: RunState schema v3 + orchestrator reserves

**Files:**
- Modify: `Assets/_Project/Core/Run/RunState.cs`
- Modify: `Assets/_Project/Game/RunOrchestrator.cs`
- Modify: `Assets/_Project/Game/RunOrchestrator.Shop.cs`
- Modify: `Assets/_Project/Game/RunManager.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/RunOrchestratorTests.cs`

- [ ] **Step 1: `SaveSchemaVersion = 3`; replace `BenchPieceIds` with `ReservesSnapshot Reserves`**

- [ ] **Step 2: `GetReserves()` / `SaveReserves(ReservesState)` on orchestrator**

- [ ] **Step 3: Replace APIs:**

| Remove | Add |
|--------|-----|
| `TryAcquireOfferToBench` | `TryAcquireOfferToReserves(offerId, anchor, rotation)` |
| `TryPlaceFromBench(index, anchor)` | `TryPlaceFromReserves(instanceId, boardAnchor, rotation)` |
| `TrySellFromBench` | `TrySellFromReserves(instanceId)` |
| `TryMoveBoardToBench` | `TryMoveBoardToReserves(boardInstanceId, reservesAnchor, rotation)` |

Each calls `Persist()` on success.

- [ ] **Step 4: Update tests** — `TryAcquireOfferToBench` → reserves placement at `new GridCoord(0,0)`; `SaveMidBuild` asserts `Reserves.Pieces` not empty list of strings.

- [ ] **Step 5: `TryLoadSavedRun`** — if `SaveSchemaVersion < 3`, return false (or clear save) per spec no-migration.

- [ ] **Step 6: Run EditMode tests — PASS**

- [ ] **Step 7: Commit**

```bash
git commit -m "feat(run): reserves snapshot v3 replaces bench list"
```

---

## Task 6: Shop offer slot index + reroll merge

**Files:**
- Modify: `Assets/_Project/Core/Shop/ShopOffer.cs`
- Modify: `Assets/_Project/Core/Shop/ShopGenerator.cs`
- Modify: `Assets/_Project/Core/Run/ShopOfferRecord.cs`
- Modify: `Assets/_Project/Game/RunOrchestrator.Shop.cs`
- Modify: `Assets/_Project/Core.Tests/EditMode/RunOrchestratorTests.cs`

- [ ] **Step 1: Add `public int SlotIndex { get; set; }` to offer + record; `FromOffer` / `ToOffer` copy it**

- [ ] **Step 2: `RollLane` — `SlotIndex = i` in loop**

- [ ] **Step 3: Replace `RefreshShop` + `ApplyLockedOffer` reroll path:**

```csharp
public bool TryRerollLane(ShopLane lane)
{
    // ... pay cost ...
    var previous = State.Shop.Offers.Where(o => o.Lane == lane).ToDictionary(o => o.SlotIndex);
    var locked = previous.Values.FirstOrDefault(o => IsOfferLocked(o));
    int slotCount = previous.Count > 0 ? previous.Keys.Max() + 1 : OffersPerLane;

    var newLaneOffers = GenerateLaneOffers(lane, slotCount, seed); // extract lane roll helper
    var merged = new List<ShopOffer>();
    for (int i = 0; i < slotCount; i++)
    {
        if (locked != null && locked.SlotIndex == i)
            merged.Add(locked);
        else
            merged.Add(newLaneOffers.First(o => o.SlotIndex == i));
    }
    State.Shop.Offers = State.Shop.Offers.Where(o => o.Lane != lane).Concat(merged).ToList();
    Persist();
}
```

Refactor `ShopGenerator` to expose per-lane generation or rebuild lane in orchestrator with same seed rules.

- [ ] **Step 4: Delete prepend logic in `ApplyLockedOffer`** (locking only sets `LockedOffer` record including `SlotIndex`)

- [ ] **Step 5: Extend test**

```csharp
int lockedSlot = toLock.SlotIndex;
// after 2 rerolls:
var offer = _orchestrator.State.Shop.Offers.First(o => o.PieceId == lockedPieceId);
Assert.AreEqual(lockedSlot, offer.SlotIndex);
```

- [ ] **Step 6: Commit**

```bash
git commit -m "fix(shop): locked offers keep slot index on lane reroll"
```

---

## Task 7: Pause menu (Presentation)

**Files:**
- Create: `Assets/_Project/Presentation/Run/PauseMenuView.cs`
- Modify: `Assets/_Project/Presentation/Run/RunSceneController.cs`
- Modify: `Assets/_Project/Presentation/Editor/RunSceneSetup.cs`

- [ ] **Step 1: `PauseMenuView`** — serialized buttons Resume/Options/MainMenu/Exit; Options panel stub; `Show()`/`Hide()`; Exit calls `RunManager` persist + `Application.Quit()` (#if UNITY_EDITOR `EditorApplication.isPlaying = false`)

- [ ] **Step 2: MENU button top-right opens overlay; Resume hides**

- [ ] **Step 3: Remove save/main menu buttons from top bar wiring in `RunSceneController`**

- [ ] **Step 4: `RunSceneSetup` build overlay + wire references**

- [ ] **Step 5: Manual — Play build scene, MENU works**

- [ ] **Step 6: Commit**

```bash
git commit -m "feat(ui): build-phase pause menu with auto-save actions"
```

---

## Task 8: HUD consolidation + shop currency removal

**Files:**
- Modify: `Assets/_Project/Presentation/Run/RunHudView.cs`
- Modify: `Assets/_Project/Presentation/Shop/ShopView.cs`
- Modify: `Assets/_Project/Presentation/Editor/RunSceneSetup.cs`

- [ ] **Step 1: `RunHudView.Refresh` — single `statusText` block or optional second field merged:**

```csharp
statusText.text =
    $"Fight {state.FightIndex} / {RunOrchestrator.MaxFights}\n" +
    $"Phase: {state.Phase}{gateLine}\n" +
    $"Supplies: {state.Supplies}  Manpower: {state.Manpower}  ...";
```

Remove separate `currenciesText` usage from scene or leave unassigned.

- [ ] **Step 2: Remove `UpdateCurrencyText` and `currenciesText` from `ShopView`; `RebuildLane` orders `OrderBy(o => o.SlotIndex)`**

- [ ] **Step 3: Re-anchor top bar in `RunSceneSetup` (HUD left ~0.02, MENU ~0.95)**

- [ ] **Step 4: Commit**

```bash
git commit -m "feat(ui): consolidate run HUD and remove shop duplicate currencies"
```

---

## Task 9: Board & reserves presentation + zone chrome

**Files:**
- Modify: `Assets/_Project/Presentation/Editor/RunSceneSetup.cs`
- Create: `Assets/_Project/Presentation/Bench/ReservesView.cs`
- Create: `Assets/_Project/Presentation/Bench/ReservesTileView.cs`
- Modify: `Assets/_Project/Presentation/Board/BoardView.cs` (zone header row optional component)
- Delete/retire: `BenchView.cs`, `BenchSlotView.cs` references

- [ ] **Step 1: `CreateBoardSection` — grid `constraintCount = 9`, cell size ~40–44 to fit 10 rows; add zone header row above grid (3 labels positioned by column width ratio 4:3:2); add `ZoneStrip` image below grid**

- [ ] **Step 2: `CreateReservesSection` — 9×2 `GridLayoutGroup`, `ReservesView.BuildGrid()`, refresh from `RunManager` reserves state, chips per placed piece**

- [ ] **Step 3: Drop targets on reserve tiles (`ReservesTileDropTarget`) accepting shop/board drags**

- [ ] **Step 4: Rename UI strings Bench → Reserves**

- [ ] **Step 5: Editor menu refresh Run scene** — **DeadManZone → Refresh Run Scene** (or Setup)

- [ ] **Step 6: Commit**

```bash
git commit -m "feat(ui): board zone chrome and 2x9 reserves grid"
```

---

## Task 10: Drag-drop rotation (R / Q)

**Files:**
- Modify: `Assets/_Project/Presentation/DragDrop/DragPayload.cs`
- Modify: `Assets/_Project/Presentation/DragDrop/DragDropController.cs`
- Modify: `Assets/_Project/Presentation/DragDrop/DragGhost.cs`
- Modify: drop targets / `BoardView` / reserves / shop sources

- [ ] **Step 1: Payload fields**

```csharp
public PieceRotation Rotation { get; set; } = PieceRotation.R0;
public string ReservesInstanceId { get; set; }
// Rename enum value BenchPiece -> ReservesPiece
```

- [ ] **Step 2: `DragDropController.Update` while `_activePayload != null`:**

```csharp
if (Input.GetKeyDown(KeyCode.R))
    _activePayload.Rotation = RotateCW(_activePayload.Rotation);
if (Input.GetKeyDown(KeyCode.Q))
    _activePayload.Rotation = RotateCCW(_activePayload.Rotation);
_ghost?.SetRotation(_activePayload.Rotation);
```

- [ ] **Step 3: Board drop uses `payload.Rotation`; pick-up from board/reserves copies stored rotation into payload**

- [ ] **Step 4: `DragGhost` draws N cells for rotated shape (or rotated rect bounds)**

- [ ] **Step 5: Manual playtest — R/Q during shop→board, board→reserves drags**

- [ ] **Step 6: Commit**

```bash
git commit -m "feat(input): R/Q rotate pieces while dragging"
```

---

## Task 11: Regression sweep

**Files:**
- Modify: any remaining `BenchPieceIds` / `BenchLimit` references (grep repo)
- Modify: `README.md` — save v3, old saves invalid
- Modify: `Assets/_Project/Core.Tests/EditMode/VerticalSliceRegressionTests.cs` if schema assertions exist

- [ ] **Step 1: `rg "BenchPiece|BenchLimit|BenchView|BenchSlot"` — fix all**

- [ ] **Step 2: Run full EditMode suite — all PASS**

- [ ] **Step 3: Play Mode smoke optional (`ShopViewPlayModeTests`)**

- [ ] **Step 4: Commit**

```bash
git commit -m "chore: README and test cleanup for build screen layout"
```

---

## Spec coverage checklist

| Spec section | Task |
|--------------|------|
| §1 Pause menu + auto-save | 7 |
| §2 HUD consolidation | 8 |
| §3 Board 9×10 + zone chrome | 1, 9 |
| §4 Reserves spatial | 4, 5, 9 |
| §5 Rotation R/Q | 2, 3, 10 |
| §6 Shop lock slot | 6 |
| §8 Testing | 1–6, 11 |
| No migration | 5 (`SaveSchemaVersion < 3`) |

---

## Manual acceptance (post-implementation)

- [ ] MENU: Resume / Options stub / Main Menu / Exit quits
- [ ] Resources only top-left
- [ ] Zone headers + strip visible, not clipped
- [ ] Reserves fits pieces by shape; full grid blocks more
- [ ] Lock mid-lane, reroll → same slot position
- [ ] New run after deploy (old save discarded)
