#!/usr/bin/env python3
"""DeadManZone grimdark icon set generator.
Bone (#EBDEBD) silhouettes, 64x64 viewBox each, negative detail in plate color.
Outputs: contact sheet SVG + PNG for approval; per-icon SVGs for later export.
"""
import math, os

BONE = "#EBDEBD"
PLATE = "#131110"      # CardBody
SHEET_BG = "#0B0908"   # BandDark-ish, opaque
LEATHER = "#2B2219"    # ButtonLeather
GOLD = "#E6C780"       # VictoryGold (headers only)
TEXT = "#D1C7B3"       # BodyText

def P(pts):
    return " ".join(f"{x:.1f},{y:.1f}" for x, y in pts)

def thick_line(x1, y1, x2, y2, w, tip=0.0, fill=BONE):
    dx, dy = x2 - x1, y2 - y1
    L = math.hypot(dx, dy)
    ux, uy = dx / L, dy / L
    px, py = -uy * w / 2, ux * w / 2
    pts = [(x1 + px, y1 + py), (x2 + px, y2 + py)]
    if tip > 0:
        pts.append((x2 + ux * tip, y2 + uy * tip))
    pts += [(x2 - px, y2 - py), (x1 - px, y1 - py)]
    return f'<polygon points="{P(pts)}" fill="{fill}"/>'

def rect(x, y, w, h, rx=0, fill=BONE):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{rx}" fill="{fill}"/>'

def circ(cx, cy, r, fill=BONE):
    return f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="{fill}"/>'

def ring(cx, cy, r, sw, stroke=BONE):
    return f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="none" stroke="{stroke}" stroke-width="{sw}"/>'

def poly(pts, fill=BONE):
    return f'<polygon points="{P(pts)}" fill="{fill}"/>'

def path(d, fill=BONE, stroke=None, sw=0):
    s = f' stroke="{stroke}" stroke-width="{sw}" stroke-linecap="round"' if stroke else ""
    return f'<path d="{d}" fill="{fill}"{s}/>'

def chevron(cy, x0=15, x1=49, apex_dy=12, band=7):
    cx = (x0 + x1) / 2
    return poly([(x0, cy), (cx, cy - apex_dy), (x1, cy),
                 (x1, cy + band), (cx, cy - apex_dy + band), (x0, cy + band)])

def cog(cx, cy, r_body, r_teeth, n, tooth_w, tooth_h, hole_r):
    out = []
    for i in range(n):
        a = 2 * math.pi * i / n
        tx, ty = cx + math.cos(a) * r_teeth, cy + math.sin(a) * r_teeth
        ux, uy = math.cos(a), math.sin(a)          # radial
        px, py = -uy, ux                            # tangential
        hw, hh = tooth_w / 2, tooth_h / 2
        pts = [(tx + px * hw + ux * hh, ty + py * hw + uy * hh),
               (tx - px * hw + ux * hh, ty - py * hw + uy * hh),
               (tx - px * hw - ux * hh, ty - py * hw - uy * hh),
               (tx + px * hw - ux * hh, ty + py * hw - uy * hh)]
        out.append(poly(pts))
    out.append(circ(cx, cy, r_body))
    out.append(circ(cx, cy, hole_r, PLATE))
    return "".join(out)

def arc(cx, cy, r, a0, a1, sw, stroke=BONE):
    x0, y0 = cx + r * math.cos(math.radians(a0)), cy + r * math.sin(math.radians(a0))
    x1, y1 = cx + r * math.cos(math.radians(a1)), cy + r * math.sin(math.radians(a1))
    large = 1 if abs(a1 - a0) > 180 else 0
    return (f'<path d="M{x0:.1f},{y0:.1f} A{r},{r} 0 {large} 1 {x1:.1f},{y1:.1f}" '
            f'fill="none" stroke="{stroke}" stroke-width="{sw}" stroke-linecap="round"/>')

ICONS = {}

# ---------- RESOURCES ----------
ICONS["supplies"] = (
    rect(12, 15, 40, 8, 1) +
    rect(14, 26, 36, 25, 1) +
    f'<line x1="17" y1="29" x2="47" y2="48" stroke="{PLATE}" stroke-width="3"/>'
    f'<line x1="47" y1="29" x2="17" y2="48" stroke="{PLATE}" stroke-width="3"/>' +
    rect(28, 17, 8, 4, 1, PLATE)
)

ICONS["manpower"] = (
    path("M14,36 A18,18 0 0 1 50,36 Z") +
    rect(7, 36, 50, 6, 3) +
    circ(32, 20, 1.8, PLATE)
)

ICONS["dread"] = (
    circ(32, 27, 14.5) +
    rect(23, 33, 18, 13, 4) +
    circ(26, 27, 4.4, PLATE) + circ(38, 27, 4.4, PLATE) +
    poly([(30, 36.5), (34, 36.5), (32, 31.5)], PLATE) +
    "".join(f'<rect x="{x}" y="40" width="1.8" height="6" fill="{PLATE}"/>' for x in (27, 30.5, 34, 37.5))
)

ICONS["authority"] = chevron(27) + chevron(39) + chevron(51)

ICONS["army_strength"] = (
    # raised gauntlet fist
    "".join(circ(x, 26.5, 4.4) for x in (24, 29.5, 35, 40.5)) +
    rect(19.5, 26, 25.5, 21, 5) +
    rect(43, 34, 6.5, 11, 3) +           # thumb
    rect(23, 48, 18, 5) +                 # wrist
    rect(21, 54, 22, 5, 1) +              # cuff
    "".join(f'<rect x="{x}" y="23" width="1.6" height="8" fill="{PLATE}"/>' for x in (26.9, 32.4, 37.9)) +
    circ(26, 51, 1.4, PLATE) + circ(38, 51, 1.4, PLATE)
)

ICONS["salvage"] = (
    # horseshoe magnet (poles up) attracting a small cog
    path("M20,18 V38 A12,12 0 0 0 44,38 V18 H35 V38 A3,3 0 0 1 29,38 V18 Z") +
    rect(20, 21, 9, 3.5, 0, PLATE) + rect(35, 21, 9, 3.5, 0, PLATE) +
    cog(32, 9, 5, 6.8, 6, 3.2, 3.2, 2) +
    thick_line(22, 13, 25, 16.5, 2) + thick_line(42, 13, 39, 16.5, 2)
)

ICONS["sell_zone"] = (
    rect(38, 9, 8, 13) +
    rect(14, 20, 36, 31, 3) +
    path("M23,51 V41 A9,9 0 0 1 41,41 V51 Z", PLATE) +
    path("M32,36 C34.5,40 37,41 37,44.5 C37,48 34.8,50 32,50 C29.2,50 27,48 27,44.5 C27,41 29.5,40 32,36 Z") +
    rect(16, 51, 7, 5) + rect(41, 51, 7, 5)
)

# ---------- DAMAGE TYPES ----------
ICONS["piercing"] = (
    thick_line(20, 48, 44, 20, 6.5, tip=9) +
    poly([(20, 48), (12, 60), (24, 52)]) +      # fin
    thick_line(30, 56, 38, 47, 2.4) +
    thick_line(38, 58, 44, 51, 2.4)
)

ICONS["ballistic"] = (
    path("M26,27 Q26,13 32,9 Q38,13 38,27 Z") +
    rect(26, 26, 12, 18) +
    rect(23.5, 45, 17, 5.5, 1) +
    f'<line x1="26" y1="30" x2="38" y2="30" stroke="{PLATE}" stroke-width="1.8"/>'
)

def _burst():
    pts = []
    R_out = [17, 12, 16, 11, 17, 12, 15, 11]
    R_in = [6.5, 7, 6, 7, 6.5, 6, 7, 6.5]
    for i in range(8):
        a_o = 2 * math.pi * i / 8 - math.pi / 2
        a_i = a_o + math.pi / 8
        pts.append((32 + R_out[i] * math.cos(a_o), 33 + R_out[i] * math.sin(a_o)))
        pts.append((32 + R_in[i] * math.cos(a_i), 33 + R_in[i] * math.sin(a_i)))
    return poly(pts)
ICONS["shredding"] = _burst()

ICONS["fire"] = (
    path("M32,8 C36,16 44,21 44,34 C44,46 38,54 32,54 C26,54 20,46 20,34 "
         "C20,26 25,21 26,14 C28,20 31,19 32,8 Z") +
    path("M32,31 C34.5,35.5 38,36.5 38,42 C38,47.5 35,51 32,51 C29,51 26,47.5 26,42 "
         "C26,36.5 29.5,35.5 32,31 Z", PLATE)
)

ICONS["explosive"] = (
    circ(29, 39, 13.5) +
    rect(24, 21, 10, 6, 1) +
    path("M29,21 C33,13 39,16 44,11", "none", BONE, 3) +
    poly([(46, 4), (48, 9), (53, 11), (48, 13), (46, 18), (44, 13), (39, 11), (44, 9)])
)

ICONS["gas"] = (
    circ(20, 39, 8.5) + circ(31, 32, 11) + circ(43, 35, 9) + circ(48, 42, 6) +
    rect(14, 39, 40, 9, 4.5) +
    circ(50, 23, 2.6) + circ(46, 16.5, 2.1) + circ(52, 11, 1.7)
)

ICONS["melee"] = (
    poly([(28.5, 45), (28.5, 20), (32, 9), (35.5, 20), (35.5, 45)]) +
    f'<rect x="31" y="17" width="2" height="24" fill="{PLATE}"/>' +
    rect(22, 44, 20, 5, 2) +
    rect(28, 50, 8, 9, 2) +
    rect(27, 59.5, 10, 3.5, 1.5) +
    circ(32, 54.5, 1.3, PLATE)
)

# ---------- COMBAT ROLES ----------
ICONS["assault"] = (
    thick_line(14, 50, 44, 21, 5.5) +
    thick_line(44, 21, 51, 14, 2.6, tip=4) +
    thick_line(11, 56, 19, 46, 7.5) +
    thick_line(30, 38, 33, 45, 3.5)          # magazine nub
)

ICONS["tank"] = (
    # WWI rhomboid (Mark I) side profile: high front-top point, sloped rear
    poly([(10, 45), (22, 27), (45, 24), (59, 37), (52, 51), (17, 52)]) +
    rect(54, 34, 9, 4.5, 1) +                     # sponson gun stub
    f'<polygon points="{P([(16,44),(24,32),(43,29.5),(53,38),(48,46.5),(21,47)])}" '
    f'fill="none" stroke="{PLATE}" stroke-width="2.2"/>' +
    "".join(circ(x, y, 2.0, PLATE) for x, y in ((23, 42), (32, 42.5), (41, 41)))
)

ICONS["artillery"] = (
    thick_line(24, 42, 51, 16, 6) +
    thick_line(24, 46, 11, 57, 4.5) +
    circ(23, 45, 9.5) + circ(23, 45, 3, PLATE) +
    f'<line x1="23" y1="37" x2="23" y2="53" stroke="{PLATE}" stroke-width="1.8"/>'
    f'<line x1="15" y1="45" x2="31" y2="45" stroke="{PLATE}" stroke-width="1.8"/>'
)

ICONS["support"] = (
    rect(19, 27, 23, 25, 2) +
    rect(23, 32, 15, 2.4, 0, PLATE) +
    circ(26, 45, 2.2, PLATE) + circ(33, 45, 2.2, PLATE) +
    thick_line(40, 28, 48, 11, 2.6) +
    arc(48, 10, 6, -55, 35, 2.4) +
    arc(48, 10, 11, -55, 35, 2.4)
)

ICONS["utility"] = (
    thick_line(23, 45, 42, 26, 6.5) +
    circ(44, 24, 9) +
    poly([(44, 24), (58, 12), (58, 24), (50, 30)], PLATE) +
    circ(21, 47, 7.5) + circ(21, 47, 3.2, PLATE)
)

ICONS["sniper"] = (
    ring(32, 32, 15, 4.5) +
    rect(30, 7, 4, 11) + rect(30, 46, 4, 11) +
    rect(7, 30, 11, 4) + rect(46, 30, 11, 4) +
    circ(32, 32, 3.6)
)

ICONS["defender"] = (
    path("M32,8 L52,15 L52,30 C52,44 44,52 32,57 C20,52 12,44 12,30 L12,15 Z") +
    "".join(circ(x, y, 1.8, PLATE) for x, y in ((32, 15.5), (20, 19), (44, 19), (15.5, 30), (48.5, 30)))
)

ICONS["hp"] = rect(25, 11, 15, 42, 3) + rect(11, 25, 42, 15, 3)
ICONS["morale"] = (
    rect(17, 8, 4.5, 48) + circ(19.2, 7, 3.2) +
    poly([(21.5, 12), (52, 12), (45, 23), (52, 34), (21.5, 34)])
)

# ---------- BUFFS & CRITICAL MASS ----------
ICONS["infantry"] = (
    thick_line(16, 52, 43, 17, 5, tip=6) +
    thick_line(48, 52, 21, 17, 5, tip=6)
)

ICONS["vehicle"] = (
    ring(32, 32, 16, 7) +
    "".join(thick_line(32 + 10*dx, 32 + 10*dy, 32 + 3*dx, 32 + 3*dy, 4) for dx, dy in
            ((0.707,0.707),(-0.707,0.707),(0.707,-0.707),(-0.707,-0.707))) +
    circ(32, 32, 5.5) + circ(32, 32, 2, PLATE)
)

ICONS["structure"] = (
    poly([(12, 50), (18, 27), (46, 27), (52, 50)]) +
    rect(15, 20, 34, 8, 2) +
    rect(22, 36, 20, 5, 2, PLATE) +
    rect(10, 50, 44, 5, 2)
)

def _shield(x, y, w, h):
    return (f'<path d="M{x+w/2},{y} L{x+w},{y+h*0.16} V{y+h*0.5} '
            f'C{x+w},{y+h*0.78} {x+w*0.75},{y+h*0.92} {x+w/2},{y+h} '
            f'C{x+w*0.25},{y+h*0.92} {x},{y+h*0.78} {x},{y+h*0.5} V{y+h*0.16} Z" '
            f'fill="{BONE}" stroke="{PLATE}" stroke-width="2"/>')
ICONS["phalanx"] = _shield(8, 18, 22, 30) + _shield(21, 16, 22, 32) + _shield(34, 18, 22, 30)

ICONS["command"] = (
    path("M16,37 C16,26 23,21 32,21 C41,21 48,26 48,37 Z") +
    rect(13, 37, 38, 8, 3) +
    rect(13, 36, 38, 2, 0, PLATE) +
    rect(18, 45, 28, 5, 2.5) +
    rect(18, 44.4, 28, 1.6, 0, PLATE) +
    circ(32, 30, 2.8, PLATE)
)

ICONS["supplier"] = (
    rect(19, 15, 25, 16, 1) +
    f'<line x1="21" y1="17" x2="42" y2="29" stroke="{PLATE}" stroke-width="2.4"/>'
    f'<line x1="42" y1="17" x2="21" y2="29" stroke="{PLATE}" stroke-width="2.4"/>' +
    rect(13, 34, 38, 19, 1) +
    f'<line x1="16" y1="37" x2="48" y2="50" stroke="{PLATE}" stroke-width="2.6"/>'
    f'<line x1="48" y1="37" x2="16" y2="50" stroke="{PLATE}" stroke-width="2.6"/>'
)

ICONS["convoy"] = (
    poly([(40, 24), (53, 24), (58, 33), (58, 44), (40, 44)]) +
    rect(12, 29, 29, 15, 1) +
    circ(21, 46, 5) + circ(21, 46, 2, PLATE) +
    circ(48, 46, 5) + circ(48, 46, 2, PLATE) +
    thick_line(4, 31, 10, 31, 2.6) + thick_line(2, 38, 8, 38, 2.6)
)

ICONS["medic"] = (
    path("M21,22 C21,11 43,11 43,22", "none", BONE, 3.5) +
    rect(13, 22, 38, 31, 6) +
    rect(29, 28, 6, 19, 1.5, PLATE) + rect(22.5, 34.5, 19, 6, 1.5, PLATE)
)

ICONS["mechanic"] = (
    thick_line(19, 49, 39, 29, 5) + circ(41, 27, 7.5) +
    poly([(41, 27), (53, 16), (53, 26), (46, 32)], PLATE) +
    thick_line(45, 49, 28, 32, 4.5) +
    poly([(18, 26), (25, 19), (33, 27), (26, 34)])
)

ICONS["fanatic"] = (
    path("M32,54 C17,41 13,31 19,24 C24,19 31,23 32,29 C33,23 40,19 45,24 C51,31 47,41 32,54 Z") +
    path("M32,4 C35.5,10 39,12 37.5,18 C36.5,22.5 27.5,22.5 26.5,18 C25,12 28.5,10 32,4 Z")
)

ICONS["entrenched"] = (
    rect(24, 8, 16, 5, 2.5) +
    rect(30, 12, 4.5, 22) +
    path("M22,33 H42 V41 C42,49 37,54 32,57 C27,54 22,49 22,41 Z")
)

ICONS["berserk"] = (
    # three claw slashes, pointed both ends
    "".join(poly([(x1, y1), ((x1+x2)/2 + 3.2, (y1+y2)/2 + 2.8), (x2, y2), ((x1+x2)/2 - 3.2, (y1+y2)/2 - 2.8)])
            for x1, y1, x2, y2 in ((12, 46, 30, 10), (23, 52, 41, 16), (34, 58, 52, 22)))
)

ICONS["grenadier"] = (
    rect(23, 8, 18, 20, 3) +
    rect(23, 14, 18, 2.6, 0, PLATE) +
    rect(28, 28, 8, 24, 2) +
    ring(32, 56, 3.2, 2.2)
)

ICONS["siege"] = (
    rect(20, 14, 7, 9) + rect(28.5, 14, 7, 9) + rect(37, 14, 7, 9) +
    rect(20, 21, 24, 34) +
    poly([(32, 21), (28, 29), (33, 35), (28, 43), (33, 52), (30, 55), (26, 45), (30, 37), (25, 29), (28, 21)], PLATE)
)

ICONS["logistics"] = (
    rect(7, 25, 17, 15, 1) +
    f'<line x1="9" y1="27" x2="22" y2="38" stroke="{PLATE}" stroke-width="2"/>'
    f'<line x1="22" y1="27" x2="9" y2="38" stroke="{PLATE}" stroke-width="2"/>' +
    thick_line(27, 32.5, 36, 32.5, 3, tip=5) +
    rect(40, 25, 17, 15, 1) +
    f'<line x1="42" y1="27" x2="55" y2="38" stroke="{PLATE}" stroke-width="2"/>'
    f'<line x1="55" y1="27" x2="42" y2="38" stroke="{PLATE}" stroke-width="2"/>' +
    thick_line(20, 48, 44, 48, 2.4)
)

ICONS["ironmarch_union"] = (
    path("M14,22 C7,22 5,27 8,30 L14,31 Z") +
    rect(13, 21, 41, 9, 3) +
    poly([(24, 30), (41, 30), (37, 42), (28, 42)]) +
    rect(22, 42, 21, 5, 2) +
    rect(17, 47, 31, 6, 2)
)

ICONS["lock"] = (
    path("M22,30 V22 A10,10 0 0 1 42,22 V30", "none", BONE, 6) +
    rect(16, 30, 32, 24, 4) +
    circ(32, 40, 3.5, PLATE) + rect(30.4, 42, 3.2, 6, 0, PLATE)
)

GROUPS = [
    ("RESOURCES &amp; ECONOMY", ["supplies", "manpower", "dread", "authority", "army_strength", "salvage", "sell_zone"]),
    ("DAMAGE TYPES", ["piercing", "ballistic", "shredding", "fire", "explosive", "gas", "melee"]),
    ("COMBAT ROLES", ["assault", "tank", "artillery", "support", "utility", "sniper", "defender"]),
]

LABELS = {"army_strength": "ARMY STRENGTH", "sell_zone": "SELL ZONE", "salvage": "SALVAGE CHANCE", "ironmarch_union": "IRONMARCH CREST"}

def label(n):
    return LABELS.get(n, n.upper())

def build_sheet(out_svg, groups=None):
    cell_w, cell_h, plate = 116, 138, 88
    cols = 7
    header_h = 44
    W = cols * cell_w + 40
    H = 30 + sum(header_h + cell_h for _ in (groups or GROUPS)) + 20
    s = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">']
    s.append(f'<rect width="{W}" height="{H}" fill="{SHEET_BG}"/>')
    y = 30
    font = "Georgia, 'Times New Roman', serif"
    for title, names in (groups or GROUPS):
        s.append(f'<text x="22" y="{y+18}" fill="{GOLD}" font-family="{font}" font-size="17" '
                 f'letter-spacing="3" font-weight="bold">{title}</text>')
        s.append(f'<line x1="22" y1="{y+27}" x2="{W-22}" y2="{y+27}" stroke="{LEATHER}" stroke-width="2"/>')
        y += header_h
        for i, n in enumerate(names):
            x = 22 + i * cell_w
            s.append(f'<rect x="{x}" y="{y}" width="{plate}" height="{plate}" rx="7" fill="{PLATE}" '
                     f'stroke="{LEATHER}" stroke-width="2"/>')
            pad = 14
            sc = (plate - 2 * pad) / 64.0
            s.append(f'<g transform="translate({x+pad},{y+pad}) scale({sc:.4f})">{ICONS[n]}</g>')
            s.append(f'<text x="{x+plate/2}" y="{y+plate+22}" fill="{TEXT}" font-family="{font}" '
                     f'font-size="9.5" letter-spacing="0.4" text-anchor="middle">{label(n)}</text>')
        y += cell_h
    s.append('</svg>')
    with open(out_svg, "w") as f:
        f.write("".join(s))
    return out_svg

def write_singles(outdir):
    os.makedirs(outdir, exist_ok=True)
    for name, body in ICONS.items():
        with open(os.path.join(outdir, f"icon_{name}.svg"), "w") as f:
            f.write(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">{body}</svg>')

if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__))
    svg = build_sheet(os.path.join(here, "icon_contact_sheet.svg"))
    BUFF_GROUPS = [
        ("CRIT MASS — PRIMARIES, RUN, FACTION", ["infantry", "vehicle", "structure", "command", "supplier", "logistics", "ironmarch_union"]),
        ("CRIT MASS — SYNERGIES", ["phalanx", "convoy", "medic", "mechanic", "fanatic", "entrenched", "siege"]),
        ("ABILITIES + UTILITY GLYPHS", ["berserk", "grenadier", "hp", "morale", "lock"]),
    ]
    build_sheet(os.path.join(here, "buff_contact_sheet.svg"), BUFF_GROUPS)
    write_singles(os.path.join(here, "icon_svgs"))
    print("done")
