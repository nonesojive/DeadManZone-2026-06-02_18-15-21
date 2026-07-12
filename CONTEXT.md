# DeadManZone — Domain Context

Domain language for DeadManZone. Started during the 2026-07-10 art-plan grilling session (art & presentation); widened 2026-07-12 with run/meta language. Captures terminology as decisions crystallize.

## Run & meta language

**Dread**:
The run's escalation currency and difficulty clock. Earned by *winning* fights — the amount set by the Fight Option taken (harder assaults escalate the war faster); losses grant none. Fixed Dread thresholds make the next fight a Boss Fight; defeating the third boss wins the run. Replaces the fixed 10-fight counter: run length is variable and player-paced, and anything difficulty-curved (enemy strength, shop tiering, salvage) keys off Dread, not fight number.
_Avoid_: Threat (collides with taunt/aggro vocabulary), Desperation (reads as the player's state; the meter rises when the player wins), fight counter

**Boss Fight**:
The mandatory fight triggered when Dread crosses a threshold — no Fight Options that round; the enemy comes to you. A boss is a handcrafted army led by a named commander unit with one readable rule-bend (twist), never a stat-inflated normal army. Boss *identity* (commander, twist, enemy pool) is decoupled from boss *stage*: each boss has three stage loadouts, and the run's boss order is a seeded random permutation of the three pool bosses, hidden until threshold. Losing a boss fight is a normal loss; the boss waits for the next attempt.
_Avoid_: Elite (a boss is a stage capstone, not a beefy normal fight), miniboss

**Fight Option**:
One of three seeded fronts offered each build round — easy, normal, hard — each an independently rolled enemy army (pools may repeat across slots). Easy costs Authority from that round's command pool and grants the least Dread; hard grants the most Dread plus a materiel package on victory. Options are generated at round start, visible throughout Build (informed shopping), locked on entering combat, and persist across save/load. Boss rounds replace the options with the boss report.
_Avoid_: Encounter choice, node (there is no map — it's a per-round choice), mission select

**Front Report**:
The Build-phase panel showing the three Fight Options. Default intel per option: enemy pool, coarse strength band, and the tier's stakes. Recon pieces ladder up the intel: unit count → composition → next Boss Fight's identity.
_Avoid_: Scouting screen, world map

**Battle Condition**:
A readable combat rule-bend attached to a hard Fight Option, drawn seeded from an authored deck and shown in the Front Report by default (consent, not gotcha). Shares one mechanical seam with a Boss Fight's **Twist** — same rule-modifier hook in the sim, two content decks. Environmental hazards are Battle Conditions, not board features.
_Avoid_: Mutator (genre-generic), curse, hazard (a hazard is one kind of Condition)

**Twist**:
The one readable rule-bend that defines a Boss Fight (e.g. the commander resurrects early casualties). Same mechanical seam as Battle Conditions; boss-authored, scales with boss stage.
_Avoid_: Boss mechanic (vague), gimmick

**Morale (combat)**:
A per-unit bar beside HP, damaged by terror effects and battlefield events. At zero the unit **Breaks** and routs. No longer a run resource — see Manpower.
_Avoid_: Sanity, fear meter, run-level morale (deleted)

**Break / Rout**:
The state a unit enters when combat Morale hits zero: it flees the field and is out of that fight, but is not dead. Enemy routs grant no salvage roll; player routs cost no Manpower and the piece returns to its board slot next round.
_Avoid_: Retreat (implies an order), death, despawn (presentation word)

**Manpower**:
Run health. Combat deaths deduct it directly; the run is lost when an army can no longer be fielded. Pieces and effects that grant Manpower are the game's heals.
_Avoid_: Lives, HP (reserve for units), morale (the retired run resource)

**Rarity**:
A piece's design role, not its raw power: Common = line units, Uncommon = synergy enablers and support, Rare = build-arounds (ability granters, vehicles). Three tiers (enum leaves room for a fourth, unauthored). Gates shop offer odds (weighted by Dread) and salvage quality; price stays authored per piece.
_Avoid_: Tier (reserve for boss stages), quality, power level

## Art & presentation language

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
An environment dressing over the single shared board geometry. Canonical list (2026-07-12): Trenchline (today's no-man's-land), Wartorn Forest, Ravaged Town, Trench (parapets flanking the strip — a dressing, NOT an interior), Siege Ground, Fog Field; more may be added. A theme shifts hue and props only — value structure, camera, flat combat strip, and silhouette rules never change. Each enemy pool owns a small set of home themes; the fight's theme rolls seeded from the chosen Fight Option's pool set, and a Boss Fight lands on its pool's signature theme. First wave ships 3–4 of the list.
_Avoid_: Biome, level, map (the board geometry is constant), interior (breaks the camera/strip law)

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
