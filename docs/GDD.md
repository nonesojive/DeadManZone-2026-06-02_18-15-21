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
- **A piece's Supplies price is derived from its rarity, not authored per piece**
  (`Core/Shop/RarityPricing.cs` — Common **10**, Uncommon **15**, Rare **25**; one table for
  units, structures and buildings alike). See §6 for the Dread-tax formula and §11 for the
  per-piece table.

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
  - **`field_hospital` fielded** (2026-07-15 faction-roster-v1): the "survived but damaged" bodies
    figure is cut **-50%** (PROVISIONAL). `ComputeCasualties` takes an optional HQ `BoardState` and
    checks for the piece by id there (`field_hospital` is Building-primary, always HQ-board).
- **Income (Muster):** `faction.baseMusterPerShop + Σ piece.MusterPerShop + supply-synergy pairs`
  (`MusterCalculator`). Each adjacent pair of `supplier`/`supply_line` pieces = **+2**.
- **Emergency Draft:** once per run, covers a shortfall (`EmergencyDraft.TryUse`).
- **Despair Dividend** (Blightborn Pact's economy passive, 2026-07-15 faction-roster-v1 §1.9,
  W1b): **+1 Supply per enemy unit that routed** that fight, win or lose (`FactionPassives
  .DespairDividendSupplies`, applied in `RunOrchestrator.CompleteCombat` right after the fight's
  kill share is stamped). No-op for every other faction. PROVISIONAL magnitude.

### Authority — the command currency
A **per-round pool**, not a bank.

- Pool = **2 base** (`AuthorityCalculator.cs:8`) + buildings: `command_outpost` **+1/round**.
  > 2026-07-15 faction-roster-v1: `officer_quarters` (the old "+1 per `command`-tagged piece"
  > building) was cut with no direct replacement — `command_outpost`'s flat bonus is IronMarch's
  > only Authority building now (`BuildingIncomeRules.cs`).
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
- **4 dormant/reserved slots** (indices **5–8**, all `ReservedAbility`), still not wired to
  anything. Plus **4 Bonus slots (9–12)**: slot 9 is now live for exactly one case — the Cartel
  mercenary slot (below); 10–12 remain inactive.
  *(The ShopV2 band authors 3 dormant slots visually — a UI/data mismatch worth reconciling.)*
  > 2026-07-16 faction-roster-v1 W1b: `RunOrchestrator`'s `ShopGenerator` is now constructed with
  > a non-empty `ShopSlotUnlockRegistry` containing `CartelMercenarySlotProvider` — the previous
  > "`ShopSlotUnlockRegistry.Empty` is always used" is no longer literally true, but the provider
  > is a no-op (`FactionPassives.HasMercenarySlot`) for every faction except Cartel of Echoes.
- Slots 0–2 **offensive**-biased, 3–4 **defensive**-biased (`slot_0..slot_4.asset`).
- **Reroll cost:** `1 + rerolls this round` — climbs within a round, resets between rounds.
  **Paradox Engine** (2026-07-15 faction-roster-v1 §1.9, W1b): the **first reroll each Build is
  free** (`RunOrchestrator.ComputeRerollGoldCost`, `FactionPassives.HasFreeFirstReroll` — Supplies
  only, lock Authority costs are untouched). No-op for every other faction.
- **Lock:** right-click to keep an offer through a reroll. **First lock free; each extra lock costs
  1 Authority** (`max(0, locks - 1)`).
- **Sell (Smelter):** **50%** of the rarity-derived Supplies base cost (`RarityPricing.BaseCost`,
  int-truncated — 5/7/12 by tier) + **50%** of Authority cost; **0% Manpower** (`SalvageCalculator`).
  Dust Scourge: **×1.25** Supplies (applied after the 50% truncation, then truncated again — e.g.
  Common 10 → 5 → **6**, not 6.25). Refund is 50% of the **base** tier cost while purchases pay the
  Dread-inflated price — late-run flips are deliberately lossy. **A mercenary (below) always sells
  for 0** — Supplies, Authority, AND Manpower (`SalvageCalculator.Compute(..., isMercenary: true)`).
  > Trap: `SalvageCalculator.ManpowerRefundRatio = 0.25f` is **declared but never used** —
  > manpower is hardcoded to 0. The constant lies.

### How an offer is rolled (order matters)
1. **Source roll** per slot: **neutral 10% / faction 80% / salvage 10%** (`ShopSlotProfileSO`), with
   a fallback chain. **No duplicate piece within a batch** (`ShopGenerator.cs:183,224`). The salvage
   pity timer (below) can force the source to Salvage before this roll runs.
2. **Rarity roll** — table below, at odds keyed on `FightEquivalent` — **or `FightEquivalent + 1`
   for Crimson Assembly** (`FactionPassives.RarityOddsFightEquivalent`, "Ahead of Schedule",
   2026-07-15 faction-roster-v1 §1.9, W1b) — **prices still use the real `FightEquivalent`** (next
   step). No-op for every other faction.
3. **Price** = `discount(tierBase)` **+ `max(0, FightEquivalent - 1)`** (`ShopGenerator.cs:358-360`).
   **Shop prices inflate with the Dread clock.** The Gold-discount modifier (below) is applied to
   the tier base *before* the Dread tax is added — never to the taxed total.

**Tier base is rarity-derived, not authored per piece** (`Core/Shop/RarityPricing.cs`,
2026-07-13 rarity-standardized-pricing spec):

| Rarity | Base cost | Salvage refund (50%) |
|---|---|---|
| Common | **10** | 5 |
| Uncommon | **15** | 7 |
| Rare | **25** | 12 |

One table for every category — units, structures and economy buildings share it. Building
viability is tuned via *yields* (building income, supplier Critical-Mass thresholds), never via
a per-piece price exception. Moving a tier price is the intended tuning surface; see §14.

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
- **Salvage pity timer** (2026-07-15 faction-roster-v1 §1.5, W1b): same appear-reset architecture
  as the rare pity above — a global salvage-drought counter (`RunState.SalvagePityBatches`) forces
  a salvage-source offer after **4** dry batches (**Dust Scourge tightens this to 2**,
  `FactionPassives.SalvagePityDryBatchThreshold`); counts SHOWN batches (round rolls + rerolls),
  resets on a salvage-source offer appearing (`SalvagePityRules`). **Edge case:** while the salvage
  pool is empty (no enemy fought yet, or that faction has no registered pieces) the counter
  **HOLDS** — neither resets nor climbs (`SalvagePoolAvailability.IsEmpty`,
  `RunOrchestrator.UpdateSalvagePity`).

### Off-faction pieces: salvage vs. mercenary (2026-07-15 faction-roster-v1 §1.4, W1b)
- **`salvage` is a DERIVED tag, never stored:** any board piece with `factionId ≠ player faction`,
  `≠ neutral`, and not flagged mercenary (`OffFactionRules.IsSalvage`). Acquisition history is
  never tracked — it's recomputed from board state every time. No inherent downside; the cost is
  losing the piece's faction-CM contribution.
- **`mercenary` is acquisition-based and PERMANENT:** only the Cartel mercenary slot creates it
  (`PlacedPiece.IsMercenary`, carried through every board/reserves move and the save schema —
  `PlacedPieceRecord.IsMercenary`, additive field, false on older saves). Mercenary suppresses the
  salvage tag and **always sells for 0** (above).
- **Neutral pieces are neither.**

### Cartel of Echoes' mercenary shop slot (2026-07-15 faction-roster-v1 §1.9/§2.4, W1b)
A 6th offer slot (bonus slot **9**, `ShopSlotKind.SpecialRule`), live only when the run's faction
is Cartel of Echoes (`CartelMercenarySlotProvider`, wired through the `IShopSlotUnlockProvider`
seam). Stocks an **off-faction FIGHTER** (`OffFactionRules.IsFighter` — excludes buildings and
`structure`-primary pieces), drawn from the whole registry rather than gated on the last enemy
fought (`MercenaryPoolBuilder`) — mercs are a standing contract, not battlefield spoils. Priced at
the normal tier base **+25% surcharge** (`FactionPassives.MercenarySurchargePercent`,
`ShopGenerator.CreateMercenaryOffer`), same Dread tax as any other offer. A later content wave's
Freelance Colonel rare reduces the surcharge 25%→10% — `FactionPassives.MercenarySurchargeFor` is
the hook it will retune.

### Shop modifiers (live)
`ShopGenerator.ComputeModifiers` — board-driven: **Gold discount** (stacking, **capped 25%**),
**ExtraGeneralSlot**, **EnemyTagPreview**, **GuaranteeEngineerOffer** (injects a defensive/building
offer if none rolled).

### Faction economy/shop passives (2026-07-15 faction-roster-v1 §1.9, W1b)
Single home: `Core/FactionPassives.cs` — a static lookup keyed on `FactionIds`, not a plugin
framework. Every passive is a no-op for factions that don't own it (mirrors
`MoraleRules.IsDeathShockInverted`, W1a). Only IronMarch, Dust Scourge, and Cartel of Echoes have
a `FactionSO` asset today; Paradox Engine, Blightborn Pact, Crimson Assembly and Oathborn Accord
are wired at the rule level (`FactionIds`) ahead of their W2 content pass.

| Faction | Economy/shop passive | Implementation |
|---|---|---|
| IronMarch | None (fallback: +1 Muster/shop if playtests read bland) | — |
| Dust Scourge | ×1.25 salvage refund + salvage pity tightened to 2 | `SalvageCalculator.Compute`, `FactionPassives.SalvagePityDryBatchThreshold` |
| Cartel of Echoes | Mercenary 6th offer slot, +25% surcharge | `CartelMercenarySlotProvider`, `FactionPassives.MercenarySurchargeFor` |
| Oathborn Accord | Medic/healing hook (soft-TBD, leans on the heal-pulse tech) | — |
| Paradox Engine | First shop reroll each Build is free | `RunOrchestrator.ComputeRerollGoldCost`, `FactionPassives.HasFreeFirstReroll` |
| Blightborn Pact | Despair Dividend: +1 Supply per enemy unit that routs | `FactionPassives.DespairDividendSupplies` (§3) |
| Crimson Assembly | Ahead of Schedule: shop rarity odds roll as if FightEquivalent+1 (prices unchanged) | `FactionPassives.RarityOddsFightEquivalent` |
| Ashen Covenant | Inverted death-shock (combat passive, not economy — see §10 W1a) | `MoraleRules.IsDeathShockInverted` |

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

> **`SynergyTraitRegistry` is DESCRIPTION-ONLY and referenced by nothing.** Its blurbs can drift from
> the real per-piece `customAbilities` (the 2026-07-15 faction-roster-v1 pass retired the
> `bulwark_squad`/phalanx example this section used to cite — `iron_guard` is its replacement and
> carries no phalanx ability). Read the piece assets, not the registry.

### Critical Mass (army-wide)
`CriticalMassDefaultRules.Build()` — **31 rules** (2026-07-15: the old single `sniper` rule split
into `sniper_accuracy` + `sniper_damage`, §3). Count a tag, cross a threshold, get a **tiered**
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
| `sniper_accuracy` | role | 2→+5%, 4→+5%, 6→+5% | Accuracy %, sniper role |
| `sniper_damage` | role | 2→+0%, 4→+5%, 6→+10% | Damage %, sniper role |

> The faction rule counts `definition.FactionId` — and **7 of the 19 pieces are `neutral`, not
> `ironmarch_union`** (§11). Neutral pieces do **not** count toward it.
>
> 2026-07-15 faction-roster-v1 §3: the approved **sniper** rule is two `CriticalMassRuleDefinition`s
> sharing the `sniper` combat-role count tag (one rule = one stat, so the "accuracy first, then
> damage%" design needs two) — thresholds land low (≈2/4/6) because sniper counts run small.

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
  neutral ground — see §5). `trench_works` (2026-07-15 faction-roster-v1) cuts a tick's charge
  accrual **-50%** (PROVISIONAL) for any enemy unit within 1 cell (Chebyshev) of it —
  `MovementSlowRules.IsSlowed`, keyed off `GameTagIds.MovementSlowAura`. This
  is a live cross-side check in the tick loop, not the pre-fight `AdjacentAura` engine (which only
  ever sees one side's own board) — and deliberately not Suppression (§1.8 reserves that for
  Crimson).
- **Accuracy:** Melee 92, Piercing 80, Gas 75, Explosive 72, Shredding 68, default 78.
  Snipers 88; Artillery floor 72.

### Morale and rout (ADR-0005)
- Every unit has its own **Morale**. At **0 it BREAKS** — routs, stops being `IsActive`, leaves.
- **Death shock:** a unit dying deals **6 morale damage** to every ally within **2 cells**. Losses
  cascade.
- **Terror damage** is morale damage on hit (e.g. `machine_gun_nest`).
- **Routed units cost no Manpower.** Dead ones cost full. Morale is the difference between a
  bloodied army and a dead run.
- **Morale-damage resistance** (2026-07-15 faction-roster-v1, `MoraleRules.ApplyResistance` +
  `CombatantState.MoraleDamageResistancePercent`): a percent (0-100, clamped) reduction applied to
  every incoming morale hit before the break check. Two sources stack: a piece's own
  `PieceDefinition.MoraleDamageResistancePercent` (Iron Guard, **-40%** PROVISIONAL) and
  Breakthrough Tank's `AdjacentAura` (`SynergyStat.MoraleResistancePercent`, **-25%** PROVISIONAL,
  infantry within 2 board-adjacency hops).

### Win / loss / draw
- A side loses when it has no `IsActive` fighters with `MaxHp > 0` — **all dead *or* all routed**.
- **Mutual wipe → counted as a player WIN.**
- **Timeout at 10,000 ticks → draw, and `PlayerWon = false`** (`TickCombatRun.cs:270-276`).
  **A timeout is NOT a win.** These two draws resolve oppositely — deliberate, but a sharp edge.

### New combat-sim tech (2026-07-15 faction-roster-v1 Wave 1a)
Systems only — content (piece data) lands in a later wave. Every rule below is driven by new
`PieceDefinition` fields the content wave will author; all magnitudes are **PROVISIONAL**.

- **Suppression** (Crimson's tentpole, §1.8). *"Suppressed = attack-speed tier stepped down +
  movement charge slowed for N ticks, applied on hit by suppression-tagged attacks."* The game's
  **only** enemy-facing debuff family. `PieceDefinition.AppliesSuppressionOnHit` triggers it on a
  non-lethal hit; `CombatantState.SuppressionTicksRemaining`/`IsSuppressed` track it;
  `SuppressionRules.cs` holds the two dials (`SuppressionDurationTicks` = 40, PROVISIONAL;
  `SuppressionAttackSpeedStepDown` = 1; `SuppressionMovementSlowPercent` = 50%, PROVISIONAL) and
  is ticked down once per sim tick (`TickCombatRun.TickSuppressionDurations`). **Stacking rule
  (PROVISIONAL): a new hit refreshes the duration, it does not stack.** Wired into
  `TickCombatRun.ResolveAttacks` (apply on hit, fold into effective attack-speed steps) and
  `TryMoveSide` (fold into movement charge). Deliberately distinct from `MovementSlowRules`
  (Trench Works) — see that class's header note.
- **Transport load/target/unload** (Oathborn's tentpole, Armored Ark, §1.8/§2.5). Cargo loads
  into a transport during Build (`BoardState.TryLoadCargo`, tagging `PlacedPiece.CarrierInstanceId`
  — geometry untouched). At spawn, loaded cargo rides **embarked**: off the field, untargetable,
  can't move/attack, doesn't count for the win check (`CombatantState.IsEmbarked`,
  folded into `IsActive`). At the **opening pause window only** the player targets a cell
  (`CommandType.TransportTarget`, validated in `CommandProcessor.TryApplyBatch` — free, no
  Authority cost, PROVISIONAL); the transport drives there (`TickCombatRun.TryMoveSide`,
  overriding its normal engagement goal) and unloads on arrival (`TickCombatRun.UnloadTransport`).
  **If destroyed in transit, cargo spills at the wreck with a morale shock** — never dies inside
  (`TickCombatRun.SpillTransportCargo`, `TransportRules.SpillMoraleShock` = 6, PROVISIONAL, called
  from `LogDestroyed` before the death-shock pulse). Pure cargo-resolution seam:
  `TransportRules.cs`. `PieceDefinition.IsTransport`/`TransportCapacity`.
- **Repeat activations** (Paradox's tentpole, Doctor Recursion, §1.8). *"Your pause-window
  abilities each fire twice."* Deterministic, zero randomness (border rule: Paradox manipulates
  only its own tempo). `PieceDefinition.RepeatsPauseAbilities`; checked once per command batch in
  `CommandProcessor.TryApplyBatch` (`repeatAbilities`) — every successful `UseAbility` command
  executes a second time for free. Also implements Resonance Coil's **Echo**
  (`GrantedAbility.Echo`, free, `checkpointIndex`-agnostic): replays
  `TacticState.LastAbilityCommand` (the last successfully-executed ability this fight). Scoped to
  abilities only this wave, not `SetTactic` — see the class's header note for why.
- **In-combat healing** (heal pulse, §4 🟡; consumers later: Mercy Sister, Field Chirurgeon,
  Hospitaller-General). The sim had no HP-restoration path before this. `PieceDefinition`.
  `HealPulseAmount`/`HealPulseRadius`/`HealPulseIntervalTicks`; pure targeting/cap logic in
  `HealPulseRules.cs`; ticked every sim tick via `TickCombatRun.ApplyHealPulses`, capped at the
  target's MaxHp.
- **Low-state triggers** (Ashen, §2.9). One universal threshold, evaluated live, per-unit: **below
  50% HP or morale** → the piece's own bonuses activate. `LowStateRules.cs`
  (`LowStateThresholdPercent` = 50); `PieceDefinition.LowStateDamageBonus`/
  `LowStateAttackSpeedSteps`; folded into `TickCombatRun.ResolveAttacks`' damage bonus and
  effective attack-speed steps alongside Suppression's step-down.
- **Death-shock inversion** (Ashen passive, §2.9). *"An Ashen death GRANTS morale to allies
  within 2 cells instead of draining it."* Keyed directly off `PieceDefinition.FactionId ==
  FactionIds.AshenCovenant` (`MoraleRules.IsDeathShockInverted`) — smaller than a new per-piece
  flag for a whole-faction passive. `TickCombatRun.ApplyDeathShock` branches to the new
  `ApplyMoraleGain` (clamped at MaxMorale, no resistance modifier, no break check) instead of
  `ApplyMoraleDamage` when true. Same `DeathShockRadius`/`DeathShockDamage` constants as the
  normal case (ADR-0005).
- **Third pause window via piece** (Paradox's The Second Hand, §1.7/§4 🟡). `PauseThresholds` was
  already a list — a fielded piece with `PieceDefinition.AddsPauseWindow` (HQ or combat board,
  scanned the same way `CommandProcessor.GetAvailableCommands` scans for HQ-granted abilities)
  appends one extra threshold (`CombatPacingConfig.ThirdPauseWindowThreshold` = 0.30, PROVISIONAL)
  to a per-fight `TickCombatRun._pauseThresholds` array. `TickCombatRun.CurrentPauseIndex` now
  equals `CheckpointsFired` for any non-opening pause (was hardcoded to `1`) so it generalizes
  correctly past two windows. `TacticPauseValidator.GetTacticCost` now charges the tactic-switch
  Authority premium for **any** pause after the opening one (was `checkpointIndex == 1` only).
- **Gas→morale fusion** (Blightborn's Duchess of Sighs, rare-only, §2.7). *"Your gas damage also
  deals equal morale damage."* Scoped to **attack-sourced** gas damage (pieces with
  `AttackType.Gas`), not the ambient `GasDamageSystem` tick. `PieceDefinition.GasDealsMoraleDamage`
  checked once per `TickCombatRun.ResolveAttacks` volley (`gasMoraleFusion`); on a non-lethal gas
  hit, `ApplyMoraleDamage` fires again for the same amount as the HP damage.
- **Ambient-gas hijack** (Blightborn's Yellow Autumn, rare-only, §2.7). *"The ambient anti-stall
  gas starts far earlier and YOUR units are immune to it."* Sanctioned reuse of
  `GasDamageSystem` — only the start tick and per-side immunity change.
  `PieceDefinition.HijacksAmbientGas`; `GasHijackRules.EarlyGasStartTick` = 120 (PROVISIONAL,
  default is 300). Tracked per side (`TickCombatRun._playerAmbientGasHijack`/
  `_enemyAmbientGasHijack`) so a future enemy-fielded Yellow Autumn is immune on its own side, not
  the player's; the earlier start applies fight-wide once either side hijacks.

**New Core.Tests coverage:** `SuppressionRulesTests`, `TickCombatRunSuppressionTests`,
`TransportRulesTests`, `BoardStateTransportTests`, `TickCombatRunTransportTests`,
`CommandProcessorRepeatActivationTests`, `HealPulseRulesTests`, `TickCombatRunHealPulseTests`,
`LowStateRulesTests`, `MoraleRulesDeathShockInversionTests`, `TickCombatRunPauseWindowTests`,
`TickCombatRunGasAndMoraleFusionTests` — all deterministic, no Unity APIs.

---

## 11. Content — IronMarch Union

**One playable faction right now** (IronMarch Union); **more are planned in the next pass or two.**
`neutral`, `crimson_legion`, `ash_wraiths` are enemy pools; `dust_scourge` and `cartel_of_echoes`
have faction assets and identity hooks (Dust Scourge's ×1.25 salvage refund) but **no roster yet**.

**2026-07-15 faction-roster-v1 pass:** the Neutral and IronMarch Union rosters below replace the
old 17-piece set per `docs/superpowers/specs/2026-07-15-faction-roster-v1-design.md` §2.1/§2.2.
The other five factions (Dust Scourge, Cartel of Echoes, Crimson Legion, Ash Wraiths, plus the
design's Oathborn/Paradox/Blightborn/Ashen slate) are **unchanged pending their own passes** — the
demo pipeline's Crimson Legion / Ash Wraiths pieces (`crimson_elite`, `wraith_stalker`, etc., see
`DemoPieceFactory.cs`) are untouched, and `BossRoster.cs`'s Crimson Marshal / Wraith Harbinger
bosses still field IronMarch/neutral pieces as their (documented) rifleman-fallback armies.

**19 pieces total** — **7 `factionId: neutral`** (§2.1, 4 Common / 3 Uncommon, no rares, no
vehicles/tactics/build-around tags) + **12 `factionId: ironmarch_union`** (§2.2, 6 Common / 3
Uncommon / 3 Rare). Neutral pieces do **not** count toward the `ironmarch_union` Critical-Mass rule.

**Faction baseline** (`ironmarch_union.asset`): Supplies **50**, Manpower **15**, Authority **2**,
Supplies/round **10**, Muster/shop **1**, base salvage **1%**, Combat **6×6**, HQ **3×6**.
**Starting board:** `supply_depot`, `command_outpost` (HQ); `field_medic`, `conscript_rifles`
(Combat).

**"S" (Supplies price) is rarity-derived, not authored** (`Core/Shop/RarityPricing.cs`: Common 10 /
Uncommon 15 / Rare 25 — see §6). `ManpowerCost` and `RequisitionCost` ("M" below) remain authored
per piece — they are the within-tier differentiators. **Every HP/Dmg/M value below is PROVISIONAL
(balance pass pending)** — anchored to the closest pre-existing piece per the design spec's own
instruction; see `IronmarchUnionContentFactory.Pieces.cs` for the `// PROVISIONAL` call-outs.

### Neutral — "The War's Flotsam" (§2.1)

| Piece | Primary | Role | Rar | S | M | HP | Dmg | Notes |
|---|---|---|---|---|---|---|---|---|
| militia_squad | infantry | assault | C | 10 | 1 | 45 | 5 | baseline body, no text |
| field_medic | infantry | support | C | 10 | 1 | 30 | 3 | adjacent allies +HP |
| supply_depot | building | utility | C | 10 | 0 | 50 | — | **+5 Supplies/round** |
| recruitment_office | building | utility | C | 10 | 0 | 35 | — | +Muster/shop |
| machine_gun_nest | structure | defender | U | 15 | 2 | 100 | 2 | terror ping (`maxMorale`40/`terrorDamage`4); **Combat** board |
| trench_works | structure | defender | U | 15 | 2 | 140 | — | HP wall; adjacent enemy movement-charge accrual **-50%** (PROVISIONAL) while it lives — live tick-sim proximity check, `GameTagIds.MovementSlowAura` (`MovementSlowRules.IsSlowed`); deliberately not Suppression (Crimson-exclusive, §1.8) |
| field_hospital | building | support | U | 15 | 0 | 60 | — | post-fight: damaged survivors' Manpower-cost bodies **-50%** (PROVISIONAL) — `ManpowerCalculator.ComputeCasualties(playerCombatants, hqBoard)` detects it by id on the HQ board |

### IronMarch Union — "The Relentless War Machine" (§2.2)

| Piece | Primary | Role | Rar | S | M | HP | Dmg | Notes |
|---|---|---|---|---|---|---|---|---|
| conscript_rifles | infantry | assault | C | 10 | 1 | 50 | 5 | the faction body |
| line_grenadiers | infantry | assault | C | 10 | 1 | 45 | 8 | explosive — anti-structure/anti-heavy via the attack-type triangle |
| field_mortar_team | infantry | artillery | C | 10 | 2 | 30 | 7 | artillery count piece |
| sharpshooter | infantry | sniper | C | 10 | 1 | 30 | 6 | sniper count piece |
| iron_guard | infantry | defender | C | 10 | 2 | 70 | 4 | takes **-40%** morale damage (PROVISIONAL) — `PieceDefinition.MoraleDamageResistancePercent` + `MoraleRules.ApplyResistance` |
| command_outpost | building | utility | C | 10 | 0 | 40 | — | **+1 Authority/round**, `command` |
| forward_observer | infantry | support | U | 15 | 1 | 25 | — | adjacent artillery: +1 attack-speed tier |
| shock_sergeant | infantry | utility | U | 15 | 1 | 35 | 5 | `command`; adjacent assault infantry: +2 damage |
| artillery_park | building | utility | U | 15 | 0 | 90 | — | **HQ board.** *Ranging Barrage* = `GrantedAbility.MortarShot`, now fires from the HQ board — see §10's new HQ-ability wire |
| breakthrough_tank | vehicle | tank | R | 25 | 4 | 90 | 8 | terror ≥2× dmg (wired: `terrorDamage`16 = 2×`baseDamage`8); adjacent infantry **within 2 board-hops** take **-25%** morale damage (PROVISIONAL) — `AdjacentAura` at `Radius`2 |
| grand_battery | structure | artillery | R | 25 | 3 | 110 | 10 | **Combat board.** *Rolling Barrage* = its own `GrantedAbility.RollingBarrage`: radius-2 strike, damage `40 + 8×armyArtilleryCount` (PROVISIONAL) |
| marksman_doctrine_officer | infantry | sniper | R | 25 | 2 | 35 | 6 | stealth until 2nd window (`CombatStealthRules`); snipers +1 dmg per sniper in army (`BoardPerTagCount`) |

**Granted abilities:** `MortarShot` (area, pause 0/1 — Grand Battery no longer uses this, Artillery
Park now does), `ShieldAllies` (protect allies at pause), `CannonBlast` (heavy blast, pause 1 —
**defined but unused by any piece**), `RollingBarrage` (bigger area strike scaling with artillery
count — Grand Battery only).

### HQ-board granted abilities (2026-07-15 faction-roster-v1 §4, was 🟡 "new wire")
Fight-start ability evaluation already read both boards for auras/counts
(`PieceAbilityEngine.EvaluateFightStart(combatBoard, buildBoards)`), but pause-window
**`GrantedAbility` execution** only ever scanned the combat board — HQ buildings could never
surface or fire an ability. Minimal fix, no new abstraction:
- `TickCombatRun` now keeps the player's HQ `BoardState` (`_playerHqBoard`) alongside the combat
  board it already held.
- `CommandProcessor.GetAvailableCommands(board, requisition, checkpointIndex, hqBoard)` scans
  `board.Pieces.Concat(hqBoard.Pieces)` so HQ granted-ability sources show up as commands.
- `CombatAbilityExecutor.Execute(..., hqBoard, artilleryCount)`: if the source instance isn't a
  live combatant (HQ pieces are never spawned into the fight), it falls back to looking the piece
  up on `hqBoard` and treats it as always-active — buildings can't be attacked off-board, so
  there's no "is it alive" question to ask.
- Consumer: `artillery_park`'s *Ranging Barrage* (`GrantedAbility.MortarShot`).

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
8. **Price is never the tuning lever (2026-07-13 rarity-standardized-pricing spec).** A piece's
   Supplies price is derived from its rarity tier (`Core/Shop/RarityPricing.cs`), not authored —
   one table for units, structures and buildings alike. Tuning policy:
   - **Mis-tuned piece:** decide per piece. If its role tolerates being *seen less often*, move its
     rarity (accepting the coupled shop-odds and salvage-weighting changes that come with it). If
     its current visibility is right, adjust its stats instead. Price is never the lever.
   - **"Economy isn't worth building":** fix the payoff side (building yields, supplier
     Critical-Mass thresholds), never the global price table.
   - Moving a tier price is a one-line change in `RarityPricing` — the intended tuning surface.

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
