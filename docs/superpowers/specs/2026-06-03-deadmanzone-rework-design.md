# DeadManZone — Combat & Economy Rework Design Spec

**Date:** 2026-06-03  
**Engine:** Unity  
**Status:** Approved for planning (brainstorming)  
**Supersedes:** `2026-05-31-deadmanzone-autobattler-design.md` for combat, economy, board orientation, and demo scope  
**Demo scope:** 1 faction × 10-fight linear gauntlet

---

## Section 1 — Vision & pillars

**Fantasy:** A planet locked in endless trench war — warlords rise and fall, but the grind never ends. Grimdark retro-futurist WW1 with post-apocalyptic wear: brass, diesel, gas, collapsed industry.

**Genre:** Autobattler / shop-build roguelite. The player is quartermaster and general: arrange forces on a **spatial grid**, shop between fights, then watch combat play out with **two command pauses** — no unit micro.

**Reference mix:**

| Layer | Influences |
|--------|------------|
| Build / shop | *Backpack Battles*, *The Bazaar* |
| Combat feel | *Total War* tempo, TFT-style board clarity, “top troops” style readable auto-battle |
| Long-term | Async PvP (schema + deterministic sim from day one) |

**Core pillars:**

1. **Spatial loadout** — Shaped pieces, zones, adjacency, tag synergies.
2. **Economy vs war** — Four resources; rear buildings and shop choices compete with front combat power.
3. **Casualties without per-unit HP bookkeeping** — **Manpower** upkeep and refunds abstract losses.
4. **Run tension via Morale** — Rare heals; losses hurt more as gauntlet progress and fight severity increase.
5. **Horizontal war** — Build layout is fight layout; war pushes **left (player) → right (enemy)**.
6. **General, not sergeant** — Positioning and timing matter in sim; player only acts on **AI behavior** and **queued one-shots** at pauses.

**Long-term roster:** 12 factions. **Demo:** 1 playable faction, 2 added later.

**Demo gauntlet:** 10 fights, linear semi-random opponents.

**Required systems (unchanged intent):** Mid-run save/resume at any point; deterministic sim for replay and future async PvP.

**Out of demo:** Meta unlocks, 11 other playable factions, branching campaign map, live/async matchmaking UI.

**Target run length:** ~30–40 minutes.

---

## Section 2 — Run flow, economy & shop

### Run flow

```
Main Menu
  → Faction select (1 faction in rework demo)
  → Opening shop + initial board placement
  → Fight loop ×10
       Build phase   — shop, place/move pieces, meet manpower gate
       Combat        — shared horizontal grid, 3 segments, 2 pause windows
       Aftermath     — rewards, morale check; continue or run ends
  → Victory (clear fight 10) / Defeat (Morale = 0)
```

### Save & resume

Players must exit at any point mid-run and resume exactly where they left off.

**Saved state includes:**

- Fight index (1–10), build vs mid-combat
- Full board layout, bench, frozen shop offer (if any)
- Supplies, Manpower, Authority, Morale, reroll counts
- Current shop offers (seed-locked)
- Mid-combat: combat seed, both board snapshots, segment index, tick within segment, pause pending vs submitted commands, partial event log for replay catch-up
- Faction choice and run seed

**Behavior:** Auto-save on fight start/end, shop purchase, board finalize, each pause command submission, app pause/quit. Manual “Save & Exit” from run hub and build phase. One save slot for MVP. Corrupt save → clear error; offer new run.

**Serialization:** JSON in `Application.persistentDataPath`; schema aligned with future cloud save / async PvP submission.

### Four resources

| Resource | Role | Typical earn | Typical spend |
|----------|------|--------------|---------------|
| **Supplies** | Main shop currency | Fight rewards, selling pieces (~50% refund) | Purchases, rerolls, some manpower relief |
| **Manpower** | Field armies — upkeep per deployed `Combatant` to **start** a fight | HQ + buildings each build round; refund survivors after fight | Implicit deploy cost; relief via Supplies/Morale |
| **Authority** | Command currency; **resets each build round** (after aftermath, before shop) | HQ, buildings, units | Shop one-shots; pause-window AI orders and queued one-shots |
| **Morale** | Run health (**0 = run over**) | Rare pieces/effects | Fight **losses** (severity × fight index); some shop buys; emergency manpower |

**Authority:** Unspent Authority does not bank by default (unless a future piece explicitly allows banking).

### Manpower gate

- **Hard block:** Cannot start battle if board upkeep exceeds available Manpower until resolved (sell, bench, rearrange).
- **Once per run:** Emergency draft to cover a shortfall.
- **Relief:** Shop one-shots and buildings may spend **Supplies** and/or **Morale** to cover manpower gaps.

### Morale on loss

Morale loss on defeat uses **fight severity** (e.g. units lost, HQ damaged) **and** **gauntlet progress** (later fights cost more). Morale heals remain rare and expensive.

### Shop structure

**Three lanes each build round:**

| Lane | Stock |
|------|--------|
| **Offensive** | Units, offensive structures, offensive one-shots |
| **Defensive** | Buildings, defensive structures, defensive one-shots |
| **Specialty** | Hidden/empty until unlocked |

**Offers:** 3 base offers per lane. Pieces on the board at **shop open** may unlock **extra offers** in specific lanes.

**Specialty lane unlock:** Faction-specific rules plus board milestones (specific building abilities, tags, etc.).

**Lane actions:** Buy, reroll one lane per round (Supplies, scaling cost), freeze one offer across rounds.

### Build phase

- **Zone orientation:** Vertical bands left → right on the player’s half: **Rear | Support | Front** (Front is rightmost, toward enemy).
- Shaped pieces, adjacency, tag synergies, faction special tiles.
- Bench limit and sell rules carry forward from prior spec unless playtest changes them (default: bench 3 slots).
- Board persists across gauntlet; manpower evaluated at **Start battle**.

---

## Section 3 — Board, battlefield & win conditions

### Player half (build = combat)

Grid dimensions per half are content-tuned (implementation starting point: prior 8×6-style density, reauthored as 3 zone columns × N rows).

| Zone | Position on player half | Placement |
|------|-------------------------|-----------|
| **Rear** | Leftmost columns | Buildings, HQ, economy |
| **Support** | Middle columns | Buildings or units |
| **Front** | Rightmost columns | Units (and hybrids per piece rules) |

**Faction special tiles:** 3–5 cells overlapping zone bands; bonuses per faction when pieces occupy them (shop open and/or fight start — per piece/faction data).

**Tags:** Used for synergies, adjacency, and content rules. **`Combatant`** tag marks pieces that count for the “no enemy combatants” win condition.

### Combined battlefield

```
┌──────────┬──────────┬──────────┬────┬────┬──────────┬──────────┬──────────┐
│  REAR    │ SUPPORT  │  FRONT   │ N  │ N  │  FRONT   │ SUPPORT  │  REAR    │
│ (player) │ (player) │ (player) │    │    │ (enemy)  │ (enemy)  │ (enemy)  │
└──────────┴──────────┴──────────┴────┴────┴──────────┴──────────┴──────────┘
     ←——— player half ———→      neutral      ←——— enemy half ———→
```

- **Player:** Left half; zones rear → support → front (front faces center).
- **Neutral:** 2 columns between halves.
- **Enemy:** Right half; mirrored zones.
- Units **move cell-to-cell** during combat; build placement is starting position.

### Neutral columns by segment

| Segment | Presentation (~sim time) | Neutral behavior |
|---------|----------------------------|------------------|
| **1 — Opening** | ~10s | **Contested:** increased movement cost; no gas |
| **2 — Main fight** | ~50s | **Contested:** still increased movement cost |
| **3 — Final / gas** | ~20s | **Contested + gas:** strongest gas in neutral columns; **intensity ramps over time** |

### Combatants, buildings & HQ

| Piece | Moves in combat? | Destroyed? | “No combatants” win? |
|-------|------------------|------------|----------------------|
| **Unit** (`Combatant`) | Yes | Yes | Yes |
| **Combat-active building** (`Combatant`) | Usually no | Yes; benefit stops | Yes |
| **Passive building** | No | Yes | No |
| **HQ** | No | Yes → **instant loss** for owner | Separate win rule |

**Win:** Enemy has zero `Combatant` pieces **OR** enemy **HQ** destroyed.

**Fight loss:** Player combatants eliminated or player HQ destroyed.

---

## Section 4 — Combat segments, pauses & gas

### Backend model

- **Deterministic tick sim** (pure C#) produces an ordered **event log**.
- Unity **replays** the log for presentation; segment durations (~10s / ~50s / ~20s) are presentation pacing of sim time, not wall-clock authority.
- Same seed + boards + pause submissions → identical log (required for save/resume and async PvP).

### Segment flow

```
START (boards + seed locked)
  → Segment 1 — deployment, repositioning, light fire
  → PAUSE 1 — player commands
  → Segment 2 — main damage, cooldowns, synergies
  → PAUSE 2 — player commands
  → Segment 3 — gas ramp, cleanup
  → END — win/loss
```

**Segment 3:** No pause.

### During segments (no player input)

- Units path, move, and fight on the shared grid.
- Buildings without combat behavior do not move; `Combatant` buildings participate per their data.
- Adjacency and tags apply per sim rules.

### Pause windows (after segments 1 & 2)

| Action type | Examples | Cost |
|-------------|----------|------|
| **AI behavior orders** | Focus weakest, hold line, protect HQ/support, all-out assault | Authority; some require Command-tagged pieces |
| **Queued one-shots** | Artillery strike, gas countermeasure, etc. | Authority; some require prior shop purchase |

- Orders affect **next segment**; one-shots **execute automatically** at segment start (or defined tick) per piece rules.
- Invalid commands: rejected with reason; no soft-lock.

### Gas (segment 3)

- Environmental damage each tick; **strongest in neutral columns**; falloff on front zones (content-tuned).
- **Intensity ramps** over segment 3.
- Mitigation via tags/abilities (e.g. gas masks) — data-driven.

### Aftermath

| Outcome | Manpower | Morale |
|---------|----------|--------|
| **Win** | Refund upkeep for **survivors** | No loss by default (optional piece costs TBD) |
| **Loss** | Refund survivors if any remain | Loss = f(severity, fight index) |

Fight rewards grant Supplies (and other resources per fight table). Next build round: Authority resets, shop generates, manpower gate applies.

---

## Section 5 — Architecture, demo & migration

### Layer split

```
Unity Presentation — grid/combat replay, shop UI, pause command UI, save UI
        ↕
Game Orchestrator — RunManager, scenes, enemy templates, save triggers
        ↕
Core Sim (pure C#) — Board, movement, combat ticks, shop, commands, gas, serializers
```

### Core sim modules

| Module | Responsibility |
|--------|----------------|
| `BoardState` | Vertical zones on each half; combined grid (player + neutral + enemy) |
| `PieceDefinition` | Shape, zones, tags including `Combatant` |
| `ShopGenerator` | Offensive / defensive / specialty lanes; unlocks; extra offers |
| `CombatResolver` | Tick loop, movement, segments, gas ramp, win detection |
| `CommandProcessor` | Pause validation: AI orders + queued one-shots |
| `CombatEventLog` | Movement, damage, gas, deaths, HQ destroyed |
| `RunSaveSerializer` | Four currencies, 10-fight state, mid-combat tick position |

### Demo content (1 × 10)

| Item | Plan |
|------|------|
| **Faction** | One fully authored (evolve Iron Vanguard or equivalent) |
| **Fights** | 10 escalating enemy boards (semi-random within templates) |
| **Pieces** | ~20–25 with clear `Combatant` vs passive roles; HQ required |
| **Specialty** | Faction rules + 3–5 building milestones for demo |

### Migration from 2026-05-31 vertical slice

| Prior | Rework |
|-------|--------|
| Gold / Requisition | Supplies / Authority (+ Manpower, Morale) |
| 5 fights | 10 fights |
| Vertical zones (rear top, front bottom) | Horizontal war; zones as **columns** rear → support → front |
| Instant phase resolve | Tick sim + movement + gas |
| Army HP win | `Combatant` elimination or HQ destroyed |
| Shop lane names (General / Engineers / Black Market) | Offensive / Defensive / Specialty |

**Implementation order:** Economy & run state → shop lanes/unlocks → combined grid → combat ticks/movement → gas → pause commands → UI/replay → content.

### Testing

| Layer | Focus |
|-------|--------|
| Core (NUnit) | Manpower gate, morale loss, specialty unlock, movement determinism, gas, HQ win, save mid-pause |
| Integration | 10-fight headless; reload mid-segment → identical outcome |
| Play Mode | Grid extension, pause UI, replay catch-up |
| Manual | Pacing, horizontal readability, gas readability |

### Content tuning (post-spec)

- Grid rows/columns per half  
- Manpower per piece tier  
- Morale loss coefficients  
- Authority costs  
- Gas DPS curve  

---

## Design decisions log

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Combat backend | Deterministic tick sim + replay | Player feel without micro; PvP/save safe |
| Board in combat | Same grid + enemy half + 2 neutral columns | Placement matters; horizontal push fantasy |
| Zone layout | Vertical columns: rear \| support \| front | Front adjacent to neutral/enemy |
| Neutral ground | Contested all fight; gas ramps segment 3 | No-man’s land tension |
| Win condition | No `Combatant` or HQ destroyed | Clear goal; HQ pressure |
| Passive buildings | Destroyable but don’t count for combatant win | Economy can be shelled |
| Demo scope | 1 faction × 10 fights | Prove systems before faction breadth |
| Manpower | Hard block + once/run draft + shop relief | War attrition without per-unit HP tracking |
| Morale | Severity × fight index on loss | Run arc wears down |
| Specialty lane | Faction + board milestones | Bazaar-style discovery |

---

## Success criteria (rework demo)

- Playtester completes 10-fight run in ~30–40 minutes  
- Understands four-resource tradeoffs and manpower gate  
- Uses pause commands meaningfully at least twice per fight on average  
- Save mid-combat → resume with identical outcome  
- Horizontal combat readable without micro  
