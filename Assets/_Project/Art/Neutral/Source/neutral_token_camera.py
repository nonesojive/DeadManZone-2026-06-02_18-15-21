"""
DeadManZone — locked isometric token camera for neutral piece renders.

Used by Blender prep scripts (optional path; primary art is SuperGrok AI).
Spec: docs/superpowers/specs/2026-06-06-deadmanzone-top-down-visual-commitment.md
"""
import math

from mathutils import Vector

# Classic 3/4 isometric — matches Grok style anchor (facing bottom-right of frame).
TOKEN_CAMERA_ELEVATION_DEG = 35.0
TOKEN_CAMERA_AZIMUTH_DEG = 225.0
TOKEN_CAMERA_DISTANCE = 8.0


def configure_token_camera(cam, ortho_scale, target_z=0.0):
    """Position an orthographic isometric camera for shop icons and board tokens."""
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = ortho_scale

    elev = math.radians(TOKEN_CAMERA_ELEVATION_DEG)
    az = math.radians(TOKEN_CAMERA_AZIMUTH_DEG)
    dist = TOKEN_CAMERA_DISTANCE
    target = Vector((0.0, 0.0, target_z))

    cam.location = Vector(
        (
            dist * math.cos(elev) * math.cos(az),
            dist * math.cos(elev) * math.sin(az),
            dist * math.sin(elev),
        )
    )
    direction = target - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
