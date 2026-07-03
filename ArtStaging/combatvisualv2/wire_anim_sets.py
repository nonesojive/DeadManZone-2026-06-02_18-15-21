"""One-shot wiring: build CombatUnit2DAnimationSetSO .asset YAML for the 11 fresh
combatvisualv2 unit sheets and bind them to their PieceDefinitionSO assets.
ponytail: duplicates Combat2DAnimationSetBuilder because Unity editor is unreachable;
rerun the editor menu later to re-verify via the official path."""
import os
import re
import uuid

ROOT = r"C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone"
ANIM = os.path.join(ROOT, "Assets", "_Project", "Art", "Combat2D", "Units", "Animations")
PIECES = os.path.join(ROOT, "Assets", "_Project", "Data", "Resources", "DeadManZone", "Pieces")
SCRIPT_GUID = "c4c0186fbc4806b4da4c818aa5dd1ea4"

# state -> (fps, loop) for 49-frame sheets
INFANTRY = {"idle": (12, 1), "walk": (14, 1), "shoot": (24, 0), "die": (12, 0)}
VEHICLE = {"idle": (12, 1), "walk": (12, 1), "shoot": (24, 0), "die": (12, 0)}
STRUCTURE = {"idle": (12, 1), "shoot": (24, 0), "die": (12, 0)}

UNITS = {
    "field_medic": INFANTRY,
    "conscript_rifleman": INFANTRY,
    "armored_transport": VEHICLE,
    "ironmarch_surgeon": INFANTRY,
    "bulwark_squad": INFANTRY,
    "enlisted_rifleman": INFANTRY,
    "ironmarch_iron_horse": VEHICLE,
    "ironclad_mortars": INFANTRY,
    "ironclad_marksman": INFANTRY,
    "ironclad_field_marshal": INFANTRY,
    "machine_gun_nest": STRUCTURE,
}

EMPTY_STRIP = """    sheet: {fileID: 0}
    frameCount: 0
    columns: 0
    framesPerSecond: 1
    loop: 0"""

META_TEMPLATE = """fileFormatVersion: 2
guid: %s
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def sheet_guid(piece, state):
    meta = os.path.join(ANIM, piece, f"{piece}_{state}.png.meta")
    if not os.path.exists(meta):
        return None
    m = re.search(r"guid: (\w+)", open(meta).read())
    return m.group(1) if m else None


def strip_yaml(guid, fps, loop):
    return f"""    sheet: {{fileID: 21300000, guid: {guid}, type: 3}}
    frameCount: 49
    columns: 7
    framesPerSecond: {fps}
    loop: {loop}"""


def build_asset(piece, states):
    sections = {}
    for state in ("idle", "walk", "run", "hurt", "hitReact", "shoot", "die"):
        if state in states:
            guid = sheet_guid(piece, state)
            if guid is None:
                print(f"  MISSING SHEET: {piece} {state}")
                sections[state] = EMPTY_STRIP
            else:
                fps, loop = states[state]
                sections[state] = strip_yaml(guid, fps, loop)
        else:
            sections[state] = EMPTY_STRIP

    name = f"{piece}_anim_set"
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier: DeadManZone.Data::DeadManZone.Data.CombatUnit2DAnimationSetSO
  idle:
{sections['idle']}
  walk:
{sections['walk']}
  run:
{sections['run']}
  hurt:
{sections['hurt']}
  hitReact:
{sections['hitReact']}
  shoot:
{sections['shoot']}
  die:
{sections['die']}
"""


def asset_guid(path_meta):
    if os.path.exists(path_meta):
        return re.search(r"guid: (\w+)", open(path_meta).read()).group(1)
    guid = uuid.uuid4().hex
    open(path_meta, "w", newline="\n").write(META_TEMPLATE % guid)
    return guid


def wire_piece(piece, set_guid):
    piece_path = os.path.join(PIECES, f"{piece}.asset")
    if not os.path.exists(piece_path):
        print(f"  PIECE NOT FOUND: {piece}")
        return
    text = open(piece_path).read()
    new_ref = f"combatArena2DAnimations: {{fileID: 11400000, guid: {set_guid}, type: 2}}"
    text2 = re.sub(r"combatArena2DAnimations: \{[^}]*\}", new_ref, text)
    if text2 != text:
        open(piece_path, "w", newline="\n").write(text2)
        print(f"  wired {piece} -> {set_guid}")
    else:
        print(f"  {piece}: reference unchanged")


for piece, states in UNITS.items():
    folder = os.path.join(ANIM, piece)
    asset_path = os.path.join(folder, f"{piece}_anim_set.asset")
    yaml_text = build_asset(piece, states)
    open(asset_path, "w", newline="\n").write(yaml_text)
    guid = asset_guid(asset_path + ".meta")
    print(f"{piece}: anim set written ({guid})")
    wire_piece(piece, guid)

print("done")
