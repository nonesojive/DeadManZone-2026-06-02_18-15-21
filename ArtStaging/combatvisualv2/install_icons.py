"""Install the 17 fresh shop icons into Assets and wire PieceDefinitionSO.icon.
Also writes trimmed transparent building renders as combatArenaSprite sources."""
import os
import re
import shutil
import uuid

ROOT = r"C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone"
STAGING = os.path.join(ROOT, "ArtStaging", "combatvisualv2")
ICONS_SRC = os.path.join(STAGING, "icons")
ICONS_DST = os.path.join(ROOT, "Assets", "_Project", "Art", "Combat2D", "Icons", "ShopV2")
PIECES = os.path.join(ROOT, "Assets", "_Project", "Data", "Resources", "DeadManZone", "Pieces")
META_TEMPLATE_PATH = os.path.join(
    ROOT, "Assets", "_Project", "Art", "IronMarch", "Icons", "enlisted_rifleman_icon.png.meta")

ALL_PIECES = [
    "supply_depot", "field_hospital", "officer_quarters", "command_outpost",
    "surgical_center", "recruitment_office", "field_medic", "conscript_rifleman",
    "armored_transport", "ironmarch_surgeon", "bulwark_squad", "enlisted_rifleman",
    "ironmarch_iron_horse", "ironclad_mortars", "ironclad_marksman",
    "ironclad_field_marshal", "machine_gun_nest",
]

template = open(META_TEMPLATE_PATH).read()
os.makedirs(ICONS_DST, exist_ok=True)

folder_meta = os.path.join(ICONS_DST + ".meta")
if not os.path.exists(folder_meta):
    open(folder_meta, "w", newline="\n").write(
        "fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n"
        "  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        % uuid.uuid4().hex)

for piece in ALL_PIECES:
    src = os.path.join(ICONS_SRC, f"shop_icon_{piece}.png")
    dst = os.path.join(ICONS_DST, f"shop_icon_{piece}.png")
    shutil.copyfile(src, dst)

    meta_path = dst + ".meta"
    if os.path.exists(meta_path):
        guid = re.search(r"guid: (\w+)", open(meta_path).read()).group(1)
    else:
        guid = uuid.uuid4().hex
        meta = re.sub(r"guid: \w+", "guid: " + guid, template, count=1)
        meta = re.sub(r"spriteID: \w+", "spriteID: " + uuid.uuid4().hex[:32], meta, count=1)
        open(meta_path, "w", newline="\n").write(meta)

    piece_path = os.path.join(PIECES, f"{piece}.asset")
    if not os.path.exists(piece_path):
        print(f"MISSING PIECE ASSET: {piece}")
        continue
    text = open(piece_path).read()
    new_icon = f"icon: {{fileID: 21300000, guid: {guid}, type: 3}}"
    text2 = re.sub(r"icon: \{[^}]*\}", new_icon, text, count=1)
    if text2 != text:
        open(piece_path, "w", newline="\n").write(text2)
        print(f"{piece}: icon wired ({guid})")
    else:
        print(f"{piece}: icon unchanged")

print("done")
