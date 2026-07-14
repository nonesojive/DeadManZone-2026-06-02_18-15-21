> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — Current Game Design Document

**Version:** 4.0 draft  
**Date:** 2026-07-03  
**Engine:** Unity 6 / URP  
**Current milestone:** IronMarch Union vertical slice  
**Document status:** New dated GDD, created beside the existing canonical GDD for review  
**Primary audience:** Solo developer, Cursor agents, future collaborators, playtesters

---

## 1. Executive Summary

**DeadManZone** is a grimdark retro-futurist trench-war roguelite autobattler. The player acts as a quartermaster-general: buy shaped pieces, arrange them across HQ and combat boards, commit to a fight, then watch a deterministic army simulation resolve with limited command interventions.

The current playable direction is narrower than the older broad multi-faction GDD. The active vertical slice focuses on one faction, the **IronMarch Union**, a 17-piece roster, a 10-fight gauntlet, four-resource economy pressure, and a **TopTroops2D** combat presentation built from side-view sprites, readable VFX, and deterministic replay.

### Current Product Target

| Attribute | Current Direction |
| --- | --- |
| Genre | Roguelite autobattler / spatial army builder |
| Core fantasy | Build and command a brutal industrial trench force |
| Current playable faction | IronMarch Union only |
| Run shape | 10-fight linear gauntlet |
| Target run length | 30-40 minutes for a full demo run |
| Build phase | Spatial board, shop grid, reserves, resource planning |
| Combat phase | Deterministic tick sim replayed in TopTroops2D arena |
| Player control in combat | Tactics and pause abilities only, no direct unit micro |
| Architecture priority | Data-driven ScriptableObject content, deterministic core sim, presentation replay |

### Current Scope Decision

DeadManZone should ship the next demo as a **focused IronMarch Union vertical slice**, not a broad content showcase. The goal is one complete, readable, replayable run with enough tactical and economic texture to prove the core loop.

---

## 2. Design Pillars

### 2.1 Spatial War Economy

The board is the game. Piece shape, zone placement, adjacency, HQ economy buildings, and combat unit footprint all compete for the same limited space. A good build is not just a stat stack; it is a layout that expresses priorities.

### 2.2 Economy Versus Firepower

Every shop phase asks whether the player invests in future capacity or present violence. Buildings increase supplies, manpower, authority, or critical-mass thresholds, but they occupy space that could hold combatants.

### 2.3 General, Not Sergeant

The player sets formation and doctrine. Individual units move, shoot, and die through the deterministic sim. Combat input happens only through tactics and limited pause abilities.

### 2.4 Readable Mass Combat

Combat must be readable before it is flashy. The player should understand which side is winning, why casualties happened, when pauses matter, and what the battle report teaches for the next shop.

### 2.5 Determinism First

The combat sim must remain replayable from seed, board state, tactics, and commands. Presentation is a view over the event log, not the source of truth.

### 2.6 Data-Driven Content

New pieces, faction baselines, abilities, tags, enemy templates, critical-mass rules, and combat art should be authored through ScriptableObjects and editor factories where possible.

---

## 3. Player Fantasy

The player commands the IronMarch Union: an industrial war machine of brass, steel, trench discipline, field surgery, armored transports, artillery, and phalanx-style infantry. The fantasy is not heroic squad tactics. It is cold logistical command under grinding attrition.

The player should feel:

- Smart when a cramped board arrangement creates a stronger army.
- Pressured when manpower, supplies, and morale cannot all be protected.
- Responsible when bad positioning causes preventable casualties.
- Rewarded when an ugly, efficient machine survives one more fight.

---

## 4. Core Loop

```text
Main Menu
  -> Select IronMarch Union
  -> Opening shop and board setup
  -> Repeat for 10 fights:
       Build Phase
         Buy / sell / reroll / freeze offers
         Place units and buildings
         Manage reserves
         Check manpower gate and strength preview
         Review critical mass and income preview
       Combat Phase
         Deterministic tick sim resolves
         Replay in CombatArena2D
         HP-triggered command pauses at 75% and 30%
         Player chooses tactics / abilities
       Aftermath
         Battle report
         Apply outcome-independent income
         Morale and casualties update
         Shop refreshes
  -> Victory after fight 10 or defeat when morale collapses
```

### Session Target

| Segment | Target Time |
| --- | --- |
| Opening setup | 2-4 minutes |
| Build phase after each fight | 1-3 minutes |
| Combat replay | 30-90 seconds depending on pause interaction |
| Full demo run | 30-40 minutes |

---

## 5. Game Modes And Milestone Scope

### Current Mode: IronMarch Vertical Slice

The current build should support:

- One selectable faction: **IronMarch Union**.
- One 17-piece shop roster made from IronMarch and neutral pieces.
- Ten escalating enemy fights using the same current roster.
- Full build-combat-aftermath loop.
- Save and resume across combat pauses.
- CombatArena2D as the primary combat presentation.

### Hidden / Future Modes

Dust Scourge and Cartel of Echoes remain future factions. They should stay hidden until their own content passes produce enough identity, roster depth, UI copy, and combat readability.

### Out Of Current Demo Scope

- Async PvP.
- Branching campaign maps.
- More playable factions.
- Steamworks production integration.
- Full procedural event system.
- Large-scale meta progression.
- Fully unique art for every future faction.

---

## 6. Resources And Economy

DeadManZone currently uses four resources: **Supplies**, **Manpower**, **Authority**, and **Morale**.

### 6.1 Resource Roles

| Resource | Role | Main Sources | Main Sinks |
| --- | --- | --- | --- |
| Supplies | Shop currency | Faction income, buildings, critical mass, sell refunds | Purchases, rerolls, relief items |
| Manpower | Deployment gate | Faction muster, recruitment buildings, board bonuses | Upkeep for deployed combatants |
| Authority | Combat command currency | HQ and command pieces | Tactics and pause abilities |
| Morale | Run health | Rare recovery effects | Defeats, severe losses, some future events |

### 6.2 Current IronMarch Baselines

| Field | Value |
| --- | --- |
| Starting supplies | 50 |
| Starting manpower | 15 |
| Starting authority | 2 |
| Base supplies per fight | +10 |
| Base manpower per shop | +1 |
| Base salvage chance | 1% |

### 6.3 Post-Combat Income Rule

Supplies and manpower income are currently **outcome-independent**. Winning, losing, or drawing does not change the base supply/manpower payout. This keeps the economy legible and avoids hidden rubber-banding.

Formula:

```text
Supplies gain = faction base + building bonuses + critical-mass income bonuses
Manpower gain = faction base muster + piece/building muster bonuses
```

### 6.4 Salvage Chance

The shop can offer pieces from the last enemy faction pool. The chance is:

```text
min(50%, faction base salvage chance + combat-board salvage boosts)
```

Only combat-board salvage boosts count. The HQ board does not increase salvage chance for the current vertical slice.

---

## 7. Build Phase

### 7.1 Board Model

The player manages multiple spatial regions:

- **Combat board:** Deployed fighting force.
- **HQ board:** Economic and command infrastructure.
- **Reserves:** Spatial storage for pieces not currently deployed.
- **Shop:** Unified offer grid.
- **Sell zone:** Converts unwanted pieces into partial refunds.

### 7.2 Core Build Actions

- Buy pieces from the shop.
- Place, move, and rotate pieces.
- Move pieces between combat board, HQ board, and reserves.
- Sell pieces for partial refunds.
- Reroll the shop.
- Freeze shop offers when needed.
- Inspect strength, income, salvage chance, and critical mass before fighting.

### 7.3 Manpower Gate

The player cannot start combat if deployed combatant upkeep exceeds available manpower. This must be communicated as a clear build problem, not a mysterious disabled button.

Required message pattern:

```text
Need X more Manpower. Sell pieces, move units to Reserves, or use Emergency Draft.
```

### 7.4 Emergency Draft

Emergency Draft is a once-per-run recovery valve for manpower shortfall. It should be visible only when relevant, clearly labeled as limited, and accompanied by direct feedback after use.

---

## 8. Shop Design

### Current Shop Goals

- Let the player assemble meaningful builds quickly.
- Keep early shops understandable.
- Avoid lane-specific complexity until the core loop is stable.
- Support reroll/freeze interactions without breaking slot identity.

### Current Shop Shape

| Feature | Current Direction |
| --- | --- |
| Offer layout | Unified 8-12 slot grid |
| Reroll | Single reroll action |
| Offer pool | Current 17-piece IronMarch/neutral roster |
| Salvage offers | Based on last enemy faction pool and board-derived salvage chance |
| Card detail | Manual shop/unit card prefabs with explicit ability text |

---

## 9. IronMarch Union Roster

The current vertical slice roster contains 17 pieces: 6 buildings and 11 units/structures.

### 9.1 Buildings

| Id | Display Name | Role |
| --- | --- | --- |
| `supply_depot` | Supply Depot | Supplies income |
| `field_hospital` | Field Hospital | Infantry HP support |
| `officer_quarters` | Officer Quarters | Authority scaling from command tags |
| `command_outpost` | Command Outpost | Flat authority support |
| `surgical_center` | Surgical Center | Infantry HP percent support |
| `recruitment_office` | Recruitment Office | Manpower income |

### 9.2 Units And Structures

| Id | Display Name | Combat Identity |
| --- | --- | --- |
| `field_medic` | Field Medic | Adjacent infantry support |
| `conscript_rifleman` | Conscript Rifleman | Basic neutral assault infantry |
| `armored_transport` | Armored Transport | Fast support vehicle |
| `ironmarch_surgeon` | IronMarch Surgeon | Medic scaling support |
| `bulwark_squad` | Bulwark Squad | Phalanx assault infantry |
| `enlisted_rifleman` | Enlisted Rifleman | Command-adjacent rifle infantry |
| `ironmarch_iron_horse` | IronMarch Iron Horse | Heavy vehicle / tank anchor |
| `ironclad_mortars` | Ironclad Mortars | Long-range artillery infantry |
| `ironclad_marksman` | Ironclad Marksman | Stealthy long-range sniper |
| `ironclad_field_marshal` | Ironclad Field Marshal | Command aura utility |
| `machine_gun_nest` | Machine Gun Nest | Immobile shredding structure |

### 9.3 Roster Design Intent

The roster should teach four build archetypes:

- **Infantry line:** riflemen, bulwarks, medics, and field marshal positioning.
- **Command engine:** command tags build authority and support stronger pause choices.
- **Industrial armor:** tanks and transports create durable, expensive pressure.
- **Fortified fire base:** mortars and machine guns trade mobility for range or sustained damage.

---

## 10. Tags, Abilities, And Critical Mass

### 10.1 Tags

Optional tags are flavor, UI chips, and critical-mass counters. They do not automatically grant combat behavior.

### 10.2 Abilities

Piece abilities are the mechanical source of truth. If a piece heals, grants HP, modifies movement, gives authority, or applies an aura, that effect should be authored as an explicit ability.

Ability categories:

- Adjacent auras.
- Fight-start passives.
- Pause abilities.
- Command actions.
- One-off bespoke piece rules.

### 10.3 Critical Mass

Critical Mass is a separate board-wide threshold system. It counts tags across the aggregate HQ + combat boards and grants the highest reached tier only.

UI direction:

- Collapsed right-edge tab during build.
- Expanded slide-over panel for active and near-miss rules.
- Bottom bar reserved for build feedback messages.

Design rule:

```text
Tags explain identity and threshold progress.
Abilities explain actual piece mechanics.
Critical Mass rewards army-wide commitment.
```

---

## 11. Combat Simulation

### 11.1 Core Combat Contract

The combat sim is pure C# logic. Unity presentation must not change the result.

```text
Board state + faction data + seed + tactics + commands
  -> TickCombatRun
  -> CombatEventLog
  -> CombatDirector replay
  -> CombatArena2D presentation
```

### 11.2 Combat Flow

- Units move according to numeric movement speed.
- Targeting selects valid enemies based on role, range, stealth, and tactical rules.
- Attacks resolve as hit, graze, or miss.
- Damage reduces HP.
- Destroyed units emit `destroyed` events.
- Anti-stall gas begins around global tick 300.
- Fight ends when one army is defeated, morale outcome resolves, or max duration/draw rules trigger.

### 11.3 Command Pauses

Combat has two HP-triggered pause thresholds:

- Pause 1: first army crosses 75% total HP.
- Pause 2: first army crosses 30% total HP.

At each pause, the player chooses from unlocked tactics and available abilities. The current IronMarch starting tactics are:

- Hold the Line.
- Advance.
- Disciplined Fire.

Protect Support remains locked until a future unlock path is designed.

### 11.4 Movement

Movement is authored as numeric speed 0-4:

| Speed | Meaning |
| --- | --- |
| 0 | Immobile |
| 1 | Very slow |
| 2 | Standard |
| 3 | Fast |
| 4 | Fastest |

This is easier to author and balance than named tiers while still keeping deterministic movement charge behavior.

---

## 12. Combat Presentation

### 12.1 Current Presentation Mode

The current target combat scene is **CombatArena2D**, a TopTroops-style hybrid:

- Orthographic camera.
- Side-view unit sprites.
- Player starts left, enemy starts right.
- Units face along the X axis.
- Square dirt-grid battlefield with zone tinting.
- Y-sort depth.
- Arced projectiles and readable impact VFX.
- Combat sim unchanged.

### 12.2 Unit Animation Direction

The current art pass is moving toward full sprite-sheet animation for key units, starting with **Bulwark Squad**.

Current Bulwark animation set:

- Idle.
- Walk.
- Run.
- Shoot.
- Die.

Hurt and hit-react are intentionally disabled in combat flow for now so damage does not interrupt readability or movement pacing. Death animation should finish before arena unload.

### 12.3 Presentation Priorities

Priority order for the combat presentation:

1. Every event is readable.
2. Deaths are visible and emotionally clear.
3. Movement does not pop, rewind, or slide during locked animations.
4. Combat timing remains synced enough to understand cause and effect.
5. Art can be swapped without combat code changes.

---

## 13. UI And UX

### 13.1 Build HUD

Top bar fields:

| Field | Meaning |
| --- | --- |
| Supplies | Current supplies |
| Supplies income | Projected next income |
| Manpower | Current manpower pool |
| Manpower income | Projected next muster |
| Authority | Available command currency |
| Authority income | Authority pool max for next combat |
| Salvage | Current salvage shop chance |
| Strength | Army strength / matchup preview |

### 13.2 Build Feedback

The bottom-center region is the **InfoMessageRegion**. It should handle:

- Reroll errors.
- Sell feedback.
- Manpower gate messages.
- Emergency Draft feedback.
- Invalid placement messages.

### 13.3 Critical Mass UI

Critical Mass should live in a right-edge tab/drawer, not the bottom bar. The collapsed state should show how many buffs are active. The expanded state should show active and near-miss threshold progress.

### 13.4 Battle Report

The battle report should teach the player what happened:

- Outcome.
- Morale loss or victory progress.
- Supplies/manpower gained.
- Casualties.
- Top damage dealt.
- Top damage taken.
- Clear continue path back to the shop or final victory/defeat.

---

## 14. Progression And Run Structure

### Current Run Progression

The demo uses a linear 10-fight gauntlet. Enemy templates should escalate by adding more durable units, better support, more ranged threat, and stronger IronMarch compositions.

### Victory

The player wins the run by clearing fight 10.

### Defeat

The player loses when morale reaches zero or the run state otherwise resolves as failed after combat.

### Meta Progression

Local achievements, leaderboard, and faction unlock scaffolding exist, but current design focus is the playable run, not meta expansion.

---

## 15. Content And Enemy Design

### Enemy Progression Goals

Enemy templates should:

- Teach basic combat first.
- Introduce medics/support before heavy armor.
- Introduce immobilized firebases before artillery-heavy fights.
- Use IronMarch elite compositions late.
- Keep pause 2 reachable in early tutorial fights often enough for the player to learn command pauses.

### Enemy Template Direction

| Fight Band | Composition Intent |
| --- | --- |
| 1-3 | Neutral infantry, field medic, simple machine gun or support lesson |
| 4-6 | Mixed neutral and IronMarch units, first serious armor/support pressure |
| 7-9 | Stronger synergies, artillery, command, and phalanx pressure |
| 10 | Capstone IronMarch army that tests economy, formation, and tactics |

---

## 16. Art Direction

### Tone

DeadManZone is industrial, grimy, and military. It should read as retro-futurist trench war, not clean hero fantasy.

### Palette

- Mud brown.
- Dull gunmetal.
- Brass.
- Worn olive.
- Faded red/cream markings.
- Dirty off-white medical accents.
- Gas-haze greens used sparingly.

### Combat 2D Direction

Combat sprites should prioritize:

- Strong silhouettes.
- Bottom-center pivots.
- Side-facing poses.
- Readability at mobile-like scale.
- Simple outlines or value separation.
- Reusable archetypes where schedule demands.

### Asset Production Strategy

Use dedicated combat sprites for showcase pieces first. Role silhouettes and shop icons remain acceptable fallbacks while gameplay is still being validated.

Priority showcase units:

- Bulwark Squad.
- Enlisted Rifleman.
- IronMarch Iron Horse.
- Ironclad Mortars.
- Machine Gun Nest.
- Ironclad Field Marshal.

---

## 17. Audio And Feedback

Combat needs a minimal but clear feedback set:

- Rifle shot.
- Cannon shot.
- Explosion.
- Impact.
- Death.
- UI confirmation.
- UI rejection.

Audio should be event-driven from replay events, not authored inside sim logic.

---

## 18. Technical Architecture

### 18.1 Layering

| Layer | Responsibility |
| --- | --- |
| Core | Deterministic combat, economy, board rules, targeting, pathing |
| Data | ScriptableObject definitions for factions, pieces, abilities, critical mass, enemy templates |
| Game | Run state, save/load, orchestration, phase transitions |
| Presentation | Unity scenes, UI, combat replay, VFX, audio |
| Editor | Factories, content generation, setup menus, art import helpers |

### 18.2 Design Rules

- Core systems must remain testable without Unity scene state.
- Presentation listens to events; it does not decide outcomes.
- ScriptableObject content should carry data, not hidden behavior.
- Use editor factories for repeatable content generation.
- Avoid broad refactors during vertical-slice stabilization unless they remove active risk.

### 18.3 Determinism Requirements

A saved combat state should replay to the same outcome when the same commands are submitted. Any future procedural generation must be seeded and stored.

### 18.4 Performance Requirements

Current target:

- 20-40 active combat units.
- Concurrent VFX without avoidable allocations.
- Stable 60 FPS target on desktop development machine.
- No repeated Sprite.Create churn in hot paths once final animation caching is stable.

---

## 19. Testing And Verification

### 19.1 Automated Tests

Required test coverage should include:

- Round income preview and post-combat income.
- Salvage chance formula and cap.
- Movement speed 0-4 mapping.
- Tactic gating by faction.
- Piece ability adjacency and per-tag counting.
- Critical Mass aggregate board evaluation.
- Combat segment playback.
- CombatArena2D load smoke tests.
- Sprite strip slicing and animation frame index tests.

### 19.2 Manual Playtest Gates

A feature is not complete until it survives Play mode validation. For the current vertical slice:

- New run starts with IronMarch Union only.
- Shop shows the 17-piece pool.
- Player can start fight 1 without external setup.
- Both combat pauses are reachable in tutorial fights.
- Bulwark Squad can idle, walk, shoot, and die visibly.
- Battle report appears after death presentation completes.
- Player can continue back to build phase.
- Save/resume preserves deterministic outcome.

### 19.3 Demo Completion Criteria

The demo is ready for broader playtest when:

- A new player can complete fights 1-3 without developer guidance.
- A full 10-fight run is possible in 30-40 minutes.
- The player understands manpower, supplies, authority, morale, and salvage chance.
- Combat deaths and victory/defeat states are visually clear.
- EditMode tests pass.
- A standalone build launches, starts a run, enters combat, and returns from battle report.

---

## 20. Current Milestone Plan

### Milestone 1 — Combat Presentation Stabilization

Goal: The TopTroops2D arena clearly shows movement, shooting, damage, death, and battle outcome.

Exit criteria:

- Bulwark Squad death animation plays fully.
- Arena does not unload before pending death presentations finish.
- No T-pose preview leaks into combat.
- No major sprite slicing errors.
- Combat replay remains deterministic.

### Milestone 2 — IronMarch Run Clarity

Goal: The build phase communicates the current economy and roster clearly.

Exit criteria:

- 17-piece pool is visible and understandable.
- Top HUD income and salvage labels update from board state.
- Critical Mass drawer shows active and near-miss rules.
- Manpower gate messaging is clear.
- Emergency Draft is wired and obvious.

### Milestone 3 — First Three Fights Playtest

Goal: A new player can play fights 1-3 and understand why they won or lost.

Exit criteria:

- Fight 1 teaches placement and basic combat.
- Fight 2 introduces support/positioning.
- Fight 3 introduces stronger pressure and command-pause value.
- Pause 2 is reached in most tutorial fight test seeds.
- Battle report teaches at least one useful lesson.

### Milestone 4 — Full 10-Fight Demo

Goal: One complete IronMarch run is playable, tense, and not dependent on developer explanation.

Exit criteria:

- 10 fights complete.
- Enemy scaling feels fair.
- Save/resume verified.
- Standalone build smoke test passes.
- Known issues list is short and user-facing.

---

## 21. Risk Register

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Combat presentation hides sim truth | Player cannot learn from losses | Keep replay tied to event log; prioritize readable deaths, damage, and pause timing |
| Economy is too flat due outcome-independent income | Runs may feel samey | Tune board-driven income bonuses and shop pressure before reintroducing outcome variance |
| IronMarch-only scope feels narrow | Demo may seem content-light | Make the 17-piece roster deep through placement, abilities, and enemy compositions |
| Critical Mass UI overloads new players | Build phase becomes noisy | Keep collapsed tab minimal; show detail only on demand |
| Art production expands too fast | Solo schedule slips | Use silhouettes/archetypes; finish showcase units first |
| Save/resume bugs undermine trust | Deterministic promise breaks | Treat save/replay tests as release gates |
| Combat file/presentation complexity grows | Future changes become slow | Keep Core, Game, Data, Presentation, and Editor boundaries explicit |

---

## 22. Design Decisions Log

| Date | Decision |
| --- | --- |
| 2026-06-21 | Tags are flavor/critical-mass counters; explicit abilities define mechanics |
| 2026-06-23 | CombatArena2D becomes the primary TopTroops-style presentation path |
| 2026-07-01 | IronMarch Union becomes the only selectable vertical-slice faction |
| 2026-07-01 | Roster is rebuilt around 17 pieces |
| 2026-07-01 | Post-combat supplies/manpower become outcome-independent |
| 2026-07-01 | Critical Mass moves to right-edge drawer UI |
| 2026-07-03 | Current GDD split into a new dated document beside the older canonical GDD |

---

## 23. Immediate Next Steps

1. Validate the current Bulwark Squad combat animation pass in Play mode.
2. Run focused EditMode tests for animation slicing, combat replay, income, salvage, and critical mass.
3. Play fights 1-3 as a fresh player and write down confusion points.
4. Fix only the issues that block the IronMarch vertical slice from being understandable.
5. After fight 1-3 are stable, tune enemy templates toward a full 10-fight run.

---

## 24. Companion Docs

- `docs/DeadManZone-Game-Design-Document.md` — older canonical broad GDD.
- `docs/demo-guide.md` — current setup and known issues.
- `docs/superpowers/specs/2026-07-01-ironmarch-union-content-pass-design.md` — current roster and faction content pass.
- `docs/superpowers/specs/2026-07-01-build-hud-economy-design.md` — round income and HUD direction.
- `docs/superpowers/specs/2026-07-01-critical-mass-panel-design.md` — current Critical Mass UI direction.
- `docs/superpowers/specs/2026-06-23-combat-arena-2d-design.md` — TopTroops2D combat presentation design.
- `docs/art/combat-arena-2d-art-brief.md` — combat art production guidance.

