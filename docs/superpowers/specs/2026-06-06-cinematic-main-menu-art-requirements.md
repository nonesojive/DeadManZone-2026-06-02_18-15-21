# Cinematic Main Menu — Future Art Requirements

**Date:** 2026-06-06  
**Related spec:** [2026-06-06-cinematic-main-menu-design.md](./2026-06-06-cinematic-main-menu-design.md)  
**Status:** Backlog — not required for MVP (lights + fog placeholder)

This document lists art assets needed to fully realize the cinematic main menu for **DeadManZone** (grimdark retro-futurist WW1 autobattler). MVP ships with SlimUI UI chrome, post-processing, and a procedural lights/fog backdrop only.

---

## Art direction reference

| Attribute | Target |
|-----------|--------|
| Mood | Grimdark, industrial, trench warfare |
| Era feel | WW1 + diesel/brass retro-futurism |
| Palette | Charcoal, brass, amber arc-lamps, muted olive/rust |
| Avoid | Clean sci-fi cyan, glossy modern UI, bright fantasy |

Align with existing `UiThemeSO` brass accents and faction identity (Ironmarch Vanguard, Dust Scourge, Cartel of Echoes).

---

## Priority legend

| Priority | Meaning |
|----------|---------|
| **P0** | Blocks menu from feeling “finished” once cinematic shell is live |
| **P1** | Strong polish; noticeable upgrade |
| **P2** | Nice-to-have / variant / seasonal |

---

## 1. 3D environment (MenuEnvironment)

| ID | Asset | Description | Priority | Notes |
|----|-------|-------------|----------|-------|
| ENV-01 | **Hero diorama set** | Modular trench/command post backdrop: sandbags, timber supports, mud, wire silhouettes | P0 | Single cohesive set sized for menu camera frustum; low poly (<15k tris total) |
| ENV-02 | **Ground / mud plane** | Tiled or sculpted ground with trench wear, puddles, debris | P0 | Replaces flat placeholder plane |
| ENV-03 | **Sky / atmosphere** | Custom skybox or HDRI: overcast twilight, distant smoke columns | P0 | Supports fog; no bright sun |
| ENV-04 | **Arc-lamp props** | 1–2 brass arc-lamp meshes with emissive bulbs | P1 | Matches UI accent color; drives scene lighting story |
| ENV-05 | **Background depth layers** | Low-res silhouettes: ruined walls, crane, smoke stacks | P1 | Parallax or static; sells scale without draw cost |
| ENV-06 | **Faction vignette variants** | Swappable backdrop accents per faction (Vanguard brass, Scourge rust/gas pipes, Cartel resonance coils) | P2 | Optional camera Pos2 variants when faction panel opens |

**Camera notes:** Layout must read well at SlimUI `MenuCamIdle` (Pos1) and `MenuCamPos2` (Pos2). Provide blockout mesh or reference renders for both angles before final polish.

---

## 2. 3D display props (optional hero objects)

Static meshes placed in diorama — not gameplay units, menu-only scale/pose.

| ID | Asset | Description | Priority | Notes |
|----|-------|-------------|----------|-------|
| PROP-01 | **Diesel walker (menu pose)** | Iron Vanguard showcase: idle, slightly weathered | P1 | Can derive from gameplay mesh later; menu-specific LOD |
| PROP-02 | **Command bunker hatch / radio array** | Small industrial prop cluster | P1 | Reinforces “quartermaster commander” fantasy |
| PROP-03 | **Gas mask / trench gear scatter** | Tabletop-scale debris | P2 | Dust Scourge tone |
| PROP-04 | **Resonance coil / phantom tech** | Cartel of Echoes accent prop | P2 | Subtle glow, not neon |

---

## 3. 2D UI — branding & chrome

SlimUI frames are temporary. Custom art replaces or reskins SlimUI sprites.

| ID | Asset | Description | Priority | Spec |
|----|-------|-------------|----------|------|
| UI-01 | **Game logo / wordmark** | “DeadManZone” or “Until The Trenches Fall” treatment | P0 | PNG + SVG source; works on dark bg |
| UI-02 | **Menu panel frame set** | 9-slice borders, corners, dividers (brass rivet/industrial) | P0 | Match 1920×1080 reference layout |
| UI-03 | **Button states** | Normal, hover, pressed, disabled (Continue locked state) | P0 | Consistent with `UiThemeSO` button colors |
| UI-04 | **Icon set** | Continue, New Run, Achievements, Leaderboard, Options, Exit, Back | P1 | 128px source; monochrome + accent variant |
| UI-05 | **Faction select portraits / emblems** | 3 faction badges + optional banner strips | P1 | Locked state greyed overlay |
| UI-06 | **Loading screen frame** | Progress bar track + fill + vignette | P1 | Used during Run scene async load |
| UI-07 | **Cursor set** | Industrial/brass pointer (optional) | P2 | SlimUI cursors are placeholder |
| UI-08 | **Achievement / leaderboard row chrome** | List item background, rank medals | P2 | Meta panel polish |

---

## 4. Typography

| ID | Asset | Description | Priority | Notes |
|----|-------|-------------|----------|-------|
| TYPE-01 | **Display font** | Title / logo companion | P1 | Industrial serif or stamped military feel |
| TYPE-02 | **UI body font** | Menus, settings labels | P1 | High legibility at 1080p; TMP SDF |
| TYPE-03 | **Numeric / stat font** | Leaderboard scores (optional) | P2 | Tabular figures |

SlimUI Rubik/Poppins acceptable for MVP; custom fonts replace when licensed.

---

## 5. VFX & atmosphere

| ID | Asset | Description | Priority | Notes |
|----|-------|-------------|----------|-------|
| VFX-01 | **Ambient fog / dust particles** | Slow drifting motes in menu light cones | P1 | GPU-friendly, looped |
| VFX-02 | **Distant smoke plume** | Background shader or billboards | P1 | Static or slow scroll |
| VFX-03 | **Arc-lamp flicker** | Subtle light intensity noise | P2 | Shader or animation curve |
| VFX-04 | **Panel transition dust** | Brief puff on camera Pos1→Pos2 | P2 | Optional; code-triggered |

---

## 6. Post-processing & lighting

| ID | Asset | Description | Priority | Notes |
|----|-------|-------------|----------|-------|
| PP-01 | **Menu volume profile** | Color grading: crushed blacks, warm highlights, film grain | P1 | Tune from `POST_ModernMenu` baseline |
| PP-02 | **Faction color LUT variants** | 3 subtle grades for faction panel | P2 | Optional mood shift |
| LGT-01 | **Lighting preset** | Directional + fill + rim for diorama | P0 | Unity Lighting Settings asset export |

---

## 7. Audio (menu-specific)

| ID | Asset | Description | Priority | Notes |
|----|-------|-------------|----------|-------|
| AUD-01 | **Menu ambient loop** | Distant artillery rumble, wind, machinery hum | P0 | 2–3 min seamless loop |
| AUD-02 | **UI hover / click SFX** | Mechanical brass clicks | P1 | SlimUI samples OK for MVP |
| AUD-03 | **Camera sweep whoosh** | Sub-panel transition | P1 | SlimUI `SFX_Click_Whoosh` OK for MVP |
| AUD-04 | **Faction select stinger** | Short tone per faction | P2 | On faction button focus |

---

## 8. Faction select — extended art (future)

| ID | Asset | Description | Priority | Notes |
|----|-------|-------------|----------|-------|
| FAC-01 | **Faction hero stills** | Illustrated or rendered 16:9 banners | P1 | Shown on faction panel |
| FAC-02 | **Faction tagline typography** | Short lore lines per faction | P1 | Text can ship before art |
| FAC-03 | **Locked faction teaser** | Silhouette + “Unlock by …” visual | P2 | Meta progression hook |

---

## 9. Meta panels (Achievements & Leaderboard)

| ID | Asset | Description | Priority | Notes |
|----|-------|-------------|----------|-------|
| META-01 | **Achievement icons** | One icon per achievement in `AchievementCatalog` | P1 | 64–128px; shared frame style |
| META-02 | **Achievement unlocked VFX** | Brief flash on panel open (optional) | P2 | |
| META-03 | **Leaderboard medal icons** | 1st / 2nd / 3rd | P2 | |

---

## 10. Options screen

| ID | Asset | Description | Priority | Notes |
|----|-------|-------------|----------|-------|
| OPT-01 | **Settings tab icons** | Game, Video, Audio | P2 | SlimUI gear icons OK for MVP |
| OPT-02 | **Slider handle / track** | Themed to UI-02 frame set | P2 | |

---

## Suggested production order

1. **ENV-01, ENV-02, ENV-03, LGT-01** — environment blockout readable at both camera angles  
2. **UI-01, UI-02, UI-03** — replace SlimUI placeholder chrome  
3. **AUD-01, PP-01** — mood pass  
4. **UI-05, FAC-01, PROP-01** — faction identity + hero prop  
5. Remaining P1/P2 items as polish budget allows  

---

## Delivery format (per asset)

| Type | Deliverables |
|------|--------------|
| 3D | `.fbx` + textures (PBR where applicable) + Unity prefab under `Assets/_Project/Art/Menu/` |
| 2D UI | Source `.psd`/`.svg` + exported `@2x` PNG + 9-slice metadata |
| Audio | `.wav` source + compressed `.ogg` in `Assets/_Project/Audio/Menu/` |
| Fonts | `.otf`/`.ttf` + TMP SDF asset |

---

## Out of scope (this list)

- In-run UI (board, shop, combat HUD) — covered by separate UI/art pipelines  
- Gameplay unit meshes at menu quality — use dedicated menu LODs if reused  
- Marketing key art / store capsule — not listed here  

---

## Revision log

| Date | Change |
|------|--------|
| 2026-06-06 | Initial backlog; MVP confirmed as lights + fog only |
