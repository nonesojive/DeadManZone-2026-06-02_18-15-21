# ShopV2 flip checklist — the last mile of the shop visual rework

State when this was written: `ShopV2Canvas` is STAGED INACTIVE as a root object in
`Run.unity` (overlay, sorting order 10 — below the Front Report band at 250). The lab
scene `ShopBuildV2.unity` remains the iteration sandbox. All presenters bind live
RunState and were smoke-verified in play mode (fight selection, lock-survives-reroll,
begin-combat gating, both hovercards). Suites green at the wave-2 commit.

The flip is deliberately owner-present: it re-parents the REAL board views and retires
the old chrome, and the drag/drop feel needs hands-on playtesting before sign-off.

## Flip steps (execute in order, playtest between groups)

1. **Boards into the new frames.** Move `Canvas/RunScene/ShopScene/MainRow/BoardArea`'s
   real sections (`HqBoardSection`, `CombatBoardSection`, `ReservesSection`) to the
   ShopV2 layout positions (HQ region left — anchor sized for 4×8 max; combat center;
   reserves 6×2 beneath). Delete the decorative `HqGrid`/`CombatBoard`/`ReservesBoard`
   placeholder grids from ShopV2Canvas as each real section takes its place.
   `BoardAreaTripleLayoutSetup` (Presentation/Editor) is the precedent tool for board
   layout moves. Watch `RunUiAuthoringLock`: the flip IS the sanctioned structural
   migration; keep the lock asserted afterward so bootstraps never re-migrate.
2. **SellZone.** Point the existing `BoardArea/SellZone` drop logic at the ShopV2
   `SellZone` (Smelter) rect, or move the component onto it. Kill the white trash can.
3. **Old chrome off.** Deactivate `TopBar`, `ShopArea`, and `BottomBar/COMBATButton`
   (keep `InfoMessageRegion` if the flavor line stays — otherwise fold into ShopV2).
   Activate `ShopV2Canvas`. Menu button: wire `MenuButton` → same handler as old
   `MENUButton` (pause menu).
4. **Critical mass drawer.** Re-dock the REAL `CriticalMassDrawer` under the ShopV2 HQ
   region (it keeps its own sorting island, order 260). Delete the ShopV2 stub panel.
5. **Drag systems.** Verify `DragGhost` / `ShopPiecePreview` / `PieceShapeVisual` render
   above the new chrome (their canvases/sorting vs overlay order 10) and that offer
   drag-to-board still works alongside the new slot click-to-reserves input. Decide
   whether left-click-purchase stays or defers to drag-only.
6. **Hovercards.** Old `UnitCardPanelView` center-column card: replace with
   `ShopV2HovercardPresenter` (board piece hover: call it from
   `PieceHoverCardController.NotifyPieceHoverEnter`, or swap the controller's panel
   resolution to the V2 host). Keep `PositionOppositePointer` semantics.
7. **Front Report band.** The old band cards (overlay 250) are superseded by the ShopV2
   modal + Fight Orders tray — retire the old band AFTER confirming the modal covers
   all its intel (salvage targeting hints, report intel fields).
8. **Reroll.** Old `REROLLButton` keeps its authored metal plate per M6 — decide: move
   the authored sprite onto the ShopV2 `RerollPlate` or keep the old button object
   re-parented into the shop band.

## Validation gate (all must pass before deleting anything)

- EditMode + PlayMode suites green.
- Full playtest loop: new run → shop (buy via drag AND click, reroll, lock/unlock,
  sell to Smelter, fight select, change selection) → combat → aftermath → next shop.
- Save/continue roundtrip mid-shop (locked offer + chosen front persist).
- Screenshot audit at 1080p against the approved wireframe (v3).
- Old chrome deletion happens only after owner sign-off; until then everything is
  toggled off, not destroyed.

## Known deferred items

- Piece flavor text on hovercards (TODO in `ShopV2HovercardView` — source from the
  `BuildMessagesView.SetFlavorFromPiece` pattern).
- Rarity chrome cross-case: each hovercard prefab carries only its authored trim
  (rare unit / uncommon building degrade to name+tag color).
- Dormant slot unlocks: visuals authored; wire to `ShopSlotUnlockRegistry` when the
  first unlocking ability ships.
- Buffs rail expand-on-click + entry tooltips.
