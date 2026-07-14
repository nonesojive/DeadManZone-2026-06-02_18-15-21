> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — Autobattler Design Spec

**Date:** 2026-05-31  
**Engine:** Unity  
**Status:** Approved for planning  
**MVP scope:** Vertical slice

---

## Overview

DeadManZone is a grimdark, retro-futurist WW1 trench warfare autobattler built in Unity. It combines the shop-heavy strategic build arcs of *The Bazaar* with the spatial inventory and adjacency synergies of *Backpack Battles*. Players shop and arrange buildings and units on a zoned grid, then watch combat auto-resolve across three phases — with limited command decisions between each phase driven by their loadout.

**Core design pillar:** Every run forces a tension between investing in **economy/shop power** (rear buildings, faction special tiles, shop modifiers) and **combat capability** (front-line units, damage pieces).

---

## Reference Model

| Influence | What we take |
|-----------|--------------|
| Backpack Battles | Spatial grid, piece shapes, adjacency/trigger synergies, freeze/reroll shop mechanics |
| The Bazaar | Shop-heavy loop, cooldown abilities, longer build arcs, multiple shop lanes |
| Total War | Combat tempo: positioning → main engagement → final push/last stand |

**Not in MVP:** Async PvP, meta unlocks, branching maps, multiple factions, mid-combat real-time micro.

**Post-MVP goals:** Async PvP, light meta unlocks, frontline-push and campaign-chapter run structures.

---

## Core Loop

**Fantasy:** You are a trench commander in a grimdark retro-futurist WW1 — brass machinery, arc-lamps, gas and shrapnel, diesel-powered war engines. You prepare as a quartermaster and command at critical moments.

### Run Flow (Linear Gauntlet — MVP)

1. **Faction select** — pick force for the run (1 faction in vertical slice).
2. **Opening shop** — starting gold + requisition; first board layout.
3. **Fight loop (×5):**
   - **Build phase** — shop, place/move pieces, finalize loadout.
   - **Combat** — 3 auto-resolve phases with between-phase command windows.
   - **Aftermath** — rewards; board persists between fights.
4. **Win** — clear fight 5 (boss). **Lose** — army HP or commander HP reaches zero.

**Target run length:** 20–35 minutes.

### Save & Resume (MVP — Required)

Players must be able to **exit at any point mid-run** and resume exactly where they left off.

**Saved state includes:**
- Current fight index (1–5) and phase within run (build vs mid-combat)
- Full board layout, bench contents, frozen shop item (if any)
- Gold, requisition, reroll counts for current round
- Current shop offers (seed-locked so offers don't change on reload)
- If mid-combat: combat seed, both board snapshots, phase completed, pending command window or submitted actions so far, partial event log for replay catch-up
- Faction choice and run seed

**Behavior:**
- Auto-save on: fight start/end, shop purchase, board finalize, each between-phase command submission, app pause/quit
- Manual "Save & Exit" from run hub and build phase
- One save slot for MVP (single active run)
- Corrupt save → clear message; offer new run (no silent failure)

**Serialization format:** JSON file in `Application.persistentDataPath` — same schema designed for future cloud save / async PvP submission.

---

## Board, Zones & Loadout

### Grid

Fixed rectangular grid (MVP: **8×6** or **10×6**). Each tile holds at most one piece. Pieces have shapes (1×1, L-shape, 2×1, etc.) — placement is a spatial puzzle.

### Zone Layout

```
┌─────────────────────────────────────┐
│  REAR (Buildings Only)              │
├─────────────────────────────────────┤
│  SUPPORT (Either)                   │
├─────────────────────────────────────┤
│  FRONT (Units Only)                 │
└─────────────────────────────────────┘
     ★ ★ ★  ← Faction special tiles (overlap any zone row)
```

### Zone Rules

| Zone | Allowed | Purpose |
|------|---------|---------|
| **Rear** | Buildings only | Bunkers, supply, artillery, economy |
| **Support** | Buildings or units | Adjacency bridge between economy and front line |
| **Front** | Units only | Infantry, MG crews, vehicles |
| **Faction special** | Any (overlaps above zones) | Faction-specific bonuses for pieces placed there |

### Faction Special Tiles

A small set of tiles (3–5) **marked on top of** rear/support/front zones — not a separate band. Any piece placed on a special tile receives the faction bonus for that tile type, similar to Backpack Battles' starting board piece.

Example (Iron Vanguard): special tiles overlapping rear + support; Command-tagged piece on a special tile at fight start grants +1 bonus phase action.

### Piece Types (MVP)

| Type | Examples | Typical zone |
|------|----------|--------------|
| Building | Command bunker, supply depot, field gun nest | Rear |
| Unit | Rifle squad, MG team, trench raider | Front |
| Hybrid | Mobile artillery, armored walker | Either |

### Synergies

- **Adjacency** — touching edges trigger shared buffs (e.g. depot + unit = requisition regen in combat).
- **Tags** — `Artillery`, `Infantry`, `Gas`, `Mechanical`, `Command`, `Vanguard` for combo rules without strict adjacency.
- **Triggers** — "When adjacent unit fires," "At phase 2 start if on special tile."
- **Cooldowns** — abilities tick during phases 2–3; some charge during phase 1 positioning.

### Build Phase Rules

- Drag pieces from **bench** (owned, unplaced) or **shop** onto grid.
- Invalid zone = red highlight + snap-back.
- **Sell** returns ~50% gold (no requisition refund).
- **Bench limit:** 3 slots.
- Board persists between fights in the gauntlet.

---

## Combat System

### Architecture

Combat runs in a **deterministic pure C# sim**. Unity sends board state + seed; sim returns event log; Unity replays for presentation.

```
Build finalized → Sim.StartCombat(seed, boardA, boardB)
  → Phase 1 resolve → PAUSE → player submits actions →
  → Phase 2 resolve → PAUSE → player submits actions →
  → Phase 3 resolve → EndCombat(result, eventLog)
```

### Three Phases (Total War Tempo)

| Phase | Name | What happens | Player actions after |
|-------|------|--------------|----------------------|
| 1 | **Approach & Deployment** | Mostly movement/positioning; light skirmish fire; cooldowns charge | 1 primary (+1 bonus if eligible) |
| 2 | **The Grind** | Bulk of damage, cooldowns fire, adjacency synergies peak | 1 primary (+1 bonus if eligible) |
| 3 | **Final Push / Last Stand** | Morale spikes, desperation abilities, cleanup; fight ends | None (combat over) |

**Phase 1 detail:** Units reposition toward preferred targets/ranges. Light damage (snipers, ranging shots). Buildings act as anchors (command range, supply aura).

**Phase 2 detail:** Sustained fire, MG bursts, gas, ability cooldowns. Morale/resource pools drain unless buildings replenish.

**Phase 3 detail:** Winner presses; loser gets last-stand bonuses. Remaining cooldowns dump; once-per-fight abilities eligible.

**Win condition:** Opponent army HP (sum of unit/building durability) or commander HP reaches zero.

### Between-Phase Command Window

- **Budget:** 1 primary action by default; +1 bonus if qualifying command building is intact (e.g. command bunker) or faction special tile condition met.
- **Actions come from loadout** — only what pieces unlock:

| Source | Example action |
|--------|----------------|
| Command building | Stance change for next phase |
| Supply building | Spend requisition → morale or damage buff |
| Artillery / special unit | Call strike (targeting from piece rules) |
| Piece on faction special tile | Faction-unique action |

### Stances (apply to next phase)

- **Focus Weakest** — prioritize lowest-HP targets
- **Hold the Line** — defensive; protect support/buildings
- **Support Priority** — buff/heal allies over attacking
- **All-Out Assault** — +damage, −defense

Availability depends on owned command pieces; not all stances every run.

### Validation & Event Log

- Invalid actions rejected with reason; combat continues with no action (no soft-lock).
- Event log entry: `{ phase, tick, actorId, actionType, targets, values }`
- Same seed + boards + player actions → identical log (required for save/resume and future async PvP).

---

## Shop & Economy

### Currencies

| Currency | Earned from | Spent on |
|----------|-------------|----------|
| **Gold** | Fight rewards, selling pieces | Shop purchases, rerolls |
| **Requisition** | Fight rewards, some buildings in combat | Between-phase buffs, special calls, premium shop stock |

### Shop Structure

Three lanes each round:

1. **General Quartermaster** — units and basic gear (gold)
2. **Engineers & Emplacements** — buildings (gold, sometimes requisition)
3. **Black Market / Requisition** — rare and faction-flavored pieces (requisition-heavy)

Each lane shows **4–6 offers** in MVP. Player can **buy**, **reroll** one lane per round (gold, scaling cost), and **freeze** one item across rounds.

### Board-Driven Shop Modifiers

Buildings on the grid change shop generation before offers roll:

| Building | Shop effect (examples) |
|----------|------------------------|
| Command bunker | +1 slot in General lane; stance-related items in pool |
| Supply depot | −10% gold prices (cap stack: −25%); more requisition items |
| Radio array | Preview next enemy tag (e.g. "Heavy armor") |
| Field workshop | Engineers lane always has ≥1 building offer |
| Piece on faction special tile | May add tagged item to pool per faction rules |

### Shop Generation Flow

```
End of fight → rewards (gold + requisition)
→ BuildShop(playerBoard, faction, roundNumber, seed)
→ Apply building modifiers to pools and weights
→ Roll offers per lane (persist offers in save state)
→ Player shops / arranges board → next fight
```

### Pricing & Scaling

- Tiers: Common / Uncommon / Rare
- Round number increases average rarity weight
- Fight 5: one guaranteed Rare in a lane

---

## Architecture

### Layer Split

```
Unity Presentation Layer
  GridUI, ShopUI, CombatReplay, PhaseCommandUI, SaveLoadUI, Audio
        ↕ commands / events
Game Orchestrator (Unity, thin)
  RunState, scene flow, save/load, AI opponent selection
        ↕
Core Sim (pure C#, no Unity refs)
  Board, ShopGenerator, CombatResolver, ActionValidator, SaveSerializer
```

### Core Sim Modules

| Module | Responsibility |
|--------|----------------|
| `BoardState` | Grid, zones, faction special tile masks, placed pieces |
| `PieceDefinition` | Shape, tags, stats, abilities (data-only) |
| `ShopGenerator` | Pools, building modifiers, lane rolls from seed |
| `CombatResolver` | 3-phase tick loop, adjacency, cooldowns, damage |
| `CommandProcessor` | Validates/applies between-phase actions |
| `CombatEventLog` | Ordered events for replay and PvP |
| `RunSaveState` | Full serializable run snapshot |
| `Rng` | Seeded deterministic random |

### Unity Presentation Components

| Component | Responsibility |
|-----------|----------------|
| `RunManager` | Gauntlet flow, fight index, win/lose, auto-save triggers |
| `SaveManager` | Read/write save file, resume mid-combat |
| `BoardView` | Drag-drop, zone highlights, special tiles |
| `ShopView` | 3 lanes, buy/reroll/freeze, modifier tooltips |
| `CombatDirector` | Phase playback from event log; catch-up on resume |
| `PhaseCommandPanel` | Available actions filtered by loadout |
| `ContentDatabase` | ScriptableObjects → sim definitions at boot |

### Content Pipeline

Designers author ScriptableObjects (pieces, factions, enemy templates, shop pools). Bootstrap converts to sim-readable structs at runtime or build time. Sim never references Unity APIs.

### Project Structure

```
Assets/
  _Project/
    Core/           ← pure C# sim (asmdef: no Unity refs)
    Game/           ← RunManager, SaveManager
    Presentation/   ← UI, views, CombatDirector
    Data/           ← ScriptableObjects
    Scenes/         ← MainMenu, Run, Combat
docs/superpowers/specs/
```

### Async PvP (Future)

Serialize `{ seed, playerBoard, opponentBoard, playerActions[] }`. Server or client runs sim → same event log → replay. Schema aligned with save format.

---

## MVP Vertical Slice Content

### Faction: Iron Vanguard

Industrial retro-futurist trench force — brass diesel walkers, coil-rifles, field guns.

- **Special tiles:** 3 cells overlapping rear + support
- **Bonus:** Pieces gain `Vanguard` tag synergy; Command piece on special tile at fight start → +1 phase action

### Pieces (~15–20 total)

| Category | Count | Examples |
|----------|-------|----------|
| Buildings | 5–6 | Command bunker, supply depot, field gun nest, radio array, field workshop |
| Units | 6–8 | Rifle squad, MG team, trench raider, diesel walker, mortar crew |
| Hybrid / rare | 2–3 | Mobile artillery, gas drone, armored sapper |

Each piece: shape, zone restriction, one combat behavior, zero or one shop modifier or command action.

### Enemy Gauntlet

| Fight | Theme | Design hook |
|-------|-------|-------------|
| 1 | Rifle line | Teaches zones and adjacency |
| 2 | MG nest | Punishes front-heavy without rear support |
| 3 | Artillery barrage | Phase 1 damage; economy vs combat tradeoff |
| 4 | Gas + armor | Tag counters; shop prep matters |
| 5 | Boss: Siege Crawler | Heavy phase 3; full build arc test |

### UI Screens (MVP)

Main menu (Continue / New Run) → Faction select → Run hub → Board + Shop → Combat + phase commands → Win/Lose

### Out of Scope (MVP)

- Meta unlocks between runs
- Async PvP
- Branching map / campaign chapters
- Second faction
- Event / rest nodes
- Durability persistence between fights

### Success Criteria

- Playtester completes a run in ~25 minutes
- Understands economy-vs-combat tradeoff
- Uses at least one between-phase command meaningfully
- Can save mid-run, exit, and resume without lost progress
- Wants to replay with a different build

---

## Error Handling & Testing

### Validation

- Invalid grid placement: UI blocks; sim validates on finalize
- Invalid phase commands: rejected with reason; no soft-lock
- Missing/corrupt content: log at boot; disable affected pieces in pools
- Corrupt save: user-facing error; do not partial-load silently

### Determinism

- Automated test: same seed + boards + actions → byte-identical event log
- Run on every content change to catch nondeterminism regressions

### Testing Strategy

| Layer | Focus | Method |
|-------|-------|--------|
| Core sim | Adjacency, cooldowns, phases, shop modifiers, commands, save round-trip | NUnit, no Unity |
| Integration | Full gauntlet headless; save mid-combat → reload → identical outcome | Sim-only regression |
| Unity | Placement, shop, save/load UI, combat replay catch-up | Play Mode tests |
| Manual | Fun, pacing, readability | Weekly playtest |

---

## Design Decisions Log

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Architecture | Deterministic core sim + Unity shell | Testable rules; PvP-ready; clean combat/UI split |
| Loadout model | Hybrid grid + overlapping faction tiles | Backpack spatial puzzle + WW1 zone readability + faction identity |
| Run structure | Linear gauntlet (MVP) | Fastest path to vertical slice |
| Mode | PvE first, async PvP later | Prove loop before networking |
| Meta | Pure roguelike MVP | Reduce scope; light unlocks post-MVP |
| Save | Required, any-point mid-run resume | Player session flexibility |
| Economy/combat tension | Explicit core pillar | Buildings modify shop; grid space is finite |
