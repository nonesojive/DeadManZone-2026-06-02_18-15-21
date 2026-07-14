> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# Critical Mass Panel — Design Spec

**Date:** 2026-07-01  
**Status:** Approved

## Goals

1. Buff UI shows **critical mass rules only** — no active abilities or adjacency auras (those stay on the boards).
2. Fix **count/progress display** — aggregate combat + HQ boards; show progress toward next tier when active.
3. Replace bottom-bar buff strip with a **right-edge collapsed tab** + **slide-over detail panel**.

## Logic Changes

### BuffStripEvaluator

- Add `Evaluate(BuildBoardSet boards)` using `boards.ToAggregateBoard()`.
- Remove `AppendActiveAbilityAuras` entirely.
- Progress fields on `BuffStripEntry`:
  - Inactive near-miss: `current / nextThreshold`
  - Active with higher tier: `current / nextTierThreshold`
  - Active at max tier: `current / maxThreshold` (or `MAX` label)
- Collapsed tab counts **active critical masses only**.
- Expanded panel shows **active + near-miss** rules with full detail text.

### HUD wiring

- `BuildScreenHudController` refreshes from `RunOrchestrator.GetBuildBoards()` (or equivalent), not combat-only `BoardView`.

## UI

### CriticalMassTabView (collapsed)

- Docked to right screen edge during build phase.
- Label: `"N active buffs"` where N = active critical mass count.
- Click toggles panel open/closed.

### CriticalMassPanelView (expanded)

- Slides over right ~50% of screen (~200ms anchor lerp).
- Scrollable list: icon, abbreviated name, count/progress, bonus detail.
- Click tab again or optional backdrop dismiss to close.

### Layout

- Remove `BuffStripRegion` from bottom bar; rename region **`InfoMessageRegion`** for `MessagesText` / build feedback.
- `CenterColumnLayoutFitter` aligns `InfoMessageRegion` to main-row center column.
- Critical mass UI: right-edge **drawer tab** + slide-over panel (not bottom strip).

## Tests (EditMode)

`BuffStripEvaluatorTests`:

- Aggregate board counts pieces on combat + HQ.
- No ability/adjacency entries in results.
- Progress label thresholds for active, near-miss, and max-tier cases.

## Out of Scope

- New content in freed bottom-bar space.
- Aligning `TickCombatRun` combat mass evaluation with aggregate board (follow-up).
