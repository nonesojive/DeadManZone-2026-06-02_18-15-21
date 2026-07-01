> **Superseded (2026-07-01):** `FightRewardTable` removed; manpower no longer from fight rewards. See `2026-07-01-build-hud-economy-design.md`.

# DeadManZone — Manpower Rework Design Spec

**Date:** 2026-06-07  
**Engine:** Unity 6  
**Status:** Approved (brainstorming)  
**Scope:** Manpower economy, casualties, muster income, combat stat rebalance (HP/DPS/squad sizes)

---

## Summary

Manpower represents **bodies available to field squads**, not a spend-and-refund currency. Units represent **squads** (e.g. Rifle Squad = 10 soldiers), not individuals. The player maintains a **manpower pool** (~100 at run start), must meet a **fielding threshold** to begin combat, suffers **casualties** after fights based on damage taken, and recovers bodies via **muster income** driven by HQ, buildings, and synergies when each shop phase opens.

**Locked decisions:**

| Area | Choice |
|------|--------|
| Fielding | Threshold check only — **no deduction** at fight start |
| Casualties | **Hybrid C** — survivors: damage-based; destroyed: full `manpowerCost` |
| HP / combat | **Full rebalance** — rescale to squad fantasy + retune DPS for fight pacing |
| HQ damage | Same casualty rules; HQ has its own `manpowerCost` and `maxHp` |
| Muster source | **Building/piece driven** — faction HQ baseline + per-piece `musterPerShop` + synergies |
| Muster timing | **C** — casualties at fight end; muster when shop opens (+ once at run start) |
| Run start | **B** — pool starts at 100; first muster applied immediately |
| Manpower cap | **None** — pool can grow unbounded from muster stacking |
| Data model | **Approach 1** — extend `PieceDefinitionSO` / `FactionSO`; new `MusterCalculator` |
| Fight rewards | Remove or zero `BonusManpower` from `FightRewardTable` |

---

## Section 1 — Core model

### Manpower pool

- Single integer pool on `RunState.Manpower`.
- Starting value from `FactionSO.startingManpower` (**100** default).
- No maximum cap.

### Squad size (`manpowerCost`)

- Each fielded piece (combatants + HQ) has `manpowerCost` = bodies required to deploy it.
- **Fielding requirement** = sum of `manpowerCost` for all pieces on the player board that count toward fielding (combatants per `GameTagIds.Combatant`, plus HQ).
- **Can start fight** when `Manpower >= fielding requirement`.
- Fielding does **not** reduce the pool.

Example: 2× Rifle Squad (10 each) + 1× tank (5) → need **25** manpower to begin.

### HP per body

```
hpPerBody = maxHp / manpowerCost   (integer division; designer ensures clean ratios)
```

Example: Rifle Squad — `maxHp: 100`, `manpowerCost: 10` → **10 HP per body**.

Used only for casualty math, not combat resolution (combat still uses `maxHp` directly).

### Casualties (hybrid C)

After each fight, for **each player-side combatant and HQ** that participated:

| Outcome | Casualties for that piece |
|---------|---------------------------|
| **Destroyed** (`CurrentHp <= 0` at fight end) | Full `manpowerCost` |
| **Survived** | `min(manpowerCost, floor(damageTaken / hpPerBody))` |

**Total casualties** = sum across all qualifying pieces. Subtract from `RunState.Manpower` (floor at 0).

**Damage source:** `CombatantState.DamageTakenThisFight` (gross damage received). Healing during combat does not reduce the casualty ledger — healing keeps units alive to avoid full-squad wipe penalties, not to erase casualties.

**Non-combatants** (Field Medic, Supply Depot, etc.): `manpowerCost` counts toward fielding if placed; casualties apply only if they took damage and are tracked as combatants. Buildings with `noncombatant` tag and 0 damage taken contribute 0 casualties.

### Muster income

```
muster = faction.baseMusterPerShop
       + sum(piece.musterPerShop for each piece on board)
       + synergy muster bonuses
```

Applied:
1. **Once at run start** (after initial board/HQ placement) — “first shop fully staffed.”
2. **Each time the build/shop phase opens** after returning from combat.

Added to `RunState.Manpower` with no cap.

Fight win rewards do **not** grant meaningful manpower (remove `BonusManpower` from `FightRewardTable`).

---

## Section 2 — Run flow

```
Run start
  → Manpower = faction.startingManpower (100)
  → Apply first muster (HQ + buildings on board)
  → Shop / build phase

Begin fight (threshold only)
  → if Manpower < fielding requirement → block with message
  → else start combat (no pool change)

Fight end (aftermath)
  → Compute casualties from combatant states
  → Manpower -= casualties
  → Show battle report: "Casualties: −N"
  → Morale / supplies / authority unchanged unless other systems apply

Shop open
  → Apply muster income
  → Optional UI flash: "Muster: +M"
```

### Removed behaviors

- **Deduct upkeep** in `RunOrchestrator.BeginCombat` — delete `State.Manpower -= upkeep`.
- **Refund survivors** via `ManpowerCalculator.RefundSurvivors` — replace with casualty calculation.
- **Salvage manpower refund** — set to 0 (bodies are not stored in sold pieces).
- **Emergency Draft** auto-fill shortfall — remove or repurpose as optional one-time muster boost (deferred; default: remove auto shortfall fill).

---

## Section 3 — Data model (Approach 1)

### `FactionSO` (new / changed fields)

| Field | Example (Iron Vanguard) | Notes |
|-------|-------------------------|-------|
| `startingManpower` | 100 | Run-start pool |
| `baseMusterPerShop` | 12 | HQ baseline muster per shop phase |

Per-faction thematic variance: Dust Scourge lower (10), Cartel higher (14), etc.

### `PieceDefinitionSO` / `PieceDefinition` (changed fields)

| Field | Purpose |
|-------|---------|
| `manpowerCost` | Squad size + fielding requirement |
| `maxHp` | Rescaled to squad fantasy |
| `baseDamage` / `cooldownTicks` | Retuned in balance pass |
| `musterPerShop` | **New** — bodies recruited each shop while piece is on board |

### HQ example (Iron Vanguard `hq_command`)

| Stat | Target |
|------|--------|
| `manpowerCost` | 8 (command staff) |
| `maxHp` | 80 |
| `musterPerShop` | 0 (baseline comes from faction; HQ is always present) |

Faction `baseMusterPerShop` represents HQ muster; optional future `hq_command.musterPerShop` override if needed.

### Building muster examples (initial targets)

| Piece | musterPerShop |
|-------|---------------|
| Supply Depot | +3 |
| Field Workshop | +2 |
| Signal Relay | +1 |
| Radio Array | 0 (command/authority focused) |

### Synergy bonuses

Extend tag/synergy evaluation (same hook as fight-start synergies) with muster contributions:

- Example: 2+ pieces with `supply` synergy tag → **+2** muster
- Faction-specific rules added via `TagRegistry` / `SynergyEngine` as needed

### New core types

**`MusterCalculator`** (`Assets/_Project/Core/Run/MusterCalculator.cs`)

```csharp
int ComputeMuster(FactionSO faction, BoardState board, SynergySnapshot? synergies)
```

**`ManpowerCalculator`** (refactor)

```csharp
int ComputeFieldingRequirement(BoardState board, ContentRegistry content)
bool CanStartBattle(BoardState board, int manpower, ContentRegistry content)
int ComputeCasualties(IReadOnlyList<CombatantState> playerCombatants)
int HpPerBody(PieceDefinition definition)
```

Remove `RefundSurvivors`.

---

## Section 4 — Combat rebalance

### Design principle

Squads have **human-scale HP** (rifle squad ≈ 100 HP / 10 bodies). Combat pacing (grind segment length, tutorial pause rates) is preserved by retuning **enemy templates** and **player DPS**, not by hidden fight-index modifiers.

### Reference squad table (initial targets — tune in implementation)

| Tier | Piece | manpowerCost | maxHp | hpPerBody |
|------|-------|--------------|-------|-----------|
| HQ | Command HQ | 8 | 80 | 10 |
| Light infantry | Conscript Rifleman | 6 | 60 | 10 |
| Line infantry | Rifle Squad | 10 | 100 | 10 |
| Heavy infantry | MG Team | 12 | 120 | 10 |
| Medic | Field Medic | 4 | 40 | 10 |
| Light vehicle | Armored Transport | 5 | 50 | 10 |
| Tank | Crimson Tank / Diesel Walker | 4–5 | 40–50 | ~10 |
| Artillery | Mobile Cannon | 6 | 60 | 10 |

Enemy pieces rescale proportionally or per-role to keep fights 1–10 within existing tutorial balance test targets (≥90% pause #2 and survival, fights 1–3).

### Balance validation

Re-run after stat pass:

- `TutorialBalanceTests` (all 6)
- `ManpowerCalculatorTests` (rewrite for casualties + fielding)
- `VerticalSliceRegressionTests` combat flows
- `EnemyTemplatePlacementTests`

---

## Section 5 — UI & presentation

### HUD

- **Manpower: {pool}** (unchanged location)
- Optional tooltip: `Fielding need: {requirement}`

### Begin Fight

- Disabled when `pool < requirement`
- Message: `Insufficient manpower: need {requirement}, have {pool}`

### Battle report (`BattleReportPresenter`)

Replace `Manpower refunded: N` with:

```
Casualties: −{N}
```

Keep damage dealt/taken leaderboards.

### Shop phase

Brief notification or log line when muster applies: `Muster: +{M}`

---

## Section 6 — Code touch list

| File | Change |
|------|--------|
| `ManpowerCalculator.cs` | Fielding + casualties; remove refund |
| `MusterCalculator.cs` | **New** |
| `RunOrchestrator.cs` | Remove deduct/refund; casualties on fight end |
| `RunOrchestrator` shop entry | Apply muster on shop open |
| `RunState` / `FactionSO` | `baseMusterPerShop` |
| `PieceDefinition` / `PieceDefinitionSO` | `musterPerShop`; rescaled stats |
| `BattleReport` / `BattleReportBuilder` | `ManpowerCasualties` replaces `ManpowerRefunded` |
| `BattleReportPresenter` | Copy update |
| `FightRewardTable` | Zero bonus manpower |
| `SalvageCalculator` | Zero manpower salvage |
| `EmergencyDraft.cs` | Remove auto shortfall or deprecate |
| `DemoPieceFactory` / piece `.asset` files | Full stat + muster pass |
| `DemoFactionFactory` / faction `.asset` files | startingManpower, baseMusterPerShop |
| `SynergyEngine` or tag rules | Muster bonuses |
| `RunSaveSerializer` | Legacy default manpower → 100 |
| Tests | Manpower, tutorial balance, orchestrator |

---

## Section 7 — Out of scope (this pass)

- Manpower cap or over-muster penalties
- Manpower cost to **buy** shop offers (still supplies/gold only)
- Per-faction `ManpowerProfile` ScriptableObject (approach 3 — deferred)
- Enemy manpower economy (enemies unaffected)
- New healing abilities solely for casualty reduction (existing heals already strategic via wipe avoidance)

---

## Section 8 — Success criteria

1. Player starts with **100** manpower (+ first muster) and can field a typical early board (HQ + 2 infantry squads).
2. Beginning a fight **never** changes manpower; sub-threshold boards are blocked.
3. After a fight, battle report shows **casualties** derived from damage / hybrid wipe rule.
4. Each shop phase adds **muster** from HQ baseline + buildings + synergies.
5. Fight rewards do not inflate manpower.
6. Tutorial balance tests (fights 1–3) still pass at ≥90% after combat rebalance.
7. Healing and muster buildings create meaningful strategic tradeoffs in playtesting.

---

## Appendix — Casualty examples

**Rifle Squad** (10 bodies, 100 HP, 10 HP/body):

- Took 22 damage, survived → `floor(22/10) = 2` casualties
- Took 95 damage, survived → `min(10, 9) = 9` casualties
- Destroyed → **10** casualties (full squad)

**Fight total:** Rifle (22 dmg, survived) + Rifle (destroyed) + HQ (15 dmg, survived, 8 bodies, 80 HP):

- Rifle A: 2
- Rifle B: 10
- HQ: `floor(15/10) = 1`
- **Total: −13 manpower**
