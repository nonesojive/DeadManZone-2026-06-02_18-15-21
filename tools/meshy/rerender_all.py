"""
Re-render every converted unit from its CACHED GLBs with the current
render_frames.py lighting (no Meshy calls). Installs in place. Does NOT touch
backups (originals were saved on the first conversion pass).

  python rerender_all.py            # all units below
  python rerender_all.py u1 u2 ...  # explicit list
"""

import os
import subprocess
import sys

import convert_unit as cu

# conscript_rifleman already re-rendered separately with the new lighting.
UNITS = [
    "shock_trooper", "ironclad_field_marshal", "enlisted_rifleman",
    "field_medic", "grenade_thrower", "ironclad_marksman", "ironclad_mortars",
    "ironmarch_breacher", "ironmarch_engineer", "ironmarch_sniper",
    "ironmarch_surgeon", "bulwark_squad", "marksman_squad", "rifle_squad",
]
HERE = cu.HERE
BLENDER = cu.BLENDER
LOOP = cu.LOOP_CLIPS
SUFFIX = cu.FILE_SUFFIX


def rerender(unit):
    clips = cu.populated_clips(unit)
    glb_dir = os.path.join(HERE, "units", unit, "glb")
    work = os.path.join(HERE, "units", unit)
    done = 0
    for clip in clips:
        glb = os.path.join(glb_dir, f"{clip}.glb")
        if not os.path.isfile(glb):
            print(f"  MISSING {unit}/{clip}.glb -- skipped", flush=True)
            continue
        frames = os.path.join(work, "frames2", clip)
        cmd = [BLENDER, "--background", "--python",
               os.path.join(HERE, "render_frames.py"), "--",
               "--glb", glb, "--out", frames, "--yaw", "180",
               "--ortho", "2.4", "--cam-height", "0.95", "--frames", "49"]
        if clip in LOOP:
            cmd.append("--loop")
        subprocess.run(cmd, check=True, capture_output=True)

        suffix = SUFFIX.get(clip, clip)
        sheet = os.path.join(work, "sheets2", f"{unit}_{suffix}.png")
        subprocess.run([sys.executable, os.path.join(HERE, "pack_sheet.py"),
                        "--frames", frames, "--out", sheet], check=True)
        dst = os.path.join(cu.ANIM, unit, f"{unit}_{suffix}.png")
        import shutil
        shutil.copy2(sheet, dst)
        done += 1
    return done, len(clips)


def main():
    units = sys.argv[1:] or UNITS
    results = {}
    for i, unit in enumerate(units, 1):
        print(f"[{i}/{len(units)}] {unit}", flush=True)
        try:
            d, t = rerender(unit)
            results[unit] = f"OK {d}/{t}"
        except Exception as e:
            results[unit] = f"FAIL {e}"
        print(f"  -> {results[unit]}", flush=True)
    print("\n=== SUMMARY ===")
    for u, s in results.items():
        print(f"  {u:26} {s}")


if __name__ == "__main__":
    main()
