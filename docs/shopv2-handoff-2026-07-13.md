# ShopV2 handoff — session end 2026-07-13

**State:** branch `shopvisualrefreshv1`, commit `611de848`, **merged to `master`** (fast-forward,
0/0 divergence). Suites **green: 452 EditMode / 14 PlayMode**. Scene `Run.unity` saved.

The ShopV2 flip is **DONE** — all 8 steps of `docs/shopv2-flip-checklist.md`, plus a full
interaction-feedback pass on top. Old chrome is toggled off, **not deleted**: that still needs
owner sign-off.

**Read first, in order:** `CLAUDE.md` → `docs/shopv2-flip-checklist.md` (rewritten this session,
now accurate) → this file.

Working tree noise, deliberately uncommitted: `Assets/_Recovery*`, `Untitled.blend*`,
`*.prebranch.bak`, `Screenshots/`, and `TextMesh Pro/.../LiberationSans SDF - Fallback.asset`
(runtime atlas churn, not ours).

---

## THE lesson of this session — read before touching ShopV2

### Mock decoration is not state

The ShopV2 canvas was authored as a **visual mock**. The mechanisms were all present; their
**values were never authored**. This produced FOUR separate bugs that all look different and are
all the same bug:

1. **`raycastTarget = false` on every authored graphic.** A Unity Button whose `targetGraphic`
   cannot be raycast is *completely inert*. The fight modal, all five offer slots, reroll, the menu
   button and Begin Combat looked perfect and did nothing. Sell worked *only* because
   `SellDropZoneHit` had been hand-added in an earlier session.
2. **Showcase states hand-painted into the art.** Offer slot 1 was painted with `LockedGoldTint`
   as its *background*; fight card 1 with a bronze "selected" border. `ShopBandPresenter` seeded
   its baseline **from the art** (`OriginalColor = Background.color`) — pinning slot 1 to looking
   permanently locked, forever.
3. **Buttons had `transition = ColorTint` with Unity's DEFAULT ColorBlock.** Tint *multiplies* the
   base graphic. The buttons are near-black (`0.09`), so hover computed to `0.09 × 0.96 = 0.086`
   — a **0.4% change**. The feedback fired perfectly and was mathematically invisible. The buttons
   were never "missing" feedback; it was unauthored.
4. **The critical-mass strip had exactly two authored icon slots**, so it silently truncated a
   6-rule board to 2.

> **The rule, now enforced:** a presenter owns **every stateful colour** on the thing it binds,
> derives all of them from `RunState`, writes **all** of them on every bind, and **inherits nothing
> from the art**. See `ApplyVisualState` (ShopBand) and `ApplyCardVisual` (FightOrders).

### A scene-level "off" is not an off

Flip step 3 switched `TopBar` / `ShopArea` off **in the scene**. But
`RunSceneController.SetCombatPresentationLayout` does `shopArea.SetActive(!combatActive)` — so
**every Build-phase refresh turned them back on**. The old offer cards, the legacy metal reroll
plate and the old resource bar were rendering behind the V2 shop the *entire time*; the
auto-opening Front Report modal happened to cover them, so playtesting never caught it. **Only the
1080p screenshot audit did.** Those toggles are now gated on `ShopV2Surface.IsActive`.

> **If code can turn it on, turning it off in the scene is not enough.**

### Authored, not code (owner rule)

All ShopV2 changes must be **authored in the editor** (scene / prefab data), not applied at runtime
by presenters. This session converted the violations: the crit-mass chips are authored objects (was
`BuildChips()` generating them), hovercard host position/scale is scene-only (was set in `Awake`),
and `raycastTarget` flags are authored on the graphics (was set in presenter code). Presenters now
only *read state and write text/colour/sprite*.

Editor **scripts** that mutate and save the scene are fine — the artifact is scene data.

---

## Architecture added this session

- **`ShopV2Surface`** — single source of truth for "does V2 own the shop?". Legacy runtime builders
  (`FrontReportPanel`, `CriticalMassDrawerBootstrap`, the chrome toggles, `RunHudView`'s meta strip)
  ask here before spawning/showing.
  - `IsActive` — V2 owns the shop (**true even while hidden for combat**).
  - `IsVisible` — V2 is actually on screen.
  - **These differ on purpose.** During combat V2 still *owns* the shop (so legacy shop chrome must
    stay retired) but is not *drawn* (so the combat meta strip is free to return).
  - `SetVisible()` toggles the **Canvas component**, never the GameObject — deactivating the GO
    would break `GameObject.Find`, flip `IsActive` false mid-combat, and resurrect the legacy band.
- **`DragLayer`** — top-most layer (overrideSorting, 900) the drag ghost parents to. **Nested under
  the live shop canvas**, not standalone: the two shop canvases use different `CanvasScaler` match
  values (legacy 0, V2 0.5), so a standalone canvas would size ghost cells out of step with the
  board cells they must align to. No `GraphicRaycaster` — the ghost must not eat its own drop.
- **`ShopV2ButtonAffordance`** — drives the *border* channel of button feedback (a Selectable's
  ColorTint can only tint ONE graphic). Palette serialized. Where a presenter already owns the
  border's base colour (MarchButton = tier accent), that presenter calls `SetPalette` and the
  affordance layers interaction on top — a fixed gold hover would erase the tier read.
- **`ShopV2Tooltip` + `ShopV2TooltipPresenter`** — one shared panel. The **hover highlight and the
  tooltip are the same component on purpose**: the highlight is the *affordance for* the tooltip.
  A glow on something that then explains nothing teaches players to stop hovering.
- **`ShopDragMetrics`** — shared ghost cell metrics so the legacy and V2 shops can't drift.

### Canvas sorting stack (authoritative)

```
Canvas (base)          0
ShopV2Canvas          10
CriticalMassDrawer   260   nested, overrideSorting
RunMetaStrip         300   hidden while the V2 shop is visible
DragLayer            900   nested, overrideSorting
TooltipHost          950   nested, overrideSorting
PauseMenu           1000   nested, overrideSorting
RunEndOverlay       1100   nested, overrideSorting
```

---

## Gotchas earned this session (all cost real time)

- **A nested Canvas IGNORES `sortingOrder` unless `overrideSorting` is true** — it silently
  inherits the parent's. Worse: **setting `overrideSorting` from an editor script can fail to
  serialize.** It must go through `SerializedObject` (`m_OverrideSorting`). Always verify it stuck,
  or the layer still renders underneath and you will "fix" the same bug twice.
- **`ScreenPointToLocalPointInRectangle` returns CANVAS-CENTRE coordinates.** `TooltipHost` is
  anchored top-left, so the tooltip was parked 252px above the screen. It "worked" perfectly and was
  invisible. Convert into the anchor's space.
- **Editor Play Mode starts from the OPEN SCENE, not build index 0.** Anything touching boot order
  (button wiring, `RunSceneController.Awake`) **must** be tested from `MainMenu.unity`. I twice
  "reproduced a failure" that was purely an artifact of booting straight into `Run.unity` — the
  MENU button had no listeners because Awake never ran the normal path.
- **Editing a prefab does nothing if the scene copy is UNPACKED.** The hovercards were unpacked
  copies. Diff before relinking (they turned out to be 0-drift, so relink was safe — now
  **0 overrides**, prefabs are genuinely the source of truth).
- **Existing shop tests asserted against `ShopSlotLayoutResolver.VisibleOfferSlotCount`** — i.e.
  they were *tautological* and would pass at any value. Added
  `DefaultBoard_RollsExactlyFiveOffers_InSlotsZeroToFour` pinning it literally.
- **Presenter name-binding is still the API.** Renaming an authored child breaks the binding
  silently except for the one consolidated `LogWarning` each presenter emits. (I renamed the V2
  `CriticalMassDrawer` → `CriticalMassStrip` — safe, checked, but exactly the trap.)
- `script-execute` edits **during Play Mode evaporate**. Check `editor-application-get-state` first.
  An `assets-refresh` while playing kicks you out of play mode mid-script.

---

## Bugs fixed (the non-obvious ones)

- **`ShopV2ShopBandPresenter` bound offers by LIST POSITION, not `SlotIndex`.** Consuming one offer
  shortened the list and shifted every remaining offer into the wrong slot. This is what surfaced
  the "6th offer". It would have been wrong even at 5 offers.
- **Shop rolled 6 offers; the V2 band authors 5.** `VisibleOfferSlotCount` / `ReservedSlotStartIndex`
  6→5, and `slot_5.asset` stamped `ReservedAbility` (stamped, not regenerated — regen would churn
  every profile's authored weights).
- **`PauseMenuView.root` is its own GameObject and `Awake()` ended with `Hide()`.** The menu is
  authored inactive, so Awake ran the *first time `Open()` activated it* — and immediately shut it.
  Hence "the menu needs two clicks the first time"; click 2 worked because Awake only runs once.
  `Open()` also **threw** on `SaveAndExit` with no active run, aborting before it activated anything.
- **Reserves/combat board misalignment** — *not* the teal tab. `GridLayoutCellFitter.columnCount`
  was still **8** (stale from the 8×2 → 6×2 `ReservesState` migration), and the grid's anchors were
  `(0.08, 0.08)–(0.98, 0.92)`: 8% inset left, 2% right. That asymmetry was the visible shift.
- **`RunMetaStrip` (order 300)** printed a duplicate DREAD readout and a SEED label over the V2
  canvas. Hidden while the V2 shop is visible; returns for combat.

---

## What's in the shop now

- 5 offer slots (buy by **click** → reserves, or **drag** → board/reserves), right-click lock,
  hover-lit borders, lock banner on **every** slot, reroll.
- Front Report modal: 3 fight cards, tier accents (bone/bronze/gold), stakes line from `DreadRules`,
  battle-condition chip with a tooltip from `ConditionCatalog`, chosen front reads as chosen.
- `CriticalMassStrip` — up to 6 live rule chips, click opens the re-docked drawer.
- Hovercards — unified for board + shop, synergy-adjusted stats inline (`DAMAGE 12 +3`), board
  context, and **ABILITIES** (active / passive income / granted).
- Feedback: every button has hover (fill warms ×1.8 + border lights gold) / press (fill darkens
  ×0.68 + border flares, instant) / disabled. Tooltips on resources, dread, War Footing rows,
  Smelter, reroll, crit-mass chips, battle condition.

---

## Next up

**Blocking sign-off:**
- **Old chrome deletion.** Everything is toggled off, nothing destroyed. Needs owner go/no-go.
- Full playtest loop: new run → shop → combat → aftermath → next shop. In progress, all good so far.

**Queued (small):**
- **BuffsRail tooltips** — the one element from the feedback pass still unwired.
  `ShopV2BuffsRailPresenter` should push content the way `ShopV2CriticalMassPresenter` does.
- **Building hovercard's abilities block is only 40px (~2 lines).** Fine for current single-line
  income buildings; a long ability will clip.
- **Flavour quote** is parked inactive on both hovercard prefabs — the abilities block took its
  band. Bring it back when there's a real data source (`BuildMessagesView.SetFlavorFromPiece`).
- `CriticalMassStrip` shows the top 6 rules; the rest live in the drawer.
- Locked offers are still buyable/draggable (lock only pins through reroll). Intentional unless told
  otherwise.

**Queued after the flip (unchanged):** enemy pool re-authoring onto
neutral/crimson_legion/ash_wraiths, Dust Scourge/Cartel content passes, recon intel ladder UI,
3D building visuals, boss commander models, enter-seed UI, owner-driven tuning (MoraleRules
death-shock feel, DreadRules/RarityWeights constants).
