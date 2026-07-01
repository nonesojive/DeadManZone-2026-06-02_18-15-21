# Build HUD & Round Economy — Design Spec

**Date:** 2026-07-01  
**Status:** Approved (implemented)

## Goals

1. Top bar shows **projected next-round income** and **salvage chance** from faction baseline + current boards — not post-combat surprises.
2. Post-combat rewards are **outcome-independent** for supplies and manpower.
3. Salvage shop chance is **outcome-independent** — combat does not modify the displayed or stored percent.
4. Bottom bar **InfoMessageRegion** hosts build messages; critical mass uses the **right-edge drawer**.

---

## Top bar HUD fields

| Field | Shows | Source |
|-------|-------|--------|
| `SuppliesNumber` | Current supplies balance | `RunState.Supplies` |
| `SuppliesIncome` | Next fight supplies gain | `+N` from `RoundIncomeCalculator` |
| `ManpowerNumber` | Current manpower pool | `RunState.Manpower` |
| `ManpowerIncome` | Next fight manpower gain | `+N` (muster: faction base + board) |
| `AuthorityNumber` | Authority **available to spend this round** | `RunState.Authority` |
| `AuthorityIncome` | Authority **pool max** (resets each build round) | HQ + command buildings |
| `SalvageNumber` | Salvage shop chance | `{n}%` faction base + combat-board boosts |
| `StrengthNumber` | Army strength | Matchup preview |

Legacy optional line: `Salvage: {EnemyFaction} — {n}%` when `LastEnemyFactionId` is set — uses the **same** board-based percent as `SalvageNumber`.

---

## Post-combat income (`RoundIncomeCalculator`)

Applied after **every** fight (win, loss, or draw):

| Resource | Formula |
|----------|---------|
| **Supplies** | `FactionSO.baseSuppliesPerRound` + `BuildingIncomeRules` flat bonuses (e.g. Supply Depot +5) + critical-mass supplies bonuses (flat + % of faction baseline from aggregate HQ+combat board) |
| **Manpower** | `MusterCalculator` (faction `baseMusterPerShop` + piece `musterPerShop` + supply adjacency synergy on aggregate board) |

**Removed:** `FightRewardTable` (deleted); win-only supplies; draw penalty on supplies; per-fight-index supply ladder; fight-reward `BonusAuthority` / `BonusManpower`.

Critical-mass **supplies** bonuses apply at **post-combat income**, not at fight start. Critical-mass **authority** bonus still applies at fight start for combat spending.

Code: `RunOrchestrator.ApplyPostCombatIncome()`, `RunOrchestrator.Income.cs`, `RoundIncomePreview`.

HUD refresh: `RunHudIncomeRefresher` on build-phase state changes and board edits (`BuildScreenHudController.RequestRefresh`).

---

## Salvage chance (`SalvageChanceCalculator`)

```
SalvageChancePercent = min(50, faction.baseSalvageChancePercent + combatBoardSalvageBoost)
```

**Combat board only** for piece boosts (`SalvageChanceBonus`, `SalvageChanceBoost5` flag).

**Removed:** +10% victory bonus, +2% per destroyed enemy type, defeat stripping board boosts.

`RunState.SalvageChancePercent` is synced via `RunOrchestrator.SyncSalvageChancePercent()` when the shop refreshes and when the combat board changes during build — not recomputed from fight outcome.

`LastEnemyFactionId` still set after each fight (determines salvage **pool** faction, not the chance formula).

---

## Bottom bar layout

| Region | Role |
|--------|------|
| `InfoMessageRegion` | `MessagesText` / `BuildMessagesView` (reroll errors, sell feedback, etc.) |
| *(removed)* `BuffStripRegion` | Replaced by critical mass drawer |

Center column alignment: `CenterColumnLayoutFitter` tracks `infoMessageRegion` to main-row center column.

See also: `docs/superpowers/specs/2026-07-01-critical-mass-panel-design.md`.

---

## Tests

- `RoundIncomeCalculatorTests` — supplies, muster, salvage preview
- `SalvageChanceCalculatorTests` — base + board only, 50% cap
- `RunHudViewTests` — income + salvage label wiring
- `BuffStripEvaluatorTests` — critical mass drawer data

---

## Out of scope

- HQ board pieces contributing to salvage boost (combat board only for now)
- Aligning `TickCombatRun` critical mass with aggregate board evaluation
