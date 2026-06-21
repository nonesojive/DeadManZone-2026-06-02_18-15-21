# ponytail: one-shot batch processor for mixed-grid Grok button sheets.
from __future__ import annotations

import glob
import os
import shutil
from collections import deque

import numpy as np
from PIL import Image

OUT = os.path.join(
    os.path.dirname(__file__),
)
SRC_CURSOR = os.path.join(
    os.environ.get("APPDATA", ""),
    "Cursor",
    "User",
    "workspaceStorage",
    "empty-window",
    "images",
)
EXPAND = 12
EDGE_T = 225
INNER_T = 250
SPRITE_MIN_AREA = 1200
SPRITE_PAD = 12

SHEETS = [
    {"id": "97f6efc3", "name": "sheet01_sprites.png", "mode": "sprite"},
    {"id": "af5e360b", "name": "sheet02_sprites.png", "mode": "sprite"},
    {
        "id": "36f59822",
        "name": "sheet03_buttons_2x3.png",
        "mode": "grid",
        "size": (1152, 896),
        "row_gutters": [(0, 76), (304, 338), (566, 599), (827, 895)],
        "col_gutters": [(0, 71), (557, 594), (1079, 1151)],
    },
    {
        "id": "2c1b93bd",
        "name": "sheet04_buttons_2x3.png",
        "mode": "grid",
        "size": (1152, 896),
        "row_gutters": [(0, 83), (299, 345), (560, 606), (822, 895)],
        "col_gutters": [(0, 71), (556, 594), (1078, 1151)],
    },
    {
        "id": "5bafae70",
        "name": "sheet05_tiles_3x3.png",
        "mode": "grid",
        "size": (1024, 1024),
        "row_gutters": [(0, 26), (335, 358), (666, 684), (1001, 1023)],
        "col_gutters": [(0, 28), (335, 356), (667, 687), (1002, 1023)],
    },
    {
        "id": "4cbf52f6",
        "name": "sheet06_tiles_3x3.png",
        "mode": "grid",
        "size": (1024, 1024),
        "row_gutters": [(0, 39), (340, 355), (669, 684), (993, 1023)],
        "col_gutters": [(0, 33), (338, 355), (668, 685), (999, 1023)],
    },
    {
        "id": "45cee05e",
        "name": "sheet07_tiles_3x3.png",
        "mode": "grid",
        "size": (1024, 1024),
        "row_gutters": [(0, 35), (334, 363), (661, 690), (989, 1023)],
        "col_gutters": [(0, 36), (333, 363), (660, 690), (988, 1023)],
    },
    {
        "id": "613c35d7",
        "name": "sheet08_tiles_3x3.png",
        "mode": "grid",
        "size": (1024, 1024),
        "row_gutters": [(0, 34), (340, 360), (667, 686), (994, 1023)],
        "col_gutters": [(0, 31), (339, 357), (666, 684), (999, 1023)],
    },
]


def find_source(grok_id: str) -> str:
    matches = glob.glob(os.path.join(SRC_CURSOR, f"grok-image-{grok_id}*.png"))
    if not matches:
        raise FileNotFoundError(f"Missing grok-image-{grok_id}")
    return matches[0]


def is_white_pixel(rgb: np.ndarray, thresh: int = 230) -> bool:
    return int(np.min(rgb)) >= thresh


def expand_rect(x0: int, y0: int, x1: int, y1: int, w: int, h: int) -> tuple[int, int, int, int]:
    return (
        max(0, x0 - EXPAND),
        max(0, y0 - EXPAND),
        min(w - 1, x1 + EXPAND),
        min(h - 1, y1 + EXPAND),
    )


def expand_into_white(
    rgb: np.ndarray,
    x0: int,
    y0: int,
    x1: int,
    y1: int,
    max_pad: int = EXPAND,
) -> tuple[int, int, int, int]:
    h, w = rgb.shape[:2]
    left, top, right, bottom = x0, y0, x1, y1
    for _ in range(max_pad):
        changed = False
        if left > 0 and np.all([is_white_pixel(rgb[y, left - 1]) for y in range(top, bottom + 1)]):
            left -= 1
            changed = True
        if right < w - 1 and np.all([is_white_pixel(rgb[y, right + 1]) for y in range(top, bottom + 1)]):
            right += 1
            changed = True
        if top > 0 and np.all([is_white_pixel(rgb[top - 1, x]) for x in range(left, right + 1)]):
            top -= 1
            changed = True
        if bottom < h - 1 and np.all([is_white_pixel(rgb[bottom + 1, x]) for x in range(left, right + 1)]):
            bottom += 1
            changed = True
        if not changed:
            break
    return left, top, right, bottom


def gutters_to_rects(row_gutters, col_gutters, w: int, h: int) -> list[tuple[int, int, int, int]]:
    rects = []
    for ri in range(len(row_gutters) - 1):
        y0 = row_gutters[ri][1] + 1
        y1 = row_gutters[ri + 1][0] - 1
        for ci in range(len(col_gutters) - 1):
            x0 = col_gutters[ci][1] + 1
            x1 = col_gutters[ci + 1][0] - 1
            rects.append(expand_rect(x0, y0, x1, y1, w, h))
    return rects


def label_components(mask: np.ndarray) -> tuple[np.ndarray, int]:
    h, w = mask.shape
    labels = np.zeros((h, w), np.int32)
    current = 0
    for y in range(h):
        for x in range(w):
            if not mask[y, x] or labels[y, x]:
                continue
            current += 1
            stack = [(y, x)]
            while stack:
                cy, cx = stack.pop()
                if cy < 0 or cx < 0 or cy >= h or cx >= w:
                    continue
                if not mask[cy, cx] or labels[cy, cx]:
                    continue
                labels[cy, cx] = current
                stack.extend([(cy + 1, cx), (cy - 1, cx), (cy, cx + 1), (cy, cx - 1)])
    return labels, current


def sprite_rects(path: str) -> list[tuple[int, int, int, int]]:
    rgb = np.array(Image.open(path).convert("RGB"))
    h, w = rgb.shape[:2]
    mask = np.min(rgb, axis=2) < 235
    labels, count = label_components(mask)
    boxes = []
    for i in range(1, count + 1):
        ys, xs = np.where(labels == i)
        if len(xs) < SPRITE_MIN_AREA:
            continue
        x0, x1, y0, y1 = int(xs.min()), int(xs.max()), int(ys.min()), int(ys.max())
        boxes.append(expand_into_white(rgb, x0, y0, x1, y1))

    def sort_key(rect: tuple[int, int, int, int]) -> tuple[int, float]:
        cy = (rect[1] + rect[3]) * 0.5
        cx = (rect[0] + rect[2]) * 0.5
        return (int(cy // 120), cx)

    return sorted(boxes, key=sort_key)


def remove_white_bg(im: Image.Image) -> Image.Image:
    im = im.convert("RGBA")
    w, h = im.size
    px = im.load()
    remove = [[False] * w for _ in range(h)]

    for y in range(h):
        for x in range(w):
            r, g, b, _a = px[x, y]
            if min(r, g, b) >= INNER_T:
                remove[y][x] = True

    q: deque[tuple[int, int]] = deque()
    seen = [[False] * w for _ in range(h)]

    def push(x: int, y: int) -> None:
        if x < 0 or y < 0 or x >= w or y >= h or seen[y][x]:
            return
        r, g, b, _a = px[x, y]
        if min(r, g, b) < EDGE_T:
            return
        seen[y][x] = True
        q.append((x, y))

    for x in range(w):
        push(x, 0)
        push(x, h - 1)
    for y in range(h):
        push(0, y)
        push(w - 1, y)

    while q:
        x, y = q.popleft()
        remove[y][x] = True
        push(x - 1, y)
        push(x + 1, y)
        push(x, y - 1)
        push(x, y + 1)

    for y in range(h):
        for x in range(w):
            if remove[y][x]:
                px[x, y] = (0, 0, 0, 0)
    return im


def crop_tile(sheet: Image.Image, rect: tuple[int, int, int, int]) -> Image.Image:
    x0, y0, x1, y1 = rect
    return sheet.crop((x0, y0, x1 + 1, y1 + 1))


def main() -> None:
    src_dir = os.path.join(OUT, "Source")
    os.makedirs(src_dir, exist_ok=True)

    tile_num = 1
    summary = []

    for sheet in SHEETS:
        src_path = find_source(sheet["id"])
        dst_src = os.path.join(src_dir, sheet["name"])
        shutil.copy2(src_path, dst_src)
        image = Image.open(src_path).convert("RGB")
        w, h = image.size

        if sheet["mode"] == "sprite":
            rects = sprite_rects(src_path)
        else:
            rects = gutters_to_rects(sheet["row_gutters"], sheet["col_gutters"], w, h)

        start = tile_num
        for rect in rects:
            tile = crop_tile(image, rect)
            tile.save(os.path.join(OUT, f"tile{tile_num}_bg.png"))
            remove_white_bg(tile.copy()).save(os.path.join(OUT, f"tile{tile_num}.png"))
            tile_num += 1

        summary.append(f"{sheet['name']}: {len(rects)} tiles ({start}-{tile_num - 1})")
        print(f"{sheet['name']}: {len(rects)} tiles -> tile{start}-tile{tile_num - 1}")

    transparent = len([f for f in os.listdir(OUT) if f.startswith("tile") and f.endswith(".png") and "_bg" not in f])
    assert transparent == tile_num - 1
    print(f"TOTAL: {transparent} transparent + {transparent} bg = {transparent * 2} files")
    for line in summary:
        print(" ", line)


if __name__ == "__main__":
    main()
