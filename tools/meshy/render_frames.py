"""
DeadManZone - render an animated GLB (Meshy rig/animation export) to N transparent
frames from a fixed side-profile orthographic camera, for sprite-sheet packing.

Run headless:
  blender --background --python render_frames.py -- \
      --glb anim_idle.glb --out frames/idle --frames 49 --yaw 90 \
      [--res 512] [--ortho 2.3] [--cam-height 0.9] [--single]

--yaw rotates the MODEL around Z (degrees); camera stays fixed on +X looking -X.
--single renders only the middle frame (fast facing/framing check).
Camera, lights, scale are identical across invocations so all states line up.
"""

import argparse
import math
import os
import sys

import bpy


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    p = argparse.ArgumentParser()
    p.add_argument("--glb", required=True)
    p.add_argument("--out", required=True)
    p.add_argument("--frames", type=int, default=49)
    p.add_argument("--yaw", type=float, default=180.0)
    p.add_argument("--res", type=int, default=512)
    p.add_argument("--ortho", type=float, default=2.4)
    p.add_argument("--cam-height", type=float, default=0.95)
    p.add_argument("--single", action="store_true")
    p.add_argument("--loop", action="store_true",
                   help="sample so the last frame != first (looping states)")
    return p.parse_args(argv)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for collection in (bpy.data.meshes, bpy.data.armatures, bpy.data.actions,
                       bpy.data.materials, bpy.data.images, bpy.data.lights,
                       bpy.data.cameras):
        for block in list(collection):
            if block.users == 0:
                collection.remove(block)


def import_glb(path, yaw_degrees):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    imported = [o for o in bpy.data.objects if o not in before]

    root = bpy.data.objects.new("SheetRoot", None)
    bpy.context.scene.collection.objects.link(root)
    for obj in imported:
        if obj.parent is None:
            obj.parent = root
    root.rotation_euler = (0.0, 0.0, math.radians(yaw_degrees))
    return imported, root


def find_pelvis(imported):
    """Locate the armature and its root (parentless) pose bone -- the pelvis.
    Meshy/Mixamo rigs bake locomotion here, so it's the stable anchor to pin."""
    for obj in imported:
        if obj.type != "ARMATURE":
            continue
        roots = [b for b in obj.pose.bones if b.parent is None]
        if not roots:
            continue
        named = [b for b in roots
                 if any(k in b.name.lower() for k in ("hip", "pelvis", "root"))]
        return obj, (named[0] if named else roots[0])
    return None, None


def recenter_pelvis(root_empty, armature, pelvis):
    """Pin the pelvis to world Y=0 for this frame so root motion (forward
    translation) doesn't drift the character across the cell -> walk in place."""
    if armature is None or pelvis is None:
        return
    root_empty.location.y = 0.0
    bpy.context.view_layer.update()
    world_y = (armature.matrix_world @ pelvis.head).y
    root_empty.location.y = -world_y
    bpy.context.view_layer.update()


def action_frame_range(imported):
    lo, hi = None, None
    for obj in imported:
        ad = obj.animation_data
        if ad and ad.action:
            a_lo, a_hi = ad.action.frame_range
            lo = a_lo if lo is None else min(lo, a_lo)
            hi = a_hi if hi is None else max(hi, a_hi)
    if lo is None:
        return 1.0, 1.0
    return lo, hi


def setup_render(res, ortho_scale, cam_height):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.resolution_x = res
    scene.render.resolution_y = res

    # Higher-contrast view transform so fine detail (folds, webbing, buckles)
    # survives the downscale to the atlas instead of reading as mud.
    try:
        scene.view_settings.view_transform = "AgX"
        scene.view_settings.look = "AgX - High Contrast"
    except Exception:
        pass
    # Raytraced ambient occlusion deepens creases -> more perceived detail small.
    try:
        scene.eevee.use_raytracing = True
    except Exception:
        pass

    world = scene.world or bpy.data.worlds.new("SheetWorld")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs["Color"].default_value = (0.10, 0.11, 0.13, 1.0)
        bg.inputs["Strength"].default_value = 0.22  # lower ambient = more contrast

    # Strong key gives form; a low fill lifts shadows without flattening;
    # a rim/back light separates the silhouette edge from the battlefield.
    bpy.ops.object.light_add(type="SUN", location=(4.0, -2.5, 5.0))
    key = bpy.context.active_object
    key.data.energy = 4.2
    key.data.angle = math.radians(3.0)  # crisp-ish shadows
    key.rotation_euler = (math.radians(52), math.radians(-18), math.radians(58))

    bpy.ops.object.light_add(type="AREA", location=(3.0, 2.5, 2.0))
    fill = bpy.context.active_object
    fill.data.energy = 70.0
    fill.data.size = 2.5
    fill.rotation_euler = (math.radians(65), 0.0, math.radians(130))

    bpy.ops.object.light_add(type="SUN", location=(-3.0, -1.0, 4.0))
    rim = bpy.context.active_object
    rim.data.energy = 3.0
    rim.data.angle = math.radians(2.0)
    rim.rotation_euler = (math.radians(35), math.radians(20), math.radians(-120))

    bpy.ops.object.camera_add(location=(6.0, 0.0, cam_height))
    cam = bpy.context.active_object
    cam.rotation_euler = (math.radians(90), 0.0, math.radians(90))  # look -X
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = ortho_scale
    scene.camera = cam


def render_frames(out_dir, frame_count, single, loop, root_empty,
                  armature, pelvis):
    scene = bpy.context.scene
    lo, hi = action_frame_range(bpy.data.objects)
    os.makedirs(out_dir, exist_ok=True)

    if single:
        indices = [frame_count // 2]
    else:
        indices = range(frame_count)

    denom = frame_count if loop else max(1, frame_count - 1)
    for i in indices:
        t = i / denom
        scene.frame_set(int(round(lo + t * (hi - lo))))
        recenter_pelvis(root_empty, armature, pelvis)
        scene.render.filepath = os.path.join(out_dir, f"frame_{i:02d}.png")
        bpy.ops.render.render(write_still=True)
    print(f"Rendered {len(list(indices))} frame(s) "
          f"(action range {lo:.0f}-{hi:.0f}) to {out_dir}")


def main():
    args = parse_args()
    clear_scene()
    imported, root_empty = import_glb(os.path.abspath(args.glb), args.yaw)
    if not imported:
        sys.exit("Nothing imported from GLB")
    armature, pelvis = find_pelvis(imported)
    print(f"pelvis anchor: {pelvis.name if pelvis else 'NONE (no recenter)'}")
    setup_render(args.res, args.ortho, args.cam_height)
    render_frames(os.path.abspath(args.out), args.frames, args.single,
                  args.loop, root_empty, armature, pelvis)


if __name__ == "__main__":
    main()
