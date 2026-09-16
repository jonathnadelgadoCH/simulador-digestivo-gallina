"""Generate a low-poly integration model for the Unity MVP.

This is a technical schematic, not a validated anatomical model. Object IDs match
the JSON module so the same integration can later receive reviewed geometry.
"""

from pathlib import Path
import math
import bpy
import bmesh


ROOT = Path(__file__).resolve().parents[1]
BLEND_PATH = ROOT / "Blender" / "chicken_digestive_mvp.blend"
SPECIES_PATH = ROOT / "UnityProject" / "Assets" / "StreamingAssets" / "DigestiveSimulator" / "species" / "chicken"
ORGAN_SPHERE_SEGMENTS = 36
ORGAN_SPHERE_RINGS = 24
ORGAN_CURVE_RESOLUTION = 4
ORGAN_BEVEL_RESOLUTION = 4


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        if collection.name != "Collection":
            bpy.data.collections.remove(collection)
    base = bpy.data.collections.get("Collection")
    base.name = "Exterior"
    digestive = bpy.data.collections.new("DigestiveSystem")
    bpy.context.scene.collection.children.link(digestive)
    return base, digestive


def material(name, color, alpha=1.0, metallic=0.0, roughness=0.65):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = (*color, alpha)
    node = mat.node_tree.nodes.get("Principled BSDF")
    node.inputs["Base Color"].default_value = (*color, alpha)
    node.inputs["Roughness"].default_value = roughness
    node.inputs["Metallic"].default_value = metallic
    node.inputs["Alpha"].default_value = alpha
    if alpha < 1.0:
        mat.surface_render_method = "DITHERED"
    return mat


def move_to_collection(obj, collection):
    for current in list(obj.users_collection):
        current.objects.unlink(obj)
    collection.objects.link(obj)


def ellipsoid(name, location, scale, mat, collection, segments=20, rings=12):
    if collection.name == "DigestiveSystem":
        segments = max(segments, ORGAN_SPHERE_SEGMENTS)
        rings = max(rings, ORGAN_SPHERE_RINGS)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    move_to_collection(obj, collection)
    return obj


def cone(name, location, scale, rotation, mat, collection):
    bpy.ops.mesh.primitive_cone_add(vertices=16, radius1=1.0, radius2=0.12, depth=2.0, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    move_to_collection(obj, collection)
    return obj


def tube(name, points, radius, mat, collection, resolution=2):
    organ_detail = collection.name == "DigestiveSystem"
    curve = bpy.data.curves.new(name + "_curve", "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = ORGAN_CURVE_RESOLUTION if organ_detail else 2
    curve.bevel_depth = radius
    curve.bevel_resolution = max(resolution, ORGAN_BEVEL_RESOLUTION) if organ_detail else resolution
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for point, coordinate in zip(spline.bezier_points, points):
        point.co = coordinate
        point.handle_left_type = "AUTO"
        point.handle_right_type = "AUTO"
    obj = bpy.data.objects.new(name, curve)
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    obj.select_set(False)
    obj.name = name
    return obj


def join(name, objects):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    objects[0].name = name
    return objects[0]


def finalize_collection(collection, role):
    """Normalize meshes for predictable GLB import in Unity."""
    for obj in collection.all_objects:
        if obj.type != "MESH":
            continue
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

        mesh = obj.data
        mesh.validate(clean_customdata=False)
        normals_mesh = bmesh.new()
        normals_mesh.from_mesh(mesh)
        bmesh.ops.recalc_face_normals(normals_mesh, faces=normals_mesh.faces)
        normals_mesh.to_mesh(mesh)
        normals_mesh.free()
        for polygon in mesh.polygons:
            polygon.use_smooth = True

        if len(mesh.uv_layers) == 0:
            bpy.ops.object.mode_set(mode="EDIT")
            bpy.ops.mesh.select_all(action="SELECT")
            bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02)
            bpy.ops.object.mode_set(mode="OBJECT")

        bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY", center="BOUNDS")
        obj["asset_role"] = role
        obj["scientific_status"] = "schematic_pending_validation"
        if role == "digestive_organ":
            obj["organ_id"] = obj.name


def build_exterior(collection):
    feather = material("Exterior_Feather", (0.72, 0.27, 0.08), 0.38, roughness=0.9)
    feather_light = material("Exterior_Feather_Light", (0.90, 0.48, 0.16), 0.38, roughness=0.92)
    red = material("Exterior_Comb", (0.65, 0.06, 0.045), 0.65)
    beak_mat = material("Exterior_Beak", (0.95, 0.62, 0.08), 0.75)
    dark = material("Exterior_Eye", (0.03, 0.025, 0.02), 1.0)
    # Rounded laying-hen silhouette based on the supplied side-view reference.
    ellipsoid("Exterior_Body", (0, 0.15, 1.34), (0.92, 1.18, 1.04), feather, collection)
    ellipsoid("Exterior_Belly", (0, 0.22, 0.92), (0.78, 0.88, 0.64), feather_light, collection)
    ellipsoid("Exterior_Chest", (0, -0.55, 1.48), (0.72, 0.66, 0.86), feather_light, collection)
    ellipsoid("Exterior_Rump", (0, 0.78, 1.48), (0.72, 0.70, 0.78), feather, collection)
    ellipsoid("Exterior_Neck", (0, -0.61, 2.10), (0.45, 0.46, 0.78), feather, collection)
    ellipsoid("Exterior_Head", (0, -0.78, 2.67), (0.43, 0.47, 0.42), feather, collection)
    cone("Exterior_Beak", (0, -1.27, 2.63), (0.18, 0.18, 0.34), (math.radians(90), 0, 0), beak_mat, collection)
    for index, offset in enumerate((-0.18, 0.0, 0.18)):
        ellipsoid(f"Exterior_Comb_{index}", (0, -0.78 + offset, 3.05 + 0.04 * (1 - abs(index - 1))), (0.16, 0.14, 0.19), red, collection)
    ellipsoid("Exterior_Wattle_L", (-0.12, -1.04, 2.41), (0.13, 0.12, 0.23), red, collection)
    ellipsoid("Exterior_Wattle_R", (0.12, -1.04, 2.41), (0.13, 0.12, 0.23), red, collection)
    ellipsoid("Exterior_Eye_L", (-0.29, -1.08, 2.75), (0.052, 0.032, 0.052), dark, collection)
    ellipsoid("Exterior_Eye_R", (0.29, -1.08, 2.75), (0.052, 0.032, 0.052), dark, collection)
    ellipsoid("Exterior_Wing_L", (-0.76, 0.05, 1.48), (0.27, 0.78, 0.69), feather_light, collection)
    ellipsoid("Exterior_Wing_R", (0.76, 0.05, 1.48), (0.27, 0.78, 0.69), feather_light, collection)
    for side, x in (("L", -0.32), ("R", 0.32)):
        tube(f"Exterior_Leg_{side}", [(x, 0.12, 0.53), (x * 1.08, 0.10, -0.04)], 0.058, beak_mat, collection)
        tube(f"Exterior_Foot_{side}", [(x * 1.08, 0.10, -0.04), (x * 1.08, -0.22, -0.10)], 0.045, beak_mat, collection)
        for toe_index, toe_x in enumerate((-0.13, 0.0, 0.13)):
            tube(f"Exterior_Toe_{side}_{toe_index}", [(x * 1.08, -0.20, -0.10), (x + toe_x, -0.52, -0.13)], 0.028, beak_mat, collection, resolution=1)
    for index, x in enumerate((-0.30, -0.10, 0.10, 0.30)):
        tail_angle = -22 - index * 5
        tail_location = (x, 1.36 + index * 0.025, 1.82 + 0.05 * (1 - abs(index - 1.5)))
        cone(f"Exterior_Tail_{index}", tail_location, (0.15, 0.15, 0.50), (math.radians(tail_angle), 0, 0), feather, collection)


def build_digestive(collection):
    oral_mat = material("Organ_Oral", (0.92, 0.48, 0.34))
    tract_mat = material("Organ_Tract", (0.92, 0.42, 0.48))
    stomach_mat = material("Organ_Stomach", (0.69, 0.22, 0.32))
    intestine_mat = material("Organ_Intestine", (0.95, 0.58, 0.62))
    liver_mat = material("Organ_Liver", (0.38, 0.075, 0.06))
    pancreas_mat = material("Organ_Pancreas", (0.94, 0.72, 0.32))
    bile_mat = material("Organ_Bile", (0.24, 0.48, 0.12))
    landmark_mat = material("Organ_Landmark", (0.32, 0.66, 0.88))

    ellipsoid("beak", (0, -1.24, 2.63), (0.20, 0.25, 0.12), oral_mat, collection)
    ellipsoid("oral_cavity", (0, -0.92, 2.61), (0.21, 0.25, 0.15), oral_mat, collection)
    ellipsoid("tongue", (0, -0.96, 2.56), (0.09, 0.19, 0.045), tract_mat, collection)
    join("salivary_glands", [
        ellipsoid("salivary_l", (-0.14, -0.86, 2.58), (0.05, 0.09, 0.06), pancreas_mat, collection),
        ellipsoid("salivary_r", (0.14, -0.86, 2.58), (0.05, 0.09, 0.06), pancreas_mat, collection),
    ])
    ellipsoid("pharynx", (0, -0.69, 2.48), (0.13, 0.14, 0.17), tract_mat, collection)
    tube("esophagus", [(0, -0.62, 2.43), (0.01, -0.51, 2.18), (0.10, -0.42, 1.91)], 0.052, tract_mat, collection)
    ellipsoid("crop", (0.16, -0.43, 1.88), (0.25, 0.19, 0.30), tract_mat, collection)
    tube("proventriculus", [(0.09, -0.32, 1.68), (0.03, -0.20, 1.52), (0.06, -0.10, 1.40)], 0.088, stomach_mat, collection)
    ellipsoid("gizzard", (0.10, -0.02, 1.20), (0.31, 0.27, 0.25), stomach_mat, collection)
    tube("duodenum", [(0.19, 0.02, 1.10), (0.39, 0.08, 1.00), (0.39, 0.16, 0.82), (0.20, 0.17, 0.78)], 0.058, intestine_mat, collection)
    ellipsoid("pancreas", (0.31, 0.12, 0.93), (0.07, 0.06, 0.19), pancreas_mat, collection)
    join("liver", [
        ellipsoid("liver_l", (-0.20, -0.10, 1.48), (0.29, 0.22, 0.36), liver_mat, collection),
        ellipsoid("liver_r", (0.28, -0.08, 1.49), (0.27, 0.21, 0.34), liver_mat, collection),
    ])
    tube("biliary_tract", [(0.15, -0.02, 1.35), (0.24, 0.05, 1.14), (0.30, 0.10, 1.03)], 0.022, bile_mat, collection)
    tube("jejunum", [(-0.02, 0.12, 0.91), (-0.28, 0.25, 0.86), (-0.13, 0.39, 0.75), (0.17, 0.36, 0.82), (0.25, 0.19, 0.73), (0.00, 0.12, 0.67)], 0.052, intestine_mat, collection)
    tube("ileum", [(0.00, 0.12, 0.67), (0.16, 0.27, 0.64), (0.11, 0.43, 0.62)], 0.049, intestine_mat, collection)
    ellipsoid("meckels_diverticulum", (-0.02, 0.27, 0.76), (0.055, 0.055, 0.075), landmark_mat, collection)
    ceca_l = tube("ceca_l", [(0.09, 0.42, 0.63), (-0.20, 0.45, 0.72), (-0.30, 0.39, 0.98)], 0.041, intestine_mat, collection)
    ceca_r = tube("ceca_r", [(0.12, 0.43, 0.63), (0.31, 0.45, 0.72), (0.34, 0.38, 0.98)], 0.041, intestine_mat, collection)
    join("ceca", [ceca_l, ceca_r])
    tube("colon", [(0.11, 0.43, 0.62), (0.08, 0.55, 0.61), (0.03, 0.66, 0.64)], 0.055, tract_mat, collection)
    ellipsoid("cloaca", (0.02, 0.72, 0.66), (0.10, 0.12, 0.12), tract_mat, collection)


def export_collection(collection, filepath):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in collection.all_objects:
        obj.select_set(True)
    bpy.ops.export_scene.gltf(
        filepath=str(filepath),
        export_format="GLB",
        use_selection=True,
        export_apply=True,
        export_yup=True,
        export_materials="EXPORT",
        export_extras=True,
        export_cameras=False,
        export_lights=False,
    )


def main():
    exterior, digestive = clear_scene()
    build_exterior(exterior)
    build_digestive(digestive)
    finalize_collection(exterior, "exterior")
    finalize_collection(digestive, "digestive_organ")
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    bpy.context.scene["scientific_status"] = "schematic_pending_validation"
    bpy.context.scene["notice"] = "Technical integration model; not validated anatomical geometry."
    SPECIES_PATH.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    export_collection(exterior, SPECIES_PATH / "model.glb")
    export_collection(digestive, SPECIES_PATH / "digestive_system.glb")
    print(f"Generated {BLEND_PATH}")
    print(f"Generated {SPECIES_PATH / 'model.glb'}")
    print(f"Generated {SPECIES_PATH / 'digestive_system.glb'}")


if __name__ == "__main__":
    main()
