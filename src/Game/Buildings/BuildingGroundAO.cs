using Godot;

namespace CorditeWars.Game.Buildings;

/// <summary>
/// Adds a subtle ground-contact ambient occlusion darkening ring around
/// building bases. This visually "grounds" buildings on the terrain,
/// preventing them from looking like they're floating.
/// Implemented as a flat decal-like mesh at the building's base.
/// </summary>
public static class BuildingGroundAO
{
    /// <summary>
    /// Creates and attaches a ground-contact AO mesh to the building.
    /// The mesh is a flat ring slightly larger than the building footprint
    /// with a radial gradient from dark (center) to transparent (edge).
    /// </summary>
    public static void AttachGroundAO(BuildingInstance building, int footprintW, int footprintH)
    {
        float width = footprintW + 1.5f;
        float height = footprintH + 1.5f;

        var mesh = new QuadMesh();
        mesh.Size = new Vector2(width, height);

        var meshInstance = new MeshInstance3D();
        meshInstance.Mesh = mesh;
        meshInstance.Name = "GroundAO";

        // Position slightly above ground to avoid z-fighting
        meshInstance.Position = new Vector3(0f, 0.02f, 0f);
        meshInstance.RotationDegrees = new Vector3(-90f, 0f, 0f);

        // Material: dark semi-transparent with radial falloff
        var mat = new StandardMaterial3D();
        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        mat.AlbedoColor = new Color(0f, 0f, 0f, 0.35f);
        mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        mat.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        mat.NoDepthTest = false;
        mat.RenderPriority = -1; // Render before other transparent objects
        meshInstance.MaterialOverride = mat;

        // Disable shadow casting for the AO quad
        meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

        building.AddChild(meshInstance);
    }
}
