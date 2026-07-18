# DeadManZone — AI Image Prompt Pack (2026-07-17)

Copy-paste prompts for ChatGPT (or any image model). Grounded in:

- `docs/art/style-bible/` (00 game bible + faction bibles 10–40) — palette/silhouette/UI law
- `docs/superpowers/specs/2026-07-15-faction-roster-v1-design.md` §2 — the 8-faction rosters
- `docs/superpowers/specs/2026-07-15-comic-noir-template-handoff.md` §7.2 — the **locked** Meshy ref template
- Supersedes `docs/DeadManZone-Art-Prompt-Pack.md` (v1.1, isometric-token era) for image generation.

**Accent status:** IronMarch brass-gold, Dust Scourge acid yellow-green, Cartel spectral
violet-magenta are canon (style bible). The five W2 factions have **PROPOSED** accents below —
lock them in faction bibles before shipping art:

| Faction | Accent | Status |
|---|---|---|
| IronMarch Union | brass-gold | canon |
| Dust Scourge | acid yellow-green (contained liquid/vapor) | canon |
| Cartel of Echoes | spectral violet-magenta (emission only) | canon |
| Oathborn Accord | ivory-white & tarnished silver (heraldic paint) | PROPOSED |
| Paradox Engine | pale radium-cyan glow (tubes/coils) | PROPOSED — audit vs player-side blue |
| Blightborn Pact | jaundiced ochre-yellow (gas/censers) | PROPOSED — audit vs Scourge green |
| Crimson Assembly | blood-crimson cloth | inherited from Crimson Legion pool — needs the graduation audit vs enemy-side red (Neutral bible) |
| Ashen Covenant | ember-orange (carried fire only) | PROPOSED — ash-white is materials, not accent |
| Neutral | **none — by law.** No symbol, no accent, ever. | canon |

---

## 1. Faction symbols (8)

**Shared base block — paste first, then the faction subject:**

```
Design a military faction insignia for a grimdark dieselpunk WW1 game. Style: flat heraldic
military emblem, heavy black ink linework with varied stroke weight, like a woodcut or stamped
metal badge. No gradients, no glow, no photorealism. This must be an original fictional insignia —
do NOT resemble any real-world military or national emblem (no eagles, no crosses, no swastikas,
no laurel wreaths, no stars from real flags). Present it large and centered on a plain flat
light-grey background, no text, no mockups — just the emblem. Also produce a simplified
one-color stencil version beside it for small-scale use.
```

**Per-faction subject (append one):**

- **IronMarch Union** — "the machine that keeps marching." Toothed cog ring framing an anvil,
  piston, or gauntleted fist. Hard 90°/45° angles only, nothing organic. Stamped into steel.
  Colors: gunmetal grey and coal black, thin brass-gold trim only.
- **Dust Scourge** — "scavengers who weaponized the world's poison." A gas-mask face or
  round filter canister framed by a ragged, asymmetric ring of hooks, salvaged plate scraps, and
  buckled straps — deliberately lopsided, patched from mismatched parts. Colors: rust-brown and
  bone, with one small acid yellow-green glass lens or vial as the only bright element.
- **Cartel of Echoes** — "the war's ghosts, organized." A slim vertical antenna or radio mast
  emitting three concentric echo/ripple arcs, wrapped in a draped hood or cowl shape. Smooth
  flowing curves, no hard angles, mostly negative space and shadow. Colors: near-black charcoal
  with thin spectral violet-magenta emission lines only.
- **Oathborn Accord** — "peacekeepers turned crusaders." A tower shield bearing a wrapped
  banner or an open oath-book, framed by a plain ring. Solemn, symmetric, ecclesiastical-military
  but original — no real crosses. Colors: ivory-white and tarnished silver on olive-drab field.
- **Paradox Engine** — "the experiment that won't end." A clock face with mismatched or
  doubled hands inside a coil/capacitor ring, hairline circuit-like ticks. Precise, instrument-like
  linework. Colors: dark steel and copper with pale radium-cyan on the hands only.
- **Blightborn Pact** — "the rot of old houses." A decayed aristocratic crest: a wilted garden
  rose or hanging censer inside a tarnished, chipped heraldic shield, moth-eaten edges, faded
  braid border. Colors: charcoal and tarnished pewter with a jaundiced ochre-yellow vapor wisp.
- **Crimson Assembly** — "clinical optimization." A stark crosshair-reticle or calibration dial
  over a pressed steel plate, ruled index marks, machine-exact symmetry — reads like a laboratory
  stamp, not a unit patch. Colors: steel-grey and black with one blood-crimson filled segment.
- **Ashen Covenant** — "the revolution of cinders." An upraised open hand or ember urn wreathed
  in stylized rising cinder flecks, framed by a rough charred ring. Hand-painted, slightly crude —
  a cult stencil, not a state emblem. Colors: ash-white and soot-black with ember-orange sparks.

Neutral gets **no symbol** — absence of insignia is its identity (style bible 40, hard rule).

---

## 2. Shop background

```
A clean, dark background texture for a video game shop/build screen, 16:9, grimdark dieselpunk
WW1 quartermaster theme. A flat surface of aged gunmetal plate and worn field-telegraph paper,
faded stencil marks and faint scratches, subtle vignette darkening toward the edges. Extremely
low contrast and low detail — no objects, no focal points, no text, no strong light sources;
this sits BEHIND dense UI cards and must never compete with them. Desaturated palette only:
gunmetal grey, coal black, aged bone-paper, dried-mud brown. No saturated color anywhere.
Even, soft lighting. Painted texture feel with very subtle ink grain, not photorealism.
```

Note: keep the most saturated pixel OFF this image — palette law reserves saturation for
VFX/side/accents, and the shop already carries accent-colored cards.

## 3. Faction select background

```
A wide 16:9 background illustration for a video game faction select screen, grimdark dieselpunk
WW1 style, painted like an inked illustration — heavy dark linework, desaturated war-worn palette
(olive drab, gunmetal, mud brown, bone, coal black). Scene: the interior of a dim command bunker
or war tent — a large map table under a single hanging lamp, sandbagged walls, timber beams,
field telephones and papers, eight empty banner poles standing in shadow along the back wall.
No people, no faction insignia, no text. Critical composition rule: the center and lower two
thirds of the image must be dark, low-detail, and uncluttered — a grid of faction cards renders
on top of it. All visual interest stays in the upper edge and corners. The only saturated color:
a faint warm lamp glow; everything else muted. No fantasy elements, no modern hardware.
```

Rationale: the room is Neutral (the world baseline) so it doesn't pre-sell any faction — the
cards carry the accents.

## 4. Faction card UI elements (faction select)

```
A sprite sheet of UI frame elements for a video game faction selection card, grimdark dieselpunk
WW1 style, on a plain flat light-grey background, laid out separately for slicing. Aged, worn
military HUD chrome: stenciled paint, gunmetal plates, field-paper inlays. Elements needed:
(1) a portrait-orientation card frame — riveted gunmetal border with a square portrait window
in the upper half (square = identity), a horizontal name plate of stamped steel below it, and a
thin recessed trim channel running the border (this channel will be tinted per faction accent
color in-engine, so render it neutral dulled steel); (2) the same card frame in a LOCKED state —
crossed steel straps bolted over the portrait window and a heavy wax-style seal, no chains
cliché, still readable as the same frame; (3) a selected/highlight version of the border only,
slightly raised and cleaner, for hover state; (4) a small empty square badge plate and a small
empty round badge plate. Heavy black ink outlines, flat cel shading, no gradients, no glow,
no text, no faction symbols. Desaturated: gunmetal, coal black, bone paper only.
```

Notes: accent trim stays neutral in the art and gets tinted in-engine — one card asset serves
all 8 factions. Square badge = "who", round = "what you can trigger" (UI law §6). Locked state
covers Dust Scourge / Cartel pre-campaign-win.

---

## 5. Meshy unit reference images

**Already done — do not regenerate:** Neutral + IronMarch full prompts live in
`tools/meshy/refs_prompts.json` (conscript_rifles reuses the canonical s09 ref). The sections
below cover the six remaining factions: **combat-board pieces only** (infantry, vehicles,
structures). HQ-board buildings (`building`-primary) have no Meshy models today — matching the
existing convention — so they're excluded; say the word if that changes.

**Locked base template (§7.2) — paste FIRST in every unit prompt:**

```
ONE single character only — no turnaround, no side/back view sheet, no duplicate clones.
Full-body, ¾ front, neutral A-pose standing (rig-friendly), weapon or gear at side as appropriate.
Mid-stocky ~5 head-heights, slightly oversized head and hands.
Style: heavy-ink comic — thick black contour, crosshatch / hatched shadow, high contrast,
limited olive / bone / leather / metal palette.
Flat even studio lighting, plain solid light-grey background, no ground shadow, no atmosphere.
Hard clear material boundaries (cloth / leather / metal / wood).
Piece cue must change body-mass silhouette at battle distance (not headgear-only).
No backpack unless the piece cue IS pack mass.
```

For **structures/vehicles**, swap the first two lines for:
`ONE single structure/vehicle only — no turnaround sheet, no alternate views, no duplicates. ¾ front view, no crew.`

Then paste the **faction skin block**, then the **unit cue**. Gate every ref through
`python tools/refcheck.py <ref.png>` before Meshy.

### 5.1 Dust Scourge

Faction skin block:

```
Faction skin: a wasteland scavenger — hunched, asymmetric posture (still rig-friendly A-pose),
head is always a gas-mask variant (round filters, hose proboscis, or goggle dome). Materials:
rag-tan canvas wraps, rust-brown mismatched scrap plate, cracked rubber hosing, sun-bleached
fabric — patched, corroded, nothing matches. Acid yellow-green appears ONLY as contained liquid
or glow inside tanks, tubes, and mask lenses. No brass, no clean edges, no matched kit.
```

- **Waste Raider** — sawed-off scrap shotgun with a wired-on blade, one oversized armored
  shoulder plate, hide-and-canvas wraps; the lopsided shoulder is the mass cue.
- **Outrider** — lean fast harasser: short carbine, riding leathers, loot satchels strapped to
  both hips, half-mask with goggles.
- **Gasflinger** — chest rack of two fat acid-green glass canisters feeding a crude hand-sprayer
  lobber; the canister rack is the mass cue.
- **Rust Spear** — crude rebar-and-blade spear plus a scrap-plate shield, extra plating down one
  side of the body.
- **Vulture Crew** — unarmed scavenger: long pry-hook, coil of rope across the chest, bulging
  salvage sack on the hip; trophy hooks on the belt.
- **Raid Captain** — lopsided officer's coat over scrap plate, dome-goggle mask, carrying a
  banner pole strung with scavenged trophies; the trophy pole is the cue.
- **Corpse-Tithe Caravan** (structure) — a heavy scrap-built wagon piled with tarped salvage,
  bone charms and hooks hanging from its frame, mismatched wheels; no crew.
- **Stormcaller of the Yellow Wind** — robes of stitched tarps, a tall staff-sprayer wired to a
  large back-mounted still venting acid-green vapor; the still IS the pack mass.
- **Warlord of Many Banners** — bulky figure in mixed armor scraps clearly taken from several
  different armies, huge cleaver, back rack of many small ragged banners; the banner rack is
  the mass cue.

### 5.2 Cartel of Echoes

Faction skin block:

```
Faction skin: a syndicate soldier who was never officially there — slim, draped silhouette:
long charcoal oilcloth coats, hoods, capes that smooth the body into one flowing shape. No hard
angles, no visible mechanism, no bulky armor. Materials: charcoal oilcloth, black rubberized
cape, matte webbing, dark banded metal — the darkest value range in the game. Spectral
violet-magenta appears ONLY as small emission points: radio tube glow, antenna tips, lens glint.
Silhouette-breakers are signal equipment (whip aerials, dishes, coils), never hoses or trophies.
```

- **Company Rifleman** — well-maintained rifle, long dark coat over a slim armored vest, low
  flat cap, one faint violet lens glint at the collar radio.
- **Strikebreaker** — full-height riot shield and short club, armored long coat, full-face
  visor; the shield is the mass cue.
- **Repo Crew** — short wide-bore scattergun, crowbar through the belt, a satchel of
  repossessed valuables slung across the back.
- **Paymaster's Aide** — unarmed: a ledger case and a small strongbox chained to one wrist,
  hooded cape.
- **Contract Officer** — officer's greatcoat, back-mounted whip aerial rising over one
  shoulder, violet-lit handset in hand; the aerial is the cue.
- **Freelance Colonel** — medium armor visible under an open drape coat, twin holsters, small
  dish antenna on a shoulder pack.
- **Echo Chairman** — slim unarmed figure in an immaculate dark long coat and cape, a collar
  rig of three glowing violet radio tubes, a cane; reads executive, not soldier.
- **War Profiteer** — heavier draped figure with an armored briefcase chained to the wrist and
  a bandolier of assorted ammunition calibers across the chest.

### 5.3 Oathborn Accord

Faction skin block:

```
Faction skin: peacekeepers turned crusaders — upright, disciplined, devotional. Worn olive-drab
uniforms under ivory-white tabards and sashes with tarnished silver fittings; wrapped weapon
hafts, round and tower shields, banners. Heraldic paint is worn and chipped, never bright.
No brass, no rags, no glow. Melee-first kit: shields, spears, truncheons, hammers.
```

- **Truncheon Line** — tower riot shield and truncheon, white tabard over uniform; shield is
  the mass cue.
- **Pilgrim Spears** — ragged pilgrim robe over trousers, plain long spear, wrapped feet;
  deliberately the cheapest-looking Oathborn body.
- **Vow Warden** — heavy pavise shield planted at the side, medium plate over tabard; widest
  Oathborn silhouette.
- **Banner Bearer** — tall banner pole with a long ivory pennant, sidearm only; the pole and
  pennant are the mass cue.
- **Mercy Sister** — hooded, white-veiled medic, medical satchel and a rack of water flasks
  slung crosswise; no weapon.
- **Confessor** — hooded figure with an oath-book chained to the belt, short mace, ivory stole
  over the shoulders.
- **Field Chirurgeon** — bulkier medical pack-frame with a folded canvas stretcher rising above
  one shoulder; the frame is the pack-mass cue.
- **Armored Ark** (vehicle) — boxy riveted armored transport with a large rear ramp door, no
  main gun, chipped ivory heraldry on the hull sides, heavy tracks.
- **High Exarch** — worn ornate half-plate over an ivory cloak, tall helmet crest, two-handed
  ceremonial warhammer.
- **Hospitaller-General** — armored medic-commander: light plate, white cloak, a tall
  standard-staff topped with a lantern cage, satchels at both hips.

### 5.4 Paradox Engine

Faction skin block:

```
Faction skin: a WW1 research corps that weaponized time — insulated lab trench coats, heavy
rubber gauntlets, brass-free copper-and-steel instruments, glass vacuum tubes and coil apparatus
with a faint pale radium-cyan glow (the ONLY saturated element, always inside glass or on coil
tips). Goggles worn up or down. Kit reads precise and calibrated, not scavenged. Keep the
geometry clean — no afterimages or ghost effects in the reference (echo VFX live in-engine).
```

- **Chrono-Fusilier** — rifle wrapped with a copper coil and one small glowing tube on the
  stock, insulated trench coat, goggles up.
- **Phase Vanguard** — reinforced insulated coat and a small pavise shield backed with a
  capacitor bank of tubes; shield is the mass cue.
- **Arc Lancer** — extra-long beam-rifle with a glass focusing tube down the barrel — the
  longest gun line in the faction — plus a folded bracing bipod.
- **Field Dynamo** — hand-crank dynamo backpack with cable bundles running to both gauntlets;
  the dynamo IS the pack mass.
- **Overclock Engineer** — tool harness, oversized calibrated wrench, cable spools and an
  injector gun at the belt.
- **Resonance Coil** (structure) — a tesla-coil pylon on a tripod base with a ring of ceramic
  insulators and a faint cyan corona at the tip; no crew.
- **Doctor Recursion** — slight unarmed figure, lab coat over uniform, TWO pocket-watch chains
  across the vest, clipboard in hand.
- **Perpetual Engine** (structure, 2×2) — a squat engine-block machine of flywheels, copper
  coils, and glass tube banks, cable runs spilling off its base; no crew.

### 5.5 Blightborn Pact

Faction skin block:

```
Faction skin: the decayed house-guard of a poisoned aristocracy — moth-eaten dress uniforms
with faded braid, tarnished (never shiny) plate and pewter fittings, funeral veils, antique
weapons. Wear pattern: inherited finery gone to rot. Jaundiced ochre-yellow appears ONLY as
gas/vapor inside censers, glass vials, and tanks. No scrap improvisation (that is Dust
Scourge) — everything was once fine and is now decaying.
```

- **Threadbare Guard** — patched dress uniform with faded medal ribbons, antique long musket
  with bayonet.
- **Censer Carrier** — a large swinging censer on a chain venting ochre vapor, fed by a small
  tank backpack; the censer-and-tank rig is the mass cue.
- **Iron Veil Guard** — halberdier in tarnished full plate, a veiled helm hiding the face;
  widest Blightborn silhouette.
- **Court Physician** — beaked plague-doctor leather mask, long coat, satchel of glass vials
  on a cross-strap; no weapon.
- **Dirge Piper** — military piper with a ragged set of pipes under one arm and a small drum
  at the hip; the pipes are the cue.
- **Gas Alchemist** — vial bandoliers across the chest and a glass alembic rig on the back,
  gloved to the elbow.
- **Widow of the House** — veiled noblewoman in a mourning dress over uniform breeches, long
  dueling pistol held at her side.
- **Duchess of Sighs** — elaborate decayed gown, gas-veil headdress, an ornate censer-staff
  taller than she is, ochre vapor curling from it.
- **Vitriol Throne** (structure, 2×2) — a throne-altar plumbed with pipes into glass vats of
  ochre vitriol, a battery of sprayer-mortar nozzles fanned behind it; no crew.

### 5.6 Crimson Assembly

Faction skin block:

```
Faction skin: clinical military optimization — the best-equipped soldiers in the game. Pressed
dark steel-grey uniforms, smooth machined plate (no rivet clutter — that is IronMarch), sealed
masks with round lenses, regulation kit identical across bodies. Blood-crimson appears ONLY as
cloth details: armbands, collar tabs, pennants, a painted stripe. Reads laboratory-sterile and
exact, never industrial or worn-out.
```

- **Assembly Trooper** — pressed uniform, full regulation kit, clean modern rifle, crimson
  shoulder tabs, light plate.
- **Suppression Team** — light machine gun with drum magazine and folded bipod, ammunition
  belts crossed over the chest; the gun is the mass cue.
- **Hazmat Vanguard** — fully sealed hazmat suit under medium plate, round glass lenses,
  filter unit on the chest.
- **Ballistics Analyst** — unarmed: tripod-mounted rangefinder scope carried over one
  shoulder, clipboard and table-satchel; the tripod is the cue.
- **Bunker Emplacement** (structure) — a poured-concrete pillbox section with a single wide
  firing slit and a steel door, stenciled index numbers; no crew.
- **Scout Tankette** (vehicle) — a small two-crew tankette, light smooth plate, one machine-gun
  turret, a crimson pennant on a short aerial.
- **Fire-Plan Officer** — officer's coat, map case on the hip, flare pistol in hand,
  crimson-banded cap.
- **"Vanquisher" Doctrine Tank** (vehicle) — heavy boxy tank with one long large-bore cannon,
  smooth pressed plate, a single painted crimson stripe; reads exact, not battered.
- **"Stiller" Suppression Platform** (vehicle) — heavy hull carrying FOUR linked guns on one
  rotating mount; the quad-gun cluster is the silhouette cue.
- **Director of Programs** — unarmed: immaculate greatcoat, gloves, a thick data ledger under
  one arm, crimson sash.

### 5.7 Ashen Covenant

Faction skin block:

```
Faction skin: a fanatic revolution of cinders — ash-grey smeared robes and wraps over scavenged
uniform scraps, soot-blackened hands and feet wrappings, almost no armor, crude kit. Ember-orange
appears ONLY as carried fire: torch heads, brazier coals, ember cages. Ash-white cloth and
soot-black are the materials. Cult-poor, fervent, hand-made — no institution, no polish.
```

- **Zealot Mob** — robed fanatic with an improvised nail-studded club, ash-smeared face wrap,
  rope belt.
- **Ash Acolyte** — lean single figure with one heavy cleaver, bare scarred forearms, minimal
  wraps; the leanest Ashen body.
- **Torchbearer** — scrap-built pressure-tank flamethrower with a dripping nozzle, ember glow
  at the pilot; the tank rig is the pack-mass cue.
- **Penitent** — massive unarmored figure wrapped in heavy robes and draped penitent chains,
  no weapon; sheer body mass is the cue.
- **Hymnal Leader** — raised open hymnal in one hand, hand-bell in the other, long banner sash.
- **Reliquary Bearer** — a wooden reliquary box on a back-frame with a rack of ember candles;
  the reliquary IS the pack mass.
- **Firebrand Vicar** — patched vestments over wraps, a censer-mace of burning coals on a
  chain.
- **Saint of the Embers** — gaunt robed figure with a rough iron ring-crown, a staff topped
  with a caged ember; small silhouette, tall staff line.
- **The Ash Martyr** — frail figure wound in fuse cord and ember charms, carrying a great
  ceramic ember urn against the chest with both arms.

---

## Workflow notes

- Symbols/UI: generate 2–4 variations, pick one, refine solo. Highest resolution offered.
- Backgrounds: 1920×1080 minimum; a dark gradient overlay in Unity is cheaper than regenerating
  for text contrast.
- Meshy refs: one unit per image, always through `tools/refcheck.py` (~40px black-shape gate)
  before spending an image3d job. Pipeline + gotchas: `docs/superpowers/specs/2026-07-15-comic-noir-template-handoff.md`.
- As faction ref prompts get finalized, append them to `tools/meshy/refs_prompts.json` so the
  JSON stays the single machine-readable source.
