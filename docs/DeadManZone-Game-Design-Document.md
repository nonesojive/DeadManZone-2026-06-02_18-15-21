# DeadManZone — Game Design Document

**Version:** 2.0  
**Date:** 2026-06-14  
**Engine:** Unity 6 (URP)  
**Status:** Playable demo — active development toward 1.0  
**Audience:** Design, engineering, art, QA, external collaborators

---

## Document purpose

This is the **canonical design reference** for DeadManZone. It describes the full intended product as if greenfield, while noting what the current demo already ships. Use it for onboarding, content authoring, and prioritization.

**Companion docs:**

| Document | Focus |
|----------|-------|
| `docs/superpowers/plans/2026-06-14-deadmanzone-greenfield-implementation.md` | Milestone roadmap (Unity/C#) |
| `docs/demo-guide.md` | Setup, play, known issues |
| `docs/superpowers/specs/` | Subsystem drill-down specs |

---

## Table of contents

1. [Executive summary](#1-executive-summary)
2. [Vision & pillars](#2-vision--pillars)
3. [Player fantasy & core loop](#3-player-fantasy--core-loop)
4. [Economy — four resources](#4-economy--four-resources)
5. [Board, zones & reserves](#5-board-zones--reserves)
6. [Combat system](#6-combat-system)
7. [Command pauses — tactics & abilities](#7-command-pauses--tactics--abilities)
8. [Tags, synergies & critical mass](#8-tags-synergies--critical-mass)
9. [HQ rules](#9-hq-rules)
10. [Shop system](#10-shop-system)
11. [Factions & content scope](#11-factions--content-scope)
12. [Meta progression](#12-meta-progression)
13. [Save & resume](#13-save--resume)
14. [Presentation & UX](#14-presentation--ux)
15. [Art & 3D pipeline](#15-art--3d-pipeline)
16. [Technical architecture](#16-technical-architecture)
17. [Data-driven design](#17-data-driven-design)
18. [Testing & quality bar](#18-testing--quality-bar)
19. [Roadmap scope tiers](#19-roadmap-scope-tiers)
20. [Design decisions log](#20-design-decisions-log)

---

## 1. Executive summary

**DeadManZone** is a grimdark retro-futurist WW1 trench autobattler. Players act as **quartermaster and general**: arrange shaped pieces on a zoned grid, shop between fights, then watch deterministic combat auto-resolve with **two command pauses** per fight. No unit micro.

| Attribute | Value |
|-----------|-------|
| Genre | Roguelite autobattler / spatial loadout builder |
| Session | 10-fight linear gauntlet, ~30–40 minutes |
| Build phase | 2D UI grid puzzle (*Backpack Battles*, *The Bazaar*) |
| Combat phase | 3D Synty arena spectacle (*Top Troops* tempo) |
| Sim | Pure C# tick combat, event-log replay, PvP-ready |
| Demo factions | 3 playable, 3 enemy pools |
| Long-term | 12 factions, async PvP, campaign chapters |

**Core tension:** Every run forces a trade between **economy/shop power** (rear buildings, shop modifiers) and **combat capability** (front-line units, synergies).

---

## 2. Vision & pillars

### Fantasy

A planet locked in endless trench war — warlords rise and fall, but the grind never ends. Brass machinery, diesel engines, gas masks, coil-rifles, and field-expedient armor under perpetual artillery haze.

### Reference matrix

| Layer | Influences | What we take |
|-------|------------|--------------|
| Build / shop | *Backpack Battles*, *The Bazaar* | Spatial grid, adjacency synergies, lane shop, freeze/reroll |
| Combat feel | *Total War*, TFT clarity | Segment tempo, general-not-sergeant commands |
| Presentation | *Top Troops* | 3D angled arena, auto-battle spectacle |
| Long-term | Async autobattlers | Deterministic sim from day one |

### Design pillars

1. **Spatial loadout** — Shaped pieces, zone restrictions, rotation, adjacency synergies, tag combos.
2. **Economy vs war** — Four resources; rear buildings compete with front-line combat power.
3. **Casualties without per-unit HP bookkeeping** — Manpower upkeep and salvage abstract losses between fights.
4. **Run tension via Morale** — Rare heals; losses hurt more as gauntlet progress increases.
5. **Horizontal war** — Build layout is fight layout; war pushes **left (player) → right (enemy)**.
6. **General, not sergeant** — Positioning matters in sim; player sets **tactics** and **queued abilities** at pauses only.
7. **Data-first content** — New pieces, factions, enemies, and synergies ship via ScriptableObjects without code changes.

---

## 3. Player fantasy & core loop

### Run flow

```
Main Menu
  → Faction select (Ironmarch Vanguard unlocked; others on first victory)
  → Opening shop + board placement (HQ auto-spawned)
  → Fight loop ×10
       Build phase   — shop, place/move in board or Reserves, manpower gate
       Combat        — 3D arena replay, 4 segments, 2 pause windows
       Aftermath     — battle report, rewards, morale check
  → Victory (fight 10) / Defeat (Morale = 0)
```

### Build phase

- Three shop lanes: **Offensive**, **Defensive**, **Specialty** (unlocked by milestones).
- Drag pieces between **main board**, **Reserves** (2×9 spatial grid), and sell zone.
- **Q/R rotation** while dragging.
- **Manpower gate** blocks fight start if deployed upkeep exceeds available Manpower.
- Board persists across gauntlet; HQ immovable for entire run.

### Combat phase

- Deterministic tick sim → event log → Unity replay in 3D combat arena.
- Four segments: Opening → Main Fight → Brief Push → Gas ramp.
- Two pause windows after Opening and Main Fight.

### Aftermath

- Battle report: outcome, supplies, morale delta, manpower refund, top damage dealt/taken.
- Authority resets next build round.
- Shop refreshes with fight-index faction weighting.

---

## 4. Economy — four resources

| Resource | Role | Earned from | Spent on |
|----------|------|-------------|----------|
| **Supplies** | Shop currency | Fight rewards, salvage (~50%) | Purchases, rerolls, relief items |
| **Manpower** | Deploy gate — upkeep per `combatant` on board | HQ + buildings each build round; survivor refund | Implicit deploy cost |
| **Authority** | Command currency; **resets each build round** | HQ, buildings, units | Tactics, pause abilities |
| **Morale** | Run health (**0 = run over**) | Rare pieces/effects | Loss severity × fight index; some buys |

### Manpower gate

- **Hard block:** Cannot begin fight if board upkeep > available Manpower.
- **Once per run:** Emergency Draft covers shortfall.
- **Relief:** Shop one-shots and buildings may spend Supplies and/or Morale.

### Salvage (sell piece)

| Refund | Ratio |
|--------|-------|
| Supplies | 50% |
| Authority | 50% |
| Manpower | 25% |

**Dust Scourge bonus:** +25% supplies from salvage.

### Tutorial economy (fights 1–3)

| Moment | Supplies |
|--------|----------|
| Run start | 125 |
| Win fight 1 | +100 |
| Win fight 2 | +105 |
| Win fight 3 | +110 |
| Fights 4+ | Escalating curve (22, 25, 28, …) |

**Principle:** Tutorial softness from **enemy composition only** — no hidden player combat nerfs.

---

## 5. Board, zones & reserves

### Player half (build = combat)

| Property | Value |
|----------|-------|
| Width | 9 columns |
| Height | 10 rows |
| Rear | 4 leftmost columns |
| Support | 3 middle columns |
| Front | 2 rightmost columns (toward enemy) |

Pieces have **shapes** (1×1, 1×2, 2×2, L-shapes). Placement is a spatial puzzle with zone restrictions.

### Combined battlefield (25 columns)

```
[ Player 9: Rear(4) | Support(3) | Front(2) ] [ Neutral 7 ] [ Enemy 9: Front(2) | Support(3) | Rear(4) ]
|<------------------- 9 columns ------------------->| x=9..15 |<---------------- 9 columns --------------->|
```

- Units move cell-to-cell during combat from build placement.
- Neutral columns: **2× movement charge** in Opening/Main Fight; **gas damage** ramps in final segment.

### Reserves

- **2×9** spatial grid (18 cells), no zone restrictions.
- Capacity = free cells, not slot count.
- Rotation persisted in save.

---

## 6. Combat system

### Architecture

```
Build finalized → TickCombatRun.Start(seed, playerBoard, enemyBoard)
  → Segment 1 (Opening)      → PAUSE 1
  → Segment 2 (Main Fight)   → PAUSE 2
  → Segment 3a (Brief Push)  → no pause
  → Segment 3b (Gas ramp)    → until win/loss
  → BattleReport
```

- **Deterministic pure C# sim** — no Unity refs in Core.
- Same seed + boards + pause submissions → identical event log (save/resume, future async PvP).

### Segment pacing

Sim: **10 ticks/second**. Tunable via `CombatPacingConfig` ScriptableObject.

| Segment | Ticks | ~Wall-clock | Damage scale |
|---------|-------|-------------|--------------|
| Opening | 50 | ~5s | 0.2× |
| Main Fight | 300 | ~30s | 1.0× |
| Brief Push | 50 | ~5s | 1.0× |
| Gas | Until winner | — | ramps |

### Win / loss / draw

| Outcome | Condition |
|---------|-----------|
| **Win** | Enemy has zero `combatant` pieces **or** enemy HQ destroyed |
| **Loss** | Player combatants eliminated **or** player HQ destroyed |
| **Draw** | Mutual last-combatant death same tick → morale win, ~50% supplies |

### Auto-combat rules

- Movement via **charge budget** (tier-based frequency).
- Targeting by **active tactic** + **attack range** (Manhattan).
- Attacks on cooldown modified by **attack speed** tier.
- Buildings: `MovementSpeed.None` unless data overrides.
- **Rock-paper-scissors lite** on armor vs attack type.

### Stat tiers (data enums)

| Field | Tiers | Effect |
|-------|-------|--------|
| AttackSpeed | Slow / Medium / Fast | Cooldown multiplier |
| AttackRange | Short (1) / Medium (3) / Long (6) | Max Manhattan distance |
| MovementSpeed | None / Low / Medium / High | Move every N ticks |
| ArmorType | None / Light / Medium / Heavy | DR + RPS |
| AttackType | Ballistic / Explosive / Piercing | Type bonuses |

| Attack type | Bonus | Multiplier |
|-------------|-------|------------|
| Ballistic | vs Light armor | ×1.25 |
| Explosive | vs Light/Medium or building/structure | ×1.30 |
| Piercing | vs Heavy armor | ×1.35 |

**Armor baseline:** Light 100%, Medium 85%, Heavy 70%.

---

## 7. Command pauses — tactics & abilities

### Pause triggers

After **Opening** and **Main Fight** only. Full **visual freeze** in 3D arena during pause overlay.

### Tactics

| Tactic | Availability | Authority | Behavior |
|--------|--------------|-----------|----------|
| Disciplined Fire | Always (HQ default) | 0 | Focus weakest HP; +1 damage |
| Advance | Always | 0 | Push; +10% move charge |
| Stand Ground | Always | 0 | Hold; −10% move charge; prefer neutral targets |
| Protect Support | Command-tagged piece | 1 / 2 | Prefer rear/support threats; +2 armor in rear |

**Pause 2 switch surcharge:** +1 Authority when changing tactic.

### Demo abilities

Execute at **start of next segment**. Source must be alive at submission.

| Ability | Source | Pause 1 / 2 cost | Effect |
|---------|--------|------------------|--------|
| Grenade Lob | Grenade Thrower | 2 / 3 | 30 explosive, 2×2 AoE |
| Shield Allies | Armored Transport | 2 / 2 | Adjacent infantry +1 armor tier |
| Cannon Blast | Mobile Cannon | — / 4 | 50 primary + 25 splash |

---

## 8. Tags, synergies & critical mass

### Tag philosophy

Tags = **identity layer**. Stats/enums = **numbers layer**. One unified tag registry powers authoring, sim, synergies, shop filters, and player cards.

| Category | Count | Examples |
|----------|-------|----------|
| Primary | exactly 1 | `infantry`, `vehicle`, `building`, `structure` |
| Combat role | exactly 1 | `assault`, `tank`, `artillery`, `support`, `headquarters` |
| System | exactly 1 | `combatant`, `noncombatant`, `hq` |
| Faction | exactly 1 | `neutral`, `iron_vanguard`, `dust_scourge` |
| Synergy | 0–4 | `scout`, `medic`, `command`, `vanguard`, `echo`, `stealth` |

### Adjacency synergies (fight start)

| Pair | Bonus |
|------|-------|
| Any + Supply neighbor | +1 damage |
| Medic + Infantry | +1 armor step |
| Command + Artillery | +2 damage |
| Echo + Stealth | +1 damage |

### Critical mass (board-wide)

| Threshold | Bonus |
|-----------|-------|
| ≥3 Infantry | +2 damage all combatants |
| ≥2 Vehicle | +1 armor shred step |
| ≥2 Artillery | +3 damage all combatants |

---

## 9. HQ rules

| Rule | Detail |
|------|--------|
| Faction-specific | `FactionSO.hqPieceId` |
| Auto-spawn | Fixed anchor on run start |
| Immovable | No move, rotate, or Reserves |
| Not sellable | Cannot remove during run |
| Not in shop | Never offered |
| Combat | Static, destroyable; **instant loss** at 0 HP |
| Default tactic | Disciplined Fire while alive |

---

## 10. Shop system

### Lanes

| Lane | Stock |
|------|-------|
| Offensive | Units, offensive structures, one-shots |
| Defensive | Buildings, defensive structures, one-shots |
| Specialty | Unlocked by faction + board milestones |

**Base:** 3 offers per lane. Board pieces may unlock extra slots.

### Actions

- **Buy** — Supplies (+ Authority cost on some pieces).
- **Reroll** — one lane per round, scaling Supplies cost.
- **Freeze** — one offer persists at **slot index** across rerolls.

### Fight-index weighting

| Fights | Neutral pool | Faction-exclusive |
|--------|--------------|-------------------|
| 1–3 | 85% | 15% |
| 4–6 | 55% | 45% |
| 7–10 | 25% | 75% |

### Board-driven modifiers (examples)

| Building | Effect |
|----------|--------|
| Supply Depot | −10% prices (cap −25%) |
| Radio Array | Enemy tag preview |
| Field Workshop | Defensive lane always has ≥1 building |
| Command Bunker | +1 offensive slot |

---

## 11. Factions & content scope

### Playable (demo)

| Faction | ID | Hook |
|---------|-----|------|
| Ironmarch Vanguard | `iron_vanguard` | Command, heavy armor, default unlocked |
| Dust Scourge | `dust_scourge` | Gas, salvage bonus, +12 Manpower |
| Cartel of Echoes | `cartel_of_echoes` | Stealth/echo synergies, +3 Authority |

### Enemy pools (demo)

| Faction | ID | Theme |
|---------|-----|-------|
| Neutral Militia | `neutral` | Generic trench forces |
| Crimson Legion | `crimson_legion` | Heavy assault |
| Ash Wraiths | `ash_wraiths` | Gas, stealth ambush |

### Core neutral roster (shop)

| Piece | Size | Ability |
|-------|------|---------|
| Conscript Rifleman | 1×1 | — |
| Grenade Thrower | 1×2 | Grenade Lob |
| Field Medic | 1×1 | — |
| Armored Transport | 2×3 L | Shield Allies |
| Mobile Cannon | 3×2 | Cannon Blast |

**Full sandbox roster:** 25 pieces across factions (see `DemoContentGenerator`, `docs/demo-guide.md`).

### Gauntlet (10 fights)

| Fights | Theme |
|--------|-------|
| 1–3 | Tutorial — conscript lines, teach pauses |
| 4–10 | Escalating mixed faction kits |

---

## 12. Meta progression

### Demo meta

- **Achievements** — local JSON (10 demo achievements).
- **Leaderboard** — top 100 by score, faction filter.
- **Faction unlocks** — Dust Scourge + Cartel on first victory.
- **Steam stub** — schema ready; SDK wiring post-demo.

### Post-demo meta (design intent)

- Faction mastery tracks.
- Cosmetic unlocks (banners, arena skins).
- Async PvP rating.

---

## 13. Save & resume

Exit at any point; resume exactly where left off.

**Saved:** fight index, build vs combat, board + Reserves, shop offers (seed-locked), all four resources, mid-combat seed/tick/pause state, partial event log, faction, run seed.

**Behavior:** auto-save on key transitions; one active slot (demo); corrupt save → clear error + new run offer.

**Format:** JSON in `Application.persistentDataPath`; PvP-aligned schema.

---

## 14. Presentation & UX

### Screen flow

```
Main Menu → Faction Select → Run Hub (build)
  → Combat Arena (additive 3D sub-scene) + pause overlays
  → Battle Report → Win/Lose
```

### Build phase (2D UI)

| Element | Behavior |
|---------|----------|
| HUD | Fight N/10, phase, four resources, reroll cost |
| Main board | Center; zone headers; drag-drop + rotation |
| Reserves | Bottom 2×9 grid |
| Shop | Three lane columns; buff strip; enemy preview tooltip |
| Sell zone | Drag to sell; salvage feedback |

### Combat phase (3D arena)

- **Hybrid presentation:** flat build UI → additive 3D arena on fight start.
- Synty low-poly units/buildings; Sidekick locomotion for infantry.
- `CombatDirector` replays event log → arena actors.
- Tactic pause: **full visual freeze** + overlay panel.
- VFX: muzzle flash, impacts, damage numbers, death bursts.
- Return to build: unload arena, restore flat UI.

### Main menu

- Cinematic backdrop (PolygonMaps warehouse preset).
- Continue / New Run / Achievements / Leaderboard.
- Military HUD theme (InterfaceMilitaryCombatHUD).

---

## 15. Art & 3D pipeline

### Visual direction

Grimdark retro-futurist WW1 trench — brass, mud, gas-lamp amber. Synty POLYGON readability at top-down/isometric camera.

| Layer | Source | Notes |
|-------|--------|-------|
| Combat units | SidekickCharacters + AnimationBaseLocomotion | 5 role prefabs, scale/tint variance |
| Vehicles | PolygonWar / PolygonMech | Static mesh v1 |
| Buildings | PolygonWar bunkers, nests, crates | Wrapper prefabs under `_Project/Art/Synty/` |
| Arena terrain | PolygonMaps woodland apocalypse | Ground tile + trench props |
| VFX | PolygonParticleFX | Gunshot, dust, explosion |
| UI | InterfaceMilitaryCombatHUD | Replaces legacy BunkerSurvivalUI |
| Board/shop icons | Prefab snapshots → PNG | 256×256, consistent framing |

### Synty rendering rules (mandatory)

- Assign URP project-wide; **do not** batch-convert Synty materials to URP/Lit.
- Use native Synty shader graphs.
- Combat instantiates Synty prefabs as-is; wrappers live under `Assets/_Project/Art/Synty/`.
- Editor menu: **DeadManZone → Synty → Apply Full Synty Art Pass**.

### Build-phase icons

Isometric 3/4 tokens (~35°) for shop/board; top-down terrain tiles for zone grid.

---

## 16. Technical architecture

### Layer split

```
Presentation (Unity)
  BoardView, ShopView, CombatArenaPresenter, TacticPausePanel, VFX, MainMenu
        ↕ commands / events
Game (thin Unity orchestration)
  RunOrchestrator, RunManager, SaveManager, MetaProgressionService
        ↕
Core (pure C#, no Unity refs)
  Board, Reserves, Shop, TickCombatRun, Synergies, Save schema
        ↕
Data (ScriptableObjects)
  PieceDefinitionSO, FactionSO, EnemyTemplateSO, ContentDatabase, configs
```

### Assembly map

| Assembly | Responsibility |
|----------|----------------|
| `DeadManZone.Core` | All sim logic, serializers |
| `DeadManZone.Core.Tests` | EditMode NUnit |
| `DeadManZone.Data` | ScriptableObjects, runtime bootstrap |
| `DeadManZone.Data.Editor` | Content generators, art pass tools |
| `DeadManZone.Game` | Run flow, save hooks |
| `DeadManZone.Presentation` | UI, arena, combat replay |
| `DeadManZone.Presentation.Editor` | Scene setup, URP, Synty pass |
| `DeadManZone.PlayMode.Tests` | Integration tests |

### Core modules

| Module | Role |
|--------|------|
| `BoardState` / `ReservesState` | Placement, zones, rotation |
| `BattlefieldState` | 25-wide combined grid |
| `ShopGenerator` | Lanes, weighting, modifiers, freeze slots |
| `TickCombatRun` | Segment orchestration |
| `CombatMovement` / `CombatDamageResolver` | Movement + damage |
| `TacticEffects` / `CombatAbilityExecutor` | Pauses |
| `SynergyEngine` / `CriticalMassRules` | Fight-start buffs |
| `GasDamageSystem` | Environmental finisher |
| `RunSaveSerializer` | Full snapshot |
| `MetaProgressionService` | Achievements, leaderboard |

### Async PvP (future)

Submit `{ seed, playerBoard, opponentBoard, commands[] }` → server/client sim → shared event log → replay.

---

## 17. Data-driven design

### Content authoring model

All gameplay content ships as **ScriptableObjects** validated at import. Code defines **systems**; data defines **instances**.

| Asset type | Defines | Consumed by |
|------------|---------|-------------|
| `PieceDefinitionSO` | Shape, tags, stats, costs, ability, arena prefab, icon | Board, shop, combat |
| `FactionSO` | HQ piece, starting resources, specialty rules | Run start, shop weighting |
| `EnemyTemplateSO` | Pre-built enemy board per fight | Gauntlet |
| `SynergyRuleSO` | Adjacency pairs + bonuses | SynergyEngine |
| `CriticalMassRuleSO` | Tag thresholds | CriticalMassRules |
| `CombatRoleProfileSO` | AI weights per role tag | Targeting |
| `CombatPacingConfigSO` | Segment ticks, damage scales | TickCombatRun |
| `TagRegistrySO` | Canonical tag vocabulary | Authoring UI, validation |
| `VisualProfileSO` / `UiThemeSO` | Presentation theming | Presentation layer |
| `CombatArenaConfigSO` | Cell size, camera, transition timing | Arena |
| `ContentDatabase` | Master index of all content | Bootstrap |

### PieceDefinitionSO field groups

```
Identity:     pieceId, displayName, description
Shape:        cells[], defaultRotation
Tags:         primary, role, system, faction, synergy[]
Economy:      suppliesCost, authorityCost, manpowerCost, shopLane
Combat:       maxHp, baseDamage, cooldownTicks, attackSpeed, attackRange,
              movementSpeed, armorType, attackType, grantedAbility
Presentation: icon, combatArenaPrefab, combatArenaModelScale
Shop:         poolTags[], rarity, fightIndexMin
```

### Validation rules (editor)

- Exactly one primary, role, system, faction tag.
- HQ pieces: `hq` system tag, no shop lane.
- Buildings: rear-only primary unless hybrid rules apply.
- All tag IDs exist in `TagRegistrySO`.
- Arena prefab required for combatant pieces.

### Content pipeline (editor menus)

1. `DeadManZone → Generate Demo Content (5 Factions)`
2. `DeadManZone → Create Default UI Theme`
3. `DeadManZone → Setup Main Menu & Run Scenes`
4. `DeadManZone → Synty → Apply Full Synty Art Pass`
5. `DeadManZone → Rendering → Setup URP For Project`

---

## 18. Testing & quality bar

| Layer | Focus | Method |
|-------|-------|--------|
| Core sim | Placement, combat, synergies, gas, HQ, save round-trip | EditMode NUnit |
| Determinism | Same inputs → byte-identical event log | Regression test on every content change |
| Integration | 10-fight headless; mid-pause reload | Sim-only sweep |
| Balance | Tutorial reaches pause #2 ≥90% (fights 1–3) | Seeded reference boards |
| Presentation | Drag-drop, shop freeze, arena spawn, pause freeze | Play Mode |
| Art coverage | All 25 pieces have icon + arena prefab | `SandboxArtCoverageTests` |

### Demo ship criteria

- [ ] Full 10-fight run in ~30–40 minutes
- [ ] Four-resource tradeoffs understood without tutorial text wall
- [ ] Both pause windows used meaningfully
- [ ] Mid-combat save → identical outcome
- [ ] Synty-textured arena stable in Play Mode
- [ ] All EditMode + PlayMode tests pass

---

## 19. Roadmap scope tiers

| Tier | Scope | Target |
|------|-------|--------|
| **MVP / Demo** | 3 factions, 10 fights, 25 pieces, local meta, Synty arena | Current milestone |
| **1.0** | Polish pass, Steam integration, fog-of-war intro, full keyword encyclopedia UI | Post-demo |
| **1.x** | Async PvP, 6+ factions | Competitive mode |
| **2.0** | 12 factions, branching campaign, event nodes | Full roguelite |

### Explicitly out of scope (demo)

- Async PvP matchmaking UI
- 9 additional playable factions
- Branching campaign map
- Real-time combat micro
- HQ relocation abilities
- Save migration from pre-2026 layouts

---

## 20. Design decisions log

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Sim architecture | Pure C# core + Unity shell | Testable, PvP-ready |
| Build vs combat presentation | 2D UI build + 3D arena combat | Puzzle clarity + battle spectacle |
| Board in combat | Same grid + 7 neutral columns | Placement matters |
| Win condition | No combatants or HQ destroyed | Clear pressure on HQ |
| Manpower | Hard gate + draft + shop relief | Attrition without per-unit HP tracking |
| Morale | Loss severity × fight index | Run arc wears down |
| Tags | Unified keyword system | One schema for sim + UI |
| Reserves | Spatial 2×9 | More expression than slot bench |
| Art stack | Synty POLYGON on URP | Cohesive style, subscription assets |
| Content | ScriptableObject-first | Designers ship pieces without code |

---

## Appendix A — Authority costs

| Item | Pause 1 | Pause 2 |
|------|---------|---------|
| Protect Support | 1 | 2 (+ switch surcharge) |
| Grenade Lob | 2 | 3 |
| Shield Allies | 2 | 2 |
| Cannon Blast | — | 4 |
| Tactic switch (0-cost) | — | +1 |

## Appendix B — Stat tier reference

**Attack speed:** Slow ×1.5, Medium ×1.0, Fast ×0.75  
**Movement (ticks):** None ∞, Low 3, Medium 2, High 1  
**Range (Manhattan):** Short 1, Medium 3, Long 6

---

*For implementation milestones see `docs/superpowers/plans/2026-06-14-deadmanzone-greenfield-implementation.md`.*
