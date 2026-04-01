#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════════
# ASSET COHESION PIPELINE — Batch Runner
# ═══════════════════════════════════════════════════════════════════════════════
#
# Usage:
#   ./run_pipeline.sh [--aggressive] [--skip-analyze] [--skip-transform]
#
# This script:
#   1. Analyzes Quaternius models to extract the reference style fingerprint
#   2. Transforms Kenney and Majadroid models to match the Quaternius style
#   3. Copies Quaternius models as-is (they're already the reference)
#   4. Exports everything as Godot-ready .glb files
#
# Prerequisites:
#   - Blender 4.x installed and on PATH
#   - Raw assets extracted in ../raw/
# ═══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RAW_DIR="$(cd "$SCRIPT_DIR/../raw" && pwd)"
OUTPUT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)/processed"
FINGERPRINT="$OUTPUT_DIR/quaternius_fingerprint.json"

# Parse flags
AGGRESSIVE=""
SKIP_ANALYZE=false
SKIP_TRANSFORM=false
for arg in "$@"; do
    case $arg in
        --aggressive) AGGRESSIVE="--aggressive" ;;
        --skip-analyze) SKIP_ANALYZE=true ;;
        --skip-transform) SKIP_TRANSFORM=true ;;
    esac
done

mkdir -p "$OUTPUT_DIR"/{quaternius,kenney,majadroid}

echo "╔═══════════════════════════════════════════════════════════════╗"
echo "║           ASSET COHESION PIPELINE                           ║"
echo "╚═══════════════════════════════════════════════════════════════╝"
echo ""
echo "Raw assets:  $RAW_DIR"
echo "Output:      $OUTPUT_DIR"
echo "Blender:     $(blender --version 2>/dev/null | head -1)"
echo ""

# ─── STAGE 1: Analyze Quaternius Reference Style ────────────────────────────
if [ "$SKIP_ANALYZE" = false ]; then
    echo "━━━ STAGE 1: Analyzing Quaternius reference style ━━━"
    echo ""

    # We feed ALL Quaternius packs as the reference
    # Create a temporary directory with symlinks to all Quaternius FBX files
    QREF_DIR="$OUTPUT_DIR/.quaternius_reference"
    rm -rf "$QREF_DIR"
    mkdir -p "$QREF_DIR"

    # Tanks FBX
    for f in "$RAW_DIR"/quaternius-tanks/*/FBX/*.fbx; do
        [ -f "$f" ] && ln -sf "$f" "$QREF_DIR/tank_$(basename "$f")"
    done

    # Mech FBX (flat colors variant)
    for f in "$RAW_DIR"/quaternius-mech/*/"Flat Colors"/FBX/*.fbx; do
        [ -f "$f" ] && ln -sf "$f" "$QREF_DIR/mech_$(basename "$f")"
    done

    # Space Kit FBX (all subdirectories)
    find "$RAW_DIR/quaternius-space" -name "*.fbx" | while read -r f; do
        bn="space_$(basename "$f")"
        # Avoid collisions
        if [ -e "$QREF_DIR/$bn" ]; then
            bn="space2_$(basename "$f")"
        fi
        ln -sf "$f" "$QREF_DIR/$bn"
    done

    echo "Reference models: $(find "$QREF_DIR" -name "*.fbx" | wc -l) FBX files"
    echo ""

    blender --background --python "$SCRIPT_DIR/analyze_style.py" -- \
        --input-dir "$QREF_DIR" \
        --output "$FINGERPRINT" \
        2>&1 | grep -E "^\[|^Analyzing|^Style|^  |^Processed|faces|colors|ERROR" | head -80

    echo ""
    if [ -f "$FINGERPRINT" ]; then
        echo "Fingerprint saved: $FINGERPRINT"
        echo "  Models analyzed: $(python3 -c "import json; d=json.load(open('$FINGERPRINT')); print(d.get('model_count', '?'))")"
        echo "  Palette colors:  $(python3 -c "import json; d=json.load(open('$FINGERPRINT')); print(len(d.get('color_palette', [])))")"
        echo "  Median faces:    $(python3 -c "import json; d=json.load(open('$FINGERPRINT')); print(d.get('poly_stats', {}).get('median_face_count', '?'))")"
    else
        echo "ERROR: Fingerprint not generated!"
        exit 1
    fi
    echo ""
else
    echo "━━━ STAGE 1: Skipped (using existing fingerprint) ━━━"
    if [ ! -f "$FINGERPRINT" ]; then
        echo "ERROR: No fingerprint found at $FINGERPRINT"
        exit 1
    fi
fi

# ─── STAGE 2: Transform Non-Quaternius Models ──────────────────────────────
if [ "$SKIP_TRANSFORM" = false ]; then
    echo "━━━ STAGE 2: Transforming models to match Quaternius style ━━━"
    echo ""

    # Transform Kenney Space Kit (GLB format — most compatible)
    echo "── Kenney Space Kit ──"
    KENNEY_GLTF="$RAW_DIR/kenney-space/Models/GLTF format"
    if [ -d "$KENNEY_GLTF" ]; then
        echo "  Input: $(find "$KENNEY_GLTF" -name "*.glb" | wc -l) GLB files"
        blender --background --python "$SCRIPT_DIR/transform_style.py" -- \
            --fingerprint "$FINGERPRINT" \
            --input "$KENNEY_GLTF" \
            --output-dir "$OUTPUT_DIR/kenney" \
            $AGGRESSIVE \
            2>&1 | grep -E "^\[|^Processing|^  |^Transformed|^ERROR|faces|colors|Skipping" | head -60
        echo "  Output: $(find "$OUTPUT_DIR/kenney" -name "*.glb" | wc -l) cohesive GLB files"
    else
        echo "  WARNING: Kenney GLTF directory not found"
    fi
    echo ""

    # Transform Majadroid (OBJ format — they have individual OBJ files)
    echo "── Majadroid Spaceships ──"
    MAJADROID_OBJ="$RAW_DIR/majadroid-ships/LowPoly-Spaceships-By-Majadroid/obj-files"
    if [ -d "$MAJADROID_OBJ" ]; then
        echo "  Input: $(find "$MAJADROID_OBJ" -name "*.obj" | wc -l) OBJ files"
        blender --background --python "$SCRIPT_DIR/transform_style.py" -- \
            --fingerprint "$FINGERPRINT" \
            --input "$MAJADROID_OBJ" \
            --output-dir "$OUTPUT_DIR/majadroid" \
            $AGGRESSIVE \
            2>&1 | grep -E "^\[|^Processing|^  |^Transformed|^ERROR|faces|colors|Skipping" | head -60
        echo "  Output: $(find "$OUTPUT_DIR/majadroid" -name "*.glb" | wc -l) cohesive GLB files"
    else
        echo "  WARNING: Majadroid OBJ directory not found"
    fi
    echo ""

    # Copy Quaternius as-is (they're already the reference style)
    # But still export as GLB for format consistency
    echo "── Quaternius (reference — exporting as GLB) ──"
    QREF_DIR="$OUTPUT_DIR/.quaternius_reference"
    if [ -d "$QREF_DIR" ]; then
        blender --background --python "$SCRIPT_DIR/transform_style.py" -- \
            --fingerprint "$FINGERPRINT" \
            --input "$QREF_DIR" \
            --output-dir "$OUTPUT_DIR/quaternius" \
            --preserve-animations \
            2>&1 | grep -E "^\[|^Processing|^  |^Transformed|^ERROR|faces|colors|Skipping" | head -60
        echo "  Output: $(find "$OUTPUT_DIR/quaternius" -name "*.glb" | wc -l) GLB files"
    fi
    echo ""
else
    echo "━━━ STAGE 2: Skipped ━━━"
fi

# ─── STAGE 3: Summary ──────────────────────────────────────────────────────
echo "━━━ PIPELINE COMPLETE ━━━"
echo ""
echo "Output directory: $OUTPUT_DIR"
echo ""
echo "  Quaternius: $(find "$OUTPUT_DIR/quaternius" -name "*.glb" 2>/dev/null | wc -l) models"
echo "  Kenney:     $(find "$OUTPUT_DIR/kenney" -name "*.glb" 2>/dev/null | wc -l) models"
echo "  Majadroid:  $(find "$OUTPUT_DIR/majadroid" -name "*.glb" 2>/dev/null | wc -l) models"
echo "  Total:      $(find "$OUTPUT_DIR" -name "*.glb" 2>/dev/null | wc -l) cohesive models"
echo ""
echo "Godot integration files:"
echo "  Shader:  $SCRIPT_DIR/cohesive_flat.gdshader"
echo "  Factory: $SCRIPT_DIR/CohesiveMaterialFactory.gd"
echo ""
echo "To use in Godot:"
echo "  1. Copy cohesive_flat.gdshader to your project's shaders/ folder"
echo "  2. Copy CohesiveMaterialFactory.gd to your scripts/ folder"
echo "  3. Import the .glb files from $OUTPUT_DIR/"
echo "  4. Call CohesiveMaterialFactory.apply_to_scene(your_model, team_color)"
