# Faction Roster v1 — Design Spec (2026-07-15)

**Status: DESIGN SPEC — not implemented.** This replaces the current 17-piece roster as the
*design target*; [`docs/GDD.md`](../../GDD.md) remains authoritative for what the build does
today. This document was produced in a full design interview (Q1–Q17) and every ruling below
was explicitly confirmed. When a faction pass lands, update the GDD in the same commit.

**Premise:** the current piece roster is discarded as design input. Systems (boards, Dread,
Manpower, Critical Mass, tick sim, shop) are all kept — every piece below is designed against
them as they exist, plus the scoped new tech in §4.

---

## 1. Cross-cutting contracts

### 1.1 Roster arithmetic
- **Neutral: 4 common / 3 uncommon, no rares.** 8 factions × (6C / 3U / 3R) = **103 pieces total**.
- **Buildings count inside the 12.** Typical faction spends 2 slots on buildings; Cartel spends 4
  (its identity cost — fewest native fighters, backfilled by the mercenary slot).
- **Commons-distinctness rule:** each of a faction's 6 commons must differ from its siblings in at
  least one of primary tag / role tag / attack type / footprint. No stat-variant filler.
- **Escape hatch (pre-agreed):** if playtests show shop fatigue, grow **commons first**, one
  faction at a time. Rare count is never grown — 3 rares = 3 signposts is load-bearing.

### 1.2 Rarity contract
- **Common = counts.** Stat bodies that tip Critical Mass thresholds and fill the polyomino grid.
  Commons may carry **one simple ability**, but their primary job is counts.
- **Uncommon = enablers.** Adjacency synergies, economy buildings, pieces that make commons
  better. An uncommon bends a build; it doesn't define one.
- **Rare = payoff/signpost.** Always a *conditional* payoff scaling with something accumulated —
  never a standalone statball. Test: "what shop decisions does this change for the next 3 rounds?"
  If none, rewrite. **Rares are also where sanctioned rule-bends live.**
- Each faction's 3 rares lean toward 3 *distinct* builds; stacking into one archetype is allowed
  where it makes sense (used once: Crimson's two tanks).

### 1.3 Neutral's job
**Universal utility glue.** No vehicles, no tactics, no rares, no build-around synergy tags.
Boring but reliable — the sensible default at its 10% shop rate, never a reason to pivot.

### 1.4 Off-faction ruleset (salvage / mercenary)
1. **`salvage` is a derived tag:** any board piece with `factionId ≠ player faction`, `≠ neutral`,
   and not a mercenary. Deterministic from board state; acquisition history is never tracked.
2. **`mercenary` is acquisition-based and permanent:** only Cartel's extra offer creates it.
   Off-faction fighter, +25% price (Freelance Colonel reduces to +10%), **sells for 0**.
   Mercenary suppresses the salvage tag.
3. **Neutral is neither** — neutral pieces are "yours" everywhere.
4. **Salvage pieces have no inherent downside.** Losing faction-CM contribution + earning them
   through kills is the full cost. Loot that feels punished doesn't feel like loot.
- **Dust Scourge meta-unlock trigger:** field a piece carrying `salvage` or `mercenary`.
  (All factions get meta-unlocks eventually — future scope; this is the only one authored.)

### 1.5 Salvage pity timer (replaces any weight change)
Same architecture as the rare pity (state-derived, counts *shown* batches, appear-resets):
a salvage-drought counter forces a salvage-source offer after **4** dry batches globally;
**Dust Scourge's passive tightens it to 2**. Edge case: if the salvage pool is empty the counter
**holds** (does not reset) until stock exists.

### 1.6 Vehicle budget — 6 vehicles in 103 pieces
- **Vehicles are rare-only, game-wide.** Single sanctioned exception: Crimson's uncommon Scout
  Tankette.
- **Tanks trade damage for terror:** terror (morale damage on hit) ≥ 2× physical damage. Tanks
  break armies rather than kill them — and routed enemies grant no salvage, so tank armies loot
  less. Intentional.
- Distribution: **Crimson 3** (2R + the 1U exception) · IronMarch 1R · Oathborn 1R ·
  **Dust Scourge 0 native (steal-only, thematically deliberate)** · Cartel 0 (mercs may import) ·
  Paradox / Blightborn / Ashen 0 (their machines are structures).

### 1.7 Tactics budget (~19 pieces game-wide)
"Tactics piece" = a piece granting a pause-window ability.
- **Crimson 4 · Paradox 3 · every other faction 2 · neutral 0.**
- **Most carriers are buildings** (not all) — the HQ board is the command staff: combat board
  decides who fights, HQ decides what orders you can give.
- **Common/uncommon tactics = small buttons; rares = the big levers** (extra pause window,
  Firestorm, Fire Mission).
- Only ONE piece in the game adds a pause window: Paradox's **The Second Hand**.

### 1.8 The tentpole rule (new-tech scoping)
Design to the fantasy, ship tech with the faction's pass — but **each faction gets at most one
🔴 new-system mechanic, and it must live in that faction's rares.**
- 🔴 Suppression → Crimson. 🔴 Transport load/unload → Oathborn. 🔴 Repeat activations → Paradox.
- **Suppression, defined:** suppressed = attack-speed tier stepped down + movement charge slowed
  for N ticks, applied on hit by suppression-tagged attacks. Reuses two existing dials; terror
  breaks armies, suppression *stops* them.
- **Border rule:** Paradox manipulates only its **own** tempo (speed, echoes, extra moments) and
  uses **zero randomness** (everything deterministic — "fires twice", never "50% to refire").
  **Crimson owns all enemy-facing debuffs.** Any future enemy-slow effect is a suppression
  variant, never a Paradox piece.

### 1.9 Faction identity stack (mandatory 3 layers)
Every faction = roster + a faction Critical-Mass rule + one economy/shop passive.

| Faction | CM payoff (count faction tag →) | Economy/shop passive |
|---|---|---|
| IronMarch | +flat damage, infantry | **None, deliberately.** Fallback if bland: +1 Muster/shop |
| Dust Scourge | counts **salvage-tagged** pieces instead → buffs the strays | ×1.25 salvage refund + salvage pity at 2 (§1.5) |
| Cartel | +Supplies | Mercenary 6th offer slot |
| Oathborn | +max Morale, army-wide | Medic/healing hook — soft-TBD, leans on §4 healing tech |
| Paradox | +attack-speed tier steps | **First shop reroll each Build is free** |
| Blightborn | +% gas damage | **Despair Dividend: +1 Supply per enemy unit that routs** |
| Crimson | +suppression duration/potency | **Ahead of Schedule: shop rarity odds roll as if FightEquivalent+1** (prices unchanged) |
| Ashen | low-state trigger bonuses strengthen | **Inverted death-shock:** an Ashen death grants morale to allies within 2 cells instead of draining it |

---

## 2. Rosters

Costs/stats are deliberately unassigned — a later balance pass anchors them to the existing
IronMarch numbers (roughly C 10–15 Supplies, U 15–25, R 20–30; Manpower per current table).
Ashen combat pieces are the exception: **unusually low ManpowerCost (mostly 1)** by rule (§2.9).

### 2.1 NEUTRAL — "The War's Flotsam"
4C/3U · no vehicles/tactics/rares · the three uncommons are three different *defensive* answers.

| Piece | Rar | Primary | Role | Attack | Armor | Cells | Sketch |
|---|---|---|---|---|---|---|---|
| Militia Squad | C | infantry | assault | ballistic | none | 2 | Baseline body, no text |
| Field Medic | C | infantry | support | — | none | 1 | Adjacent allies +HP; the 1-cell gap-filler |
| Supply Depot | C | building | utility | — | — | 2 | +5 Supplies/round |
| Recruitment Office | C | building | utility | — | — | 2 | +Muster/shop |
| Machine-Gun Nest | U | structure | defender | ballistic | light | 2 | Small terror ping — the mechanic's teaser before you meet a tank |
| Trench Works | U | structure | defender | — | light | 3 (L) | No attack; HP wall that slows adjacent enemy movement |
| Field Hospital | U | building | support | — | — | 3 | Post-fight: reduces Manpower lost to damaged survivors. **Priced painfully — insurance costs tempo** |

### 2.2 IRONMARCH UNION — "The Relentless War Machine"
6C/3U/3R · 2 buildings · 2 tactics · 1 vehicle. Rares = infantry-mass / artillery / snipers.

| Piece | Rar | Primary | Role | Attack | Armor | Cells | Sketch |
|---|---|---|---|---|---|---|---|
| Conscript Rifles | C | infantry | assault | ballistic | none | 2 | The faction body |
| Line Grenadiers | C | infantry | assault | explosive | none | 2 | Anti-structure/anti-heavy common |
| Field Mortar Team | C | infantry | artillery | explosive | none | 2 | Artillery count piece |
| Sharpshooter | C | infantry | sniper | piercing | none | 1 | Sniper count piece |
| Iron Guard | C | infantry | defender | ballistic | medium | 3 | Takes reduced morale damage |
| Command Outpost | C | building | utility | — | — | 2 | +1 Authority/round, `command` |
| Forward Observer | U | infantry | support | — | none | 1 | Adjacent artillery: +1 attack-speed tier |
| Shock Sergeant | U | infantry | command | ballistic | light | 1 | Adjacent assault infantry: +damage |
| Artillery Park | U | building | utility | — | — | 3 | **Tactic:** *Ranging Barrage* — small area strike |
| Breakthrough Tank | R | vehicle | tank | ballistic | heavy | 4 | Terror ≥2× dmg; infantry within 2 cells gain morale resistance — armor leads, the wave follows |
| Grand Battery | R | structure | artillery | explosive | 4 (2×2) | light | **Combat board.** **Tactic:** *Rolling Barrage* — big area strike scaling with artillery count |
| Marksman-Doctrine Officer | R | infantry | sniper/command | piercing | none | 1 | Stealth until 2nd window; snipers +damage per sniper in army |

Ruling: a **`sniper` CM rule** is approved (≈2/4/6 → +accuracy, then +damage%; data-only change).

### 2.3 DUST SCOURGE — "Scavengers of the Wastes"
6C/3U/3R · 3 buildings · 2 tactics · 0 native vehicles. Gas differentiation: **Dust uses gas as a
raider's tool; Blightborn IS gas.**

| Piece | Rar | Primary | Role | Attack | Armor | Cells | Sketch |
|---|---|---|---|---|---|---|---|
| Waste Raider | C | infantry | assault | shredding | none | 2 | Scrap-shotgun body |
| Outrider | C | infantry | assault | ballistic | none | 1 | High movement harasser |
| Gasflinger | C | infantry | gas | gas | none | 2 | Gas count piece |
| Rust Spear | C | infantry | defender | melee | light | 2 | Scrap-plated line-holder |
| Vulture Crew | C | infantry | support | — | none | 1 | +salvage chance % while fielded |
| Scavenger's Cache | C | building | utility | — | — | 2 | +Supplies/round, small +salvage chance |
| Raid Captain | U | infantry | command | ballistic | light | 1 | Adjacent salvage-tagged pieces: +damage |
| Chop-Shop | U | building | utility | — | — | 3 | Salvage-tagged pieces +HP while it stands |
| Fume Still | U | building | utility | — | — | 2 | **Tactic:** *Gas Cloud* — small area gas |
| Corpse-Tithe Caravan | R | structure | support | — | light | 3 | **Rule-bend: routed enemies count as kills for salvage share** |
| Stormcaller of the Yellow Wind | R | infantry | gas | gas | none | 1 | **Tactic:** *Yellow Wind* — wide gas storm scaling with gas count |
| Warlord of Many Banners | R | infantry | command | melee | medium | 2 | Big stat buffs per distinct faction represented. **Neutral does not count**; tuned around 3 banners, ceiling ~4–5 |

### 2.4 CARTEL OF ECHOES — "War as Profit"
6C/3U/3R · **4 buildings** · 2 tactics · 0 native vehicles · 7 native fighters (fewest in game).

| Piece | Rar | Primary | Role | Attack | Armor | Cells | Sketch |
|---|---|---|---|---|---|---|---|
| Company Rifleman | C | infantry | assault | ballistic | light | 2 | PMC trooper — better-equipped, pricier |
| Strikebreaker | C | infantry | defender | melee | light | 2 | Riot-shield muscle |
| Repo Crew | C | infantry | assault | shredding | none | 1 | Close-range collections |
| Paymaster's Aide | C | infantry | support | — | none | 1 | `supply_line` — forms Muster pairs |
| Freight Depot | C | building | utility | — | — | 3 | `supplier`, +Supplies/round |
| Company Store | C | building | utility | — | — | 2 | +Muster/shop |
| Contract Officer | U | infantry | command | ballistic | light | 1 | Adjacent mercenaries: +damage |
| Executive Suite | U | building | utility | — | — | 3 | +1 Authority per `command` piece |
| Munitions Exchange | U | building | utility | — | — | 2 | **Tactic:** *Overtime Bonus* — all units +1 attack-speed tier for a stretch |
| Freelance Colonel | R | infantry | command | ballistic | medium | 2 | All mercenaries +HP/+damage; merc surcharge 25%→10% |
| Echo Chairman | R | infantry | command | — | none | 1 | +2 Authority pool; orders cost 1 less. **Tactic:** *Executive Order* — one free order at any window. (Cartel makes orders *cheap*; Paradox makes moments *plentiful*) |
| War Profiteer | R | infantry | utility | ballistic | light | 2 | **Combat board — greed needs a neck to wring.** All units +1 damage per 25 Supplies held at fight start, **capped (~+4)** |

### 2.5 OATHBORN ACCORD — "Peacekeepers Turned Crusaders"
6C/3U/3R · 2 buildings · 2 tactics · 1 vehicle. Design spine: *how do we cross the field without
breaking?* Morale so they don't rout en route, medics so they survive arriving, one transport
that skips the walk.

| Piece | Rar | Primary | Role | Attack | Armor | Cells | Sketch |
|---|---|---|---|---|---|---|---|
| Truncheon Line | C | infantry | assault | melee | light | 2 | Riot-shield peacekeepers |
| Pilgrim Spears | C | infantry | assault | melee | none | 3 | Cheap swarm — melee count piece |
| Vow Warden | C | infantry | defender | melee | medium | 2 | Shield wall anchor |
| Banner Bearer | C | infantry | support | — | none | 1 | Adjacent allies +morale |
| Mercy Sister | C | infantry | support | — | none | 1 | Heals nearby allies during combat |
| Oathhall | C | building | utility | — | — | 2 | +Muster/shop |
| Confessor | U | infantry | command | melee | light | 1 | Adjacent allies take reduced morale damage — the anti-terror tech |
| Field Chirurgeon | U | infantry | support | — | none | 2 | Stronger in-combat healing aura |
| Sanctum Command | U | building | utility | — | — | 3 | **Tactic:** *Rally* — restore morale to all units |
| Armored Ark | R | vehicle | transport | — | heavy | 4 | 🔴 Load pieces in Build. **At the opening window the player targets a cell; the Ark drives there and unloads on arrival** (choice of *where*, not when). **If destroyed in transit, cargo spills out at the wreck with a morale shock** — never dies inside |
| High Exarch | R | infantry | command | melee | medium | 2 | Units at full morale +damage; all morale damage taken halved |
| Hospitaller-General | R | infantry | support | — | light | 2 | Healing scales with support count; wounded survivors cost less Manpower |

Ruling: **melee viability sequencing** — ship as designed; if playtests show they die in transit,
the pre-agreed *first* buff lever is advance speed (e.g. charge rate while advancing). Do not
bake speed in up front.

### 2.6 PARADOX ENGINE — "The Experiment That Won't End"
6C/3U/3R · 3 buildings · **3 tactics** · 0 vehicles. Self-tempo only; zero randomness (§1.8).

| Piece | Rar | Primary | Role | Attack | Armor | Cells | Sketch |
|---|---|---|---|---|---|---|---|
| Chrono-Fusilier | C | infantry | assault | ballistic | none | 2 | The body, slightly out of sync |
| Phase Vanguard | C | infantry | defender | ballistic | light | 2 | Line-holder |
| Arc Lancer | C | infantry | sniper | piercing | none | 1 | Beam-rifle marksman |
| Field Dynamo | C | infantry | support | — | none | 1 | Adjacent allies +1 attack-speed tier |
| Chrono-Lab | C | building | utility | — | — | 2 | +Supplies/round |
| Assembly Loop | C | building | utility | — | — | 2 | +Muster/shop |
| Overclock Engineer | U | infantry | support | — | none | 1 | Adjacent piece: +movement charge rate |
| Chronometry Station | U | building | utility | — | — | 3 | **Tactic:** *Time Dilation* — all units +movement & attack speed for a stretch |
| Resonance Coil | U | structure | utility | — | light | 2 | **Tactic:** *Echo* — repeat the last order/tactic issued this fight, free |
| The Second Hand | R | building | utility | — | — | 4 | **HQ board** (counterplay is economic, not ballistic). **Adds a third tactical pause window** — the only piece in the game that does |
| Doctor Recursion | R | infantry | command | — | none | 1 | 🔴 **Your pause-window abilities each fire twice** |
| Perpetual Engine | R | structure | utility | — | light | 4 (2×2) | Combat-board machine: ALL your units +1 attack-speed tier (faction-blind, splash-friendly) |

### 2.7 BLIGHTBORN PACT — "The Rot of Old Houses"
6C/3U/3R · 3 buildings · 2 tactics · 0 vehicles. Honest weakness (unpatched, deliberate): gas is
weak vs structures/buildings — structure-heavy armies are their bad matchup.

| Piece | Rar | Primary | Role | Attack | Armor | Cells | Sketch |
|---|---|---|---|---|---|---|---|
| Threadbare Guard | C | infantry | assault | ballistic | none | 2 | Moth-eaten uniforms, family muskets |
| Censer Carrier | C | infantry | gas | gas | none | 2 | Gas count piece |
| Iron Veil Guard | C | infantry | defender | melee | medium | 2 | Halberdiers in tarnished plate |
| Court Physician | C | infantry | support | — | none | 1 | Adjacent allies +HP |
| Dirge Piper | C | infantry | support | — | none | 1 | Adjacent allies deal +morale damage on attacks |
| Poison Garden | C | building | utility | — | — | 2 | +Supplies/round |
| Gas Alchemist | U | infantry | support | — | none | 1 | Adjacent gas pieces: +damage |
| Widow of the House | U | infantry | command | ballistic | light | 1 | Attacks deal terror; adjacent allies' terror strengthened |
| Fumigation Works | U | building | utility | — | — | 3 | **Tactic:** *Creeping Cloud* — lay a gas area |
| The Yellow Autumn | R | building | utility | — | — | 3 | **Rule-bend: ambient anti-stall gas starts far earlier and your units are immune to it.** Sanctioned reuse of `GasDamageSystem` — watch pacing in playtests |
| Duchess of Sighs | R | infantry | command | gas | light | 2 | **Your gas damage also deals equal morale damage.** Rare-only fusion — base Blightborn gas hurts bodies; the Duchess makes it hurt hearts |
| Vitriol Throne | R | structure | artillery | gas | light | 4 (2×2) | **Tactic:** *Vitriol Rain* — huge gas bombardment scaling with gas count |

### 2.8 CRIMSON ASSEMBLY — "Clinical Optimization"
6C/3U/3R · 2 buildings · **4 tactics** · **3 vehicles**. Owns all enemy-facing debuffs game-wide.

| Piece | Rar | Primary | Role | Attack | Armor | Cells | Sketch |
|---|---|---|---|---|---|---|---|
| Assembly Trooper | C | infantry | assault | ballistic | light | 2 | Best-equipped common in the game, priced like it |
| Suppression Team | C | infantry | assault | ballistic | none | 2 | Attacks apply weak suppression — the count piece |
| Hazmat Vanguard | C | infantry | defender | ballistic | medium | 2 | Sealed-suit line troops |
| Ballistics Analyst | C | infantry | support | — | none | 1 | Adjacent ranged pieces: +accuracy |
| Research Annex | C | building | utility | — | — | 2 | +Supplies/round |
| Bunker Emplacement | C | structure | defender | ballistic | medium | 2 | Combat-board strongpoint |
| Scout Tankette | U | vehicle | tank | ballistic | light | 2 | **The one uncommon vehicle in the game.** Small terror. **Tactic:** *Smoke Discharge* — drops enemy accuracy in an area |
| Fire-Plan Officer | U | infantry | command | ballistic | light | 1 | Adjacent ballistic pieces: +damage |
| Operations Bunker | U | building | utility | — | — | 3 | **Tactic:** *Suppressive Sweep* — suppress the enemy front rank |
| "Vanquisher" Doctrine Tank | R | vehicle | tank | ballistic | heavy | 4 | Terror ≥2× dmg, suppresses on hit. **Tactic:** *Cannon Blast* — adopts the orphaned granted ability |
| "Stiller" Suppression Platform | R | vehicle | tank | ballistic | heavy | 4 | Quad guns: low damage, area suppression every volley, high terror. (Two rare tanks = sanctioned archetype stack: the fork is *which* tank defines the run) |
| Director of Programs | R | infantry | command | — | none | 1 | +1 Authority pool. **Tactic:** *Fire Mission* — suppress a **targeted area** (deliberately NOT army-wide, so spread formations — read: Oathborn — have real counterplay) |

### 2.9 ASHEN COVENANT — "The Revolution of Cinders"
6C/3U/3R · 2 buildings · 2 tactics · 0 vehicles.
**Faction-wide rules:** low-state trigger threshold = **below 50%** (HP or morale, per-unit, live
in sim — one universal number). **Fanatic lives are cheap:** combat pieces carry unusually low
ManpowerCost (mostly 1) and the faction baseline gets high Muster — the martyrdom faction must
not be at war with the run's health bar.

| Piece | Rar | Primary | Role | Attack | Armor | Cells | Sketch |
|---|---|---|---|---|---|---|---|
| Zealot Mob | C | infantry | assault | melee | none | 3 | Cheap fervent swarm, `fanatic` |
| Ash Acolyte | C | infantry | assault | melee | none | 1 | `fanatic`; +damage below half HP — identity taught at common tier |
| Torchbearer | C | infantry | assault | fire | none | 2 | Flamethrower common, `fanatic` |
| Penitent | C | infantry | defender | melee | none | 2 | No armor, unusually high HP |
| Hymnal Leader | C | infantry | support | — | none | 1 | Adjacent allies +morale |
| Shrine of Ash | C | building | utility | — | — | 2 | +Muster/shop |
| Reliquary Bearer | U | infantry | support | — | none | 1 | +damage per adjacent `fanatic` |
| Firebrand Vicar | U | infantry | command | fire | light | 1 | Adjacent fire pieces: +damage |
| Pyre Altar | U | building | utility | — | — | 2 | **Tactic:** *Fervor* — all units +morale & attack speed for a stretch |
| Saint of the Embers | R | infantry | command | melee | none | 1 | Army-wide: units below half HP or morale gain large +damage & +attack speed |
| The Ash Martyr | R | infantry | utility | melee | none | 1 | **On death: all allies +damage and morale restores to full.** Fielded in order to be lost — sanctioned |
| Pyre Cathedral | R | building | utility | — | — | 4 | **Tactic:** *Firestorm* — huge fire barrage scaling with fire count |

---

## 3. New Critical-Mass rules needed
- **`sniper`** — approved: ≈2/4/6 → +accuracy, then +damage% (low thresholds; sniper counts run small).
- Per-faction rules from §1.9 (8 new/changed faction rules, incl. Dust's salvage-count inversion).
- Audit during implementation whether `fanatic` / `gas` / `fire` / `melee` need count rules, or
  whether tactic-scaling (Yellow Wind, Vitriol Rain, Firestorm read counts directly) suffices.

## 4. New-tech ledger (staged with each faction's pass)

| Tech | Tier | Faction | Notes |
|---|---|---|---|
| Suppression debuff system | 🔴 | Crimson | Definition in §1.8 |
| Transport load/target/unload | 🔴 | Oathborn | Opening-window cell targeting; spill-on-destruction |
| Repeat activations (abilities fire twice) | 🔴 | Paradox | Deterministic only |
| In-combat healing (heal pulse) | 🟡 | Oathborn | Sim has no HP-restoration path today |
| Low-state triggers (<50% HP/morale) | 🟡 | Ashen | Per-unit, evaluated live |
| Death-shock inversion | 🟡 | Ashen | Faction passive; seam exists in MoraleRules |
| Third pause window via piece | 🟡 | Paradox | `PauseThresholds` is already a list |
| Mercenary shop slot | 🟡 | Cartel | Variant of `ExtraGeneralSlot` |
| Salvage pity timer | 🟡 | global | §1.5; mirror `RarityWeights` pity |
| Ambient-gas hijack (early start + immunity) | 🟡 | Blightborn | Reuses `GasDamageSystem` |
| Gas→morale fusion | 🟡 | Blightborn | Duchess only |
| HQ buildings granting pause abilities | 🟡 | global | New wire — fight-start eval currently reads combat board only |
| Rarity-odds shift (FightEquivalent+1) | 🟡 | Crimson | One lookup change |
| Terror ≥2× on tanks; `Cannon Blast` assignment | 🟢 | — | Existing seams |

## 5. Open TODOs
- **IronMarch econ passive:** ships with none; add +1 Muster/shop if playtests read bland.
- **Oathborn passive:** solidify the medic/healing hook once §4 healing tech lands.
- **Balance pass:** all costs, stats, magnitudes, and per-banner/per-25-Supplies ratios unassigned.
- **Melee viability watch:** Oathborn advance-speed lever pre-agreed as first buff (§2.5).
- **Yellow Autumn pacing watch:** fights vs it get slower before the gas works (§2.7).
- **Meta-unlocks per faction:** future scope; only Dust Scourge's trigger is authored (§1.4).
