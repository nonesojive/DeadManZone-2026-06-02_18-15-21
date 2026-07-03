"""Key out white bg from fresh building renders, trim, install as combat sprites,
and wire PieceDefinitionSO.combatArenaSprite for the 6 buildings."""
import os
import re
import uuid
from collections import deque

from PIL import Image

ROOT = r"C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone"
STAGING = os.path.join(ROOT, "ArtStaging", "combatvisualv2")
DST = os.path.join(ROOT, "Assets", "_Project", "Art", "Combat2D", "Buildings", "V2")
PIECES = os.path.join(ROOT, "Assets", "_Project", "Data", "Resources", "DeadManZone", "Pieces")
UNIT_META = os.path.join(
    ROOT, "Assets", "_Project", "Art", "Combat2D", "Units", "Animations",
    "bulwark_squad", "bulwark_squad_idle.png.meta")
WHITE_MIN = 235

BUILDINGS = [
    "supply_depot", "field_hospital", "officer_quarters",
    "command_outpost", "surgical_center", "recruitment_office",
]


def key_out_white(im):
    im = im.convert("RGBA")
    px = im.load()
    w, h = im.size
    seen = bytearray(w * h)
    queue = deque()
    for x in range(w):
        queue.append((x, 0))
        queue.append((x, h - 1))
    for y in range(h):
        queue.append((0, y))
        queue.append((w - 1, y))
    while queue:
        x, y = queue.popleft()
        if x < 0 or y < 0 or x >= w or y >= h:
            continue
        idx = y * w + x
        if seen[idx]:
            continue
        seen[idx] = 1
        r, g, b, a = px[x, y]
        if r >= WHITE_MIN and g >= WHITE_MIN and b >= WHITE_MIN:
            px[x, y] = (r, g, b, 0)
            queue.extend(((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))
    return im


template = open(UNIT_META).read()
os.makedirs(DST, exist_ok=True)
folder_meta = DST + ".meta"
if not os.path.exists(folder_meta):
    open(folder_meta, "w", newline="\n").write(
        "fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n"
        "  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        % uuid.uuid4().hex)

for piece in BUILDINGS:
    src = os.path.join(STAGING, f"{piece}_base.png")
    im = key_out_white(Image.open(src))
    im = im.crop(im.getbbox())
    out = os.path.join(DST, f"combat2d_building_v2_{piece}.png")
    im.save(out)

    meta_path = out + ".meta"
    if os.path.exists(meta_path):
        guid = re.search(r"guid: (\w+)", open(meta_path).read()).group(1)
    else:
        guid = uuid.uuid4().hex
        meta = re.sub(r"guid: \w+", "guid: " + guid, template, count=1)
        open(meta_path, "w", newline="\n").write(meta)

    piece_path = os.path.join(PIECES, f"{piece}.asset")
    text = open(piece_path).read()
    new_ref = f"combatArenaSprite: {{fileID: 21300000, guid: {guid}, type: 3}}"
    text2 = re.sub(r"combatArenaSprite: \{[^}]*\}", new_ref, text, count=1)
    if text2 != text:
        open(piece_path, "w", newline="\n").write(text2)
    print(f"{piece}: combat sprite installed ({guid}) size={im.size}")

print("done")
