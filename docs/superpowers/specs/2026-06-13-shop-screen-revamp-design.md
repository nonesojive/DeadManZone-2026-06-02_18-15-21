# DeadManZone — Shop Screen Revamp Design Spec

**Date:** 2026-06-13  
**Engine:** Unity  
**Status:** Approved (brainstorming) — pending written spec review before implementation plan  
**Builds on:** `2026-06-04-build-screen-layout-design.md`, `2026-06-06-shop-offer-card-design.md`  
**Scope:** Unified shop (6–12 slots), layout reorganization, ShopCard/UnitCard panels, multi-lock reroll, buff icon strip, messages priority stack

---

## Summary

Revamp the build-phase shop screen to match the three-column layout sketch: board/reserves left, Unit Card + buff strip center, unified shop right. Replace three shop lanes with a **single slot-indexed shop** (6 baseline offers in a 3×2 grid, up to 12 in a 4×3 grid when extra slots unlock). Each offer is a **ShopCard** (footprint MVP, art slot reserved). Hovering board, reserves, or shop shows a **fixed Unit Card panel** (hidden when idle). **Messages** use a priority stack for alerts, sell refund, and flavor text. **Buff icons** show active and near-miss synergy/critical-mass effects. **Reroll** is one action for all unlocked slots; **multi-lock** is free to toggle, with Authority charged on reroll (first lock free, +1 Authority per additional lock).

---

## Section 1 — Layout & regions

Three vertical columns in the build panel:

| Region | Position | Content |
|--------|----------|---------|
| **Resources** | Top-left | Supplies, Manpower, Authority, Morale (existing HUD block) |
| **Messages** | Top-center | Priority-stack text (Section 4) |
| **Log / Menu** | Top-right | Event log toggle + pause MENU |
| **Board** | Left-center | 9×10 grid + zone chrome |
| **Unit Card** | Center-center | Fixed panel; hidden when idle; shown on hover |
| **Shop** | Right-center | Unified offer grid (3×2 → 4×3) |
| **Reserves** | Bottom-left | 2×9 grid |
| **Buff icons** | Bottom-center | Synergy / critical-mass strip |
| **Actions** | Bottom-right | SELL, REROLL, COMBAT under shop |

`BuildLayoutMetrics` drops per-lane vertical stacking (`ShopLaneCount` lane anchors). Shop uses one `ShopArea` rect with an internal grid layout fitter.

---

## Section 2 — Unified shop & slot model

### Approach

**Slot-index shop (Option 1).** Replace lane-based generation with numbered slots and per-slot roll profiles. `ShopLane` may remain as internal pool tagging during migration but **slot index is the primary gameplay key**. Faction-specific `ShopSlotProfile` ScriptableObjects can be added later without changing the slot-index architecture.

### Baseline slots

- **6 active slots** by default: indices **0–5**.
- Displayed as **3×2** grid (2 columns × 3 rows).
- **Slot roll bias (baseline):**
  - Slots **0–2**: lean **offensive** pools
  - Slots **3–5**: lean **defensive** pools
- Per-slot weights are data-driven; factions override via profile data.

### Extra slots (indices 6–11)

- Unlock adds slots up to **12 total** → **4×3** grid.
- Unlocked slots are **rendered**; locked-out slots are **not shown** (no empty placeholders in MVP).
- Unlock sources (evaluated each shop refresh):
  - **Faction perks** (e.g. future Cartel of Echoes: +2 slots; one slot always rolls a cross-faction piece)
  - **Board pieces/buildings** with shop modifier flags or a new `UnlockShopSlot` capability
- Extra slots may use Authority-priced offers; pricing is **not lane-gated** — any slot can have `RequisitionPrice` (Authority).

### Offer data model

```csharp
// ShopOffer — SlotIndex is primary; Lane deprecated for gameplay
public int SlotIndex;           // 0–11
public ShopSlotKind SlotKind;   // BaselineOffensive, BaselineDefensive, Extra, SpecialRule...
public int GoldPrice;
public int RequisitionPrice;  // Authority; any slot
public bool IsSalvaged;
```

`ShopGenerator.Generate()`:

1. Resolve active slot count and per-slot profiles from faction + board modifiers.
2. Roll each unlocked, non-locked slot from its profile pool/weights.
3. Preserve locked offers in their slot indices across refresh/reroll.

### Reroll

- **One REROLL button** rerolls **all unlocked slots** (locked slots keep their offers).
- Removes per-lane reroll buttons.
- **Authority on reroll:** first locked slot is free; each additional locked slot costs **1 Authority** at reroll time. Locks are **free to toggle**; cost is applied only when rerolling.
- If player cannot afford Authority lock cost → reroll blocked; system alert in Messages.
- Gold reroll cost scaling unchanged (`RerollCountThisRound`).

### Multi-lock save format

Replace `RunState.LockedOffer` (singular) with a collection, e.g. `List<ShopOfferRecord> LockedOffers` or equivalent slot-index keyed structure.

**Save migration:** None. Old saves with singular `LockedOffer` and lane-based shop are unsupported (consistent with existing bench migration policy).

---

## Section 3 — ShopCard & UnitCard

### ShopCard (per offer)

Wireframe layout; MVP uses footprint in the art area:

| Zone | MVP content | Future |
|------|-------------|--------|
| Top-left | Price badge (`5G`, `2A`, `5G+2A`) | — |
| Top-center | Ability iconography (1–3 small icons from abilities/tags) | Richer ability art |
| Top-right | Lock toggle (free; reroll cost preview when multiple locks) | — |
| Center | **Piece footprint** at board cell scale | Portrait `artIcon` slot |
| Bottom | Display name (ellipsis overflow) | — |

- Drag-to-buy unchanged: in-card preview hides on drag start; piece ghost follows cursor at board cell scale.
- Prefab includes a hidden/disabled `Image artIcon` for future portraits.

Evolve `ShopOfferView` → `ShopCardView` (or extend in place).

### UnitCard (fixed center panel)

Replaces cursor-following `PieceHoverCard` on the build screen:

| Zone | Content |
|------|---------|
| Left sidebar | Combat stats: HP, DMG, move, atk speed, armor/attack type |
| Top | Name + cost badge |
| Center | Footprint preview (MVP); art slot reserved |
| Bottom | Ability text + tag chips |

**Behavior:**

- **Show on hover** of board piece, reserves piece, or shop card.
- **Hidden** when nothing is hovered (no placeholder text).
- Built from `PieceCardViewModel` + `PieceCardViewModelBuilder`.
- Synergy lines and critical-mass hints **removed from UnitCard** — buff strip owns aggregate synergy/critical-mass display (Section 4).

---

## Section 4 — Messages, buff strip, alerts

### Messages (priority stack)

Single top-center text area. Priority (highest wins):

1. **System alerts** — cannot afford purchase, insufficient Authority for reroll lock cost, insufficient Manpower for COMBAT, invalid action feedback. Timed or cleared on next valid action.
2. **Sell hover** — refund preview, e.g. `Sell: +3 Supplies`.
3. **Unit flavor** — short flavor line from piece data on unit/shop hover.
4. **Idle** — empty.

Optional new `flavorText` field on `PieceDefinition` / `PieceDefinitionSO` for authoring.

### Buff icons strip

Bottom-center strip between reserves and shop actions:

- **Active synergies** (from `SynergyEngine.EvaluateFightStart` on current board) → full-color icon; hover shows detail tooltip.
- **Near-miss thresholds** (e.g. 2/3 Infantry for critical mass) → greyed icon; hover shows progress and reward text.
- **Nothing to show** → empty strip (layout space reserved, no icons).
- Data from `SynergyEngine.EvaluateFightStart`, `CriticalMassRules.EvaluateFightStart`, and a new Core helper for near-threshold detection.

---

## Section 5 — Components & files

### Core (new / major touch)

| File / type | Change |
|-------------|--------|
| `ShopSlotProfile` / `ShopSlotResolver` | Per-slot pools, weights, unlock gates |
| `ShopGenerator` | Slot-index generation; extra slot unlock resolution |
| `ShopOffer` | Slot-centric; `SlotKind`; lane deprecated |
| `RunOrchestrator.Shop` | Multi-lock, unified reroll, Authority on reroll |
| `RunState` | `LockedOffers` collection |
| `PieceDefinition` | Optional `flavorText`; optional `UnlockShopSlot` modifier |
| Near-threshold helper | Buff strip near-miss icons |

### Presentation (new / major touch)

| File / type | Change |
|-------------|--------|
| `BuildLayoutMetrics` | Remove lane stacking; unified shop anchors |
| `RunBuildUiBootstrap` / `ShopScreenLayout` | Three-column layout per sketch |
| `UnifiedShopView` | Replaces lane roots in `ShopView` |
| `ShopCardView` | Evolve `ShopOfferView` to wireframe layout |
| `UnitCardPanelView` | Fixed center panel |
| `BuffIconStripView` | Synergy + critical-mass icons |
| `BuildMessagesView` | Priority-stack messages |
| `RunSceneSetup` | Rebuild shop column; remove `OffensiveRow` / `DefensiveRow` / `SpecialtyRow` |

### Deprecated / removed

- Per-lane reroll buttons and `ShopLaneLayoutFitter` lane stacking
- Cursor-following `PieceHoverCard` on build screen (retain for combat if still needed elsewhere)
- `ShopUiBootstrap.ApplyLaneRows` lane row migration

### Out of scope (MVP)

- Portrait art for ShopCard / UnitCard center (slots reserved)
- Empty slot placeholders for locked-out extra slots
- Object pooling for shop cards
- Save migration from lane-based shop / singular lock

---

## Section 6 — Testing

| Area | Expectation |
|------|-------------|
| Slot count | Default 6 offers in 3×2; unlock path shows up to 12 in 4×3 |
| Slot weights | Slots 0–2 offensive-leaning; 3–5 defensive-leaning (statistical or seeded fixture tests) |
| Extra slots | Faction/building unlock adds slots; Cartel-style cross-faction rule on designated slot |
| Multi-lock reroll | 0 locks: no Authority; 1 lock: free; 2 locks: 1 Authority on reroll; blocked if insufficient |
| Unit Card | Hover shows panel; idle hidden |
| Messages | Alert overrides sell hover overrides flavor; idle empty |
| Buff strip | Active + near-miss icons; empty strip when none |
| ShopCard drag | Preview hide/ghost unchanged |
| Regression | Purchase, sell, combat gate, save/load new format |

---

## Section 7 — Success criteria

1. Build screen matches three-column sketch: board left, Unit Card + buff center, shop right.
2. Single unified shop grid replaces three lanes; 6 default, 12 max with unlocks.
3. ShopCards use wireframe layout with footprint MVP and reserved art slot.
4. Unit Card is fixed center panel, hover-only, hidden when idle.
5. Messages follow priority stack; sell and flavor work on hover.
6. Buff strip shows active and near-miss synergy/critical-mass with hover detail.
7. One reroll rerolls unlocked slots; multi-lock with Authority cost on reroll (first free).
8. Extra slots unlock from faction/building rules without lane concept in UI.

---

## Spec self-review

- **Placeholders:** None. Portrait art explicitly deferred; `flavorText` optional at authoring time.
- **Consistency:** Slot indices 0–5 baseline, 6–11 extra align with 3×2 and 4×3 grids. Authority pricing decoupled from deprecated specialty lane. Multi-lock replaces singular lock consistently.
- **Scope:** Single implementation plan target; Cartel of Echoes content can ship after slot framework exists.
- **Ambiguity resolved:** Reroll cost on reroll (not on lock toggle). Near-miss icons greyed; empty strip when nothing to show. Extra slots hidden when locked out (not empty placeholders).
