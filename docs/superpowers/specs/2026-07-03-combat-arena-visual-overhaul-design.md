# CombatArena2D Visual Overhaul - Design Spec

**Date:** 2026-07-03  
**Milestone:** IronMarch Union vertical slice, combat presentation first  
**User-selected approach:** Visual Overhaul Pass  
**Primary goal:** Make CombatArena2D fights readable, weighty, and emotionally clear without changing the deterministic combat sim.

---

## Context

DeadManZone is currently narrowed to an IronMarch Union vertical slice: one playable faction, a 17-piece roster, deterministic `TickCombatRun` combat, and `CombatArena2D` as the active fight presentation. The full demo target remains a 10-fight gauntlet, but this milestone intentionally starts with combat presentation quality because a technically complete loop is not useful if players cannot understand deaths, attacks, command pauses, or why they won or lost.

Existing architecture is already aligned with the target:

- `DeadManZone.Core` owns the deterministic sim and `CombatEventLog`.
- `CombatDirector` replays combat events.
- `CombatArenaPresenter` consumes replay events and drives actors, VFX, audio, and death presentation waits.
- `CombatUnitActor`, `CombatUnitVisual2D`, and `CombatUnit2DStripPlayer` already support sprite-based movement, attack, and death states.
- `CombatFlowPresenter` already waits for pending death presentations before unloading the arena.

This spec extends those systems surgically. It does not introduce a new renderer, bypass replay, or rewrite combat rules.

---

## Success Criteria

The milestone is successful when fights 1-3 can be watched by a new player and the following are true:

- Unit identity and side ownership are readable at combat scale.
- Bulwark Squad and other key IronMarch units visibly progress through idle, move, attack/shoot, hit reaction, and death states where assets exist.
- Deaths are clear, satisfying, and never skipped by arena reset/unload.
- VFX and damage text reinforce what happened without hiding silhouettes.
- Command pauses feel like tactical beats, not generic modal interruptions.
- Battle report feedback reinforces what the player just saw: outcome, casualties, top damage, morale/resource changes.
- Presentation remains a replay of `CombatEventLog`; deterministic outcome and save/resume behavior remain unchanged.

---

## Scope

### In Scope

- Animation state polish for `CombatUnit2DStripPlayer` and `CombatUnitVisual2D`.
- Runtime selection between walk and run strips when the actor is visibly closing distance.
- Hit reaction / hurt playback for damage and graze events, with safe fallback to existing behavior when strips are missing.
- Death presentation timing, lock behavior, final-frame hold, death puff timing, and actor release safety.
- VFX readability tuning for tracers, impact flashes, explosions, damage numbers, and death effects.
- Audio presentation tuning through the existing `CombatArenaAudioPresenter` event surface.
- Camera/framing config tuning through `CombatArenaConfigSO` and existing orthographic framer.
- Tactic pause presentation polish using existing `TacticPausePanel` and freeze flow.
- Focused EditMode and PlayMode tests before implementation.

### Out Of Scope

- Changing `TickCombatRun`, combat damage formulas, targeting, pathfinding, or pause trigger rules.
- Full 10-fight gauntlet tuning.
- Reserves `2x9` migration.
- Emergency Draft, shop, manpower gate, or full build-phase UX polish.
- New faction content, new enemy systems, or new economy systems.
- Replacing the current CombatArena2D architecture with a different renderer.

---

## Approach

### Recommended Architecture

Keep the existing replay pipeline intact:

```text
TickCombatRun
  -> CombatEventLog
  -> CombatDirector.EventReplayed
  -> CombatArenaPresenter.ApplyEventVisual
  -> CombatUnitActor / CombatUnitVisual2D / CombatArena2DVfx / CombatArenaAudioPresenter
```

Visual polish happens only in the final presentation layer. The sim remains authoritative, and every visual effect is interpreted from a replay event or pause state.

### Visual Grammar

Each combat event should produce a consistent and learnable visual result:

| Event | Presentation Rule |
| --- | --- |
| `move` | Actor changes replay anchor; free-chase or interpolation shows readable forward motion. |
| `damage` | Attacker plays shoot/attack, tracer or muzzle cue fires, impact lands, target plays hit reaction, damage text rises. |
| `graze` | Same chain as damage, but lighter damage text and smaller impact intensity. |
| `miss` | Attacker fires, tracer lands near target or no impact damage text appears. |
| `gas_damage` | Target shows hurt feedback and damage text without implying a weapon muzzle. |
| `destroyed` | Actor locks locomotion, plays death strip or fallback death routine, holds final frame long enough to read, then death puff/audio fires and actor releases. |
| command pause | Arena freezes, camera and UI emphasize the tactical moment, available tactics/abilities are clear. |

### Animation Rules

- `Die` remains locked and does not transition back to idle.
- One-shot `Shoot`, `Hurt`, and `HitReact` return to locomotion or idle after completion.
- `Run` may be used only when a valid run strip exists and movement speed/distance makes it visually distinct.
- Missing optional strips degrade in this order:
  - `Run` -> `Walk`
  - `HitReact` -> `Hurt`
  - `Hurt` -> no-op with existing damage VFX
  - `Die` -> fallback scale/fade death routine
- Bulwark Squad receives extra validation because it is the key readability test case for the IronMarch front line.

---

## Component Changes

### `CombatUnit2DStripPlayer`

Add test-backed behavior for:

- One-shot return behavior from `Shoot`, `Hurt`, and `HitReact`.
- `Die` final-frame lock.
- Fallback resolution when optional strips are missing.
- A query/helper that lets callers know whether a state is playable before requesting run or hit-react variants.

### `CombatUnitVisual2D`

Implement `PlayHurt()` instead of leaving it disabled.

Expected behavior:

- Prefer `HitReact` if available for large visible hits.
- Use `Hurt` for normal damage and graze.
- Do not interrupt `Die`.
- Do not break attack timing; hit reactions may briefly lock locomotion but must return cleanly.
- Keep sprite facing and sort order stable during hit/death playback.

### `CombatUnitActor`

Add movement presentation selection without changing sim anchors:

- Continue using replay anchors and free-chase target logic.
- When movement is active and the unit is closing a meaningful distance, request `Run` if the visual can play it.
- Otherwise keep `Walk`.
- Never move while death or locked one-shot presentation requires the actor to remain readable.

### `CombatArenaPresenter`

Keep event mapping intact, but polish timing:

- Treat `gas_damage` as environmental damage: play damage text and target hurt without a false muzzle/tracer.
- Tie death puff/audio delay to the actual death presentation duration when possible instead of a fixed long delay.
- Ensure `_pendingDeathPresentations` cannot underflow if pooled actors are cleared during scene unload.
- Preserve `WaitForPendingDeathPresentations()` as the unload gate.

### `CombatArena2DVfx`

Tune for readability over density:

- Cap simultaneous floating damage text or stagger overlapping text positions.
- Keep rifle tracers thin and fast; keep cannon/explosion effects broader but shorter-lived.
- Distinguish graze from full damage using smaller text or lower intensity.
- Prefer existing strips and simple pooled/reused objects only where it reduces visible clutter or allocations without a large rewrite.

### `TacticPausePanel` / Pause Presentation

Improve the tactical beat using existing UI:

- Opening pause copy should clearly say this is pre-battle doctrine selection.
- Mid-fight pause should communicate the army HP trigger and remaining authority.
- Available tactic and ability buttons should visibly distinguish affordable, unaffordable, and selected states.
- The frozen arena remains visible behind the panel so the player understands the current battlefield context.

### `BattleReportPresenter`

Keep the report concise and educational:

- Outcome line: victory, defeat, or draw.
- Casualties and morale delta near the top.
- Top damage dealt/taken in plain language.
- Supplies/manpower gained shown as outcome-independent income.
- One short feedback line based on report data, such as heavy casualties or a standout damage dealer.

---

## Testing Strategy

Use the requested TDD workflow. Tests must be written and observed failing before implementation.

### EditMode Tests

- `CombatUnit2DStripPlayerTests`
  - `Shoot` returns to idle after its non-looping strip ends.
  - `Hurt` or `HitReact` returns to idle/walk after completion.
  - `Die` remains locked on its final frame.
  - `Run` fallback does not break when the run strip is missing.

- `CombatArena2DHelpersTests`
  - Bulwark Squad animation set has valid idle, walk, shoot, and die strips.
  - Optional hurt/run strips are either valid or explicitly handled by fallback tests.

### PlayMode Tests

- Destroyed event keeps an actor present until death presentation completes, then releases it.
- Combat completion waits for pending death presentations before unloading the arena.
- Damage event triggers attack presentation, impact VFX/audio path, and victim hit reaction when available.
- Gas damage does not create a false weapon muzzle/tracer.
- Tactic pause freezes arena presentation and resumes cleanly after submission.

### Manual Verification

Play fights 1-3 from a fresh IronMarch Union run and capture:

- Build phase with Critical Mass drawer and income HUD, if already reachable.
- Combat during opening or mid-fight pause.
- A clear death moment with the dying unit still visible.
- Battle report after combat.

For this milestone, the required claim is not "full 10-fight gauntlet complete." The required claim is "CombatArena2D presentation is readable and trustworthy enough to support the next gauntlet/economy pass."

---

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| Visual polish hides deterministic replay bugs. | Keep replay event mapping unchanged and add PlayMode tests around destroyed/damage/pause behavior. |
| Missing animation strips create silent no-ops. | Add fallback tests and asset validation for Bulwark Squad. |
| VFX/audio become noisy and reduce readability. | Tune intensity caps and prioritize silhouette clarity over spectacle. |
| Death timing becomes brittle. | Derive death VFX/release from visual completion rather than relying on fixed delays. |
| Scope expands back into full vertical slice. | Defer economy, reserves, Emergency Draft, and 10-fight tuning to the next milestone. |

---

## Decision Ledger

- The first implementation milestone is **CombatArena2D visual overhaul**, not full 10-fight delivery.
- The deterministic sim remains untouched.
- Existing `CombatArena2D` renderer and replay architecture are extended, not replaced.
- Tests guard replay trust before visual changes land.
- The next milestone after this should be build/economy clarity plus fights 1-3 full loop validation, then full 10-fight gauntlet tuning.

