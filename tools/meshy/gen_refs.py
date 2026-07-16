"""
DeadManZone - batch comic-noir ref generation via Meshy Text-to-Image.

Reads tools/meshy/refs_prompts.json (piece_id -> locked-template prompt), creates
one Text to Image task per piece per variant, polls, and downloads results to:

  tools/meshy/units/<piece_id>/refs/candidates/<piece_id>_v<N>.png

COSTS MESHY CREDITS: nano-banana-2 = 6 credits per image (default 2 variants
per piece => 12 credits/piece; 13 pieces => ~156 credits). Use --dry-run first.

Characters get pose_mode=a-pose + aspect 3:4. Non-humanoid pieces (see
NON_HUMANOID) get 1:1 and no pose preset.

State: refs_state.json beside this script — rerunning skips SUCCEEDED
downloads, so an interrupted run just resumes.

Usage:
  python gen_refs.py [--pieces id1,id2] [--variants 2]
                     [--model nano-banana-2] [--dry-run]

After it finishes, gate every candidate with:
  python ../refcheck.py units/<piece_id>/refs/candidates/<file>.png
"""

import argparse
import json
import os
import sys
import time
import urllib.request

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, SCRIPT_DIR)
import meshy_client as mc  # noqa: E402

PROMPTS_PATH = os.path.join(SCRIPT_DIR, "refs_prompts.json")
STATE_PATH = os.path.join(SCRIPT_DIR, "refs_state.json")
UNITS_DIR = os.path.join(SCRIPT_DIR, "units")

NON_HUMANOID = {"machine_gun_nest", "trench_works", "breakthrough_tank",
                "grand_battery"}
POLL_SECONDS = 6


def load_json(path, default):
    if os.path.exists(path):
        with open(path, encoding="utf-8") as f:
            return json.load(f)
    return default


def save_state(state):
    with open(STATE_PATH, "w", encoding="utf-8") as f:
        json.dump(state, f, indent=2)


def create_task(model, prompt, piece):
    body = {"ai_model": model, "prompt": prompt}
    if piece in NON_HUMANOID:
        body["aspect_ratio"] = "1:1"
    else:
        body["aspect_ratio"] = "3:4"
        body["pose_mode"] = "a-pose"
    resp = mc.request("POST", "text-to-image", body)
    return resp["result"]


def poll(task_id):
    while True:
        task = mc.request("GET", f"text-to-image/{task_id}")
        status = task.get("status")
        if status == "SUCCEEDED":
            return task
        if status in ("FAILED", "CANCELED"):
            sys.exit(f"task {task_id} {status}: "
                     f"{task.get('task_error', {}).get('message', '?')}")
        print(f"  {task_id} {status} {task.get('progress', 0)}%")
        time.sleep(POLL_SECONDS)


def download(url, dest):
    os.makedirs(os.path.dirname(dest), exist_ok=True)
    with urllib.request.urlopen(url, timeout=120) as r, open(dest, "wb") as f:
        f.write(r.read())
    print(f"  wrote {os.path.relpath(dest, SCRIPT_DIR)}")


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--pieces", help="comma-separated piece ids (default: all)")
    p.add_argument("--variants", type=int, default=2)
    p.add_argument("--model", default="nano-banana-2",
                   choices=["nano-banana", "nano-banana-2",
                            "nano-banana-pro", "gpt-image-2"])
    p.add_argument("--dry-run", action="store_true")
    args = p.parse_args()

    prompts = {k: v for k, v in load_json(PROMPTS_PATH, {}).items()
               if not k.startswith("_")}
    if not prompts:
        sys.exit(f"no prompts in {PROMPTS_PATH}")
    if args.pieces:
        wanted = [s.strip() for s in args.pieces.split(",")]
        missing = [w for w in wanted if w not in prompts]
        if missing:
            sys.exit(f"unknown piece ids: {missing}")
        prompts = {k: prompts[k] for k in wanted}

    state = load_json(STATE_PATH, {})
    jobs = [(piece, v) for piece in prompts for v in range(1, args.variants + 1)]
    print(f"{len(jobs)} images x {args.model} "
          f"({ {'nano-banana': 3, 'nano-banana-2': 6, 'nano-banana-pro': 9, 'gpt-image-2': 9}[args.model] } credits each)")
    if args.dry_run:
        for piece, v in jobs:
            kind = "structure/vehicle 1:1" if piece in NON_HUMANOID \
                else "character 3:4 a-pose"
            print(f"  would create: {piece}_v{v} ({kind})")
        return

    # Phase 1: create all tasks (parallel cook)
    for piece, v in jobs:
        key = f"{piece}_v{v}"
        if state.get(key, {}).get("done"):
            print(f"[skip] {key} already downloaded")
            continue
        if not state.get(key, {}).get("task_id"):
            task_id = create_task(args.model, prompts[piece], piece)
            state[key] = {"task_id": task_id, "done": False}
            save_state(state)
            print(f"[create] {key} -> {task_id}")

    # Phase 2: poll + download
    for piece, v in jobs:
        key = f"{piece}_v{v}"
        entry = state.get(key, {})
        if entry.get("done"):
            continue
        print(f"[wait] {key}")
        task = poll(entry["task_id"])
        urls = task.get("image_urls") or []
        if not urls:
            sys.exit(f"{key}: SUCCEEDED but no image_urls: {task}")
        dest = os.path.join(UNITS_DIR, piece, "refs", "candidates",
                            f"{key}.png")
        download(urls[0], dest)
        entry["done"] = True
        state[key] = entry
        save_state(state)

    print("\nAll refs downloaded. Next: refcheck gate, e.g.")
    print("  python tools/refcheck.py "
          "tools/meshy/units/<piece>/refs/candidates/<file>.png")


if __name__ == "__main__":
    main()
