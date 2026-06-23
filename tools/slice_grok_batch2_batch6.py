#!/usr/bin/env python3
"""Slice Grok batch-2 sheets into batch6 (icons + unit sprites). ponytail: PIL/cv2 batch; PS MCP times out."""

from __future__ import annotations

import argparse
import json
from collections import deque
from dataclasses import dataclass
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

PROJECT = Path(__file__).resolve().parents[1]
SRC = PROJECT / "Assets/Grok Images/batch 2"
OUT = SRC / "batch6"
OUT_ICONS = OUT / "icons"
OUT_UNITS = OUT / "units"
OUT_UNITS_SQ = OUT / "units_sq"

EXPAND = 8
EDGE_T = 42
INNER_T = 20
WHITE_INNER = 252
SQUARE_SIZE = 512
SQUARE_FILL = 0.82
ICON_GRID_992 = (12, 0, 20, 242.0, 239.0)  # fallback only
ICON_CIRCLE_PAD = 1.18  # radius multiplier to include drop shadow
ICON_SHADOW_BIAS = (0, 0)
ICON_RIM_PAD = 8
ICON_VCENTER_FRAC = 0.44  # center above midpoint — drop shadow sits low
BG_TOL = 22


@dataclass
class SheetCfg:
    mode: str  # grid_4x4 | grid_5x2 | horizontal
    count: int = 0


# 992x1040 → 4x4 icon grids; 1280x720 → unit strips (counts from visual QC).
SHEETS: dict[str, SheetCfg] = {
    "grok-image-06f337ca-84a5-4804-9f53-2d183e0e4fbe": SheetCfg("grid_4x4"),
    "grok-image-13377495-07bb-40be-8ae8-d831a0b1935b": SheetCfg("grid_4x4"),
    "grok-image-238216e6-dafb-4ba6-a1ad-2ca4a658a060": SheetCfg("grid_4x4"),
    "grok-image-14a5c829-d615-47f2-8bf0-8f8d239b249f": SheetCfg("grid_4x4"),
    "grok-image-df1685cf-02a3-4cb2-9046-1ac8c270061f": SheetCfg("grid_4x4"),
    "grok-image-36308bd6-6111-4a1a-8efa-97c9decf6166": SheetCfg("grid_4x4"),
    "grok-image-393d0517-c70d-44b0-b6fe-937f3d5de13d": SheetCfg("grid_4x4"),
    "grok-image-42cbbcf1-65c7-4699-a107-dd25b3621708": SheetCfg("grid_4x4"),
    "grok-image-4af3aa09-6099-41ba-bd1a-33fa03f1f788": SheetCfg("grid_4x4"),
    "grok-image-5c4d918a-c3f9-4144-a8f5-1c5890fe48f5": SheetCfg("grid_4x4"),
    "grok-image-71e60cd7-4e3b-421f-aaba-81061dc9556c": SheetCfg("grid_4x4"),
    "grok-image-7b36db2b-e4e4-4fa2-a279-cedf85838a08": SheetCfg("grid_4x4"),
    "grok-image-be9cb935-3dc4-4167-87b1-0d021815e608": SheetCfg("grid_4x4"),
    "grok-image-ea660899-e43e-4013-ba31-2c6923fb274b": SheetCfg("grid_4x4"),
    "grok-image-f2305753-d4c3-4a5b-b582-dd52ed5648a9": SheetCfg("grid_4x4"),
    "grok-image-037c2cd3-692e-43bd-ad98-e04b070cd14f": SheetCfg("horizontal", 7),
    "grok-image-09ee1410-c291-4d67-b13d-e95cdd5f8272": SheetCfg("horizontal", 8),
    "grok-image-399b423b-ba72-441a-a0d6-1c1b40c602f1": SheetCfg("horizontal", 9),
    "grok-image-3fea94f1-b1f7-4276-a182-d80a158984ae": SheetCfg("horizontal", 7),
    "grok-image-501d4a24-e85d-46d6-8aad-c1703d586e65": SheetCfg("horizontal", 5),
    "grok-image-6e2326ff-139b-41a1-b07a-5cde96cc74f7": SheetCfg("horizontal", 7),
    "grok-image-99a11592-7e6c-427a-bb67-e133fb3942cc": SheetCfg("grid_5x2"),
    "grok-image-ce796536-9a66-4f5d-8258-4ce96f976416": SheetCfg("horizontal", 6),
    "grok-image-d54cea3d-229d-42dd-bb81-6296f22d4fdb": SheetCfg("horizontal", 7),
}


def short_id(stem: str) -> str:
    return stem.replace("grok-image-", "")[:8]


def expand_box(x0: int, y0: int, x1: int, y1: int, w: int, h: int, pad: int = EXPAND) -> tuple[int, int, int, int]:
    return (
        max(0, x0 - pad),
        max(0, y0 - pad),
        min(w, x1 + pad),
        min(h, y1 + pad),
    )


def calibrated_grid(cols: int, rows: int, w: int, h: int) -> list[tuple[int, int, int, int]] | None:
    if w == 992 and h == 1040 and cols == 4 and rows == 4:
        margin, gx, gy, cw, rh = ICON_GRID_992
        boxes: list[tuple[int, int, int, int]] = []
        for r in range(rows):
            for c in range(cols):
                x0 = int(margin + c * (cw + gx))
                y0 = int(margin + r * (rh + gy))
                x1 = int(x0 + cw)
                y1 = int(y0 + rh)
                boxes.append(shrink_box(x0, y0, x1, y1, w, h, 6))
        return boxes
    return None


def _cluster_rows(pts: np.ndarray, rows: int, cols: int) -> list[np.ndarray]:
    """Split circle detections into rows by largest y-gaps (stable vs sort-and-chunk)."""
    order = np.argsort(pts[:, 1])
    ys = pts[order, 1].astype(float)
    if len(ys) <= rows:
        return [pts[i * cols : (i + 1) * cols] for i in range(rows)]

    gaps = np.diff(ys)
    cut_at = sorted(np.argsort(gaps)[-(rows - 1) :])
    groups: list[list[np.ndarray]] = []
    start = 0
    for cut in cut_at:
        idx = order[start : cut + 1]
        groups.append(list(pts[idx]))
        start = cut + 1
    groups.append(list(pts[order[start:]]))
    return [np.array(g) for g in groups[:rows]]


def _refine_center(gray: np.ndarray, cx: int, cy: int, r: int) -> tuple[int, int]:
    """Robust medal center — median inside Hough circle, ignores drop shadow."""
    h, w = gray.shape
    pad = max(28, int(r * 0.65))
    x0, y0 = max(0, cx - pad), max(0, cy - pad)
    x1, y1 = min(w, cx + pad), min(h, cy + pad)
    patch = gray[y0:y1, x0:x1]
    yy, xx = np.mgrid[0 : patch.shape[0], 0 : patch.shape[1]]
    gx = xx + x0
    gy = yy + y0
    dist = np.hypot(gx - cx, gy - cy)
    medal = (dist <= r * 0.98) & (patch > 35) & (patch < 235)
    if medal.sum() < 120:
        return cx, cy
    return int(np.median(gx[medal])), int(np.median(gy[medal]))


def _medal_bounds(gray: np.ndarray, cx: int, cy: int, r: int) -> tuple[int, int, int, int]:
    """Axis-aligned bounds of medal + drop shadow (for vertical padding)."""
    h, w = gray.shape
    x0, x1 = max(0, cx - int(r * 0.96)), min(w, cx + int(r * 0.96))
    y0, y1 = max(0, cy - int(r * 1.15)), min(h, cy + int(r * 1.15))
    top, bottom, left, right = y1, y0, x1, x0
    for y in range(y0, y1):
        row = gray[y, x0:x1]
        if np.any((row > 42) & (row < 242)):
            top = min(top, y)
            bottom = max(bottom, y)
    for x in range(x0, x1):
        col = gray[y0:y1, x]
        if np.any((col > 42) & (col < 242)):
            left = min(left, x)
            right = max(right, x)
    if bottom <= top:
        return cx - r, cy - r, cx + r, cy + r
    return left, top, right, bottom


def _square_box(cx: int, cy: int, half: int, w: int, h: int) -> tuple[int, int, int, int]:
    """Symmetric square crop; shrink half if near sheet edge."""
    bx, by = ICON_SHADOW_BIAS
    cx += bx
    cy += by
    half = min(half, cx, cy, w - cx, h - cy)
    return cx - half, cy - half, cx + half, cy + half


def detect_circle_grid_boxes(gray: np.ndarray, cols: int = 4, rows: int = 4) -> list[tuple[int, int, int, int]]:
    """Center crops on detected medal circles — snaps to median row/col grid."""
    h, w = gray.shape
    blur = cv2.GaussianBlur(gray, (9, 9), 2)
    circles = cv2.HoughCircles(
        blur,
        cv2.HOUGH_GRADIENT,
        dp=1.2,
        minDist=int(min(w, h) / (max(cols, rows) + 0.5)),
        param1=80,
        param2=32,
        minRadius=85,
        maxRadius=135,
    )
    if circles is None or len(circles[0]) < cols * rows:
        print("  warn: circle detect failed, using uniform grid fallback")
        return search_uniform_grid(gray, cols, rows)

    pts = np.round(circles[0]).astype(int)
    row_groups = _cluster_rows(pts, rows, cols)

    grid: list[list[tuple[int, int, int]]] = []
    for row in row_groups:
        ordered = row[row[:, 0].argsort()]
        grid.append([(int(x), int(y), int(r)) for x, y, r in ordered])

    col_x = [int(np.median([grid[r][c][0] for r in range(rows)])) for c in range(cols)]
    row_y = [int(np.median([grid[r][c][1] for c in range(cols)])) for r in range(rows)]

    col_sp = float(np.median(np.diff(col_x))) if cols > 1 else w
    row_sp = float(np.median(np.diff(row_y))) if rows > 1 else h
    half_cap = int(min(col_sp, row_sp) * 0.49)
    hr_median = int(np.median(pts[:, 2]))

    boxes: list[tuple[int, int, int, int]] = []
    for r in range(rows):
        row_half_cap = half_cap
        if r > 0:
            row_half_cap = min(row_half_cap, (row_y[r] - row_y[r - 1]) // 2 - 4)
        if r < rows - 1:
            row_half_cap = min(row_half_cap, (row_y[r + 1] - row_y[r]) // 2 - 4)
        # Edge rows get +2px half for top/bottom breathing room (sheet has extra margin).
        if r == 0 or r == rows - 1:
            row_half_cap = min(row_half_cap + 2, int(hr_median * 1.18) + 8)

        for c in range(cols):
            hcx, hcy, hr = grid[r][c]
            rcx, rcy = _refine_center(gray, hcx, hcy, hr)
            left, top, right, bottom = _medal_bounds(gray, rcx, rcy, hr)
            span = bottom - top
            # Snap to grid row/col; rim bounds only refine horizontal center.
            vcx = (left + right) // 2
            cx = int(round(vcx * 0.40 + col_x[c] * 0.60))
            cy = row_y[r] + (1 if r == 0 else 4 if r == rows - 1 else 3)
            half = row_half_cap
            boxes.append(_square_box(cx, cy, half, w, h))
    return boxes


def shrink_box(x0: int, y0: int, x1: int, y1: int, w: int, h: int, inset: int) -> tuple[int, int, int, int]:
    return (
        min(w, max(0, x0 + inset)),
        min(h, max(0, y0 + inset)),
        min(w, max(0, x1 - inset)),
        min(h, max(0, y1 - inset)),
    )


def search_uniform_grid(gray: np.ndarray, cols: int, rows: int) -> list[tuple[int, int, int, int]]:
    h, w = gray.shape
    fixed = calibrated_grid(cols, rows, w, h)
    if fixed is not None:
        return fixed

    best: tuple[int, int, int, float, float] | None = None
    best_score = -1.0

    for margin in range(12, 160, 4):
        for gx in range(4, 48, 4):
            for gy in range(4, 48, 4):
                cw = (w - 2 * margin - (cols - 1) * gx) / cols
                rh = (h - 2 * margin - (rows - 1) * gy) / rows
                if cw < 120 or rh < 120:
                    continue
                score = 0.0
                for r in range(rows):
                    for c in range(cols):
                        x0 = int(margin + c * (cw + gx))
                        y0 = int(margin + r * (rh + gy))
                        cell = gray[y0 : int(y0 + rh), x0 : int(x0 + cw)]
                        score += float(cell.std()) + float(cell.mean()) * 0.02
                if score > best_score:
                    best_score = score
                    best = (margin, gx, gy, cw, rh)

    if best is None:
        raise RuntimeError("grid search failed")

    margin, gx, gy, cw, rh = best
    # ponytail: single refinement pass around coarse winner
    bm, bgx, bgy, bcw, brh = margin, gx, gy, cw, rh
    for margin in range(max(12, bm - 6), bm + 7, 2):
        for gx in range(max(4, bgx - 4), bgx + 5, 2):
            for gy in range(max(4, bgy - 4), bgy + 5, 2):
                cw = (w - 2 * margin - (cols - 1) * gx) / cols
                rh = (h - 2 * margin - (rows - 1) * gy) / rows
                if cw < 120 or rh < 120:
                    continue
                score = 0.0
                for r in range(rows):
                    for c in range(cols):
                        x0 = int(margin + c * (cw + gx))
                        y0 = int(margin + r * (rh + gy))
                        cell = gray[y0 : int(y0 + rh), x0 : int(x0 + cw)]
                        score += float(cell.std()) + float(cell.mean()) * 0.02
                if score > best_score:
                    best_score = score
                    best = (margin, gx, gy, cw, rh)

    margin, gx, gy, cw, rh = best
    boxes: list[tuple[int, int, int, int]] = []
    for r in range(rows):
        for c in range(cols):
            x0 = int(margin + c * (cw + gx))
            y0 = int(margin + r * (rh + gy))
            boxes.append(shrink_box(int(x0), int(y0), int(x0 + cw), int(y0 + rh), w, h, 6))
    return boxes


def horizontal_boxes(gray: np.ndarray, count: int) -> list[tuple[int, int, int, int]]:
    h, w = gray.shape
    border = np.concatenate([gray[0, :], gray[-1, :], gray[:, 0], gray[:, -1]])
    bg = float(np.median(border))
    boxes: list[tuple[int, int, int, int]] = []

    for i in range(count):
        sx0 = int(i * w / count)
        sx1 = int((i + 1) * w / count)
        inset_l = 0.04 if i == 0 else 0.08
        inset_r = 0.04 if i == count - 1 else 0.12
        inner_w = sx1 - sx0
        x0 = sx0 + int(inner_w * inset_l)
        x1 = sx1 - int(inner_w * inset_r)

        band = gray[:, x0:x1]
        mask = band > bg + 8
        if not mask.any():
            boxes.append((x0, 0, x1, h))
            continue
        ys, xs = np.where(mask)
        pad = 14
        y0 = max(0, int(ys.min()) - pad)
        y1 = min(h, int(ys.max()) + pad + 1)
        bx0 = x0 + max(0, int(xs.min()) - pad)
        bx1 = x0 + min(x1 - x0, int(xs.max()) + pad + 1)
        boxes.append((bx0, y0, bx1, y1))
    return boxes


def _flood_remove_rgb(rgb: np.ndarray, edge_mask: np.ndarray, inner_mask: np.ndarray) -> np.ndarray:
    """Edge-connected flood on edge_mask; union with all inner_mask pixels."""
    h, w = rgb.shape[:2]
    remove = inner_mask.copy()
    q: deque[tuple[int, int]] = deque()

    def seed(x: int, y: int) -> None:
        if edge_mask[y, x] and not remove[y, x]:
            remove[y, x] = True
            q.append((x, y))

    for x in range(w):
        seed(x, 0)
        seed(x, h - 1)
    for y in range(h):
        seed(0, y)
        seed(w - 1, y)

    while q:
        x, y = q.popleft()
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < w and 0 <= ny < h and edge_mask[ny, nx] and not remove[ny, nx]:
                remove[ny, nx] = True
                q.append((nx, ny))
    return remove


def _border_bg_rgb(rgb: np.ndarray) -> np.ndarray:
    edge = np.concatenate([rgb[0], rgb[-1], rgb[:, 0], rgb[:, -1]], axis=0).astype(np.float32)
    return np.median(edge, axis=0)


def _keyed_remove(im: Image.Image, tol: int = BG_TOL) -> Image.Image:
    arr = np.array(im.convert("RGBA"))
    rgb = arr[..., :3].astype(np.float32)
    bg = _border_bg_rgb(rgb)
    keyed = np.abs(rgb - bg).max(axis=2) <= tol
    remove = _flood_remove_rgb(rgb.astype(np.uint8), keyed, keyed)
    out = arr.copy()
    out[remove] = (0, 0, 0, 0)
    return Image.fromarray(out)


def remove_dark_bg(im: Image.Image) -> Image.Image:
    cut = _keyed_remove(im, tol=BG_TOL)
    arr = np.array(cut)
    rgb = arr[..., :3]
    maxc = rgb.max(axis=2)
    remove = _flood_remove_rgb(rgb, maxc <= EDGE_T, maxc <= INNER_T)
    out = arr.copy()
    out[remove] = (0, 0, 0, 0)
    return Image.fromarray(out)


def remove_white_bg(im: Image.Image) -> Image.Image:
    cut = _keyed_remove(im, tol=30)
    arr = np.array(cut)
    rgb = arr[..., :3].astype(np.int16)
    gray = rgb.mean(axis=2)
    spread = np.maximum.reduce(
        [np.abs(rgb[..., 0] - rgb[..., 1]), np.abs(rgb[..., 1] - rgb[..., 2]), np.abs(rgb[..., 0] - rgb[..., 2])]
    )
    edge = (gray >= 200) & (spread <= 22)
    inner = rgb.min(axis=2) >= WHITE_INNER
    remove = _flood_remove_rgb(rgb.astype(np.uint8), edge, inner)
    out = arr.copy()
    out[remove] = (0, 0, 0, 0)
    return Image.fromarray(out)


def trim_alpha(im: Image.Image, pad: int = 6) -> Image.Image:
    im = im.convert("RGBA")
    arr = np.array(im)
    alpha = arr[:, :, 3]
    if not (alpha > 16).any():
        return im
    ys, xs = np.where(alpha > 16)
    y0 = max(0, int(ys.min()) - pad)
    y1 = min(arr.shape[0], int(ys.max()) + pad + 1)
    x0 = max(0, int(xs.min()) - pad)
    x1 = min(arr.shape[1], int(xs.max()) + pad + 1)
    return im.crop((x0, y0, x1, y1))


def fit_square(im: Image.Image, size: int = SQUARE_SIZE, fill: float = SQUARE_FILL) -> Image.Image:
    im = trim_alpha(im)
    w, h = im.size
    if w == 0 or h == 0:
        out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        return out
    target = size * fill
    scale = target / max(w, h)
    nw = max(1, int(round(w * scale)))
    nh = max(1, int(round(h * scale)))
    resized = im.resize((nw, nh), Image.Resampling.LANCZOS)
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    out.paste(resized, ((size - nw) // 2, (size - nh) // 2), resized)
    return out


def save_pair(crop: Image.Image, dest_stem: Path, *, dark_bg: bool) -> Image.Image:
    bg = crop.convert("RGBA")
    bg.save(dest_stem.with_name(dest_stem.name + "_bg.png"))

    cut = remove_dark_bg(bg.copy()) if dark_bg else remove_white_bg(bg.copy())
    # Icons: skip trim — asymmetric shadow was pulling medals off-center.
    if dark_bg:
        cut = trim_alpha(cut)
    cut.save(dest_stem.with_suffix(".png"))
    return cut


def process_sheet(path: Path, cfg: SheetCfg, manifest: list[dict]) -> None:
    gray = cv2.imread(str(path), cv2.IMREAD_GRAYSCALE)
    if gray is None:
        raise FileNotFoundError(path)
    h, w = gray.shape
    im = Image.open(path).convert("RGB")
    sid = short_id(path.stem)

    if cfg.mode == "grid_4x4":
        boxes = detect_circle_grid_boxes(gray, 4, 4)
        out_dir = OUT_ICONS
        dark = False
        use_expand = False
    elif cfg.mode == "grid_5x2":
        boxes = search_uniform_grid(gray, 5, 2)
        out_dir = OUT_UNITS
        dark = True
        use_expand = True
    else:
        boxes = horizontal_boxes(gray, cfg.count)
        out_dir = OUT_UNITS
        dark = True
        use_expand = True

    for i, (x0, y0, x1, y1) in enumerate(boxes, start=1):
        ex = expand_box(x0, y0, x1, y1, w, h) if use_expand else (x0, y0, x1, y1)
        crop = im.crop(ex)
        name = f"{sid}_{i:02d}"
        dest = out_dir / name
        cut = save_pair(crop, dest, dark_bg=dark)

        entry = {
            "id": name,
            "source": path.name,
            "bounds": [int(v) for v in ex],
            "size": [int(ex[2] - ex[0]), int(ex[3] - ex[1])],
            "mode": cfg.mode,
        }

        if dark:
            sq = fit_square(cut)
            sq_path = OUT_UNITS_SQ / f"{name}.png"
            sq.save(sq_path)
            entry["square"] = str(sq_path.relative_to(OUT)).replace("\\", "/")

        manifest.append(entry)
        print(f"  {name} {entry['size']}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--icons-only", action="store_true", help="Re-slice 4x4 icon sheets only")
    args = parser.parse_args()

    for d in (OUT, OUT_ICONS, OUT_UNITS, OUT_UNITS_SQ):
        d.mkdir(parents=True, exist_ok=True)

    if args.icons_only:
        for f in OUT_ICONS.glob("*.png"):
            f.unlink()

    manifest_path = OUT / "SliceManifest.json"
    existing: list[dict] = []
    if args.icons_only and manifest_path.exists():
        existing = [e for e in json.loads(manifest_path.read_text(encoding="utf-8")).get("outputs", []) if e.get("mode") != "grid_4x4"]

    manifest: list[dict] = list(existing)
    sheets = sorted(SRC.glob("grok-image-*.jpg"))
    if len(sheets) != len(SHEETS):
        missing = set(SHEETS) - {p.stem for p in sheets}
        extra = {p.stem for p in sheets} - set(SHEETS)
        if missing or extra:
            print("warn: sheet manifest mismatch", "missing", missing, "extra", extra)

    for path in sheets:
        cfg = SHEETS.get(path.stem)
        if cfg is None:
            print("skip unlisted", path.name)
            continue
        if args.icons_only and cfg.mode != "grid_4x4":
            continue
        print(path.name, cfg.mode, cfg.count or "")
        process_sheet(path, cfg, manifest)

    manifest_path.write_text(json.dumps({"sheets": len(SHEETS), "outputs": manifest}, indent=2), encoding="utf-8")

    icon_n = len(list(OUT_ICONS.glob("*.png"))) // 2
    unit_n = len(list(OUT_UNITS.glob("*.png"))) // 2
    sq_n = len(list(OUT_UNITS_SQ.glob("*.png")))
    print(f"\nDone -> {OUT}")
    print(f"  icons: {icon_n} (+ _bg variants)")
    print(f"  units: {unit_n} (+ _bg variants)")
    print(f"  units_sq: {sq_n}")


if __name__ == "__main__":
    main()

