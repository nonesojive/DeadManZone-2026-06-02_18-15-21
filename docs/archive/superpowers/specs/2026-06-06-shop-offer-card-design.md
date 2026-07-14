> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — Shop Offer Card Design Spec

**Date:** 2026-06-06  
**Engine:** Unity  
**Status:** Approved — implementation plan in `docs/superpowers/plans/2026-06-06-shop-offer-card.md`  
**Builds on:** `2026-06-04-build-screen-layout-design.md`, existing horizontal shop lanes and `ShopView` / `ShopOfferView`  
**Scope:** Visual redesign of shop offer cards, lane slot layout (3–5 offers), drag-to-buy ghost behavior

---

## Summary

Redesign each shop offer as a **rounded square card** with a board-scale piece preview centered inside, a **name strip** along the bottom, a **price badge** at the top-left, and a **lock icon button** at the top-right. Lanes use **dynamic spacing** with **fixed card size** sized for up to five offers across. Cards are **spawned as complete prefabs** on shop populate (same pattern as today). While dragging to buy, the **piece preview hides on the card** and the player drags a **piece-shape ghost** at board cell scale.

---

## Section 1 — Offer card visual layout

### Card chrome

| Element | Placement | Notes |
|---------|-----------|--------|
| **Rounded square frame** | Full card | Primary hit area for drag; uses theme card/inventory styling |
| **Piece preview** | Center of square | Multi-cell footprint at **1:1 board cell scale** (reuse `ShopPiecePreview` / board metrics) |
| **Name strip** | Thin bar along bottom edge | Single line; ellipsis overflow; piece display name |
| **Price badge** | Top-left corner | e.g. `5G`, `2R`, `5G + 2R` |
| **Lock control** | Top-right corner | Small icon button; does not block drag on the card body |

### Card dimensions

- Square body sized for a **3×3 board cell viewport** (existing `ShopLayoutMetrics.IconAreaSize`).
- Name strip height: compact (~20–24px at reference resolution); included in total card height below the square.
- Lock and price badges sit **inside** the square region (overlay), not in the name strip.

### Locked state

When an offer is locked:

1. **Lock icon** shows locked visual (closed padlock / accent).
2. **Light tint or overlay** on the card (refine current `lockedIndicator` behavior — subtle, not obscuring name or price).

When unlocked: open-lock icon, normal card chrome.

---

## Section 2 — Lane layout (3–5 offers)

### Spawn model (approved)

- **One prefab instance per active offer** when `ShopView.Render` runs (destroy/recreate or pool later — MVP: same as current `RebuildLane` instantiate pattern).
- The prefab **is** the slot shell; `ShopOfferView.Bind(offer, …)` fills preview, name, price, lock state.
- **No** pre-placed empty slot GameObjects in the scene.

### Spacing model (approved)

- Compute each card’s **width/height once** assuming **maximum five offers** in a lane (worst-case horizontal fit).
- **Only active offers** are instantiated; `HorizontalLayoutGroup` (or equivalent) spaces them evenly in the lane offers region.
- Typical case: **3 offers** → same card size, more gap between cards.
- Offensive lane may exceed 3 when `ExtraGeneralSlots` modifiers apply; layout must not resize cards when count changes.

### Lane capacity (gameplay)

| Lane | Default offers | Max (current rules) |
|------|----------------|---------------------|
| Offensive | 3 | 3 + `ExtraGeneralSlots` |
| Defensive | 3 | 3 |
| Specialty | 3 (when unlocked) | 3 |

Layout budget uses **5** as design maximum so future slot expansions do not require prefab changes.

---

## Section 3 — Drag-to-buy behavior

### Requirement

Player drags from the offer card to board or reserves to buy/place. During drag:

| Phase | Card on lane | Under cursor |
|-------|----------------|--------------|
| Idle | Full preview visible in square | — |
| Drag start | **Piece preview hidden**; frame, name strip, price, lock remain | **`DragGhost`** showing piece footprint only |
| Dragging | Preview area empty | Ghost follows pointer; Q/E rotate (existing) |
| Drop accepted | Card removed (offer consumed) | Ghost destroyed |
| Drop cancelled | **Preview restored** | Ghost destroyed |

### Implementation notes

- Toggle preview visibility from `ShopOfferView` on drag begin/end (via `ShopOfferDragSource` or `DragDropController` callbacks).
- **Do not** drag the entire card Transform; only the ghost moves.
- **`DragGhost` cell size** must match board/shop preview cell size (today uses fixed 36px — align with `BoardView.CellSize` during this work).
- Lock button remains clickable without starting a drag (standard button hit target).

---

## Section 4 — Components & files

### Touch / extend

| Component | Change |
|-----------|--------|
| `ShopOfferView` | New layout refs (badge, lock icon, name strip, preview root); preview hide/show for drag; locked overlay |
| `ShopOfferDragSource` | Notify view on begin/end drag (or subscribe to controller events) |
| `DragGhost` | Accept dynamic cell size from board metrics |
| `DragDropController` | Optional: drag lifecycle events for shop cards |
| `RunSceneSetup` | Rebuild `OfferCard` prefab hierarchy to match layout |
| `ShopView` | Lane layout unchanged logically; may pass max-slot hint for sizing |

### New / optional

- `ShopOfferCardLayout` — static metrics for badge/strip sizes if `ShopOfferView` grows too large (split only if needed).

### Out of scope

- Empty slot placeholders in lanes
- Object pooling for offer cards
- Hover-only lock UI
- Changing shop generation, pricing, or lock persistence rules

---

## Section 5 — Testing

| Test | Expectation |
|------|-------------|
| Play Mode: shop refresh | One card per offer; 3 cards spaced in lane |
| Modifier: extra offensive slot | 4 cards; same card size, tighter spacing |
| Lock toggle | Icon + overlay update; offer persists on reroll when locked |
| Drag from shop | Preview disappears on card; ghost shows shape; cancel restores preview |
| Drop on valid board/reserves | Purchase flow unchanged |
| Locked drag | Same drag rules unless design later blocks locked offers (current: locked offers remain draggable) |

---

## Section 6 — Success criteria

1. Offer reads as a **rounded square** with centered piece at board scale.
2. Name and price are readable without overlapping the preview.
3. Price top-left, lock top-right, locked state uses **icon + tint**.
4. Lanes with 3 offers look intentionally spaced; 5 offers still fit without overflow.
5. Dragging feels like moving the **piece**, not the shop card.

---

## Spec self-review

- **Placeholders:** None.
- **Consistency:** Aligns with horizontal lanes, `BuildLayoutMetrics` vertical alignment, and existing `ShopGenerator.OffersPerLane = 3`.
- **Scope:** Presentation only; no core shop logic changes.
- **Ambiguity resolved:** Price top-left (not top-right); lock top-right; drag hides in-card preview only.
