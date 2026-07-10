"""
DeadManZone - pack rendered frames into a square sprite sheet.

  python pack_sheet.py --frames frames/idle --out conscript_rifleman_idle.png \
      [--columns 7]

Frames are placed row-major (frame_00 top-left), matching the existing
CombatUnit2DAnimationSetSO sheet layout (49 frames, 7 columns, 512px cells).
"""

import argparse
import glob
import math
import os

from PIL import Image


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--frames", required=True)
    p.add_argument("--out", required=True)
    p.add_argument("--columns", type=int, default=7)
    args = p.parse_args()

    paths = sorted(glob.glob(os.path.join(args.frames, "frame_*.png")))
    if not paths:
        raise SystemExit(f"No frames found in {args.frames}")

    first = Image.open(paths[0])
    cell = first.size[0]
    cols = args.columns
    rows = math.ceil(len(paths) / cols)

    sheet = Image.new("RGBA", (cols * cell, rows * cell), (0, 0, 0, 0))
    for i, path in enumerate(paths):
        img = Image.open(path).convert("RGBA")
        sheet.paste(img, ((i % cols) * cell, (i // cols) * cell))

    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    sheet.save(args.out)
    print(f"Packed {len(paths)} frames ({cols}x{rows}, cell {cell}px) -> {args.out}")


if __name__ == "__main__":
    main()
