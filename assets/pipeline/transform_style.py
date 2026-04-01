"""
transform_style.py — Stage 2 of the Asset Cohesion Pipeline
=============================================================
Takes a style fingerprint JSON (produced by analyze_style.py, Stage 1) and
transforms input 3D models so they match that target style.

Supported input formats: FBX, GLB/GLTF, OBJ
Output: .glb (GLTF Binary)

Usage (headless Blender):
    blender --background --python transform_style.py -- \\
        --fingerprint fingerprint.json \\
        --input /path/to/models_or_single_file \\
        --output-dir /path/to/output \\
        [--preserve-animations] \\
        [--aggressive]
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


# ===========================================================================
# Argument parsing
# ===========================================================================

def parse_args():
    """Parse script arguments from sys.argv after the '--' separator."""
    try:
        separator_idx = sys.argv.index("--")
        raw_args = sys.argv[separator_idx + 1:]
    except ValueError:
        raw_args = []

    parser = argparse.ArgumentParser(
        description="Transform 3D models to match a target style fingerprint."
    )
    parser.add_argument(
        "--fingerprint",
        required=True,
        help="Path to the JSON fingerprint file produced by analyze_style.py.",
    )
    parser.add_argument(
        "--input",
        required=True,
        help="A single model file (FBX/GLB/GLTF/OBJ) or a directory of models.",
    )
    parser.add_argument(
        "--output-dir",
        required=True,
        dest="output_dir",
        help="Directory where transformed .glb files will be written.",
    )
    parser.add_argument(
        "--preserve-animations",
        action="store_true",
        dest="preserve_animations",
        help="Keep animations (armature + actions) in the exported file.",
    )
    parser.add_argument(
        "--aggressive",
        action="store_true",
        help="Use more aggressive decimation/remeshing when poly count is too low.",
    )
    return parser.parse_args(raw_args)


# ===========================================================================
# Fingerprint loading
# ===========================================================================

def load_fingerprint(path: str) -> dict:
    """Load and validate the style fingerprint JSON file."""
    fp_path = Path(path)
    if not fp_path.exists():
        raise FileNotFoundError(f"Fingerprint not found: {path}")
    with open(fp_path, "r", encoding="utf-8") as fh:
        data = json.load(fh)

    # Validate required keys
    required = ["poly_stats", "color_palette", "dominant_colors", "scale_stats", "shading"]
    for key in required:
        if key not in data:
            raise ValueError(f"Fingerprint missing required key: '{key}'")

    return data


# ===========================================================================
# File discovery
# ===========================================================================

def find_model_files(path: str):
    """
    If path is a file, return [path].
    If path is a directory, recursively find all supported model files.
    """
    p = Path(path)
    supported = {".fbx", ".glb", ".gltf", ".obj"}

    if p.is_file():
        if p.suffix.lower() in supported:
            return [p]
        else:
            print(f"[WARNING] File '{p.name}' has unsupported extension.")
            return []

    if p.is_dir():
        found = []
        for ext in supported:
            found.extend(p.rglob(f"*{ext}"))
            found.extend(p.rglob(f"*{ext.upper()}"))
        # Deduplicate (case-insensitive filesystems)
        seen = set()
        unique = []
        for f in found:
            key = str(f).lower()
            if key not in seen:
                seen.add(key)
                unique.append(f)
        return sorted(unique)

    raise ValueError(f"Input path does not exist or is not a file/directory: {path}")


# ===========================================================================
# Scene helpers
# ===========================================================================

def clear_scene():
    """Reset to a completely empty scene."""
    bpy.ops.wm.read_homefile(use_empty=True)


def import_model(filepath: str) -> bool:
    """
    Import a model file into the current Blender scene.
    Returns True on success, False on failure.
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


def has_animations() -> bool:
    """
    Return True if the scene contains any armature with associated actions,
    or any object that has NLA tracks / animation data.
    """
    for obj in bpy.context.scene.objects:
        if obj.type == "ARMATURE":
            if obj.animation_data and (
                obj.animation_data.action or obj.animation_data.nla_tracks
            ):
                return True
        if obj.animation_data and obj.animation_data.action:
            return True
    return False


# ===========================================================================
# Color utilities — LAB color space (no scipy)
# ===========================================================================

def _clamp(v, lo=0.0, hi=1.0):
    return max(lo, min(hi, v))


def hex_to_rgb_float(hex_str: str):
    """Parse '#RRGGBB' or '#RGB' hex string → (r, g, b) floats in [0, 1]."""
    hex_str = hex_str.strip().lstrip("#")
    if len(hex_str) == 3:
        hex_str = "".join(c * 2 for c in hex_str)
    r = int(hex_str[0:2], 16) / 255.0
    g = int(hex_str[2:4], 16) / 255.0
    b = int(hex_str[4:6], 16) / 255.0
    return (r, g, b)


def rgb_to_lab(r: float, g: float, b: float):
    """
    Convert linear sRGB (0-1) to CIE L*a*b* using D65 illuminant.

    Steps:
      1. Apply inverse sRGB gamma (linearise)
      2. RGB → XYZ (sRGB primaries, D65 white)
      3. XYZ → LAB
    """
    # --- Step 1: sRGB linearisation ---
    def linearise(c):
        c = _clamp(c)
        if c <= 0.04045:
            return c / 12.92
        return ((c + 0.055) / 1.055) ** 2.4

    r_lin = linearise(r)
    g_lin = linearise(g)
    b_lin = linearise(b)

    # --- Step 2: RGB → XYZ (D65) ---
    # IEC 61966-2-1 sRGB matrix
    X = r_lin * 0.4124564 + g_lin * 0.3575761 + b_lin * 0.1804375
    Y = r_lin * 0.2126729 + g_lin * 0.7151522 + b_lin * 0.0721750
    Z = r_lin * 0.0193339 + g_lin * 0.1191920 + b_lin * 0.9503041

    # Normalise by D65 white point (Xn=0.95047, Yn=1.00000, Zn=1.08883)
    Xn, Yn, Zn = 0.95047, 1.00000, 1.08883
    fx = X / Xn
    fy = Y / Yn
    fz = Z / Zn

    # --- Step 3: XYZ → LAB ---
    epsilon = 0.008856   # (6/29)^3
    kappa   = 903.3      # (29/3)^3

    def f(t):
        if t > epsilon:
            return t ** (1.0 / 3.0)
        return (kappa * t + 16.0) / 116.0

    fx = f(fx)
    fy = f(fy)
    fz = f(fz)

    L = 116.0 * fy - 16.0
    a = 500.0 * (fx - fy)
    b_val = 200.0 * (fy - fz)

    return (L, a, b_val)


def lab_distance(lab1, lab2) -> float:
    """Euclidean distance between two CIE L*a*b* triplets."""
    return math.sqrt(
        (lab1[0] - lab2[0]) ** 2
        + (lab1[1] - lab2[1]) ** 2
        + (lab1[2] - lab2[2]) ** 2
    )


def nearest_palette_color_lab(r: float, g: float, b: float, palette_labs: list):
    """
    Find the index of the nearest palette color (in LAB space) for a given
    RGB color. palette_labs is a list of (L, a, b) tuples.
    Returns the index of the best match.
    """
    lab = rgb_to_lab(r, g, b)
    best_idx = 0
    best_dist = float("inf")
    for i, p_lab in enumerate(palette_labs):
        d = lab_distance(lab, p_lab)
        if d < best_dist:
            best_dist = d
            best_idx = i
    return best_idx


def build_palette_labs(fingerprint: dict):
    """
    Build a list of (L, a, b) tuples from the fingerprint's color_palette,
    and a parallel list of (r, g, b) float tuples.
    dominant_colors come first (they map to the most common model colors).
    """
    # Dominant colors first, then remaining palette colors (no duplicates)
    dominant = fingerprint.get("dominant_colors", [])
    full_palette = fingerprint.get("color_palette", [])

    ordered = list(dominant)
    seen = set(c.upper() for c in dominant)
    for c in full_palette:
        if c.upper() not in seen:
            ordered.append(c)
            seen.add(c.upper())

    rgb_list = [hex_to_rgb_float(c) for c in ordered]
    lab_list = [rgb_to_lab(r, g, b) for r, g, b in rgb_list]
    return rgb_list, lab_list, ordered


def quantize_color(r, g, b):
    """Quantize to 1/16 steps — matches analyze_style.py for consistent binning."""
    def q(x):
        stepped = round(_clamp(x) * 16) / 16
        return int(stepped * 255)
    return (q(r), q(g), q(b))


def rgb_to_hex(r_int, g_int, b_int):
    return "#{:02X}{:02X}{:02X}".format(r_int, g_int, b_int)


# ===========================================================================
# Color extraction (mirrors analyze_style.py, used for remapping)
# ===========================================================================

def sample_texture_at_uv(image, u, v):
    """Sample a Blender image at (u, v) → (r, g, b) floats, or None."""
    if image is None or not image.has_data:
        return None
    w, h = image.size
    if w == 0 or h == 0:
        return None
    px = int(_clamp(u) * (w - 1))
    py = int(_clamp(v) * (h - 1))
    pixel_index = (py * w + px) * 4
    pixels = image.pixels
    if pixel_index + 2 >= len(pixels):
        return None
    return (pixels[pixel_index], pixels[pixel_index + 1], pixels[pixel_index + 2])


def get_mesh_color_mode(obj):
    """
    Determine how this mesh stores color:
      'vertex'   – has a color attribute / vertex color layer
      'texture'  – material uses a texture node
      'material' – material has a flat diffuse / base color
      'none'     – no color info detected
    """
    mesh = obj.data

    if hasattr(mesh, "color_attributes") and len(mesh.color_attributes) > 0:
        return "vertex"
    if hasattr(mesh, "vertex_colors") and len(mesh.vertex_colors) > 0:
        return "vertex"

    for mat in (mesh.materials or []):
        if mat is None:
            continue
        if mat.use_nodes and mat.node_tree:
            for node in mat.node_tree.nodes:
                if node.type == "BSDF_PRINCIPLED":
                    inp = node.inputs.get("Base Color")
                    if inp and inp.is_linked:
                        for link in inp.links:
                            if link.from_node.type == "TEX_IMAGE":
                                return "texture"
                    return "material"
                elif node.type in ("BSDF_DIFFUSE", "EMISSION"):
                    return "material"
        elif hasattr(mat, "diffuse_color"):
            return "material"

    return "none"


def extract_face_color_floats(obj):
    """
    Extract one (r, g, b) float tuple per polygon, using vertex colors,
    material base color, or texture UV sample.
    Returns list of (r, g, b) per face, same length as mesh.polygons.
    """
    mesh = obj.data
    face_colors = []

    # --- Vertex colors (priority 1) ---
    color_attr = None
    if hasattr(mesh, "color_attributes") and len(mesh.color_attributes) > 0:
        color_attr = mesh.color_attributes[0]
    elif hasattr(mesh, "vertex_colors") and len(mesh.vertex_colors) > 0:
        color_attr = mesh.vertex_colors[0]

    if color_attr is not None:
        try:
            # Map loop index → color
            loop_colors = []
            for datum in color_attr.data:
                c = datum.color  # RGBA
                loop_colors.append((c[0], c[1], c[2]))

            for poly in mesh.polygons:
                if poly.loop_total > 0 and poly.loop_start < len(loop_colors):
                    # Average the corner colors for this face
                    r_sum = g_sum = b_sum = 0.0
                    count = 0
                    for li in range(poly.loop_start, poly.loop_start + poly.loop_total):
                        if li < len(loop_colors):
                            r_sum += loop_colors[li][0]
                            g_sum += loop_colors[li][1]
                            b_sum += loop_colors[li][2]
                            count += 1
                    if count > 0:
                        face_colors.append((r_sum / count, g_sum / count, b_sum / count))
                    else:
                        face_colors.append((0.5, 0.5, 0.5))
                else:
                    face_colors.append((0.5, 0.5, 0.5))

            if face_colors:
                return face_colors
        except Exception:
            pass

    # --- Material / texture (priority 2 & 3) ---
    uv_layer = mesh.uv_layers.active if mesh.uv_layers else None

    for poly in mesh.polygons:
        mat_index = poly.material_index
        if mat_index >= len(mesh.materials):
            face_colors.append((0.5, 0.5, 0.5))
            continue

        mat = mesh.materials[mat_index] if mesh.materials else None
        if mat is None:
            face_colors.append((0.5, 0.5, 0.5))
            continue

        base_color = None
        tex_image = None

        if mat.use_nodes and mat.node_tree:
            for node in mat.node_tree.nodes:
                if node.type == "BSDF_PRINCIPLED":
                    inp = node.inputs.get("Base Color")
                    if inp:
                        if inp.is_linked:
                            for link in inp.links:
                                if link.from_node.type == "TEX_IMAGE":
                                    tex_image = link.from_node.image
                                    break
                        else:
                            c = inp.default_value
                            base_color = (c[0], c[1], c[2])
                    break
                elif node.type == "BSDF_DIFFUSE":
                    inp = node.inputs.get("Color")
                    if inp and not inp.is_linked:
                        c = inp.default_value
                        base_color = (c[0], c[1], c[2])
                    break
        else:
            if hasattr(mat, "diffuse_color"):
                c = mat.diffuse_color
                base_color = (_clamp(c[0]), _clamp(c[1]), _clamp(c[2]))

        if tex_image is not None and tex_image.has_data and uv_layer is not None:
            try:
                u_sum = v_sum = 0.0
                loop_count = poly.loop_total
                for li in range(poly.loop_start, poly.loop_start + loop_count):
                    uv = uv_layer.data[li].uv
                    u_sum += uv[0]
                    v_sum += uv[1]
                if loop_count > 0:
                    sampled = sample_texture_at_uv(tex_image, u_sum / loop_count, v_sum / loop_count)
                    if sampled:
                        face_colors.append(sampled)
                        continue
            except Exception:
                pass

        if base_color is not None:
            face_colors.append(base_color)
        else:
            face_colors.append((0.5, 0.5, 0.5))

    return face_colors


# ===========================================================================
# Stage 1 — Poly count normalization
# ===========================================================================

def normalize_poly_count(obj, fingerprint: dict, aggressive: bool):
    """
    Decimate or subdivide the mesh to bring it closer to the fingerprint
    median face count.
    """
    poly_stats = fingerprint["poly_stats"]
    target_median = poly_stats["median_face_count"]
    p25 = poly_stats["p25_face_count"]
    p75 = poly_stats["p75_face_count"]

    current_faces = len(obj.data.polygons)
    print(f"    Poly count: {current_faces} faces  (target median: {target_median:.0f})")

    # --- Too many polygons: decimate ---
    if current_faces > p75 * 1.5:
        ratio = target_median / current_faces if current_faces > 0 else 1.0
        ratio = max(0.1, min(1.0, ratio))
        print(f"    Applying Decimate (COLLAPSE) ratio={ratio:.4f}")

        mod = obj.modifiers.new(name="DECIMATE_cohesion", type="DECIMATE")
        mod.decimate_type = "COLLAPSE"
        mod.ratio = ratio

        # Apply immediately so subsequent steps see the reduced mesh
        try:
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.modifier_apply(modifier=mod.name)
        except Exception as exc:
            print(f"    [WARNING] Decimate apply failed: {exc}")

        new_faces = len(obj.data.polygons)
        print(f"    After decimate: {new_faces} faces")

    # --- Too few polygons and aggressive mode: subdivide then decimate back ---
    elif current_faces < p25 * 0.5 and aggressive:
        print(f"    Mesh too low-poly ({current_faces} < {p25 * 0.5:.0f}); "
              f"subdividing then decimating to target.")

        # Subdivision Surface level 1
        mod_sub = obj.modifiers.new(name="SUBD_cohesion", type="SUBSURF")
        mod_sub.levels = 1
        mod_sub.render_levels = 1

        try:
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.modifier_apply(modifier=mod_sub.name)
        except Exception as exc:
            print(f"    [WARNING] Subdivision apply failed: {exc}")

        sub_faces = len(obj.data.polygons)
        print(f"    After subdivision: {sub_faces} faces")

        # Decimate back to target median
        if sub_faces > target_median:
            ratio = target_median / sub_faces
            ratio = max(0.1, min(1.0, ratio))
            mod_dec = obj.modifiers.new(name="DECIMATE_back_cohesion", type="DECIMATE")
            mod_dec.decimate_type = "COLLAPSE"
            mod_dec.ratio = ratio
            try:
                bpy.ops.object.modifier_apply(modifier=mod_dec.name)
            except Exception as exc:
                print(f"    [WARNING] Back-decimate apply failed: {exc}")

        new_faces = len(obj.data.polygons)
        print(f"    After back-decimate: {new_faces} faces")


# ===========================================================================
# Stage 2 — Flat shading enforcement
# ===========================================================================

def enforce_flat_shading(obj, fingerprint: dict):
    """
    If the fingerprint style is predominantly flat-shaded, set all faces to
    flat shading, remove custom normals, disable auto smooth, and add an
    Edge Split modifier for crisp hard edges.
    """
    flat_pct = fingerprint["shading"].get("flat_percentage", 1.0)
    if flat_pct <= 0.7:
        print("    Shading: style is smooth — skipping flat shading enforcement.")
        return

    print(f"    Enforcing flat shading (style flat_percentage={flat_pct:.2f})")

    mesh = obj.data

    # Set every polygon to flat (not smooth)
    for poly in mesh.polygons:
        poly.use_smooth = False

    # Remove split normals / custom normal data
    if hasattr(mesh, "free_normals_split"):
        try:
            mesh.free_normals_split()
        except Exception:
            pass

    # Turn off auto smooth (attribute name changed across Blender versions)
    if hasattr(mesh, "use_auto_smooth"):
        mesh.use_auto_smooth = False

    # Remove any existing custom normal modifier to avoid conflicts
    for mod in list(obj.modifiers):
        if mod.type in ("NORMAL_EDIT", "WEIGHTED_NORMAL"):
            obj.modifiers.remove(mod)

    # Remove any existing Edge Split modifier so we don't double up
    for mod in list(obj.modifiers):
        if mod.type == "EDGE_SPLIT" and "cohesion" in mod.name:
            obj.modifiers.remove(mod)

    # Add Edge Split modifier at 30° for the low-poly hard-edge look
    mod_es = obj.modifiers.new(name="EDGESPLIT_cohesion", type="EDGE_SPLIT")
    mod_es.split_angle = math.radians(30.0)
    mod_es.use_edge_angle = True
    mod_es.use_edge_sharp = True

    mesh.update()
    print("    Flat shading enforced + EdgeSplit modifier added.")


# ===========================================================================
# Stage 3 — Color palette remapping
# ===========================================================================

def build_frequency_color_map(obj, palette_rgb, palette_lab):
    """
    Build a mapping from each unique (quantized) color found in the mesh to
    a palette RGB triplet.

    Strategy:
      - Sort model's unique colors by frequency (most common first)
      - Map model_color[i] → palette_color[i] for the top N colors
      - For remaining colors, use nearest-neighbor in LAB space
    Returns a dict: quantized_rgb_tuple → (r, g, b) float palette color
    """
    face_colors_raw = extract_face_color_floats(obj)

    # Count quantized colors
    color_counter = Counter()
    for fc in face_colors_raw:
        qc = quantize_color(*fc)
        color_counter[qc] += 1

    if not color_counter:
        return {}

    # Sort model colors by frequency (most common first)
    model_colors_sorted = [c for c, _ in color_counter.most_common()]

    color_map = {}
    n_direct = min(len(model_colors_sorted), len(palette_rgb))

    # Direct frequency mapping for top N colors
    for i in range(n_direct):
        color_map[model_colors_sorted[i]] = palette_rgb[i]

    # LAB nearest-neighbor for any remaining model colors
    for qc in model_colors_sorted[n_direct:]:
        r = qc[0] / 255.0
        g = qc[1] / 255.0
        b = qc[2] / 255.0
        idx = nearest_palette_color_lab(r, g, b, palette_lab)
        color_map[qc] = palette_rgb[idx]

    return color_map


def remap_vertex_colors(obj, color_map, palette_rgb, palette_lab):
    """Remap vertex color attribute values using the frequency color map."""
    mesh = obj.data

    color_attr = None
    if hasattr(mesh, "color_attributes") and len(mesh.color_attributes) > 0:
        color_attr = mesh.color_attributes[0]
    elif hasattr(mesh, "vertex_colors") and len(mesh.vertex_colors) > 0:
        color_attr = mesh.vertex_colors[0]

    if color_attr is None:
        return 0

    remapped = 0
    try:
        for datum in color_attr.data:
            c = datum.color  # RGBA
            qc = quantize_color(c[0], c[1], c[2])
            if qc in color_map:
                new_c = color_map[qc]
            else:
                idx = nearest_palette_color_lab(c[0], c[1], c[2], palette_lab)
                new_c = palette_rgb[idx]
            datum.color = (new_c[0], new_c[1], new_c[2], c[3])  # preserve alpha
            remapped += 1
    except Exception as exc:
        print(f"    [WARNING] Vertex color remap error: {exc}")

    return remapped


def remap_texture_colors(obj, color_map, palette_rgb, palette_lab):
    """
    Replace texture-based materials with a small (64×64) remapped image
    where each texel is mapped to the nearest palette color.
    """
    mesh = obj.data
    if not mesh.materials:
        return 0

    remapped_mats = 0
    for mat_idx, mat in enumerate(mesh.materials):
        if mat is None or not mat.use_nodes:
            continue

        tex_image = None
        principled = None

        for node in mat.node_tree.nodes:
            if node.type == "BSDF_PRINCIPLED":
                principled = node
                inp = node.inputs.get("Base Color")
                if inp and inp.is_linked:
                    for link in inp.links:
                        if link.from_node.type == "TEX_IMAGE":
                            tex_image = link.from_node.image
                            break
                break

        if tex_image is None or not tex_image.has_data:
            continue

        w, h = tex_image.size
        if w == 0 or h == 0:
            continue

        print(f"    Remapping texture '{tex_image.name}' ({w}x{h}) → 64x64 palette image")

        # Create a new 64x64 image with remapped palette colors
        new_w, new_h = 64, 64
        new_img_name = f"remapped_{mat.name}"
        new_img = bpy.data.images.new(new_img_name, width=new_w, height=new_h)
        new_pixels = [0.0] * (new_w * new_h * 4)

        orig_pixels = list(tex_image.pixels)

        for py in range(new_h):
            for px in range(new_w):
                # Map new pixel coord to original image coord
                src_x = int(px / new_w * w)
                src_y = int(py / new_h * h)
                src_idx = (src_y * w + src_x) * 4

                if src_idx + 2 < len(orig_pixels):
                    r = orig_pixels[src_idx]
                    g = orig_pixels[src_idx + 1]
                    b = orig_pixels[src_idx + 2]
                    a = orig_pixels[src_idx + 3]
                else:
                    r, g, b, a = 0.5, 0.5, 0.5, 1.0

                # Find nearest palette color in LAB space
                idx = nearest_palette_color_lab(r, g, b, palette_lab)
                new_c = palette_rgb[idx]

                dst_idx = (py * new_w + px) * 4
                new_pixels[dst_idx]     = new_c[0]
                new_pixels[dst_idx + 1] = new_c[1]
                new_pixels[dst_idx + 2] = new_c[2]
                new_pixels[dst_idx + 3] = a

        new_img.pixels = new_pixels

        # Replace texture node with the remapped image
        for node in mat.node_tree.nodes:
            if node.type == "TEX_IMAGE" and node.image == tex_image:
                node.image = new_img
                break

        remapped_mats += 1

    return remapped_mats


def remap_material_colors(obj, color_map, palette_rgb, palette_lab):
    """
    For flat-color materials: update the Base Color on the Principled BSDF
    to the nearest palette color.
    """
    mesh = obj.data
    if not mesh.materials:
        return 0

    remapped = 0
    for mat in mesh.materials:
        if mat is None or not mat.use_nodes:
            continue

        for node in mat.node_tree.nodes:
            if node.type == "BSDF_PRINCIPLED":
                inp = node.inputs.get("Base Color")
                if inp and not inp.is_linked:
                    c = inp.default_value
                    qc = quantize_color(c[0], c[1], c[2])
                    if qc in color_map:
                        new_c = color_map[qc]
                    else:
                        idx = nearest_palette_color_lab(c[0], c[1], c[2], palette_lab)
                        new_c = palette_rgb[idx]
                    inp.default_value = (new_c[0], new_c[1], new_c[2], 1.0)
                    remapped += 1
                break

    return remapped


def remap_colors(obj, fingerprint: dict, palette_rgb, palette_lab):
    """
    Dispatch color remapping based on how the mesh stores its colors.
    """
    color_mode = get_mesh_color_mode(obj)
    print(f"    Color mode detected: {color_mode}")

    color_map = build_frequency_color_map(obj, palette_rgb, palette_lab)
    print(f"    Color mapping built: {len(color_map)} unique colors mapped")

    if color_mode == "vertex":
        n = remap_vertex_colors(obj, color_map, palette_rgb, palette_lab)
        print(f"    Remapped {n} vertex color samples")
    elif color_mode == "texture":
        n = remap_texture_colors(obj, color_map, palette_rgb, palette_lab)
        print(f"    Remapped {n} texture(s)")
    elif color_mode == "material":
        n = remap_material_colors(obj, color_map, palette_rgb, palette_lab)
        print(f"    Remapped {n} material color(s)")
    else:
        print("    No color data found to remap.")


# ===========================================================================
# Stage 4 — Material simplification (Quaternius flat-color look)
# ===========================================================================

def simplify_materials(obj, palette_rgb, palette_lab):
    """
    Remove all texture-based materials and replace with flat-color Principled
    BSDF materials. One material per unique palette color used in the mesh.
    Slight roughness variation per material for visual interest.
    """
    mesh = obj.data

    # First remap all face colors to palette colors (LAB nearest neighbor)
    # and record which palette index each face maps to.
    face_colors_raw = extract_face_color_floats(obj)
    face_palette_idx = []
    for fc in face_colors_raw:
        idx = nearest_palette_color_lab(fc[0], fc[1], fc[2], palette_lab)
        face_palette_idx.append(idx)

    # Collect the unique palette indices used by this mesh
    used_indices = sorted(set(face_palette_idx))
    print(f"    Simplifying materials: {len(used_indices)} unique palette color(s) used")

    # Build a mapping: palette_idx → new material
    # Roughness varies slightly between 0.75 and 0.85 for visual interest
    new_materials = {}
    for i, pal_idx in enumerate(used_indices):
        rgb = palette_rgb[pal_idx]
        roughness = 0.75 + (i % 5) * 0.025  # steps: 0.75, 0.775, 0.80, 0.825, 0.85

        mat_name = f"cohesive_{pal_idx:03d}"
        # Reuse if already created in this session
        mat = bpy.data.materials.get(mat_name)
        if mat is None:
            mat = bpy.data.materials.new(name=mat_name)
            mat.use_nodes = True
            mat.node_tree.nodes.clear()

            # Output node
            out_node = mat.node_tree.nodes.new("ShaderNodeOutputMaterial")
            out_node.location = (400, 0)

            # Principled BSDF
            bsdf = mat.node_tree.nodes.new("ShaderNodeBsdfPrincipled")
            bsdf.location = (0, 0)

            # Base Color
            bsdf.inputs["Base Color"].default_value = (rgb[0], rgb[1], rgb[2], 1.0)

            # Metallic
            bsdf.inputs["Metallic"].default_value = 0.0

            # Roughness
            bsdf.inputs["Roughness"].default_value = roughness

            # Specular / IOR — handle both old and new Blender 4.x API
            if "Specular" in bsdf.inputs:
                bsdf.inputs["Specular"].default_value = 0.2
            if "IOR" in bsdf.inputs:
                bsdf.inputs["IOR"].default_value = 1.3
            if "Specular IOR Level" in bsdf.inputs:
                bsdf.inputs["Specular IOR Level"].default_value = 0.2

            # Emission strength = 0 (no emission)
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = 0.0
            if "Emission" in bsdf.inputs:
                bsdf.inputs["Emission"].default_value = (0.0, 0.0, 0.0, 1.0)

            # Link BSDF → Output
            mat.node_tree.links.new(bsdf.outputs["BSDF"], out_node.inputs["Surface"])

        new_materials[pal_idx] = mat

    # Build palette_idx → slot_index mapping for this mesh
    # Clear all existing materials and add only the needed palette materials
    mesh.materials.clear()

    pal_idx_to_slot = {}
    for slot_i, pal_idx in enumerate(used_indices):
        mesh.materials.append(new_materials[pal_idx])
        pal_idx_to_slot[pal_idx] = slot_i

    # Assign each polygon to the correct material slot
    for poly_i, poly in enumerate(mesh.polygons):
        if poly_i < len(face_palette_idx):
            pal_idx = face_palette_idx[poly_i]
            poly.material_index = pal_idx_to_slot.get(pal_idx, 0)
        else:
            poly.material_index = 0

    mesh.update()
    print(f"    Materials simplified: {len(used_indices)} cohesive material(s) assigned")


# ===========================================================================
# Stage 5 — Scale normalization
# ===========================================================================

def normalize_scale(obj, fingerprint: dict):
    """
    Scale the model uniformly so that its largest bounding-box dimension
    matches the largest median extent in the fingerprint.
    Applies the transform afterward.
    """
    scale_stats = fingerprint["scale_stats"]
    ref_x = scale_stats.get("median_extent_x", 1.0)
    ref_y = scale_stats.get("median_extent_y", 1.0)
    ref_z = scale_stats.get("median_extent_z", 1.0)
    ref_largest = max(ref_x, ref_y, ref_z)

    if ref_largest <= 0.0:
        print("    Scale normalization skipped (zero reference extent).")
        return

    # Compute current world-space bounding box
    bb_verts = [obj.matrix_world @ mathutils.Vector(v) for v in obj.bound_box]
    xs = [v.x for v in bb_verts]
    ys = [v.y for v in bb_verts]
    zs = [v.z for v in bb_verts]

    cur_x = max(xs) - min(xs)
    cur_y = max(ys) - min(ys)
    cur_z = max(zs) - min(zs)
    cur_largest = max(cur_x, cur_y, cur_z)

    if cur_largest <= 0.0:
        print("    Scale normalization skipped (zero current extent).")
        return

    scale_factor = ref_largest / cur_largest
    print(f"    Scale: current largest={cur_largest:.4f}, "
          f"reference largest={ref_largest:.4f}, "
          f"factor={scale_factor:.4f}")

    obj.scale *= scale_factor

    # Apply transforms (equivalent to Ctrl+A → Apply All Transforms)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)


# ===========================================================================
# Stage 6 — Edge cleanup
# ===========================================================================

def cleanup_edges(obj):
    """
    Merge doubles (remove duplicate vertices), recalculate normals outward,
    and remove loose geometry (vertices / edges not connected to faces).
    """
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)

    mesh = obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh)

    # Merge by distance (remove doubles)
    verts_before = len(bm.verts)
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=0.001)
    verts_after = len(bm.verts)
    merged = verts_before - verts_after
    if merged > 0:
        print(f"    Merged {merged} duplicate vertex/vertices")

    # Recalculate normals pointing outward
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])

    # Remove loose vertices (not connected to any edge)
    loose_verts = [v for v in bm.verts if not v.link_edges]
    if loose_verts:
        bmesh.ops.delete(bm, geom=loose_verts, context="VERTS")
        print(f"    Removed {len(loose_verts)} loose vertex/vertices")

    # Remove loose edges (not connected to any face)
    loose_edges = [e for e in bm.edges if not e.link_faces]
    if loose_edges:
        bmesh.ops.delete(bm, geom=loose_edges, context="EDGES")
        print(f"    Removed {len(loose_edges)} loose edge(s)")

    bm.to_mesh(mesh)
    bm.free()
    mesh.update()

    print("    Edge cleanup complete.")


# ===========================================================================
# Stage 7 — GLB export
# ===========================================================================

def export_glb(model_path: Path, output_dir: Path, preserve_animations: bool) -> Path:
    """
    Export the current Blender scene as a .glb file.
    Output filename: <original_stem>_cohesive.glb
    """
    out_stem = model_path.stem + "_cohesive"
    out_file = output_dir / (out_stem + ".glb")
    out_file.parent.mkdir(parents=True, exist_ok=True)

    print(f"    Exporting → {out_file}")

    export_kwargs = dict(
        filepath=str(out_file),
        export_format="GLB",
        export_apply=True,          # bake modifiers
        export_colors=True,         # include vertex colors
        export_materials="EXPORT",  # include materials
        use_selection=False,        # export entire scene
    )

    # Animation export
    if preserve_animations:
        export_kwargs["export_animations"] = True
        export_kwargs["export_skins"] = True
        export_kwargs["export_morph"] = True
    else:
        export_kwargs["export_animations"] = False

    try:
        bpy.ops.export_scene.gltf(**export_kwargs)
    except TypeError:
        # Blender 4.x may have slightly different parameter names; fall back
        # to minimal arguments to avoid keyword errors.
        print("    [WARNING] Some export kwargs not supported; retrying with minimal args.")
        bpy.ops.export_scene.gltf(
            filepath=str(out_file),
            export_format="GLB",
            export_apply=True,
        )

    return out_file


# ===========================================================================
# Animation preservation helper
# ===========================================================================

def preserve_object_animations(scene_objects, preserve: bool):
    """
    When preserve=False, unlink actions from all objects so they don't
    pollute the export. When preserve=True, do nothing — animations are
    already attached and will be exported by GLTF exporter.
    """
    if preserve:
        return  # Nothing to do; exporter handles it

    for obj in scene_objects:
        if obj.animation_data:
            obj.animation_data.action = None


# ===========================================================================
# Per-model transformation pipeline
# ===========================================================================

def transform_model(
    model_path: Path,
    fingerprint: dict,
    output_dir: Path,
    preserve_animations: bool,
    aggressive: bool,
    palette_rgb: list,
    palette_lab: list,
):
    """
    Full transformation pipeline for a single model file.
    Returns the output Path on success, or None on failure.
    """
    name = model_path.name
    print(f"\n{'─' * 60}")
    print(f"  Processing: {name}")
    print(f"{'─' * 60}")

    # --- Fresh scene ---
    clear_scene()

    # --- Import ---
    print(f"  Importing: {model_path}")
    if not import_model(str(model_path)):
        print(f"  [SKIP] Import failed: {name}")
        return None

    scene_objects = list(bpy.context.scene.objects)
    mesh_objects = [o for o in scene_objects if o.type == "MESH"]

    if not mesh_objects:
        print(f"  [SKIP] No mesh objects in: {name}")
        return None

    print(f"  Found {len(mesh_objects)} mesh object(s)")

    # Detect animations before any modifications
    anim_present = has_animations()
    if anim_present:
        print(f"  Animations detected: {'preserving' if preserve_animations else 'stripping'}")
    else:
        print("  No animations detected.")

    # Strip animation data early if not preserving (keeps pipeline clean)
    if not preserve_animations:
        preserve_object_animations(scene_objects, preserve=False)

    # Faces before transformation
    total_faces_before = sum(len(o.data.polygons) for o in mesh_objects)

    # ---------------------------------------------------------------
    # Apply stages to each mesh object
    # ---------------------------------------------------------------
    for obj in mesh_objects:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)

        print(f"\n  Mesh: '{obj.name}' ({len(obj.data.polygons)} faces)")

        try:
            # Stage 1: Poly count normalization
            print("  [Stage 1] Poly count normalization")
            normalize_poly_count(obj, fingerprint, aggressive)

            # Stage 2: Flat shading enforcement
            print("  [Stage 2] Flat shading enforcement")
            enforce_flat_shading(obj, fingerprint)

            # Stage 3: Color palette remapping
            print("  [Stage 3] Color palette remapping")
            remap_colors(obj, fingerprint, palette_rgb, palette_lab)

            # Stage 4: Material simplification
            print("  [Stage 4] Material simplification")
            simplify_materials(obj, palette_rgb, palette_lab)

            # Stage 5: Scale normalization
            print("  [Stage 5] Scale normalization")
            normalize_scale(obj, fingerprint)

            # Stage 6: Edge cleanup
            print("  [Stage 6] Edge cleanup")
            cleanup_edges(obj)

        except Exception as exc:
            print(f"  [ERROR] Transformation failed on '{obj.name}': {exc}")
            traceback.print_exc()
            print("  Continuing with remaining meshes...")

    # Faces after transformation
    mesh_objects_after = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    total_faces_after = sum(len(o.data.polygons) for o in mesh_objects_after)

    print(f"\n  Summary: {total_faces_before} faces → {total_faces_after} faces")

    # Stage 7: Export
    print("  [Stage 7] Exporting GLB")
    try:
        out_path = export_glb(model_path, output_dir, preserve_animations)
        print(f"  [OK] Exported: {out_path}")
        return out_path
    except Exception as exc:
        print(f"  [ERROR] Export failed: {exc}")
        traceback.print_exc()
        return None


# ===========================================================================
# Main entry point
# ===========================================================================

def main():
    args = parse_args()

    print("=" * 60)
    print("Asset Cohesion Pipeline — Stage 2: Style Transformer")
    print("=" * 60)
    print(f"Fingerprint  : {args.fingerprint}")
    print(f"Input        : {args.input}")
    print(f"Output dir   : {args.output_dir}")
    print(f"Preserve anim: {args.preserve_animations}")
    print(f"Aggressive   : {args.aggressive}")
    print()

    # Load fingerprint
    try:
        fingerprint = load_fingerprint(args.fingerprint)
    except Exception as exc:
        print(f"[FATAL] Could not load fingerprint: {exc}")
        sys.exit(1)

    print(
        f"[INFO] Fingerprint loaded: "
        f"median_faces={fingerprint['poly_stats']['median_face_count']:.0f}, "
        f"palette={len(fingerprint['color_palette'])} colors, "
        f"flat_pct={fingerprint['shading']['flat_percentage']:.2%}"
    )

    # Build palette LAB cache (do once, reuse per model)
    palette_rgb, palette_lab, palette_hex = build_palette_labs(fingerprint)
    print(f"[INFO] Palette order: {palette_hex[:8]} ...")

    # Find model files
    try:
        model_files = find_model_files(args.input)
    except ValueError as exc:
        print(f"[FATAL] {exc}")
        sys.exit(1)

    if not model_files:
        print("[WARNING] No supported model files found. Nothing to do.")
        sys.exit(0)

    print(f"[INFO] Found {len(model_files)} model file(s) to transform.\n")

    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    # Process each model
    success_count = 0
    fail_count = 0
    results = []

    for idx, model_path in enumerate(model_files, start=1):
        print(f"\n[{idx}/{len(model_files)}]")
        try:
            out = transform_model(
                model_path=model_path,
                fingerprint=fingerprint,
                output_dir=output_dir,
                preserve_animations=args.preserve_animations,
                aggressive=args.aggressive,
                palette_rgb=palette_rgb,
                palette_lab=palette_lab,
            )
            if out is not None:
                success_count += 1
                results.append({"input": str(model_path), "output": str(out), "status": "ok"})
            else:
                fail_count += 1
                results.append({"input": str(model_path), "output": None, "status": "failed"})
        except Exception as exc:
            print(f"[ERROR] Unhandled error processing '{model_path.name}': {exc}")
            traceback.print_exc()
            fail_count += 1
            results.append({"input": str(model_path), "output": None, "status": "error",
                            "error": str(exc)})

    # Write a run summary JSON next to the output
    summary_path = output_dir / "transform_summary.json"
    with open(summary_path, "w", encoding="utf-8") as fh:
        json.dump({
            "fingerprint": args.fingerprint,
            "input": args.input,
            "output_dir": args.output_dir,
            "preserve_animations": args.preserve_animations,
            "aggressive": args.aggressive,
            "total": len(model_files),
            "success": success_count,
            "failed": fail_count,
            "results": results,
        }, fh, indent=2)

    print("\n" + "=" * 60)
    print(f"Transform complete: {success_count} succeeded, {fail_count} failed")
    print(f"Summary written to: {summary_path}")
    print("=" * 60)


if __name__ == "__main__":
    main()
