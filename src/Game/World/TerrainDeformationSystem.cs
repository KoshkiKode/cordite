using Godot;
using CorditeWars.Core;

namespace CorditeWars.Game.World;

/// <summary>
/// Represents a single terrain deformation mark (crater, scorch, or vehicle track).
/// Stored in a pre-allocated ring buffer — zero heap allocations during gameplay.
/// </summary>
public struct TerrainDeformation
{
    /// <summary>World XZ position of the deformation center.</summary>
    public Vector2 Position;

    /// <summary>Effect radius in world units.</summary>
    public float Radius;

    /// <summary>Vertex displacement depth (craters only).</summary>
    public float Depth;

    /// <summary>Current intensity: 1.0 = fresh, fades to 0.0 over FadeDuration.</summary>
    public float Intensity;

    /// <summary>Deformation type: 0 = crater, 1 = scorch, 2 = track.</summary>
    public int Type;

    /// <summary>Time (in milliseconds) when this deformation was created.</summary>
    public float BirthTime;
}

/// <summary>
/// Manages runtime terrain deformation from combat events.
/// Listens to EventBus combat signals and applies visual modifications
/// to the terrain mesh and shader parameters.
///
/// This system is purely visual — it NEVER affects the simulation grid,
/// pathfinding costs, or unit movement logic.
///
/// Uses a fixed 256-slot ring buffer pre-allocated at initialization.
/// Zero heap allocations occur during gameplay.
/// </summary>
public sealed partial class TerrainDeformationSystem : Node
{
    /// <summary>Maximum concurrent deformation marks before oldest are recycled.</summary>
    public const int MaxDeformations = 256;

    /// <summary>Time in seconds before a deformation fully fades.</summary>
    public const float FadeDuration = 120f;

    /// <summary>Pre-allocated ring buffer of deformation slots.</summary>
    private readonly TerrainDeformation[] _deformations = new TerrainDeformation[MaxDeformations];

    /// <summary>Current write index into the ring buffer (wraps with modulo).</summary>
    private int _writeIndex;

    /// <summary>Number of active deformations (capped at MaxDeformations).</summary>
    private int _count;

    /// <summary>
    /// Initializes the deformation system and wires to EventBus combat signals.
    /// </summary>
    /// <param name="parent">Parent node to attach this system to in the scene tree.</param>
    public void Initialize(Node parent)
    {
        // Pre-allocate all slots (struct array — already zero-initialized)
        _writeIndex = 0;
        _count = 0;

        // Add to scene tree so _Process is called
        parent.AddChild(this);

        // Wire to EventBus signals
        var eventBus = EventBus.Instance;
        if (eventBus != null)
        {
            eventBus.AttackImpact += OnAttackImpact;
            eventBus.SuperweaponFired += OnSuperweaponFired;
        }
    }

    /// <summary>
    /// Creates a crater at the given world position.
    /// Displaces mesh vertices downward with cosine-bell falloff.
    /// </summary>
    /// <param name="worldPosition">World-space position of the crater center.</param>
    /// <param name="radius">Radius of the crater effect.</param>
    /// <param name="depth">Maximum vertex displacement depth.</param>
    public void CreateCrater(Vector3 worldPosition, float radius, float depth)
    {
        AddDeformation(
            new Vector2(worldPosition.X, worldPosition.Z),
            radius,
            depth,
            type: 0
        );
    }

    /// <summary>
    /// Scorches terrain in a radius (fire weapons, napalm).
    /// Darkens and desaturates albedo, increases roughness.
    /// </summary>
    /// <param name="worldPosition">World-space position of the scorch center.</param>
    /// <param name="radius">Radius of the scorch effect.</param>
    public void ScorchTerrain(Vector3 worldPosition, float radius)
    {
        AddDeformation(
            new Vector2(worldPosition.X, worldPosition.Z),
            radius,
            depth: 0f,
            type: 1
        );
    }

    /// <summary>
    /// Adds a vehicle track segment between two world positions.
    /// Stored as a darkened strip with slight displacement.
    /// </summary>
    /// <param name="from">Start position of the track segment.</param>
    /// <param name="to">End position of the track segment.</param>
    /// <param name="width">Width of the track.</param>
    public void AddVehicleTrack(Vector3 from, Vector3 to, float width)
    {
        // Store the midpoint of the track segment with radius = half the segment length
        Vector2 fromXZ = new Vector2(from.X, from.Z);
        Vector2 toXZ = new Vector2(to.X, to.Z);
        Vector2 midpoint = (fromXZ + toXZ) * 0.5f;
        float halfLength = fromXZ.DistanceTo(toXZ) * 0.5f;
        float radius = Mathf.Max(halfLength, width * 0.5f);

        AddDeformation(
            midpoint,
            radius,
            depth: 0.05f,
            type: 2
        );
    }

    /// <summary>
    /// Called each frame to fade old deformations.
    /// Intensity = max(0, 1.0 - age / FadeDuration).
    /// </summary>
    public override void _Process(double delta)
    {
        if (_count == 0)
            return;

        float currentTime = Time.GetTicksMsec();

        int slotsToCheck = _count;
        for (int i = 0; i < slotsToCheck; i++)
        {
            int idx = i % MaxDeformations;
            ref TerrainDeformation def = ref _deformations[idx];

            if (def.Intensity <= 0f)
                continue;

            float ageSeconds = (currentTime - def.BirthTime) / 1000f;
            float newIntensity = 1.0f - ageSeconds / FadeDuration;
            def.Intensity = Mathf.Max(0f, newIntensity);
        }
    }

    /// <summary>
    /// Returns a read-only span of the active deformations for shader consumption.
    /// The shader can read position, radius, depth, intensity, and type from each slot.
    /// </summary>
    /// <returns>A span covering all deformation slots (up to MaxDeformations).</returns>
    public ReadOnlySpan<TerrainDeformation> GetActiveDeformations()
    {
        return new ReadOnlySpan<TerrainDeformation>(_deformations, 0, _count);
    }

    /// <summary>
    /// Returns the current number of active deformations in the ring buffer.
    /// </summary>
    public int ActiveCount => _count;

    // ── Private Helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Adds a deformation to the ring buffer. Overwrites the oldest when full.
    /// Zero heap allocations — writes directly into the pre-allocated struct array.
    /// </summary>
    private void AddDeformation(Vector2 position, float radius, float depth, int type)
    {
        _deformations[_writeIndex] = new TerrainDeformation
        {
            Position = position,
            Radius = radius,
            Depth = depth,
            Intensity = 1.0f,
            Type = type,
            BirthTime = Time.GetTicksMsec()
        };

        _writeIndex = (_writeIndex + 1) % MaxDeformations;

        if (_count < MaxDeformations)
            _count++;
    }

    // ── EventBus Signal Handlers ────────────────────────────────────────────

    /// <summary>
    /// Handles AttackImpact signal — creates a small crater if the attack has AoE.
    /// </summary>
    private void OnAttackImpact(int targetId, bool isHit, bool hasAoe, Vector3 position)
    {
        if (hasAoe)
        {
            CreateCrater(position, radius: 2.0f, depth: 0.3f);
        }
    }

    /// <summary>
    /// Handles SuperweaponFired signal — creates a large crater at the target position.
    /// </summary>
    private void OnSuperweaponFired(int playerId, string weaponId, Vector3 targetPosition)
    {
        CreateCrater(targetPosition, radius: 8.0f, depth: 1.5f);
    }

    // ── Cleanup ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Disconnects from EventBus signals when removed from the scene tree.
    /// </summary>
    public override void _ExitTree()
    {
        var eventBus = EventBus.Instance;
        if (eventBus != null)
        {
            eventBus.AttackImpact -= OnAttackImpact;
            eventBus.SuperweaponFired -= OnSuperweaponFired;
        }

        base._ExitTree();
    }
}
