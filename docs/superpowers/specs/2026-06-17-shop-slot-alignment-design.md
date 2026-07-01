# DeadManZone — Shop Slot Alignment Design (Data-Driven)

**Date:** 2026-06-17  
**Status:** Approved — implemented 2026-06-17  
**Builds on:** `2026-06-13-shop-screen-revamp-design.md`  
**Approach:** Option 2 — `ShopSlotProfile` ScriptableObjects + unlock providers  
**Scope:** Core shop generation, slot layout, source weights, specialty lane retirement, presentation grid constants

---

## Summary

Align the shop with the target model: **8 baseline slots** always visible, **slots 9–12** only when unlocked by rare criteria (none active in this build). Each slot rolls from a **data-driven profile** with default source weights **10% neutral / 80% player faction / 10% salvage** (salvage leg layered with fight aftermath + board modifiers). Slots **1–4** bias aggressive combat roles; slots **5–8** bias defensive/utility. **Retire `ShopLane.Specialty`** for generation — specialty-tagged pieces register into offensive or defensive pools via `ShopLaneResolver`.

---

## Locked design decisions

| Topic | Decision |
|-------|----------|
| Salvage | 10% baseline per slot; `SalvageChanceCalculator.Compute(base, boardBoost)` **replaces** the salvage leg; neutral fixed at 10%; faction absorbs remainder |
| Specialty lane | Retired for shop gen; pieces route to Offensive/Defensive by combat role |
| Neutral / faction (non-salvage) | Fixed **10 / 80** for the run; piece abilities patch weights later |
| Extra slots | Indices 8–11; unlocked only via `IShopSlotUnlockProvider`; **zero providers ship in this milestone** |
| Fight-index neutral escalation | Removed (`ShopPoolFilter.GetNeutralWeight` deleted) |

---

## Architecture

### Layer separation

```
ContentDatabase / FactionSO
        ↓
ShopConfigSO (default profiles + unlock provider list)
        ↓
ShopSlotLayoutResolver  →  active slots + per-slot ShopSlotProfile
        ↓
ShopOfferWeightResolver   →  merge profile weights + run salvage state + ability patches
        ↓
ShopOfferSourceRoller     →  pick Neutral | Faction | Salvage
        ↓
ShopPiecePicker           →  weighted pick within pool + role bias
        ↓
ShopGenerator             →  ShopOffer list
```

Core stays Unity-free. `DeadManZone.Data` holds ScriptableObjects that map to core structs at load time.

---

## Data model

### `ShopOfferWeights` (Core struct)

```csharp
public readonly struct ShopOfferWeights
{
    public int NeutralPercent { get; }   // default 10
    public int FactionPercent { get; }   // default 80 (renormalized after salvage)
    public int SalvagePercent { get; }   // default 10; overridden by SalvageChanceCalculator

    public static ShopOfferWeights Default => new(10, 80, 10);

    public ShopOfferWeights WithSalvageLeg(int salvagePercent);
    public ShopOfferWeights ApplyPatch(ShopOfferWeightPatch patch);
}
```

**Renormalization rule:** After salvage leg is set from `SalvageChanceCalculator.Compute(baseSalvagePercent, boardBoost)` (faction base + combat-board boost, cap 50%):

- `neutral = profile.NeutralPercent` (default 10, unchanged by fight aftermath)
- `salvage = computed value` (0 if no `lastEnemyFactionId`)
- `faction = max(0, 100 - neutral - salvage)`

Ability patches (future) add deltas to any leg, then clamp and renormalize the same way.

### `ShopSlotProfile` (Core struct)

Immutable runtime profile resolved from SO + faction override:

```csharp
public sealed class ShopSlotProfile
{
    public int SlotIndex { get; }
    public ShopSlotKind Kind { get; }           // BaselineOffensive, BaselineDefensive, Bonus
    public ShopPoolBias PoolBias { get; }       // Offensive, Defensive
    public ShopOfferWeights BaseWeights { get; }
    public float PreferredRoleWeight { get; }   // multiplier for in-bias roles (default 2f)
    public string[] PreferredCombatRoles { get; }  // from profile SO
}
```

### `ShopSlotProfileSO` (Data)

```csharp
[CreateAssetMenu(menuName = "DeadManZone/Shop/Slot Profile")]
public class ShopSlotProfileSO : ScriptableObject
{
    public int slotIndex;                       // 0–11
    public ShopSlotKind slotKind;
    public ShopPoolBias poolBias;
    public int neutralPercent = 10;
    public int factionPercent = 80;
    public int salvagePercent = 10;           // baseline; runtime salvage leg overrides
    public float preferredRoleWeight = 2f;
    public string[] preferredCombatRoles;       // e.g. assault, sniper, tank
}
```

### `ShopConfigSO` (Data)

Single config asset referenced by `ContentDatabase` or loaded from `Resources/DeadManZone/ShopConfig`:

```csharp
[CreateAssetMenu(menuName = "DeadManZone/Shop/Config")]
public class ShopConfigSO : ScriptableObject
{
    public ShopSlotProfileSO[] baselineProfiles;   // 8 entries, indices 0–7
    public ShopSlotProfileSO[] bonusProfiles;      // 4 entries, indices 8–11
    // ponytail: unlock providers authored as SO refs later; empty array for now
}
```

### `FactionShopOverrideSO` (Data, optional per faction)

```csharp
[CreateAssetMenu(menuName = "DeadManZone/Shop/Faction Override")]
public class FactionShopOverrideSO : ScriptableObject
{
    public string factionId;
    public ShopSlotProfileSO[] slotOverrides;  // sparse: only slots that differ from default
    public ShopSlotUnlockRuleSO[] unlockRules; // future: Cartel +2 slots, etc.
}
```

`FactionSO` gains optional `FactionShopOverrideSO shopOverride` reference (null = use default config).

### Slot layout constants

| Constant | Value |
|----------|-------|
| `BaselineSlotCount` | 8 |
| `MaxSlotCount` | 12 |
| `BonusSlotCount` | 4 |

| Slot index (UI) | Kind | Pool bias | Preferred roles |
|-----------------|------|-----------|-----------------|
| 0–3 (slots 1–4) | BaselineOffensive | Offensive | Assault, Sniper, Tank |
| 4–7 (slots 5–8) | BaselineDefensive | Defensive | Support, Utility, Defender, Headquarters |
| 8–11 (slots 9–12) | Bonus | Configurable per unlock rule | Per bonus profile SO |

### Grid layout (`ShopSlotLayoutResolver.GetGridShape`)

| Active slots | Grid |
|--------------|------|
| 8 | 4 × 2 |
| 9–12 | 4 × 3 |

---

## Unlock system (infrastructure only this milestone)

```csharp
public interface IShopSlotUnlockProvider
{
    IReadOnlyList<ShopSlotUnlock> Evaluate(ShopUnlockContext context);
}

public readonly struct ShopSlotUnlock
{
    public int SlotIndex { get; }
    public ShopSlotProfile Profile { get; }  // or profile SO id
}
```

`ShopUnlockContext`: board, factionId, registry, modifiers.

**This build:** `ShopSlotUnlockRegistry` returns empty. No Cartel rules, no `UnlockShopSlot` building flag wired.

**Retire for slot unlocks:**

- `SpecialtyLaneUnlock` adding specialty slots
- `ShopModifierFlags.ExtraGeneralSlot` → extra shop slots (flag may remain for save compat but **ignored by layout resolver**)

**Keep (non-slot effects):**

- `GoldDiscount10`, `EnemyTagPreview`, `GuaranteeEngineerOffer`, `SalvageChanceBoost5` (board boost into calculator)

---

## Pool registration changes

`ContentDatabase.BuildRegistry()`:

```csharp
var lane = ShopLaneResolver.ResolveLane(piece.combatRole);
// Map Specialty → Offensive or Defensive; never register into ShopLane.Specialty
if (lane == ShopLane.Specialty)
    lane = ShopLane.Offensive; // or re-resolve: artillery → offensive per catalog
registry.Register(piece.ToCore(), lane, includeInShopPool: piece.includeInShopPool);
```

`ShopLane.Specialty` enum value **deprecated** but kept for compile migration; no pieces in specialty pool after migration.

---

## Roll algorithm (per slot)

1. Resolve `ShopSlotProfile` for slot index (config + faction override + bonus unlock).
2. Build weights: `ShopOfferWeightResolver.Resolve(profile, salvageChancePercent, abilityPatches)`.
3. Roll source: `ShopOfferSourceRoller.Roll(weights, rng)`.
4. Build candidate pool:
   - Filter `registry.GetPool(poolBias)` by source faction (`neutral`, `playerFactionId`, `lastEnemyFactionId`).
   - Exclude already-consumed piece ids (non-salvage dedup).
5. Pick piece: `ShopPiecePicker.PickWeighted(candidates, preferredRoles, preferredRoleWeight, rng)`.
6. Create offer: gold + requisition from piece data (Authority not lane-gated).

**Salvage:** `IsSalvaged = true` when source is Salvage. Uses enemy faction pieces from the same pool bias (offensive/defensive), not a separate salvage lane filter.

**Fight 1:** `salvageChancePercent` or missing `lastEnemyFactionId` → salvage leg 0.

---

## Files to add / change

### New Core

| File | Purpose |
|------|---------|
| `ShopOfferWeights.cs` | Weight struct + renormalization |
| `ShopOfferWeightResolver.cs` | Profile + salvage + patches → final weights |
| `ShopOfferSourceRoller.cs` | Tri-source RNG |
| `ShopPiecePicker.cs` | Role-biased piece pick |
| `ShopPoolBias.cs` | Offensive / Defensive enum |
| `ShopSlotProfile.cs` | Runtime profile struct |
| `ShopSlotUnlock.cs` | Unlock result struct |
| `IShopSlotUnlockProvider.cs` | Unlock interface |
| `ShopSlotUnlockRegistry.cs` | Aggregates providers (empty list default) |
| `ShopUnlockContext.cs` | Context for unlock evaluation |

### New Data

| File | Purpose |
|------|---------|
| `ShopSlotProfileSO.cs` | Per-slot authoring |
| `ShopConfigSO.cs` | Baseline + bonus profile arrays |
| `FactionShopOverrideSO.cs` | Faction-specific overrides |
| `Resources/DeadManZone/ShopConfig.asset` | 8 baseline + 4 bonus profile refs |
| Editor menu or generator to create default 12 profile assets |

### Modified

| File | Change |
|------|--------|
| `ShopSlotLayoutResolver.cs` | 8 baseline; resolve profiles from config; bonus via unlock registry |
| `ShopGenerator.cs` | Use profile-driven roll; remove specialty paths |
| `ShopPoolFilter.cs` | Remove fight-index neutral weight; delegate to new picker or delete |
| `ContentDatabase.cs` | Specialty → offensive/defensive mapping; load ShopConfig |
| `FactionSO.cs` | Optional `shopOverride` ref |
| `BuildLayoutMetrics.cs` | `BaselineShopSlots = 8` |
| `ShopLayoutMetrics.cs` | Grid shape for 8 slots |
| `SpecialtyLaneUnlock.cs` | Remove slot unlock usage (delete or gut) |
| Tests | New + update existing shop tests |

---

## Testing (TDD)

### `ShopOfferWeightResolverTests`

- Default profile → 10/80/10
- Salvage calculator 25% → neutral 10, salvage 25, faction 65
- No enemy faction → salvage 0, faction 90
- Patch +5 salvage (future hook) → correct renormalization

### `ShopSlotLayoutResolverTests`

- Default board + empty unlock registry → 8 slots, indices 0–7
- Bonus unlock mock provider → 9+ slots
- Command Bunker on board → still 8 slots

### `ShopGeneratorTests`

- No specialty lane offers
- Same seed → same offers
- Slots 0–3 offensive role bias (statistical over 200 seeds)
- Slots 4–7 defensive role bias

### `SalvageShopGeneratorTests`

- Update for tri-roll model: 100% salvage leg → all salvaged
- Layered: calculator output affects salvage rate

### `ContentDatabaseTests` (or existing content test)

- No pieces registered to `ShopLane.Specialty`

---

## Presentation

- `BuildLayoutMetrics.BaselineShopSlots = 8`
- `ShopSlotLayoutResolver.GetGridShape(8)` → (4, 2)
- Remove tooltip copy referencing “extra shop slot” from `ExtraGeneralSlots` unless re-purposed for future unlock UI
- `ShopView` reads offer count from state, not hardcoded 6

---

## Migration / compatibility

- **Saves:** Unsupported old lane-based shop (consistent with existing policy).
- **`ShopLane` on `ShopOffer`:** Retained for display/debug; set from `PoolBias`, not gameplay gate.
- **`ShopModifierFlags.ExtraGeneralSlot`:** Ignored for layout; consider rename to `UnlockShopSlot` in a follow-up.
- **GDD §10:** Update fight-index weighting table to 10/80/10 + salvage layer (separate doc pass).

---

## Out of scope (follow-up milestones)

- `FactionShopOverrideSO` assets for Cartel of Echoes (+2 slots, cross-faction slot rule)
- Building `UnlockShopSlot` modifier wired to unlock provider
- Piece ability `ShopOfferWeightPatch` application
- Authority pricing UI changes beyond existing per-piece data

---

## Implementation order

1. Core weight + roll types + tests  
2. `ShopSlotProfile` resolution + layout resolver tests  
3. Data SOs + default `ShopConfig` asset (Editor generator)  
4. `ShopGenerator` integration + salvage/content tests  
5. Presentation constants + smoke playtest  

---

## Risks

| Risk | Mitigation |
|------|------------|
| 12 profile assets are tedious | Editor `Create Default Shop Profiles` menu item |
| Statistical bias tests flaky | Use seeded sweep + tolerance bands, or mock RNG |
| Specialty pieces mis-laned | Audit `ShopLaneResolver` for artillery → offensive default |
| Weight sum ≠ 100 | Assert in resolver; clamp in tests |

---

## Spec self-review

- No TBD placeholders in roll algorithm or constants  
- Salvage layering matches user choice B  
- Approach 2 fully specified with SO types and unlock seam  
- Scope bounded: no unlock providers ship active  
- Consistent with 8 baseline / 12 max from user request  
