"""
DeadManZone — Conscript Rifleman generator
Run inside Blender: Scripting workspace → Open → Run Script
Or CLI: blender --python create_conscript_rifleman.py

Output: conscript_rifleman.blend in this folder.
"""
import bpy
import bmesh
import math
import os
from mathutils import Vector, Euler

# Muted neutral palette from art spec
MUD_CANVAS = (0.45, 0.48, 0.42, 1.0)
GUNMETAL = (0.25, 0.26, 0.28, 1.0)
WORN_LEATHER = (0.22, 0.16, 0.11, 1.0)
HELMET_DARK = (0.30, 0.31, 0.28, 1.0)

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ART_NEUTRAL = os.path.normpath(os.path.join(SCRIPT_DIR, ".."))
OUTPUT_BLEND = os.path.join(SCRIPT_DIR, "conscript_rifleman.blend")
OUTPUT_ICON = os.path.join(ART_NEUTRAL, "Renders", "Icons", "conscript_rifleman_icon.png")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for datablock in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for block in list(datablock):
            if block.users == 0:
                datablock.remove(block)


def make_material(name, base_color, roughness=0.72, metallic=0.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Base Color"].default_value = base_color
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    links.new(bsdf.outputs["BSDF"], output.inputs["Surface"])
    return mat


def assign_material(obj, material):
    if obj.data.materials:
        obj.data.materials[0] = material
    else:
        obj.data.materials.append(material)


def make_box(name, location, scale, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.rotation_euler = Euler(rotation, "XYZ")
    return obj


def make_cylinder(name, location, radius, depth, rotation=(0.0, 0.0, 0.0), vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.active_object
    obj.name = name
    return obj


def build_rifleman():
    mats = {
        "canvas": make_material("MUD_CANVAS", MUD_CANVAS),
        "metal": make_material("GUNMETAL", GUNMETAL, roughness=0.45, metallic=0.85),
        "leather": make_material("WORN_LEATHER", WORN_LEATHER, roughness=0.8),
        "helmet": make_material("HELMET_DARK", HELMET_DARK, roughness=0.55, metallic=0.35),
    }

    parts = []

    # Boots and legs — slight forward combat stance
    left_leg = make_box("Leg_L", (0.12, 0.0, 0.18), (0.11, 0.12, 0.22))
    right_leg = make_box("Leg_R", (-0.12, 0.0, 0.18), (0.11, 0.12, 0.22))
    parts.extend([left_leg, right_leg])

    # Pelvis / belt
    pelvis = make_box("Pelvis", (0.0, 0.0, 0.42), (0.22, 0.14, 0.12))
    parts.append(pelvis)

    # Hunched torso
    torso = make_box("Torso", (0.04, 0.0, 0.68), (0.24, 0.16, 0.28), (math.radians(12), 0.0, math.radians(-6)))
    parts.append(torso)

    # Small field pack
    pack = make_box("Pack", (-0.18, -0.08, 0.72), (0.14, 0.1, 0.2), (math.radians(10), 0.0, 0.0))
    assign_material(pack, mats["leather"])
    parts.append(pack)

    # Head
    head = make_box("Head", (0.06, 0.02, 0.98), (0.11, 0.12, 0.12))
    parts.append(head)

    # Brodie-style trench helmet
    helmet_bowl = make_cylinder(
        "Helmet_Bowl",
        (0.06, 0.02, 1.06),
        radius=0.16,
        depth=0.1,
        rotation=(math.radians(90), 0.0, 0.0),
        vertices=16,
    )
    helmet_bowl.scale = (1.0, 0.85, 1.0)
    helmet_brim = make_cylinder(
        "Helmet_Brim",
        (0.06, 0.02, 1.0),
        radius=0.2,
        depth=0.02,
        rotation=(math.radians(90), 0.0, 0.0),
        vertices=16,
    )
    assign_material(helmet_bowl, mats["helmet"])
    assign_material(helmet_brim, mats["helmet"])
    parts.extend([helmet_bowl, helmet_brim])

    # Arms
    left_arm = make_box(
        "Arm_L",
        (0.24, 0.08, 0.72),
        (0.09, 0.09, 0.24),
        (math.radians(35), 0.0, math.radians(25)),
    )
    right_arm = make_box(
        "Arm_R",
        (-0.1, 0.12, 0.74),
        (0.09, 0.09, 0.22),
        (math.radians(55), math.radians(10), math.radians(-40)),
    )
    parts.extend([left_arm, right_arm])

    # Coil-carbine body
    rifle_stock = make_box(
        "Rifle_Stock",
        (0.28, 0.22, 0.66),
        (0.05, 0.12, 0.06),
        (0.0, math.radians(18), math.radians(72)),
    )
    rifle_body = make_box(
        "Rifle_Body",
        (0.42, 0.34, 0.74),
        (0.06, 0.22, 0.07),
        (math.radians(8), math.radians(22), math.radians(68)),
    )
    rifle_barrel = make_cylinder(
        "Rifle_Barrel",
        (0.58, 0.46, 0.8),
        radius=0.025,
        depth=0.34,
        rotation=(math.radians(82), 0.0, math.radians(68)),
        vertices=10,
    )
    coil_1 = make_cylinder(
        "Coil_1",
        (0.5, 0.4, 0.77),
        radius=0.045,
        depth=0.03,
        rotation=(math.radians(82), 0.0, math.radians(68)),
        vertices=10,
    )
    coil_2 = make_cylinder(
        "Coil_2",
        (0.54, 0.43, 0.79),
        radius=0.04,
        depth=0.025,
        rotation=(math.radians(82), 0.0, math.radians(68)),
        vertices=10,
    )
    parts.extend([rifle_stock, rifle_body, rifle_barrel, coil_1, coil_2])

    for part in parts:
        if not part.data.materials:
            if "Rifle" in part.name or "Coil" in part.name:
                assign_material(part, mats["metal"])
            elif part.name == "Pack":
                continue
            else:
                assign_material(part, mats["canvas"])

    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = torso
    bpy.ops.object.join()
    rifleman = bpy.context.active_object
    rifleman.name = "ConscriptRifleman"
    bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY", center="BOUNDS")
    rifleman.location = (0.0, 0.0, 0.0)
    return rifleman


def setup_render_scene():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.resolution_x = 256
    scene.render.resolution_y = 256

    # World — dark neutral fill
    world = scene.world or bpy.data.worlds.new("NeutralWorld")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs["Color"].default_value = (0.08, 0.09, 0.11, 1.0)
        bg.inputs["Strength"].default_value = 0.35

    # Key light
    bpy.ops.object.light_add(type="SUN", location=(3.0, -2.0, 5.0))
    key = bpy.context.active_object
    key.name = "KeyLight"
    key.data.energy = 2.4
    key.rotation_euler = (math.radians(50), math.radians(8), math.radians(25))

    # Fill light
    bpy.ops.object.light_add(type="AREA", location=(-2.5, 2.0, 2.5))
    fill = bpy.context.active_object
    fill.name = "FillLight"
    fill.data.energy = 80.0
    fill.data.size = 2.5
    fill.rotation_euler = (math.radians(60), 0.0, math.radians(-140))

    # Orthographic isometric camera (spec: 35° elev, 45° azimuth)
    bpy.ops.object.camera_add()
    cam = bpy.context.active_object
    cam.name = "RenderCamera"
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 2.35

    elev = math.radians(35)
    az = math.radians(225)
    dist = 6.0
    target = Vector((0.05, 0.0, 0.55))
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
    build_rifleman()
    setup_render_scene()
    bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_BLEND)
    print(f"Saved Conscript Rifleman to: {OUTPUT_BLEND}")
    render_icon()


if __name__ == "__main__":
    main()
