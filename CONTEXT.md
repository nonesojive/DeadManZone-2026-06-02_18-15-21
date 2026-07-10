# DeadManZone — Art & Presentation Context

Domain language for DeadManZone's art direction and combat presentation. Started during the 2026-07-10 greenfield art-plan grilling session; captures terminology as decisions crystallize.

## Language

**Style Bible**:
The game-wide art-direction rulebook: the visual laws every asset must obey (palette discipline, line treatment, silhouette rules, camera, VFX grammar). Faction bibles are children of it, defined as deviations from its baseline.
_Avoid_: Art guide, mood board, brand doc

**Faction**:
One of the three aligned armies — IronMarch Union, Dust Scourge, Cartel of Echoes. A faction has its own style bible defining its deviation from the game-wide baseline.
_Avoid_: Iron Vanguard (stale slice name — the faction is IronMarch Union), team, side (a side is who a unit fights for in one battle; a faction is who a unit belongs to)

**Neutral**:
The unaligned pool of pieces usable by anyone — any player faction and the enemy pool, potentially both sides of the same battle. Neutral art must never intrinsically encode allegiance; side-identity comes from a separate always-on channel, never baked into the sprite.
_Avoid_: Fourth faction, grey faction

**Side**:
Which army a unit fights for in a single battle (player vs. enemy). A battle-scoped property, orthogonal to faction — a Neutral piece has a side but no faction; an IronMarch piece fighting in the enemy gauntlet has faction IronMarch, side enemy.
_Avoid_: Team color (that presumes the channel; see Side Channel)

**Side Channel**:
The always-on visual carrier of side-identity in combat: a side-colored shader outline on the unit plus a tinted base ring under its feet. Two redundant channels; neither touches the sprite's interior — faction/Neutral art stays desaturated inside its black ink regardless of side. Full-sprite side tint is explicitly rejected.
_Avoid_: Team tint, multiply tint (the old brief's rejected approach)

**Accent**:
The single saturated color a faction is permitted over the desaturated base world, applied to details (trim, glow, reservoirs), never large surfaces. IronMarch brass-gold, Dust Scourge acid yellow-green, Cartel spectral violet-magenta, Neutral none. Accents may not approach the reserved side-channel hues (player blue / enemy red).
_Avoid_: Faction color, team color

**Arena Theme**:
One of three environment dressings (Trenchline, Siege ground, Fog field) over the single shared board geometry, keyed to enemy pools. A theme shifts hue and props only — value structure, camera, and silhouette rules never change.
_Avoid_: Biome, level, map (the board geometry is constant)

**Punch-In**:
A brief scripted camera dolly/zoom on a combat beat (kill, crit, HQ hit) — the game's maximum feedback intensity. There is deliberately no full-screen interrupt tier above it.
_Avoid_: Cutscene, killcam

**Backbone**:
The shared low-poly humanoid base family from which the infantry roster is kitbashed — one rig, one retargeted animation set (ADR-0003).
_Avoid_: Base mesh (ambiguous with per-unit meshes)

## Example dialogue

> **Dev:** The Cartel sniper needs to read as enemy in fight 7 — should I tint the coat red?
> **Expert:** No — side never touches the model. The side channel does it: red outline tint plus red base ring. The coat stays charcoal, the accent stays violet, whichever side fields it.
> **Dev:** And if a Neutral MG nest is on both boards that fight?
> **Expert:** Same sprite, same textures, opposite side channels. If you can tell allegiance with the outlines hidden, the asset is off-model.

## Flagged ambiguities

- **"Iron Vanguard"** appears in the prettycombat slice docs; the faction's canonical name is **IronMarch Union**. Treat Iron Vanguard as a stale internal slice label.
- ~~"Crimson" pieces~~ Resolved: `crimson_*` pieces belong to the **Crimson Legion** enemy pool (GDD: heavy assault). Enemy pools (Neutral Militia, Crimson Legion, Ash Wraiths) are not Factions — they get palette entries in the game-wide bible, not bibles of their own.

## Decisions in force

See `docs/adr/`. Key: art identity is "DD skin, TT skeleton" (ADR-0001).
