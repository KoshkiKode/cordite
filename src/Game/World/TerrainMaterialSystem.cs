using Godot;
using CorditeWars.Systems.Graphics;

namespace CorditeWars.Game.World
{
    /// <summary>
    /// Configures the terrain PBR uber-shader material per quality tier.
    /// Loads the procedural shader and sets uniform values for layer count,
    /// triplanar sharpness, and macro variation scale based on the active
    /// QualityTier. All surface detail is generated analytically in the shader —
    /// no external texture files are required.
    /// </summary>
    public sealed class TerrainMaterialSystem
    {
        private const string ShaderPath = "res://src/Game/World/Shaders/terrain_pbr.gdshader";

        private readonly ShaderMaterial _material;
        private readonly QualityTier _tier;

        /// <summary>
        /// Creates a new TerrainMaterialSystem configured for the given quality tier.
        /// Loads the terrain PBR shader and sets uniform values accordingly.
        /// </summary>
        /// <param name="tier">The rendering quality tier to configure for.</param>
        public TerrainMaterialSystem(QualityTier tier)
        {
            _tier = tier;

            var shader = ResourceLoader.Load<Shader>(ShaderPath);
            if (shader == null)
            {
                GD.PrintErr($"[TerrainMaterialSystem] Failed to load shader: {ShaderPath}");
            }

            _material = new ShaderMaterial();
            _material.Shader = shader;

            ApplyTierSettings(tier);
        }

        /// <summary>
        /// Returns the configured ShaderMaterial ready to be assigned to terrain meshes.
        /// </summary>
        public ShaderMaterial GetMaterial()
        {
            return _material;
        }

        /// <summary>
        /// Adjusts shader parameters for a specific biome. Reserved for future use
        /// when per-biome material tuning is implemented (e.g., desert maps emphasizing
        /// sand/rock layers, temperate maps emphasizing grass/soil).
        /// </summary>
        /// <param name="biome">The biome identifier (e.g., "temperate", "desert", "coastal").</param>
        public void SetBiome(string biome)
        {
            // Future: adjust layer weights, color tints, or noise parameters per biome.
            // The shader already supports per-vertex biome hints via vertex color channels,
            // so this method can be extended to set additional uniforms when biome-specific
            // shader parameters are added.
            GD.Print($"[TerrainMaterialSystem] Biome set to: {biome} (reserved for future use)");
        }

        /// <summary>
        /// Applies shader uniform values based on the quality tier.
        /// </summary>
        private void ApplyTierSettings(QualityTier tier)
        {
            switch (tier)
            {
                case QualityTier.Potato:
                    _material.SetShaderParameter("u_layer_count", 2);
                    _material.SetShaderParameter("u_triplanar_sharpness", 2.0f);
                    _material.SetShaderParameter("u_macro_scale", 0.03f);
                    break;

                case QualityTier.Low:
                    _material.SetShaderParameter("u_layer_count", 3);
                    _material.SetShaderParameter("u_triplanar_sharpness", 3.0f);
                    _material.SetShaderParameter("u_macro_scale", 0.03f);
                    break;

                case QualityTier.Medium:
                    _material.SetShaderParameter("u_layer_count", 5);
                    _material.SetShaderParameter("u_triplanar_sharpness", 4.0f);
                    _material.SetShaderParameter("u_macro_scale", 0.03f);
                    break;

                case QualityTier.High:
                    _material.SetShaderParameter("u_layer_count", 5);
                    _material.SetShaderParameter("u_triplanar_sharpness", 6.0f);
                    _material.SetShaderParameter("u_macro_scale", 0.025f);
                    break;

                default:
                    GD.PrintErr($"[TerrainMaterialSystem] Unknown tier: {tier}, using Medium defaults.");
                    _material.SetShaderParameter("u_layer_count", 5);
                    _material.SetShaderParameter("u_triplanar_sharpness", 4.0f);
                    _material.SetShaderParameter("u_macro_scale", 0.03f);
                    break;
            }
        }
    }
}
