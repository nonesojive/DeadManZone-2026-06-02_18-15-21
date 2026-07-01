> **Superseded (2026-07-01):** Per-fight supply rewards via `FightRewardTable` were removed. Post-combat supplies use `FactionSO.baseSuppliesPerRound` + board bonuses only. See `docs/superpowers/specs/2026-07-01-ironmarch-union-content-pass-design.md`.

# DeadManZone — Tutorial Balance Pass Design Spec

**Date:** 2026-06-04  
**Engine:** Unity 6  
**Status:** Approved (brainstorming)  
**Builds on:** `2026-06-04-deadmanzone-combat-units-demo-design.md`  
**Scope:** Revert combat pacing, tune fights 1–3 economy and enemy templates, validate pause #2 reach rate

---

## Summary

This pass restores production combat pacing after dev-shortened segments, replaces the inflated testing economy (`startingSupplies: 400`), and softens fights **1–3** so new players reliably experience **both tactic pauses** while learning the shop loop.

**Locked decisions:**

| Area | Choice |
|------|--------|
| Balancing window | Fights **1–3** only (tutorial) |
| Starting supplies | **125** (wiggle beyond a 2-normal buy) |
| Win income model | Each of fights **1–3** grants ~**100 / 105 / 110** supplies on top of savings |
| Combat rules | **No fight-index damage/HP modifiers** — builds perform identically in all fights |
| Combat gentleness | **Enemy-side only** — composition, piece choice, placement |
| Pause #2 target | **≥90%** of reference-board sims reach pause #2 in fights 1–3 |
| Fights 4–10 | Economy rewards unchanged from current curve; enemy templates unchanged this pass |

---

## Section 1 — Scope & goals

### In scope

- Revert `CombatPacingConfig` to **50 / 300 / 50** ticks at **10 ticks/sec**
- Set `iron_vanguard.startingSupplies` to **125** (revert from dev **400**)
- Bump fight **1–3** win rewards in `FightRewardTable` to **100 / 105 / 110**
- Keep demo unit **costs** unchanged (normal 50–70, special 90, big 140–180)
- Revise enemy templates **fight_1**, **fight_2**, **fight_3** (composition + placement)
- Add headless balance validation test(s) for pause #2 reach rate

### Out of scope

- Fights **4–10** combat or economy rebalance
- Ability/tactic Authority cost changes
- Player piece stat changes
- Fight-index combat modifier code (damage scale, HP multipliers)
- Removing dev-only UI (Last Log button, etc.)

### Success criteria

1. Shop before fight 1: player can buy **2 normal units** (100) and still have **25** supplies wiggle
2. Each win in fights 1–3: **new income alone** supports another 2-normal package (~100)
3. Saving across a round: player can afford **1 big unit** (~140) without breaking the normal curve
4. **≥90%** of seeded headless sims (reference board, fights 1–3) complete the grind segment and reach pause #2
5. Combat rules are **consistent** across all fight indices — no hidden player-side nerfs/buffs

---

## Section 2 — Economy tuning

### Unit tiers (costs unchanged)

| Tier | Pieces | Cost range |
|------|--------|------------|
| Normal | Conscript Rifleman, Rifle Squad, Field Medic, Grenade Thrower | 50–70 |
| Special | Radio Array | 90 |
| Big | Diesel Walker, Armored Transport, Mobile Cannon | 140–180 |

### Supplies flow (fights 1–3)

| Moment | Supplies available | Notes |
|--------|-------------------|-------|
| Run start (shop 1) | **125** | Buy 2 normals (100) + 25 wiggle (reroll or save) |
| Win fight 1 | **+100** | Income alone = another 2-normal package |
| Win fight 2 | **+105** | Same intent |
| Win fight 3 | **+110** | Same intent; reconnects to existing curve at fight 4 |

### Fight 4+ rewards (unchanged)

Keep existing `FightRewardTable` values from fight 4 onward: **22, 25, 28, 30, 32, 35, 45** (indexed from fight 4). Tutorial generosity ends after fight 3.

### Big-unit path

Player can pursue a big unit by banking wiggle + skipping a purchase:

- Example: start 125 → buy 1 normal (50) → save 75 → win +100 → **175** total → afford Diesel Walker (140)
- No cost changes required; rewards and starting supplies create the fork naturally

### Files

| File | Change |
|------|--------|
| `Assets/_Project/Data/Resources/DeadManZone/Factions/iron_vanguard.asset` | `startingSupplies: 125` |
| `Assets/_Project/Game/FightRewardTable.cs` | Fights 1–3 supplies → 100, 105, 110 |

---

## Section 3 — Combat & enemy templates

### Pacing revert

| Segment | Ticks | Wall-clock @ 10/sec |
|---------|-------|---------------------|
| Opening (deployment) | 50 | ~5s |
| Grind (main fight) | 300 | ~30s |
| Brief push | 50 | ~5s |
| Gas | Until winner | — |

Remove temporary dev comment and values (`OpeningTicks = 30`, `MainFightTicks = 30`). Restore spec values: **50 / 300 / 50**.

Segment damage scales unchanged:

- Opening: **0.2×**
- Grind / brief push: **1.0×**

### Design principle: enemy-side only

Tutorial fights are softer because the **enemy fields less threatening boards**, not because player units behave differently. A Diesel Walker bought in fight 1 deals the same damage in fight 1 and fight 10.

### Proposed enemy templates (fights 1–3)

| Fight | Display name | Units | Placement notes |
|-------|--------------|-------|-----------------|
| **1** | Conscript Line | HQ + **1** Conscript Rifleman | Conscript in **support** (~x=4), not front line |
| **2** | Patrol | HQ + 1 Conscript + **1 Field Medic** | Both in support; medic adds body, low DPS (8 base) |
| **3** | Field Support | HQ + **2** Conscript Riflemen | Support/rear anchors (~x=4, x=5); no grenade |

**HQ placement:** Unchanged rear anchor `(0, 4)` with `instanceId: enemy_hq`.

**Removals from tutorial fights:**

- Grenade Thrower deferred to **fight 4+** (fight_2 currently fields grenade — remove)
- Fight 3 currently fields grenade — remove; keep low-DPS roster only

### Difficulty step at fight 4

Fights 4–10 enemy templates are **not** changed in this pass. Players should feel a deliberate step up after the tutorial trio. Future passes can smooth the fight 3→4 transition if needed.

### Reference board (balance validation)

Headless sims use a fixed loadout representing a typical shop-1 purchase:

- Player HQ (auto-spawned)
- **2× Conscript Rifleman** placed on board
- Tactic: **Disciplined Fire** (default)
- No abilities selected

### Pause #2 metric

**Reach pause #2** = grind segment runs to completion without `fight_end` during `CombatPhase.Grind`.

**Target:** ≥90% across a seed sweep (recommend 32–50 seeds per fight).

**Acceptable deviations:**

- Optimized boards may end grind early via **victory** — does not fail the pass (rules-consistent)
- Occasional RNG losses &lt;10% — acceptable

### Files

| File | Change |
|------|--------|
| `Assets/_Project/Core/Combat/CombatPacingConfig.cs` | Revert to 50 / 300 / 50 |
| `Assets/_Project/Data/Resources/DeadManZone/Enemies/fight_1.asset` | 1 conscript, support placement |
| `Assets/_Project/Data/Resources/DeadManZone/Enemies/fight_2.asset` | Rename to Patrol; conscript + medic |
| `Assets/_Project/Data/Resources/DeadManZone/Enemies/fight_3.asset` | 2 conscripts; remove grenade |

---

## Section 4 — Implementation & testing

### Implementation order

1. **Pacing revert** — `CombatPacingConfig` (unblocks accurate sim timing)
2. **Economy** — `iron_vanguard.asset`, `FightRewardTable` fights 1–3
3. **Enemy templates** — `fight_1` through `fight_3` assets
4. **Balance test** — headless pause #2 validation
5. **Regression** — existing EditMode / vertical-slice tests updated if they assert old reward or pacing values

### Balance test (new)

Add `TutorialBalanceTests` (or extend existing combat integration tests):

```
For fightIndex in 1..3:
  For each seed in seedSweep:
    Build reference player board (HQ + 2 conscripts)
    Run headless combat through deployment segment + grind segment
    Assert: LastCompletedPhase reaches Grind AND grind did not end early via fight_end
    OR: segment playback budget completes for Grind phase
```

Report per-fight pass rate; CI assertion: pass rate ≥ 0.90 per fight.

If a fight falls below 90% after content-only changes, adjust **enemy count/placement** further — do **not** add fight-index damage modifiers.

### Manual playtest checklist

- [ ] Shop 1: buy 2 normals from 125, reroll once if desired
- [ ] Fight 1: see opening replay (~5s) → pause 1 → grind (~30s) → pause 2 → brief push → gas/results
- [ ] Win fight 1: +100 supplies visible on battle report
- [ ] Fight 2–3: same pause flow on reference-style board
- [ ] Strong board (big unit early): units deal same damage as in later fights
- [ ] Fight 4: noticeably tougher than fight 3 (expected)

### Tests to update

| Test area | Likely change |
|-----------|---------------|
| `CombatSegmentPlaybackTests` | Budget assertions use restored `MainFightTicks` (300) |
| `FightRewardTable` tests (if any) | Fights 1–3 expected supplies |
| `VerticalSliceRegressionTests` | Uses `faction.startingSupplies` from asset — should flow through at 125 |
| Any hardcoded `400` or `15` supply assertions | Update to new values |

---

## Design decisions log

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Tutorial window | Fights 1–3 | User-confirmed scope |
| Starting supplies | 125 | 2 normals + wiggle; user preference over 100 |
| Income model | ~100 per early win | Supports 2-normal shop rhythm on top of savings |
| Combat consistency | No fight-index modifiers | Savvy players see identical build performance |
| Softness lever | Enemy templates only | Transparent, content-tunable, no hidden code |
| Grenade on fight 2 | Removed | Too much early DPS for tutorial pause goal |
| Fight 4+ | Unchanged this pass | Clear tutorial cliff; avoids scope creep |

---

## Success criteria (this pass)

- Pacing restored to 50 / 300 / 50
- Starting supplies 125; fight 1–3 rewards 100 / 105 / 110
- Fights 1–3 enemy templates match Section 3 table
- Headless balance test ≥90% pause #2 reach on reference board
- No fight-index combat modifier code added
- All existing EditMode tests pass after value updates
