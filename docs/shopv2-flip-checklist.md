# ShopV2 flip checklist — the last mile of the shop visual rework

**Status: the flip is DONE.** All 8 steps executed, both suites green (451 EditMode /
14 PlayMode), 1080p screenshot audit passed against the approved reference
(`Screenshots/shopv2_polished.png`). Old chrome is toggled off, **not deleted** —
that still needs owner sign-off.

`ShopV2Canvas` is ACTIVE in `Run.unity` (overlay, order 10) and is the live shop.

---

## Flip steps — outcomes

1. **Boards into the new frames.** DONE. Real `HqBoardSection`, `CombatBoardSection`,
   `ReservesSection` re-parented into the ShopV2 frames at the placeholders' rects;
   placeholders deleted; `RunUiAuthoringLock` asserted on `ShopV2Canvas`.
2. **SellZone.** DONE. Old drop-zone lives as an invisible raycast overlay
   (`ShopBand/SellZone/SellDropZoneHit`) filling the Smelter rect.
3. **Old chrome off.** DONE — but see **"A scene-level off is not an off"** below; this
   step was silently undone at runtime and needed a code fix.
4. **Critical mass drawer.** DONE — **DEVIATED from the plan.** The plan said "delete the
   ShopV2 stub panel". Owner chose BOTH instead: the stub is now `CriticalMassStrip`, a
   live always-visible summary (top-2 rules, icon + progress, bound by
   `ShopV2CriticalMassPresenter`), and **clicking it opens** the real `CriticalMassDrawer`,
   which is re-docked under `ShopV2Canvas` as a sorting island (order 260). The drawer's
   right-edge tab is deactivated — the strip is the opener now.
5. **Drag systems.** DONE. Ghost sorting fixed (see `DragLayer`). Offer drag-to-board and
   drag-to-reserves wired on the V2 slots. **Open question resolved: left-click purchase
   STAYS alongside drag** — click buys to reserves, drag places.
6. **Hovercards.** DONE. `PieceHoverCardController` routes board-piece hover to
   `ShopV2HovercardPresenter` whenever V2 owns the shop. It passes the
   `PieceCardBuildContext` through, and `ShopV2HovercardView.Bind` runs it through
   `PieceCardViewModelBuilder` — the SAME builder the legacy card uses, so the two cards
   can never disagree about a piece. Placed pieces show synergy-adjusted stats inline
   (`DAMAGE 12 +3`, gold) with synergy / critical-mass / salvage lines in the `Flavor` slot.
   A shop offer passes no context → flat base stats, context block collapses.
7. **Front Report band.** DONE. Legacy `FrontReportPanel` (overlay 250) retired — it was a
   SECOND fight selector with its own `ChooseFightOption` buttons. Its intel is ported: the
   V2 `FightCard`s now carry the stakes line (authority cost / dread / spoils) sourced from
   `DreadRules`, not hardcoded.
8. **Reroll.** DONE — **resolved OPPOSITE to the plan's assumption.** The plan assumed the
   M6 metal plate (`tile63`) would carry over. Owner chose the **V2 kit look**: `RerollPlate`
   keeps `ui_rounded_fill` + outline, matching the offer slots, Smelter and March buttons.
   The approved reference render confirms this. Old `REROLLButton` retires with the chrome.

---

## THE bug pattern of this flip — read this before touching ShopV2

### Mock decoration is not state

The ShopV2 layout was authored as a **visual mock**. Two consequences bit us three separate
times, and will bite again:

**1. `raycastTarget = false` on every authored graphic.** The canvas was art, not UI. A
Button whose `targetGraphic` can't be raycast is *completely inert* — it looks perfect and
does nothing. This killed the fight modal, every offer slot, the reroll plate, the menu
button and Begin Combat. **A presenter must OWN its hit surface** (set `raycastTarget = true`
where it binds), never trust the art.

**2. Showcase states baked into the art.** The mock hand-painted individual elements to
*demonstrate* states — offer slot 1 was painted with `LockedGoldTint` as its background,
fight card 1 with a bronze "selected" border. Any presenter that reads a baseline off the
art inherits the decoration forever. The original `ShopBandPresenter` did exactly that
(`slot.OriginalColor = slot.Background.color`), pinning slot 1 to looking permanently locked.

**The rule: a presenter owns EVERY stateful colour on the thing it binds, derives all of
them from `RunState`, and writes all of them on every bind. It inherits nothing from the
art.** See `ApplyVisualState` (ShopBand) and `ApplyCardVisual` (FightOrders).

### A scene-level "off" is not an off

Step 3 switched `TopBar` / `ShopArea` off **in the scene**. But
`RunSceneController.SetCombatPresentationLayout` does `shopArea.SetActive(!combatActive)` —
so every Build-phase refresh **turned them back on**. The old offer cards, the old metal
reroll plate and the old resource bar were rendering behind the V2 shop the whole time; the
auto-opening Front Report modal just happened to cover them, so playtesting never caught it.
Only the screenshot audit did. Those toggles are now gated on `ShopV2Surface.IsActive`.

**If code can turn it on, turning it off in the scene is not enough.**

---

## Architecture added during the flip

- **`ShopV2Surface`** (`Presentation/ShopV2/`) — single source of truth for "does V2 own the
  shop?". Legacy runtime builders (`FrontReportPanel`, `CriticalMassDrawerBootstrap`, the
  chrome toggles, `RunHudView`'s meta strip) ask here before spawning/showing.
  - `IsActive` — V2 owns the shop (true even while hidden for combat).
  - `IsVisible` — V2 is actually on screen. **These are different on purpose**: during combat
    V2 still owns the shop (so legacy shop chrome must stay retired) but isn't drawn (so the
    combat meta strip is free to return).
  - `SetVisible()` toggles the **Canvas component**, never the GameObject — deactivating the
    GO would break `GameObject.Find` and flip `IsActive` false mid-combat, resurrecting the
    legacy Front Report band.
- **`DragLayer`** (`Presentation/DragDrop/`) — top-most layer (overrideSorting, order 900)
  the drag ghost is parented to. It is **nested under the live shop canvas**, not standalone:
  the two shop canvases use different `CanvasScaler` match values (legacy 0, V2 0.5), so a
  standalone canvas would size ghost cells out of step with the board cells they must align
  to. No `GraphicRaycaster` — the ghost must not eat its own drop.

### Canvas sorting stack (authoritative)

```
Canvas (base)          0
ShopV2Canvas          10
CriticalMassDrawer   260   (nested, overrideSorting)
RunMetaStrip         300   (hidden while the V2 shop is visible)
DragLayer            900   (nested, overrideSorting)
PauseMenu           1000   (nested, overrideSorting)
RunEndOverlay       1100   (nested, overrideSorting)
```

**Nested-canvas trap:** a nested Canvas *ignores* `sortingOrder` unless `overrideSorting` is
true — it silently inherits the parent's order. Worse, setting `overrideSorting` from an
editor script can fail to serialize; it must be written via `SerializedObject`
(`m_OverrideSorting`). Verify it actually stuck, or the layer will still render underneath.

---

## Validation gate

- [x] EditMode (451) + PlayMode (14) green.
- [x] Save/continue roundtrip mid-shop — now a **test**, not a click-through:
      `RunSaveSerializerTests.SerializeDeserialize_PreservesLockedOffersAndChosenFront`
      (locked offer keeps its `SlotIndex`, chosen front + hard-tier `ConditionId` survive).
- [x] Screenshot audit at 1080p vs the approved reference.
- [ ] Full playtest loop: new run → shop → combat → aftermath → next shop. *In progress with
      the owner; everything reported working so far.*
- [ ] **Old chrome deletion — awaiting owner sign-off.** Everything is toggled off, nothing
      destroyed.

---

## Known deferred items

- Vertical teal `RESERVES` tab on the reserves grid is legacy chrome the reference lacks.
- Piece flavor text on hovercards — the `Flavor` slot now carries board context (synergy /
  critical mass / salvage) for placed pieces, but a shop offer still shows nothing there.
  Source real flavor from the `BuildMessagesView.SetFlavorFromPiece` pattern.
- Rarity chrome cross-case: each hovercard prefab carries only its authored trim.
- Dormant slot unlocks: visuals authored; wire to `ShopSlotUnlockRegistry` when the first
  unlocking ability ships.
- Buffs rail expand-on-click + entry tooltips.
- `CriticalMassStrip` shows only the top TWO rules; the rest live in the drawer. If runs
  routinely carry 3+ active rules this will feel lossy — widen the strip or add `I3`.
- Locked offers are still buyable/draggable (lock only pins through reroll). Intentional
  unless told otherwise.
