> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# Board and HQ Redesign

## Goal

Replace the current single zoned board with two explicit build-phase boards: a 6x6 combat board for anything that fights, and a faction-specific HQ board for buildings, economy, and fight-start effects. Combat becomes cleaner, smaller, and 2D-only while the HQ board becomes the long-term place for faction economy identity.

## Core Decisions

- **Combat board:** fixed 6x6 per side. Infantry, vehicles, and combat structures are placed here.
- **HQ board:** faction-specific rectangle with optional blocked cells. IronMarch Union starts as 6x3.
- **Battlefield projection:** combat uses player 6x6, neutral 5x6, enemy 6x6.
- **Zones removed:** Rear, Support, and Front are no longer visual or placement zones.
- **Front rules remain:** player front means the rightmost combat-board column; enemy front means the leftmost enemy column after mirroring.
- **HQ removed from combat:** HQ is no longer a piece, target, or win/loss condition.
- **Enemies:** enemy templates only use combat boards. They do not have HQ boards.
- **Reserves:** one shared reserves grid stores all owned unplaced pieces.
- **Save compatibility:** clean schema break. Old saves are invalidated instead of migrated.

## Placement Model

Board membership defines combat participation.

- Buildings are HQ-board only.
- Infantry, vehicles, and structures are combat-board only.
- Anything placed on the combat board is involved in combat.
- `combatant` and `noncombatant` tags should be retired as combat gates.
- Invalid drops should explain the board rule, for example "Buildings must be placed on the HQ board" or "Units must be placed on the combat board."

This keeps the content model simple: piece kind decides legal board, and board ownership decides whether the sim receives the piece.

## HQ Board Effects

The combat and HQ boards are spatially separate. There is no cross-board adjacency.

Allowed interactions:

- HQ-board buildings may query combat-board pieces by tags, roles, counts, or board positions.
- Combat pieces may query HQ-board buildings by tags or counts.
- Same-board adjacency still works within each board.
- Fight-start effects can bridge the boards, such as "+10 HP to all Assault units" or "+1 attack per Command-tagged building."

HQ effects should evaluate before combat starts and produce deterministic buffs for the combat sim. They should not create runtime battlefield entities.

## Combat Flow

The new flow is:

```text
Shop/build
-> combat loading
-> 2D arena loaded and visible
-> opening tactics pause
-> combat begins
-> one side reaches 60% total combat-board HP
-> mid-fight tactics pause
-> combat continues
-> win/loss/draw
```

Pause behavior:

- Opening pause happens before tick 0.
- Opening pause allows tactic selection and abilities with timing `Opening` or `Any`.
- The 60% pause fires once when either army reaches 60% total combat-board HP.
- The 60% pause allows normal mid-fight tactics and abilities with timing `MidFight` or `Any`.
- Existing 75% and 30% thresholds are replaced by this model.

Win/loss behavior:

- Player loses when no player combat-board pieces with battlefield HP remain alive.
- Player wins when no enemy combat-board pieces with battlefield HP remain alive.
- HQ destruction no longer exists.

## Build UI

The build phase shows the combat board and HQ board side-by-side.

- Shop and shared reserves can drag to either board.
- The combat board should visually read as the source layout for the arena.
- The HQ board should visually read as the economy and command layer.
- Board labels can identify the two boards, but combat sub-zones should not be shown.

## Combat Presentation

Combat presentation is 2D-only moving forward.

- Render only the player and enemy combat boards plus neutral band.
- Do not render the HQ board in combat.
- Keep the current 2D arena style and adapt it to the 6x6 / 5x6 / 6x6 dimensions.
- Remove old 3D/Synty arena code and prefabs in a later cleanup pass after the new 2D flow is stable.

## Implementation Slices

1. Add the new save/data model with `CombatBoard`, `HqBoard`, and shared reserves.
2. Add placement validators for combat-board and HQ-board eligibility.
3. Update board snapshot restore and schema invalidation.
4. Project only combat boards into combat.
5. Remove HQ combat win/loss and combatant/noncombatant gate assumptions.
6. Add opening pause and single 60% pause.
7. Add ability timing: `Opening`, `MidFight`, `Any`.
8. Update build UI to side-by-side boards.
9. Adapt 2D arena binding to 6x6 / 5x6 / 6x6.
10. Cleanup old zones, HQ combat behavior, and 3D arena leftovers.

## Testing Gates

Use TDD for the implementation.

- Board tests: combat/HQ placement, blocked HQ cells, no zone placement, and shared reserves restore.
- Save tests: new schema round-trip and old save invalidation.
- Combat tests: opening pause before tick 0, one 60% pause, ability timing filters, no HQ win/loss, and elimination by combat-board pieces.
- Effect tests: HQ-board effects buff combat-board pieces, combat pieces can count HQ-board tags, and no cross-board adjacency is used.
- Presentation tests where practical: side-by-side board binding and 2D arena initialization from combat boards only.

## Non-Goals

- No old-save migration.
- No enemy HQ boards.
- No visual zone labels on the combat board.
- No arbitrary HQ polygon shapes beyond rectangle plus blocked cells.
- No 3D arena cleanup until the new 2D combat flow is stable.
