"""Generate a low-poly integration model for the Unity MVP.

This is a technical schematic, not a validated anatomical model. Object IDs match
the JSON module so the same integration can later receive reviewed geometry.
"""

from pathlib import Path
import math
import bpy
import bmesh
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
BLEND_PATH = ROOT / "Blender" / "chicken_digestive_mvp.blend"
SPECIES_PATH = ROOT / "UnityProject" / "Assets" / "StreamingAssets" / "DigestiveSimulator" / "species" / "chicken"
# High-detail defaults.  These values are suitable for a desktop anatomical
# viewer; reduce them by about 40% when targeting low-end mobile hardware.
ORGAN_SPHERE_SEGMENTS = 64
ORGAN_SPHERE_RINGS = 48
ORGAN_CURVE_RESOLUTION = 10
ORGAN_BEVEL_RESOLUTION = 6
ORGAN_BEVEL_SEGMENTS = 12


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
    if collection.name == "DigestiveSystem":
        # The 64x48 source sphere already provides a smooth anatomical
        # silhouette.  A further subdivision would quadruple geometry and push
        # the complete digestive asset beyond the WebGL triangle budget.
        obj["surface_resolution"] = f"{segments}x{rings}_no_extra_subdivision"
    return obj


def build_profiled_mesh(name, location, scale, rotation, mat, collection, profile,
                        radial_segments=64, longitudinal_segments=44):
    """Create a closed organ from an anatomical longitudinal profile."""
    mesh = bpy.data.meshes.new(name + "_mesh")
    vertices = []
    faces = []
    bottom = profile(0.0)
    vertices.append((bottom[0], bottom[1], bottom[2]))
    for ring in range(1, longitudinal_segments):
        t = ring / longitudinal_segments
        center_x, center_y, z, radius_x, radius_y = profile(t)
        for segment in range(radial_segments):
            angle = math.tau * segment / radial_segments
            vertices.append((center_x + math.cos(angle) * radius_x,
                             center_y + math.sin(angle) * radius_y, z))
    top_index = len(vertices)
    top = profile(1.0)
    vertices.append((top[0], top[1], top[2]))
    for segment in range(radial_segments):
        following = (segment + 1) % radial_segments
        faces.append((0, 1 + segment, 1 + following))
    for ring in range(longitudinal_segments - 2):
        current = 1 + ring * radial_segments
        following_ring = current + radial_segments
        for segment in range(radial_segments):
            following = (segment + 1) % radial_segments
            faces.append((current + segment, following_ring + segment,
                          following_ring + following, current + following))
    last_ring = 1 + (longitudinal_segments - 2) * radial_segments
    for segment in range(radial_segments):
        following = (segment + 1) % radial_segments
        faces.append((last_ring + following, last_ring + segment, top_index))
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = location
    obj.scale = scale
    obj.rotation_euler = rotation
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def build_crop_mesh(name, location, scale, rotation, mat, collection):
    """Create a right-sided, dependent crop with narrow esophageal necks."""
    def crop_profile(t):
        envelope = math.sin(math.pi * t) ** 0.62
        dependent_bulge = math.exp(-((t - 0.36) / 0.23) ** 2)
        upper_shoulder = math.exp(-((t - 0.68) / 0.24) ** 2)
        center_x = 0.05 + 0.20 * dependent_bulge + 0.04 * upper_shoulder
        center_y = -0.035 * dependent_bulge
        z = -1.0 + 2.0 * t - 0.09 * dependent_bulge
        radius_x = envelope * (0.69 + 0.37 * dependent_bulge + 0.10 * upper_shoulder)
        radius_y = envelope * (0.65 + 0.22 * dependent_bulge + 0.09 * upper_shoulder)
        return center_x, center_y, z, radius_x, radius_y

    obj = build_profiled_mesh(name, location, scale, rotation, mat, collection,
                              crop_profile, radial_segments=64, longitudinal_segments=48)
    obj["procedural_form"] = "right_sided_dependent_esophageal_diverticulum"
    obj["connection_profile"] = "tapered_cranial_and_caudal_necks"
    return obj


def build_proventriculus_mesh(name, upper_endpoint, lower_endpoint, mat, collection):
    """Create a fusiform glandular stomach between esophagus and gizzard."""
    def proventriculus_profile(t):
        envelope = math.sin(math.pi * t) ** 0.72
        glandular_bulge = math.exp(-((t - 0.48) / 0.25) ** 2)
        caudal_taper = 0.92 - 0.12 * max(0.0, 0.40 - t)
        center_x = 0.035 * math.sin(math.pi * t)
        center_y = -0.025 * math.sin(math.tau * t)
        z = -1.0 + 2.0 * t
        radius_x = envelope * caudal_taper * (0.50 + 0.52 * glandular_bulge)
        radius_y = envelope * caudal_taper * (0.48 + 0.46 * glandular_bulge)
        return center_x, center_y, z, radius_x, radius_y

    upper = Vector(upper_endpoint)
    lower = Vector(lower_endpoint)
    direction = upper - lower
    midpoint = (upper + lower) * 0.5
    rotation = Vector((0.0, 0.0, 1.0)).rotation_difference(direction.normalized()).to_euler()
    obj = build_profiled_mesh(name, midpoint, (0.105, 0.095, direction.length * 0.5), rotation,
                              mat, collection, proventriculus_profile,
                              radial_segments=56, longitudinal_segments=40)
    obj["procedural_form"] = "fusiform_glandular_stomach"
    obj["connection_profile"] = "esophageal_to_ventricular_taper"
    return obj


def build_gizzard_mesh(name, location, scale, rotation, mat, collection):
    """Create a thick biconvex muscular disc with a subtle equatorial belt."""
    mesh = bpy.data.meshes.new(name + "_mesh")
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=72, v_segments=48, radius=1.0)
    for vertex in bm.verts:
        x, y, z = vertex.co
        radial = min(1.0, math.sqrt(y * y + z * z))
        central_dome = (1.0 - radial) ** 2
        angular_lobe = math.exp(-(((y + 0.12) / 0.48) ** 2 + ((z - 0.18) / 0.40) ** 2))
        vertex.co.x = x * (0.60 + 0.52 * central_dome + 0.08 * angular_lobe)
        belt = math.exp(-((z / 0.24) ** 2))
        vertex.co.y = y * (1.0 - 0.10 * belt) + 0.035 * angular_lobe
        vertex.co.z = z * (0.93 + 0.05 * max(0.0, -y))
        if z < -0.18:
            vertex.co.y -= 0.035 * min(1.0, abs(z))
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = location
    obj.scale = scale
    obj.rotation_euler = rotation
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    obj["procedural_form"] = "thick_biconvex_muscular_disc"
    obj["surface_features"] = "equatorial_muscular_belt_and_asymmetric_faces"
    return obj


def build_liver_lobe(name, location, scale, rotation, mat, collection, is_left):
    """Create an avian liver lobe with a thin ventral edge and visceral relief."""
    mesh = bpy.data.meshes.new(name + "_mesh")
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=64, v_segments=44, radius=1.0)
    for vertex in bm.verts:
        x, y, z = vertex.co
        if z < 0.0:
            ventral = min(1.0, abs(z))
            vertex.co.x = x * (1.0 + 0.14 * ventral)
            vertex.co.y = y * (1.0 - 0.36 * ventral)
            vertex.co.z = z * (1.0 + 0.13 * ventral)
        # Shallow medial relief suggests the visceral surface around the
        # proventriculus/gizzard without opening or self-intersecting the mesh.
        medial = x > 0.0 if is_left else x < 0.0
        if medial and y > -0.15:
            relief = (1.0 - min(1.0, abs(x))) * max(0.0, 1.0 - abs(z))
            vertex.co.y += 0.13 * relief
        cranial_notch = math.exp(-(((x + (0.35 if is_left else -0.35)) / 0.24) ** 2 +
                                    ((z - 0.55) / 0.28) ** 2))
        vertex.co.z -= 0.08 * cranial_notch
        vertex.co.x *= 1.03 if is_left else 0.96
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = location
    obj.scale = scale
    obj.rotation_euler = rotation
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    obj["procedural_form"] = "avian_liver_lobe"
    obj["surface_features"] = "thin_ventral_edge_visceral_relief_cranial_notch"
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


def tube(name, points, radius, mat, collection, resolution=2, radii=None, cyclic=False, tangent_scale=None):
    """Create a smooth tube, optionally tapered at each control point.

    ``radii`` contains multipliers relative to ``radius`` and is the key to
    avoiding the artificial hose-like appearance of the original model.
    """
    organ_detail = collection.name == "DigestiveSystem"
    curve = bpy.data.curves.new(name + "_curve", "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = max(resolution, ORGAN_CURVE_RESOLUTION) if organ_detail else resolution
    curve.bevel_depth = radius
    curve.bevel_resolution = max(resolution, ORGAN_BEVEL_RESOLUTION) if organ_detail else resolution
    curve.twist_smooth = 12
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(len(points) - 1)
    spline.use_cyclic_u = cyclic
    if radii is None:
        radii = [1.0] * len(points)
    if len(radii) != len(points):
        raise ValueError(f"{name}: radii must match points")
    coordinates = [Vector(coordinate) for coordinate in points]
    for index, (point, coordinate, point_radius) in enumerate(zip(spline.bezier_points, coordinates, radii)):
        point.co = coordinate
        point.radius = point_radius
        if tangent_scale is None:
            point.handle_left_type = "AUTO"
            point.handle_right_type = "AUTO"
        else:
            previous = coordinates[(index - 1) % len(coordinates)] if cyclic or index > 0 else coordinates[index]
            following = coordinates[(index + 1) % len(coordinates)] if cyclic or index < len(coordinates) - 1 else coordinates[index]
            tangent = (following - previous) * tangent_scale
            point.handle_left_type = "FREE"
            point.handle_right_type = "FREE"
            point.handle_left = coordinate - tangent
            point.handle_right = coordinate + tangent
    obj = bpy.data.objects.new(name, curve)
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    obj.select_set(False)
    obj.name = name
    obj["source_geometry"] = "bezier_tube"
    obj["explicit_tangents"] = tangent_scale is not None
    return obj


def organic_lobe(name, location, scale, rotation, mat, collection, taper=0.18):
    """Asymmetric lobe used for liver and glandular organs."""
    obj = ellipsoid(name, location, scale, mat, collection)
    obj.rotation_euler = rotation
    bpy.context.view_layer.objects.active = obj
    # Displace only very slightly; this breaks the perfect primitive silhouette
    # while retaining a clean mesh for GLB export.
    texture = bpy.data.textures.new(name + "_microform", type="CLOUDS")
    texture.noise_scale = 0.32
    modifier = obj.modifiers.new("Organic_Microform", "DISPLACE")
    modifier.texture = texture
    modifier.strength = taper
    modifier.texture_coords = "GLOBAL"
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
    tube("esophagus", [(0, -0.62, 2.43), (-0.015, -0.57, 2.31), (0.0, -0.51, 2.18),
                       (0.035, -0.47, 2.05), (0.10, -0.42, 1.91)],
         0.052, tract_mat, collection, radii=[0.82, 0.96, 1.02, 1.05, 1.12])
    # The avian crop is a compliant, asymmetric diverticulum rather than a ball.
    crop = build_crop_mesh("crop", (0.15, -0.43, 1.87), (0.25, 0.205, 0.30),
                           (math.radians(-8), math.radians(12), math.radians(-10)),
                           tract_mat, collection)
    crop["anatomical_note"] = "right-sided esophageal diverticulum"
    proventriculus = build_proventriculus_mesh(
        "proventriculus", (0.095, -0.34, 1.68), (0.06, -0.10, 1.39),
        stomach_mat, collection)
    proventriculus["anatomical_note"] = "fusiform glandular stomach"
    gizzard = build_gizzard_mesh("gizzard", (0.10, -0.02, 1.20), (0.33, 0.28, 0.25),
                                 (math.radians(4), math.radians(-9), math.radians(6)),
                                 stomach_mat, collection)
    gizzard = join("gizzard", [
        gizzard,
        tube("gizzard_inlet", [(0.065, -0.095, 1.39), (0.085, -0.055, 1.31)],
             0.061, stomach_mat, collection, radii=[0.82, 1.0]),
        tube("gizzard_outlet", [(0.145, 0.0, 1.12), (0.19, 0.02, 1.10)],
             0.057, stomach_mat, collection, radii=[1.0, 0.90]),
    ])
    gizzard["anatomical_note"] = "muscular ventriculus; external model only"
    # Descending and ascending limbs of the duodenal loop surround the pancreas.
    tube("duodenum", [(0.19, 0.02, 1.10), (0.31, 0.045, 1.06), (0.41, 0.09, 0.98),
                      (0.43, 0.15, 0.86), (0.39, 0.20, 0.75), (0.28, 0.20, 0.72),
                      (0.19, 0.17, 0.78)], 0.055, intestine_mat, collection,
         radii=[0.95, 1.02, 1.0, 0.96, 0.92, 0.95, 0.9], tangent_scale=0.20)
    tube("pancreas", [(0.32, 0.105, 1.01), (0.355, 0.135, 0.93),
                      (0.35, 0.16, 0.84), (0.30, 0.175, 0.77)],
         0.050, pancreas_mat, collection, radii=[0.58, 1.0, 0.88, 0.42])
    join("liver", [
        build_liver_lobe("liver_l", (-0.205, -0.10, 1.48), (0.32, 0.205, 0.40),
                         (math.radians(-5), math.radians(-12), math.radians(5)), liver_mat, collection, True),
        build_liver_lobe("liver_r", (0.265, -0.075, 1.49), (0.295, 0.215, 0.375),
                         (math.radians(4), math.radians(10), math.radians(-6)), liver_mat, collection, False),
    ])
    tube("biliary_tract", [(0.15, -0.02, 1.35), (0.24, 0.05, 1.14), (0.30, 0.10, 1.03)], 0.022, bile_mat, collection)
    tube("jejunum", [(-0.02, 0.12, 0.91), (-0.19, 0.17, 0.90), (-0.31, 0.27, 0.84),
                     (-0.25, 0.40, 0.76), (-0.08, 0.43, 0.72), (0.12, 0.39, 0.80),
                     (0.27, 0.29, 0.82), (0.28, 0.18, 0.72), (0.14, 0.11, 0.65),
                     (-0.02, 0.12, 0.67)], 0.050, intestine_mat, collection,
         radii=[0.9, 1.02, 1.0, 0.94, 1.04, 1.0, 0.96, 1.03, 0.94, 0.88], tangent_scale=0.18)
    tube("ileum", [(0.00, 0.12, 0.67), (0.08, 0.18, 0.64), (0.17, 0.28, 0.63),
                   (0.15, 0.37, 0.62), (0.11, 0.43, 0.62)],
         0.048, intestine_mat, collection, radii=[0.92, 1.0, 0.96, 0.91, 0.88], tangent_scale=0.18)
    ellipsoid("meckels_diverticulum", (-0.02, 0.27, 0.76), (0.055, 0.055, 0.075), landmark_mat, collection)
    ceca_l = tube("ceca_l", [(0.09, 0.42, 0.63), (-0.02, 0.45, 0.65), (-0.18, 0.46, 0.72),
                                  (-0.29, 0.42, 0.86), (-0.30, 0.39, 0.98)],
                  0.043, intestine_mat, collection, radii=[0.82, 1.08, 1.15, 0.82, 0.22])
    ceca_r = tube("ceca_r", [(0.12, 0.43, 0.63), (0.22, 0.46, 0.66), (0.32, 0.46, 0.74),
                                  (0.36, 0.42, 0.88), (0.34, 0.38, 0.98)],
                  0.043, intestine_mat, collection, radii=[0.82, 1.08, 1.15, 0.82, 0.22])
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
