"""
analyze_style.py — Stage 1 of the Asset Cohesion Pipeline
==========================================================
Analyzes 3D models (FBX, GLTF/GLB, OBJ) to extract a "style fingerprint"
describing the visual characteristics of a reference art style
(Quaternius flat-shaded low-poly).

Usage (headless):
    blender --background --python analyze_style.py -- \
        --input-dir /path/to/models \
        --output /path/to/fingerprint.json
"""

import sys
import os
import json
import math
import argparse
import traceback
from collections import Counter
from pathlib import Path

import bpy
import bmesh
import mathutils


# ---------------------------------------------------------------------------
# Argument parsing
# ---------------------------------------------------------------------------

def parse_args():
    """Parse arguments from sys.argv after the '--' separator."""
    # Blender consumes everything before '--'; our args come after it.
    try:
        separator_idx = sys.argv.index("--")
        raw_args = sys.argv[separator_idx + 1:]
    except ValueError:
        raw_args = []

    parser = argparse.ArgumentParser(
        description="Extract style fingerprint from a folder of 3D models."
    )
    parser.add_argument(
        "--input-dir",
        required=True,
        help="Directory (searched recursively) containing FBX/GLB/GLTF/OBJ files.",
    )
    parser.add_argument(
        "--output",
        required=True,
        help="Path for the output JSON fingerprint file.",
    )
    return parser.parse_args(raw_args)


# ---------------------------------------------------------------------------
# Scene helpers
# ---------------------------------------------------------------------------

def clear_scene():
    """Remove all objects, meshes, materials, and images from the current scene."""
    bpy.ops.wm.read_homefile(use_empty=True)


def import_model(filepath: str) -> bool:
    """
    Import a model file into the current Blender scene.
    Returns True on success, False if the import raised an exception.
    """
    ext = Path(filepath).suffix.lower()
    try:
        if ext == ".fbx":
            bpy.ops.import_scene.fbx(filepath=filepath)
        elif ext in (".glb", ".gltf"):
            bpy.ops.import_scene.gltf(filepath=filepath)
        elif ext == ".obj":
            # Blender 4.x new OBJ importer
            bpy.ops.wm.obj_import(filepath=filepath)
        else:
            print(f"  [SKIP] Unsupported extension: {ext}")
            return False
        return True
    except Exception as exc:
        print(f"  [ERROR] Import failed for {filepath}: {exc}")
        traceback.print_exc()
        return False


def find_model_files(root_dir: str):
    """Recursively find all supported model files under root_dir."""
    supported = {".fbx", ".glb", ".gltf", ".obj"}
    root = Path(root_dir)
    found = []
    for ext in supported:
        found.extend(root.rglob(f"*{ext}"))
        found.extend(root.rglob(f"*{ext.upper()}"))
    # Deduplicate (case-insensitive filesystems may return duplicates)
    seen = set()
    unique = []
    for p in found:
        key = str(p).lower()
        if key not in seen:
            seen.add(key)
            unique.append(p)
    return sorted(unique)


# ---------------------------------------------------------------------------
# Color utilities
# ---------------------------------------------------------------------------

def _clamp(v, lo=0.0, hi=1.0):
    return max(lo, min(hi, v))


def quantize_color(r, g, b):
    """
    Quantize each channel to the nearest 1/16 step (i.e. multiples of 16 in
    0-255 space).  This groups near-identical colors together.
    """
    def q(x):
        # x in [0, 1] → round to nearest 1/16 → convert to 0-255 int
        stepped = round(x * 16) / 16
        return int(_clamp(stepped) * 255)

    return (q(r), q(g), q(b))


def rgb_to_hex(r_int, g_int, b_int):
    return "#{:02X}{:02X}{:02X}".format(r_int, g_int, b_int)


def color_distance(c1, c2):
    """
    Euclidean distance in normalized RGB space between two (r, g, b) int triples.
    """
    return math.sqrt(sum((a / 255 - b / 255) ** 2 for a, b in zip(c1, c2)))


def deduplicate_colors(color_counter: Counter, threshold: float = 0.1):
    """
    Given a Counter of (r, g, b) int tuples → count, merge colors that are
    within `threshold` Euclidean distance of each other (keeping the more
    frequent representative).
    Returns a list of (r, g, b) tuples sorted by frequency descending.
    """
    sorted_colors = [c for c, _ in color_counter.most_common()]
    kept = []
    for candidate in sorted_colors:
        is_dupe = False
        for existing in kept:
            if color_distance(candidate, existing) < threshold:
                is_dupe = True
                break
        if not is_dupe:
            kept.append(candidate)
    return kept


def sample_texture_at_uv(image, u, v):
    """
    Sample a Blender image at (u, v) in [0,1]^2 and return (r, g, b) floats.
    Returns None if the image has no pixel data.
    """
    if image is None or not image.has_data:
        return None
    w, h = image.size
    if w == 0 or h == 0:
        return None
    px = int(_clamp(u) * (w - 1))
    py = int(_clamp(v) * (h - 1))
    pixel_index = (py * w + px) * 4  # RGBA
    pixels = image.pixels
    if pixel_index + 2 >= len(pixels):
        return None
    return pixels[pixel_index], pixels[pixel_index + 1], pixels[pixel_index + 2]


def extract_colors_from_mesh(obj):
    """
    Extract colors from a mesh object using the priority:
      1. Vertex color attributes (color attributes / vertex colors)
      2. Material base color / diffuse color
      3. Texture sampled at UV centroid of each face

    Returns a Counter mapping quantized (r, g, b) int tuples → count.
    """
    mesh = obj.data
    color_counts = Counter()

    # ------------------------------------------------------------------
    # Strategy 1: Vertex colors / color attributes
    # ------------------------------------------------------------------
    color_attr = None
    if hasattr(mesh, "color_attributes") and len(mesh.color_attributes) > 0:
        color_attr = mesh.color_attributes[0]
    elif hasattr(mesh, "vertex_colors") and len(mesh.vertex_colors) > 0:
        # Legacy API (Blender < 3.3)
        color_attr = mesh.vertex_colors[0]

    if color_attr is not None:
        try:
            for datum in color_attr.data:
                c = datum.color  # RGBA
                qc = quantize_color(c[0], c[1], c[2])
                color_counts[qc] += 1
            if color_counts:
                return color_counts
        except Exception:
            pass  # Fall through to next strategy

    # ------------------------------------------------------------------
    # Strategy 2: Material base / diffuse color (+ optional texture)
    # ------------------------------------------------------------------
    if not mesh.materials:
        return color_counts  # No color info available

    # Build a face → material index map
    mesh.calc_loop_triangles()

    # Gather UV layer if available
    uv_layer = mesh.uv_layers.active if mesh.uv_layers else None

    for poly in mesh.polygons:
        mat_index = poly.material_index
        if mat_index >= len(mesh.materials):
            continue
        mat = mesh.materials[mat_index]
        if mat is None:
            continue

        # Try to get diffuse/base color from the material
        base_color = None
        tex_image = None

        if mat.use_nodes and mat.node_tree:
            # Walk nodes looking for Principled BSDF or Diffuse BSDF
            for node in mat.node_tree.nodes:
                if node.type == "BSDF_PRINCIPLED":
                    base_color_input = node.inputs.get("Base Color")
                    if base_color_input:
                        # Check if it's linked to a texture
                        if base_color_input.is_linked:
                            link = base_color_input.links[0]
                            if link.from_node.type == "TEX_IMAGE":
                                tex_image = link.from_node.image
                        else:
                            c = base_color_input.default_value
                            base_color = (c[0], c[1], c[2])
                    break
                elif node.type == "BSDF_DIFFUSE":
                    color_input = node.inputs.get("Color")
                    if color_input and not color_input.is_linked:
                        c = color_input.default_value
                        base_color = (c[0], c[1], c[2])
                    break
        else:
            # Non-node material (legacy)
            if hasattr(mat, "diffuse_color"):
                c = mat.diffuse_color
                base_color = (c[0], c[1], c[2])

        # Strategy 3: Sample texture at UV centroid of the face
        if tex_image is not None and tex_image.has_data and uv_layer is not None:
            try:
                # Compute UV centroid from the polygon's loop UVs
                u_sum, v_sum = 0.0, 0.0
                loop_count = poly.loop_total
                for loop_index in range(poly.loop_start, poly.loop_start + loop_count):
                    uv = uv_layer.data[loop_index].uv
                    u_sum += uv[0]
                    v_sum += uv[1]
                u_avg = u_sum / loop_count
                v_avg = v_sum / loop_count
                sampled = sample_texture_at_uv(tex_image, u_avg, v_avg)
                if sampled:
                    qc = quantize_color(*sampled)
                    color_counts[qc] += 1
                    continue
            except Exception:
                pass

        if base_color is not None:
            qc = quantize_color(*base_color)
            color_counts[qc] += 1

    return color_counts


# ---------------------------------------------------------------------------
# Geometry analysis
# ---------------------------------------------------------------------------

ANGLE_BUCKETS = [
    ("0-30",   0,   30),
    ("30-60",  30,  60),
    ("60-90",  60,  90),
    ("90-120", 90,  120),
    ("120-150",120, 150),
    ("150-180",150, 180),
]


def analyze_mesh(obj):
    """
    Extract geometric and visual statistics from a single mesh object.

    Returns a dict with:
        face_count, vertex_count, mean_face_area,
        edge_angle_counts (dict bucket→int),
        color_counts (Counter of quantized rgb tuples),
        extent (list [x, y, z]),
        flat_shaded (bool),
        smooth_face_count, total_face_count
    """
    mesh = obj.data

    # Ensure mesh is in a consistent evaluated state
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.transform(obj.matrix_world)  # Apply world transform for real-world scale

    face_count = len(bm.faces)
    vertex_count = len(bm.verts)

    # Face areas
    total_area = sum(f.calc_area() for f in bm.faces)
    mean_face_area = total_area / face_count if face_count > 0 else 0.0

    # Edge dihedral angle histogram
    angle_counts = {b[0]: 0 for b in ANGLE_BUCKETS}
    bm.edges.ensure_lookup_table()
    for edge in bm.edges:
        linked_faces = edge.link_faces
        if len(linked_faces) == 2:
            n1 = linked_faces[0].normal
            n2 = linked_faces[1].normal
            dot = max(-1.0, min(1.0, n1.dot(n2)))
            # Dihedral angle in degrees (angle between face normals)
            angle_deg = math.degrees(math.acos(dot))
            for label, lo, hi in ANGLE_BUCKETS:
                if lo <= angle_deg < hi or (hi == 180 and angle_deg == 180):
                    angle_counts[label] += 1
                    break

    bm.free()

    # Bounding box extents (world-space AABB)
    world_verts = [obj.matrix_world @ mathutils.Vector(v) for v in obj.bound_box]
    xs = [v.x for v in world_verts]
    ys = [v.y for v in world_verts]
    zs = [v.z for v in world_verts]
    extent = [max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs)]

    # Shading: count flat vs smooth faces
    smooth_count = sum(1 for f in mesh.polygons if f.use_smooth)
    flat_count = face_count - smooth_count

    # Color extraction
    color_counts = extract_colors_from_mesh(obj)

    return {
        "face_count": face_count,
        "vertex_count": vertex_count,
        "mean_face_area": mean_face_area,
        "edge_angle_counts": angle_counts,
        "color_counts": color_counts,
        "extent": extent,
        "flat_shaded": flat_count >= smooth_count,  # majority wins
        "smooth_face_count": smooth_count,
        "total_face_count": face_count,
    }


# ---------------------------------------------------------------------------
# Statistics helpers
# ---------------------------------------------------------------------------

def median(values):
    if not values:
        return 0.0
    s = sorted(values)
    n = len(s)
    mid = n // 2
    if n % 2 == 0:
        return (s[mid - 1] + s[mid]) / 2.0
    return float(s[mid])


def percentile(values, p):
    """Return the p-th percentile (0-100) of a sorted or unsorted list."""
    if not values:
        return 0.0
    s = sorted(values)
    n = len(s)
    idx = (p / 100.0) * (n - 1)
    lo = int(idx)
    hi = min(lo + 1, n - 1)
    frac = idx - lo
    return s[lo] * (1 - frac) + s[hi] * frac


# ---------------------------------------------------------------------------
# Main pipeline
# ---------------------------------------------------------------------------

def analyze_directory(input_dir: str, output_path: str):
    model_files = find_model_files(input_dir)
    if not model_files:
        print(f"[WARNING] No supported model files found in: {input_dir}")
        return

    print(f"[INFO] Found {len(model_files)} model file(s) to analyze.")

    # Aggregation accumulators
    all_face_counts = []
    all_mean_face_areas = []
    all_angle_counts = {b[0]: 0 for b in ANGLE_BUCKETS}
    all_color_counts = Counter()
    all_extents_x = []
    all_extents_y = []
    all_extents_z = []
    flat_face_total = 0
    smooth_face_total = 0
    per_model_records = []
    processed_count = 0

    for idx, model_path in enumerate(model_files, start=1):
        filepath = str(model_path)
        filename = model_path.name
        print(f"[{idx}/{len(model_files)}] Processing: {filename}")

        # Fresh scene for each model
        clear_scene()

        success = import_model(filepath)
        if not success:
            print(f"  [SKIP] Could not import {filename}")
            continue

        # Gather all mesh objects in the scene
        mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
        if not mesh_objects:
            print(f"  [SKIP] No mesh objects found in {filename}")
            continue

        # Per-model aggregates (across all meshes in this file)
        model_face_count = 0
        model_vertex_count = 0
        model_color_counts = Counter()
        model_flat_faces = 0
        model_total_faces = 0
        model_extents = []

        for obj in mesh_objects:
            try:
                stats = analyze_mesh(obj)
            except Exception as exc:
                print(f"  [ERROR] Failed analyzing mesh '{obj.name}': {exc}")
                traceback.print_exc()
                continue

            model_face_count += stats["face_count"]
            model_vertex_count += stats["vertex_count"]
            model_color_counts.update(stats["color_counts"])
            model_flat_faces += stats["total_face_count"] - stats["smooth_face_count"]
            model_total_faces += stats["total_face_count"]
            model_extents.append(stats["extent"])

            # Global accumulators
            all_face_counts.append(stats["face_count"])
            all_mean_face_areas.append(stats["mean_face_area"])
            for bucket in all_angle_counts:
                all_angle_counts[bucket] += stats["edge_angle_counts"].get(bucket, 0)
            all_color_counts.update(stats["color_counts"])
            flat_face_total += stats["total_face_count"] - stats["smooth_face_count"]
            smooth_face_total += stats["smooth_face_count"]

        # Per-model bounding box: union of all mesh extents
        if model_extents:
            merged_extent = [
                max(e[0] for e in model_extents),
                max(e[1] for e in model_extents),
                max(e[2] for e in model_extents),
            ]
        else:
            merged_extent = [0.0, 0.0, 0.0]

        all_extents_x.append(merged_extent[0])
        all_extents_y.append(merged_extent[1])
        all_extents_z.append(merged_extent[2])

        # Build per-model color list (top colors, deduplicated)
        model_deduped = deduplicate_colors(model_color_counts, threshold=0.1)
        model_hex_colors = [rgb_to_hex(*c) for c in model_deduped[:32]]

        per_model_records.append({
            "file": filename,
            "face_count": model_face_count,
            "vertex_count": model_vertex_count,
            "colors": model_hex_colors,
            "extent": [round(v, 4) for v in merged_extent],
            "flat_shaded": model_flat_faces >= model_total_faces / 2 if model_total_faces > 0 else True,
        })

        processed_count += 1
        print(
            f"  faces={model_face_count}  verts={model_vertex_count}  "
            f"colors={len(model_hex_colors)}  "
            f"extent=[{merged_extent[0]:.2f}, {merged_extent[1]:.2f}, {merged_extent[2]:.2f}]"
        )

    if processed_count == 0:
        print("[ERROR] No models were successfully processed. Aborting.")
        return

    print(f"\n[INFO] Successfully processed {processed_count}/{len(model_files)} models.")

    # ------------------------------------------------------------------
    # Build edge angle histogram (normalized to fractions summing to 1)
    # ------------------------------------------------------------------
    total_edges = sum(all_angle_counts.values())
    if total_edges > 0:
        edge_angle_histogram = {
            bucket: round(count / total_edges, 6)
            for bucket, count in all_angle_counts.items()
        }
    else:
        edge_angle_histogram = {b[0]: 0.0 for b in ANGLE_BUCKETS}

    # ------------------------------------------------------------------
    # Global color palette
    # ------------------------------------------------------------------
    deduped_global = deduplicate_colors(all_color_counts, threshold=0.1)
    global_palette = [rgb_to_hex(*c) for c in deduped_global[:32]]
    dominant_colors = global_palette[:8]

    # ------------------------------------------------------------------
    # Shading percentages
    # ------------------------------------------------------------------
    total_faces_all = flat_face_total + smooth_face_total
    flat_pct = round(flat_face_total / total_faces_all, 4) if total_faces_all > 0 else 1.0
    smooth_pct = round(smooth_face_total / total_faces_all, 4) if total_faces_all > 0 else 0.0

    # ------------------------------------------------------------------
    # Assemble fingerprint JSON
    # ------------------------------------------------------------------
    fingerprint = {
        "source": "quaternius",
        "model_count": processed_count,
        "poly_stats": {
            "median_face_count": round(median(all_face_counts), 2),
            "p25_face_count": round(percentile(all_face_counts, 25), 2),
            "p75_face_count": round(percentile(all_face_counts, 75), 2),
            "mean_face_area": round(
                sum(all_mean_face_areas) / len(all_mean_face_areas), 6
            ) if all_mean_face_areas else 0.0,
        },
        "edge_angle_histogram": edge_angle_histogram,
        "color_palette": global_palette,
        "dominant_colors": dominant_colors,
        "scale_stats": {
            "median_extent_x": round(median(all_extents_x), 4),
            "median_extent_y": round(median(all_extents_y), 4),
            "median_extent_z": round(median(all_extents_z), 4),
        },
        "shading": {
            "flat_percentage": flat_pct,
            "smooth_percentage": smooth_pct,
        },
        "per_model": per_model_records,
    }

    # Write output
    output_file = Path(output_path)
    output_file.parent.mkdir(parents=True, exist_ok=True)
    with open(output_file, "w", encoding="utf-8") as fh:
        json.dump(fingerprint, fh, indent=2)

    print(f"\n[INFO] Fingerprint written to: {output_file}")
    print(f"       Models processed : {processed_count}")
    print(f"       Palette size      : {len(global_palette)}")
    print(f"       Flat shading      : {flat_pct * 100:.1f}%")


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    args = parse_args()
    print("=" * 60)
    print("Asset Cohesion Pipeline — Stage 1: Style Fingerprint")
    print("=" * 60)
    print(f"Input directory : {args.input_dir}")
    print(f"Output file     : {args.output}")
    print()
    analyze_directory(args.input_dir, args.output)
