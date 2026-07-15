# DeadManZone — Game Style Bible

**Identity in one line:** a grim dieselpunk war that reads like a toy battle — Darkest Dungeon's skin on Top Troops' skeleton (ADR-0001).

This is the law layer. Faction bibles (`10`–`40`) are defined as deviations from this document; the arena spec (`50`) implements it. When any two rules conflict on the battlefield, **legibility wins and mood is recovered through surface** — palette, texture, grade — never by darkening or cluttering the read.

Decided 2026-07-10 (greenfield art-plan grilling session). Companion decisions: ADR-0001 (identity split), ADR-0002 (cel-shaded 3D + ink shader), ADR-0003 (mesh sourcing). Glossary: `CONTEXT.md`.

---

## 1. The two parents, and what each owns

**Top Troops owns structure** — everything that makes a 20+ unit battle parseable in one glance:

- Bold, archetype-coded silhouettes readable at combat scale with color and detail removed.
- Stocky, readable proportions (heads/hands/weapons oversized enough to read small; never chibi-cute).
- The combat camera, board staging, and animation smoothness (interpolated rigs, not held poses).
- The bright, saturated VFX and status color-coding layer (Section 5).
- Systemic art production: shared formulas with per-instance variation, never bespoke-everything.

**Darkest Dungeon owns surface and mood** — everything the eye feels rather than parses:

- The desaturated, war-worn base palette where saturation must be *earned* (Section 3).
- Ink: heavy dark outlines with varied stroke weight; interior material separation drawn in black, not gradient shading (Section 4).
- Grim environments — a dead world hostile to the men walking through it.
- Aged, worn HUD texturing (Section 6).
- Camera punch-ins as the impact channel (juice through framing, not animation frame count).

What we deliberately did NOT take from DD: pooling black that eats silhouettes, elongated horror proportions, low-light scenes, and full-screen interrupt reveals (Section 5.3).

## 2. Silhouette law

Every piece must pass the **black-shape test**: rendered as a flat black silhouette at final combat screen size, a player who knows the game names its archetype. The archetype silhouette vocabulary:

| Archetype | Silhouette signature |
|---|---|
| Rifle infantry | Upright, long-gun line across the body |
| Marksman | Kneeling/braced, longest gun line, scope bump |
| Mortar/artillery crew | Low cluster + steep tube angle |
| Assault/melee | Forward lean, short weapon, bulk at shoulders |
| Vehicle | Wider than tall, track/wheel base, turret mass |
| Building | Rigid, architectural, taller than any unit — announces "structure" before the HP bar does |

Squad pieces (manpowerCost > 1) render as multiple bodies; their formation offsets are part of the silhouette budget — stagger so no body fully occludes another at the canonical camera angle. (Top Troops paid for ignoring this with a rebalance patch; we pay up front.)

## 3. Palette law

**Base world: desaturated.** Ground, buildings, uniforms, skin, steel all live in a narrow low-saturation band (olive, khaki, gunmetal, bone, char). Value structure carries the scene; hue is nearly silent.

**Saturation budget, in priority order** (brightest thing on screen wins attention — so ration it):

1. **VFX** — muzzle flashes, explosions, status effects. The most saturated pixels in the game.
2. **Side channel** — player cool blue / enemy warm red, on outlines and base rings only (arena spec §3). These two hues are *reserved*: nothing else on the battlefield may use them at similar saturation.
3. **Faction accents** — exactly one saturated accent per faction, on trim/glow/insignia details only, never large surfaces: IronMarch **brass-gold**, Dust Scourge **acid yellow-green**, Cartel of Echoes **spectral violet-magenta**, Neutral **none**. Enemy pools: Crimson Legion **blood-red** (harmonizes with enemy-side red — they are always hostile), Ash Wraiths **pale ash-white**.
4. **Environment** — arena themes shift hue, never saturation or value structure (arena spec §5).

**Rule of thumb:** if a screenshot's most saturated pixel isn't a VFX, a side marker, or an accent detail, something is breaking the law.

## 4. Line law (the ink)

- Every unit and building carries a **dark exterior outline** (shader pass, thickness tuned per silhouette size — smaller units get relatively thicker lines).
- Interior material boundaries (armor vs. cloth vs. skin vs. weapon) separate via **dark ink-style edges**, not soft gradients — in a texture pass or edge-detect shader term. Vary apparent stroke weight so the result reads "inked illustration," not "uniform cel filter." This is the single technique that keeps us out of generic-toon territory; it is the Phase-0 kill/keep criterion (arena spec §8).
- The exterior outline is always pure black ink in combat. Side allegiance reads via the base ring only (arena spec §3). Interior ink stays black always.

## 5. Feedback grammar

**5.1 Two status pipelines, never mixed.** Body-state effects (burning, gassed, suppressed, frozen supply) render as **material/shader changes on the unit** — the body itself looks different. Behavior-state effects (stunned, taunting, rallied, targeting) render as **symbols anchored above the unit** — scannable independent of the model. Every new status must declare which pipeline it uses before art is made; no status may use both.

**5.2 Status colors** come from the near-universal RPG vocabulary (green = poison/gas, blue = shock/freeze, red = rage/damage, gold = buff, violet = arcane/echo) so they're guessable without tooltips — with the constraint that gas-green vs. Dust Scourge acid accent and violet vs. Cartel accent must differ in value/placement enough to never be confusable mid-fight.

**5.3 Two escalation tiers — and only two.** Ambient indicators (HP bars, status marks, meters) carry ongoing state; **camera punch-ins** (kills, crits, HQ hits) carry moments. There is **no full-screen interrupt tier** — deliberately rejected: punch-ins are the ceiling, run pacing is never taken hostage by drama. Run-level events (HQ destruction, morale collapse) get the biggest punch-in plus HUD treatment, not a takeover.

**5.4 Modify existing channels before adding new ones.** Before any new indicator, check whether the state can live on something the player already watches — a health-bar tint (TT's "injured = partially pink" trick), a portrait border, a base-ring pulse. New floating icons are the last resort.

## 6. UI law

- HUD chrome is **aged and worn** — stenciled paint, field-telegraph paper, gunmetal plates — matching the DD skin.
- **Exception, by design:** purely mechanical overlays (targeting previews, placement highlights, range indicators) drop the diegetic register entirely and use maximum-contrast information-design color. Top Troops' targeting arrows proved this: when ambiguity costs the player a fight, clarity outranks fiction.
- Square badge = "who" (units/portraits); round badge = "what you can trigger" (abilities/items). Keep the shape grammar consistent.
- Skill/ability icons: shared per-faction backdrop template (accent-colored burst + silhouette) + small differentiating badge per ability. Never bespoke-per-ability art.

## 7. Production law

- One strict formula, human-varied surface: shared rigs, shared shaders, shared icon templates — variation happens in kitbash parts, texture wear, accent placement.
- Rank/upgrade states must be visible on the model (trim, plating, brass detail at max rank), not only in a stats panel.
- Nothing ships that fails the black-shape test at combat scale, no matter how good it looks zoomed in.

## Related documents

- Faction bibles: [IronMarch Union](10-faction-ironmarch-union.md) · [Dust Scourge](20-faction-dust-scourge.md) · [Cartel of Echoes](30-faction-cartel-of-echoes.md) · [Neutral](40-neutral.md)
- [Combat arena spec & tech pipeline](50-combat-arena-spec.md)
- ADRs: `docs/adr/0001`–`0003` · Glossary: `/CONTEXT.md`
- Source studies (BrokenHopeBrain vault, Game Studies/): top-troops-combat-art-direction-deep-dive.md, darkest-dungeon-art-direction-teardown.md
