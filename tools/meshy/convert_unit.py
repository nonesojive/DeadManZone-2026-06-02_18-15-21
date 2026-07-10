"""
DeadManZone - convert ONE humanoid unit's sprite sheets to the 3D-rendered
pipeline, end to end. Reads which animation clips the unit's anim_set actually
populates and only produces those (avoids old-art flashing on run/hurt).

  python convert_unit.py <unit_id> [--dry-run] [--no-install]

Chain per unit: crop idle ref -> image-to-3d -> remesh 30k -> rig (walk+run GLBs
free) -> animate needed actions -> Blender render (yaw 180, pelvis-pinned,
ortho 2.4) -> pack 7x7 sheets -> back up + overwrite the unit's PNGs in place.

Meta GUIDs are preserved (filename replacement), so no Unity rewiring.
Held props (rifles/shields) are dropped by Meshy's a-pose step -- known follow-up.
"""

import argparse
import os
import re
import subprocess
import sys
import time

import meshy_client as mc
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(HERE, "..", ".."))
ANIM = os.path.join(REPO, "Assets", "_Project", "Art", "Combat2D",
                    "Units", "Animations")
BLENDER = r"C:\Program Files\Blender Foundation\Blender 4.4\blender.exe"
STATES = ["idle", "walk", "run", "hurt", "hitReact", "shoot", "die"]

# clip -> how to source its GLB. ('anim', id) = Meshy animation action;
# ('rig', key) = a GLB that ships free in the rigging task result.
CLIP_SOURCE = {
    # 11 = Idle_02: TRUE standing idle (~2.33s). Action 0 "Idle" is a
    # marching-in-place loop -- wrong for stationary units in this game.
    "idle": ("anim", 11),
    "shoot": ("anim", 104),      # Side_Shot
    "hurt": ("anim", 177),       # Gunshot_Reaction (stays upright)
    "hitReact": ("anim", 178),   # Hit_Reaction
    "die": ("anim", 180),        # Shot_and_Fall
    "walk": ("rig", "walking"),
    "run": ("rig", "running"),
}
LOOP_CLIPS = {"idle", "walk", "run"}
# anim_set state name -> PNG filename suffix (only where they differ)
FILE_SUFFIX = {"hitReact": "hit_react"}


def log(msg):
    print(f"[{time.strftime('%H:%M:%S')}] {msg}", flush=True)


def populated_clips(unit):
    asset = os.path.join(ANIM, unit, f"{unit}_anim_set.asset")
    txt = open(asset).read()
    out = []
    for st in STATES:
        m = re.search(rf"^  {st}:\s*\n    sheet:\s*\{{fileID:\s*(\d+)",
                      txt, re.M)
        if m and m.group(1) != "0":
            out.append(st)
    return out


def crop_ref(unit, work):
    src = os.path.join(ANIM, unit, f"{unit}_idle.png")
    img = Image.open(src).convert("RGBA")
    cell = img.size[0] // 7
    ref = img.crop((0, 0, cell, cell))
    path = os.path.join(work, "ref.png")
    ref.save(path)
    return path


def poll(kind, task_id, timeout_min=40):
    deadline = time.time() + timeout_min * 60
    while True:
        task = mc.get_task(kind, task_id)
        status = task.get("status")
        if status == "SUCCEEDED":
            return task
        if status in ("FAILED", "CANCELED"):
            raise RuntimeError(f"{kind} {task_id} -> {status}: "
                               f"{task.get('task_error')}")
        if time.time() > deadline:
            raise RuntimeError(f"{kind} {task_id} timed out")
        time.sleep(15)


def download(url, dest):
    import urllib.request
    urllib.request.urlretrieve(url, dest)


def run_meshy_chain(unit, clips, work):
    """Returns {clip: glb_path} for every requested clip."""
    ref = crop_ref(unit, work)
    log(f"{unit}: image-to-3d")
    body = {"image_url": mc.image_to_data_uri(ref), "ai_model": "latest",
            "should_texture": True, "enable_pbr": True, "topology": "triangle",
            "target_polycount": 30000, "pose_mode": "a-pose",
            "target_formats": ["glb"]}
    img_id = mc.request("POST", "image-to-3d", body)["result"]
    poll("image3d", img_id)

    log(f"{unit}: remesh -> 30k")
    rem_id = mc.request("POST", "remesh", {"input_task_id": img_id,
                        "target_formats": ["glb"], "topology": "triangle",
                        "target_polycount": 30000})["result"]
    poll("remesh", rem_id)

    log(f"{unit}: rig")
    rig_id = mc.request("POST", "rigging", {"input_task_id": rem_id,
                        "height_meters": 1.75})["result"]
    # Persist task ids: rig_id is needed again for any later animation swap
    # (recovering it from API listings by timestamp is error-prone).
    import json
    with open(os.path.join(work, "task_ids.json"), "w") as f:
        json.dump({"image3d": img_id, "remesh": rem_id, "rig": rig_id}, f)
    rig_task = poll("rig", rig_id)
    rig_urls = mc.collect_urls(rig_task.get("result", {}))

    glbs = {}
    glb_dir = os.path.join(work, "glb")
    os.makedirs(glb_dir, exist_ok=True)

    for clip in clips:
        kind, spec = CLIP_SOURCE[clip]
        dest = os.path.join(glb_dir, f"{clip}.glb")
        if kind == "rig":
            key = f"basic_animations.{spec}_glb_url"
            url = rig_urls.get(key) or rig_urls.get(f"{spec}_glb_url")
            if not url:
                log(f"  WARN {unit}/{clip}: no {spec} url in rig result")
                continue
            download(url, dest)
        else:
            log(f"{unit}: animate {clip} (action {spec})")
            an_id = mc.request("POST", "animations",
                               {"rig_task_id": rig_id, "action_id": spec})["result"]
            an_task = poll("anim", an_id)
            url = mc.collect_urls(an_task.get("result", {})).get("animation_glb_url")
            if not url:
                log(f"  WARN {unit}/{clip}: no animation_glb_url")
                continue
            download(url, dest)
        glbs[clip] = dest
    return glbs


def render_and_install(unit, glbs, work, install):
    for clip, glb in glbs.items():
        frames = os.path.join(work, "frames", clip)
        cmd = [BLENDER, "--background", "--python",
               os.path.join(HERE, "render_frames.py"), "--",
               "--glb", glb, "--out", frames, "--yaw", "180",
               "--ortho", "2.4", "--cam-height", "0.95", "--frames", "49"]
        if clip in LOOP_CLIPS:
            cmd.append("--loop")
        subprocess.run(cmd, check=True, capture_output=True)

        suffix = FILE_SUFFIX.get(clip, clip)
        sheet = os.path.join(work, "sheets", f"{unit}_{suffix}.png")
        subprocess.run([sys.executable, os.path.join(HERE, "pack_sheet.py"),
                        "--frames", frames, "--out", sheet], check=True)

        if install:
            dst = os.path.join(ANIM, unit, f"{unit}_{suffix}.png")
            backup = os.path.join(HERE, "backup", unit)
            os.makedirs(backup, exist_ok=True)
            if os.path.isfile(dst):
                import shutil
                shutil.copy2(dst, os.path.join(backup, os.path.basename(dst)))
                shutil.copy2(sheet, dst)
        log(f"  {unit}/{clip} -> {os.path.basename(sheet)}"
            f"{' [installed]' if install else ''}")


def main():
    p = argparse.ArgumentParser()
    p.add_argument("unit")
    p.add_argument("--no-install", action="store_true")
    p.add_argument("--dry-run", action="store_true")
    args = p.parse_args()

    clips = populated_clips(args.unit)
    log(f"{args.unit}: clips = {clips}")
    if args.dry_run:
        return
    work = os.path.join(HERE, "units", args.unit)
    os.makedirs(work, exist_ok=True)
    glbs = run_meshy_chain(args.unit, clips, work)
    render_and_install(args.unit, glbs, work, install=not args.no_install)
    log(f"{args.unit}: DONE ({len(glbs)}/{len(clips)} clips)")


if __name__ == "__main__":
    main()
