"""
Replace every converted unit's idle with a TRUE standing idle (Meshy action 11,
Idle_02, authored ~2.33s) — action 0 turned out to be a marching-in-place loop,
which is wrong now that the game only shows Idle on genuinely stationary units.

Rig task ids recovered from the Meshy API listing (serial batch order matches
creation timestamps). A wrong mapping would render the WRONG CHARACTER MODEL,
so visually verify the contact sheet before trusting installs.

  python replace_idles.py
"""

import os
import shutil
import subprocess
import sys
import time

import meshy_client as mc
import convert_unit as cu

IDLE_ACTION = 11  # Idle_02: standing, subtle weight shift

RIGS = {
    "conscript_rifleman":     "019f48a8-3918-72be-9e37-909b0b53d7a9",
    "shock_trooper":          "019f4960-4ed6-7ec5-b415-20a6c8b18982",
    "ironclad_field_marshal": "019f4960-b065-76f0-be08-bf00b24f079d",
    "enlisted_rifleman":      "019f4968-20c2-781a-87ee-9581d062f841",
    "field_medic":            "019f496d-9355-7210-80d8-2d8f941a17d8",
    "grenade_thrower":        "019f4973-3e41-7a59-8765-71149876f249",
    "ironclad_marksman":      "019f4980-25b3-7e0a-acd5-eca697263e1b",
    "ironclad_mortars":       "019f4986-bd13-7e6a-bd06-1d70bad559ae",
    "ironmarch_breacher":     "019f498c-92b3-7de9-a0a8-ddfaf2acfd56",
    "ironmarch_engineer":     "019f4993-621e-7f56-87c7-45ae99af31a5",
    "ironmarch_sniper":       "019f499a-6596-70e3-b04f-c7eb6bca545b",
    "ironmarch_surgeon":      "019f49a3-4e54-7b2a-ab32-6965145e5c29",
    "bulwark_squad":          "019f49aa-1be5-743a-987b-92b5d7c7c8fa",
    "marksman_squad":         "019f49b0-3059-7dfd-a592-6329bfb7b9ec",
    "rifle_squad":            "019f49b7-e62a-7984-8b37-38d299154204",
}


def log(msg):
    print(f"[{time.strftime('%H:%M:%S')}] {msg}", flush=True)


def main():
    # 1) submit all animation tasks up front (conscript's Idle_02 already exists)
    pending = {}
    for unit, rig in RIGS.items():
        if unit == "conscript_rifleman":
            continue
        task = mc.request("POST", "animations",
                          {"rig_task_id": rig, "action_id": IDLE_ACTION})["result"]
        pending[unit] = task
        log(f"submitted {unit}: {task}")

    # conscript: reuse the pilot download
    conscript_glb = os.path.join(cu.HERE, "output", "glb",
                                 "idle02_animation_glb.glb")
    glb_by_unit = {"conscript_rifleman": conscript_glb}

    # 2) poll + download
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
        dest = os.path.join(cu.HERE, "units", unit, "glb", "idle.glb")
        os.makedirs(os.path.dirname(dest), exist_ok=True)
        import urllib.request
        urllib.request.urlretrieve(url, dest)
        glb_by_unit[unit] = dest
        log(f"downloaded {unit}")

    # 3) render + pack + install (backup already holds pre-conversion originals;
    #    keep a copy of the marching idle too, first time only)
    results = {}
    for unit, glb in glb_by_unit.items():
        try:
            frames = os.path.join(cu.HERE, "units", unit, "frames_idle2")
            cmd = [cu.BLENDER, "--background", "--python",
                   os.path.join(cu.HERE, "render_frames.py"), "--",
                   "--glb", glb, "--out", frames, "--yaw", "180",
                   "--ortho", "2.4", "--cam-height", "0.95",
                   "--frames", "49", "--loop"]
            subprocess.run(cmd, check=True, capture_output=True)
            sheet = os.path.join(cu.HERE, "units", unit, "sheets2",
                                 f"{unit}_idle.png")
            subprocess.run([sys.executable,
                            os.path.join(cu.HERE, "pack_sheet.py"),
                            "--frames", frames, "--out", sheet], check=True)
            dst = os.path.join(cu.ANIM, unit, f"{unit}_idle.png")
            march_backup = os.path.join(cu.HERE, "backup", unit,
                                        f"{unit}_idle_marching.png")
            if os.path.isfile(dst) and not os.path.isfile(march_backup):
                os.makedirs(os.path.dirname(march_backup), exist_ok=True)
                shutil.copy2(dst, march_backup)
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
