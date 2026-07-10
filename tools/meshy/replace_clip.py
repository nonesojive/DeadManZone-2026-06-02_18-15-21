"""
Replace ONE animation clip across all converted units (generalizes
replace_idles.py — same rig ids, any clip/action).

  python replace_clip.py <clip> <action_id> [--loop] [--reuse-conscript GLB]

e.g. shoot replacement (Side_Shot twist read as a warp when compressed):
  python replace_clip.py shoot 585 --reuse-conscript output/glb/shootc585_animation_glb.glb
"""

import argparse
import os
import shutil
import subprocess
import sys
import time
import urllib.request

import convert_unit as cu
import meshy_client as mc
from replace_idles import RIGS


def log(msg):
    print(f"[{time.strftime('%H:%M:%S')}] {msg}", flush=True)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("clip")
    p.add_argument("action_id", type=int)
    p.add_argument("--loop", action="store_true")
    p.add_argument("--reuse-conscript", default=None,
                   help="existing GLB for conscript_rifleman (skip its anim task)")
    args = p.parse_args()

    suffix = cu.FILE_SUFFIX.get(args.clip, args.clip)

    glb_by_unit = {}
    pending = {}
    for unit, rig in RIGS.items():
        # only touch units whose anim_set populates this clip
        if args.clip not in cu.populated_clips(unit):
            log(f"skip {unit}: no {args.clip} clip")
            continue
        if unit == "conscript_rifleman" and args.reuse_conscript:
            glb_by_unit[unit] = os.path.abspath(args.reuse_conscript)
            continue
        task = mc.request("POST", "animations",
                          {"rig_task_id": rig, "action_id": args.action_id})["result"]
        pending[unit] = task
        log(f"submitted {unit}: {task}")

    for unit, task in pending.items():
        while True:
            t = mc.get_task("anim", task)
            if t.get("status") == "SUCCEEDED":
                break
            if t.get("status") in ("FAILED", "CANCELED"):
                log(f"FAILED {unit}: {t.get('task_error')}")
                t = None
                break
            time.sleep(10)
        if t is None:
            continue
        url = mc.collect_urls(t.get("result", {})).get("animation_glb_url")
        dest = os.path.join(cu.HERE, "units", unit, "glb", f"{args.clip}.glb")
        os.makedirs(os.path.dirname(dest), exist_ok=True)
        urllib.request.urlretrieve(url, dest)
        glb_by_unit[unit] = dest
        log(f"downloaded {unit}")

    results = {}
    for unit, glb in glb_by_unit.items():
        try:
            frames = os.path.join(cu.HERE, "units", unit, f"frames_{args.clip}2")
            cmd = [cu.BLENDER, "--background", "--python",
                   os.path.join(cu.HERE, "render_frames.py"), "--",
                   "--glb", glb, "--out", frames, "--yaw", "180",
                   "--ortho", "2.4", "--cam-height", "0.95", "--frames", "49"]
            if args.loop:
                cmd.append("--loop")
            subprocess.run(cmd, check=True, capture_output=True)
            sheet = os.path.join(cu.HERE, "units", unit, "sheets2",
                                 f"{unit}_{suffix}.png")
            subprocess.run([sys.executable, os.path.join(cu.HERE, "pack_sheet.py"),
                            "--frames", frames, "--out", sheet], check=True)
            dst = os.path.join(cu.ANIM, unit, f"{unit}_{suffix}.png")
            old_backup = os.path.join(cu.HERE, "backup", unit,
                                      f"{unit}_{suffix}_replaced.png")
            if os.path.isfile(dst) and not os.path.isfile(old_backup):
                os.makedirs(os.path.dirname(old_backup), exist_ok=True)
                shutil.copy2(dst, old_backup)
            shutil.copy2(sheet, dst)
            results[unit] = "OK"
        except Exception as e:
            results[unit] = f"FAIL {e}"
        log(f"{unit}: {results[unit]}")

    print("\n=== SUMMARY ===")
    for u, s in results.items():
        print(f"  {u:26} {s}")


if __name__ == "__main__":
    main()
