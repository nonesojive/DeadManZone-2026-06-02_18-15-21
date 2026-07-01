# DeadManZone — Mechanics Sandbox Prototype Design

**Date:** 2026-06-12  
**Status:** Approved (brainstorming)  
**Goal:** Fully functional **mechanics sandbox** — every core system fires correctly in sim + UI. Art and roster stay thin; internal iteration and headless testing are the priority.

**Supersedes for planning purposes:** Demo completion roadmap priorities where they conflict (e.g. art-first milestones, “salvage done” claims in GDD).

---

## 1. Prototype goal & success criteria

### Goal

A systems sandbox where any mechanic can be tested in isolation with confidence. **Not** a shippable demo polish pass.

### Success criteria

| # | Criterion |
|---|-----------|
| 1 | Multi-cell pieces spawn in combat occupying **full footprint**; move as rigid shapes |
| 2 | Units path around allies, enemies, and buildings (A* on anchor positions) |
| 3 | Role profiles produce readable Top Troops-style behavior (Assault closes front, Artillery holds range, etc.) |
| 4 | Buildings spawn in combat, block paths, render in arena |
| 5 | Attack type × armor type matrix fully wired and test-covered |
| 6 | Adjacency synergies fire in combat (not just UI counts) |
| 7 | **Salvage system** offers pieces from the **last fight’s enemy faction** in shop; sell refunds are one sub-feature |
| 8 | Specialty lane shows correct offers based on board composition |
| 9 | Emergency Draft, Critical Mass, Tactics functional end-to-end |
| 10 | Tag Creator adds tags → Unit Creator picks them up automatically |
| 11 | UnitCard tooltips explain stats, tags, synergies, critical mass, salvage context |
| 12 | ~10 stat-complete neutral + ~15 IronMarch test pieces (placeholder art OK) |
| 13 | Headless sim tests pass; save/resume mid-combat produces **identical** outcome |

### Explicitly deferred

- Art polish beyond category tints  
- Steam / async PvP  
- 11 additional playable factions  
- Combat rotation during fight (v1)  
- EMP / Incendiary / full 25-keyword set  
- Tactic → movement speed modifiers  
- Full 10-fight balance pass (after sandbox green)

---

## 2. Combat architecture (full footprint + Top Troops movement)

### 2.1 Data model

**`CombatantState`** gains footprint awareness:

| Field | Purpose |
|-------|---------|
| `AnchorPosition` | Leader cell (replaces single `Position`) |
| `ShapeOffsets` | Relative cells from anchor, rotated at spawn |
| `OccupiedCells` | Computed anchor + offsets |
| `Facing` | Fixed at spawn from board rotation |
| `EngagementGoal` | Role-computed movement target |

**`CombatOccupancyGrid`** (new) replaces flat `HashSet<GridCoord>`:

- Maps every cell → `InstanceId`
- `CanPlace(combatant, anchorCandidate)` validates full footprint
- Rebuilt after each move

### 2.2 Movement pipeline (per tick)

1. **`RoleEngagement.ComputeGoal(combatant, battlefield)`** — role-specific target position  
2. **`ShapePathfinder.FindStep(...)`** — A* on anchor positions; each step validates all footprint cells; respects move-charge budget and neutral band 2× cost; soft lane bias ±1 Y from spawn column  
3. **Apply step** — translate anchor, recompute `OccupiedCells`, log deterministic move event  
4. **Spacing** — hold if goal occupied by friendly (Assault may override via critical mass charge bonus)

### 2.3 Multi-cell rules (locked)

- Full rigid footprint for **all** pieces (1×1 included — same as anchor-only)  
- **No rotation during combat** in v1  
- Destroyed pieces free all footprint cells immediately  

### 2.4 Buildings in combat

| Type | Sim | Arena |
|------|-----|-------|
| HQ | Immovable, full footprint, win condition | Static prefab |
| Combat buildings | Immovable, attacks with range/speed | Structure prefab |
| Utility buildings | Immovable, blocks paths, no attack | Small static prefab |
| `noncombatant` tag | Excluded from army HP bar | Grayed/static |

### 2.5 Extended role profiles

Extend `CombatRoleProfile` for **movement + targeting**:

| Role | Movement goal | Target priority |
|------|---------------|-----------------|
| Assault | Nearest front-line enemy column | Highest HP line troops |
| Infantry | Nearest enemy front | First in range |
| Artillery | Hold max range behind friendly front | Furthest enemy |
| Sniper | Rear support, LOS preferred | Lowest max HP, rear-biased |
| Support | Behind front, near allies | No attack |
| Cavalry | Flank via side columns (Y±2 bias) | Nearest rear (if any test piece needs it) |

### 2.6 Attack / armor matrix

Audit `AttackTypeProfileCatalog` + `CombatDamageResolver`. Every `AttackType` × `ArmorType` combo must have a defined multiplier. Headless matrix fixture required.

### 2.7 Combat delivery phases

| Phase | Deliverable |
|-------|-------------|
| 2a | `CombatOccupancyGrid` + footprint spawn |
| 2b | `ShapePathfinder` (A* on anchor) |
| 2c | Role engagement goals + lane bias |
| 2d | Buildings spawn, block, arena render |
| 2e | Attack/armor audit + matrix tests |
| 2f | Spacing + tuning pass |

---

## 3. Shop, salvage & economy

### 3.1 Salvage system (full rework — P0)

**Current state:** Only `SalvageCalculator.cs` exists — sell-refund math (50% Supplies, 50% Authority). Original demo spec explicitly deferred **“Salvage (enemy faction stock)”** and **“salvage shop.”** This is **not** the complete salvage system.

**Design intent (locked):** After each fight, the player may see pieces from the **faction they just fought** in the next shop refresh. Thematically this is scavenged gear and survivors absorbed into your army. All factions are eventually playable; enemy fights use the same piece rosters — salvage is how you **acquire another faction’s kit** mid-run.

**Long-term context:** Enemy boards will become **procedurally randomized** per fight. Salvage always keys off **`LastEnemyFactionId`** from the most recent combat, whether that board came from a fixed template or a generator.

#### 3.1.1 Concepts

| Concept | Description |
|---------|-------------|
| **`LastEnemyFactionId`** | Faction id of the opponent in the most recent fight (from `EnemyTemplateSO.enemyFactionId` today; from procedural board metadata later) |
| **Base salvage chance** | Per **player faction** — defined on `FactionSO` (e.g. IronMarch 10%, Dust Scourge 18%) |
| **Salvage boost** | Bonus % from **pieces on board** (buildings/units with salvage modifier tags or flags) |
| **Salvage chance** | Final % used per offer slot after aftermath calculation |
| **Salvage pool** | Lane-eligible pieces where `FactionId == LastEnemyFactionId` and `FactionId != playerFactionId` |
| **Normal pool** | Existing `ShopPoolFilter` neutral vs own-faction weighting — unchanged |
| **Salvage offer** | A shop slot that rolled salvage; piece comes from salvage pool; tagged for UI |

**Run start / fight 1:** No prior fight → `LastEnemyFactionId` unset → salvage chance effectively 0.

**Neutral enemy fights:** If `LastEnemyFactionId == "neutral"`, salvage pool is neutral pieces. Overlap with baseline neutral weight is acceptable; salvage slots still get the **“Salvaged”** badge and use the last-fight thematic (field pickups after militia engagement).

#### 3.1.2 Salvage chance (build-phase + shop)

Recalculated from **current boards during build** and persisted in `RunState.SalvageChancePercent` for shop generation. **Not** modified by combat outcome.

**Faction base:**

Each playable faction defines `baseSalvageChancePercent` on `FactionSO`. Examples for tuning (not final balance):

| Faction | Base salvage % (example) |
|---------|--------------------------|
| IronMarch Union | 10 |
| Dust Scourge | 18 |
| Cartel of Echoes | 12 |

**Piece boosts (combat board only):**

Pieces on the **combat board** add salvage chance via `ShopModifierFlags.SalvageChanceBoost5` (+5% each, stackable) or `salvageChanceBonus` on `PieceDefinition`. Evaluated live during build; synced on shop refresh and board edits.

**Formula (locked v3.1):**

```
SalvageChancePercent = min(50, faction.baseSalvageChancePercent + combatBoardSalvageBoost)
```

**Removed:** victory bonus (+10%), destroyed-enemy-type bonus, defeat ignoring board boosts.

HUD: `SalvageNumber` shows `{n}%`; optional legacy line `Salvage: {Faction} — {n}%` when `LastEnemyFactionId` is set.

See `docs/superpowers/specs/2026-07-01-build-hud-economy-design.md`.

#### 3.1.3 Shop generation flow

```
For each offer slot in lane:
  1. If LastEnemyFactionId is set AND rng roll < SalvageChance:
       pick from salvage pool (lane + fight-index filtered)
       mark offer as IsSalvaged = true
  2. Else:
       existing ShopPoolFilter.PickWeighted (neutral vs own faction)
```

Salvage pieces use normal pricing (no discount unless a building modifier applies). Player can buy and deploy them like any piece — cross-faction hybrids are intentional.

#### 3.1.4 Aftermath wiring

On fight end (`RunOrchestrator` aftermath):

1. Read `enemyFactionId` from current fight’s enemy template (or procedural metadata).  
2. Set `RunState.LastEnemyFactionId`.  
3. Compute / sync `RunState.SalvageChancePercent` via `SalvageChanceCalculator` (faction base + combat-board boost only).  
4. Persist in save schema.

#### 3.1.5 Sell refunds (sub-feature)

`SalvageCalculator` remains for **selling pieces** during build phase (50% Supplies, 50% Authority). Does **not** affect salvage shop chance. Dust Scourge +25% sell-refund bonus stays on refunds only; faction salvage aptitude is expressed via higher `baseSalvageChancePercent` on `FactionSO`.

#### 3.1.6 UI & tooltips

- Run HUD: “Salvage: Crimson Legion — 28%” (shows last enemy + current chance breakdown on hover)  
- Salvaged shop offers: **“Salvaged”** badge + faction chip  
- UnitCard: “Salvaged from your last battle against {FactionName}”  
- Tooltip on salvage HUD explains the mechanic for sandbox testers  

#### 3.1.7 Modules

| Module | Role |
|--------|------|
| `FactionSO.baseSalvageChancePercent` | Per-faction base salvage aptitude |
| `SalvageState` | `LastEnemyFactionId`, `SalvageChancePercent` on `RunState` + serialization |
| `SalvageChanceCalculator` | Outcome + board boosts → final chance |
| `SalvageBoardBoostAggregator` | Sum piece salvage bonuses from deployed board |
| `SalvageShopPool` | Filter registry by last enemy faction + lane + fight index |
| `ShopGenerator` | Per-slot salvage roll + `IsSalvaged` flag on `ShopOffer` |
| `ShopModifierFlags.SalvageChanceBoost5` | Piece-level +5% salvage boost (victory / draw) |
| `SalvageCalculator` | Sell refunds (unchanged scope) |

#### 3.1.8 Future: randomized enemy boards

Sandbox implements salvage against **fixed templates** (`EnemyTemplateSO.enemyFactionId` already exists). Procedural enemy generator (deferred) must output the same `enemyFactionId` field so salvage requires no rework.

#### 3.1.9 Tests

- Fight 1 shop: zero salvage offers with unset last enemy  
- After fight vs Crimson: salvage pool contains only Crimson pieces  
- **Defeat:** chance equals faction base only; piece boosts ignored  
- **Victory / Draw:** chance equals base + board boosts + victory bonus (fixture)  
- Salvage chance 0 → no salvage slots; 100% test hook → all slots salvage  
- Deterministic shop with fixed seed + salvage chance  
- Two pieces with +5% boost → +10% on victory/draw only  
- Save/resume preserves `LastEnemyFactionId` and chance  
- Salvage offer does not include player-faction pieces

### 3.2 Shop availability matrix

Extend fight-index pool gating (existing `ShopGenerator` + `ContentRegistry` pools):

| Fight range | Offensive | Defensive | Notes |
|-------------|-----------|-----------|-------|
| 1–2 | Basic infantry, 1×1 | Medic, supply buildings | Tutorial-soft |
| 3–5 | + vehicles, multi-cell | + utility buildings | Specialty unlocks |
| 6–8 | + artillery, assault | + combat buildings | Salvage from last enemy faction more valuable |
| 9–10 | Full faction pool | Full building pool | All synergies relevant |

Filters: `FactionId`, `ShopLane`, `fightIndex`, `combatRole`, `primaryTag`, `isNeutral`, `isSalvaged`.

### 3.3 Specialty lane rules

Replace `SpecialtyLaneRuleCatalog` stub with board-composition-driven offers:

**Unlock:** fight ≥ 3 OR Command/Engineer building on board (existing `SpecialtyLaneUnlock`).

**Context pool:**

| Board signal | Specialty bias |
|--------------|----------------|
| 2+ Infantry | Assault or Tank role pieces |
| Has Artillery | Support / spotter pieces |
| 2+ Buildings | Engineer / utility buildings |
| Has Vehicle | Vehicle upgrade or supply pieces |
| Default | Wildcard: any faction-eligible specialty tag |

3 offers per refresh; weight toward what the board is **missing**.

### 3.4 Emergency Draft

Wire `RunOrchestrator.TryEmergencyDraft()` to `EmergencyDraft.TryUse()` (core + tests exist; orchestrator currently returns `false`). UI button already in `RunSceneController`.

### 3.5 Muster supply synergy

Implement `MusterCalculator.CountSupplySynergyBonus()`: adjacent `supply`-tagged pieces → +1 Manpower per pair at fight start.

---

## 4. Tags, synergies & authoring tools

### 4.1 SynergyRuleCatalog (demo set)

Populate empty rules array. Uses existing `SynergyEngine` + `SynergyEffectDefinition`:

| Source tag | Neighbor | Effect |
|------------|----------|--------|
| `supply` | Adjacent any | +1 Muster (fight-start) |
| `medic` | Adjacent `infantry` | +1 Armor buff step |
| `command` | Adjacent `artillery` | +2 Damage bonus |
| `echo` | Adjacent `stealth` | +1 Damage bonus |
| `inspiring` | Adjacent friendly | +5% move charge |

### 4.2 Tag Creator (new editor)

**Menu:** DeadManZone → Tag Creator

- Fields: id, displayName, category, tooltip, displayPriority  
- Category: Primary | CombatRole | System | Faction | Synergy | Optional  
- Validation: unique id, category rules  
- Save: codegen into `TagRegistry.cs` (v1); ScriptableObject registry if tag count exceeds ~40  
- On save: refresh Unit Creator tag pickers  

### 4.3 Unit Creator updates

- Tag pickers from `TagRegistry.GetAll()` dynamically  
- Category conflict validation  
- Preview panel renders `PieceCardViewModel`  
- Combat role dropdown includes new CombatRole tags  

### 4.4 Critical mass

Keep demo rules. New tags add rules via `CriticalMassRuleCatalog` (subsection of Tag Creator or separate catalog editor later).

---

## 5. Tooltips, content & timeline

### 5.1 UnitCard tooltip sections

| Section | Source |
|---------|--------|
| Header | Name, faction chip |
| Stats | HP, DMG, Range, Atk Spd, Move Spd |
| Types | Attack/armor with catalog tooltips |
| Tag chips | Primary + CombatRole + up to 4 Optional |
| Synergy block | Live `SynergyEngine` eval on current board |
| Critical mass hint | Threshold diff from `CriticalMassRules` |
| Salvage context | “Salvaged from {last enemy faction}” when `IsSalvaged` |
| Ability | `GrantedAbility` name + description |

### 5.2 Minimal test content

**Neutral (~10):** one piece per mechanic — 1×1 rifleman, 1×2 grenadier, L transport, 3×2 cannon, medic, supply depot, field gun, Assault/Artillery/Sniper exemplars.

**IronMarch (~15):** existing pieces + heavy tank 2×2, mortar 1×2, engineer, assault breacher, HQ, 3 utility, 4 role variants.

Author via Unit Creator after Tag Creator lands.

### 5.3 Phased timeline

| Phase | Duration | Deliverables |
|-------|----------|--------------|
| **0 — Wire stubs** | 2–3 days | Emergency Draft, SynergyRuleCatalog, Muster synergy, specialty lane |
| **0b — Salvage system** | 3–4 days | Last-enemy faction state, per-slot salvage roll, UI badge, tests |
| **1 — Combat footprints** | 4–5 days | Occupancy grid, footprint spawn |
| **2 — Pathfinding** | 4–5 days | ShapePathfinder, building obstacles, determinism |
| **3 — Role engagement** | 3–4 days | Extended role profiles, lane bias, spacing |
| **4 — Buildings in arena** | 2–3 days | Prefabs, path blocking, render |
| **5 — Attack/armor audit** | 2 days | Matrix + damage fixtures |
| **6 — Tag Creator** | 3–4 days | Editor + Unit Creator sync |
| **7 — Tooltips** | 2–3 days | Synergy, critical mass, salvage context |
| **8 — Test content** | 3–4 days | Neutral + IronMarch pieces |
| **9 — Integration** | 3–4 days | Full sandbox checklist, save/resume gate |

**Estimate:** ~6–8 weeks solo (salvage rework adds ~1 week vs prior plan).

### 5.4 Testing strategy

- EditMode fixture per new subsystem before presentation  
- Determinism gate on every combat movement change  
- Role fixture per combat role  
- Synergy fixture per rule  
- Salvage fixture: last enemy faction + chance → salvaged offer faction  
- Sandbox smoke: dev menu test board → fight → verify all 13 criteria  

---

## 6. Architecture summary

| Layer | Decision |
|-------|----------|
| Prototype type | Mechanics sandbox |
| Combat feel | Top Troops: full footprint, A*, role engagement, lane bias |
| Multi-cell | Full rigid footprint; no combat rotation v1 |
| Buildings | Spawn, block paths, render in arena |
| Salvage | Faction base chance + piece boosts on victory/draw; defeat = base only; last enemy → shop pool |
| Shop | Availability matrix + composition specialty lane |
| Synergies | Demo rule set in combat + economy |
| Tooling | Tag Creator → Unit Creator live sync |
| Content | ~10 neutral + ~15 IronMarch test pieces |

---

## 7. Open tuning parameters (implementation plan)

These are locked structurally but values are data-tuned during sandbox pass:

- Per-faction `baseSalvageChancePercent` on `FactionSO`  
- Piece salvage boost values; victory bonus; destroyed-type bonus; 50% cap  
- Specialty lane wildcard weights  
- Role engagement distances and lane bias strength  
- Spacing hold vs charge override threshold  
