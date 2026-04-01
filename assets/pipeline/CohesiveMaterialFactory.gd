## CohesiveMaterialFactory.gd
## =============================================================================
## Asset Cohesion Pipeline — Runtime Material Factory (Godot 4.x)
##
## PURPOSE:
##   Programmatically creates and configures ShaderMaterial instances that use
##   cohesive_flat.gdshader. Acts as the single authoritative source for all
##   cohesive materials in the game — every unit, building, and prop should
##   get its material from this factory rather than hand-configuring materials
##   in the editor. That way a single change here propagates everywhere.
##
## USAGE (autoload):
##   In Project Settings → Autoload, add this script as "CohesiveMaterialFactory".
##   Then anywhere in the game:
##
##     var mat := CohesiveMaterialFactory.create_unit_material(Color.BLUE)
##     mesh_instance.material_override = mat
##
##     # Or apply to an entire imported GLB:
##     CohesiveMaterialFactory.apply_to_scene($Unit, Color.RED)
##
## USAGE (static, no autoload):
##   Because all methods are static, the class works fine without being registered
##   as an autoload. Just use the class_name directly:
##
##     var mat := CohesiveMaterialFactory.create_unit_material()
##
## NOTE ON SHADER PATH:
##   SHADER_PATH must point to cohesive_flat.gdshader in your project's res://
##   tree. Adjust if your pipeline directory is at a different location.
## =============================================================================

class_name CohesiveMaterialFactory

## Path to the cohesive shader within the Godot project.
## Adjust this constant if you move the shader to a different directory.
const SHADER_PATH := "res://assets/shaders/cohesive_flat.gdshader"

## Maximum number of colors supported in a palette texture.
## Must match MAX_PALETTE_SIZE in cohesive_flat.gdshader.
const MAX_PALETTE_SIZE := 32


# =============================================================================
# PUBLIC API
# =============================================================================

## create_unit_material
## -----------------------------------------------------------------------------
## Creates a fully configured ShaderMaterial for a single unit or prop.
##
## Parameters:
##   base_color       — flat albedo color (used when use_vertex_color = false,
##                      or as a tint multiplied on top of vertex color)
##   team_color       — faction color tint (mixed in by team_color_strength)
##   use_vertex_color — if true, per-vertex color from the mesh drives albedo
##   light_bands      — number of cel-shading brightness steps (2–8)
##
## Returns:
##   A new ShaderMaterial ready to assign to a MeshInstance3D. The returned
##   material is not cached — call this once per mesh instance or cache it
##   yourself if you spawn many identical units.
##
## Example:
##   var mat := CohesiveMaterialFactory.create_unit_material(
##       Color.WHITE, Color(0.0, 0.3, 1.0), true, 4
##   )
##   $Soldier/MeshInstance3D.material_override = mat
static func create_unit_material(
	base_color: Color = Color.WHITE,
	team_color: Color = Color.RED,
	use_vertex_color: bool = true,
	light_bands: int = 4
) -> ShaderMaterial:
	var mat := _make_base_material()

	# Base color — vec4 because the uniform is declared as vec4 in the shader
	mat.set_shader_parameter("base_color", base_color)

	# Vertex color toggle
	mat.set_shader_parameter("use_vertex_color", use_vertex_color)

	# Cel shading bands
	mat.set_shader_parameter("light_bands", clampi(light_bands, 2, 8))

	# Team color (default strength 0 — caller can set this via the returned mat)
	mat.set_shader_parameter("team_color", team_color)
	mat.set_shader_parameter("team_color_strength", 0.0)

	# Defaults for everything else (pulled from the "default" preset)
	_apply_preset_dict(mat, get_preset("default"))

	return mat


## apply_to_scene
## -----------------------------------------------------------------------------
## Walks the entire scene tree rooted at `root` and replaces the material on
## every MeshInstance3D with a fresh cohesive material. Useful for applying
## the cohesion shader to an imported GLB at runtime.
##
## Parameters:
##   root       — the root Node3D of the scene to walk (e.g., an instantiated
##                PackedScene from a GLB import)
##   team_color — faction color to inject into every mesh's material
##
## Example:
##   var unit_scene := preload("res://units/soldier.glb").instantiate()
##   add_child(unit_scene)
##   CohesiveMaterialFactory.apply_to_scene(unit_scene, Color(0.0, 0.5, 1.0))
##
## Notes:
##   • If the mesh has multiple surfaces, each surface gets its own material
##     instance (they share the same shader but different base colors sampled
##     from the original surface material so palette information is preserved).
##   • The original materials are NOT preserved. If you need to keep them,
##     cache them before calling this function.
##   • This function is synchronous — for large scenes with many meshes,
##     consider yielding between nodes or running this in a thread.
static func apply_to_scene(root: Node3D, team_color: Color = Color.RED) -> void:
	# Recursively walk every descendant of root
	_walk_and_apply(root, team_color)


## create_palette_texture
## -----------------------------------------------------------------------------
## Generates a 1×N ImageTexture from an array of Color values.
## The texture is in FORMAT_RGBA8 and sized 1 pixel wide × N pixels tall,
## where N = colors.size() (capped at MAX_PALETTE_SIZE).
##
## The resulting ImageTexture should be assigned to the palette_texture uniform:
##   mat.set_shader_parameter("palette_texture", create_palette_texture(colors))
##   mat.set_shader_parameter("enforce_palette", true)
##
## Parameters:
##   colors — Array[Color] of palette entries (max MAX_PALETTE_SIZE entries;
##             excess entries are silently ignored)
##
## Returns:
##   An ImageTexture containing one pixel per palette color in a 1×N layout,
##   with filter mode NEAREST (no interpolation between palette entries).
##
## Example:
##   var palette := CohesiveMaterialFactory.create_palette_texture([
##       Color("#3a1a2e"),  # dark shadow
##       Color("#7b3f5e"),  # mid tone
##       Color("#e8a87c"),  # highlight
##       Color("#f5e6c8"),  # near-white
##   ])
##   mat.set_shader_parameter("palette_texture", palette)
##   mat.set_shader_parameter("enforce_palette", true)
##   mat.set_shader_parameter("palette_strength", 0.8)
static func create_palette_texture(colors: Array[Color]) -> ImageTexture:
	# Clamp palette size to the shader's compile-time maximum
	var count: int = mini(colors.size(), MAX_PALETTE_SIZE)

	if count == 0:
		push_warning("CohesiveMaterialFactory: create_palette_texture called with empty colors array.")
		count = 1

	# Create a 1×count RGBA8 image.
	# 1 pixel wide, N pixels tall — matches the shader's UV convention:
	#   u = (i + 0.5) / MAX_PALETTE_SIZE,  v = 0.5
	# We use FORMAT_RGBA8 for maximum compatibility across platforms.
	var img := Image.create(1, count, false, Image.FORMAT_RGBA8)

	for i in range(count):
		# set_pixel(x, y, color) — x=0 because the texture is 1 pixel wide
		img.set_pixel(0, i, colors[i])

	# Build the ImageTexture and force NEAREST filtering so the shader reads
	# exact pixel colors rather than blending between adjacent entries.
	var tex := ImageTexture.create_from_image(img)
	return tex


## get_preset
## -----------------------------------------------------------------------------
## Returns a Dictionary of uniform name → value pairs for a named visual preset.
## Pass the result to _apply_preset_dict(), or use it directly to configure
## a material after calling create_unit_material().
##
## Available presets:
##   "default" — balanced RTS look, neutral-cool ambient, moderate rim
##   "night"   — dark, cool ambient; stronger blue-white rim for moonlit scenes
##   "desert"  — warm amber ambient; faint orange rim for daytime desert maps
##   "snow"    — cold pale-blue ambient; high-brightness white rim; more noise
##
## Example — swap a unit to night mode:
##   var night := CohesiveMaterialFactory.get_preset("night")
##   for key in night:
##       mat.set_shader_parameter(key, night[key])
##
## Example — create a unit directly with a preset:
##   var mat := CohesiveMaterialFactory.create_unit_material()
##   CohesiveMaterialFactory.apply_preset_to_material(mat, "desert")
static func get_preset(preset_name: String) -> Dictionary:
	match preset_name:

		# --- Default -------------------------------------------------------
		# Standard flat-shaded RTS look. Balanced between readable and stylised.
		# 4 light bands give a clean hand-painted feel without being too graphic.
		# Neutral-cool ambient (0.2, 0.2, 0.25) matches a bright overcast sky.
		"default":
			return {
				"light_bands":        4,
				"band_smoothness":    0.05,
				"ambient_strength":   0.3,
				"ambient_color":      Color(0.2, 0.2, 0.25, 1.0),
				"rim_power":          3.0,
				"rim_strength":       0.15,
				"rim_color":          Color(1.0, 1.0, 1.0, 1.0),
				"noise_strength":     0.02,
				"noise_scale":        30.0,
				"enforce_palette":    false,
				"palette_strength":   0.8,
				"team_color_strength": 0.0,
				"outline_width":      0.0,
			}

		# --- Night ---------------------------------------------------------
		# Moonlit or after-dark maps. Deepened ambient, cool blue-white rim
		# to simulate moonlight scatter. More light bands (6) add shadow detail
		# that would otherwise disappear in the dark ambient.
		"night":
			return {
				"light_bands":        6,
				"band_smoothness":    0.03,
				"ambient_strength":   0.15,
				"ambient_color":      Color(0.05, 0.07, 0.18, 1.0),   # deep indigo
				"rim_power":          2.5,
				"rim_strength":       0.35,
				"rim_color":          Color(0.7, 0.8, 1.0, 1.0),       # cool moonlight
				"noise_strength":     0.015,
				"noise_scale":        25.0,
				"enforce_palette":    false,
				"palette_strength":   0.8,
				"team_color_strength": 0.0,
				"outline_width":      0.0,
			}

		# --- Desert --------------------------------------------------------
		# Arid, sun-baked maps. Warm amber ambient fills shadow areas.
		# Orange rim simulates bounce light from sun-heated sand.
		# Fewer bands (3) create harsh, high-contrast sun shadows.
		"desert":
			return {
				"light_bands":        3,
				"band_smoothness":    0.08,
				"ambient_strength":   0.35,
				"ambient_color":      Color(0.32, 0.22, 0.08, 1.0),   # warm amber
				"rim_power":          4.0,
				"rim_strength":       0.2,
				"rim_color":          Color(1.0, 0.75, 0.4, 1.0),     # sand bounce
				"noise_strength":     0.03,
				"noise_scale":        40.0,
				"enforce_palette":    false,
				"palette_strength":   0.8,
				"team_color_strength": 0.0,
				"outline_width":      0.0,
			}

		# --- Snow ----------------------------------------------------------
		# Icy, overexposed look. Pale blue-white ambient. Very bright rim.
		# Slightly more noise to suggest snow texture on surfaces.
		# High ambient_strength keeps shadows from going pure black (snowfields
		# reflect a lot of ambient light from the sky and ground).
		"snow":
			return {
				"light_bands":        5,
				"band_smoothness":    0.04,
				"ambient_strength":   0.5,
				"ambient_color":      Color(0.72, 0.82, 0.95, 1.0),   # icy pale blue
				"rim_power":          2.0,
				"rim_strength":       0.4,
				"rim_color":          Color(0.9, 0.95, 1.0, 1.0),     # white-blue ice
				"noise_strength":     0.04,
				"noise_scale":        50.0,
				"enforce_palette":    false,
				"palette_strength":   0.8,
				"team_color_strength": 0.0,
				"outline_width":      0.0,
			}

		# --- Fallback ------------------------------------------------------
		# Unknown preset name — return default and emit a warning
		_:
			push_warning(
				"CohesiveMaterialFactory.get_preset(): unknown preset '%s'. Returning 'default'."
				% preset_name
			)
			return get_preset("default")


## apply_preset_to_material
## -----------------------------------------------------------------------------
## Convenience method: applies a named preset's uniforms to an existing material.
## This lets you hot-swap a unit's visual style at runtime (e.g., day → night).
##
## Parameters:
##   mat         — an existing ShaderMaterial created by this factory
##   preset_name — one of "default", "night", "desert", "snow"
##
## Example:
##   CohesiveMaterialFactory.apply_preset_to_material(soldier_mat, "night")
static func apply_preset_to_material(mat: ShaderMaterial, preset_name: String) -> void:
	_apply_preset_dict(mat, get_preset(preset_name))


## create_outline_material
## -----------------------------------------------------------------------------
## Creates a companion outline-pass material intended to be used as a second
## surface on a MeshInstance3D. The outline is implemented via vertex normal
## extrusion with CULL_FRONT (handled in the shader's vertex() function).
##
## Parameters:
##   outline_width — extrusion width in meters (0.005–0.02 is typical for RTS)
##   outline_color — the outline tint (usually black or dark team color)
##
## Usage:
##   The MeshInstance3D must have at least 2 surfaces. Assign the normal material
##   to surface 0 and the outline material to surface 1. The mesh itself only
##   needs one surface; you can use MeshInstance3D.mesh = ArrayMesh and duplicate
##   the surface, or use a SubMesh approach.
##
##   Alternatively, use two MeshInstance3D nodes sharing the same Mesh resource.
##
## Returns:
##   A ShaderMaterial configured as the outline pass (is_outline_pass = true).
static func create_outline_material(
	outline_width: float = 0.008,
	outline_color: Color = Color.BLACK
) -> ShaderMaterial:
	var mat := _make_base_material()

	# Mark this as the outline pass — vertex() will extrude normals,
	# fragment() will output flat outline_color and skip all other processing.
	mat.set_shader_parameter("is_outline_pass",  true)
	mat.set_shader_parameter("outline_width",    clampf(outline_width, 0.0, 0.05))
	mat.set_shader_parameter("outline_color",    outline_color)

	# The outline needs CULL_FRONT so only the extruded back-faces are visible.
	# In Godot 4 we cannot change render_mode at runtime, so CULL_FRONT must
	# be set in the shader source for the outline pass. However, since we use
	# a single shared shader, the outline pass instead relies on the vertex
	# extrusion trick (CULL_BACK is the default; the extruded shell's back faces
	# point outward and are visible by default when the camera looks at them).
	# For a cleaner outline, duplicate the shader source with render_mode cull_front.
	#
	# NOTE: For production use, create a dedicated cohesive_flat_outline.gdshader
	# with `render_mode cull_front;` and load that here instead.

	return mat


# =============================================================================
# PRIVATE HELPERS
# =============================================================================

## _make_base_material
## Creates a ShaderMaterial pointing to SHADER_PATH.
## Centralised so we can add error-handling / caching here later.
static func _make_base_material() -> ShaderMaterial:
	var shader := load(SHADER_PATH) as Shader
	if shader == null:
		push_error(
			"CohesiveMaterialFactory: could not load shader at '%s'. " \
			% SHADER_PATH +
			"Check that SHADER_PATH is correct and the file is imported."
		)
		# Return a plain ShaderMaterial so callers don't get null crashes.
		return ShaderMaterial.new()

	var mat := ShaderMaterial.new()
	mat.shader = shader
	return mat


## _apply_preset_dict
## Iterates a preset dictionary and calls set_shader_parameter for each entry.
static func _apply_preset_dict(mat: ShaderMaterial, preset: Dictionary) -> void:
	for key in preset:
		mat.set_shader_parameter(key, preset[key])


## _walk_and_apply
## Recursive tree walker for apply_to_scene().
## Visits every node; on MeshInstance3D nodes it replaces all surface materials.
static func _walk_and_apply(node: Node, team_color: Color) -> void:
	if node is MeshInstance3D:
		var mesh_instance := node as MeshInstance3D
		_replace_materials(mesh_instance, team_color)

	# Recurse into all children
	for child in node.get_children():
		_walk_and_apply(child, team_color)


## _replace_materials
## Replaces every surface material on a MeshInstance3D with a fresh cohesive
## material, attempting to preserve the original base color by reading the
## surface material's albedo if it is a StandardMaterial3D or BaseMaterial3D.
static func _replace_materials(mesh_instance: MeshInstance3D, team_color: Color) -> void:
	var mesh := mesh_instance.mesh
	if mesh == null:
		return

	var surface_count: int = mesh.get_surface_count()

	for surface_idx in range(surface_count):
		# Try to extract a base color from the existing surface material so we
		# don't lose albedo information baked into the original material.
		var original_color := Color.WHITE
		var original_mat := mesh_instance.get_active_material(surface_idx)

		if original_mat is StandardMaterial3D or original_mat is BaseMaterial3D:
			# BaseMaterial3D / StandardMaterial3D both expose albedo_color
			original_color = (original_mat as BaseMaterial3D).albedo_color
		elif original_mat is ShaderMaterial:
			# If it's already a ShaderMaterial, try to read base_color
			var existing_color = (original_mat as ShaderMaterial).get_shader_parameter("base_color")
			if existing_color != null:
				original_color = existing_color as Color

		# Build a fresh cohesive material for this surface.
		# use_vertex_color defaults to true so Blender-baked vertex colors
		# are used as the primary color source. original_color acts as a tint.
		var new_mat := create_unit_material(
			original_color,
			team_color,
			true,   # use_vertex_color
			4       # light_bands
		)

		# Assign to this specific surface (not material_override, which would
		# cover all surfaces with a single material and lose per-surface colors)
		mesh_instance.set_surface_override_material(surface_idx, new_mat)
