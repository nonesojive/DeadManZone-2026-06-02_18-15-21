# Art Direction Readability Audit — Design

**Date:** 2026-07-14 · **Status:** approved in brainstorming session, pending implementation plan
**Amends:** `docs/art/style-bible/00-style-bible.md` (§5, §6), `docs/art/style-bible/50-combat-arena-spec.md` (§8, pause behavior)
**Does NOT touch:** ADR-0001/0002/0003 (all reconfirmed), the Core sim's rules — except one flagged
Core bug (§6.1) which is a rule-intent fix, not an art change.

## 0. Why this document exists

Stress-test prompted by a research principle: *pick the art style's complexity budget from the core
mechanic's readability requirement, not from a generic "make it look good" brief.* We re-derived
DeadManZone's art direction from scratch, starting from "what must the player reliably perceive at
a glance," and compared the result against the existing style bible.

**Verdict: the bible's laws are right; its scope was wrong.** Both of its parents (Top Troops,
Darkest Dungeon) are combat-presentation games, so the bible answered "how do I parse a 20-unit
battle?" — but per GDD §14.1 the game is the **Build phase**; the fight is the consequence. The
bible art-directed the consequence and under-directed the decisions. Everything below fills that
gap. No existing law is repealed.

## 1. The derivation — perceptual load, ranked by cost of a mis-read

**Tier 1 — mis-read loses the run:**
1. **Morale state and the rout/death distinction.** Routed units cost 0 Manpower, dead cost full
   (GDD §3); Manpower is the only run-ending resource; death shock cascades within 2 cells. The
   player watches, they don't act — perception *is* their job in combat.
2. **Critical Mass counts and thresholds.** GDD §14.6: "you buy counts, not units… tag legibility
   in the UI is a first-class concern."

**Tier 2 — mis-read loses a fight:** side allegiance (faction ≠ side); archetype at battle
distance; the tactical-pause read (parse everything, spend Authority).

**Tier 3 — mis-read costs efficiency:** adjacency/packing, army HP trend, statuses, gas ramp,
attack-type matchups.

**Confirmed as-is by the derivation:** silhouette law / black-shape test, reserved blue/red side
channel, saturation budget with VFX on top, two status pipelines, grid fade during playback,
two escalation tiers with no full-screen interrupts, worn HUD chrome with the max-contrast
exception for mechanical overlays.

## 2. Morale visual channel *(new — amends bible §5, arena spec §4/§6)*

Feasibility verified: the sim already logs `morale_damage` and `rout` events
(`TickCombatRun.cs:494,502`) and `CombatantState` carries per-unit morale. Everything here is
Presentation-layer.

### 2.1 Continuous read — ring integrity, progressive disclosure
The side-color base ring carries morale as **physical integrity, not color**:

| Morale fraction | Ring state |
|---|---|
| ≥ ⅔ (healthy) | Solid — exactly the existing spec |
| ⅓–⅔ (shaken) | Frays — notched, small flickers |
| < ⅓ (breaking) | Gutters — sputters like a dying flame |

- Achromatic on purpose: shape/flicker only, hue untouched. Costs nothing from the saturation
  budget, can't collide with status colors, and the blue/red allegiance read survives to the end.
- Lawful under bible §5.4 (modify existing channels first — base-ring pulse is its named example).
- Driven per-instance off morale fraction via `MaterialPropertyBlock` (same mechanism as the
  uber-shader status hooks). Band thresholds are presentation constants, playtest-tunable.

### 2.2 The break — grammatically opposite exits
- **Death** (specced already): dissolve **in place**.
- **Rout** (new): weapon drops, unit turns and **flees toward its own board edge**, ring gutters
  out mid-flight. One retargeted flee clip covers the shared-rig infantry roster.
- Acceptance: at full battle distance, silhouette motion alone tells you which economy fired.
- Individual breaks are ambient. A **cascade — 3+ breaks within ~2s — earns one punch-in**, under
  existing discipline (one active, ~2s spacing, disabled in fast-forward).

### 2.3 Pause-amplified read
While paused, progressive disclosure relaxes: every unit shows a full morale bar under its HP bar;
hovering a unit outlines its 2-cell (Chebyshev) death-shock radius. The cascade math is in front of
the player at the only moment they can spend Authority on it.

### 2.4 Aftermath reinforcement
The casualty report separates **Died** (full Manpower cost, red) from **Fled** (0 cost,
ash-neutral) — the routing-saves-lives economy is taught after every fight.

### 2.5 Risk
Guttering must read at 20+ units without becoming screen noise → judged in the same Phase 0
screenshot review as the ink (§5). One spike, two verdicts.

## 3. Build-phase threshold legibility *(new — amends bible §6)*

**Law:** the Build screen gets the same art-direction rigor as the arena. Acceptance test, parallel
to the black-shape test: *a player who knows the game can answer "what should I buy?" from screen
state without opening a single tooltip.* Threshold meters, tag chips, and placement previews are
mechanical overlays under the existing §6 exception — max-contrast information design.

### 3.1 Tag vocabulary
One fixed icon per tag, shape-coded by category (primary / role / attack type / synergy),
monochrome-on-dark at rest. Same glyph everywhere a tag appears (offer cards, hovercards,
crit-mass chips, unit badges). No per-tag hue — color is reserved for state.

### 3.2 Critical Mass strip as progress instrument
Each chip reads `count / next threshold` with three states:
- **Dormant** — grey, below any tier.
- **Armed** — one piece from a tier: warm glow. This is the "go shopping" signal and the point of
  the whole feature.
- **Active** — tier lit, gold (existing buff vocabulary).

### 3.3 Shop offers answer the threshold question in two stages
- **Passive:** an offer that would advance an *armed* tag carries the armed glow echoed on the
  card edge — same language as the strip, no new vocabulary.
- **Hover:** the hovercard gains a counts block with exact math, e.g.
  `ASSAULT 4 → 5 · Tier 1: +1 dmg lights`.
The marker triggers the hover; the hover teaches the system.

### 3.4 Placement telegraphs adjacency live
While dragging, any placed neighbour the drop would synergize with lights a connection read (edge
glow on touching cells); post-drop the connection pulses once to confirm. Reuses the drag ghost +
`BoardAdjacency.GetTouchingPairs`. Presentation-only.

### 3.5 Scope communication
After the Core fix (§6.1), chips count Combat + HQ. Chip tooltip: "counts pieces in play — reserves
don't count." The future tutorial owns teaching it. (Explicit decision: no purchase-moment warnings,
no ghost segments — tooltip-only.)

## 4. Tactical pause — the briefing register *(new — amends arena spec)*

**Playback is the theater; pause is the board.**

- **The grid returns** partway on pause (it is faded to diegetic markings during playback) — the
  placement-phase language reappears exactly when the player is deciding again.
- **Info layer amplifies:** full morale bars (§2.3), death-shock hover radius, army summaries; bars
  render slightly larger — nothing is moving, parse speed beats subtlety.
- **Mood layer steps down:** grade cools, environment darkens a step, units stay lit. No camera
  motion — punch-ins remain the only camera channel.
- **Order cards** carry their Authority price in the worn-chrome register. Orders are army-wide;
  no targeting UI.

## 5. Interior ink — Phase 0 wording amendment *(arena spec §8)*

The kill/keep gate is judged **at final combat screen size with 20+ units on field**, with three
explicit outcomes:

1. **Full pass** — interior ink reads as inked illustration at battle distance without silhouette
   noise → proceed as specced.
2. **Close-camera pass** — ink reads near, is invisible/noisy far → **two-tier surface**: interior
   ink lives where the camera is close (shop cards, hovercards, portraits, punch-ins); battlefield
   models carry exterior outline + 2–3 band cel only. **A legal outcome, not a failure** — the DD
   skin is delivered at the distances the eye can receive it.
3. **Fail** — generic toon at every distance → revisit ADR-0002 before roster work (as specced).

The same review judges §2.5 (guttering-ring legibility).

## 6. Companion items — NOT art, flagged during this audit

### 6.1 Critical Mass scope bug (Core)
`TickCombatRun.cs:79` evaluates Critical Mass against the **combat board only**. Owner intent
(stated 2026-07-14): count **everything in play — Combat AND HQ boards — excluding only reserves.**
The two run-resource rules (`command`, `supplier`) already count both boards; combat rules must
match. Requires: evaluator fed both boards, `Core.Tests` coverage, **GDD §9 updated in the same
commit** (per GDD contract).

### 6.2 `ProtectSupport` no-op (known, GDD §15)
The pause order UI must not display a broken order — fix the zone bug or hide the card before the
§4 register ships.

## 7. Build cost summary

| Item | Layer | Cost |
|---|---|---|
| Ring morale states | Presentation (shader states) | Small |
| Flee clip + rout exit | Presentation (1 retargeted clip) | Small |
| Cascade punch-in trigger | Presentation (punch-in director) | Tiny |
| Pause register (grid, bars, grade, radius hover) | Presentation | Medium |
| Aftermath Died/Fled split | Presentation (report styling) | Tiny |
| Tag icon set (~15 glyphs) | Art | Medium |
| Crit-mass chip states | Presentation (existing strip) | Small |
| Offer marker + hovercard counts block | Presentation (existing presenters) | Small |
| Drag adjacency preview | Presentation | Small |
| Phase 0 wording | Docs only | Tiny |
| CM scope fix | **Core + tests + GDD** | Small |

## 8. Sequencing note

§2 and §5 ride the existing arena-spec Phase 0/3 roadmap (the ring is side-channel shader work =
Phase 1; flee clip = Phase 2; cascade punch-in = Phase 3). §3 is independent of the 3D combat work
and can proceed immediately against the live ShopV2 surface. §6.1 should land before §3.2/§3.3, so
the counts the UI displays are the corrected ones.
