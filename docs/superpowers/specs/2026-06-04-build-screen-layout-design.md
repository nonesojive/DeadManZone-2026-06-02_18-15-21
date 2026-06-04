# DeadManZone — Build Screen Layout & Reserves Design Spec

**Date:** 2026-06-04  
**Engine:** Unity  
**Status:** Approved (brainstorming) — pending written spec review before implementation plan  
**Builds on:** `2026-06-03-deadmanzone-rework-design.md` (economy, horizontal board, shop lanes)  
**Scope:** Build-phase UI layout, pause menu, board/reserves geometry, piece rotation, shop lock slot behavior

---

## Summary

Reorganize the build (shop) screen: consolidate HUD, replace dual exit buttons with a top-right pause menu and always-on auto-save, resize the main board and zone chrome, replace the 3-slot bench with a spatial **2×9 Reserves** grid, add optional **keyboard rotation** on main board and reserves, and fix shop **lock** so rerolls keep locked offers in the same lane slot.

**Save migration:** None. Only new runs are supported after ship; existing saves may fail or require deletion (document for developers; no player-facing migration UI).

---

## Section 1 — Pause menu & auto-save

### UI

- Remove **Save & Exit** and top-left **Main Menu** from the build top bar.
- Add **MENU** button, top-right of build HUD.
- Opening MENU shows a modal overlay (`CanvasGroup`: blocks raycasts, dims or panels over build UI).

| Action | Behavior |
|--------|----------|
| **Resume** | Close overlay; continue build phase |
| **Options** | Stub sub-panel (“Options — coming soon”) with Back, same tone as main menu |
| **Main Menu** | `Persist()` → load `MainMenu` scene; save file remains for **Continue** |
| **Exit** | `Persist()` → `Application.Quit()` (Editor: stop Play Mode) |

### Auto-save

- No manual save button in build UI.
- Keep existing `RunOrchestrator.Persist()` triggers (shop, board, reserves, combat transitions, etc.).
- Keep `RunSaveBootstrap` on application pause/quit.
- Optional: persist when opening MENU (low cost; ensures state before Exit/Main Menu).

### Components

- New `PauseMenuView` (or extend `RunSceneController`) wired from `RunSceneSetup`.
- `RunSceneController` drops `saveAndExitButton` / top-left `backToMenuButton`; wires MENU overlay only.

---

## Section 2 — HUD consolidation

- Single **top-left** info block via `RunHudView`:
  - Lines 1–2: `Fight N / 10`, `Phase: Build`, optional battle-gate message
  - Line 3+: Supplies, Manpower, Authority, Morale, current reroll cost
- Remove `ShopView` duplicate `currenciesText`; shop area shows lane offers + modifier/enemy preview tooltip only (top area near shop columns).
- Re-layout `RunSceneSetup` top bar anchors: HUD left, MENU right, tooltips/modifiers where they fit without overlapping board.

---

## Section 3 — Main board geometry & zone chrome

### Dimensions

| Property | Value |
|----------|--------|
| Width | 9 |
| Height | 10 |
| Rear columns | 4 |
| Support columns | 3 |
| Front columns | 2 (remaining width) |

- Update `FactionSO` defaults (`boardHeight`, `rearCols`, `supportCols`).
- Update `TestBoards` constants and placement helpers (e.g. front-line anchors).
- Enemy templates / snapshots: use same width/height/zone columns when generating enemy boards.

### Zone presentation (option C — approved)

- **Above board:** zone headers **REAR**, **SUPPORT**, **FRONT** aligned to column spans (not on left edge).
- **Below board:** thin horizontal **zone color strip** (rear / support / front tints matching `BoardView` zone colors).
- Board grid region uses more horizontal space; left margin no longer hosts vertical zone labels.

### Special tiles

- Reposition `FactionSO.specialTileCoords` for the 10-row grid (content/design pass in Unity; not auto-scaled from old 6-row saves).

---

## Section 4 — Reserves (replaces bench)

### Player-facing

- Rename **Bench** → **Reserves** in UI strings and type names where practical (`ReservesView`, `ReservesState`).

### Grid

- **2 rows × 9 columns** (18 cells).
- No zone restrictions; pieces must fit entirely inside the grid with no overlap.

### Storage model (spatial — approved)

- Each stored piece: `pieceId`, `anchor` (`GridCoord`), `rotation` (`PieceRotation`).
- Placement uses rotated footprint (same rules as main board, bounds-only).
- Capacity is **implicit free cells**, not a fixed slot count.

### Gameplay flows

- Shop purchase → drop on reserves or main board (invalid drop = snap-back / no charge per existing rules).
- Main board → reserves: remove from board if footprint fits on reserves.
- Reserves → main board: remove from reserves if footprint fits with zone/category rules.
- Sell zone: unchanged (drop to sell, refund rules unchanged).

### Save format

- Replace `RunState.BenchPieceIds` (`List<string>`) with `ReservesSnapshot`:
  - `Width` = 9, `Height` = 2 (fixed for MVP)
  - `Pieces`: list of `{ pieceId, anchorX, anchorY, rotation }`
- **No migration** from `BenchPieceIds`; old saves are unsupported (option C).

### Core / orchestrator

- New `ReservesState` in Core (mirror `BoardState` placement API at fixed 2×9 layout).
- Replace `BenchLimit`, `TryPlaceFromBench(int index, ...)`, `TryMoveBoardToBench(..., benchIndex)`, etc. with reserves anchor + rotation APIs.
- Remove `RunOrchestrator.BenchLimit`.

### Presentation

- `ReservesView`: tile grid 9×2, drag-drop parity with `BoardView`.
- `RunSceneSetup`: bottom bar reserves region sized for 2×9 grid; remove three `BenchSlot` panels.

---

## Section 5 — Piece rotation

### Core

- `PieceRotation` enum: `0`, `90`, `180`, `270` degrees.
- `PieceShape.GetCells(GridCoord anchor, PieceRotation rotation)` applies rotation around piece local origin (document transform: rotate cell offsets before adding anchor).
- `PlacedPiece` and `PlacedPieceRecord` include `Rotation` (default `0`).
- `BoardState` / `ReservesState`: `CanPlace` / `TryPlace` / `TryRelocate` take rotation; restore from save with rotation.

### Input (approved: keyboard while dragging)

- During drag on **main board** or **reserves**:
  - **`R`**: rotate clockwise 90°
  - **`Q`**: rotate counter-clockwise 90°
- Update drag preview ghost immediately; drop uses current preview rotation.
- Rotation is optional (player may place at 0°).

### Presentation

- Extend drag payload (`BoardPieceDragSource`, shop drag sources) with `Rotation`.
- `PieceChipView` / preview rendering reflects rotated cell layout (bounds box or per-cell chips).

### Tests (EditMode)

- Rotated piece fits / rejects on zones and reserves bounds.
- Drag pick-up uses the piece’s saved rotation; **R** / **Q** only change the drag preview; drop writes preview rotation to state.

---

## Section 6 — Shop lock slot preservation

### Problem

`ApplyLockedOffer` reorders lane offers so the locked piece is first → UI shows it at the top after reroll.

### Fix

- Add `SlotIndex` (`int`, 0-based per lane) on `ShopOffer` and `ShopOfferRecord`.
- `ShopGenerator.RollLane`: assign `SlotIndex = i` for each rolled offer in that lane.
- `TryRerollLane`:
  1. Capture previous lane offers keyed by `SlotIndex` (including locked).
  2. Generate new offers for unlocked slots only at those indices.
  3. Preserve locked offer at its `SlotIndex` (same `PieceId`, prices, `OfferId` may stay or regenerate — prefer **stable `OfferId`** for locked slot to avoid breaking lock UI).
- `ShopView.RebuildLane`: order by `SlotIndex` ascending.
- Remove “prepend locked offer” behavior from `ApplyLockedOffer`; locking only marks state, reroll merge handles position.

### Tests

- Extend `LockedOffer_PersistsAcrossMultipleRerolls`: assert `SlotIndex` unchanged across two rerolls, not only `PieceId`.

---

## Section 7 — Architecture & file map

| Area | Primary files |
|------|----------------|
| Pause menu | `RunSceneSetup.cs`, `RunSceneController.cs`, new `PauseMenuView.cs` |
| HUD | `RunHudView.cs`, `ShopView.cs`, `RunSceneSetup.cs` |
| Board layout | `FactionSO.cs`, `BoardView.cs`, `RunSceneSetup.cs` |
| Reserves | `ReservesState.cs`, `ReservesSnapshot.cs`, `RunOrchestrator` (partial), `ReservesView.cs`, drag-drop types |
| Rotation | `PieceShape.cs`, `PlacedPiece.cs`, `BoardState.cs`, drag handlers |
| Shop slots | `ShopOffer.cs`, `ShopGenerator.cs`, `RunOrchestrator.Shop.cs`, `ShopView.cs` |
| Tests | `TestBoards.cs`, `BoardStateTests.cs`, `RunOrchestratorTests.cs`, new reserves/rotation tests |

### Layering

- **Core:** rotation math, reserves grid, shop `SlotIndex`, snapshot types (no Unity refs).
- **Game:** orchestrator wiring, persist, save schema.
- **Presentation:** UI layout, menu, drag preview, scene setup menu refresh.

### Scene refresh

After presentation changes: run editor menu to rebuild Run scene (e.g. **DeadManZone → Refresh Run Scene** or project-equivalent documented in implementation plan).

---

## Section 8 — Testing & acceptance

### EditMode

- Board 9×10 zones: rear/support/front column boundaries.
- Reserves: place multiple pieces, overlap rejected, out-of-bounds rejected.
- Rotation: 90° changes occupied cells; zone validation with rotation.
- Shop: lock at slot 2, reroll twice → still slot 2, same piece.

### Manual playtest

- [ ] MENU: Resume, Options stub, Main Menu (Continue works), Exit quits
- [ ] No duplicate currency line above shop; fight + resources top-left
- [ ] Zone headers above + color strip below; labels not clipped
- [ ] Reserves 2×9: buy large/small pieces, fit until full
- [ ] R/Q while dragging on board and reserves
- [ ] Lock offer mid-lane, reroll → same vertical slot
- [ ] New run only after update (old save discarded if broken)

---

## Section 9 — Out of scope

- Save migration from 6-high board or `BenchPieceIds`
- Mouse/touch rotate button (keyboard only for MVP)
- Combat-phase pause menu (build phase only unless trivial to share overlay)
- Changing shop lane count or offers-per-lane balance (only slot-index behavior)

---

## Decisions log

| Topic | Decision |
|-------|----------|
| Reserves capacity | Spatial 2×9, Backpack Battles–style |
| Rotation | Optional; R/Q while dragging; main + reserves |
| Zone labels | Above headers + below color strip |
| MENU Exit | Quit application after save |
| Old saves | No migration (new runs only) |
| Implementation approach | Single pass (core + presentation + scene setup) |
