"""Throwaway: black-shape test at real rendered scale (Phase 0 Task 7).

Silhouettes are extracted as a pixel diff between battle_distance.png and an
identical-camera plate with the units hidden (battle_distance_bgplate2.png) --
more robust than corner-color thresholding olive units on brown ground
(conceptually the refcheck.py gate, section 7.3, applied to a live render).
"""
from PIL import Image, ImageFilter
import numpy as np

battle = np.asarray(Image.open("battle_distance.png").convert("RGB")).astype(int)
plate = np.asarray(Image.open("battle_distance_bgplate2.png").convert("RGB")).astype(int)
diff = np.abs(battle - plate).sum(axis=2)
mask = diff > 35  # sum-of-channels threshold; soft shadows mostly stay under it

# Projected screen-x centers of the five lineup units (computed from the
# capture camera at (0,11,-8.4) looking at origin, FOV 60, 1280x720).
names = ["enlisted_baseline", "cel_stocky", "cel_real", "neutral_stocky", "neutral_real"]
centers = [397, 518, 640, 762, 883]
Y0, Y1, HALF_W = 235, 372, 58  # head clearance to ring top

ZOOM = 2
tile_w, tile_h = HALF_W * 2, Y1 - Y0
pad = 8
canvas = Image.new("RGB", ((tile_w * ZOOM + pad) * 5 + pad, tile_h * ZOOM + 2 * pad), "white")
for i, cx in enumerate(centers):
    tile = mask[Y0:Y1, cx - HALF_W:cx + HALF_W]
    fill = tile.sum()
    print(f"{names[i]}: {fill} silhouette px")
    assert fill > 300, f"{names[i]} silhouette nearly empty - crop/threshold wrong"
    img = Image.fromarray(np.where(tile, 0, 255).astype(np.uint8), "L")
    # Morphological close (black is min): fills interior speckle where unit
    # color happens to match the ground plate.
    img = img.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3))
    img = img.resize((tile_w * ZOOM, tile_h * ZOOM), Image.NEAREST)
    canvas.paste(img.convert("RGB"), (pad + i * (tile_w * ZOOM + pad), pad))
canvas.save("blackshape_sim.png")
print("wrote blackshape_sim.png")
