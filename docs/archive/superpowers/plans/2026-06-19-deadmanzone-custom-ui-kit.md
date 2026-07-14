> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone Custom UI Starter Kit — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce 20 original gritty trench-industrial UI PNG assets (17 components + 3 hero screens) via SuperGrok + Photoshop, organized under `Assets/_Project/Art/UI/DeadManZoneCustom/` for a future visual overhaul.

**Architecture:** Lock style bible first, batch-generate Grok components in one session, derive button/slot states in Photoshop (not separate Grok passes), generate hero screens second, run QC contact sheet, Blender-re-render heroes only if content zones fail. No Unity `UiThemeSO` wiring in this pass.

**Tech Stack:** SuperGrok Imagine, Adobe Photoshop MCP (`photoshop_*` tools), optional Blender MCP (`user-blender`), Windows filesystem. Reference spec: `docs/superpowers/specs/2026-06-19-deadmanzone-custom-ui-kit-design.md`.

---

## Execution order & green gates

| Task | Green gate |
|------|------------|
| 1 — Folder scaffold + StyleBible | Directories exist; `StyleBible.md` has full prompt prefix + palette |
| 2 — Grok component batch (13 unique) | Raw PNGs in `Source/Components/` |
| 3 — Photoshop component cleanup | Clean PNGs in `Components/` (C01, C05–C17) |
| 4 — Derive button + slot states | C02–C04, C11 exist; match C01/C10 rivet layout |
| 5 — Grok hero screens | Raw H01–H03 in `Source/Screens/` |
| 6 — Photoshop hero cleanup + wireframes | Clean H01–H03; `SliceNotes.md` started |
| 7 — QC contact sheet | All 7 QC gates pass or heroes sent to Blender fallback |
| 8 — Blender fallback (conditional) | Only if Task 7 hero QC #6 fails |
| 9 — Final packaging | 20 cleaned PNGs + `SliceNotes.md` complete |

---

## Master file map

| Path | Action |
|------|--------|
| `Assets/_Project/Art/UI/DeadManZoneCustom/StyleBible.md` | Create — prompt prefix, palette, lighting lock |
| `Assets/_Project/Art/UI/DeadManZoneCustom/SliceNotes.md` | Create — 9-slice insets per frame asset |
| `Assets/_Project/Art/UI/DeadManZoneCustom/Source/Components/` | Create — raw Grok exports |
| `Assets/_Project/Art/UI/DeadManZoneCustom/Source/Screens/` | Create — raw Grok/Blender exports |
| `Assets/_Project/Art/UI/DeadManZoneCustom/Components/*.png` | Create — 17 cleaned component PNGs |
| `Assets/_Project/Art/UI/DeadManZoneCustom/Screens/*.png` | Create — 3 cleaned hero PNGs |
| `Assets/_Project/Art/UI/DeadManZoneCustom/QC/contact_sheet.png` | Create — review composite |

**Out of scope (do not touch):** `UiThemeSO.cs`, scene prefabs, `GrittyPostApocalyptic/` purchased pack files.

---

## Shared constants

**Prompt prefix** (prepend to every Grok prompt):

```
game UI asset, gritty post-apocalyptic trench-industrial survival interface,
scavenged welded gunmetal steel plates, oxidized copper rivets at corners,
chipped rust-orange safety paint accents, matte hammered metal texture,
soft top-left key light, subtle ambient occlusion, weathered scratches,
functional military salvage aesthetic, text-free, no labels, no logos,
transparent background, high resolution UI sprite,
```

**Palette eyedropper targets:**

| Role | Hex |
|------|-----|
| Gunmetal base | `#1A1D22` |
| Charcoal panel | `#2A2E35` |
| Rust accent | `#C45C1A` |
| Copper hardware | `#8B5A2B` |
| Amber glow | `#E8A030` |
| Danger rust-red | `#8B2E1A` |

**Reference images** (mood only — do NOT upload Nexa pack to AI):

- `assets/c__Users_jiveg_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_download__2_-85e91e38-e32d-49a9-bc25-c65f14ccca9d.png` (component plates)
- `assets/c__Users_jiveg_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_download-aa847671-fc8f-4b10-9c17-3cb08149e108.png` (inventory mockup)
- `assets/c__Users_jiveg_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_download__3_-0e8d4af2-77fb-410b-bf4f-249dbcd99807.png` (Trench Warfare V2 style guide)
- `assets/c__Users_jiveg_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_download__1_-7312415b-2c2c-4b6e-8b2c-64172629a519.png` (panel collection)

---

## Task 1: Folder scaffold + StyleBible

**Files:**
- Create: `Assets/_Project/Art/UI/DeadManZoneCustom/StyleBible.md`
- Create: directories under `DeadManZoneCustom/`

- [ ] **Step 1: Create folder tree**

```powershell
$root = "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone\Assets\_Project\Art\UI\DeadManZoneCustom"
@("Source\Components", "Source\Screens", "Components", "Screens", "QC") | ForEach-Object {
    New-Item -ItemType Directory -Force -Path (Join-Path $root $_) | Out-Null
}
```

Expected: five subfolders exist under `DeadManZoneCustom/`.

- [ ] **Step 2: Write `StyleBible.md`**

Create `Assets/_Project/Art/UI/DeadManZoneCustom/StyleBible.md` with:

```markdown
# DeadManZone Custom UI — Style Bible

## Mood
Scavenged trench-industrial: welded gunmetal, oxidized rivets, chipped rust-orange paint, amber status glow.

## Palette
| Role | Hex |
|------|-----|
| Gunmetal base | #1A1D22 |
| Charcoal panel | #2A2E35 |
| Rust accent | #C45C1A – #D4722A |
| Copper hardware | #8B5A2B |
| Amber glow | #E8A030 |
| Danger rust-red | #8B2E1A |

## Lighting
- Key light: upper-left
- Matte metal, soft AO, no mirror specular
- Transparent PNG backgrounds
- Text-free assets (TMP labels added in Unity later)

## Grok prompt prefix
game UI asset, gritty post-apocalyptic trench-industrial survival interface,
scavenged welded gunmetal steel plates, oxidized copper rivets at corners,
chipped rust-orange safety paint accents, matte hammered metal texture,
soft top-left key light, subtle ambient occlusion, weathered scratches,
functional military salvage aesthetic, text-free, no labels, no logos,
transparent background, high resolution UI sprite,

## Generation order
1. Components (single Grok session)
2. Hero screens (second Grok session)
3. Photoshop cleanup + state derivation
4. QC contact sheet
```

- [ ] **Step 3: Commit scaffold**

```bash
git add "Assets/_Project/Art/UI/DeadManZoneCustom/StyleBible.md"
git commit -m "docs: add DeadManZone custom UI kit folder scaffold and style bible"
```

---

## Task 2: Grok component batch (13 unique assets)

**Files:**
- Create: `Assets/_Project/Art/UI/DeadManZoneCustom/Source/Components/*.png` (13 files)

Generate in **one SuperGrok session** to minimize style drift. Save each result immediately to `Source/Components/` with the exact filename below.

- [ ] **Step 1: Generate C01 `btn_normal.png`**

Full prompt:

```
game UI asset, gritty post-apocalyptic trench-industrial survival interface,
scavenged welded gunmetal steel plates, oxidized copper rivets at corners,
chipped rust-orange safety paint accents, matte hammered metal texture,
soft top-left key light, subtle ambient occlusion, weathered scratches,
functional military salvage aesthetic, text-free, no labels, no logos,
transparent background, high resolution UI sprite,
horizontal metal UI button 512x128 aspect, recessed dark charcoal center plate,
copper rivet bolts in all four corners, normal unpressed state, beveled frame
```

Save as: `Source/Components/btn_normal_raw.png`

- [ ] **Step 2: Generate C05 `btn_accent.png`**

Append suffix: `primary CTA button, chipped rust-orange safety paint on gunmetal frame, same rivet style as btn_normal`

Save as: `Source/Components/btn_accent_raw.png`

- [ ] **Step 3: Generate C06 `btn_danger.png`**

Append suffix: `danger action button, deep rust-red accent paint, hazard wear on edges, same dimensions and rivet layout`

Save as: `Source/Components/btn_danger_raw.png`

- [ ] **Step 4: Generate C07 `panel_9slice.png`**

Append suffix: `large square panel frame 1024x1024, thick beveled metal border with rivets, empty flat charcoal center for 9-slice scaling, no interior content`

Save as: `Source/Components/panel_9slice_raw.png`

- [ ] **Step 5: Generate C08 `card_frame.png`**

Append suffix: `vertical portrait shop offer card frame 512x640, recessed center area for unit icon, riveted metal border`

Save as: `Source/Components/card_frame_raw.png`

- [ ] **Step 6: Generate C09 `modal_frame.png`**

Append suffix: `dialog modal window frame 1024x768, heavier asymmetric bolt pattern, empty center content zone`

Save as: `Source/Components/modal_frame_raw.png`

- [ ] **Step 7: Generate C10 `slot_empty.png`**

Append suffix: `square inventory slot 256x256, empty recessed dark charcoal inset, thin metal bezel, corner rivets`

Save as: `Source/Components/slot_empty_raw.png`

- [ ] **Step 8: Generate C12 `bar_track.png`**

Append suffix: `horizontal progress bar track 512x64, industrial recessed channel groove, dark gunmetal, empty unfilled`

Save as: `Source/Components/bar_track_raw.png`

- [ ] **Step 9: Generate C13 `bar_fill_segmented.png`**

Append suffix: `horizontal segmented health bar fill 512x64, four amber-orange glowing blocks, same track width as bar_track`

Save as: `Source/Components/bar_fill_segmented_raw.png`

- [ ] **Step 10: Generate C14 `divider_rivet.png`**

Append suffix: `thin horizontal metal divider strip 512x32, evenly spaced rivet studs, flat weathered steel`

Save as: `Source/Components/divider_rivet_raw.png`

- [ ] **Step 11: Generate C15 `banner_header.png`**

Append suffix: `section header banner strip 1024x96, optional faded hazard diagonal stripes, riveted ends, empty center for text overlay`

Save as: `Source/Components/banner_header_raw.png`

- [ ] **Step 12: Generate C16 `sidebar_panel.png`**

Append suffix: `tall narrow sidebar panel frame 512x1024, vertical 9-slice metal border, rivets along long edges, empty interior`

Save as: `Source/Components/sidebar_panel_raw.png`

- [ ] **Step 13: Generate C17 `icon_slot.png`**

Append suffix: `small square quickslot icon frame 128x128, minimal recessed bezel, single rivet per corner`

Save as: `Source/Components/icon_slot_raw.png`

- [ ] **Step 14: Style drift check**

Lay out all 13 raw files in a 4-column contact sheet (Photoshop or Preview). If rivet style or metal tone diverges noticeably on 3+ assets, regenerate the outliers in the same session before proceeding.

Green gate: 13 files in `Source/Components/`.

---

## Task 3: Photoshop component cleanup

**Files:**
- Create: `Assets/_Project/Art/UI/DeadManZoneCustom/Components/*.png` (C01, C05–C17)
- Create: `Assets/_Project/Art/UI/DeadManZoneCustom/SliceNotes.md` (partial)

Use Photoshop MCP. Call `photoshop_ping` once at session start if not already done.

**Per-asset cleanup recipe** (repeat for each raw file):

- [ ] **Step 1: Open raw and remove background**

```
photoshop_open_image path=<Source/Components/{name}_raw.png>
photoshop_recipe_remove_background
```

If generative remove unavailable: magic wand on flat background → delete → refine edge 1px.

- [ ] **Step 2: Crop to target aspect and resize**

| Asset | Canvas size |
|-------|-------------|
| btn_normal, btn_accent, btn_danger | 512 × 128 |
| panel_9slice | 1024 × 1024 |
| card_frame | 512 × 640 |
| modal_frame | 1024 × 768 |
| slot_empty | 256 × 256 |
| bar_track, bar_fill_segmented | 512 × 64 |
| divider_rivet | 512 × 32 |
| banner_header | 1024 × 96 |
| sidebar_panel | 512 × 1024 |
| icon_slot | 128 × 128 |

```
photoshop_resize_canvas width=<W> height=<H> anchor=center
```

- [ ] **Step 3: Level-adjust to palette**

```
photoshop_adjust_brightness_contrast brightness=5 contrast=12
```

Eyedropper spot-check: largest flat metal area within 15 RGB units of `#1A1D22` or `#2A2E35`.

- [ ] **Step 4: Fix edge halos**

Select → Modify → Contract 1px → inverse → delete fringe pixels. Zoom 400% on corners.

- [ ] **Step 5: Export cleaned PNG**

```
photoshop_export_png path=Assets/_Project/Art/UI/DeadManZoneCustom/Components/{filename}.png
```

Export mapping:

| Raw | Output |
|-----|--------|
| `btn_normal_raw.png` | `btn_normal.png` |
| `btn_accent_raw.png` | `btn_accent.png` |
| `btn_danger_raw.png` | `btn_danger.png` |
| `panel_9slice_raw.png` | `panel_9slice.png` |
| `card_frame_raw.png` | `card_frame.png` |
| `modal_frame_raw.png` | `modal_frame.png` |
| `slot_empty_raw.png` | `slot_empty.png` |
| `bar_track_raw.png` | `bar_track.png` |
| `bar_fill_segmented_raw.png` | `bar_fill_segmented.png` |
| `divider_rivet_raw.png` | `divider_rivet.png` |
| `banner_header_raw.png` | `banner_header.png` |
| `sidebar_panel_raw.png` | `sidebar_panel.png` |
| `icon_slot_raw.png` | `icon_slot.png` |

- [ ] **Step 6: Seed `SliceNotes.md` with 9-slice insets**

Create `SliceNotes.md`:

```markdown
# 9-Slice Border Insets (pixels from edge)

Measure bevel+rivet border on cleaned assets. Typical starting values — adjust after visual test:

| Asset | Left | Right | Top | Bottom |
|-------|------|-------|-----|--------|
| panel_9slice.png | 80 | 80 | 80 | 80 |
| card_frame.png | 40 | 40 | 48 | 40 |
| modal_frame.png | 64 | 64 | 72 | 64 |
| sidebar_panel.png | 48 | 48 | 64 | 64 |
| banner_header.png | 32 | 32 | 24 | 24 |
| btn_normal.png | 24 | 24 | 20 | 20 |
```

Refine insets after placing assets on checkerboard at multiple scales.

Green gate: 13 files in `Components/`.

---

## Task 4: Derive button + slot states (Photoshop only)

**Files:**
- Create: `Components/btn_highlighted.png`, `btn_pressed.png`, `btn_disabled.png`
- Create: `Components/slot_selected.png`

Do **not** re-run Grok for these — derive from C01 and C10 to keep rivet layout identical.

- [ ] **Step 1: Derive `btn_highlighted.png` from `btn_normal.png`**

1. Duplicate `btn_normal.png` layer
2. Image → Adjustments → Brightness/Contrast: Brightness +15, Contrast +5
3. Layer Style → Outer Glow: color `#E8A030`, size 2px, opacity 40%
4. Export `btn_highlighted.png`

- [ ] **Step 2: Derive `btn_pressed.png` from `btn_normal.png`**

1. Duplicate `btn_normal.png`
2. Select inner charcoal plate → Image → Adjustments → Brightness/Contrast: Brightness −20
3. Layer Style → Inner Shadow: distance 2px, size 4px, opacity 60%
4. Export `btn_pressed.png`

- [ ] **Step 3: Derive `btn_disabled.png` from `btn_normal.png`**

1. Duplicate `btn_normal.png`
2. Image → Adjustments → Hue/Saturation: Saturation −40
3. Image → Adjustments → Brightness/Contrast: Brightness −10, Contrast −15
4. Export `btn_disabled.png`

- [ ] **Step 4: Derive `slot_selected.png` from `slot_empty.png`**

1. Duplicate `slot_empty.png`
2. Layer Style → Stroke: 3px outside, color `#E8A030`, opacity 80%
3. Brightness +10 on rivet layer only
4. Export `slot_selected.png`

- [ ] **Step 5: Verify state set at 48px height**

Scale `btn_normal.png` and all three states to 48px height in Photoshop. Rivets and frame must remain readable. If pressed state collapses visually, increase inner shadow to 3px distance.

Green gate: 17 files in `Components/` (C01–C17).

---

## Task 5: Grok hero screen batch

**Files:**
- Create: `Source/Screens/screen_shop_raw.png`, `screen_main_menu_raw.png`, `screen_combat_hud_raw.png`

Run in a **second** Grok session (after components pass QC). Use 16:9 aspect; request 3840×2160 or upscale in Photoshop.

- [ ] **Step 1: Generate H01 shop screen**

```
[prompt prefix]
full shop requisition screen UI backdrop 16:9 3840x2160, three vertical lane columns for unit offers,
central grid zone for offer cards, bottom-right reroll button area, top resource bar zone,
heavy riveted gunmetal panels, rust-orange hazard accents, large empty content areas, no text, no icons
```

Save: `Source/Screens/screen_shop_raw.png`

- [ ] **Step 2: Generate H02 main menu**

```
[prompt prefix]
main menu title screen backdrop 16:9 3840x2160, large central weathered metal title plate,
vertical column of six button slots on right third, metal handle detail at top,
dim vignette edges, empty zones for game title and menu buttons, no text
```

Save: `Source/Screens/screen_main_menu_raw.png`

- [ ] **Step 3: Generate H03 combat HUD**

```
[prompt prefix]
combat HUD overlay frame 16:9 3840x2160, top army health bar housing spanning width,
center porthole viewport bezel with crosshair wire mesh, bottom combat banner strip,
semi-transparent dark scrim at edges leaving center clear for battlefield view, no text, no numbers
```

Save: `Source/Screens/screen_combat_hud_raw.png`

Green gate: 3 files in `Source/Screens/`.

---

## Task 6: Photoshop hero cleanup + content-zone wireframes

**Files:**
- Create: `Screens/screen_shop.png`, `screen_main_menu.png`, `screen_combat_hud.png`
- Modify: `SliceNotes.md` — add hero content zones

- [ ] **Step 1: Clean and export each hero**

For each raw screen:
1. Crop to 3840 × 2160 (or resize if Grok returned lower res)
2. Remove unwanted background if not already transparent at edges
3. Optional: add 30% opacity charcoal scrim layer on outer 8% margins for `screen_combat_hud.png` only
4. Export to `Screens/`

- [ ] **Step 2: Document content zones in `SliceNotes.md`**

Append:

```markdown
## Hero screen content zones (px from top-left of 3840×2160)

### screen_shop.png
- Offer card grid: x=480 y=280 w=2200 h=1400
- Reroll button area: x=3000 y=1700 w=520 h=180
- Top resource bar: x=120 y=60 w=3600 h=120

### screen_main_menu.png
- Title plate: x=960 y=320 w=1920 h=400
- Button stack: x=2520 y=480 w=520 h=1200 (six 180px-tall slots)

### screen_combat_hud.png
- Army health bar housing: x=120 y=48 w=3600 h=96
- Viewport clear zone: x=320 y=200 w=3200 h=1760
- Combat banner: x=960 y=1960 w=1920 h=120
```

Adjust coordinates to match actual empty regions in cleaned PNGs.

- [ ] **Step 3: Overlay wireframe QC pass**

In Photoshop, draw semi-transparent red rectangles on a separate layer matching the zones above. Confirm no rivet detail or hazard stripe sits inside card/button zones. If zones are cluttered → flag for Task 8 Blender fallback.

---

## Task 7: QC contact sheet + gate review

**Files:**
- Create: `QC/contact_sheet.png`
- Create: `QC/qc_checklist.md`

- [ ] **Step 1: Build component contact sheet**

Arrange all 17 `Components/*.png` on a 4096×4096 canvas:
- Row 1: all button states (7)
- Row 2: panels + card + modal (4)
- Row 3: slots, bars, divider, banner, sidebar, icon slot (6)

Export: `QC/contact_sheet_components.png`

- [ ] **Step 2: Build hero contact sheet**

Scale heroes to 960×540 thumbnails side-by-side. Export: `QC/contact_sheet_heroes.png`

- [ ] **Step 3: Run QC checklist**

Create `QC/qc_checklist.md`:

```markdown
# QC Results — 2026-06-19

| # | Criterion | Pass? | Notes |
|---|-----------|-------|-------|
| 1 | Buttons readable at 48px | | |
| 2 | 9-slice borders documented | | |
| 3 | Components feel like one art pass | | |
| 4 | Transparent BG, no halos | | |
| 5 | No baked text/logos | | |
| 6 | Hero content zones clear | | |
| 7 | Palette spot-check | | |
```

Fill in Pass/Fail for each row. **Any Fail on row 6 for heroes → execute Task 8.** Any Fail on row 3 for components → regenerate offending Grok asset and repeat Tasks 3–4 for that asset only.

- [ ] **Step 4: Commit art pass**

```bash
git add "Assets/_Project/Art/UI/DeadManZoneCustom/"
git commit -m "art: add DeadManZone custom UI starter kit (20 assets)"
```

---

## Task 8: Blender hero fallback (conditional)

**Only run if Task 7 QC row 6 fails for one or more heroes.**

**Files:**
- Replace: `Source/Screens/screen_{shop,main_menu,combat_hud}_blender.png`
- Replace: corresponding `Screens/*.png` after Photoshop composite

- [ ] **Step 1: Model base plate in Blender**

1. Add 16:9 plane scaled to 3.84 × 2.16 BU
2. Solidify modifier: thickness 0.04 BU
4. Bevel outer edges: width 0.02 BU, 2 segments
5. Add four corner rivet cylinders (radius 0.03 BU)

- [ ] **Step 2: Materials**

- Gunmetal: Principled BSDF base color `(0.10, 0.11, 0.13)`, roughness 0.65, metallic 0.85
- Rust accent: `(0.77, 0.36, 0.10)`, roughness 0.75, mix via noise mask on accent faces
- Amber emissive for bar zones: `(0.91, 0.63, 0.19)`, strength 2.0

- [ ] **Step 3: Lighting**

- Sun lamp: angle 45°, rotation X=50° Y=0° Z=-35° (upper-left key)
- Area fill: strength 30 W, behind camera, cool tint

- [ ] **Step 4: Orthographic render**

- Camera orthographic, scale 1.0
- Resolution 3840 × 2160, transparent film ON
- Render → save to `Source/Screens/screen_{name}_blender.png`

- [ ] **Step 5: Photoshop composite**

Layer Grok texture grime overlay at 15% opacity if Blender render is too clean. Re-run Task 6 wireframe QC on Blender output.

---

## Task 9: Final packaging

- [ ] **Step 1: Verify file inventory**

```powershell
$base = "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone\Assets\_Project\Art\UI\DeadManZoneCustom"
@(Get-ChildItem "$base\Components\*.png").Count -eq 17
@(Get-ChildItem "$base\Screens\*.png").Count -eq 3
@(Get-ChildItem "$base\Source\Components\*").Count -ge 13
@(Get-ChildItem "$base\Source\Screens\*").Count -ge 3
Test-Path "$base\StyleBible.md"
Test-Path "$base\SliceNotes.md"
```

Expected: all expressions return `True`.

- [ ] **Step 2: Update spec status**

In `docs/superpowers/specs/2026-06-19-deadmanzone-custom-ui-kit-design.md`, change:

```markdown
**Status:** Approved (brainstorming)
```

to:

```markdown
**Status:** Implemented (assets delivered)
```

- [ ] **Step 3: Handoff note for overhaul phase**

The kit is ready for a follow-up plan:

- `DeadManZone/UI Kit/Configure Gritty Post-Apocalyptic Sprites` pattern → new menu for `DeadManZoneCustom`
- Create `GrittyTrenchHybridUiTheme.asset` mapping per spec Section 7
- Apply 9-slice borders from `SliceNotes.md` in TextureImporter

---

## Spec coverage self-review

| Spec section | Plan task |
|--------------|-----------|
| Style bible §2 | Task 1 |
| Manifest C01–C17 | Tasks 2–4 |
| Manifest H01–H03 | Tasks 5–6 |
| Grok pipeline §4.1–4.2 | Tasks 2, 3, 5, 6 |
| Button derivation §4.3 | Task 4 |
| Blender fallback §4.4 | Task 8 |
| QC gates §5 | Task 7 |
| Folder structure §6 | Task 1, 9 |
| Out of scope §8 | No Unity code tasks |
| Success criteria §10 | Task 9 |

No placeholders. No Unity wiring tasks (intentionally deferred per spec).

---

## Execution handoff

Plan complete and saved to `docs/superpowers/plans/2026-06-19-deadmanzone-custom-ui-kit.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — dispatch a fresh subagent per task (Grok batch, Photoshop cleanup, QC), review between tasks
2. **Inline Execution** — run tasks in this session: start with folder scaffold, then Grok component generation

Which approach do you want?
