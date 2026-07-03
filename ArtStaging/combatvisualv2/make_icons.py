"""Build the 17 consistent 256x256 shop icons from fresh AutoSprite base renders.
Pipeline per icon: flood-fill white bg -> trim -> fit onto shared grimdark card.
ponytail: border flood-fill keying assumes clean white studio bg from the generator;
if a render ships with off-white noise, regenerate the render rather than patch here."""
import os
from collections import deque

from PIL import Image, ImageDraw, ImageFilter

STAGING = r"C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone\ArtStaging\combatvisualv2"
OUT = os.path.join(STAGING, "icons")
ICON = 256
PAD = 18
WHITE_MIN = 235

PIECES = [
    "supply_depot", "field_hospital", "officer_quarters", "command_outpost",
    "surgical_center", "recruitment_office", "field_medic", "conscript_rifleman",
    "armored_transport", "ironmarch_surgeon", "bulwark_squad", "enlisted_rifleman",
    "iron_horse", "ironclad_mortars", "ironclad_marksman", "field_marshal",
    "machine_gun_nest",
]

# staging filename stem -> canonical piece id
RENAME = {"iron_horse": "ironmarch_iron_horse", "field_marshal": "ironclad_field_marshal"}


def key_out_white(im):
    """Make border-connected near-white pixels transparent (flood fill)."""
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


def make_card():
    """Shared grimdark background card: dark mud gradient + vignette + brass frame."""
    card = Image.new("RGBA", (ICON, ICON))
    draw = ImageDraw.Draw(card)
    top, bottom = (58, 52, 44), (28, 26, 22)
    for y in range(ICON):
        t = y / (ICON - 1)
        col = tuple(int(a + (b - a) * t) for a, b in zip(top, bottom))
        draw.line([(0, y), (ICON, y)], fill=col + (255,))
    vignette = Image.new("L", (ICON, ICON), 0)
    vd = ImageDraw.Draw(vignette)
    vd.ellipse([-40, -40, ICON + 40, ICON + 40], fill=90)
    vignette = vignette.filter(ImageFilter.GaussianBlur(40))
    card = Image.composite(Image.new("RGBA", card.size, (14, 13, 11, 255)), card, vignette.point(lambda v: 90 - v))
    fd = ImageDraw.Draw(card)
    fd.rectangle([1, 1, ICON - 2, ICON - 2], outline=(110, 92, 50, 255), width=2)
    fd.rectangle([4, 4, ICON - 5, ICON - 5], outline=(58, 48, 30, 255), width=1)
    return card


def build_icon(stem, card):
    src = os.path.join(STAGING, f"{stem}_base.png")
    im = key_out_white(Image.open(src))
    bbox = im.getbbox()
    im = im.crop(bbox)
    inner = ICON - PAD * 2
    scale = min(inner / im.width, inner / im.height)
    im = im.resize((max(1, int(im.width * scale)), max(1, int(im.height * scale))), Image.LANCZOS)

    icon = card.copy()
    shadow = Image.new("RGBA", icon.size)
    sd = ImageDraw.Draw(shadow)
    cx = ICON // 2
    base_y = PAD + im.height + (inner - im.height) // 2
    sd.ellipse([cx - im.width // 2, base_y - 10, cx + im.width // 2, base_y + 8], fill=(0, 0, 0, 110))
    icon = Image.alpha_composite(icon, shadow.filter(ImageFilter.GaussianBlur(4)))
    x = (ICON - im.width) // 2
    y = PAD + (inner - im.height) // 2
    icon.alpha_composite(im, (x, y))

    out_id = RENAME.get(stem, stem)
    out_path = os.path.join(OUT, f"shop_icon_{out_id}.png")
    icon.save(out_path)
    return out_path


os.makedirs(OUT, exist_ok=True)
card = make_card()
for stem in PIECES:
    print(build_icon(stem, card))
print("done")
