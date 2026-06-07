"""
DeadManZone — Armored Transport from Meshy tank source
Decimates the high-poly Meshy export, saves a game-ready .blend, and renders the shop icon.

Run in Blender: Scripting → Open → Run Script
CLI: blender --background --python prepare_armored_transport_from_meshy.py
"""
import bpy
import math
import os
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ART_NEUTRAL = os.path.normpath(os.path.join(SCRIPT_DIR, ".."))

SOURCE_BLEND = os.path.join(SCRIPT_DIR, "armored_transport_meshy_source.blend")
OUTPUT_BLEND = os.path.join(SCRIPT_DIR, "armored_transport.blend")
OUTPUT_ICON = os.path.join(ART_NEUTRAL, "Renders", "Icons", "armored_transport_icon.png")

# Target ~8–12k tris for neutral vehicles (art spec)
TARGET_TRIS = 11_000
MESH_OBJECT_NAME = "mesh_node"
# Front-facing 3/4: camera opposite default Meshy export orientation.
ICON_CAMERA_AZIMUTH_DEG = 225.0


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for block in list(bpy.data.meshes):
        if block.users == 0:
            bpy.data.meshes.remove(block)


def load_tank_mesh():
    if not os.path.isfile(SOURCE_BLEND):
        raise FileNotFoundError(
            f"Missing source blend. Copy Meshy export to:\n  {SOURCE_BLEND}"
        )

    bpy.ops.wm.open_mainfile(filepath=SOURCE_BLEND)
    obj = bpy.data.objects.get(MESH_OBJECT_NAME)
    if obj is None:
        meshes = [o for o in bpy.data.objects if o.type == "MESH"]
        if not meshes:
            raise RuntimeError("No mesh objects found in source blend.")
        obj = meshes[0]

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    obj.name = "ArmoredTransport"

    bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY", center="BOUNDS")
    obj.location = (0.0, 0.0, 0.0)
    max_dim = max(obj.dimensions)
    if max_dim > 0:
        scale = 1.8 / max_dim
        obj.scale = (scale, scale, scale)
        bpy.ops.object.transform_apply(scale=True)

    return obj


def decimate_to_target(obj, target_tris=TARGET_TRIS):
    current_tris = sum(len(p.vertices) for p in obj.data.polygons)
    if current_tris <= target_tris:
        print(f"Mesh already within budget: {current_tris} tris")
        return current_tris

    ratio = max(0.001, min(1.0, target_tris / current_tris))
    mod = obj.modifiers.new("DecimateForGame", "DECIMATE")
    mod.ratio = ratio
    bpy.ops.object.modifier_apply(modifier=mod.name)

    final_tris = sum(len(p.vertices) for p in obj.data.polygons)
    print(f"Decimated {current_tris} -> {final_tris} tris (ratio={ratio:.4f})")
    return final_tris


def setup_render_scene():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.resolution_x = 256
    scene.render.resolution_y = 256

    world = scene.world or bpy.data.worlds.new("NeutralWorld")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs["Color"].default_value = (0.08, 0.09, 0.11, 1.0)
        bg.inputs["Strength"].default_value = 0.35

    bpy.ops.object.light_add(type="SUN", location=(3.0, -2.0, 5.0))
    key = bpy.context.active_object
    key.name = "KeyLight"
    key.data.energy = 2.5
    key.rotation_euler = (math.radians(50), math.radians(8), math.radians(25))

    bpy.ops.object.light_add(type="AREA", location=(-2.5, 2.0, 2.5))
    fill = bpy.context.active_object
    fill.name = "FillLight"
    fill.data.energy = 85.0
    fill.data.size = 2.5
    fill.rotation_euler = (math.radians(60), 0.0, math.radians(-140))

    bpy.ops.object.camera_add()
    cam = bpy.context.active_object
    cam.name = "RenderCamera"
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 3.2

    elev = math.radians(35)
    az = math.radians(ICON_CAMERA_AZIMUTH_DEG)
    dist = 8.0
    target = Vector((0.0, 0.0, 0.5))
    cam.location = Vector(
        (
            dist * math.cos(elev) * math.cos(az),
            dist * math.cos(elev) * math.sin(az),
            dist * math.sin(elev),
        )
    )
    direction = target - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam


def render_icon():
    os.makedirs(os.path.dirname(OUTPUT_ICON), exist_ok=True)
    bpy.context.scene.render.filepath = OUTPUT_ICON
    bpy.ops.render.render(write_still=True)
    print(f"Rendered shop icon to: {OUTPUT_ICON}")


def main():
    clear_scene()
    load_tank_mesh()
    tank = bpy.context.active_object
    decimate_to_target(tank)
    setup_render_scene()
    bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_BLEND)
    print(f"Saved game-ready blend to: {OUTPUT_BLEND}")
    render_icon()


if __name__ == "__main__":
    main()
