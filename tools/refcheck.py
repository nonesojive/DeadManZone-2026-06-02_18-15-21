"""
DeadManZone - pre-Meshy black-shape gate (unit-art spec section 7.3).

Thresholds a reference image to a flat black silhouette, renders it at combat
size (~40 px tall) and at 4x zoom, and writes a side-by-side judgment strip.
A human/agent judges: does archetype + piece cue still read? If not, re-prompt
the ref BEFORE spending a Meshy chain.

Usage:
  python refcheck.py <ref.png> [--out <dir>] [--alpha-bg]

--alpha-bg: treat transparent pixels as background (for refs with alpha).
Otherwise background = pixels close to the dominant corner color.
"""
import argparse
import os
import sys

from PIL import Image

COMBAT_HEIGHT = 40   # px, spec section 7.3
ZOOM = 4
BG_TOLERANCE = 28    # per-channel distance from corner color counted as background


def silhouette(img: Image.Image, alpha_bg: bool) -> Image.Image:
    rgba = img.convert("RGBA")
    px = rgba.load()
    w, h = rgba.size
    if alpha_bg:
        def is_bg(p):
            return p[3] < 16
    else:
        # ponytail: dominant-corner background detection; ceiling is refs with
        # busy backgrounds — the spec template mandates solid backgrounds anyway.
        corner = px[0, 0]

        def is_bg(p):
            return all(abs(p[i] - corner[i]) <= BG_TOLERANCE for i in range(3))
    out = Image.new("RGB", (w, h), "white")
    opx = out.load()
    filled = 0
    for y in range(h):
        for x in range(w):
            if not is_bg(px[x, y]):
                opx[x, y] = (0, 0, 0)
                filled += 1
    if filled < (w * h) // 200:  # <0.5% figure pixels = detection failed
        sys.exit("silhouette detection found almost nothing - wrong --alpha-bg? busy background?")
    return out


def strip(sil: Image.Image) -> Image.Image:
    w, h = sil.size
    combat = sil.resize((max(1, w * COMBAT_HEIGHT // h), COMBAT_HEIGHT), Image.LANCZOS)
    zoomed = combat.resize((combat.width * ZOOM, combat.height * ZOOM), Image.NEAREST)
    pad = 10
    total_w = combat.width + zoomed.width + 3 * pad
    total_h = max(combat.height, zoomed.height) + 2 * pad
    canvas = Image.new("RGB", (total_w, total_h), "white")
    canvas.paste(combat, (pad, (total_h - combat.height) // 2))
    canvas.paste(zoomed, (combat.width + 2 * pad, (total_h - zoomed.height) // 2))
    return canvas


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("ref")
    ap.add_argument("--out", default=None)
    ap.add_argument("--alpha-bg", action="store_true")
    args = ap.parse_args()
    img = Image.open(args.ref)
    result = strip(silhouette(img, args.alpha_bg))
    out_dir = args.out or os.path.dirname(args.ref) or "."
    base = os.path.splitext(os.path.basename(args.ref))[0]
    dest = os.path.join(out_dir, f"{base}_shape.png")
    result.save(dest)
    print(dest)


if __name__ == "__main__":
    main()
