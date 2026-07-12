"""
DeadManZone - one-command Meshy unit pipeline (stdlib only, wraps meshy_client.py).

Runs the proven chain from a reference image to scene-ready GLBs:

  image3d(ref) -> remesh(--polycount) -> rig(--height)
      -> animate(0=Idle) + animate(8=Dead)   [created together, cook in parallel]
      -> download idle.glb / walk.glb / die.glb into tools/meshy/units/<unit>/glb12k/
      -> copy the three GLBs to Assets/_Project/Combat3D/Models/<unit>/
      -> print the remaining MANUAL Unity steps.

COSTS MESHY CREDITS (~30-40 per unit across image3d/remesh/rig/2x animate).
Use --dry-run to walk the whole chain without spending anything or touching disk.

State: tools/meshy/units/<unit>/pipeline_state.json is rewritten after every
stage. Task ids are printed as they are created - they double as resume keys.
After an interruption just rerun the same command (state file is enough), or
inject a known id with --resume <stage:task_id>
(stage: image3d|remesh|rig|anim_idle|anim_die).

Usage:
  python generate_unit.py <unit_name> [--ref path] [--polycount 12000]
         [--height 1.8] [--resume stage:task_id] [--dry-run] [--no-unity-copy]

Default ref: tools/meshy/units/<unit>/ref.png
"""

import argparse
import difflib
import json
import os
import re
import shutil
import sys
import time
import urllib.request

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, SCRIPT_DIR)
import meshy_client as mc  # noqa: E402  (stdlib-only sibling module)

REPO_ROOT = os.path.dirname(os.path.dirname(SCRIPT_DIR))
PIECES_DIR = os.path.join(REPO_ROOT, "Assets", "_Project", "Data", "Resources",
                          "DeadManZone", "Pieces")
UNITY_MODELS_DIR = os.path.join(REPO_ROOT, "Assets", "_Project", "Combat3D", "Models")
BOOTSTRAP_CS = ("Assets/_Project/Presentation/Editor/"
                "Combat3DDemoSceneBootstrap.cs")

ACTION_IDLE = 0   # Meshy animation library
ACTION_DEAD = 8
TASK_STAGES = ("image3d", "remesh", "rig", "anim_idle", "anim_die")
STAGE_KIND = {"image3d": "image3d", "remesh": "remesh", "rig": "rig",
              "anim_idle": "anim", "anim_die": "anim"}
UPSTREAM = {
    "image3d": (),
    "remesh": ("image3d",),
    "rig": ("image3d", "remesh"),
    "anim_idle": ("image3d", "remesh", "rig"),
    "anim_die": ("image3d", "remesh", "rig"),
}
FINAL_GLBS = ("idle.glb", "walk.glb", "die.glb")
# --vehicle mode (tanks/transports/emplacements): no humanoid rig, no animations —
# a single static mesh; motion/death are code-driven in CombatUnitVisual3D's
# vehicle mode. Named model.glb so the Unity bootstrap can tell the modes apart.
VEHICLE_GLBS = ("model.glb",)


# ---------------------------------------------------------------- state file

def state_path(unit_dir):
    return os.path.join(unit_dir, "pipeline_state.json")


def load_state(unit_dir):
    path = state_path(unit_dir)
    if os.path.exists(path):
        with open(path, encoding="utf-8") as f:
            return json.load(f)
    return {"stages": {}}


def save_state(unit_dir, state, dry):
    if dry:
        return  # --dry-run is fully side-effect free
    state["updated"] = time.strftime("%Y-%m-%dT%H:%M:%S")
    os.makedirs(unit_dir, exist_ok=True)
    with open(state_path(unit_dir), "w", encoding="utf-8") as f:
        json.dump(state, f, indent=2)


# ------------------------------------------------------------- Meshy tasks

def create_image3d(ref, polycount):
    body = {
        "image_url": mc.image_to_data_uri(ref),
        "ai_model": "latest",
        "should_texture": True,
        "enable_pbr": True,
        "topology": "triangle",
        # NOTE: ignored at generation (raw gens land ~500k faces);
        # the remesh stage is what actually enforces the budget.
        "target_polycount": polycount,
        "pose_mode": "a-pose",
        "target_formats": ["glb", "fbx"],
    }
    return mc.request("POST", "image-to-3d", body)["result"]


def create_remesh(image3d_id, polycount):
    body = {
        "input_task_id": image3d_id,
        "target_formats": ["glb"],
        "topology": "triangle",
        "target_polycount": polycount,
    }
    return mc.request("POST", "remesh", body)["result"]


def create_rig(remesh_id, height):
    body = {"input_task_id": remesh_id, "height_meters": height}
    return mc.request("POST", "rigging", body)["result"]


def create_animate(rig_id, action_id):
    body = {"rig_task_id": rig_id, "action_id": action_id}
    return mc.request("POST", "animations", body)["result"]


def wait_task(kind, task_id, timeout_minutes=45, interval=15):
    deadline = time.time() + timeout_minutes * 60
    while True:
        task = mc.get_task(kind, task_id)
        status = task.get("status", "?")
        print(f"  {time.strftime('%H:%M:%S')} {kind} {task_id}: "
              f"{status} ({task.get('progress', '?')}%)", flush=True)
        if status in mc.TERMINAL:
            if status != "SUCCEEDED":
                print(json.dumps(task, indent=2))
                sys.exit(f"{kind} task {task_id} ended {status} - "
                         "fix the input and rerun (state file keeps prior ids)")
            return task
        if time.time() > deadline:
            sys.exit(f"Timed out waiting for {kind} {task_id} - rerun the same "
                     "command later; pipeline_state.json keeps the id")
        time.sleep(interval)


def require_id(task_id, stage):
    if not task_id:
        sys.exit(f"{stage} task id missing from pipeline_state.json - rerun with "
                 f"--resume {stage}:<task_id> (ids are printed at creation and "
                 "logged in docs/meshy-roster-jobs-*.md / the Meshy dashboard)")
    return task_id


def ensure_task(state, unit_dir, stage, create_fn, dry):
    """Create the stage's task if it doesn't exist yet. Returns task id."""
    rec = state["stages"].get(stage, {})
    if rec.get("task_id"):
        label = "done" if rec.get("status") == "SUCCEEDED" else "in flight"
        print(f"[{stage}] existing task {rec['task_id']} ({label})")
        return rec["task_id"]
    if rec.get("status") == "SUCCEEDED":
        # marked done by --resume without an id - never re-create (re-pay) it
        print(f"[{stage}] assumed done via --resume (task id unknown) - skipping")
        return ""
    if dry:
        task_id = f"DRY-{stage}"
        print(f"[{stage}] DRY RUN: would POST a paid {STAGE_KIND[stage]} task")
    else:
        task_id = create_fn()
        print(f"[{stage}] created {STAGE_KIND[stage]} task {task_id}   <- resume key")
    state["stages"][stage] = {"task_id": task_id, "status": "PENDING"}
    save_state(unit_dir, state, dry)
    return task_id


def finish_task(state, unit_dir, stage, dry):
    """Poll the stage's task to SUCCEEDED and record it."""
    rec = state["stages"][stage]
    if rec.get("status") == "SUCCEEDED":
        return
    if dry:
        print(f"[{stage}] DRY RUN: would poll {STAGE_KIND[stage]}/"
              f"{rec['task_id']} every 15s until SUCCEEDED")
    else:
        wait_task(STAGE_KIND[stage], rec["task_id"])
    rec["status"] = "SUCCEEDED"
    save_state(unit_dir, state, dry)


# ---------------------------------------------------------------- downloads

def pick_url(task, want, reject=()):
    """Pick the one result URL whose key contains all `want` fragments."""
    urls = mc.collect_urls(task.get("model_urls", {}), "model_urls.")
    urls.update(mc.collect_urls(task.get("result", {}), "result."))
    for name, url in sorted(urls.items()):
        low = name.lower()
        if all(w in low for w in want) and not any(r in low for r in reject):
            return url
    sys.exit(f"No task URL matching {want} (rejecting {reject}); "
             f"available keys: {sorted(urls)}")


def download_outputs(state, unit_dir, out_dir, dry, vehicle=False):
    final = VEHICLE_GLBS if vehicle else FINAL_GLBS
    if state["stages"].get("download", {}).get("status") == "SUCCEEDED" and \
            all(os.path.exists(os.path.join(out_dir, f)) for f in final):
        print("[download] already complete - skipping")
        return
    stages = state["stages"]
    # Both anim tasks expose the SAME remote filename (animation_glb.glb) - the
    # manual flow collided when downloading them into one dir. Avoided here by
    # streaming each URL straight to its final local name, sequentially.
    if vehicle:
        targets = [
            ("model.glb", "remesh",
             require_id(stages.get("remesh", {}).get("task_id"), "remesh"),
             ("glb",), ("fbx", "usdz", "obj", "mtl")),
        ]
    else:
        targets = [
            ("idle.glb", "anim",
             require_id(stages.get("anim_idle", {}).get("task_id"), "anim_idle"),
             ("animation", "glb"), ("fbx",)),
            ("die.glb", "anim",
             require_id(stages.get("anim_die", {}).get("task_id"), "anim_die"),
             ("animation", "glb"), ("fbx",)),
            # walk comes free with the rig task; real key is
            # result.basic_animations.walking_glb_url (dot-separated)
            ("walk.glb", "rig",
             require_id(stages.get("rig", {}).get("task_id"), "rig"),
             ("walking", "glb"), ("armature", "fbx", "running")),
        ]
    if not dry:
        os.makedirs(out_dir, exist_ok=True)
    for fname, kind, task_id, want, reject in targets:
        dest = os.path.join(out_dir, fname)
        if dry:
            print(f"[download] DRY RUN: would fetch {kind}/{task_id} "
                  f"url with key matching {want} -> {dest}")
            continue
        task = mc.get_task(kind, task_id)
        if task.get("status") != "SUCCEEDED":
            sys.exit(f"{kind} {task_id} is {task.get('status')}, not SUCCEEDED")
        url = pick_url(task, want, reject)
        print(f"[download] {fname} <- {kind} {task_id}", flush=True)
        urllib.request.urlretrieve(url, dest)
    # prune extras left behind by older manual runs (fbx/running/armature/etc.)
    if not dry:
        junk_markers = (".fbx", "armature", "running", "animation_glb",
                        "rigged_character", "basic_animations")
        for entry in os.listdir(out_dir):
            low = entry.lower()
            if entry in final or not os.path.isfile(os.path.join(out_dir, entry)):
                continue
            if any(m in low for m in junk_markers):
                os.remove(os.path.join(out_dir, entry))
                print(f"[download] pruned extra {entry}")
    state["stages"]["download"] = {"status": "SUCCEEDED", "files": list(final)}
    save_state(unit_dir, state, dry)


def copy_to_unity(state, unit_dir, unit, out_dir, dry, vehicle=False):
    final = VEHICLE_GLBS if vehicle else FINAL_GLBS
    dest_dir = os.path.join(UNITY_MODELS_DIR, unit)
    if dry:
        print(f"[unity] DRY RUN: would copy {', '.join(final)} -> {dest_dir}")
        return
    os.makedirs(dest_dir, exist_ok=True)
    for fname in final:
        shutil.copy2(os.path.join(out_dir, fname), os.path.join(dest_dir, fname))
        print(f"[unity] copied {fname} -> {dest_dir}")
    state["stages"]["unity_copy"] = {"status": "SUCCEEDED", "dest": dest_dir}
    save_state(unit_dir, state, dry)


# ------------------------------------------------------------ manual steps

def resolve_piece_id(unit):
    """The piece id must exist in ContentDatabase; the piece .asset is the proxy."""
    if os.path.exists(os.path.join(PIECES_DIR, unit + ".asset")):
        return unit, True
    existing = sorted(os.path.splitext(f)[0] for f in os.listdir(PIECES_DIR)
                      if f.endswith(".asset"))
    close = difflib.get_close_matches(unit, existing, n=3, cutoff=0.4)
    print(f"\nWARNING: no piece asset '{unit}.asset' under "
          f"Assets/_Project/Data/Resources/DeadManZone/Pieces/ - the piece id "
          "must exist in ContentDatabase or the archetype will fall back to "
          "rifleman visuals.")
    if close:
        print(f"         Closest existing piece ids: {', '.join(close)} "
              "(the model can be worn by one of these, like grenade_thrower -> "
              "ironclad_mortars was).")
    return (close[0] if close else "<piece_id>"), False


def print_manual_checklist(unit, vehicle=False):
    piece_id, exact = resolve_piece_id(unit)
    note = "" if exact else "   # piece id guessed - VERIFY (see warning above)"
    if vehicle:
        print(f"""
================ REMAINING MANUAL STEPS (Unity, vehicle) ================
1. Add the VEHICLE archetype mapping to VehicleUnits in
   {BOOTSTRAP_CS}:
       ("{unit}", "{piece_id}"),{note}
2. In Unity: assets-refresh, then rebuild BOTH scenes via
   DeadManZone -> Combat3D -> Build Combat3D Demo Scene  and
   DeadManZone -> Combat3D -> Build CombatArena3D Scene (Run Flow).
3. Enter Play mode and verify: proportions/texture sane (per-gen roulette),
   mesh seated on the base ring, movement bob while marching, muzzle flash
   from the built-in weapon, collapse+dissolve on death.
==========================================================================""")
        return
    print(f"""
================ REMAINING MANUAL STEPS (Unity) ================
1. Add the archetype mapping to RosterUnits in
   {BOOTSTRAP_CS}:
       ("{unit}", "{piece_id}"),{note}
2. In Unity: assets-refresh (if the editor was open during the copy), then run
   menu  DeadManZone -> Combat3D -> Build Combat3D Demo Scene
   (generates the looped clips + AnimatorController for the new unit).
3. Put the piece id in a demo roster on Combat3DDemoDriver, enter Play mode,
   and verify: proportions sane, texture color acceptable (per-gen roulette -
   re-run image3d if off), feet planted ON the base ring, rifle attached to
   the right hand, idle/walk/die clips all read.
================================================================""")


# ----------------------------------------------------------------- main

def main():
    p = argparse.ArgumentParser(description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("unit_name")
    p.add_argument("--ref", help="reference image "
                   "(default tools/meshy/units/<unit>/ref.png)")
    p.add_argument("--polycount", type=int, default=12000)
    p.add_argument("--height", type=float, default=1.8)
    p.add_argument("--resume", metavar="STAGE:TASK_ID",
                   help="inject a known task id, e.g. rig:019f5254-...")
    p.add_argument("--dry-run", action="store_true",
                   help="walk the chain with zero API calls / zero writes")
    p.add_argument("--no-unity-copy", action="store_true",
                   help="skip copying GLBs into Assets/_Project/Combat3D/Models/")
    p.add_argument("--vehicle", action="store_true",
                   help="non-humanoid unit (tank/transport/emplacement): "
                        "image3d + remesh only, single static model.glb — "
                        "no rig, no animations (~half the credits)")
    args = p.parse_args()

    unit = args.unit_name
    if not re.fullmatch(r"[A-Za-z0-9_\-]+", unit):
        sys.exit(f"Unit name '{unit}' must be alphanumeric/underscore "
                 "(it becomes folder + piece id)")
    unit_dir = os.path.join(SCRIPT_DIR, "units", unit)
    out_dir = os.path.join(unit_dir, "glb12k")
    ref = args.ref or os.path.join(unit_dir, "ref.png")
    if not os.path.exists(ref):
        sys.exit(f"Reference image not found: {ref}\n"
                 f"Put it at tools/meshy/units/{unit}/ref.png or pass --ref <path>.")

    state = load_state(unit_dir)
    state.setdefault("stages", {})
    state.update({"unit": unit, "ref": os.path.abspath(ref),
                  "polycount": args.polycount, "height": args.height})

    if args.resume:
        stage, _, task_id = args.resume.partition(":")
        if stage not in TASK_STAGES or not task_id:
            sys.exit(f"--resume wants <stage:task_id> with stage one of "
                     f"{'|'.join(TASK_STAGES)}, got '{args.resume}'")
        state["stages"][stage] = {"task_id": task_id, "status": "PENDING"}
        for up in UPSTREAM[stage]:
            rec = state["stages"].setdefault(up, {})
            rec["status"] = "SUCCEEDED"  # keeps any known task_id
        for later in ("download", "unity_copy"):
            state["stages"].pop(later, None)
        print(f"[resume] {stage} = {task_id} "
              f"(upstream stages assumed done: {', '.join(UPSTREAM[stage]) or 'none'})")

    if args.dry_run:
        print(f"DRY RUN for '{unit}' - no API calls, no credits, no file writes.")
    else:
        mc.api_key()  # fail fast before any stage
        print(f"Generating '{unit}' from {ref} "
              f"(polycount {args.polycount}, height {args.height}m).")
        print("NOTE: this creates PAID Meshy tasks (~30-40 credits total). "
              "Ctrl+C within 5s to abort.")
        time.sleep(5)

    dry = args.dry_run
    img_id = ensure_task(state, unit_dir, "image3d",
                         lambda: create_image3d(ref, args.polycount), dry)
    finish_task(state, unit_dir, "image3d", dry)

    remesh_id = ensure_task(state, unit_dir, "remesh",
                            lambda: create_remesh(require_id(img_id, "image3d"),
                                                  args.polycount), dry)
    finish_task(state, unit_dir, "remesh", dry)

    if args.vehicle:
        print("[vehicle] non-humanoid mode: skipping rig + animations "
              "(motion/death are code-driven in Unity).")
    else:
        rig_id = ensure_task(state, unit_dir, "rig",
                             lambda: create_rig(require_id(remesh_id, "remesh"),
                                                args.height), dry)
        finish_task(state, unit_dir, "rig", dry)

        # create both anim tasks up front so they cook in parallel, then wait each
        ensure_task(state, unit_dir, "anim_idle",
                    lambda: create_animate(require_id(rig_id, "rig"), ACTION_IDLE), dry)
        ensure_task(state, unit_dir, "anim_die",
                    lambda: create_animate(require_id(rig_id, "rig"), ACTION_DEAD), dry)
        finish_task(state, unit_dir, "anim_idle", dry)
        finish_task(state, unit_dir, "anim_die", dry)

    download_outputs(state, unit_dir, out_dir, dry, vehicle=args.vehicle)

    if args.no_unity_copy:
        print("[unity] skipped (--no-unity-copy)")
    else:
        copy_to_unity(state, unit_dir, unit, out_dir, dry, vehicle=args.vehicle)

    print_manual_checklist(unit, vehicle=args.vehicle)


if __name__ == "__main__":
    main()
