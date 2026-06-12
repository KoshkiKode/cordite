using Godot;

namespace CorditeWars.Game.VFX;

/// <summary>
/// Visual type classification for projectile rendering.
/// Each type has distinct flight behavior and visual treatment.
/// </summary>
public enum ProjectileVisualType
{
    /// <summary>Thin bright line, very fast (≥50 units/s). Machine guns, autocannons, AA.</summary>
    Tracer,
    /// <summary>Mesh with smoke trail, moderate speed (~20 units/s). Missiles, rockets, SAMs.</summary>
    Missile,
    /// <summary>Parabolic arc, moderate speed. Cannons, artillery, mortars.</summary>
    ArcingShell,
    /// <summary>Instant line between origin and target (laser weapons). Fades after 0.1s.</summary>
    Beam
}

/// <summary>
/// Configuration for a projectile visual type, defining flight speed,
/// arc height, trail behavior, and appearance.
/// </summary>
public record ProjectileConfig(
    ProjectileVisualType VisualType,
    float FlightSpeed,
    float ArcHeight,
    bool HasTrail,
    Color TrailColor,
    float ProjectileScale
);
