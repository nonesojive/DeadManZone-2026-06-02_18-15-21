# Combat Arena — Look & Build Spec (Greenfield Plan)

**Target in one line:** Top Troops' combat presentation — camera, board staging, smoothness, readability — wearing DeadManZone's DD skin.

Status: **plan only, no code changed** (2026-07-10 session). Supersedes-on-adoption: the sprite-billboard direction in `docs/art/combat-arena-2d-art-brief.md` and `docs/combatvisualv4-handoff.md`. The Core sim (deterministic tick replay, `CombatEventLog` → director playback) is untouched by everything below — this is a Presentation-layer plan, per the project's layering rules.

Decisions referenced: ADR-0001 (identity), ADR-0002 (cel-shaded 3D + ink shader), ADR-0003 (mesh sourcing).

---

## 1. Camera

| Property | Spec |
|---|---|
| Projection | **Perspective** (replaces current ortho) |
| Pitch | 45–55° elevated three-quarter (start at current 52° equivalent framing) |
| FOV | Narrow, ~30–40° — cells must still read near-square, minimal perspective distortion |
| Orientation | Player **left**, enemy **right** (unchanged from sim) |
| Default behavior | **Fixed frame** during playback — readability first |
| Punch-ins | Brief scripted dolly/zoom on kill blows, crits, HQ hits; return to home frame. This is the impact channel (DD's cheap-juice lesson) and the *only* camera motion. No follow-cam, no drift. |

Punch-in discipline: max one active at a time; skip if another fired within ~2s (spam degrades the currency); fast-forward mode disables them entirely.

## 2. Board & grid

- One board geometry across all fights; themes re-dress it (§5).
- **Placement phase:** full-strength cell highlights — the grid is the interface.
- **Combat playback:** grid fades to faint diegetic ground markings (ruts, duckboards, chalk lines). Terrain reads as battlefield, not board. Punch-in shots show no grid at all.
- Units free-chase between anchor cells (keep the v4 pacing solution: `EmptyTickPaceScale` + coupled `moveSpeedPresentationScale` — that math is presentation-agnostic and survives the 3D move).

## 3. Side channel (always-on allegiance read)

Per CONTEXT.md: faction ≠ side, Neutral units fight for anyone, mirror matches exist. Two redundant channels, neither touching the model's interior:

1. **Outline tint** — the ink outline pass (§6) shifts toward side color: player cool blue, enemy warm red.
2. **Base ring** — tinted disc/ring under the feet (replaces/absorbs the blob shadow).

Blue and red at these saturations are reserved battlefield-wide (game bible §3). Full-model side tint is rejected — it would destroy the DD surface and collide with faction accents.

## 4. Unit rendering pipeline (ADR-0002/0003)

**Mesh sourcing**
- Infantry backbone: one low-poly humanoid base family (existing Synty-class packs), kitbashed per archetype — heads/packs/weapons swapped, bodies shared. Silhouette consistency is protected because ~80% of the roster shares one body family.
- Showcase pieces (HQs, diesel walker, bosses, unique vehicles): AI 3D generation (Meshy/Tripo-class), manually retopo'd/cleaned to backbone poly density and proportion. Budget real cleanup time per piece.
- Squads (manpowerCost > 1): N instances of the same rig with formation offsets + per-instance anim phase offset; staggered so no full occlusion at the canonical camera (game bible §2).

**Animation**
- One humanoid rig; Unity Humanoid retargeting spreads a single anim set (idle/walk/attack/hit/death per weapon class) across the entire infantry roster. Blend trees give the TT smoothness that sprite sheets structurally could not.
- Weapon-class anim sets: rifle, braced/scoped, crew-served (mortar), melee, plus per-vehicle bespoke.
- Death: brief rig reaction → dissolve via shader (ink-edge dissolve), no ragdolls.

**The uber-shader (URP, one shader for all units/buildings)**
Requirements, in priority order:
1. Cel/toon ramp (2–3 hard bands, no smooth falloff).
2. **Ink outline pass** — dark exterior outline, thickness scaled by screen size; accepts side-color tint parameter (§3).
3. **Interior ink** — material-boundary dark edges (texture-authored line work and/or edge-detect term), with varied stroke weight. This is the "reads as inked illustration, not toon" make-or-break (Phase 0).
4. Status body-pipeline hooks: hit flash, damage desaturation, burn/gas/freeze material overlays, stealth dissolve — all as shader states driven per-instance (`MaterialPropertyBlock`), replacing the old C# sprite-flash approach.
5. Faction accent as a masked emission/tint channel (one mask texture slot; accents are authored into masks, so palette changes never require re-texturing).

**Grade & post**
- Keep a grimdark URP volume in spirit (current: ACES, bloom, vignette, split-tone, grain) but re-tune against the cel look — desaturation and tone live mostly in *textures and shader ramp*, with post as a finishing pass, so the legibility layers (VFX, side channel, accents) survive the grade. Settings toggle stays.

## 5. Arena themes (3)

Same geometry, same camera, same value structure — hue and dressing shift (DD region-palette law):

| Theme | Keyed to | Dressing |
|---|---|---|
| Trenchline | Neutral Militia fights | Olive/dirt, sandbags, duckboards, wire |
| Siege ground | Crimson Legion fights | Scorched red-brown, shell craters, ember haze |
| Fog field | Ash Wraith fights | Pale ash, dead trees, low green-white ground gas |

Cost per theme: ground material + skybox/backdrop + one prop set. Escalation across the 10-fight gauntlet reads environmentally for free. Fog/haze must never drop unit-vs-ground contrast below the readability bar — themes are graded around the side channel, not over it.

## 6. VFX & feedback systems

- **Two status pipelines** (game bible §5.1): body-state = shader states on the unit (§4 shader hooks); behavior-state = symbol anchors above the unit. Two separate systems in code (`MaterialOverride` vs `SymbolOverlay` routing on a status-visual definition SO); no status uses both.
- VFX owns the saturation budget: muzzle flashes, tracers (arced, per current profiles), impacts, explosions are the brightest elements on screen. Mesh/particle VFX replace the sprite-sheet VFX strips; pooling discipline carries over from v4.
- **Punch-in director**: consumes the same replay events the presenter already emits (kill/crit/HQ-damage) and drives §1's camera beats. Two feedback tiers only — ambient + punch-in. **No full-screen interrupts** (decided; deliberate deviation from the DD template).
- HP bars, damage pops, army bars: keep current behavior; restyle chrome to game bible §6 (worn/aged, with information-design contrast for the bars themselves).

## 7. What carries over vs. what's replaced

| Keep (proven, presentation-agnostic) | Replace |
|---|---|
| Core sim + event replay + director pacing math | Sprite-sheet billboards + `CombatUnitVisual2D` quad pipeline |
| Free-chase/anchor smoothing (`CombatUnitActor`, chase controller) | Sprite outline shader plan (frame-rect clamping problem disappears with 3D) |
| VFX/material pooling, `Time.maximumDeltaTime` clamp, save-on-completed | 2D art brief's 46-PNG asset plan + grayscale-multiply-tint faction plan |
| HUD layout, hover cards, board/shop scene structure | Checker-grid always-on combat ground |

## 8. Build roadmap (agreed 2026-07-10)

- **Phase 0 — kill/keep gate (style spike).** ONE kitbashed unit + uber-shader ink look + grade, screenshotted in the §1 camera against a graybox board. Pass = "inked illustration"; fail = "generic toon" → revisit ADR-0002 before any roster work. Nothing else starts until this gate is judged.
- **Phase 1 — stage.** Perspective camera rig + graybox board with placement/combat grid states + side-channel shader (outline tint + base ring; shares code with the ink outline).
- **Phase 2 — backbone.** Base body family, one rig, retargeted anim set, four archetypes (rifle, marksman, mortar crew, assault) fighting in the arena.
- **Phase 3 — feedback.** Two-pipeline status system + punch-in director + VFX saturation pass.
- **Phase 4 — themes.** Three arena dressings.
- **Phase 5 — showcase.** AI-gen HQs, walker, key vehicles; IronMarch accent pass (brass masks).
- **Phase 6+ — factions.** Dust Scourge and Cartel of Echoes visual passes when their content passes land (post-demo, per GDD scope).

Each phase ends with a Game-view screenshot review against the game bible's laws (black-shape test, saturation audit) — the bible is the acceptance criteria, not a mood document.

## Related

[Game bible](00-style-bible.md) · faction bibles [10](10-faction-ironmarch-union.md)/[20](20-faction-dust-scourge.md)/[30](30-faction-cartel-of-echoes.md)/[40](40-neutral.md) · ADRs `docs/adr/0001-0003` · source studies in the BrokenHopeBrain vault (`Game Studies/top-troops-combat-art-direction-deep-dive.md`, `Game Studies/darkest-dungeon-art-direction-teardown.md`)
