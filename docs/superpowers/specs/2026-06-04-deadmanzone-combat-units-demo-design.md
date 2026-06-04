# DeadManZone — Combat & Units Demo Design Spec

**Date:** 2026-06-04  
**Engine:** Unity 6  
**Status:** Approved (brainstorming)  
**Builds on:** `2026-06-03-deadmanzone-rework-design.md`  
**Scope:** Demo milestone — combat pacing, tactics/abilities pauses, unit stat model, 8-piece shop pool, HQ rules, battle report

---

## Summary

This spec defines the next playable combat slice for DeadManZone: retimed combat segments, a **Tactics + Abilities** pause system, core combat stat enums with rock-paper-scissors-lite damage, eight shop pieces (5 neutral + 3 Iron Vanguard), permanent faction HQ, and a standard battle report. Full keyword mechanics, salvage, fog-of-war intro, and multi-faction play remain deferred.

**Locked demo decisions:**

| Area | Choice |
|------|--------|
| Scope | Demo milestone only |
| Pauses | Full tactic rules + 3 abilities (Grenade Lob, Shield Allies, Cannon Blast) |
| Pacing | ~5s → Pause 1 → ~30s → Pause 2 → ~5s → gas until winner |
| Unit stats | Attack Speed, Range, Movement Speed, Armor Type, Attack Type + **tags = keywords** |
| Playable faction | Iron Vanguard (existing) |
| Shop pool | 5 neutral + 3 IV exclusives; IV weight ramps from fight 4 |
| Battle report | Outcome, resources, morale, top 3 dealt/taken |
| Damage math | Rock-paper-scissors lite |
| HQ | Faction-specific, auto-spawn, immovable, permanent for run |

---

## Section 1 — Combat flow & segments

### Transition into combat (demo)

Placeholder only: brief loading overlay (“Entering combat…”) while the battlefield is built. Fog-of-war gas reveal is documented as post-demo polish.

### Segment structure

Sim runs at **10 ticks per second** (presentation pacing). All durations live in `CombatPacingConfig` (tunable without code changes).

```
START (boards + seed locked)
  → Segment 1 — Opening (~50 ticks / ~5s): reposition, light fire
  → PAUSE 1 — tactic + optional abilities
  → Segment 2 — Main fight (~300 ticks / ~30s): full combat
  → PAUSE 2 — tactic + optional abilities (tactic switch surcharge)
  → Segment 3a — Brief push (~50 ticks / ~5s): no pause
  → Segment 3b — Gas ramp (no tick cap): environmental damage until win/loss
  → END — battle report
```

### Gas behavior

After Segment 3a, gas begins at low DPS and ramps each tick. Strongest in neutral columns; falloff in front zones (extends existing `GasDamageSystem`). Fight ends only when a win condition is met — no fixed gas duration.

### Win / loss / draw

- **Win:** Enemy has zero `Combatant`-tagged pieces **or** enemy HQ destroyed.
- **Loss:** Player combatants eliminated **or** player HQ destroyed.
- **Draw:** Both sides' last combatants die on the same tick (extremely rare). Treated as **win for morale** (no morale loss) but **~50% supplies** vs normal win reward.

### Auto-combat during segments

Units move cell-to-cell, acquire targets by **active tactic** and **attack range**, attack on cooldown modified by **attack speed**. Buildings use `MovementSpeed.None` unless data specifies otherwise. Deterministic tick sim → `CombatEventLog` → `CombatDirector` replay (unchanged architecture).

### Presentation (demo)

Top-down board view; simple attack flashes and damage numbers. No 3D models.

---

## Section 2 — Pause windows (tactics + abilities)

### Pause triggers

After Segment 1 (Opening) and Segment 2 (Main fight). No pause after Segment 3a or during gas.

### Pause UI

Single panel with two regions:

1. **Tactics** (required) — exactly **one** selected (radio).
2. **Abilities** (optional) — cards for each ability unlocked by a **living** source piece on the player board.

**Continue** enabled only when:

- Exactly one tactic is selected.
- Total Authority cost ≤ available Authority.
- Each selected ability has a valid **alive** source combatant on board.
- Invalid selections show inline reason; no soft-lock.

### Tactics (replaces stances)

Rename `StanceType` → `TacticType`. `CombatTargeting` reads active player tactic.

| Tactic | Availability | Authority (base) | Behavior |
|--------|--------------|------------------|----------|
| **Disciplined Fire** (HQ default) | Always while player HQ alive | 0 | Focus weakest HP enemy |
| **Advance** | Always | 0 | Aggressive push; prefer high-HP / front targets; forward movement bias |
| **Stand Ground** | Always | 0 | Hold position; prefer enemies in/nearest neutral column |
| **Protect Support** | Piece with `Command` tag on board (demo: Radio Array) | 1 | Prefer enemies threatening rear/support zone |

**Pause 2 tactic switch surcharge:** If selected tactic differs from tactic active entering this pause:

- **0-cost tactics** (Disciplined Fire, Advance, Stand Ground): **+1 Authority** switch fee.
- **Protect Support:** pay **base cost + 1** when switching **to** it from a different tactic.

Keeping the same tactic incurs no switch fee.

**Enemy tactics:** Fixed per fight template (mostly Disciplined Fire or Stand Ground for demo).

### Demo abilities (3 total)

Execute at **start of next segment** (first tick). Logged to `CombatEventLog`. Source piece must be **alive** at pause submission. Duplicate sources: **once per ability per pause**.

| Ability | Source piece | Pause 1 | Pause 2 | Effect |
|---------|--------------|---------|---------|--------|
| **Grenade Lob** | Grenade Thrower | 2 | 3 | 30 explosive damage in 2×2 area on valid enemy cell |
| **Shield Allies** | Armored Transport | 2 | 2 | Adjacent friendly infantry +1 armor tier for next segment |
| **Cannon Blast** | Mobile Cannon | — | 4 | 50 explosive to primary + 25 splash to adjacent enemies |

Multiple different abilities may be selected if Authority allows.

### Authority costs (default, tunable)

| Item | Pause 1 | Pause 2 |
|------|---------|---------|
| Protect Support | 1 | 2 (+ switch surcharge if applicable) |
| Grenade Lob | 2 | 3 |
| Shield Allies | 2 | 2 |
| Cannon Blast | — | 4 |
| Tactic switch fee (0-cost tactics) | — | +1 |

Authority deducts atomically on pause submission. Unspent Authority at fight end is lost (unchanged rework rule).

### Code direction

- `PhaseCommand` carries selected tactic and selected abilities.
- Split `CommandProcessor` validation into `TacticValidator` + `CombatAbilityExecutor`.
- Deprecate demo use of `SpendRequisitionBuff` / `CallStrike`.
- `PieceDefinition.GrantedAbility` enum: `None`, `GrenadeLob`, `ShieldAllies`, `CannonBlast`.
- Presentation: `TacticPausePanel` replaces stance-focused pause UI.

---

## Section 3 — Unit data model & combat stats

### Tags = keywords

One tag list per piece. Content authoring convention (editor warnings, not separate schema):

| Category | Rule | Examples |
|----------|------|----------|
| Primary | Exactly one | `infantry`, `vehicle`, `building`, `structure` |
| Combat role | Exactly one | `assault`, `tank`, `artillery`, `support`, `utility`, `sniper`, `harasser`, `defender`, `HQ` |
| Synergy | 1–4 additional | `scout`, `damage`, `engineer`, `small_arms`, … |
| System | Auto or explicit | `Combatant`, `HQ`, `Vanguard`, `Neutral`, `Command` |

Demo: tags drive shop filtering and **Protect Support** unlock (`Command`). Adjacency synergies and critical-mass rules deferred.

### New `PieceDefinition` fields

| Field | Type | Demo use |
|-------|------|----------|
| `AttackSpeed` | Slow / Medium / Fast | Cooldown multiplier |
| `AttackRange` | Short / Medium / Long | Max Manhattan target distance |
| `MovementSpeed` | None / Low / Medium / High | Move frequency |
| `ArmorType` | None / Light / Medium / Heavy | Damage reduction + RPS |
| `AttackType` | None / Ballistic / Explosive / Piercing | RPS modifiers |
| `GrantedAbility` | None / GrenadeLob / ShieldAllies / CannonBlast | Pause unlock |
| `FactionId` | string | `neutral` or `iron_vanguard` |

Existing fields unchanged: `MaxHp`, `BaseDamage`, `CooldownTicks`, shape, category, tags, costs, shop lane.

**Stat scale:** Demo uses compact integers (HP ~100–400, damage ~10–50 per hit) tuned during content authoring.

### Stat → sim mapping

**Attack speed (cooldown multiplier on `CooldownTicks`):**

| Tier | Multiplier |
|------|------------|
| Slow | ×1.5 (round up) |
| Medium | ×1.0 |
| Fast | ×0.75 (round down, min 1) |

**Attack range (Manhattan cells):**

| Tier | Range |
|------|-------|
| Short | 1 |
| Medium | 3 |
| Long | 6 |

No in-range target → move toward nearest valid target. Static units (`MovementSpeed.None`) only attack in range.

**Movement speed (move attempt every N ticks):**

| Tier | N |
|------|---|
| None | Never |
| Low | 3 |
| Medium | 2 |
| High | 1 |

### `CombatDamageResolver` (rock-paper-scissors lite)

Base damage = `BaseDamage` × segment scale × buffs, then armor and attack-type modifiers.

**Armor baseline (no special attack rule):**

| Armor | Damage taken |
|-------|--------------|
| None / Light | 100% |
| Medium | 85% |
| Heavy | 70% |

**Attack-type bonuses (multiplicative):**

| Attack type | Condition | Multiplier |
|-------------|-----------|------------|
| Ballistic | vs Light armor | ×1.25 |
| Explosive | vs Light or Medium, or target tag `building`/`structure` | ×1.30 |
| Piercing | vs Heavy armor | ×1.35 |

EMP, Incendiary, etc. present in data at ×1.0 until mechanics are added.

**Shield Allies:** Temporarily bumps adjacent `infantry` one armor tier for the next segment via runtime buff on `CombatantState`.

### Runtime tracking

`CombatantState` adds:

- `DamageDealtThisFight` / `DamageTakenThisFight` (battle report)
- Segment-scoped armor buffs (cleared on segment boundary)

---

## Section 4 — HQ rules

### Behavior

| Rule | Detail |
|------|--------|
| Faction-specific | Each faction references one HQ piece via `FactionSO.hqPieceId` (demo: `hq_command`) |
| Auto-spawn | On `StartNewRun`, placed at fixed `hqSpawnAnchor` + rotation on `FactionSO` |
| Immovable | Cannot relocate, rotate, or move to reserves for entire run |
| Not sellable | Cannot sell or remove during build phase |
| Not in shop | Never a shop offer |
| Combat | Static; destroyable; **instant loss** for owner at 0 HP |
| Default tactic | **Disciplined Fire** available while player HQ alive |
| Save/resume | HQ instance persisted; load validates HQ present at spawn |

**Out of demo:** Rare abilities that relocate or replace HQ (future); schema uses `HQ` tag + immovable enforcement so exceptions can be added without refactor.

### Iron Vanguard HQ — Command HQ (`hq_command`)

- 2×1 rear-zone building; tags `HQ`, `building`, role `HQ`
- HP ~200 (content pass); no attack
- Distinct presentation (tint/frame) indicating immovable core building

**Enemy HQ:** Same rules on enemy fight templates; predetermined placement in template data.

---

## Section 5 — Demo content roster & shop

### Shop pool (8 pieces — excludes HQ)

**Neutral (5):**

| Piece | Size | Key tags | Profile | Supplies | Ability |
|-------|------|----------|---------|----------|---------|
| Conscript Rifleman | 1×1 | infantry, assault, scout, small_arms | HP 120, DMG 18, Med/Med/Light/Ballistic | 50 | — |
| Grenade Thrower | 1×2 | infantry, artillery, assault, damage | HP 140, DMG 28, Slow/Med/Light/Explosive | 70 | Grenade Lob |
| Field Medic | 1×1 | infantry, support, utility, engineer | HP 130, DMG 8, Fast/Short/Light/Ballistic | 60 | — |
| Armored Transport | 2×3 | vehicle, tank, defender, heavy_arms | HP 350, DMG 20, Med/Short/Heavy/Explosive | 150 | Shield Allies |
| Mobile Cannon | 3×2 | vehicle, artillery, damage, heavy_arms | HP 300, DMG 40, Slow/Long/Med/Explosive | 180 | Cannon Blast |

**Iron Vanguard exclusives (3):**

| Piece | Asset | Size | Key tags | Profile | Supplies | Special |
|-------|-------|------|----------|---------|----------|---------|
| Rifle Squad | `rifle_squad` | 1×1 | infantry, assault, Vanguard, small_arms | HP 130, DMG 22, Med/Med/Light/Ballistic | 55 | Core IV infantry |
| Diesel Walker | `diesel_walker` | 2×2 | vehicle, tank, Vanguard, damage | HP 320, DMG 35, Slow/Med/Heavy/Piercing | 140 | IV bruiser |
| Radio Array | `radio_array` | 1×2 | building, utility, Vanguard, Command | HP 180, static | 90 | Unlocks Protect Support |

### Shop weighting by fight index

`ShopPoolFilter` in `ShopGenerator`:

| Fight | Neutral | Iron Vanguard |
|-------|---------|---------------|
| 1–3 | 85% | 15% |
| 4–6 | 55% | 45% |
| 7–10 | 25% | 75% |

Uniform random within lane-eligible pieces. Salvage (enemy faction stock) deferred.

### Enemy content

Fights 1–10 keep template structure; stat blocks migrate to new enums. Early fights skew neutral kit; later fights use recognizable units from the same vocabulary.

---

## Section 6 — Battle report & aftermath

### Battle report (demo)

| Block | Content |
|-------|---------|
| Outcome | Win / Loss / Draw |
| Casualties | Manpower refunded this fight |
| Rewards | Supplies (draw ~50% of win) |
| Morale | Delta this fight |
| Top damage dealt | Up to 3 friendly pieces |
| Top damage taken | Up to 3 friendly pieces |
| Continue | → shop build phase |

Deferred: top 5 lists, separate board-income screen, fog intro.

### Aftermath sim

- Manpower refund for surviving combatants
- Authority resets at next build round start
- Shop refresh with fight-index weighting
- Manpower gate before next fight
- Board persists including HQ at fixed anchor

---

## Section 7 — Architecture & testing

### Recommended approach

Extract focused Core modules; keep `TickCombatRun` as orchestrator:

| Module | Role |
|--------|------|
| `CombatPacingConfig` | Segment tick budgets |
| `CombatDamageResolver` | Armor + attack-type RPS |
| `TacticState` / `TacticType` | Replaces stance system |
| `CombatAbilityExecutor` | Three demo abilities |
| `BattleReportBuilder` | Fight stat aggregation |
| `ShopPoolFilter` | Fight-index weighting |
| `FactionSO` + `RunOrchestrator` | HQ spawn and immovable rules |

Presentation: `TacticPausePanel`, `BattleReportPresenter`, combat loading placeholder, HQ drag disabled.

### Testing

| Layer | Focus |
|-------|-------|
| Core | HQ spawn; move/sell rejected; tactic/ability validation; RPS damage; gas-until-win; draw; report top-3 |
| Integration | Headless fight with pauses; save mid-pause → identical resume |
| Play Mode | Pause gating; report UI; HQ non-draggable |

### Out of demo

Fog-of-war intro, 3D combat models, salvage shop, 11 factions, ammo on pieces, full 25-keyword mechanics, HQ relocation abilities, EMP/Incendiary combat effects, top-5 report lists.

---

## Design decisions log

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Implementation shape | Extract modules (Approach 2) | Keeps files focused; testable damage/abilities |
| Tags vs keywords | Same system | Avoid duplicate schema |
| Pause abilities | 3 offensive (Grenade/Shield/Cannon) | Hybrid scope; ties to neutral roster |
| Segment timing | 5s / 30s / 5s + gas until end | Matches player combat flow doc |
| HQ | Permanent, immovable, auto-spawn | Faction identity; strategic anchor |
| Shop size | 8 pieces + HQ | Enough variety without content overload |

---

## Success criteria (this slice)

- Player completes a fight using tactic selection at both pauses
- At least one demo ability usable when source piece on board
- Armor/attack type visibly changes damage outcomes in playtest
- HQ present at run start; cannot move or sell
- Battle report shows top 3 dealt/taken
- Save mid-pause → resume with identical outcome
- Deterministic replay unchanged for same seed + commands
