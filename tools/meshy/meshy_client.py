"""
DeadManZone - Meshy API client (stdlib only, no pip deps).

Wraps the three Meshy endpoints used by the 3D->sprite-sheet unit pipeline:
  image-to-3d  : reference image -> textured 3D model
  rigging      : model -> rigged humanoid (+ free walking/running animations)
  animations   : rigged model + numeric action_id -> animated GLB/FBX

Auth: MESHY_API_KEY env var (Bearer token).

Usage:
  python meshy_client.py image3d --image ref.png [--no-pbr] [--polycount 30000]
  python meshy_client.py rig <image3d_task_id> [--height 1.75]
  python meshy_client.py animate <rig_task_id> <action_id>
  python meshy_client.py status <kind> <task_id>        # kind: image3d|rig|anim
  python meshy_client.py wait <kind> <task_id>          # poll until terminal state
  python meshy_client.py download <kind> <task_id> --out <dir>

Action IDs (Meshy animation library): 0=Idle, 104=Side_Shot, 180-190=Shot_and_Fall
variants, 8=Dead. Walking/running GLBs come free in the rigging task result.
"""

import argparse
import base64
import json
import mimetypes
import os
import sys
import time
import urllib.error
import urllib.request

BASE = "https://api.meshy.ai/openapi/v1"
KINDS = {
    "image3d": "image-to-3d",
    "rig": "rigging",
    "anim": "animations",
    "remesh": "remesh",
}
TERMINAL = {"SUCCEEDED", "FAILED", "CANCELED"}


def api_key():
    key = os.environ.get("MESHY_API_KEY", "")
    if not key:
        sys.exit("MESHY_API_KEY is not set")
    return key


def request(method, path, body=None):
    url = f"{BASE}/{path}"
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", f"Bearer {api_key()}")
    if data:
        req.add_header("Content-Type", "application/json")
    try:
        with urllib.request.urlopen(req, timeout=120) as resp:
            return json.loads(resp.read().decode())
    except urllib.error.HTTPError as e:
        detail = e.read().decode(errors="replace")
        sys.exit(f"HTTP {e.code} on {method} {url}\n{detail}")


def image_to_data_uri(path):
    mime = mimetypes.guess_type(path)[0] or "image/png"
    with open(path, "rb") as f:
        b64 = base64.b64encode(f.read()).decode()
    return f"data:{mime};base64,{b64}"


def cmd_image3d(args):
    body = {
        "image_url": image_to_data_uri(args.image),
        "ai_model": "latest",
        "should_texture": True,
        "enable_pbr": not args.no_pbr,
        "topology": "triangle",
        "target_polycount": args.polycount,
        "pose_mode": "a-pose",
        "target_formats": ["glb", "fbx"],
    }
    task_id = request("POST", "image-to-3d", body)["result"]
    print(task_id)


def cmd_remesh(args):
    body = {
        "input_task_id": args.task_id,
        "target_formats": ["glb"],
        "topology": "triangle",
        "target_polycount": args.polycount,
    }
    task_id = request("POST", "remesh", body)["result"]
    print(task_id)


def cmd_rig(args):
    body = {"input_task_id": args.task_id, "height_meters": args.height}
    task_id = request("POST", "rigging", body)["result"]
    print(task_id)


def cmd_animate(args):
    body = {"rig_task_id": args.rig_task_id, "action_id": args.action_id}
    task_id = request("POST", "animations", body)["result"]
    print(task_id)


def get_task(kind, task_id):
    return request("GET", f"{KINDS[kind]}/{task_id}")


def cmd_status(args):
    print(json.dumps(get_task(args.kind, args.task_id), indent=2))


def cmd_wait(args):
    deadline = time.time() + args.timeout_minutes * 60
    while True:
        task = get_task(args.kind, args.task_id)
        status = task.get("status", "?")
        progress = task.get("progress", "?")
        print(f"{time.strftime('%H:%M:%S')} {args.kind} {args.task_id}: "
              f"{status} ({progress}%)", flush=True)
        if status in TERMINAL:
            if status != "SUCCEEDED":
                print(json.dumps(task, indent=2))
                sys.exit(f"Task ended {status}")
            return
        if time.time() > deadline:
            sys.exit("Timed out waiting for task")
        time.sleep(args.interval)


def collect_urls(obj, prefix=""):
    """Recursively find every http(s) URL field in the task result."""
    found = {}
    if isinstance(obj, dict):
        for k, v in obj.items():
            found.update(collect_urls(v, f"{prefix}{k}."))
    elif isinstance(obj, str) and obj.startswith("http"):
        found[prefix.rstrip(".")] = obj
    return found


def cmd_download(args):
    task = get_task(args.kind, args.task_id)
    if task.get("status") != "SUCCEEDED":
        sys.exit(f"Task status is {task.get('status')}, not SUCCEEDED")
    urls = collect_urls(task.get("model_urls", {}), "model_urls.")
    urls.update(collect_urls(task.get("result", {}), "result."))
    if args.filter:
        urls = {k: v for k, v in urls.items() if args.filter in k}
    if not urls:
        sys.exit("No downloadable URLs found (after filter)")
    os.makedirs(args.out, exist_ok=True)
    for name, url in urls.items():
        ext = url.split("?")[0].rsplit(".", 1)[-1]
        safe = name.replace("model_urls.", "").replace("result.", "")
        safe = safe.replace(".", "_").replace("_url", "")
        dest = os.path.join(args.out, f"{args.prefix}{safe}.{ext}")
        print(f"downloading {name} -> {dest}", flush=True)
        urllib.request.urlretrieve(url, dest)
    print("done")


def main():
    p = argparse.ArgumentParser(description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="cmd", required=True)

    s = sub.add_parser("image3d")
    s.add_argument("--image", required=True)
    s.add_argument("--no-pbr", action="store_true")
    s.add_argument("--polycount", type=int, default=30000)
    s.set_defaults(fn=cmd_image3d)

    s = sub.add_parser("remesh")
    s.add_argument("task_id")
    s.add_argument("--polycount", type=int, default=30000)
    s.set_defaults(fn=cmd_remesh)

    s = sub.add_parser("rig")
    s.add_argument("task_id")
    s.add_argument("--height", type=float, default=1.75)
    s.set_defaults(fn=cmd_rig)

    s = sub.add_parser("animate")
    s.add_argument("rig_task_id")
    s.add_argument("action_id", type=int)
    s.set_defaults(fn=cmd_animate)

    for name, fn in (("status", cmd_status), ("wait", cmd_wait),
                     ("download", cmd_download)):
        s = sub.add_parser(name)
        s.add_argument("kind", choices=KINDS)
        s.add_argument("task_id")
        if name == "wait":
            s.add_argument("--interval", type=int, default=15)
            s.add_argument("--timeout-minutes", type=int, default=30)
        if name == "download":
            s.add_argument("--out", required=True)
            s.add_argument("--filter", default="")
            s.add_argument("--prefix", default="")
        s.set_defaults(fn=fn)

    args = p.parse_args()
    args.fn(args)


if __name__ == "__main__":
    main()
