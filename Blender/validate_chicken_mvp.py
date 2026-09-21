from pathlib import Path
import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
SPECIES_PATH = ROOT / "UnityProject" / "Assets" / "StreamingAssets" / "DigestiveSimulator" / "species" / "chicken"
EXPECTED_ORGANS = {
    "beak", "oral_cavity", "tongue", "salivary_glands", "pharynx", "esophagus", "crop",
    "proventriculus", "gizzard", "duodenum", "pancreas", "liver", "biliary_tract", "jejunum",
    "ileum", "meckels_diverticulum", "ceca", "colon", "cloaca"
}
COMPACT_TORSO_ORGANS = EXPECTED_ORGANS - {
    "beak", "oral_cavity", "tongue", "salivary_glands", "pharynx", "esophagus"
}
REMODELED_MIN_TRIANGLES = {
    "beak": 2500,
    "oral_cavity": 1700,
    "tongue": 1400,
    "salivary_glands": 2800,
    "pharynx": 1400,
    "esophagus": 2200,
    "crop": 5000,
    "proventriculus": 4000,
    "gizzard": 7000,
    "liver": 10000,
    "duodenum": 6000,
    "pancreas": 4000,
    "jejunum": 5000,
    "ileum": 1500,
    "meckels_diverticulum": 800,
    "ceca": 5000,
    "colon": 1500,
    "cloaca": 4000,
    "biliary_tract": 2000,
}


def clear():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def inspect(path):
    clear()
    bpy.ops.import_scene.gltf(filepath=str(path))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    triangles = sum(len(mesh.data.loop_triangles) for mesh in meshes)
    vertices = sum(len(mesh.data.vertices) for mesh in meshes)
    return meshes, vertices, triangles


def validate_mesh_quality(meshes, label):
    for obj in meshes:
        if any(abs(component - 1.0) > 0.0001 for component in obj.scale):
            raise RuntimeError(f"{label} mesh has unapplied scale: {obj.name}")
        if len(obj.data.materials) == 0:
            raise RuntimeError(f"{label} mesh has no material: {obj.name}")
        if len(obj.data.uv_layers) == 0:
            raise RuntimeError(f"{label} mesh has no UV map: {obj.name}")
        obj.data.calc_loop_triangles()
        if any(polygon.normal.length < 0.99 for polygon in obj.data.polygons):
            raise RuntimeError(f"{label} mesh has invalid normals: {obj.name}")
        corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
        minimum = Vector((min(p.x for p in corners), min(p.y for p in corners), min(p.z for p in corners)))
        maximum = Vector((max(p.x for p in corners), max(p.y for p in corners), max(p.z for p in corners)))
        origin = obj.matrix_world.translation
        if any(origin[axis] < minimum[axis] - 0.0001 or origin[axis] > maximum[axis] + 0.0001 for axis in range(3)):
            raise RuntimeError(f"{label} mesh origin lies outside its bounds: {obj.name}")


exterior, exterior_vertices, exterior_triangles = inspect(SPECIES_PATH / "model.glb")
validate_mesh_quality(exterior, "Exterior")
digestive, digestive_vertices, digestive_triangles = inspect(SPECIES_PATH / "digestive_system.glb")
validate_mesh_quality(digestive, "Digestive")
names = {obj.name for obj in digestive}
missing = sorted(EXPECTED_ORGANS - names)
unexpected = sorted(names - EXPECTED_ORGANS)
if missing:
    raise RuntimeError(f"Missing organ objects: {missing}")
if unexpected:
    raise RuntimeError(f"Unexpected organ objects: {unexpected}")
for organ in digestive:
    organ.data.calc_loop_triangles()
    minimum_triangles = REMODELED_MIN_TRIANGLES.get(organ.name)
    if minimum_triangles is not None and len(organ.data.loop_triangles) < minimum_triangles:
        raise RuntimeError(
            f"Remodeled organ {organ.name} fell below its detail floor: "
            f"{len(organ.data.loop_triangles)} < {minimum_triangles}"
        )
    if organ.name not in COMPACT_TORSO_ORGANS:
        continue
    corners = [organ.matrix_world @ Vector(corner) for corner in organ.bound_box]
    if any(not (-0.65 <= point.x <= 0.65 and -0.75 <= point.y <= 0.95 and 0.45 <= point.z <= 2.25) for point in corners):
        raise RuntimeError(f"Torso organ exceeds compact body envelope: {organ.name}")
by_name = {organ.name: organ for organ in digestive}
duodenum = by_name["duodenum"]
pancreas = by_name["pancreas"]
duodenum_corners = [duodenum.matrix_world @ Vector(corner) for corner in duodenum.bound_box]
duodenum_minimum = Vector(tuple(min(point[axis] for point in duodenum_corners) for axis in range(3)))
duodenum_maximum = Vector(tuple(max(point[axis] for point in duodenum_corners) for axis in range(3)))
pancreas_center = pancreas.matrix_world.translation
if any(pancreas_center[axis] < duodenum_minimum[axis] - 0.02 or
       pancreas_center[axis] > duodenum_maximum[axis] + 0.02 for axis in range(3)):
    raise RuntimeError("Pancreas is not centered within the duodenal loop envelope")


def world_bounds(obj):
    corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    return (
        Vector(tuple(min(point[axis] for point in corners) for axis in range(3))),
        Vector(tuple(max(point[axis] for point in corners) for axis in range(3))),
    )


def require_bounds_contact(first_name, second_name, margin=0.025):
    first_minimum, first_maximum = world_bounds(by_name[first_name])
    second_minimum, second_maximum = world_bounds(by_name[second_name])
    if any(first_maximum[axis] + margin < second_minimum[axis] or
           second_maximum[axis] + margin < first_minimum[axis] for axis in range(3)):
        raise RuntimeError(f"Digestive transition is spatially discontinuous: {first_name} -> {second_name}")


require_bounds_contact("duodenum", "jejunum")
require_bounds_contact("beak", "oral_cavity")
require_bounds_contact("oral_cavity", "tongue")
require_bounds_contact("oral_cavity", "salivary_glands")
require_bounds_contact("oral_cavity", "pharynx")
require_bounds_contact("pharynx", "esophagus")
require_bounds_contact("esophagus", "crop")
require_bounds_contact("liver", "biliary_tract")
require_bounds_contact("biliary_tract", "duodenum")
require_bounds_contact("jejunum", "ileum")
require_bounds_contact("jejunum", "meckels_diverticulum")
require_bounds_contact("ileum", "ceca")
require_bounds_contact("ceca", "colon")
require_bounds_contact("colon", "cloaca")
print(f"Exterior: {len(exterior)} meshes, {exterior_vertices} vertices, {exterior_triangles} triangles")
print(f"Digestive: {len(digestive)} meshes, {digestive_vertices} vertices, {digestive_triangles} triangles")
if digestive_triangles > 120000 or exterior_triangles > 120000:
    raise RuntimeError("Triangle budget exceeded")
if digestive_triangles < 15000:
    raise RuntimeError("Digestive mesh resolution is below the high-detail MVP target")
print("GLB validation passed: all 19 organ IDs are present.")
print("Compactness validation passed: all torso organs remain inside the body envelope.")
print("Mesh quality validation passed: transforms, pivots, normals, UVs and materials are valid.")
print("Anatomical relation passed: pancreas remains centered within the duodenal loop.")
print("Intestinal continuity passed: duodenum, jejunum, ileum, Meckel landmark and ceca overlap at their transitions.")
print("Terminal tract continuity passed: paired ceca, colon and three-region cloaca remain connected.")
print("Upper tract continuity passed: beak, oral structures, pharynx, esophagus and crop remain connected.")
print("Biliary continuity passed: gallbladder and ducts bridge liver to duodenum.")
