// ═══════════════════════════════════════════════════════════════════════════════
// Terrain PBR Uber-Shader — Cordite Wars: Six Fronts
// ═══════════════════════════════════════════════════════════════════════════════
//
// 5-layer procedural PBR material system with:
//   • Slope/height/moisture/noise-based layer blending
//   • Triplanar projection on steep surfaces (>45°)
//   • Multi-octave FBM noise for surface variation
//   • Procedural normal perturbation for micro-detail
//   • Per-layer roughness variation
//   • Macro variation overlay (large-scale color shifts)
//   • Vertex ambient occlusion
//   • Height-based color grading
//
// Compatible with gl_compatibility renderer (no tessellation/compute).
// All surface detail is generated analytically — no external texture files.
//
// Vertex color channels:
//   COLOR.rgb = biome tint hint
//   COLOR.a   = moisture hint (0 = dry, 1 = wet/near water)
// ═══════════════════════════════════════════════════════════════════════════════

shader_type spatial;
render_mode cull_back, diffuse_burley, specular_schlick_ggx;

// ─── Uniforms ─────────────────────────────────────────────────────────────────

// Quality tier control: allows reducing active layers for performance.
uniform int u_layer_count : hint_range(2, 5) = 5;

// Triplanar blend sharpness: higher = sharper transition between projections.
uniform float u_triplanar_sharpness : hint_range(1.0, 8.0) = 4.0;

// Macro variation noise scale: controls large-scale color shift frequency.
uniform float u_macro_scale : hint_range(0.01, 0.1) = 0.03;

// ─── Varyings ─────────────────────────────────────────────────────────────────

varying vec3 v_world_pos;
varying vec3 v_world_normal;
varying vec3 v_biome_color;
varying float v_moisture;
varying float v_elevation;
varying float v_vertex_ao;

// ═══════════════════════════════════════════════════════════════════════════════
// NOISE FUNCTIONS
// ═══════════════════════════════════════════════════════════════════════════════

// High-quality hash — avoids visible grid artifacts from simpler hashes.
float hash(vec2 p) {
    p = fract(p * vec2(443.897, 441.423));
    p += dot(p, p + 19.19);
    return fract(p.x * p.y);
}

// 3D hash for triplanar variation.
float hash3(vec3 p) {
    p = fract(p * vec3(443.897, 441.423, 437.195));
    p += dot(p, p.yzx + 19.19);
    return fract((p.x + p.y) * p.z);
}

// Smooth bicubic value noise with quintic interpolant (C2 continuous).
float valueNoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

// Multi-octave Fractal Brownian Motion — natural surface variation.
// 5 octaves with lacunarity ~2.13 and persistence 0.5.
float fbm(vec2 p) {
    float v = 0.0;
    float a = 0.5;
    vec2 s = vec2(1.0);
    for (int i = 0; i < 5; i++) {
        v += a * valueNoise(p * s);
        s *= 2.13;
        a *= 0.5;
    }
    return v;
}

// 3-octave FBM for cheaper sampling where full detail isn't needed.
float fbm3(vec2 p) {
    float v = 0.0;
    float a = 0.5;
    vec2 s = vec2(1.0);
    for (int i = 0; i < 3; i++) {
        v += a * valueNoise(p * s);
        s *= 2.13;
        a *= 0.5;
    }
    return v;
}

// Ridged FBM — creates eroded channels and rock striations.
float ridgedFbm(vec2 p) {
    float v = 0.0;
    float a = 0.5;
    vec2 s = vec2(1.0);
    for (int i = 0; i < 4; i++) {
        float n = valueNoise(p * s);
        v += a * (1.0 - abs(n * 2.0 - 1.0));
        s *= 2.07;
        a *= 0.48;
    }
    return v;
}

// Voronoi-style cellular noise for cracked/rocky patterns.
float cellNoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    float min_dist = 1.0;
    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            vec2 neighbor = vec2(float(x), float(y));
            vec2 point = vec2(hash(i + neighbor), hash(i + neighbor + vec2(31.0, 17.0)));
            vec2 diff = neighbor + point - f;
            float dist = length(diff);
            min_dist = min(min_dist, dist);
        }
    }
    return min_dist;
}

// ═══════════════════════════════════════════════════════════════════════════════
// PROCEDURAL NORMAL PERTURBATION
// ═══════════════════════════════════════════════════════════════════════════════

// Generates a tangent-space normal from a procedural height field.
// Creates bumpy micro-surface lighting without a normal-map texture.
vec3 proceduralNormal(vec2 pos, float scale, float strength) {
    float eps = 0.5;
    float hL = fbm((pos + vec2(-eps, 0.0)) * scale);
    float hR = fbm((pos + vec2( eps, 0.0)) * scale);
    float hD = fbm((pos + vec2(0.0, -eps)) * scale);
    float hU = fbm((pos + vec2(0.0,  eps)) * scale);
    vec3 n = normalize(vec3((hL - hR) * strength, 1.0, (hD - hU) * strength));
    return n;
}

// Cheaper 3-octave normal for layers that don't need full detail.
vec3 proceduralNormalFast(vec2 pos, float scale, float strength) {
    float eps = 0.6;
    float hL = fbm3((pos + vec2(-eps, 0.0)) * scale);
    float hR = fbm3((pos + vec2( eps, 0.0)) * scale);
    float hD = fbm3((pos + vec2(0.0, -eps)) * scale);
    float hU = fbm3((pos + vec2(0.0,  eps)) * scale);
    vec3 n = normalize(vec3((hL - hR) * strength, 1.0, (hD - hU) * strength));
    return n;
}

// ═══════════════════════════════════════════════════════════════════════════════
// VERTEX STAGE
// ═══════════════════════════════════════════════════════════════════════════════

void vertex() {
    // World-space position for triplanar and noise sampling.
    v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;

    // World-space normal for slope calculations.
    v_world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);

    // Biome hints from vertex color.
    v_biome_color = COLOR.rgb;
    v_moisture = COLOR.a;

    // Elevation for height-based blending.
    v_elevation = VERTEX.y;

    // Vertex ambient occlusion: computed from elevation relative to neighbors.
    // Lower vertices in valleys get darker AO. This is a simple approximation
    // that works well with the mesh generation pass which can bake proper AO.
    // Range: 0.4 (deep valley) to 1.0 (exposed peak).
    float ao_factor = clamp(v_elevation * 0.08 + 0.6, 0.4, 1.0);
    v_vertex_ao = ao_factor;
}

// ═══════════════════════════════════════════════════════════════════════════════
// TRIPLANAR PROJECTION
// ═══════════════════════════════════════════════════════════════════════════════

// Computes triplanar blend weights from world normal.
// Sharpness controls how quickly the blend transitions between axes.
vec3 triplanarWeights(vec3 normal, float sharpness) {
    vec3 w = abs(normal);
    w = pow(w, vec3(sharpness));
    return w / (w.x + w.y + w.z + 0.0001);
}

// Samples FBM using triplanar projection (for steep surfaces).
float triplanarFbm(vec3 pos, vec3 weights, float scale) {
    float xy = fbm(pos.xy * scale);
    float xz = fbm(pos.xz * scale);
    float yz = fbm(pos.yz * scale);
    return xy * weights.z + xz * weights.y + yz * weights.x;
}

// Samples ridged FBM using triplanar projection.
float triplanarRidged(vec3 pos, vec3 weights, float scale) {
    float xy = ridgedFbm(pos.xy * scale);
    float xz = ridgedFbm(pos.xz * scale);
    float yz = ridgedFbm(pos.yz * scale);
    return xy * weights.z + xz * weights.y + yz * weights.x;
}

// Samples cell noise using triplanar projection.
float triplanarCell(vec3 pos, vec3 weights, float scale) {
    float xy = cellNoise(pos.xy * scale);
    float xz = cellNoise(pos.xz * scale);
    float yz = cellNoise(pos.yz * scale);
    return xy * weights.z + xz * weights.y + yz * weights.x;
}

// ═══════════════════════════════════════════════════════════════════════════════
// MATERIAL LAYER DEFINITIONS
// ═══════════════════════════════════════════════════════════════════════════════

// ─── Layer 0: Bedrock ─────────────────────────────────────────────────────────
// Dark grey/brown, rough, cracked. Appears at high elevation and steep slopes.
// Uses cellular noise for crack patterns and ridged FBM for weathering.

struct LayerResult {
    vec3 albedo;
    vec3 normal_ts;  // tangent-space normal perturbation
    float roughness;
};

// Helper: blend two tangent-space normals using Reoriented Normal Mapping.
vec3 blendNormals(vec3 n1, vec3 n2) {
    n1 = n1 * 2.0 - 1.0;
    n2 = n2 * 2.0 - 1.0;
    return normalize(vec3(n1.xy + n2.xy, n1.z * n2.z)) * 0.5 + 0.5;
}

// ═══════════════════════════════════════════════════════════════════════════════
// FRAGMENT STAGE
// ═══════════════════════════════════════════════════════════════════════════════

void fragment() {
    vec3 pos = v_world_pos;
    vec2 uv = pos.xz;
    vec3 normal = normalize(v_world_normal);

    // ── Slope calculation ─────────────────────────────────────────────────────
    // slope = 0.0 means perfectly flat (normal pointing up)
    // slope = 1.0 means perfectly vertical (cliff face)
    float slope = 1.0 - abs(normal.y);

    // ── Triplanar vs UV projection blend ──────────────────────────────────────
    // Smooth transition at 45° (slope ~0.293 for 45° from vertical = cos(45°))
    // We use slope > 0.707 as the threshold (sin(45°) ≈ 0.707)
    float triplanar_blend = smoothstep(0.55, 0.75, slope);
    vec3 tri_weights = triplanarWeights(normal, u_triplanar_sharpness);

    // ── Noise sampling (adaptive: triplanar on steep, UV on flat) ─────────────
    // Macro noise for large-scale variation
    float n_macro;
    if (triplanar_blend > 0.01) {
        float n_macro_uv = fbm(uv * u_macro_scale * 8.0);
        float n_macro_tri = triplanarFbm(pos, tri_weights, u_macro_scale * 8.0);
        n_macro = mix(n_macro_uv, n_macro_tri, triplanar_blend);
    } else {
        n_macro = fbm(uv * u_macro_scale * 8.0);
    }

    // Meso noise for medium-scale detail
    float n_meso;
    if (triplanar_blend > 0.01) {
        float n_meso_uv = fbm(uv * 1.1);
        float n_meso_tri = triplanarFbm(pos, tri_weights, 1.1);
        n_meso = mix(n_meso_uv, n_meso_tri, triplanar_blend);
    } else {
        n_meso = fbm(uv * 1.1);
    }

    // Micro noise for fine grain detail
    float n_micro;
    if (triplanar_blend > 0.01) {
        float n_micro_uv = fbm(uv * 7.5);
        float n_micro_tri = triplanarFbm(pos, tri_weights, 7.5);
        n_micro = mix(n_micro_uv, n_micro_tri, triplanar_blend);
    } else {
        n_micro = fbm(uv * 7.5);
    }

    // Ridge noise for eroded channels
    float n_ridge;
    if (triplanar_blend > 0.01) {
        float n_ridge_uv = ridgedFbm(uv * 0.6);
        float n_ridge_tri = triplanarRidged(pos, tri_weights, 0.6);
        n_ridge = mix(n_ridge_uv, n_ridge_tri, triplanar_blend);
    } else {
        n_ridge = ridgedFbm(uv * 0.6);
    }

    // Cell noise for cracks (used by bedrock layer)
    float n_cell;
    if (triplanar_blend > 0.01) {
        float n_cell_uv = cellNoise(uv * 3.0);
        float n_cell_tri = triplanarCell(pos, tri_weights, 3.0);
        n_cell = mix(n_cell_uv, n_cell_tri, triplanar_blend);
    } else {
        n_cell = cellNoise(uv * 3.0);
    }

    // ── Normalized elevation for blending (0 = lowest, 1 = highest) ───────────
    // Assumes terrain elevation range of roughly 0–12 units.
    float elev_norm = clamp(v_elevation / 12.0, 0.0, 1.0);

    // Moisture from vertex color alpha + noise perturbation for organic edges.
    float moisture = clamp(v_moisture + (n_macro - 0.5) * 0.2, 0.0, 1.0);

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 0: BEDROCK
    // High elevation, steep slopes. Dark grey/brown, rough, cracked.
    // ══════════════════════════════════════════════════════════════════════════

    // Albedo: dark grey-brown with crack darkening and ridge highlights.
    vec3 bedrock_base = vec3(0.18, 0.16, 0.14);
    vec3 bedrock_light = vec3(0.32, 0.28, 0.24);
    float bedrock_pattern = n_ridge * 0.6 + n_meso * 0.4;
    // Cracks appear as dark lines in the cellular noise.
    float crack_mask = smoothstep(0.05, 0.15, n_cell);
    vec3 bedrock_albedo = mix(bedrock_base * 0.6, bedrock_light, bedrock_pattern);
    bedrock_albedo *= mix(0.5, 1.0, crack_mask); // darken in cracks

    // Normal: strong perturbation from cracks and ridges.
    vec3 bedrock_normal = proceduralNormal(uv, 3.5, 1.2);

    // Roughness: very rough (0.85–0.98), slightly smoother on worn surfaces.
    float bedrock_roughness = mix(0.98, 0.85, bedrock_pattern * crack_mask);

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 1: ROCKY SOIL
    // Mid-high elevation, moderate slopes. Brown/grey mix, medium roughness.
    // ══════════════════════════════════════════════════════════════════════════

    vec3 rocky_base = vec3(0.30, 0.25, 0.20);
    vec3 rocky_grey = vec3(0.38, 0.36, 0.33);
    float rocky_pattern = n_meso * 0.5 + n_micro * 0.3 + n_ridge * 0.2;
    vec3 rocky_albedo = mix(rocky_base, rocky_grey, rocky_pattern);
    // Add small pebble highlights.
    float pebble = smoothstep(0.6, 0.8, n_micro);
    rocky_albedo = mix(rocky_albedo, rocky_albedo * 1.3, pebble * 0.3);

    vec3 rocky_normal = proceduralNormalFast(uv, 5.0, 0.8);
    float rocky_roughness = mix(0.72, 0.88, rocky_pattern);

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 2: GRASS / VEGETATION
    // Low-mid elevation, flat areas. Green with variation, low roughness.
    // ══════════════════════════════════════════════════════════════════════════

    vec3 grass_dark = vec3(0.12, 0.28, 0.08);
    vec3 grass_light = vec3(0.25, 0.45, 0.12);
    vec3 grass_dry = vec3(0.35, 0.38, 0.15);
    float grass_pattern = n_macro * 0.4 + n_meso * 0.35 + n_micro * 0.25;
    // Blend between lush and dry grass based on moisture.
    vec3 grass_lush = mix(grass_dark, grass_light, grass_pattern);
    vec3 grass_albedo = mix(grass_dry, grass_lush, moisture * 0.7 + 0.3);
    // Biome tint influence — allows map-specific grass coloring.
    grass_albedo = mix(grass_albedo, grass_albedo * v_biome_color * 1.5, 0.3);

    vec3 grass_normal = proceduralNormalFast(uv, 8.0, 0.5);
    // Grass is relatively smooth (leaf surfaces).
    float grass_roughness = mix(0.55, 0.72, 1.0 - moisture);

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 3: SAND / DIRT
    // Low elevation, dry areas, paths. Tan/brown, high roughness.
    // ══════════════════════════════════════════════════════════════════════════

    vec3 sand_light = vec3(0.62, 0.52, 0.35);
    vec3 sand_dark = vec3(0.45, 0.36, 0.22);
    float sand_pattern = n_meso * 0.4 + n_micro * 0.4 + n_macro * 0.2;
    vec3 sand_albedo = mix(sand_dark, sand_light, sand_pattern);
    // Wind ripple effect from ridged noise.
    float ripple = ridgedFbm(uv * 2.5) * 0.15;
    sand_albedo += vec3(ripple, ripple * 0.8, ripple * 0.5);
    sand_albedo = clamp(sand_albedo, 0.0, 1.0);

    vec3 sand_normal = proceduralNormalFast(uv, 6.0, 0.4);
    // Sand is quite rough and granular.
    float sand_roughness = mix(0.88, 0.96, sand_pattern);

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 4: RIVER SEDIMENT / MUD
    // Near water, low elevation. Dark brown, wet look, low roughness.
    // ══════════════════════════════════════════════════════════════════════════

    vec3 mud_dark = vec3(0.12, 0.09, 0.06);
    vec3 mud_mid = vec3(0.22, 0.16, 0.10);
    float mud_pattern = n_meso * 0.5 + n_macro * 0.3 + n_micro * 0.2;
    vec3 mud_albedo = mix(mud_dark, mud_mid, mud_pattern);
    // Wet sheen: darken and add slight blue-ish reflection hint.
    mud_albedo *= mix(0.7, 1.0, 1.0 - moisture);
    mud_albedo += vec3(0.0, 0.005, 0.01) * moisture;

    vec3 mud_normal = proceduralNormalFast(uv, 4.0, 0.3);
    // Mud is smooth when wet (low roughness = shiny).
    float mud_roughness = mix(0.25, 0.55, 1.0 - moisture);

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER BLENDING WEIGHTS
    // Based on elevation, slope, moisture, and noise for organic transitions.
    // ══════════════════════════════════════════════════════════════════════════

    // Weight 0: Bedrock — dominates at high elevation OR steep slopes.
    float w0 = smoothstep(0.65, 0.85, elev_norm) * 0.7
             + smoothstep(0.6, 0.85, slope) * 0.8;
    w0 += (n_ridge - 0.5) * 0.15; // noise-driven organic edge

    // Weight 1: Rocky soil — mid-high elevation, moderate slopes.
    float w1 = smoothstep(0.35, 0.6, elev_norm) * (1.0 - smoothstep(0.6, 0.85, elev_norm));
    w1 += smoothstep(0.3, 0.55, slope) * 0.5;
    w1 += (n_meso - 0.5) * 0.12;

    // Weight 2: Grass — low-mid elevation, flat areas, moderate moisture.
    float w2 = (1.0 - smoothstep(0.5, 0.7, elev_norm)) * (1.0 - smoothstep(0.3, 0.5, slope));
    w2 *= mix(0.4, 1.0, moisture * 0.5 + 0.5);
    w2 += (n_macro - 0.5) * 0.1;

    // Weight 3: Sand/dirt — low elevation, dry areas.
    float w3 = (1.0 - smoothstep(0.2, 0.45, elev_norm)) * (1.0 - moisture * 0.8);
    w3 *= (1.0 - smoothstep(0.25, 0.45, slope));
    w3 += (n_meso - 0.5) * 0.08;

    // Weight 4: River sediment/mud — near water (high moisture), low elevation.
    float w4 = moisture * (1.0 - smoothstep(0.25, 0.5, elev_norm));
    w4 *= (1.0 - smoothstep(0.2, 0.4, slope));
    w4 += (n_macro - 0.5) * 0.06;

    // Clamp weights to [0, 1] before normalization.
    w0 = max(w0, 0.0);
    w1 = max(w1, 0.0);
    w2 = max(w2, 0.0);
    w3 = max(w3, 0.0);
    w4 = max(w4, 0.0);

    // ── Quality tier layer masking ────────────────────────────────────────────
    // When u_layer_count < 5, zero out higher layers and redistribute.
    if (u_layer_count < 5) { w4 = 0.0; }
    if (u_layer_count < 4) { w3 = 0.0; }
    if (u_layer_count < 3) { w1 = 0.0; }
    // Layers 0 (bedrock) and 2 (grass) are always active as minimum 2 layers.

    // Normalize weights so they sum to 1.
    float w_total = w0 + w1 + w2 + w3 + w4 + 0.0001;
    w0 /= w_total;
    w1 /= w_total;
    w2 /= w_total;
    w3 /= w_total;
    w4 /= w_total;

    // ══════════════════════════════════════════════════════════════════════════
    // FINAL MATERIAL COMPOSITION
    // ══════════════════════════════════════════════════════════════════════════

    // Blend albedo from all active layers.
    vec3 final_albedo = bedrock_albedo * w0
                      + rocky_albedo * w1
                      + grass_albedo * w2
                      + sand_albedo * w3
                      + mud_albedo * w4;

    // Blend roughness.
    float final_roughness = bedrock_roughness * w0
                          + rocky_roughness * w1
                          + grass_roughness * w2
                          + sand_roughness * w3
                          + mud_roughness * w4;

    // Blend normals (weighted average in tangent space, then renormalize).
    vec3 final_normal_ts = bedrock_normal * w0
                         + rocky_normal * w1
                         + grass_normal * w2
                         + sand_normal * w3
                         + mud_normal * w4;
    final_normal_ts = normalize(final_normal_ts);

    // ── Macro variation overlay ───────────────────────────────────────────────
    // Large-scale color shifts to break repetition across the terrain.
    float macro_var = fbm3(uv * u_macro_scale);
    vec3 macro_warm = vec3(1.06, 1.02, 0.94);  // warm shift
    vec3 macro_cool = vec3(0.94, 0.98, 1.06);  // cool shift
    vec3 macro_tint = mix(macro_cool, macro_warm, macro_var);
    final_albedo *= macro_tint;

    // ── Height-based color grading ────────────────────────────────────────────
    // Hilltops: warmer, drier appearance. Valleys: cooler, wetter.
    float height_grade = smoothstep(0.0, 1.0, elev_norm);
    vec3 hilltop_tint = vec3(1.08, 1.03, 0.90);  // warm/dry
    vec3 valley_tint = vec3(0.92, 0.97, 1.05);   // cool/wet
    vec3 height_tint = mix(valley_tint, hilltop_tint, height_grade);
    final_albedo *= height_tint;

    // ── Vertex ambient occlusion ──────────────────────────────────────────────
    // Darkens valleys and crevices for added depth.
    final_albedo *= v_vertex_ao;

    // ── Specular micro-highlights (dew, quartz, mica) ─────────────────────────
    float glint = pow(clamp(n_micro, 0.0, 1.0), 8.0) * 0.08;
    final_albedo += glint * vec3(1.0, 0.98, 0.95);
    final_albedo = clamp(final_albedo, 0.0, 1.0);

    // ── Per-layer roughness variation from noise ──────────────────────────────
    // Adds micro-scale roughness variation so surfaces don't look uniformly matte.
    float roughness_var = (n_micro - 0.5) * 0.08;
    final_roughness = clamp(final_roughness + roughness_var, 0.0, 1.0);

    // ── Output ────────────────────────────────────────────────────────────────
    ALBEDO = final_albedo;
    ROUGHNESS = final_roughness;
    METALLIC = 0.0;
    SPECULAR = 0.06;

    // Normal map from procedural perturbation.
    NORMAL_MAP = final_normal_ts * 0.5 + 0.5;
    NORMAL_MAP_DEPTH = 0.85;
}
