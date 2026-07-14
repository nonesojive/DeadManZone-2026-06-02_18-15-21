# Rarity-Standardized Supply Pricing — Design

**Date:** 2026-07-13
**Status:** Approved (brainstorm w/ owner), pending implementation plan
**Supersedes:** per-piece authored `GoldCost` (GDD §6 pricing, §11 piece table "S" column)

## Problem

Every piece carries a hand-authored Supplies cost. The authored spread has drifted rather than
encoding a curve: commons run 12–15, uncommons 10–25 (a `field_medic` at 10 undercuts every
common; `officer_quarters` at 25 outprices two rares), rares 18–30. Tuning the economy means
touching 17 numbers that each interact with income, salvage, and the Dread price tax.

## Decision

A piece's Supplies price is **derived from its rarity**. `GoldCost` stops being authored data.

### 1. Price table (M-series initial values, playtest-tunable)

| Rarity | Base cost | Salvage refund (50%) |
|---|---|---|
| Common | **10** | 5 |
| Uncommon | **15** | 7 |
| Rare | **25** | 12 |

- **One table for all categories** — units, structures, and economy buildings share it.
  Building viability is tuned via *yields* (e.g. `supply_depot` +5/round, supplier Critical
  Mass thresholds), never via per-piece price exceptions.
- The **Dread tax is unchanged**: shop price = `discount(tierBase) + max(0, FightEquivalent − 1)`.
  Discount applies to the tier base *before* the tax is added (preserves current
  `ShopGenerator.CreateOffer` ordering).
- `ManpowerCost` and `RequisitionCost` **remain authored per piece** — they are the
  within-tier differentiators and feed the casualty/Authority systems.

Rationale for 10/15/25 over 10/20/30: eco buildings share the table, so steeper prices make
economy more necessary while making eco pieces worse investments (a 20-supply depot pays back
in 4 rounds of a ~10-fight run) — the effects cancel. Scarcity pressure over time is already
the Dread tax's job ("one clock"). Rares are appearance-gated (2–15% odds + pity); pricing
them at 30+ double-gates them.

### 2. `GoldCost` is removed, not bulk-edited

Editing 17 assets to matching numbers reopens drift the moment one is edited. Instead:

- New `Core/Shop/RarityPricing.cs`: `static int BaseCost(Rarity)` returning the three
  constants. **Single source of truth.**
- `ShopGenerator.CreateOffer` uses `RarityPricing.BaseCost(piece.Rarity)` in place of
  `piece.GoldCost`.
- `SalvageCalculator.Compute` refunds `RarityPricing.BaseCost(piece.Rarity) * 0.5` for
  Supplies (Authority refund unchanged; Dust Scourge ×1.25 unchanged).
- `PieceDefinition.GoldCost` is **deleted**, along with the serialized `goldCost` on
  `PieceDefinitionSO`, its mapping, and the `goldCost` parameters in the content factories
  (`DemoContentGenerator`, `DemoPieceFactory`, `IronmarchUnionContentFactory*`). Stale YAML
  keys in existing `.asset` files are ignored by Unity and cleaned up on next content
  regeneration.

### 3. Salvage semantics (confirmed intended)

Refund is 50% of the **base** tier cost while purchases pay the Dread-inflated price.
Late-run flips are deliberately lossy. Refunds are now three fixed numbers (5/7/12),
making salvage-loop exploits auditable by inspection.

### 4. Effects on existing pieces

Notable repricings (base, before tax): `field_medic` 10→15, `officer_quarters` 25→15,
`ironclad_field_marshal` 30→25, `ironmarch_iron_horse` 24→25, `conscript_rifleman` 12→10.
Net: early game loosens slightly; `officer_quarters` gets markedly cheaper — **watch it in
the next playtest** (its +1 Authority per command piece scales).

Save compatibility: unaffected. Prices are computed at shop-roll time and locked offers
persist their rolled `GoldPrice`; schema v10 does not serialize piece costs.

### 5. Tuning policy (guidance, not code)

- **Mis-tuned piece:** decide per piece. If its role tolerates being *seen less often*,
  move its rarity (accepting the coupled shop-odds and salvage-weighting changes). If its
  current visibility is right, adjust stats. Price is never the lever — that's the point.
- **"Economy isn't worth building":** fix the payoff side (building yields, supplier
  Critical Mass thresholds 2→+20 / 4→+45 / 6→+70 / 8→+100), never the global price table.
- Moving a tier price is a one-line change in `RarityPricing` — the intended tuning surface.

## Testing

`Core.Tests` (EditMode, deterministic, no Unity APIs):

- `RarityPricingTests` — the three constants and refund math (including Dust Scourge ×1.25
  and int truncation).
- Update `ShopGenerator` price assertions: tier base + tax + discount ordering.
- Migrate any test constructing `PieceDefinition` with `GoldCost` / asserting on it
  (`RunOrchestratorTests` salvage refund test, `VerticalSliceRegressionTests`, shop tests).

## Documentation

GDD §3 (Supplies), §6 (price formula, salvage), §11 (piece table "S" column becomes the
rarity-derived price) updated **in the same commit** as the code change, per the GDD's own
rule. This spec's tuning policy (§5) lands in the GDD's design-principles or tuning notes.

## Out of scope

- No changes to rarity odds, pity, Dread thresholds, income, or building yields.
- No fourth rarity tier (enum stays append-only; `RarityPricing` will throw on unknown values).
- No UI changes beyond whatever already displays `GoldPrice`.
