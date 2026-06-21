"""Generate 512x512 temp tag icons (grey bg, black border, white label). ponytail: PIL batch; Photoshop JSX timed out at 65."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parents[1] / "Assets" / "critmassicons"
SIZE = 512
BORDER = 18
BG = (210, 210, 210)
FG = (255, 255, 255)
EDGE = (0, 0, 0)

TAGS: list[tuple[str, str]] = [
    ("infantry", "Infantry"),
    ("vehicle", "Vehicle"),
    ("building", "Building"),
    ("structure", "Structure"),
    ("assault", "Assault"),
    ("tank", "Tank"),
    ("artillery", "Artillery"),
    ("support", "Support"),
    ("utility", "Utility"),
    ("headquarters", "Headquarters"),
    ("sniper", "Sniper"),
    ("defender", "Defender"),
    ("combatant", "Combatant"),
    ("noncombatant", "Non-Combatant"),
    ("hq", "HQ"),
    ("ballistic", "Ballistic"),
    ("piercing", "Piercing"),
    ("shredding", "Shredding"),
    ("explosive", "Explosive"),
    ("fire", "Fire"),
    ("melee", "Melee"),
    ("gas", "Gas"),
    ("phalanx", "Phalanx"),
    ("inspiring", "Inspiring"),
    ("medic", "Medic"),
    ("mechanic", "Mechanic"),
    ("spotter", "Spotter"),
    ("fortify", "Fortify"),
    ("jammer", "Jammer"),
    ("bunker", "Bunker"),
    ("fanatic", "Fanatic"),
    ("supplier", "Supplier"),
    ("entrenched", "Entrenched"),
    ("bombard", "Bombard"),
    ("gas_cloud", "Gas Cloud"),
    ("convoy", "Convoy"),
    ("supply_line", "Supply Line"),
    ("gas_division", "Gas Division"),
    ("chemical_corps", "Chemical Corps"),
    ("stealth", "Stealth"),
    ("ambush", "Ambush"),
    ("berserk", "Berserk"),
    ("emp", "EMP"),
    ("grenadier", "Grenadier"),
    ("suppression", "Suppression"),
    ("repair", "Repair"),
    ("last_stand", "Last Stand"),
    ("taunt", "Taunt"),
    ("flamethrower", "Flamethrower"),
    ("echo", "Echo"),
    ("toxic", "Toxic"),
    ("ironclad", "Ironclad"),
    ("fortified", "Fortified"),
    ("veteran", "Veteran"),
    ("prototype", "Prototype"),
    ("mercenary", "Mercenary"),
    ("siege", "Siege"),
    ("fortification", "Fortification"),
    ("logistics", "Logistics"),
    ("command", "Command"),
    ("bomber", "Bomber"),
    ("airstrip", "Airstrip"),
    ("gas_mask", "Gas Mask"),
    ("bastion", "Bastion"),
    ("testsynergy", "Test Synergy"),
]


def load_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    for name in ("arialbd.ttf", "Arial Bold.ttf", "segoeuib.ttf", "calibrib.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def font_size(label: str) -> int:
    n = len(label)
    if n > 16:
        return 34
    if n > 12:
        return 40
    if n > 8:
        return 46
    return 54


def wrap_label(draw: ImageDraw.ImageDraw, label: str, font: ImageFont.ImageFont, max_w: int) -> str:
    words = label.split()
    if len(words) <= 1:
        return label
    lines: list[str] = []
    current = words[0]
    for word in words[1:]:
        trial = f"{current} {word}"
        if draw.textlength(trial, font=font) <= max_w:
            current = trial
        else:
            lines.append(current)
            current = word
    lines.append(current)
    return "\n".join(lines)


def render(tag_id: str, label: str) -> None:
    img = Image.new("RGB", (SIZE, SIZE), BG)
    draw = ImageDraw.Draw(img)
    draw.rectangle((0, 0, SIZE - 1, BORDER - 1), fill=EDGE)
    draw.rectangle((0, SIZE - BORDER, SIZE - 1, SIZE - 1), fill=EDGE)
    draw.rectangle((0, 0, BORDER - 1, SIZE - 1), fill=EDGE)
    draw.rectangle((SIZE - BORDER, 0, SIZE - 1, SIZE - 1), fill=EDGE)

    inner_w = SIZE - BORDER * 2 - 24
    size = font_size(label)
    font = load_font(size)
    text = wrap_label(draw, label, font, inner_w)
    bbox = draw.multiline_textbbox((0, 0), text, font=font, align="center", spacing=4)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    x = (SIZE - tw) / 2 - bbox[0]
    y = (SIZE - th) / 2 - bbox[1]
    draw.multiline_text((x, y), text, font=font, fill=FG, align="center", spacing=4)

    OUT.mkdir(parents=True, exist_ok=True)
    img.save(OUT / f"{tag_id}_tempicon.png", format="PNG")


def main() -> None:
    for tag_id, label in TAGS:
        render(tag_id, label)
    print(f"Wrote {len(TAGS)} icons to {OUT}")


if __name__ == "__main__":
    main()
