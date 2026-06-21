# Gritty Post-Apocalyptic UI — Slice Notes

Sliced from Nexa Visuals premium pack via `Tools/slice_gritty_ui_pack.py`.

## Folder layout

| Folder | Contents |
|--------|----------|
| `Sprites/` | Raw 4K reference screens (43 PNGs) |
| `Components/` | 28 sliced buttons, panels, slots, frames |
| `Icons/` | 66 survival icons from 3 icon sheets |
| `Screens/` | 14 named hero/backdrop plates |
| `AssetManifest.json` | Machine-readable index + theme wiring |

## 9-slice starting insets (px)

Applied automatically by **DeadManZone → UI Kit → Gritty Post-Apocalyptic → Configure All Sprites** for known stems. Tune in Inspector if borders stretch oddly.

| Asset | L | R | T | B |
|-------|---|---|---|---|
| btn_normal | 48 | 48 | 32 | 32 |
| btn_accent | 48 | 48 | 32 | 32 |
| panel_wide | 120 | 120 | 96 | 96 |
| panel_square | 96 | 96 | 96 | 96 |
| card_frame_01 | 96 | 96 | 80 | 80 |
| modal_frame_01 | 128 | 128 | 104 | 104 |
| sidebar_panel | 64 | 64 | 96 | 96 |
| slot_empty | 64 | 64 | 64 | 64 |

## Theme wiring

**DeadManZone → UI Kit → Gritty Post-Apocalyptic → Import Theme** creates:

- `GrittyPostApocalypticUiTheme.asset`
- `GrittyPostApocalypticVisualProfile.asset`

Key sprite picks: `btn_normal`, `btn_accent`, `panel_wide`, `card_frame_01`, `modal_frame_01`, `slot_empty`, `slot_progress_50`.

## Re-slice after pack update

1. Copy new `assets/` into `Sprites/` (preserve category subfolders).
2. **Slice Sheets (Python)** — requires Python 3 + Pillow (`pip install Pillow`).
3. **Configure All Sprites**
4. **Import Theme**
