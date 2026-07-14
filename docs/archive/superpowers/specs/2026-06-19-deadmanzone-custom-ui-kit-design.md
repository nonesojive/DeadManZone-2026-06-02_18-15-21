> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — Custom UI Starter Kit Design

**Date:** 2026-06-19  
**Status:** Approved (brainstorming)  
**Goal:** Generate a brand-new gritty post-apocalyptic / trench-industrial UI asset kit (20 assets) for a future complete visual overhaul. Assets only — no Unity theme wiring in this pass.

---

## 1. Summary

DeadManZone already has a `UiThemeSO` system and placeholder themes (`BunkerSurvival`, `SyntyTrench`, etc.). The user purchased the Nexa Visuals *Gritty Post-Apocalyptic Survival UI* pack as **style reference only** — this project generates **original custom assets** inspired by user references and DeadManZone's grimdark WW1 tone.

| Decision | Choice |
|----------|--------|
| Deliverable | Brand-new custom PNG assets (not slicing the purchased pack) |
| Kit size | 17 reusable components + 3 hero screen frames |
| Aesthetic | **Hybrid** — gunmetal/charcoal trench-industrial base + rust-orange post-apoc accents |
| Hero screens | Shop, Main Menu, Combat HUD |
| Pipeline | **C-lite:** SuperGrok Imagine + Photoshop; Blender fallback for hero frames only if QC fails |
| Text on assets | **Text-free** — labels added later via TextMeshPro |
| Unity integration | **Out of scope** — folder organization + import settings deferred to overhaul phase |

---

## 2. Style bible

### 2.1 Mood

Scavenged trench-industrial: welded gunmetal plates, oxidized rivets, chipped safety-orange paint, amber status glow. Functional and weathered — not glossy sci-fi or clean flat UI.

### 2.2 Palette

| Role | Hex | Usage |
|------|-----|-------|
| Gunmetal base | `#1A1D22` | Primary plate surfaces |
| Charcoal panel | `#2A2E35` | Recessed interiors, slots |
| Rust accent | `#C45C1A` – `#D4722A` | CTA buttons, bar fills, hazard paint |
| Copper hardware | `#8B5A2B` | Rivets, bezels, corner bolts |
| Amber glow | `#E8A030` | Active indicators, segmented bar fill |
| Cream text (TMP later) | `#E8E4D8` | Not baked into assets |
| Danger rust-red | `#8B2E1A` | Danger/disabled emphasis |

### 2.3 Lighting & composition lock

- Single key light from **upper-left**
- Soft ambient occlusion, subtle top-edge highlights
- Matte metal — no mirror specular
- Transparent PNG backgrounds on all components
- No logos, watermarks, or readable baked text

### 2.4 Grok prompt prefix (copy into every generation)

```
game UI asset, gritty post-apocalyptic trench-industrial survival interface,
scavenged welded gunmetal steel plates, oxidized copper rivets at corners,
chipped rust-orange safety paint accents, matte hammered metal texture,
soft top-left key light, subtle ambient occlusion, weathered scratches,
functional military salvage aesthetic, text-free, no labels, no logos,
transparent background, high resolution UI sprite
```

Append asset-specific suffix per manifest entry (Section 3).

### 2.5 Reference inputs

User-provided reference sheets (component plates, inventory mockup, Trench Warfare V2 style guide, panel collection). Nexa premium pack used for **mood/composition reference only** — do not feed pack images into AI training or generation inputs (license restriction).

---

## 3. Asset manifest (20 assets)

### 3.1 Core components (17)

| ID | Filename | Size | Grok suffix | Maps to `UiThemeSO` (later) |
|----|----------|------|-------------|------------------------------|
| C01 | `btn_normal.png` | 512×128 | horizontal metal button, recessed dark charcoal center, copper rivet corners, normal state | `buttonNormalSprite` |
| C02 | `btn_highlighted.png` | 512×128 | derived in Photoshop from C01 (+15% brightness, subtle amber edge glow) | `buttonHighlightedSprite` |
| C03 | `btn_pressed.png` | 512×128 | derived in Photoshop from C01 (inset shadow, darkened center) | `buttonPressedSprite` |
| C04 | `btn_disabled.png` | 512×128 | derived in Photoshop from C01 (40% desaturate, darker) | `buttonDisabledSprite` |
| C05 | `btn_accent.png` | 512×128 | primary CTA button, chipped rust-orange paint on gunmetal frame | `accentButtonSprite` |
| C06 | `btn_danger.png` | 512×128 | danger button, deep rust-red accent, hazard wear | `dangerButtonSprite` |
| C07 | `panel_9slice.png` | 1024×1024 | large rectangular panel frame, thick beveled metal border, rivets, empty center for 9-slice | `panelSprite` |
| C08 | `card_frame.png` | 512×640 | vertical shop offer card frame, portrait aspect, recessed icon area | `cardSprite` |
| C09 | `modal_frame.png` | 1024×768 | dialog/modal window frame, heavier border, asymmetric bolt pattern | `modalFrameSprite` |
| C10 | `slot_empty.png` | 256×256 | inventory slot, empty recessed square, dark charcoal inset | `slotEmptySprite` |
| C11 | `slot_selected.png` | 256×256 | derived from C10 (amber border glow, brighter rivets) | `slotSelectedSprite` |
| C12 | `bar_track.png` | 512×64 | horizontal progress bar track, industrial channel, dark recessed groove | — (HUD) |
| C13 | `bar_fill_segmented.png` | 512×64 | segmented amber-orange fill blocks for health/stamina bar | — (HUD) |
| C14 | `divider_rivet.png` | 512×32 | thin horizontal metal divider strip with rivet studs | — |
| C15 | `banner_header.png` | 1024×96 | section header banner strip, hazard stripe accent optional | `bannerSprite` |
| C16 | `sidebar_panel.png` | 512×1024 | tall narrow sidebar 9-slice frame | `sidebarPanelSprite` |
| C17 | `icon_slot.png` | 128×128 | small square quickslot / resource icon frame | — |

### 3.2 Hero screen frames (3)

Full compositions for visual preview. Content zones left empty for TMP/cards. Sliced into reusable parts during overhaul phase.

| ID | Filename | Size | Grok suffix | Maps to (later) |
|----|----------|------|-------------|-----------------|
| H01 | `screen_shop.png` | 3840×2160 | full shop/requisition screen backdrop, lane columns, offer card grid zone, reroll button area, rust-orange accents, no text | `shopBackgroundSprite` |
| H02 | `screen_main_menu.png` | 3840×2160 | main menu backdrop, large central title plate, vertical button stack zone, weathered metal handle detail | `menuBackgroundSprite` |
| H03 | `screen_combat_hud.png` | 3840×2160 | combat overlay frame, army health bar housing top, porthole/viewport bezel, combat banner zone, dim scrim edges | `combatBackgroundSprite` / `CombatHudAssetsSO` |

---

## 4. Production pipeline

### 4.1 Phase overview

```
Phase 1 — Lock style bible (this document) + palette swatch reference sheet
Phase 2 — Grok batch: components C01,C05,C06,C07–C17 (skip derived states)
Phase 3 — Grok batch: hero frames H01–H03
Phase 4 — Photoshop cleanup per asset (see 4.2)
Phase 5 — Derive button/slot states in Photoshop (C02–C04, C11)
Phase 6 — QC gate (Section 5); Blender re-render H01–H03 only if heroes fail
Phase 7 — Export to Assets/_Project/Art/UI/DeadManZoneCustom/ (Section 6)
```

### 4.2 Photoshop cleanup checklist (per asset)

1. Remove background / fix edge halos
2. Level-adjust to palette targets (Section 2.2)
3. Document 9-slice border insets in `SliceNotes.md` (pixels from each edge)
4. Export PNG, sRGB, no mipmaps
5. Save raw Grok export to `Source/` before cleanup

### 4.3 Button state derivation (ponytail: one Grok pass, Photoshop variants)

Generate **one** `btn_normal` (C01). Derive:

- **Highlighted:** +15% brightness, 1px amber outer glow
- **Pressed:** inset shadow, −20% brightness on center plate
- **Disabled:** desaturate 40%, flatten contrast

Avoids four separate Grok passes that drift in rivet/bevel style.

### 4.4 Blender fallback (heroes only, conditional)

Trigger if H01–H03 fail QC item #6 (Section 5).

- Low-poly beveled metal plates, PBR gunmetal + rust materials
- Orthographic camera, upper-left key light matching Grok outputs
- Render transparent PNG at 3840×2160
- Composite hazard stripes / rivet details in Photoshop if needed

---

## 5. Quality gates

| # | Criterion | How to verify |
|---|-----------|---------------|
| 1 | Buttons readable at 48px display height | Import test image under shop card mockup |
| 2 | 9-slice panels scale 200px–1200px wide without corner stretch | Unity Image test (overhaul phase) or Photoshop slice preview |
| 3 | All 17 components feel like one art pass | Side-by-side contact sheet review |
| 4 | Transparent backgrounds, no white halos | Checkerboard preview at 400% zoom |
| 5 | No baked text, logos, or watermarks | Visual inspection |
| 6 | Hero frames have clear empty content zones | Overlay wireframe boxes for TMP/card placement |
| 7 | Style matches hybrid palette | Eyedropper spot-check against Section 2.2 |

---

## 6. Output folder structure

```
Assets/_Project/Art/UI/DeadManZoneCustom/
├── StyleBible.md              # prompt prefix + palette (copy of Section 2)
├── SliceNotes.md              # 9-slice border pixel insets per frame asset
├── Source/
│   ├── Components/            # raw Grok exports
│   └── Screens/               # raw Grok/Blender exports
├── Components/                # cleaned PNGs (C01–C17)
└── Screens/                   # cleaned PNGs (H01–H03)
```

Unity import (`TextureImporterType.Sprite`, 9-slice borders, `UiThemeSO` preset) is a **separate overhaul task** — not part of this spec's implementation scope.

---

## 7. Future integration notes (overhaul phase, not this pass)

When wiring the kit into DeadManZone:

| Asset group | Target |
|-------------|--------|
| Components C01–C11, C15–C16 | New `GrittyTrenchHybridUiTheme.asset` preset fields on `UiThemeSO` |
| H01 | `UiThemeSO.shopBackgroundSprite` |
| H02 | `UiThemeSO.menuBackgroundSprite` |
| H03 | `UiThemeSO.combatBackgroundSprite` + `CombatHudAssetsSO.combatBackgroundSprite` |
| C12–C13 | Army health bar prefab materials |
| Palette colors | `UiThemeSO.Apply*Defaults()` or manual Inspector copy from Section 2.2 |

Existing references: `Assets/_Project/Presentation/Visual/UiThemeSO.cs`, `GrittyPostApocalypticUiKitSetup.cs` (import pattern to mirror).

---

## 8. Out of scope

- Slicing purchased Nexa premium pack
- Unity `UiThemeSO` wiring or scene prefab changes
- TextMeshPro font selection / stencil font procurement
- Icon sheet generation (survival item icons)
- Inventory/crafting/map/journal full-screen compositions
- AI training on purchased pack assets

---

## 9. Risks & mitigations

| Risk | Mitigation |
|------|------------|
| Grok style drift between batches | Locked prompt prefix; generate components in one session; contact sheet review before heroes |
| Hero frames too busy for UI overlay | QC #6 wireframe zones; add scrim layer in Photoshop if needed |
| 4K hero PNGs large in repo | Accept for source quality; downscale to 1920×1080 variants optional in overhaul |
| 9-slice borders hard to guess from full renders | Document insets in `SliceNotes.md` during Photoshop pass; verify in Unity later |

---

## 10. Success criteria (this pass complete when)

1. All **20** PNG assets exist in `DeadManZoneCustom/Components/` and `Screens/`
2. Raw sources preserved in `Source/`
3. `StyleBible.md` and `SliceNotes.md` committed alongside assets
4. QC gates 1–7 pass on contact sheet review
5. Kit is ready for overhaul-phase `UiThemeSO` wiring without additional art generation
