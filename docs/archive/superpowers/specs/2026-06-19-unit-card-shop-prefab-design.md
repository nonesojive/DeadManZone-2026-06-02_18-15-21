> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — Unit Card Prefab, Shop Card Asset & Footprint Input Design

**Date:** 2026-06-19  
**Status:** Approved (brainstorming)  
**Scope:** Build-screen unit detail card, shop offer cards, multi-cell piece hover/drag  
**Related:** `PieceCardViewModelBuilder`, `ShopOfferView`, `BoardView`, `PieceShapeVisual`

---

## Summary

Two build-screen UX bugs and one workflow gap:

| Issue | Root cause | Fix |
|-------|------------|-----|
| Empty center frame on unit hover | `PieceHoverCard` builds UI procedurally; scene wiring / layout fragile | Author **`UnitDetailCard.prefab`** + `PieceCardView.Bind()` |
| Multi-cell hover/drag only on anchor cell | `BoardPieceDragSource` on anchor tile only; shape art has `raycastTarget = false` | **`BoardPieceFootprintHit`** on shape visual root |
| Shop cards not editable in Project | Prefab built in code by `RunSceneSetup.CreateOfferCardPrefab()` | Save **`ShopOfferCard.prefab`** asset; reference from `ShopView` |

**Locked decisions:**

| Area | Choice |
|------|--------|
| Unit card idle state | **Fully hidden** — no frame when nothing hovered |
| Unit card pattern | **One prefab instance** in center column; Show/Hide + rebind (no per-hover Instantiate) |
| Shop card pattern | **Same architecture** — prefab asset + existing `ShopOfferView.Bind()` |
| Shared prefab | **No** — separate `UnitDetailCard`, `BuildingPrefab`, and `ShopOfferCard` layouts; shared data layer only |
| Multi-cell input | **Full footprint hit target** on `PieceShapeVisual` root |
| Data layer | Keep `PieceCardViewModelBuilder` + `ShopOffer` — presentation only changes |
| Branch | New feature branch off `master` (or current mainline after merge) |

---

## Section 1 — Goals & success criteria

### Purpose

- Hovering any cell of a multi-cell piece shows the unit detail card and allows drag.
- Center unit card displays name, stats, tags, synergy, salvage context — not an empty frame.
- Shop and unit detail cards are **authorable `.prefab` assets** the designer edits in Unity.
- Idle build screen shows **no** unit card chrome.

### Success criteria

1. Hover any footprint cell of a 2×2 (or larger) piece → center card populates with correct piece data.
2. Begin drag from any footprint cell (except HQ) → drag works.
3. Pointer exit / no hover → center panel fully inactive (no visible frame).
4. `UnitDetailCard.prefab` changes in Editor (font size, colors, layout) appear in Play mode without code edits.
5. `ShopOfferCard.prefab` assigned on `ShopView`; shop reroll still renders N offers correctly.
6. EditMode: `PieceCardView` bind test + footprint hit resolver test green.
7. PlayMode: existing `ShopViewPlayModeTests` / `ShopOfferDragPlayModeTests` green.

---

## Section 2 — Unit detail card architecture

### New types

| Type | Path | Role |
|------|------|------|
| `PieceCardView` | `Assets/_Project/Presentation/UI/PieceCardView.cs` | Binds `PieceCardViewModel` to serialized TMP/Image/chip container |
| `UnitDetailCard.prefab` | `Assets/_Project/Presentation/UI/Prefabs/` | Authorable layout — **combat units** |
| `BuildingPrefab.prefab` | `Assets/_Project/Presentation/UI/Prefabs/` | Fork of unit card — **HQ/buildings** (designer-owned layout) |
| `ShopOfferCard.prefab` | `Assets/_Project/Presentation/UI/Prefabs/` | Authorable shop offer layout |
| `TagChip.prefab` (optional) | same folder | Template for dynamic tag chips |

### Prefab structure (recommended hierarchy)

```
UnitDetailCard (root, Image background)
├── Header (TMP name)
├── StatsBlock (HP, DMG, Move, Atk Speed, Attack Type, Armor Type)
├── SynergyBlock (summary, lines, critical mass, salvage, ability)
├── TagChipContainer (HorizontalLayoutGroup)
│   └── TagChipTemplate (inactive)
└── OverflowTooltip (TMP, optional)
```

All text fields wired on `PieceCardView` via `[SerializeField]`.

### `PieceCardView` API

```csharp
public void Bind(PieceCardViewModel model, string overflowTooltip);
public void Show();
public void Hide();
```

- Move bind logic from `PieceHoverCard.Bind()` into `PieceCardView` (or shared static helper called by both during migration).
- Apply theme via `UiThemeSO` / `UiThemeProvider` on bind.
- Instantiate tag chips from template; pool or destroy extras per bind (match current chip count behavior, max 4 optional + identity + overflow chip).

### `UnitCardPanelView` changes

- `[SerializeField] PieceCardView cardView` (replaces direct `PieceHoverCard` reference).
- `Show`: build model via `PieceCardViewModelBuilder`, `cardView.Bind()`, `cardView.Show()`, activate `panelRoot`.
- `Hide`: `cardView.Hide()`, deactivate `panelRoot`.
- **Require** prefab reference — no procedural fallback in production path.

### `PieceHoverCardController` changes

- Fixed panel path unchanged: delegates to `UnitCardPanelView`.
- Remove or deprecate runtime `ResolveHoverCard()` procedural card for build screen (floating cursor card out of scope unless needed later).

### Scene / bootstrap migration

- `RunSceneSetup.CreateCenterColumnSection`: instantiate `UnitDetailCard.prefab` instead of bare `PieceHoverCard`.
- `RunBuildUiBootstrap` / `BuildScreenHudController`: validate `UnitCardPanelView` references at runtime; log clear error if prefab missing.
- Existing Run scenes: one-time re-run setup menu or manual prefab assign.

---

## Section 3 — Shop offer card asset extraction

### Current state

- `ShopView` already uses `offerCardPrefab` + `ShopOfferView.Bind()`.
- Prefab is created procedurally in `RunSceneSetup.CreateOfferCardPrefab()` and stored as inactive child — not a Project asset.

### Target state

| Asset | Purpose |
|-------|---------|
| `ShopOfferCard.prefab` | Saved hierarchy matching current `CreateOfferCardPrefab` output |
| `ShopView.offerCardPrefab` | References asset in `Assets/_Project/Presentation/UI/Prefabs/` |

### Migration steps

1. Editor utility menu **or** one-shot export: build hierarchy once, save as prefab, wire serialized refs on `ShopOfferView`.
2. Update `RunSceneSetup.CreateShopSection` to load prefab from `Resources` or `[SerializeField]` default path instead of `CreateOfferCardPrefab()`.
3. Keep `CreateOfferCardPrefab()` as deprecated fallback only if asset missing (log warning).
4. Delete inactive procedural prefab child from generated scenes on next setup pass.

### Unchanged behavior

- `ShopOfferView.ConfigureLayout`, lock toggle, drag via `ShopOfferDragSource`, piece preview rendering.
- Grid sizing via `ShopLayoutMetrics` — binder still adjusts `LayoutElement` sizes on bind.

---

## Section 4 — Multi-cell footprint input

### New type: `BoardPieceFootprintHit`

**Path:** `Assets/_Project/Presentation/Board/BoardPieceFootprintHit.cs`

Attached to `PieceShapeVisual` root in `BoardView.CreateShapeVisual` (or inside `PieceShapeVisual.Create` return setup).

| Responsibility | Detail |
|----------------|--------|
| Hit area | Full footprint `RectTransform`; `Image` alpha 0, `raycastTarget = true` |
| Events | `IPointerEnter`, `IPointerExit`, `IBeginDrag`, `IDrag`, `IEndDrag` — same as `BoardPieceDragSource` |
| Data | Configured with `instanceId`, `definition`, `anchor`, `rotation`, `BoardView`, `PieceHoverCardController` |
| HQ | No drag if HQ tag |

### `BoardView` changes

- **Remove** anchor-only `BoardPieceDragSource` on tile in `RefreshOccupancyVisuals`.
- **Add** `BoardPieceFootprintHit.Configure(...)` when creating shape visual.
- Occupied tiles may still show occupancy tint; pointer hits shape overlay first (drawn above grid).

### Edge cases

- Moving between cells of same piece: `OnPointerExit` on one cell must not hide card if entering another cell of same piece — use **reference count** or **shared piece hover id** on controller (`HoverLock instanceId`) to prevent flicker.
- Drag start hides card (existing behavior).

---

## Section 5 — Testing

### EditMode (new)

| Test class | Cases |
|------------|-------|
| `PieceCardViewTests` | Bind populates name/HP; synergy lines visible when context provided; overflow chip when >4 tags |
| `BoardPieceFootprintHitTests` | Pure helper: given board + instanceId + cell coord → resolves owning piece (if logic extracted) |

Use `PieceCardView` on prefab loaded via `Resources.Load` in test or test-specific minimal prefab fixture.

### PlayMode (regression)

- `ShopViewPlayModeTests.Render_CreatesOfferCardsInUnifiedGrid`
- `ShopOfferDragPlayModeTests`
- Manual: hover 2×2 building on board → card filled; drag from non-anchor cell

### TDD order

1. Footprint hit resolver / hover lock tests  
2. `PieceCardView` bind tests  
3. Implement footprint hit + prefab migration  
4. PlayMode shop regression  

---

## Section 6 — Out of scope

- UI Toolkit migration  
- Floating cursor-following tooltip card (can reuse prefab later)  
- Reserves bench card prefab (follow-up; same pattern)  
- Combat arena unit tooltips  
- Changing `PieceCardViewModelBuilder` rules  

---

## Section 7 — Risks & mitigations

| Risk | Mitigation |
|------|------------|
| Hover flicker between footprint cells | Piece-level hover lock on controller |
| Scene references break after prefab migration | Editor setup menu + bootstrap validation logs |
| Shop prefab size drift vs grid metrics | Keep `ConfigureLayout` in code; prefab only structure/styling |
| Duplicate bind logic during migration | Extract shared bind from `PieceHoverCard` → `PieceCardView`; thin wrapper on old type temporarily |

---

## Section 8 — File map

| File | Action |
|------|--------|
| `PieceCardView.cs` | **Create** |
| `UnitDetailCard.prefab` | **Create** |
| `ShopOfferCard.prefab` | **Create** |
| `TagChip.prefab` | **Create** (optional) |
| `UnitCardPanelView.cs` | Modify — use `PieceCardView` |
| `BoardPieceFootprintHit.cs` | **Create** |
| `BoardView.cs` | Footprint hit; remove tile drag |
| `PieceShapeVisual.cs` | Optional: add hit image in Create |
| `PieceHoverCardController.cs` | Minor cleanup |
| `RunSceneSetup.cs` | Load prefab assets |
| `ShopView.cs` | Document default prefab path |
| `PieceCardViewTests.cs` | **Create** |
| `BoardFootprintInputTests.cs` | **Create** |

---

## Approval

Brainstorming approved 2026-06-19: Approach B with shop prefab asset extraction in same pass (scope B).
