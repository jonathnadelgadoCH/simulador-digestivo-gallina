"""Render a reproducible three-quarter preview of the generated MVP model."""

from pathlib import Path
import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "Blender" / "chicken_digestive_preview.png"


def point_at(obj, target):
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


bpy.context.scene.render.engine = "BLENDER_EEVEE"
bpy.context.scene.render.resolution_x = 800
bpy.context.scene.render.resolution_y = 800
bpy.context.scene.render.resolution_percentage = 100
bpy.context.scene.render.image_settings.file_format = "PNG"
bpy.context.scene.render.film_transparent = False
bpy.context.scene.render.filepath = str(OUTPUT)
bpy.context.scene.world.color = (0.92, 0.92, 0.92)

# Make only the inspection render more transparent; the saved model keeps its
# original material values and Unity controls the final view-mode opacity.
for mat in bpy.data.materials:
    if not mat.name.startswith("Exterior_") or mat.name in {"Exterior_Eye", "Exterior_Comb"}:
        continue
    mat.diffuse_color[3] = 0.14
    node = mat.node_tree.nodes.get("Principled BSDF") if mat.use_nodes else None
    if node is not None:
        node.inputs["Alpha"].default_value = 0.14

bpy.ops.object.camera_add(location=(6.2, -0.35, 2.55))
camera = bpy.context.object
camera.data.lens = 58
point_at(camera, Vector((0, 0.05, 1.45)))
bpy.context.scene.camera = camera

for location, energy, size in (((-4, -4, 6), 1100, 4.0), ((4, 1, 4), 800, 3.0)):
    bpy.ops.object.light_add(type="AREA", location=location)
    light = bpy.context.object
    light.data.energy = energy
    light.data.shape = "DISK"
    light.data.size = size
    point_at(light, Vector((0, 0, 1.3)))

bpy.ops.render.render(write_still=True)
print(f"Rendered {OUTPUT}")
