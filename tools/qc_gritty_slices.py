#!/usr/bin/env python3
"""Quick QC: flag slices with vertical/horizontal center seams (split cells)."""

from __future__ import annotations

import sys
from pathlib import Path

import cv2
import numpy as np

KIT = Path(__file__).resolve().parents[1] / "Assets/_Project/Art/UI/GrittyPostApocalyptic"


def split_score(path: Path) -> dict | None:
    g = cv2.imread(str(path), cv2.IMREAD_GRAYSCALE)
    if g is None:
        return None
    h, w = g.shape
    mid = w // 2
    col = g[:, max(0, mid - 2) : min(w, mid + 3)]
    center_dark = float(np.mean(col < 30))
    left_fg = float(np.mean(g[:, : w // 4] > 40))
    right_fg = float(np.mean(g[:, 3 * w // 4 :] > 40))

    midy = h // 2
    row = g[max(0, midy - 2) : min(h, midy + 3), :]
    center_dark_y = float(np.mean(row < 30))
    top_fg = float(np.mean(g[: h // 4, :] > 40))
    bot_fg = float(np.mean(g[3 * h // 4 :, :] > 40))

    v_split = center_dark > 0.75 and left_fg > 0.15 and right_fg > 0.15
    h_split = center_dark_y > 0.75 and top_fg > 0.15 and bot_fg > 0.15
    return {
        "v": v_split,
        "h": h_split,
        "cd": center_dark,
        "cdy": center_dark_y,
        "size": (w, h),
    }


def main() -> int:
    bad: list[tuple[str, str, dict]] = []
    ok = 0
    for folder in ("Components", "Icons"):
        d = KIT / folder
        if not d.exists():
            continue
        for p in sorted(d.glob("*.png")):
            if p.name.startswith("_"):
                continue
            s = split_score(p)
            if s is None:
                continue
            if s["v"] or s["h"]:
                bad.append((folder, p.name, s))
            else:
                ok += 1

    print(f"OK: {ok}  Suspect splits: {len(bad)}")
    for folder, name, s in bad:
        flags = ("V" if s["v"] else "") + ("H" if s["h"] else "")
        w, h = s["size"]
        print(f"  [{flags}] {folder}/{name} {w}x{h} cd={s['cd']:.2f} cdy={s['cdy']:.2f}")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
