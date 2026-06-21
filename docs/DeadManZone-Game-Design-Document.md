# DeadManZone — Game Design Document

**Version:** 3.0  
**Date:** 2026-06-19  
**Engine:** Unity 6 (URP)  
**Status:** Playable demo — active development toward 1.0  
**Audience:** Design, engineering, art, QA, external collaborators

**Supersedes:** GDD v2.0 (2026-06-14)

---

## Document purpose

This is the **canonical design reference** for DeadManZone. It describes the intended product and **what the demo ships today**, with explicit callouts where design intent differs from long-term vision.

Use it for onboarding, content authoring, prioritization, and playtest planning.

**Companion docs:**

| Document | Focus |
|----------|-------|
| `docs/demo-guide.md` | Setup, factions, known issues |
| `docs/superpowers/plans/2026-06-14-deadmanzone-greenfield-implementation.md` | Milestone roadmap (M0–M9) |
| `docs/superpowers/specs/` | Subsystem drill-down specs (combat pauses, shop, UI kits, prefabs) |
| `docs/superpowers/specs/2026-06-19-manual-card-prefab-protection-design.md` | Unit/shop card prefab authoring rules |

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
19. [Implementation status](#19-implementation-status)
20. [Roadmap scope tiers](#20-roadmap-scope-tiers)
21. [Design decisions log](#21-design-decisions-log)

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

### v3.0 headline changes (from v2.0)

| Area | v2.0 doc | **Shipped (v3.0)** |
|------|----------|-------------------|
| Combat pacing | Fixed 4 segments (Opening / Main / Brief / Gas) | **Continuous fight** + **HP-triggered pauses** at 75% / 30% army HP |
| Battlefield width | 25 columns (7 neutral) | **23 columns (5 neutral)** |
| Shop UX | 3 lanes × 3 offers, per-lane reroll | **Unified 8–12 slot grid**, single reroll (lanes remain in data) |
| Synergies | Supply neighbor +1 damage | **Removed**; **Inspiring** +5% move charge added |
| Echo synergy | Echo + Stealth tag | Echo + neighbor with **Stealth ability** |
| Critical mass | 3 rules | **4 rules** (+ Assault ≥3 → +10% move charge) |
| Build HUD | Resources only | **Army strength / matchup preview** |
| Content count | “25 pieces” | **38 generated**, **25 Synty art-pass roster**, **46 piece assets** |
| UI cards | Procedural runtime cards | **Manually authored** `UnitDetailCard` + `ShopOfferCard` prefabs |

---

## 2. Vision & pillars

### Fantasy

A planet locked in endless trench war — warlords rise and fall, but the grind never ends. Brass machinery, diesel engines, gas masks, coil-rifles, and field-expedient armor under perpetual artillery haze.

### Reference matrix

| Layer | Influences | What we take |
|-------|------------|--------------|
| Build / shop | *Backpack Battles*, *The Bazaar* | Spatial grid, adjacency synergies, offer grid, freeze/reroll |
| Combat feel | *Total War*, TFT clarity | General-not-sergeant commands, readable army HP stakes |
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
  → Faction select (IronMarch Union unlocked; others on first victory)
  → Opening shop + board placement (HQ auto-spawned)
  → Fight loop ×10
       Build phase   — shop, place/move in board or Reserves, manpower gate
       Combat        — 3D arena replay, continuous sim, 2 HP-triggered pauses
       Aftermath     — battle report, rewards, morale check
  → Victory (fight 10 cleared) / Defeat (Morale ≤ 0)
```

### Build phase

- **Unified shop grid** (8 baseline slots, up to 12 with board unlocks).
- Drag pieces between **main board**, **Reserves** (2×9 spatial grid), and sell zone.
- **Q/R rotation** while dragging.
- **Manpower gate** blocks fight start if deployed upkeep exceeds available Manpower.
- **Army strength preview** compares player board vs next enemy template.
- Board persists across gauntlet; HQ immovable for entire run.

### Combat phase

- Deterministic tick sim → event log → Unity replay in additive 3D combat arena.
- **Two command pauses** when either army’s total combatant HP fraction crosses **75%**, then **30%** (first crossing per threshold, either side).
- **Anti-stall gas** begins at global tick **300** (~30 s); ramps until win or max fight length.

### Aftermath

- Battle report: outcome, supplies, morale delta, manpower refund, top damage dealt/taken.
- Authority resets next build round.
- Shop refreshes with fight-index faction weighting.

---

## 4. Economy — four resources

| Resource | Role | Earned from | Spent on |
|----------|------|-------------|----------|
| **Supplies** | Shop currency (UI label; code may use `goldCost`) | Fight rewards, salvage (~50%) | Purchases, rerolls, relief items |
| **Manpower** | Deploy gate — upkeep per `combatant` on board | HQ + buildings each build round; survivor refund | Implicit deploy cost |
| **Authority** | Command currency; **resets each build round** | HQ, buildings, units | Tactics, pause abilities (combat UI may show as Requisition) |
| **Morale** | Run health (**0 = run over**) | Rare pieces/effects | Loss severity × fight index; some buys |

### Manpower gate

- **Hard block:** Cannot begin fight if board upkeep > available Manpower.
- **Once per run:** Emergency Draft covers shortfall (Inspector wiring may be required in Run scene).
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
| Fights 4+ | Escalating curve |

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

### Combined battlefield (23 columns)

```
[ Player 9: Rear(4) | Support(3) | Front(2) ] [ Neutral 5 ] [ Enemy 9: Front(2) | Support(3) | Rear(4) ]
|<------------------- 9 columns ------------------->| x=9..13 |<---------------- 9 columns --------------->|
```

- Units move cell-to-cell during combat from build placement.
- Neutral columns: **2× movement charge** cost; contested positioning matters.
- **Gas damage** ramps in neutral band after global tick 300.

### Reserves

- **2×9** spatial grid (18 cells), no zone restrictions.
- Capacity = free cells, not slot count.
- Rotation persisted in save.

---

## 6. Combat system

### Architecture

```
Build finalized → TickCombatRun.Start(seed, playerBoard, enemyBoard)
  → Continuous tick loop (10 ticks/sec)
       → Pause 1 when army HP ≤ 75% threshold crossed
       → Pause 2 when army HP ≤ 30% threshold crossed
       → Gas ramp from tick 300
       → Win / loss / draw cap at tick 10,000
  → BattleReport
```

- **Deterministic pure C# sim** — no Unity refs in Core.
- Same seed + boards + pause submissions → identical event log (save/resume, future async PvP).

### Pacing constants (`CombatPacingConfig`)

| Parameter | Value | Notes |
|-----------|-------|-------|
| `TicksPerSecond` | 10 | Sim clock |
| `PauseThresholds` | 0.75, 0.30 | Army **total combatant HP fraction** |
| `GasStartTick` | 300 | ~30 s wall-clock |
| `MaxFightTicks` | 10,000 | Forces draw if no winner |
| `GasRampReferenceTicks` | 200 | Gas escalation curve |

Pauses fire on **either army** crossing a threshold (first time per threshold). Army HP tracked via `ArmyHealthTracker`; UI shows aggregate bars driving pause cues.

### Win / loss / draw

| Outcome | Condition |
|---------|-----------|
| **Win** | Enemy has zero `combatant` pieces **or** enemy HQ destroyed |
| **Loss** | Player combatants eliminated **or** player HQ destroyed |
| **Draw** | Mutual last-combatant death same tick **or** `MaxFightTicks` reached → morale win, reduced supplies |

### Auto-combat rules

- Movement via **charge budget** (tier-based frequency); multi-cell footprints use `ShapePathfinder`.
- Targeting by **active tactic** + **attack range** (Manhattan) + role engagement weights.
- Attacks on cooldown modified by **attack speed** tier.
- **Accuracy** resolves each shot as full hit, graze (33% damage), or clean miss (cooldown still spent).
- Distance falloff in **outer 40%** of max range; inner 60% has no falloff.
- Buildings: `MovementSpeed.None` unless data overrides.
- **Rock-paper-scissors lite** on armor vs attack type.

### Army strength (build phase)

`ArmyStrengthCalculator` produces a single strength score per board snapshot. `MatchupStrengthView` on the build HUD compares player vs next enemy template so players can gauge upcoming fight difficulty before committing Manpower.

### Stat tiers (data enums)

| Field | Tiers | Effect |
|-------|-------|--------|
| AttackSpeed | Slow / Medium / Fast | Cooldown multiplier |
| AttackRange | Melee (1) / Short (3) / Medium (5) / Long (8) | Max Manhattan distance |
| MovementSpeed | None / Low / Medium / High | Move every N ticks |
| ArmorType | None / Light / Medium / Heavy | DR + RPS |
| AttackType | Ballistic / Explosive / Piercing / Shredding / Melee / Gas | Type bonuses + accuracy default |

| Attack type | Bonus | Multiplier |
|-------------|-------|------------|
| Ballistic | vs Light armor | ×1.25 |
| Explosive | vs Light/Medium or building/structure | ×1.30 |
| Piercing | vs Heavy armor | ×1.35 |

**Armor baseline:** Light 100%, Medium 85%, Heavy 70%.

---

## 7. Command pauses — tactics & abilities

### Pause triggers

When **either army’s** combatant HP pool crosses **75%** then **30%** of fight-start total. Full **visual freeze** in 3D arena during pause overlay (`CombatArenaFreezeController`).

Player chooses tactic and optional abilities; submissions are deterministic inputs to the sim.

### Tactics

| Tactic | Availability | Authority | Behavior |
|--------|--------------|-----------|----------|
| Disciplined Fire | Always (HQ default) | 0 | Focus weakest HP; +1 damage |
| Advance | Always | 0 | Push; +10% move charge |
| Stand Ground | Always | 0 | Hold; −10% move charge; prefer neutral targets |
| Protect Support | Command-tagged piece | 1 / 2 | Prefer rear/support threats; rear armor buff |

**Pause 2 tactic switch surcharge:** +1 Authority when changing tactic.

### Demo abilities

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
| Synergy | 0–4 | `medic`, `command`, `echo`, `inspiring`, `stealth`, … |

Full 25-keyword encyclopedia UI is **post-demo**; expanded vocabulary exists in data for future mechanics.

### Adjacency synergies (fight start)

| Source | Neighbor filter | Bonus |
|--------|-----------------|-------|
| Medic | Primary `infantry` | +1 armor step |
| Command | Combat role `artillery` | +2 damage |
| Echo | Neighbor with **Stealth ability** | +1 damage |
| Inspiring | Any neighbor | +5% move charge |

### Critical mass (board-wide)

| Threshold | Bonus |
|-----------|-------|
| ≥3 Primary `infantry` | +2 damage all combatants |
| ≥2 Primary `vehicle` | +1 armor shred step |
| ≥2 Combat role `artillery` | +3 damage all combatants |
| ≥3 Combat role `assault` | +10% move charge all combatants |

### Planned UX (not shipped)

TFT-style synergy connection lines and trait panel (`Assets/Plans/synergy-visualization.md`).

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

### Layout (shipped)

| Property | Value |
|----------|-------|
| Baseline slots | 8 (4×2 grid) |
| Max slots | 12 (4×3 with board unlocks) |
| Slot kinds | Offensive / Defensive / Bonus (from `ShopSlotLayoutResolver`) |
| Reroll | **Single** reroll button (unified mode) |
| Freeze | One offer locked by **slot index** across rerolls |

**Note:** Lane enum (`Offensive` / `Defensive` / `Specialty`) still drives offer weighting and filters; legacy three-column lane UI is hidden in unified mode.

### Actions

- **Buy** — Supplies (+ Authority cost on some pieces).
- **Reroll** — scaling Supplies cost (one button).
- **Freeze** — lock icon per offer card.

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
| Radio Array | Enemy tag preview in shop tooltip |
| Field Workshop | Guaranteed building offer |
| Command Bunker | Bonus offensive-weighted slot (not extra baseline count) |

### Salvaged shop offers

Some offers appear as **Salvaged** (discounted/recycled pool) with distinct card badge.

---

## 11. Factions & content scope

### Playable (demo)

| Faction | ID | Display name | Hook |
|---------|-----|--------------|------|
| IronMarch Union | `iron_vanguard` | IronMarch Union | Command, heavy armor, default unlocked |
| Dust Scourge | `dust_scourge` | Dust Scourge | Gas, salvage bonus, +12 Manpower |
| Cartel of Echoes | `cartel_of_echoes` | Cartel of Echoes | Stealth/echo synergies, +3 Authority |

### Enemy pools (demo)

| Faction | ID | Theme |
|---------|-----|-------|
| Neutral Militia | `neutral` | Generic trench forces |
| Crimson Legion | `crimson_legion` | Heavy assault |
| Ash Wraiths | `ash_wraiths` | Gas, stealth ambush |

### Content counts (repo, June 2026)

| Category | Count | Notes |
|----------|-------|-------|
| Generated piece definitions | 38 | `DemoPieceFactory.CreateAll()` |
| Piece assets on disk | 46 | Includes legacy/extra variants |
| Synty sandbox art roster | 25 | `SandboxArtRoster` — full icon + arena coverage gate |
| Enemy templates | 10 | `fight_1` … `fight_10` |
| HQ pieces | 3 | `ironmarch_hq`, `dust_hq`, `echo_hq` |
| Demo abilities | 3 | Grenade / Shield / Cannon |
| Tactics | 4 | See §7 |
| Achievements | 10 | Local meta |

### Core neutral roster (shop highlights)

| Piece | Size | Ability |
|-------|------|---------|
| Conscript Rifleman | 1×1 | — |
| Grenade Thrower | 1×2 | Grenade Lob |
| Field Medic | 1×1 | — |
| Armored Transport | 2×3 L | Shield Allies |
| Mobile Cannon | 3×2 | Cannon Blast |

IronMarch roster adds tanks, walkers, mortars, engineers, snipers, and rear buildings (Supply Depot, Radio Array, Field Workshop, nests, artillery, etc.). Faction exclusives include Dust raiders/scrap rigs and Cartel phantoms/signal relays.

### Gauntlet (10 fights)

| Fights | Theme |
|--------|-------|
| 1–3 | Tutorial — conscript lines, teach pauses |
| 4–10 | Escalating mixed faction kits |

---

## 12. Meta progression

### Demo meta (shipped)

| Feature | Implementation |
|---------|----------------|
| Achievements | 10 entries, local JSON |
| Leaderboard | Top 100 by score, faction filter |
| Faction unlocks | Dust Scourge + Cartel on first victory |
| Storage | `%LOCALAPPDATA%/DeadManZone/deadmanzone_meta.json` |

### Steam

`SteamIntegration` stub present; SDK wiring post-demo.

### Post-demo intent

- Faction mastery tracks
- Cosmetic unlocks (banners, arena skins)
- Async PvP rating

---

## 13. Save & resume

Exit at any point; resume exactly where left off.

**Saved:** fight index, build vs combat phase, board + Reserves, shop offers (seed-locked), all four resources, mid-combat seed/tick/pause state, partial event log, faction, run seed.

**Behavior:** auto-save on key transitions; one active slot (demo); corrupt save → clear error + new run offer.

**Format:** JSON via Newtonsoft; **schema v7**; PvP-aligned structure. **No migration** from pre-2026 layouts.

---

## 14. Presentation & UX

### Scenes

| Scene | Role |
|-------|------|
| `MainMenu.unity` | Continue / New Run / Achievements / Leaderboard |
| `Run.unity` | Build phase (2D Canvas) |
| `CombatArena.unity` | Additive 3D combat sub-scene |
| `Diorama_Trench_PremiumShowcase.unity` | Art reference |

### Build phase (2D UI)

| Element | Behavior |
|---------|----------|
| HUD | Fight N/10, four resources, reroll cost, **matchup strength** |
| Main board | Center 9×10; zone coloring; drag-drop + rotation |
| Reserves | Bottom 2×9 grid |
| Shop | Unified offer grid (8–12 cards) |
| Unit detail | Fixed **UnitCardPanel** + hover routing; `UnitDetailCard.prefab` |
| Sell zone | Drag to sell; salvage feedback |
| Pause menu | Resume / Main Menu (auto-save) |

### Unit & shop cards (authored prefabs)

| Prefab | Path | Rules |
|--------|------|-------|
| Unit detail | `Assets/_Project/Presentation/UI/Prefabs/UnitDetailCard.prefab` | **Manual authoring only** — bake menus blocked |
| Shop offer | `Assets/_Project/Presentation/UI/Prefabs/ShopOfferCard.prefab` | **Manual authoring only** |

`PieceCardView` / `ShopOfferView` bind **text, preview, and state** at runtime. Layout resize and theme colors during **Play** are allowed on instances; changes **revert on Stop** unless the user explicitly **Applies** overrides to the prefab asset. `AuthoredCardPrefabGuard` blocks programmatic prefab overwrites.

### Combat phase (3D arena)

- Synty low-poly units/buildings; oblique camera (`CombatArenaCameraFramer`).
- `CombatArenaPresenter` replays deterministic event log.
- Army HP bars mirror pause thresholds.
- Tactic pause: full visual freeze + overlay panel.
- VFX/audio driven by ScriptableObject sets.

### UI theme tracks

| Kit | Status |
|-----|--------|
| **SyntyTrenchUiTheme** | Active default |
| **GrittyPostApocalyptic** | Imported pack + editor setup |
| **DeadManZoneCustom** | Art-only (StyleBible + components); not wired to `UiThemeSO` yet |

---

## 15. Art & 3D pipeline

### Visual direction

Grimdark retro-futurist WW1 trench — brass, mud, gas-lamp amber. Synty POLYGON readability at top-down/isometric camera.

| Layer | Source | Notes |
|-------|--------|-------|
| Combat units | SidekickCharacters + AnimationBaseLocomotion | Role prefabs, scale/tint variance |
| Vehicles | PolygonWar / PolygonMech | Static mesh v1 |
| Buildings | PolygonWar bunkers, nests, crates | Wrappers under `_Project/Art/Synty/` |
| Arena terrain | PolygonMaps woodland apocalypse | Ground + trench props |
| VFX | PolygonParticleFX | Gunshot, dust, explosion |
| UI | InterfaceMilitaryCombatHUD + custom kits | See §14 |
| Board/shop icons | Prefab snapshots → PNG | 256×256; 25-piece coverage gate |

### Synty rendering rules (mandatory)

- URP project-wide; **do not** batch-convert Synty materials to URP/Lit.
- Use native Synty shader graphs.
- Editor menu: **DeadManZone → Synty → Apply Full Synty Art Pass**.

---

## 16. Technical architecture

### Layer split

```
Presentation (Unity)
  BoardView, ShopView, UnitCardPanelView, CombatArenaPresenter, TacticPausePanel
        ↕
Game
  RunOrchestrator, RunManager, SaveManager, MetaProgressionService
        ↕
Core (pure C#, no Unity refs)
  Board, Reserves, Shop, TickCombatRun, Synergies, ArmyStrength, Save schema
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
| `DeadManZone.Presentation.Editor` | Scene setup, prefab guards, Synty pass |
| `DeadManZone.PlayMode.Tests` | Integration tests |

### Core modules (selected)

| Module | Role |
|--------|------|
| `BoardState` / `ReservesState` | Placement, zones, rotation |
| `BattlefieldState` | 23-wide combined grid |
| `ShopGenerator` / `ShopSlotLayoutResolver` | Offers, weighting, modifiers |
| `TickCombatRun` | Combat loop, checkpoints, event log |
| `ArmyHealthTracker` | Pause threshold detection |
| `CombatMovement` / `CombatDamageResolver` | Movement + damage + accuracy |
| `ArmyStrengthCalculator` | Build-phase strength snapshots |
| `SynergyEngine` / `CriticalMassRules` | Fight-start buffs |
| `GasDamageSystem` | Environmental finisher |
| `RunSaveSerializer` | Full snapshot (v7) |
| `MetaProgressionService` | Achievements, leaderboard |

### Async PvP (future)

Submit `{ seed, playerBoard, opponentBoard, commands[] }` → shared sim → identical event log → replay.

---

## 17. Data-driven design

### Content authoring model

All gameplay content ships as **ScriptableObjects** validated at import. Code defines **systems**; data defines **instances**.

| Asset type | Defines |
|------------|---------|
| `PieceDefinitionSO` | Shape, tags, stats, costs, ability, arena prefab, icon |
| `FactionSO` | HQ piece, starting resources, board size |
| `EnemyTemplateSO` | Pre-built enemy board per fight |
| `SynergyRuleSO` / catalogs | Adjacency pairs + bonuses |
| `CriticalMassRuleSO` / catalogs | Tag thresholds |
| `CombatPacingConfig` | Tick rate, pause thresholds, gas timing |
| `CombatAccuracyConfig` | Hit/graze/miss curves |
| `UiThemeSO` / `VisualProfileSO` | Presentation theming |
| `ContentDatabase` | Master index |

### Editor bootstrap menus

1. `DeadManZone → Generate Demo Content (5 Factions)`
2. `DeadManZone → Create Default UI Theme`
3. `DeadManZone → Setup Main Menu & Run Scenes`
4. `DeadManZone → Synty → Apply Full Synty Art Pass`
5. `DeadManZone → Rendering → Setup URP For Project`

**Disabled:** `DeadManZone → UI → Bake Card Prefabs` (manual prefab authoring).

---

## 18. Testing & quality bar

| Layer | Focus | Method |
|-------|-------|--------|
| Core sim | Placement, combat, synergies, gas, HQ, save round-trip | EditMode NUnit (~90+ classes) |
| Determinism | Same inputs → identical event log | Regression on content changes |
| Integration | Shop, board, arena, pause, save | PlayMode (16 suites) |
| Art coverage | 25 sandbox pieces: icon + arena prefab | `SandboxArtCoverageTests` |
| Mechanics sandbox | 7 end-to-end integration criteria | `MechanicsSandboxChecklistTests` |

### Demo ship criteria

- [ ] Full 10-fight run in ~30–40 minutes
- [ ] Four-resource tradeoffs understood without tutorial text wall
- [ ] Both pause windows used meaningfully
- [ ] Mid-combat save → identical outcome
- [ ] Synty-textured arena stable in Play Mode
- [ ] All EditMode + PlayMode tests pass

---

## 19. Implementation status

### Shipped (demo-playable)

| System | Status |
|--------|--------|
| 10-fight gauntlet + save/resume (v7) | ✅ |
| 3 playable factions + unlock flow | ✅ |
| 4-resource economy + manpower gate | ✅ |
| Board / Reserves / rotation / sell | ✅ |
| Unified shop grid + reroll + slot lock | ✅ |
| Tick combat + determinism | ✅ |
| HP-triggered pauses + tactics + 3 abilities | ✅ |
| Accuracy + range systems | ✅ |
| 3D arena replay + army HP bars | ✅ |
| Synergies + critical mass (4 rules) | ✅ |
| Army strength / matchup HUD | ✅ |
| Battle report + morale curve | ✅ |
| Achievements + local leaderboard | ✅ |
| Unit detail panel + hover routing | ✅ |
| Prefab asset protection (bake guard) | ✅ |

### In progress

| Item | Notes |
|------|-------|
| **DeadManZoneCustom UI kit** | PNG assets + StyleBible; `UiThemeSO` wiring pending |
| **GrittyPostApocalyptic UI** | Alternate theme assets imported |
| **Shop/unit card visual polish** | Prefabs manually authored; ongoing art pass |
| **Synergy visualization** | Design only |
| **Steam** | Stub only |
| **Non-sandbox piece icons** | Category tints until full art pass |

### Explicitly deferred (post-demo)

- Async PvP matchmaking UI
- 9 additional playable factions
- Branching campaign / event nodes
- Fog-of-war combat intro
- Full keyword encyclopedia UI
- HQ relocation, EMP/Incendiary effects
- Save migration from pre-2026 layouts

---

## 20. Roadmap scope tiers

| Tier | Scope | Target |
|------|-------|--------|
| **MVP / Demo** | 3 factions, 10 fights, Synty arena, local meta, authored UI cards | **Current** |
| **1.0** | Polish pass, Steam, fog-of-war intro, synergy viz, custom UI wired | Post-demo |
| **1.x** | Async PvP, 6+ factions | Competitive mode |
| **2.0** | 12 factions, branching campaign | Full roguelite |

---

## 21. Design decisions log

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Sim architecture | Pure C# core + Unity shell | Testable, PvP-ready |
| Build vs combat presentation | 2D UI build + 3D arena combat | Puzzle clarity + spectacle |
| Combat pacing (v3) | HP-triggered pauses + gas at tick 300 | Tension tracks actual fight state, not fixed segments |
| Battlefield width | 23 columns (5 neutral) | Tighter contested middle; tested in sim |
| Shop UX (v3) | Unified 8–12 slot grid | Cleaner layout; lanes remain in data |
| Win condition | No combatants or HQ destroyed | Clear pressure on HQ |
| Manpower | Hard gate + draft + shop relief | Attrition without per-unit HP tracking |
| Morale | Loss severity × fight index | Run arc wears down |
| Card prefabs | Manual authoring + bake guard | Prevents tooling from wiping designer layout |
| Runtime card mutation | Allowed in Play on instances | Reverts on Stop; only asset writes are blocked |
| Art stack | Synty POLYGON on URP | Cohesive style, subscription assets |
| Content | ScriptableObject-first | Designers ship pieces without code |
| Faction naming | **IronMarch Union** (`iron_vanguard`) | Single display name across GDD + demo guide |

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
**Range (Manhattan):** Melee 1, Short 3, Medium 5, Long 8  

**Accuracy defaults (before distance falloff):** Melee 92, Ballistic 78, Piercing 80, Explosive 72, Shredding 68, Sniper role 88, Artillery role 72+. Per-piece override optional.

**Accuracy outcomes:** Hit (100% damage) · Graze (33%, min 1) · Miss (0, cooldown spent).

## Appendix C — Code ↔ player-facing naming

| Player UI | Code / legacy field |
|-----------|---------------------|
| Supplies | `goldCost`, `GoldPrice` |
| Authority | `RequisitionPrice`, Requisition (combat UI) |
| IronMarch Union | `iron_vanguard` faction id |

---

*End of document — DeadManZone GDD v3.0*
