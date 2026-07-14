> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — Game Design Document

**Version:** 1.0 (Demo)  
**Date:** 2026-06-06  
**Engine:** Unity 6  
**Status:** Playable demo — active development  
**Audience:** Design, engineering, art, QA, external collaborators

---

## Document purpose

This is the consolidated design reference for **DeadManZone** as of the June 2026 demo milestone. It synthesizes prior specs and reflects the current playable build. Use it as the single shareable source of truth for vision, systems, content scope, and implementation status.

**Prior detailed specs** (for drill-down):

| Spec | Focus |
|------|-------|
| `docs/superpowers/specs/2026-05-31-deadmanzone-autobattler-design.md` | Original vertical-slice vision |
| `docs/superpowers/specs/2026-06-03-deadmanzone-rework-design.md` | Economy rework, horizontal war, tick combat |
| `docs/superpowers/specs/2026-06-04-deadmanzone-combat-units-demo-design.md` | Tactics, abilities, unit stats, HQ rules |
| `docs/superpowers/specs/2026-06-04-deadmanzone-tutorial-balance-pass-design.md` | Fights 1–3 economy and enemy tuning |
| `docs/superpowers/specs/2026-06-04-deadmanzone-combat-sim-completion-design.md` | Movement/attack-speed plumbing, 7-column neutral |
| `docs/superpowers/specs/2026-06-04-build-screen-layout-design.md` | Build UI, reserves grid, rotation, pause menu |
| `docs/superpowers/specs/2026-06-05-deadmanzone-neutral-faction-art-design.md` | Neutral art pipeline |
| `docs/superpowers/specs/2026-06-06-deadmanzone-top-down-visual-commitment.md` | Isometric tokens + top-down terrain; 3D combat deferred |

---

## 1. Vision & pillars

### Fantasy

A planet locked in endless trench war — warlords rise and fall, but the grind never ends. Grimdark retro-futurist WW1 with post-apocalyptic wear: brass machinery, diesel engines, gas masks, coil-rifles, and field-expedient armor.

The player is **quartermaster and general**: arrange forces on a spatial grid, shop between fights, then watch combat auto-resolve with **two command pauses** per fight. No unit micro.

### Genre & references

| Layer | Influences |
|-------|------------|
| Build / shop | *Backpack Battles*, *The Bazaar* |
| Combat feel | *Total War* tempo, TFT-style board clarity |
| Long-term | Async PvP (deterministic sim from day one) |

### Core pillars

1. **Spatial loadout** — Shaped pieces, zone restrictions, adjacency synergies, tag-based combos.
2. **Economy vs war** — Four resources; rear buildings and shop choices compete with front-line combat power.
3. **Casualties without per-unit HP bookkeeping** — Manpower upkeep and salvage refunds abstract losses.
4. **Run tension via Morale** — Rare heals; losses hurt more as gauntlet progress increases.
5. **Horizontal war** — Build layout is fight layout; war pushes **left (player) → right (enemy)**.
6. **General, not sergeant** — Positioning and timing matter in sim; player only sets **tactics** and **queued abilities** at pauses.

### Demo scope vs long-term

| | Demo (now) | Post-demo |
|---|------------|-----------|
| Playable factions | 3 (+ unlock flow) | 12 factions |
| Gauntlet length | 10 fights, linear | Campaign chapters, branching |
| PvP | Schema-ready, not shipped | Async PvP |
| Meta | Achievements, local leaderboard | Steam, broader unlock tree |
| Combat presentation | 2D grid, isometric tokens, simple VFX | Fog-of-war intro, richer VFX, optional 3D combat skin |

**Target run length:** ~30–40 minutes for a full 10-fight gauntlet.

---

## 2. Core loop

```
Main Menu
  → Faction select (Ironmarch Vanguard unlocked; others unlock on first victory)
  → Opening shop + initial board placement (HQ auto-spawned)
  → Fight loop ×10
       Build phase   — shop, place/move pieces in Reserves or on board, meet manpower gate
       Combat        — shared horizontal grid, 3 segments, 2 pause windows
       Aftermath     — battle report, rewards, morale check
  → Victory (clear fight 10) / Defeat (Morale = 0)
```

### Build phase

- Shop three lanes: **Offensive**, **Defensive**, **Specialty** (unlocked by board milestones).
- Drag pieces between **main board**, **Reserves** (2×9 spatial grid), and sell zone.
- Optional **Q/R rotation** while dragging.
- **Manpower gate** blocks starting a fight if deployed upkeep exceeds available Manpower.
- Board persists across the gauntlet; HQ is immovable for the entire run.

### Combat phase

- Deterministic tick sim produces an event log; Unity replays for presentation.
- Three segments with two pause windows (after Opening and Main Fight).
- Gas ramp in final segment until a win condition is met.

### Aftermath

- Battle report: outcome, supplies reward, morale delta, manpower refund, top damage dealt/taken.
- Authority resets at next build round.
- Shop refreshes with fight-index faction weighting.

---

## 3. Economy — four resources

| Resource | Role | Earned from | Spent on |
|----------|------|-------------|----------|
| **Supplies** | Main shop currency | Fight rewards, selling pieces (~50% refund) | Purchases, rerolls, some manpower relief |
| **Manpower** | Field armies — upkeep per deployed `Combatant` to **start** a fight | HQ + buildings each build round; refund survivors after fight | Implicit deploy cost |
| **Authority** | Command currency; **resets each build round** | HQ, buildings, units | Tactic switches, pause-window abilities |
| **Morale** | Run health (**0 = run over**) | Rare pieces/effects | Fight **losses** (severity × fight index); some shop buys |

### Manpower gate

- **Hard block:** Cannot start battle if board upkeep exceeds available Manpower.
- **Once per run:** Emergency draft to cover a shortfall.
- **Relief:** Shop one-shots and buildings may spend Supplies and/or Morale to cover gaps.

### Salvage (selling pieces)

| Refund | Ratio |
|--------|-------|
| Supplies | 50% of purchase cost |
| Authority | 50% of Authority cost |
| Manpower | 25% of manpower cost |

**Dust Scourge faction bonus:** +25% supplies from salvage.

### Tutorial economy (fights 1–3)

Designed so new players reliably experience both tactic pauses:

| Moment | Supplies |
|--------|----------|
| Run start | 125 |
| Win fight 1 | +100 |
| Win fight 2 | +105 |
| Win fight 3 | +110 |
| Fights 4+ | Existing curve (22, 25, 28, …) |

**Design principle:** Tutorial softness comes from **enemy composition only** — no fight-index damage/HP modifiers on player units.

---

## 4. Board & battlefield

### Player half (build = combat)

| Property | Value |
|----------|-------|
| Width | 9 columns |
| Height | 10 rows |
| Rear columns | 4 (leftmost) |
| Support columns | 3 (middle) |
| Front columns | 2 (rightmost, toward enemy) |

Pieces have **shapes** (1×1, 1×2, 2×2, L-shapes, etc.). Placement is a spatial puzzle with zone restrictions.

### Combined battlefield

```
[ Player 9: Rear(4) | Support(3) | Front(2) ] [ Neutral 7 ] [ Enemy 9: Front(2) | Support(3) | Rear(4) ]
|<------------------- 9 columns ------------------->|  x=9..15  |<---------------- 9 columns --------------->|
Total width: 25
```

- **Player:** Left half; zones rear → support → front.
- **Neutral:** 7 contested columns (no-man's land).
- **Enemy:** Right half; mirrored zones.
- Units **move cell-to-cell** during combat from build placement.

### Neutral column behavior

| Segment | Behavior |
|---------|----------|
| Opening & Main Fight | **Contested:** 2× movement charge cost |
| Final / Gas | **Contested + gas:** environmental damage ramps; strongest in neutral columns |

### Zone rules

| Zone | Allowed | Purpose |
|------|---------|---------|
| **Rear** | Buildings, HQ, economy | Command, supply, artillery anchors |
| **Support** | Buildings or units | Bridge between economy and front |
| **Front** | Units (and hybrids per piece rules) | Infantry, vehicles, assault pieces |
| **Faction special tiles** | Any (overlaps zones) | Faction-specific bonuses |

### Reserves (replaces bench)

- **2 rows × 9 columns** (18 cells), spatial storage.
- No zone restrictions; pieces must fit entirely with no overlap.
- Each stored piece: `pieceId`, anchor, rotation.
- Capacity is implicit free cells, not a slot count.

### Piece rotation

- `0°`, `90°`, `180°`, `270°` via **Q** (counter-clockwise) and **R** (clockwise) while dragging.
- Rotation persisted in save; applies to main board and Reserves.

---

## 5. Combat system

### Architecture

```
Build finalized → TickCombatRun.Start(seed, playerBoard, enemyBoard)
  → Segment 1 (Opening)     → PAUSE 1 → player submits tactic + abilities
  → Segment 2 (Main Fight)  → PAUSE 2 → player submits tactic + abilities
  → Segment 3a (Brief Push) → no pause
  → Segment 3b (Gas ramp)   → until win/loss
  → Battle report
```

- **Deterministic pure C# sim** — no Unity refs in Core.
- Same seed + boards + pause submissions → identical event log (save/resume and future async PvP).

### Segment pacing

Sim runs at **10 ticks per second**. Values in `CombatPacingConfig` (tunable):

| Segment | Spec ticks | ~Wall-clock | Damage scale |
|---------|------------|-------------|--------------|
| Opening | 50 | ~5s | 0.2× |
| Main Fight | 300 (spec) / 200 (current code) | ~30s / ~20s | 1.0× |
| Brief Push | 50 | ~5s | 1.0× |
| Gas | Until winner | — | ramps |

> **Note:** Main Fight is currently set to 200 ticks in code; spec target is 300. Revert pending playtest validation.

### Win / loss / draw

| Outcome | Condition |
|---------|-----------|
| **Win** | Enemy has zero `Combatant`-tagged pieces **or** enemy HQ destroyed |
| **Loss** | Player combatants eliminated **or** player HQ destroyed |
| **Draw** | Both sides' last combatants die same tick — treated as win for morale, ~50% supplies |

### Auto-combat during segments

- Units move cell-to-cell using **movement charge budget** (tier-based frequency).
- Acquire targets by **active tactic** and **attack range** (Manhattan distance).
- Attack on cooldown modified by **attack speed** tier.
- Buildings use `MovementSpeed.None` unless data specifies otherwise.
- Neutral columns cost **2×** movement charge vs friendly ground.

### Unit stat model

| Field | Tiers | Sim effect |
|-------|-------|------------|
| `AttackSpeed` | Slow / Medium / Fast | Cooldown multiplier on `CooldownTicks` |
| `AttackRange` | Short (1) / Medium (3) / Long (6) | Max Manhattan target distance |
| `MovementSpeed` | None / Low / Medium / High | Move attempt every N ticks |
| `ArmorType` | None / Light / Medium / Heavy | Damage reduction + rock-paper-scissors |
| `AttackType` | Ballistic / Explosive / Piercing | Type bonuses vs armor |

**Rock-paper-scissors lite:**

| Attack type | Bonus condition | Multiplier |
|-------------|-----------------|------------|
| Ballistic | vs Light armor | ×1.25 |
| Explosive | vs Light/Medium or `building`/`structure` tag | ×1.30 |
| Piercing | vs Heavy armor | ×1.35 |

**Armor baseline:** Light 100%, Medium 85%, Heavy 70% (before type bonuses).

### Tags = keywords

One tag list per piece drives synergies, critical mass, shop filtering, and ability unlocks.

| Category | Rule | Examples |
|----------|------|----------|
| Primary | Exactly one | `Infantry`, `Vehicle`, `building`, `structure` |
| Combat role | Exactly one | `assault`, `tank`, `artillery`, `support`, `HQ` |
| Synergy | 1–4 additional | `scout`, `damage`, `Vanguard`, `Command` |
| System | Auto or explicit | `Combatant`, `HQ`, `Neutral` |

---

## 6. Pause windows — tactics & abilities

### Pause triggers

After **Opening** and **Main Fight** segments. No pause after Brief Push or during gas.

### UI

Single panel with two regions:

1. **Tactics** (required) — exactly one selected.
2. **Abilities** (optional) — cards for each ability unlocked by a **living** source piece.

Continue enabled when: one tactic selected, total Authority ≤ available, each ability has valid alive source.

### Tactics

| Tactic | Availability | Authority | Behavior |
|--------|--------------|-----------|----------|
| **Disciplined Fire** | Always (HQ default) | 0 | Focus weakest HP enemy; +1 damage buff |
| **Advance** | Always | 0 | Aggressive push; +10% move charge |
| **Stand Ground** | Always | 0 | Hold position; −10% move charge; prefer neutral-column targets |
| **Protect Support** | `Command`-tagged piece on board | 1 (pause 1) / 2 (pause 2) | Prefer rear/support threats; +2 armor steps in rear zone |

**Pause 2 tactic switch surcharge:** Switching to a different tactic costs +1 Authority (0-cost tactics) or base + 1 (Protect Support).

### Demo abilities

Execute at **start of next segment**. Source piece must be alive at submission.

| Ability | Source | Pause 1 | Pause 2 | Effect |
|---------|--------|---------|---------|--------|
| **Grenade Lob** | Grenade Thrower | 2 | 3 | 30 explosive damage in 2×2 area |
| **Shield Allies** | Armored Transport | 2 | 2 | Adjacent infantry +1 armor tier next segment |
| **Cannon Blast** | Mobile Cannon | — | 4 | 50 explosive primary + 25 splash adjacent |

### Enemy tactics

Fixed per fight template (mostly Disciplined Fire or Stand Ground in demo).

---

## 7. Synergies & critical mass

### Adjacency synergies (fight start)

Applied by `SynergyEngine` when combat begins:

| Adjacent pair | Bonus |
|---------------|-------|
| Any piece + `Supply`-tagged neighbor | +1 damage |
| `Medic` adjacent to `Infantry` | +1 armor step |
| `Command` adjacent to `Artillery` | +2 damage |
| `Echo` adjacent to `Stealth` | +1 damage |

### Critical mass (board-wide thresholds)

Applied by `CriticalMassRules` when thresholds met:

| Threshold | Tag count | Team bonus |
|-----------|-----------|------------|
| Infantry mass | ≥3 `Infantry` | +2 damage (all friendly combatants) |
| Vehicle mass | ≥2 `Vehicle` | +1 armor shred step |
| Artillery mass | ≥2 `Artillery` | +3 damage (all friendly combatants) |

---

## 8. HQ rules

| Rule | Detail |
|------|--------|
| Faction-specific | Each faction references one HQ piece via `FactionSO.hqPieceId` |
| Auto-spawn | On `StartNewRun`, placed at fixed anchor on faction SO |
| Immovable | Cannot relocate, rotate, or move to Reserves |
| Not sellable | Cannot sell or remove during build phase |
| Not in shop | Never a shop offer |
| Combat | Static; destroyable; **instant loss** for owner at 0 HP |
| Default tactic | Disciplined Fire while player HQ alive |

---

## 9. Shop system

### Three lanes

| Lane | Stock |
|------|--------|
| **Offensive** | Units, offensive structures, offensive one-shots |
| **Defensive** | Buildings, defensive structures, defensive one-shots |
| **Specialty** | Hidden until unlocked by faction rules + board milestones |

**Offers:** 3 base per lane. Board pieces at shop open may unlock extra offers.

### Lane actions

- **Buy** — spend Supplies (and sometimes Authority cost on piece).
- **Reroll** — one lane per round, Supplies cost scales.
- **Freeze** — one offer persists across rounds at its **slot index** (position preserved on reroll).

### Shop weighting by fight index

| Fight | Neutral pool | Faction-exclusive pool |
|-------|--------------|------------------------|
| 1–3 | 85% | 15% |
| 4–6 | 55% | 45% |
| 7–10 | 25% | 75% |

### Board-driven shop modifiers

Buildings on grid modify generation (examples):

| Building | Effect |
|----------|--------|
| Supply Depot | −10% gold prices (cap −25%) |
| Radio Array | Enemy tag preview |
| Field Workshop | Engineers lane always has ≥1 building offer |
| Command bunker | +1 offensive slot |

---

## 10. Factions & content roster

### Playable factions (demo)

| Faction | ID | Theme | Starting quirks |
|---------|-----|-------|-----------------|
| **Ironmarch Vanguard** | `iron_vanguard` | Industrial brass, heavy armor, command focus | Default unlocked; `hq_command` |
| **Dust Scourge** | `dust_scourge` | Nomadic scavengers, gas warfare, salvage bonus | Unlocks on first victory; +12 starting Manpower |
| **Cartel of Echoes** | `cartel_of_echoes` | Stealth, echo tech, adjacency synergies | Unlocks on first victory; +3 starting Authority |

### Enemy factions (variety in gauntlet)

| Faction | ID | Theme |
|---------|-----|-------|
| Neutral Militia | `neutral` | Generic trench forces |
| Crimson Legion | `crimson_legion` | Heavy assault, tanks and elites |
| Ash Wraiths | `ash_wraiths` | Gas phantoms, stealth ambush |

### Core shop pool (8 pieces + HQ)

**Neutral (5):**

| Piece | Size | Key tags | Ability |
|-------|------|----------|---------|
| Conscript Rifleman | 1×1 | Infantry, Combatant | — |
| Grenade Thrower | 1×2 | Infantry, Combatant | Grenade Lob |
| Field Medic | 1×1 | Medic, Combatant | — |
| Armored Transport | 2×3 L | Vehicle, Combatant | Shield Allies |
| Mobile Cannon | 3×2 | Artillery, Vehicle | Cannon Blast |

**Iron Vanguard exclusives (3):**

| Piece | Size | Key tags | Special |
|-------|------|----------|---------|
| Rifle Squad | 1×1 | Infantry, Vanguard | Core IV infantry |
| Diesel Walker | 2×2 | Mechanical, Vanguard | IV bruiser |
| Radio Array | 1×2 | Command, Vanguard | Unlocks Protect Support |

**Faction-exclusive pieces** (Dust Scourge, Cartel, enemy factions) extend the pool via `DemoContentGenerator` — see `docs/demo-guide.md` for full roster.

### Enemy gauntlet (10 fights)

| Fight | Tutorial theme | Notes |
|-------|----------------|-------|
| 1 | Conscript Line | HQ + 1 conscript in support |
| 2 | Patrol | HQ + conscript + field medic |
| 3 | Field Support | HQ + 2 conscripts |
| 4–10 | Escalating | Mixed faction kits; grenades from fight 4+ |

Fights 4–10 use recognizable units from the same vocabulary with increasing threat.

---

## 11. Meta progression

### Achievements (local)

Examples from `AchievementCatalog`:

| ID | Name | Trigger |
|----|------|---------|
| `clear_gauntlet` | Gauntlet Cleared | Win full 10-fight campaign |
| `win_no_hq_damage` | Untouched Command | Win fight without HQ damage |
| `critical_mass_five` | Critical Mass | Trigger critical mass 5× in one run |
| `salvage_hundred` | Salvage Master | Salvage 100 supplies in one run |
| `perfect_morale_victory` | Perfect Morale Victory | Win gauntlet at max morale |

### Faction unlocks

- **Ironmarch Vanguard:** Available from start.
- **Dust Scourge** and **Cartel of Echoes:** Unlock on first gauntlet victory.

### Leaderboard

- Local JSON persistence (`deadmanzone_meta.json`).
- Top 100 entries by score; filterable by faction.
- **Steam integration:** Stub ready (`SteamIntegration.cs`); requires Steamworks SDK wiring.

### UI

- Main menu: Achievements panel, Leaderboard panel.
- Achievements and scores persist across runs.

---

## 12. Save & resume

Players can exit at any point mid-run and resume exactly where they left off.

**Saved state includes:**

- Fight index (1–10), build vs mid-combat
- Full board layout, Reserves snapshot, frozen shop offer
- Supplies, Manpower, Authority, Morale, reroll counts
- Current shop offers (seed-locked)
- Mid-combat: seed, board snapshots, segment index, tick position, pause state, partial event log
- Faction choice, run seed, HQ instance

**Behavior:**

- Auto-save on fight start/end, shop purchase, board finalize, pause submission, app pause/quit.
- Pause menu: Resume, Options (stub), Main Menu (persist + continue later), Exit (persist + quit).
- One active run save slot for demo.
- Corrupt save → clear error; offer new run.
- **No migration** from legacy bench format or 6-row board — new runs only after layout update.

**Serialization:** JSON in `Application.persistentDataPath`; schema aligned with future cloud save / async PvP.

---

## 13. Presentation & UI

### Screens

```
Main Menu (Continue / New Run / Achievements / Leaderboard)
  → Faction select
  → Run hub (build phase)
  → Combat replay + tactic pause panels
  → Battle report
  → Win / Lose
```

### Build screen layout

| Element | Position / behavior |
|---------|-------------------|
| HUD | Top-left: fight N/10, phase, four resources, reroll cost |
| MENU button | Top-right: pause overlay |
| Main board | Center; zone headers above, color strip below |
| Reserves | Bottom; 2×9 grid |
| Shop | Three lane columns; modifier/enemy preview tooltip |

### Combat presentation (demo)

- **Unified 2D grid** — isometric unit tokens on top-down terrain; same sprites in build and combat (no 3D scene transition).
- Simple attack flashes and damage numbers (`CombatVfxController`).
- Brief loading overlay entering combat.
- Y-sort, drop shadows, and VFX for depth (see 2D visual commitment spec).
- Fog-of-war gas reveal: **deferred** post-demo.
- Full 3D battle replay: **deferred** post-demo (optional presentation skin on same event log).

---

## 14. Art pipeline

### Visual direction — Neutral forces

| Attribute | Direction |
|-----------|-----------|
| Palette | Worn olive drab, mud-brown, dull gunmetal, off-white bandages |
| Wear | Beat-up kit, patched coats, scuffed vehicles |
| vs Iron Vanguard | IV = brass, diesel glow; Neutral = mud, canvas, field-expedient |

### Camera standard (locked)

**Visual commitment:** `2026-06-06-deadmanzone-top-down-visual-commitment.md`

| Asset | Camera | Resolution |
|-------|--------|------------|
| Unit tokens (shop/board/combat) | Orthographic 3/4 isometric (~35°, facing bottom-right) | 256×256 icon, 128×128 cell |
| Terrain tiles (zones) | True top-down (90°) | Per tile asset |

**AI style anchor:** `Assets/Grok Images/Isometric Batch 2/grok-image-2eb75a93-e52d-4847-ae43-03394588e5fd.jpg`

### Pipeline (primary: AI)

```
SuperGrok Imagine → crop + background removal → PNG → Unity Sprite → PieceDefinitionSO.icon
```

Optional Blender fallback: `neutral_token_camera.py` isometric render for vehicles.

**Phased delivery:**

| Phase | Deliverable | Status |
|-------|-------------|--------|
| 1 | Conscript Rifleman icon + template scene | In progress |
| 2 | Remaining 4 neutral icons | Planned |
| 3 | Per-cell board tiles + `PieceShapeVisual` hook | Requires implementation plan |
| 4 | Drag-ghost sprites, faction variants | Optional |

Folder: `Assets/_Project/Art/Neutral/`

---

## 15. Technical architecture

### Layer split

```
Unity Presentation Layer
  GridUI, ShopUI, CombatReplay, TacticPausePanel, SaveLoadUI, MainMenu, VFX
        ↕ commands / events
Game Orchestrator (Unity, thin)
  RunOrchestrator, scene flow, save/load, meta tracking, Steam stub
        ↕
Core Sim (pure C#, no Unity refs)
  Board, Reserves, Shop, Combat ticks, Synergies, Meta serializers
        ↕
Data (ScriptableObjects)
  Pieces, Factions, Enemies, ContentDatabase
```

### Core modules

| Module | Responsibility |
|--------|----------------|
| `BoardState` / `ReservesState` | Grid placement, zones, rotation |
| `BattlefieldState` | Combined 25-wide grid, neutral band |
| `ShopGenerator` | Lanes, modifiers, fight-index weighting, slot-index lock |
| `TickCombatRun` | Segment orchestration, win detection |
| `CombatMovement` | Charge budget, terrain costs, range closure |
| `CombatDamageResolver` | Armor + attack-type RPS |
| `TacticEffects` / `CombatAbilityExecutor` | Tactic modifiers, demo abilities |
| `SynergyEngine` / `CriticalMassRules` | Fight-start buffs |
| `SalvageCalculator` | Sell refunds |
| `GasDamageSystem` | Segment 3 environmental damage |
| `BattleReportBuilder` | Fight stat aggregation |
| `MetaProgressionService` | Achievements, unlocks, leaderboard |
| `RunSaveSerializer` | Full run snapshot |

### Project structure

```
Assets/_Project/
  Core/           ← pure C# sim (asmdef: no Unity refs)
  Core.Tests/     ← NUnit EditMode tests
  Game/           ← RunOrchestrator, SteamIntegration
  Presentation/   ← UI, views, CombatDirector, VFX
  Data/           ← ScriptableObjects, Editor content generators
  Art/            ← Neutral pipeline assets
docs/
  DeadManZone-Game-Design-Document.md   ← this file
  demo-guide.md                         ← setup & play instructions
  superpowers/specs/                    ← detailed subsystem specs
```

### Async PvP (future)

Serialize `{ seed, playerBoard, opponentBoard, playerCommands[] }`. Server or client runs sim → same event log → replay. Schema aligned with save format.

---

## 16. Testing strategy

| Layer | Focus | Method |
|-------|-------|--------|
| Core sim | Adjacency, synergies, critical mass, movement, gas, HQ win, save round-trip | NUnit EditMode |
| Integration | 10-fight headless; reload mid-pause → identical outcome | Sim-only regression |
| Balance | Tutorial pause #2 reach rate ≥90% on reference board (fights 1–3) | Seeded sweep |
| Unity | Placement, shop lock slots, Reserves, pause UI, replay catch-up | Play Mode + manual |
| Manual | Fun, pacing, readability, faction variety | Weekly playtest |

### Determinism requirement

Automated test: same seed + boards + commands → byte-identical event log. Run on every content change.

---

## 17. Implementation status (June 2026)

### Shipped in demo build

| System | Status |
|--------|--------|
| 10-fight gauntlet | ✅ |
| Four-resource economy | ✅ |
| Tick combat sim + replay | ✅ |
| Tactics + 3 demo abilities | ✅ |
| HQ auto-spawn, immovable | ✅ |
| Battle report (top 3 dealt/taken) | ✅ |
| 7-column neutral (25-wide battlefield) | ✅ |
| Movement charge + attack speed wiring | ✅ |
| Reserves 2×9 + rotation | ✅ |
| Pause menu + auto-save | ✅ |
| Shop lock slot preservation | ✅ |
| 3 playable + 3 enemy factions (content gen) | ✅ |
| Synergies + critical mass | ✅ |
| Salvage calculator + Dust Scourge bonus | ✅ |
| Meta: achievements, leaderboard, faction unlocks | ✅ |
| Tutorial economy + softened fights 1–3 | ✅ |
| Steam stub | ✅ (SDK not wired) |

### Known gaps / tuning in flight

| Item | Notes |
|------|-------|
| Main Fight ticks | Code: 200; spec: 300 — pending revert |
| Neutral art icons | Pipeline started; Conscript + Armored Transport in progress |
| Per-cell board sprites | Phase 3 art + code plan not started |
| Fog-of-war combat intro | Deferred |
| Full 25-keyword mechanics | Deferred; tags scaffold in place |
| Steam achievements/leaderboards | Requires Steamworks SDK |
| Build screen zone headers | Spec approved; verify scene refresh |
| Emergency Draft button | May need Inspector wiring in Run scene |

---

## 18. Out of scope (demo)

- Async PvP matchmaking UI
- 11 additional playable factions
- Branching campaign map
- Event / rest nodes
- 3D in-engine combat models
- Rigging / animation
- Full keyword encyclopedia (25 keywords)
- HQ relocation abilities
- EMP / Incendiary combat effects
- Save migration from legacy formats

---

## 19. Success criteria

### Demo complete when

- [ ] Playtester completes 10-fight run in ~30–40 minutes
- [ ] Understands four-resource tradeoffs and manpower gate
- [ ] Uses tactic selection at both pauses meaningfully
- [ ] At least one demo ability used when source piece on board
- [ ] Save mid-combat → resume with identical outcome
- [ ] ≥90% tutorial sims reach pause #2 (fights 1–3)
- [ ] 5 neutral shop icons assigned and readable at shop scale
- [ ] All EditMode tests pass

### Player experience goals

- Feels the economy-vs-combat tension every run
- Horizontal combat readable without micro
- Wants to replay with different faction or build
- Tutorial teaches shop rhythm without hidden combat modifiers

---

## 20. Design decisions log

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Architecture | Deterministic core sim + Unity shell | Testable; PvP-ready; clean combat/UI split |
| Board in combat | Same grid + enemy half + 7 neutral columns | Placement matters; horizontal push fantasy |
| Zone layout | Vertical columns: rear \| support \| front | Front adjacent to neutral/enemy |
| Win condition | No `Combatant` or HQ destroyed | Clear goal; HQ pressure |
| Manpower | Hard block + once/run draft + shop relief | Attrition without per-unit HP tracking |
| Morale | Severity × fight index on loss | Run arc wears down |
| Tags | Same system as keywords | Avoid duplicate schema |
| Reserves | Spatial 2×9, Backpack Battles–style | More build expression than 3-slot bench |
| Rotation | Optional Q/R while dragging | Spatial puzzle depth |
| Tutorial softness | Enemy templates only | Transparent; no hidden player nerfs |
| Faction unlock | First victory unlocks 2 factions | Light meta without grind |
| Visual presentation | 2D grid: isometric tokens + top-down terrain | Infantry readable; matches code and solo scope |
| Art pipeline | SuperGrok AI sprites (Blender optional) | Style anchor locked; 3D combat deferred |

---

## 21. Setup & play (developer)

1. Open project in Unity 6
2. **DeadManZone → Generate Demo Content (5 Factions)**
3. **DeadManZone → Create Default UI Theme**
4. **DeadManZone → Setup Main Menu & Run Scenes**
5. Play from Main Menu

See `docs/demo-guide.md` for faction IDs and known issues.

---

## Appendix A — Authority cost reference

| Item | Pause 1 | Pause 2 |
|------|---------|---------|
| Protect Support | 1 | 2 (+ switch surcharge) |
| Grenade Lob | 2 | 3 |
| Shield Allies | 2 | 2 |
| Cannon Blast | — | 4 |
| Tactic switch (0-cost tactics) | — | +1 |

---

## Appendix B — Stat tier reference

**Attack speed multipliers:** Slow ×1.5, Medium ×1.0, Fast ×0.75  
**Movement frequency (ticks):** None ∞, Low 3, Medium 2, High 1  
**Attack range (Manhattan):** Short 1, Medium 3, Long 6

---

*This document will be updated as subsystems ship. For implementation tasks, see `docs/superpowers/plans/`.*
