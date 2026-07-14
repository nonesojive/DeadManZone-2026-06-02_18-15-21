# DeadManZone — Game Design Document

**Status: AUTHORITATIVE. Supersedes every other design doc in this repo.**
Last verified against code: **2026-07-13** (commit `7f86adf9`, branch `master`).
Every number below was read out of the source and **independently fact-checked** against it.

> **Why this document exists.** The repo accumulated three competing GDDs and ~80 dated plan/spec
> files. Systems were renamed and replaced (Morale → per-unit; Manpower is run health; Gold →
> Supplies; 8×2 → 6×2 reserves; 6 → 5 shop slots; the Manpower **fielding gate was deleted**) but
> the old documents still described the old rules and kept getting picked up as current. **They are
> archived in `docs/archive/` and stamped SUPERSEDED.** If a number here disagrees with any other
> document, **this one is right** — source files are cited throughout.
>
> **If you change a rule, change this document in the same commit.**

**Still authoritative, NOT superseded:** `docs/adr/` and `docs/art/style-bible/`. This GDD cites
them rather than duplicating them.

---

## 1. The game in one paragraph

DeadManZone is a **single-player, run-based tactics autobattler** in a grimdark WW1-flavoured war.
You are a commander, not a soldier. In the **Build phase** you spend resources in a shop and place
pieces on two spatial grids — a **Combat board** (who fights) and an **HQ board** (what your war
machine produces). Adjacency buffs neighbours, and **tag thresholds** ("Critical Mass") light up
army-wide bonuses. You then choose one of three **fronts** and press Begin Combat, and the fight
**resolves itself** on a deterministic tick simulation you cannot micro — you get a small number of
**tactical pauses** to issue orders. Winning earns **Dread**, the run clock. Enough Dread summons a
**Boss**. Beat three bosses to win. Run out of **Manpower** and the run is over.

**Core fantasy:** the decisions that matter happen *before* the shooting starts — and then you have
to watch.

```
BUILD  →  choose a front  →  COMBAT (auto, with tactical pauses)  →  AFTERMATH  →  BUILD …
  ↑                                                                                   │
  └───────────────────────── Dread accrues; at a threshold the next fight is a BOSS ──┘
```

---

## 2. Run structure

| | |
|---|---|
| **Phases** | `Build → Combat → Aftermath → (Build \| Victory \| Defeat)` (`Core/Run/RunPhase.cs`) |
| **Victory** | Defeat **3 bosses** (`RunOrchestrator.cs:739-741`) |
| **Defeat** | **Manpower reaches 0** (`RunOrchestrator.cs:689, 762`) |
| **Run length** | Not a fixed fight count. The run ends on the third boss, or on death. |
| **Determinism** | Everything seeded off `RunState.RunSeed` via named sub-streams (`Core/Common/SeedStreams.cs`). |

> `RunOrchestrator.MaxFights = 10` is **unused at runtime** — but it *is* referenced by
> `VerticalSliceRegressionTests`, `RunOrchestratorTests` and `DreadBossRunTests`. **Deleting it
> breaks the suite.** It is not a design constraint; do not design against it.

---

## 3. Economy — three currencies

### Supplies — the buying currency
Spent on shop offers and rerolls. Running out means you can't buy; it does not kill you.

- Income per Build: `faction.baseSuppliesPerRound + board bonuses` (`RoundIncomeCalculator`).
- `supply_depot` → **+5 flat each** (`BuildingIncomeRules.cs:16-17`), plus Critical-Mass Supplies
  rules (flat + percent).

### Manpower — the run health bar
**The only resource that can end your run.**

> **THERE IS NO FIELDING COST.** Since M5 / ADR-0005 the Manpower gate is **gone** —
> `CanStartBattle` unconditionally returns true (`RunOrchestrator.cs:146-152`). You can always
> march, however thin you are. Manpower is debited **only by casualties**.
> *(`ManpowerCalculator.ComputeFieldingRequirement`/`ComputeUpkeep` survive solely to size the
> Emergency Draft — `RunOrchestrator.cs:539-544`. Do not read them as a cost.)*

- **Casualties** (`ManpowerCalculator.ComputeCasualties`, debited `RunOrchestrator.cs:645,649`):
  - **Died** → costs its full `ManpowerCost`.
  - **Survived but damaged** → partial bodies: `DamageTaken / (MaxHp / ManpowerCost)`, capped at
    `ManpowerCost`.
  - **Routed (`IsBroken`)** → **costs nothing.** *Routing is how you save lives.*
- **Income (Muster):** `faction.baseMusterPerShop + Σ piece.MusterPerShop + supply-synergy pairs`
  (`MusterCalculator`). Each adjacent pair of `supplier`/`supply_line` pieces = **+2**.
- **Emergency Draft:** once per run, covers a shortfall (`EmergencyDraft.TryUse`).

### Authority — the command currency
A **per-round pool**, not a bank.

- Pool = **2 base** (`AuthorityCalculator.cs:8`) + buildings: `command_outpost` **+1**;
  `officer_quarters` **+1 per `command`-tagged piece on your boards**.
- Spent on: the **Easy** front (2), **each offer lock past the first** (1), and **tactical orders**
  mid-fight. Critical-Mass `command` also grants flat Authority.

---

## 4. Dread — the run clock (ADR-0004)

*(`Core/Run/DreadRules.cs` — single source of truth)*

**Dread is earned ONLY by winning.** Losses and boss wins grant none (`RunOrchestrator.cs:736-757`).

| Front won | Dread |
|---|---|
| Easy | **+1** |
| Normal | **+2** |
| Hard | **+3** |

- **Boss thresholds: `6, 12, 18`.** At the threshold the next fight is a **Boss** — no front choice.
- **3 bosses → Victory.**
- **`FightEquivalent(dread) = dread / 2 + 1`** — the difficulty yardstick. It drives **enemy army
  templates, shop rarity odds AND shop prices**. Not a fight counter.

> **Dread is the central tuning dial.** Difficulty, pacing, shop quality, shop cost and the boss
> timer all hang off one number the player advances by choosing fronts. Farm Easy fronts and you
> stay in a low band longer *and* keep prices/odds low; take Hard fronts and you accelerate into the
> bosses, the better loot, and the higher bills. **Do not add a second clock.**

---

## 5. The boards

| Board | Size | Holds |
|---|---|---|
| **Combat** | **6×6** | `infantry`, `vehicle`, `structure` |
| **HQ** | **3×6** | `building` (economy only; never fights) |
| **Reserves** | **6×2** | bench — not fielded |

- Board is chosen by the **`primary` tag**, not the category (`BoardPlacementRules`).
  > **Gotcha:** `machine_gun_nest` is `PieceCategory.Unit` with `primary: structure` → it goes on
  > the **Combat** board. Only `primary: building` goes to HQ.
- Pieces are **polyominoes** with rotation — packing is a real spatial puzzle.
- **Adjacency is a mechanic** (`BoardAdjacency.GetTouchingPairs`).

### The battlefield is NOT the build board
Combat is fought on a **battlefield** built from your 6-wide half + **neutral columns** + a mirrored
enemy half (`Core/Board/BattlefieldLayout.cs`, `CombatBattlefieldConfig.NeutralColumnCount`). Your
board is your **formation**, which is then projected into that arena. This is what "200 charge
through neutral ground" (§10) refers to.

---

## 6. The shop

- **5 live offer slots** (`ShopSlotLayoutResolver.VisibleOfferSlotCount = 5`).
- **4 dormant/reserved slots** (indices **5–8**, all `ReservedAbility`), **not wired to anything** —
  no ability currently unlocks one (`ShopSlotUnlockRegistry.Empty` is always used;
  `RunOrchestrator.cs:41`). Plus **4 Bonus slots (9–12)** that are likewise never active.
  *(The ShopV2 band authors 3 dormant slots visually — a UI/data mismatch worth reconciling.)*
- Slots 0–2 **offensive**-biased, 3–4 **defensive**-biased (`slot_0..slot_4.asset`).
- **Reroll cost:** `1 + rerolls this round` — climbs within a round, resets between rounds.
- **Lock:** right-click to keep an offer through a reroll. **First lock free; each extra lock costs
  1 Authority** (`max(0, locks - 1)`).
- **Sell (Smelter):** **50%** of Supplies cost + **50%** of Authority cost; **0% Manpower**
  (`SalvageCalculator`). Dust Scourge: **×1.25** Supplies.
  > Trap: `SalvageCalculator.ManpowerRefundRatio = 0.25f` is **declared but never used** —
  > manpower is hardcoded to 0. The constant lies.

### How an offer is rolled (order matters)
1. **Source roll** per slot: **neutral 10% / faction 80% / salvage 10%** (`ShopSlotProfileSO`), with
   a fallback chain. **No duplicate piece within a batch** (`ShopGenerator.cs:183,224`).
2. **Rarity roll** — table below.
3. **Price** = base cost **+ `max(0, FightEquivalent - 1)`** (`ShopGenerator.cs:360`).
   **Shop prices inflate with the Dread clock.**

### Rarity odds — keyed on `FightEquivalent`, not fight count

| Fight-equivalent | Common | Uncommon | Rare |
|---|---|---|---|
| 1–2 | 80% | 18% | 2% |
| 3–4 | 74% | 22% | 4% |
| 5–6 | 68% | 25% | 7% |
| 7–8 | 62% | 28% | 10% |
| 9+ | 55% | 30% | 15% |

- **Pity (appear-reset):** a batch showing **no** rare-or-above adds **+4%**; a batch showing one
  **resets**. Cap **40%**. A rare is **forced** after **9** dry batches **or as soon as the odds hit
  the 40% cap** (`RarityWeights.ForcesRare`). Counts what is *shown*, locked slots included — so it
  is state-derived and seeded runs stay identical.

### Salvage
- Chance = `faction.baseSalvageChancePercent + board boost`, **capped at 50%**.
- Scaled by **kill share** = `kills / (kills + routs)`. **Routing an enemy denies you its salvage** —
  a deliberate tension with routing *your own* units to save Manpower.
- **A Hard-front win upweights the next round's salvage picks** (Rare ×3 / Uncommon ×2 / Common ×1)
  (`RunOrchestrator.cs:643`).

### Shop modifiers (live)
`ShopGenerator.ComputeModifiers` — board-driven: **Gold discount** (stacking, **capped 25%**),
**ExtraGeneralSlot**, **EnemyTagPreview**, **GuaranteeEngineerOffer** (injects a defensive/building
offer if none rolled).

---

## 7. Choosing the fight — the Front Report

Three fronts per Build, **one of each tier** (tier = slot index):

| Tier | Costs | A win gives | Battle Condition |
|---|---|---|---|
| **Easy** | **2 Authority** | +1 Dread | none |
| **Normal** | free | +2 Dread | none |
| **Hard** | free | +3 Dread, **+15 Supplies, +6 Manpower** | **always one** |

- Armies drawn from templates within **±1** `FightNumber` of your `FightEquivalent`.
- Each front shows a **strength preview** and an **arena theme**.
- **COMBAT is gated on choosing a front** — `BeginCombat` throws otherwise.

> **The Easy front's hidden discount:** taking Easy **suppresses the enemy's fight-start engines
> entirely** — no enemy synergies, no enemy Critical Mass (`TickCombatRun.cs:83-92`,
> `suppressEnemyFightStartEngines`). It is far softer than "+1 Dread instead of +2" implies. This is
> a major, currently-undocumented difficulty lever.

### Battle Conditions (Hard only) — *consent, not gotcha*
Shown **before** you commit (`Core/Combat/ConditionCatalog.cs`):

| Id | Effect |
|---|---|
| `entrenched_foe` | Enemy **front rank** (column nearest you): **+1 armor step** |
| `veteran_cadre` | Every enemy **behind** the front rank: **+25% HP** |
| `storm_barrage` | **Your** units start at **−15% HP** (min 1) |
| `iron_resolve` | Every enemy: **+1 damage** |

---

## 8. Bosses

Three, fixed roster, seeded order. A boss replaces the front choice.

| Boss | Faction | Twist |
|---|---|---|
| Militia Warden | neutral | `endless_muster` — all enemies **+30% HP** |
| Crimson Marshal | crimson_legion | `iron_discipline` — all enemies **+1 armor step** |
| Wraith Harbinger | ash_wraiths | `deathless_cold` — enemy **front rank +60% HP** |

- Each has **3 stage loadouts** escalating with `BossesDefeated` — the same boss is harder later.
- Twists and Battle Conditions share one `ICombatRuleModifier` seam, so a save stores one id.

---

## 9. Tags, synergies and Critical Mass

Tags are categorised: **primary** (infantry/vehicle/structure/building), **combat role**, **attack
type**, **synergy**, **ability**, **flavor**.

### Adjacency synergies (local)
Authored **per piece** as `customAbilities` (neighbour filter → stat → mod type → magnitude) and
resolved by **`PieceAbilityEngine`**.

> **`SynergyTraitRegistry` is DESCRIPTION-ONLY and referenced by nothing.** Its blurbs are stale
> (e.g. it says phalanx buffs "per adjacent infantry"; the real `bulwark_squad` ability filters on
> `SynergyTagId: phalanx` — i.e. **per adjacent phalanx**). Read the piece assets, not the registry.

### Critical Mass (army-wide)
`CriticalMassDefaultRules.Build()` — **30 rules**. Count a tag, cross a threshold, get a **tiered**
bonus applied to a **filtered target set**.

> **Scope trap.** At fight start, Critical Mass counts the **COMBAT BOARD ONLY**
> (`TickCombatRun.cs:79` — `Evaluate(playerBoard)`). **An HQ building cannot tip a combat
> threshold.** Only the two *run-resource* rules (`command`, `supplier`) evaluate across **both**
> boards, because they pay out in economy, not combat.

Representative rules:

| Rule | Counts | Tiers (threshold → magnitude) | Effect on |
|---|---|---|---|
| `infantry` | primary | 5→+10, 7→+15, 10→+20 | Max HP, infantry |
| `structure` | primary | 3→+15, 5→+25, 7→+40 | Max HP, structures |
| `assault` | role | 5→+1, 7→+2, 10→+3 | Damage, infantry |
| `artillery` | role | 3→+1, 5→+2, 7→+3 | Attack-speed **tier step**, explosive units |
| `support` | role | 3→+1, 5→+2, 7→+3 | Attack-speed tier step, infantry+vehicles |
| `command` | run | 2→+1, 4→+3, 6→+6, 8→+10 | **Authority** *(both boards)* |
| `supplier` | run | 2→+20, 4→+45, 6→+70, 8→+100 | **Supplies** *(both boards)* |
| `ballistic` | attack type | 5→+5%, 7→+10%, 10→+15% | Damage % |
| `ironmarch_union` | faction | 5→+1, 7→+2, 10→+3 | **Damage** (flat), infantry |

> The faction rule counts `definition.FactionId` — and **7 of the 17 pieces are `neutral`, not
> `ironmarch_union`** (§11). Neutral pieces do **not** count toward it.

**Design intent:** the shop sells *pieces*, but you are buying **counts**. The real question is
rarely "is this unit good" — it's "does this tip a threshold".

---

## 10. Combat — the tick simulation

*(`Core/Combat/` — deterministic, headless, Unity-free.)*

- **10 ticks/sec.** Hard cap **10,000 ticks**. Combat is **automatic**.

### Tactical pauses — the only in-fight agency
- An **opening pause**, plus one when **either army** drops to **60% health**.
  > It fires on the **lower of the two armies'** HP fraction (`TickCombatRun.cs:233-244`) — so the
  > **enemy** collapsing to 60% also triggers your pause.
- Orders cost **Authority**:

| Tactic | Effect |
|---|---|
| `DisciplinedFire` | **+1 damage** to all units |
| `Advance` | movement charge **×1.10** |
| `StandGround` | movement charge **×0.90** |
| `ProtectSupport` | rear-column units **+2 armor steps** — **⚠ CURRENTLY A NO-OP** |

> **⚠ `ProtectSupport` does nothing.** It buffs cells whose zone is `ZoneType.Rear`, but the live
> combat board is always built **unzoned** (every cell is `ZoneType.Support`). No unit can ever
> qualify. This is a **bug**, not a design choice — see §15.

### Anti-stall gas
From **tick 300** (~30s) gas ramps and damages every active unit by position
(`CombatPacingConfig.GasStartTick`, `GasDamageSystem`). **Fights degrade; they do not stalemate.**

### Damage
```
damage = (BaseDamage + flatBonus) × damageScale
       × armorMultiplier(defenderArmor)   // None/Light 1.00 · Medium 0.85 · Heavy 0.70
       × attackTypeMultiplier(...)
       × (1 + damagePercentBonus/100)
minimum 1
```

**Attack-type triangle** (`AttackTypeProfileCatalog`; weak multiplier defaults **0.85**):

| Attack type | Strong vs | × | Weak vs |
|---|---|---|---|
| Ballistic | Medium armor | 1.25 | Heavy |
| Piercing | Heavy armor | 1.35 | Light |
| Shredding | Light armor | 1.25 | Medium |
| Explosive | Heavy armor **+ structures AND buildings** | 1.30 | — |
| Fire | Light armor | 1.20 | Heavy |
| Melee | Light armor | 1.25 | Heavy (**×0.80**, not the 0.85 default) |
| Gas | **Infantry primary** (not armor-keyed) | 1.25 | Buildings / structures |

### Speed, range, movement, accuracy
- **Attack speed** → cooldown: Slow ×1.5, Medium ×1.0, Fast ×0.75.
- **Range** (Chebyshev): Melee 1, Short 3, Medium 5, Long 8.
- **Movement:** charge accrues `movementSpeed + 1`/tick; a step costs **100** (**200** through
  neutral ground — see §5).
- **Accuracy:** Melee 92, Piercing 80, Gas 75, Explosive 72, Shredding 68, default 78.
  Snipers 88; Artillery floor 72.

### Morale and rout (ADR-0005)
- Every unit has its own **Morale**. At **0 it BREAKS** — routs, stops being `IsActive`, leaves.
- **Death shock:** a unit dying deals **6 morale damage** to every ally within **2 cells**. Losses
  cascade.
- **Terror damage** is morale damage on hit (e.g. `machine_gun_nest`).
- **Routed units cost no Manpower.** Dead ones cost full. Morale is the difference between a
  bloodied army and a dead run.

### Win / loss / draw
- A side loses when it has no `IsActive` fighters with `MaxHp > 0` — **all dead *or* all routed**.
- **Mutual wipe → counted as a player WIN.**
- **Timeout at 10,000 ticks → draw, and `PlayerWon = false`** (`TickCombatRun.cs:270-276`).
  **A timeout is NOT a win.** These two draws resolve oppositely — deliberate, but a sharp edge.

---

## 11. Content — IronMarch Union

**One playable faction right now** (IronMarch Union); **more are planned in the next pass or two.**
`neutral`, `crimson_legion`, `ash_wraiths` are enemy pools; `dust_scourge` and `cartel_of_echoes`
have faction assets and identity hooks (Dust Scourge's ×1.25 salvage refund) but **no roster yet**.

**17 pieces total** — but only **10 carry `factionId: ironmarch_union`**. The other **7**
(`field_medic`, `machine_gun_nest`, `armored_transport`, `supply_depot`, `field_hospital`,
`recruitment_office`, `surgical_center`) are **`factionId: neutral`** — a **shared pool**. This
matters: they do **not** count toward the `ironmarch_union` Critical-Mass rule.

**Faction baseline** (`ironmarch_union.asset`): Supplies **50**, Manpower **15**, Authority **2**,
Supplies/round **10**, Muster/shop **1**, base salvage **1%**, Combat **6×6**, HQ **3×6**.
**Starting board:** `supply_depot`, `command_outpost` (HQ); `field_medic`, `conscript_rifleman`
(Combat).

| Piece | Faction | Primary | Role | Rar | S | M | HP | Dmg | Notes |
|---|---|---|---|---|---|---|---|---|---|
| conscript_rifleman | IM | infantry | assault | C | 12 | 1 | 50 | 5 | |
| enlisted_rifleman | IM | infantry | assault | C | 15 | 1 | 55 | 6 | +1 atk-speed tier per adjacent Command |
| bulwark_squad | IM | infantry | assault | U | 18 | 1 | 55 | 3 | phalanx (per adjacent **phalanx**) |
| ironmarch_surgeon | IM | infantry | support | U | 15 | 1 | 40 | 3 | |
| ironclad_marksman | IM | infantry | sniper | U | 20 | 2 | 35 | 6 | **stealth until the 2nd tactics window** |
| ironclad_mortars | IM | infantry | artillery | R | 20 | 3 | 25 | 8 | **Mortar Shot** (area, pause 0) |
| ironclad_field_marshal | IM | infantry | utility | R | 30 | 2 | 50 | 3 | |
| ironmarch_iron_horse | IM | vehicle | tank | R | 24 | 4 | 75 | 6 | |
| officer_quarters | IM | building | utility | U | 25 | 0 | 45 | — | **+1 Authority per Command piece** |
| command_outpost | IM | building | support | C | 15 | 0 | 40 | — | **+1 Authority/round** |
| field_medic | neutral | infantry | support | U | 10 | 1 | 30 | 3 | adjacency HP buff |
| armored_transport | neutral | vehicle | defender | R | 18 | 3 | 75 | 2 | **Shield Allies** at pause |
| machine_gun_nest | neutral | structure | utility | R | 20 | 2 | 100 | 2 | terror; **Combat** board |
| supply_depot | neutral | building | utility | U | 15 | 0 | 50 | — | **+5 Supplies/round** |
| recruitment_office | neutral | building | utility | C | 15 | 0 | 35 | — | Muster |
| field_hospital | neutral | building | support | U | 20 | 0 | 60 | — | |
| surgical_center | neutral | building | support | U | 20 | 0 | 35 | — | |

**Granted abilities:** `MortarShot` (area, pause 0), `ShieldAllies` (protect allies at pause),
`CannonBlast` (heavy blast, pause 1 — **defined but unused by any piece**).

---

## 12. Save

- Schema **v10**; minimum supported **v8**. Older saves are **rejected**, not migrated.
- Additive only. Removed members (run-level Morale in v10; `PlayerBoard` / singular `LockedOffer`
  in v9) are ignored on load.
- Whole `RunState` serialized (Newtonsoft). Locked offers persist **with `SlotIndex`**; chosen front
  and its `ConditionId` persist.

---

## 13. Presentation (brief)

- **Shop/Build:** `ShopV2Canvas` in `Run.unity`. See `docs/shopv2-handoff-2026-07-13.md` and
  `docs/shopv2-flip-checklist.md` — **read "mock decoration is not state" before touching it.**
- **Combat:** additive 3D arena scenes, cel-shaded ink pipeline (ADR-0002, ADR-0003).
- **Art:** grimdark WW1, bone-on-dark, brass accents — ADR-0001, `docs/art/style-bible/`.
- **Layering:** `Core` = pure deterministic rules (no `UnityEngine`). `Game` orchestrates.
  `Presentation` reads state and renders. **Rules never live in Presentation.**

---

## 14. Design principles (the "why" — so decisions don't get re-litigated)

1. **Decisions before the shooting.** Combat auto-resolves *on purpose*. The game is the Build
   phase; the fight is the consequence. Tactical pauses exist so the player isn't a spectator — not
   so they can micro.
2. **Consent, not gotcha.** Everything hard is shown before it's taken: Battle Conditions on Hard,
   strength previews on all three fronts, the Dread clock warning of the boss.
3. **One clock: Dread.** Difficulty, pacing, shop quality *and shop prices* hang off a single number
   the player advances by choosing fronts. **Do not add a second clock.**
4. **Manpower is the real health bar.** There is no fielding cost — you can always march. What kills
   you is what you *lose*.
5. **Routing is a mercy and a cost.** Broken units cost no Manpower but deny you salvage. Morale is
   a resource.
6. **You buy counts, not units.** Critical Mass means the shop's real question is "does this tip a
   threshold" — which is why tag legibility in the UI is a first-class concern.
7. **Determinism is non-negotiable.** Seeded sub-streams everywhere; a run must reproduce from its
   seed or balance work is guesswork.

---

## 15. Known gaps, bugs and open questions

**Bugs / dead code:**
- **`ProtectSupport` is a no-op** (§10) — it needs `ZoneType.Rear` cells, which unzoned combat boards
  never have. Either zone the board or reimplement the tactic against column index.
- **`SalvageCalculator.ManpowerRefundRatio = 0.25f` is unused** — manpower refund is hardcoded 0.
- **`CannonBlast`** is defined but no piece grants it.
- **Dormant shop slots:** Core reserves **4** (5–8) and the ShopV2 band authors **3**. Neither is
  wired to `ShopSlotUnlockRegistry` — nothing can unlock one. Reconcile the count and wire it.
- **`RunOrchestrator.MaxFights = 10`** is unused at runtime but **load-bearing for tests**.

**Design gaps:**
- **Easy fronts suppress ALL enemy fight-start engines** — a much bigger difficulty swing than the
  Dread table suggests. Intentional? If so, say so in the UI.
- **Only one playable faction today.** Dust Scourge and Cartel of Echoes are next (1–2 passes out).
- **`CombatPacingConfig.PauseThresholds` has a single entry (0.60)** — the design has talked about
  multiple mid-fight windows; today you get the opening pause plus one.
- Meta-progression (`Core/Meta/`: achievements, `MetaProgressionService`) exists but is not part of
  the run design above.
- Balance constants (`MoraleRules` death-shock, `DreadRules` thresholds, `RarityWeights`) are
  **M-series initial values, explicitly flagged for playtest tuning.**
