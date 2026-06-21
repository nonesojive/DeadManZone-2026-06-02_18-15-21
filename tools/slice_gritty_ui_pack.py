#!/usr/bin/env python3
"""Slice Nexa Gritty Post-Apocalyptic UI sheets — gutter-aligned grid search."""

from __future__ import annotations

import json
import sys
from dataclasses import dataclass
from pathlib import Path

import cv2
import numpy as np

TOOLS = Path(__file__).resolve().parent
if str(TOOLS) not in sys.path:
    sys.path.insert(0, str(TOOLS))

from gritty_slice_detect import build_detected_icon_grid, refine_box

PROJECT = Path(__file__).resolve().parents[1]
KIT = PROJECT / "Assets/_Project/Art/UI/GrittyPostApocalyptic"
SPRITES = KIT / "Sprites"
OUT_COMPONENTS = KIT / "Components"
OUT_ICONS = KIT / "Icons"
OUT_SCREENS = KIT / "Screens"
MANIFEST = KIT / "AssetManifest.json"

# Calibrated grid params (margin, gutter_x, gutter_y) from scored search on 3840×2160 sheets.
CALIBRATED: dict[str, tuple[int, int, int]] = {
    "component_sheet_01": (169, 38, 38),
    "component_sheet_03": (118, 32, 36),
    "component_sheet_04": (112, 34, 34),
    "icon_sheet_01": (142, 28, 30),
    "icon_sheet_03": (146, 24, 26),
}

SHEET_LAYOUTS: dict[str, dict] = {
    "component_sheet_01": {"cols": 4, "rows": 2},
    "component_sheet_02": {"mode": "rects", "rects": [
        (0.040, 0.070, 0.325, 0.310),  # btn_normal — top-left
        (0.040, 0.330, 0.325, 0.530),  # btn_accent — mid-left copper
        (0.040, 0.550, 0.325, 0.900),  # btn_secondary — bottom-left
        (0.820, 0.070, 0.965, 0.310),  # btn_normal_alt — top-right
        (0.820, 0.330, 0.965, 0.530),  # btn_tab — mid-right
        (0.820, 0.550, 0.965, 0.900),  # btn_secondary_alt — bottom-right
        (0.580, 0.075, 0.665, 0.925),  # sidebar_panel
        (0.685, 0.075, 0.770, 0.925),  # sidebar_panel_alt
    ]},
    "component_sheet_03": {"cols": 3, "rows": 2},
    "component_sheet_04": {"cols": 3, "rows": 2},
    "icon_sheet_01": {
        "mode": "variable_cols",
        "rows": 3,
        "col_fracs": [0.128, 0.128, 0.128, 0.185, 0.145, 0.286],
        "margin": 148,
        "gutter_x": 26,
        "gutter_y": 32,
    },
    "icon_sheet_02": {"mode": "detect_grid", "cols": 6, "rows": 4, "row_fracs": [1.35, 1.0, 1.0, 1.0]},
    "icon_sheet_03": {"cols": 6, "rows": 4, "row_fracs": [1.35, 1.0, 1.0, 1.0]},
}

THEME_MAP = {
    "component_sheet_02_01": "btn_normal",
    "component_sheet_02_02": "btn_accent",
    "component_sheet_02_03": "btn_secondary",
    "component_sheet_02_04": "btn_normal_alt",
    "component_sheet_02_05": "btn_tab",
    "component_sheet_02_06": "btn_secondary_alt",
    "component_sheet_02_07": "sidebar_panel",
    "component_sheet_02_08": "sidebar_panel_alt",
    "component_sheet_01_01": "slot_empty",
    "component_sheet_01_02": "slot_empty_02",
    "component_sheet_01_03": "slot_empty_03",
    "component_sheet_01_04": "slot_empty_04",
    "component_sheet_01_05": "slot_progress_35",
    "component_sheet_01_06": "slot_progress_50",
    "component_sheet_01_07": "slot_empty_05",
    "component_sheet_01_08": "slot_empty_06",
    "component_sheet_03_01": "panel_wide",
    "component_sheet_03_02": "panel_square",
    "component_sheet_03_03": "panel_wide_list",
    "component_sheet_03_04": "panel_wide_dials",
    "component_sheet_03_05": "panel_square_list",
    "component_sheet_03_06": "panel_square_alt",
    "component_sheet_04_01": "card_frame_01",
    "component_sheet_04_02": "card_frame_02",
    "component_sheet_04_03": "card_frame_03",
    "component_sheet_04_04": "modal_frame_01",
    "component_sheet_04_05": "modal_frame_02",
    "component_sheet_04_06": "modal_frame_03",
}

SCREEN_ALIASES = {
    "main_menu/main_menu_01": "screen_main_menu_title",
    "main_menu/main_menu_02": "screen_main_menu_buttons",
    "main_menu/main_menu_03": "screen_main_menu_dark",
    "game_hud/game_hud_01": "hud_quickbar_center",
    "game_hud/game_hud_02": "hud_status_bars",
    "game_hud/game_hud_03": "hud_minimap_frame",
    "game_hud/game_hud_04": "hud_compass_strip",
    "game_hud/game_hud_05": "hud_ammo_stamina",
    "inventory_management/inventory_management_01": "screen_inventory_grid",
    "inventory_management/inventory_management_02": "screen_inventory_equipment",
    "crafting_workbench/crafting_workbench_01": "screen_crafting_bench",
    "character_stats/character_stats_01": "screen_character_stats",
    "map_navigation/map_navigation_01": "screen_map_full",
    "journal_log/journal_log_01": "screen_journal_quests",
}


@dataclass
class Box:
    x0: int
    y0: int
    x1: int
    y1: int

    @property
    def w(self) -> int:
        return self.x1 - self.x0

    @property
    def h(self) -> int:
        return self.y1 - self.y0


def panel_cell_score(crop: np.ndarray) -> float:
    if crop.size == 0:
        return -999.0
    _, bw = cv2.threshold(crop, 40, 255, cv2.THRESH_BINARY)
    n, _, stats, _ = cv2.connectedComponentsWithStats(bw)
    areas = [stats[i, cv2.CC_STAT_AREA] for i in range(1, n)]
    if not areas:
        return -999.0
    largest = max(areas)
    frac = largest / crop.size
    if frac < 0.25 or frac > 0.98:
        return -100.0
    if sum(1 for a in areas if a > crop.size * 0.15) > 1:
        return -200.0
    return frac


def icon_cell_score(crop: np.ndarray) -> float:
    if crop.size == 0:
        return -999.0
    h, w = crop.shape
    if h < 80 or w < 80:
        return -999.0

    sx = cv2.Sobel(crop, cv2.CV_64F, 1, 0, ksize=3)
    sy = cv2.Sobel(crop, cv2.CV_64F, 0, 1, ksize=3)
    edge = max(6, min(h, w) // 16)
    border = (
        np.abs(sx[:edge, :]).mean()
        + np.abs(sx[-edge:, :]).mean()
        + np.abs(sy[:, :edge]).mean()
        + np.abs(sy[:, -edge:]).mean()
    )

    # Split cell: dark vertical seam in center, not at edges.
    mid = w // 2
    center_dark = float(np.mean(crop[:, max(0, mid - 3) : min(w, mid + 3)] < 36))
    left_dark = float(np.mean(crop[:, : w // 4] < 36))
    if center_dark > 0.82 and left_dark < 0.55:
        return -80.0

    fg = float(np.mean(crop > 38))
    if fg < 0.2:
        return -50.0
    return border * 0.01 + fg


def build_grid(
    gray: np.ndarray,
    cols: int,
    rows: int,
    margin: int,
    gutter_x: int,
    gutter_y: int,
    row_fracs: list[float] | None,
) -> list[Box]:
    h, w = gray.shape
    if row_fracs is None:
        row_fracs = [1.0 / rows] * rows
    cell_w = (w - 2 * margin - (cols - 1) * gutter_x) / cols
    inner_h = h - 2 * margin - (rows - 1) * gutter_y
    row_hs = [inner_h * f / sum(row_fracs) for f in row_fracs]
    boxes: list[Box] = []
    y = float(margin)
    for row_h in row_hs:
        x = float(margin)
        for _ in range(cols):
            boxes.append(Box(int(x), int(y), int(x + cell_w), int(y + row_h)))
            x += cell_w + gutter_x
        y += row_h + gutter_y
    return boxes


def search_grid(
    gray: np.ndarray,
    cols: int,
    rows: int,
    row_fracs: list[float] | None,
    score_fn,
) -> list[Box]:
    best_score = -1e9
    best_params = (120, 30, 30)
    best_boxes: list[Box] = []

    for margin in range(60, 180, 6):
        for gutter_x in range(12, 42, 4):
            for gutter_y in range(12, 42, 4):
                boxes = build_grid(gray, cols, rows, margin, gutter_x, gutter_y, row_fracs)
                score = sum(score_fn(gray[b.y0 : b.y1, b.x0 : b.x1]) for b in boxes)
                if score > best_score:
                    best_score = score
                    best_params = (margin, gutter_x, gutter_y)
                    best_boxes = boxes

    bm, bgx, bgy = best_params
    for margin in range(max(60, bm - 8), bm + 9, 2):
        for gutter_x in range(max(12, bgx - 6), bgx + 7, 2):
            for gutter_y in range(max(12, bgy - 6), bgy + 7, 2):
                boxes = build_grid(gray, cols, rows, margin, gutter_x, gutter_y, row_fracs)
                score = sum(score_fn(gray[b.y0 : b.y1, b.x0 : b.x1]) for b in boxes)
                if score > best_score:
                    best_score = score
                    best_boxes = boxes
    return best_boxes


def trim_cell(crop: np.ndarray, thresh: int = 36, pad: int = 6) -> np.ndarray:
    mask = crop > thresh
    if not mask.any():
        return crop
    ys, xs = np.where(mask)
    y0 = max(0, int(ys.min()) - pad)
    x0 = max(0, int(xs.min()) - pad)
    y1 = min(crop.shape[0], int(ys.max()) + pad + 1)
    x1 = min(crop.shape[1], int(xs.max()) + pad + 1)
    return crop[y0:y1, x0:x1]


def fraction_boxes(h: int, w: int, rects: list[tuple[float, float, float, float]]) -> list[Box]:
    return [Box(int(w * l), int(h * t), int(w * r), int(h * b)) for l, t, r, b in rects]


def build_variable_cols_grid(
    gray: np.ndarray,
    rows: int,
    col_fracs: list[float],
    margin: int,
    gutter_x: int,
    gutter_y: int,
) -> list[Box]:
    h, w = gray.shape
    total_frac = sum(col_fracs)
    inner_w = w - 2 * margin - (len(col_fracs) - 1) * gutter_x
    col_ws = [inner_w * f / total_frac for f in col_fracs]
    inner_h = h - 2 * margin - (rows - 1) * gutter_y
    row_h = inner_h / rows
    boxes: list[Box] = []
    y = float(margin)
    for _ in range(rows):
        x = float(margin)
        for cw in col_ws:
            boxes.append(Box(int(x), int(y), int(x + cw), int(y + row_h)))
            x += cw + gutter_x
        y += row_h + gutter_y
    return boxes


def boxes_for_sheet(gray: np.ndarray, stem: str, layout: dict) -> list[Box]:
    if layout.get("mode") == "rects":
        h, w = gray.shape
        return fraction_boxes(h, w, layout["rects"])

    if layout.get("mode") == "detect_grid":
        raw = build_detected_icon_grid(
            gray, layout["cols"], layout["rows"], layout.get("row_fracs")
        )
        return [Box(b.x0, b.y0, b.x1, b.y1) for b in raw]

    if layout.get("mode") == "variable_cols":
        return build_variable_cols_grid(
            gray,
            layout["rows"],
            layout["col_fracs"],
            layout["margin"],
            layout["gutter_x"],
            layout["gutter_y"],
        )

    cols, rows = layout["cols"], layout["rows"]
    row_fracs = layout.get("row_fracs")

    if stem in CALIBRATED:
        m, gx, gy = CALIBRATED[stem]
        return build_grid(gray, cols, rows, m, gx, gy, row_fracs)

    score_fn = icon_cell_score if stem.startswith("icon_sheet") else panel_cell_score
    return search_grid(gray, cols, rows, row_fracs, score_fn)


def slice_sheet(path: Path, out_dir: Path, name_fn) -> list[dict]:
    gray = cv2.imread(str(path), cv2.IMREAD_GRAYSCALE)
    if gray is None:
        raise FileNotFoundError(path)

    stem = path.stem
    layout = SHEET_LAYOUTS[stem]
    boxes = boxes_for_sheet(gray, stem, layout)

    entries: list[dict] = []
    out_dir.mkdir(parents=True, exist_ok=True)
    refine = layout.get("refine", False)
    for i, box in enumerate(boxes, start=1):
        if refine and layout.get("mode") not in ("detect_grid",):
            box = refine_box(gray, box)
        crop = gray[box.y0 : box.y1, box.x0 : box.x1]
        key = f"{stem}_{i:02d}"
        alias = name_fn(key)
        dest = out_dir / f"{alias}.png"
        cv2.imwrite(str(dest), crop)
        ch, cw = crop.shape[:2]
        entries.append(
            {
                "id": key,
                "alias": alias,
                "source": str(path.relative_to(PROJECT)).replace("\\", "/"),
                "output": str(dest.relative_to(PROJECT)).replace("\\", "/"),
                "bounds": [int(box.x0), int(box.y0), int(box.x1), int(box.y1)],
                "size": [int(cw), int(ch)],
            }
        )
        print(f"  {alias}.png ({cw}x{ch})")
    return entries


def component_name(key: str) -> str:
    return THEME_MAP.get(key, key)


def icon_name(key: str) -> str:
    return key


def copy_screen_plates() -> list[dict]:
    entries: list[dict] = []
    OUT_SCREENS.mkdir(parents=True, exist_ok=True)
    for rel, alias in SCREEN_ALIASES.items():
        src = SPRITES / f"{rel}.png"
        if not src.exists():
            print(f"  skip missing screen: {src.name}")
            continue
        img = cv2.imread(str(src), cv2.IMREAD_UNCHANGED)
        cv2.imwrite(str(OUT_SCREENS / f"{alias}.png"), img)
        entries.append(
            {
                "alias": alias,
                "source": str(src.relative_to(PROJECT)).replace("\\", "/"),
                "output": str(OUT_SCREENS / f"{alias}.png").replace("\\", "/"),
            }
        )
        print(f"  {alias}.png")
    return entries


def main() -> None:
    manifest: dict = {"components": [], "icons": [], "screens": []}

    print("=== Component sheets ===")
    OUT_COMPONENTS.mkdir(parents=True, exist_ok=True)
    for sheet in sorted((SPRITES / "component_sheet").glob("*.png")):
        print(sheet.name)
        manifest["components"].extend(slice_sheet(sheet, OUT_COMPONENTS, component_name))

    print("\n=== Icon sheets ===")
    OUT_ICONS.mkdir(parents=True, exist_ok=True)
    for sheet in sorted((SPRITES / "icon_sheet").glob("*.png")):
        print(sheet.name)
        manifest["icons"].extend(slice_sheet(sheet, OUT_ICONS, icon_name))

    print("\n=== Screen plates ===")
    manifest["screens"] = copy_screen_plates()

    manifest["theme_wiring"] = {
        "panelSprite": "Components/panel_wide.png",
        "cardSprite": "Components/card_frame_01.png",
        "modalFrameSprite": "Components/modal_frame_01.png",
        "sidebarPanelSprite": "Components/sidebar_panel.png",
        "bannerSprite": "Components/panel_square.png",
        "buttonNormalSprite": "Components/btn_normal.png",
        "accentButtonSprite": "Components/btn_accent.png",
        "secondaryButtonSprite": "Components/btn_secondary.png",
        "slotEmptySprite": "Components/slot_empty.png",
        "slotSelectedSprite": "Components/slot_progress_50.png",
        "menuBackgroundSprite": "Screens/screen_main_menu_title.png",
        "runBackgroundSprite": "Screens/screen_inventory_grid.png",
        "combatBackgroundSprite": "Screens/hud_status_bars.png",
        "shopBackgroundSprite": "Screens/screen_crafting_bench.png",
    }

    MANIFEST.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(f"\nWrote manifest: {MANIFEST.relative_to(PROJECT)}")
    print(
        f"Done: {len(manifest['components'])} components, "
        f"{len(manifest['icons'])} icons, {len(manifest['screens'])} screens"
    )


if __name__ == "__main__":
    main()
