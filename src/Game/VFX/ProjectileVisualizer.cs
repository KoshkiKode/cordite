using System.Collections.Generic;
using Godot;
using CorditeWars.Core;
using CorditeWars.Game.Units;
using CorditeWars.Systems.Graphics;

namespace CorditeWars.Game.VFX;

/// <summary>
/// Spawns and animates cosmetic projectile nodes between attacker origin and
/// target position. Subscribes to EventBus.AttackFired and EventBus.AttackImpact
/// to correlate fire origins with impact positions.
///
/// Visual-only: no collision, no damage, no simulation mutation.
///
/// Strategy: On AttackFired, record (attackerId → origin). On AttackImpact,
/// look up the stored origin and spawn the projectile from origin to impact position.
/// </summary>
public partial class ProjectileVisualizer : Node3D
{
    // ── Constants ────────────────────────────────────────────────────

    private const float TracerSpeed = 60f;          // units/s (≥50 required)
    private const float MissileSpeed = 20f;         // units/s
    private const float ArcingShellSpeed = 30f;     // units/s
    private const float BeamFadeDuration = 0.1f;    // seconds
    private const float MaxLifetime = 5f;           // safety timeout
    private const int TracerPoolSize = 64;
    private const float ArcHeightPerDistance = 0.3f; // arc height = distance * this factor

    // ── State ────────────────────────────────────────────────────────

    private Node3D? _worldRoot;

    /// <summary>
    /// Pending fire origins keyed by attackerId. Consumed when AttackImpact arrives.
    /// </summary>
    private readonly Dictionary<int, PendingFire> _pendingFires = new();

    /// <summary>Active projectiles being animated each frame.</summary>
    private readonly List<ActiveProjectile> _activeProjectiles = new();

    /// <summary>Object pool for tracer projectiles to avoid allocation pressure.</summary>
    private readonly Queue<MeshInstance3D> _tracerPool = new();

    // ── Initialization ───────────────────────────────────────────────

    /// <summary>
    /// Subscribes to EventBus signals and stores the world root for spawning.
    /// Call once after the scene tree is populated.
    /// </summary>
    public void Initialize(Node3D? worldRoot = null)
    {
        _worldRoot = worldRoot;

        var bus = EventBus.Instance;
        if (bus == null)
        {
            GD.PushError("[ProjectileVisualizer] EventBus not available.");
            return;
        }

        bus.AttackFired += OnAttackFired;
        bus.AttackImpact += OnAttackImpact;

        // Pre-populate tracer pool
        for (int i = 0; i < TracerPoolSize; i++)
        {
            var tracer = CreateTracerMesh();
            tracer.Visible = false;
            AddToWorld(tracer);
            _tracerPool.Enqueue(tracer);
        }

        GD.Print("[ProjectileVisualizer] Initialized — listening for combat events.");
    }

    public override void _ExitTree()
    {
        var bus = EventBus.Instance;
        if (bus != null)
        {
            bus.AttackFired -= OnAttackFired;
            bus.AttackImpact -= OnAttackImpact;
        }

        // Clean up pooled tracers
        while (_tracerPool.Count > 0)
        {
            var tracer = _tracerPool.Dequeue();
            if (GodotObject.IsInstanceValid(tracer))
                tracer.QueueFree();
        }
    }

    // ── Per-frame update ─────────────────────────────────────────────

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
        {
            var proj = _activeProjectiles[i];

            if (!GodotObject.IsInstanceValid(proj.Node))
            {
                _activeProjectiles.RemoveAt(i);
                continue;
            }

            // Safety timeout
            proj.ElapsedTime += dt;
            if (proj.ElapsedTime >= MaxLifetime)
            {
                FreeProjectile(proj);
                _activeProjectiles.RemoveAt(i);
                continue;
            }

            // Advance progress
            if (proj.FlightDuration > 0f)
            {
                proj.Progress += dt / proj.FlightDuration;
            }

            if (proj.Progress >= 1f)
            {
                // Arrived at target
                FreeProjectile(proj);
                _activeProjectiles.RemoveAt(i);
                continue;
            }

            // Update position based on type
            switch (proj.VisualType)
            {
                case ProjectileVisualType.Tracer:
                    UpdateTracer(proj);
                    break;
                case ProjectileVisualType.Missile:
                    UpdateMissile(proj);
                    break;
                case ProjectileVisualType.ArcingShell:
                    UpdateArcingShell(proj);
                    break;
                // Beam doesn't move — it fades (handled by tween)
            }

            _activeProjectiles[i] = proj;
        }
    }

    // ── Signal Handlers ──────────────────────────────────────────────

    private void OnAttackFired(int attackerId, int weaponType, Vector3 position)
    {
        // Store the fire origin; will be consumed when AttackImpact arrives
        _pendingFires[attackerId] = new PendingFire
        {
            Origin = position,
            WeaponType = weaponType,
            Timestamp = Time.GetTicksMsec() / 1000.0
        };
    }

    private void OnAttackImpact(int targetId, bool isHit, bool hasAoe, Vector3 position)
    {
        // Try to find a pending fire that matches. Since AttackImpact doesn't
        // carry attackerId, we consume the most recent pending fire.
        // In practice, AttackFired and AttackImpact fire in sequence for the same attack.
        if (_pendingFires.Count == 0)
            return;

        // Consume the first pending fire (FIFO order for determinism)
        int? consumedKey = null;
        PendingFire consumed = default;
        double oldestTime = double.MaxValue;

        foreach (var kvp in _pendingFires)
        {
            if (kvp.Value.Timestamp < oldestTime)
            {
                oldestTime = kvp.Value.Timestamp;
                consumedKey = kvp.Key;
                consumed = kvp.Value;
            }
        }

        if (consumedKey == null)
            return;

        _pendingFires.Remove(consumedKey.Value);

        // Don't spawn projectile if origin == target (zero distance)
        Vector3 origin = consumed.Origin;
        Vector3 target = position;
        if (origin.DistanceSquaredTo(target) < 0.01f)
            return;

        // Spawn the appropriate projectile type
        var visualType = GetVisualType((WeaponType)consumed.WeaponType);
        SpawnProjectile(visualType, origin, target);
    }

    // ── WeaponType → ProjectileVisualType Mapping ────────────────────

    /// <summary>
    /// Maps a WeaponType enum value to the appropriate projectile visual type.
    /// Covers all WeaponType enum values defined in the project.
    /// </summary>
    public static ProjectileVisualType GetVisualType(WeaponType weapon) => weapon switch
    {
        WeaponType.MachineGun or WeaponType.GatlingGun or WeaponType.Flak => ProjectileVisualType.Tracer,
        WeaponType.Missile or WeaponType.Rockets or WeaponType.SAM or WeaponType.Torpedo => ProjectileVisualType.Missile,
        WeaponType.Cannon or WeaponType.Mortar => ProjectileVisualType.ArcingShell,
        WeaponType.Laser or WeaponType.EMP => ProjectileVisualType.Beam,
        WeaponType.Sniper => ProjectileVisualType.Tracer,
        WeaponType.Flamethrower or WeaponType.ChemicalSpray => ProjectileVisualType.Tracer,
        WeaponType.Bomb => ProjectileVisualType.ArcingShell,
        _ => ProjectileVisualType.Tracer
    };

    // ── Projectile Spawning ──────────────────────────────────────────

    private void SpawnProjectile(ProjectileVisualType visualType, Vector3 origin, Vector3 target)
    {
        switch (visualType)
        {
            case ProjectileVisualType.Tracer:
                SpawnTracer(origin, target);
                break;
            case ProjectileVisualType.Missile:
                SpawnMissile(origin, target);
                break;
            case ProjectileVisualType.ArcingShell:
                SpawnArcingShell(origin, target);
                break;
            case ProjectileVisualType.Beam:
                SpawnBeam(origin, target);
                break;
        }
    }

    // ── Tracer ───────────────────────────────────────────────────────

    /// <summary>
    /// Thin bright quad mesh, linear lerp at ≥50 units/s, no trail, auto-free on arrival.
    /// Uses object pooling (pool size 64) to avoid allocation pressure.
    /// </summary>
    private void SpawnTracer(Vector3 origin, Vector3 target)
    {
        MeshInstance3D tracer;

        if (_tracerPool.Count > 0)
        {
            tracer = _tracerPool.Dequeue();
            tracer.Visible = true;
        }
        else
        {
            // Pool exhausted — create a new one (rare for high fire rates)
            tracer = CreateTracerMesh();
            AddToWorld(tracer);
        }

        tracer.GlobalPosition = origin;

        // Orient tracer toward target
        Vector3 direction = (target - origin).Normalized();
        if (direction.LengthSquared() > 0.001f)
        {
            tracer.LookAt(target, Vector3.Up);
        }

        float distance = origin.DistanceTo(target);
        float flightDuration = distance / TracerSpeed;

        _activeProjectiles.Add(new ActiveProjectile
        {
            Node = tracer,
            Origin = origin,
            Target = target,
            VisualType = ProjectileVisualType.Tracer,
            Progress = 0f,
            FlightDuration = flightDuration,
            ElapsedTime = 0f,
            ArcHeight = 0f,
            IsPooled = true,
            TrailParticles = null
        });
    }

    private static MeshInstance3D CreateTracerMesh()
    {
        var mesh = new MeshInstance3D();
        var quad = new QuadMesh();
        quad.Size = new Vector2(0.05f, 0.8f); // thin and elongated
        mesh.Mesh = quad;

        var mat = new StandardMaterial3D();
        mat.AlbedoColor = new Color(1.0f, 0.95f, 0.4f); // yellow/white bright
        mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        mat.BillboardMode = BaseMaterial3D.BillboardModeEnum.FixedY;
        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        mat.NoDepthTest = true;
        mesh.MaterialOverride = mat;

        // No collision
        mesh.SetLayerMaskValue(1, false);

        return mesh;
    }

    // ── Missile ──────────────────────────────────────────────────────

    /// <summary>
    /// Small cylinder mesh with attached GpuParticles3D smoke trail,
    /// moderate speed (~20 units/s), guided toward target.
    /// Trail particles disabled on Potato quality tier.
    /// </summary>
    private void SpawnMissile(Vector3 origin, Vector3 target)
    {
        var missile = new Node3D();
        missile.GlobalPosition = origin;

        // Missile body: small cylinder
        var body = new MeshInstance3D();
        var cylinder = new CylinderMesh();
        cylinder.TopRadius = 0.05f;
        cylinder.BottomRadius = 0.08f;
        cylinder.Height = 0.5f;
        body.Mesh = cylinder;

        var bodyMat = new StandardMaterial3D();
        bodyMat.AlbedoColor = new Color(0.4f, 0.4f, 0.45f);
        bodyMat.Roughness = 0.6f;
        body.MaterialOverride = bodyMat;
        missile.AddChild(body);

        // Smoke trail (disabled on Potato quality)
        GpuParticles3D? trail = null;
        bool isPotato = QualityManager.Instance?.CurrentTier == QualityTier.Potato;

        if (!isPotato)
        {
            trail = CreateSmokeTrail();
            missile.AddChild(trail);
        }

        // Orient toward target
        Vector3 direction = (target - origin).Normalized();
        if (direction.LengthSquared() > 0.001f)
        {
            missile.LookAt(target, Vector3.Up);
        }

        AddToWorld(missile);

        float distance = origin.DistanceTo(target);
        float flightDuration = distance / MissileSpeed;

        _activeProjectiles.Add(new ActiveProjectile
        {
            Node = missile,
            Origin = origin,
            Target = target,
            VisualType = ProjectileVisualType.Missile,
            Progress = 0f,
            FlightDuration = flightDuration,
            ElapsedTime = 0f,
            ArcHeight = 0f,
            IsPooled = false,
            TrailParticles = trail
        });
    }

    private static GpuParticles3D CreateSmokeTrail()
    {
        var material = new ParticleProcessMaterial();
        material.Direction = new Vector3(0, 0, -1); // emit behind missile
        material.Spread = 10f;
        material.InitialVelocityMin = 0.5f;
        material.InitialVelocityMax = 1.5f;
        material.Gravity = new Vector3(0, 0.3f, 0);
        material.ScaleMin = 0.1f;
        material.ScaleMax = 0.3f;
        material.DampingMin = 2f;
        material.DampingMax = 3f;

        var colorRamp = new GradientTexture1D();
        var gradient = new Gradient();
        gradient.SetColor(0, new Color(0.7f, 0.7f, 0.7f, 0.6f));
        gradient.SetColor(1, new Color(0.4f, 0.4f, 0.4f, 0.0f));
        colorRamp.Gradient = gradient;
        material.ColorRamp = colorRamp;

        var particles = new GpuParticles3D();
        particles.Amount = 12;
        particles.Lifetime = 0.8f;
        particles.OneShot = false;
        particles.Explosiveness = 0f;
        particles.ProcessMaterial = material;
        particles.Emitting = true;

        var drawMesh = new QuadMesh();
        drawMesh.Size = new Vector2(0.15f, 0.15f);
        var drawMat = new StandardMaterial3D();
        drawMat.BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles;
        drawMat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        drawMat.VertexColorUseAsAlbedo = true;
        drawMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        drawMesh.Material = drawMat;
        particles.DrawPass1 = drawMesh;

        return particles;
    }

    // ── Arcing Shell ─────────────────────────────────────────────────

    /// <summary>
    /// Sphere mesh following quadratic bezier arc (origin → elevated midpoint → target).
    /// Arc height is configurable based on distance.
    /// </summary>
    private void SpawnArcingShell(Vector3 origin, Vector3 target)
    {
        var shell = new MeshInstance3D();
        var sphere = new SphereMesh();
        sphere.Radius = 0.15f;
        sphere.Height = 0.3f;
        shell.Mesh = sphere;

        var mat = new StandardMaterial3D();
        mat.AlbedoColor = new Color(0.3f, 0.3f, 0.3f);
        mat.Roughness = 0.5f;
        shell.MaterialOverride = mat;

        shell.GlobalPosition = origin;
        AddToWorld(shell);

        float distance = origin.DistanceTo(target);
        float arcHeight = distance * ArcHeightPerDistance;
        float flightDuration = distance / ArcingShellSpeed;

        _activeProjectiles.Add(new ActiveProjectile
        {
            Node = shell,
            Origin = origin,
            Target = target,
            VisualType = ProjectileVisualType.ArcingShell,
            Progress = 0f,
            FlightDuration = flightDuration,
            ElapsedTime = 0f,
            ArcHeight = arcHeight,
            IsPooled = false,
            TrailParticles = null
        });
    }

    /// <summary>
    /// Computes the world position of an arcing projectile at time t ∈ [0, 1].
    /// Uses a quadratic bezier with the control point elevated by arcHeight.
    /// </summary>
    public static Vector3 ComputeArcPosition(Vector3 origin, Vector3 target, float arcHeight, float t)
    {
        // Midpoint elevated by arc height
        Vector3 midpoint = (origin + target) / 2f;
        midpoint.Y += arcHeight;

        // Quadratic bezier: B(t) = (1-t)²·P0 + 2(1-t)t·P1 + t²·P2
        float oneMinusT = 1f - t;
        float oneMinusTSq = oneMinusT * oneMinusT;
        float tSq = t * t;

        return new Vector3(
            oneMinusTSq * origin.X + 2f * oneMinusT * t * midpoint.X + tSq * target.X,
            oneMinusTSq * origin.Y + 2f * oneMinusT * t * midpoint.Y + tSq * target.Y,
            oneMinusTSq * origin.Z + 2f * oneMinusT * t * midpoint.Z + tSq * target.Z
        );
    }

    // ── Beam ─────────────────────────────────────────────────────────

    /// <summary>
    /// Instant thin cylinder between origin and target, fades after 0.1s.
    /// No movement — appears instantly and disappears.
    /// </summary>
    private void SpawnBeam(Vector3 origin, Vector3 target)
    {
        var beam = new MeshInstance3D();

        // Create a thin cylinder oriented along the beam direction
        float distance = origin.DistanceTo(target);
        var cylinder = new CylinderMesh();
        cylinder.TopRadius = 0.02f;
        cylinder.BottomRadius = 0.02f;
        cylinder.Height = distance;
        beam.Mesh = cylinder;

        var mat = new StandardMaterial3D();
        mat.AlbedoColor = new Color(0.3f, 0.8f, 1.0f, 0.9f); // bright cyan/blue
        mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        beam.MaterialOverride = mat;

        // Position at midpoint between origin and target
        Vector3 midpoint = (origin + target) / 2f;
        beam.GlobalPosition = midpoint;

        // Orient the cylinder along the beam direction
        // CylinderMesh is Y-aligned by default, so we need to rotate it
        Vector3 direction = (target - origin).Normalized();
        if (direction.LengthSquared() > 0.001f)
        {
            // Use LookAt with a perpendicular up vector, then rotate 90° on X
            // to align the cylinder's Y-axis with the beam direction
            beam.LookAt(target, Vector3.Up);
            beam.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2f);
        }

        // No collision
        beam.SetLayerMaskValue(1, false);

        AddToWorld(beam);

        // Fade out after BeamFadeDuration using a tween
        var tween = CreateTween();
        tween.TweenProperty(mat, "albedo_color",
            new Color(mat.AlbedoColor.R, mat.AlbedoColor.G, mat.AlbedoColor.B, 0f),
            BeamFadeDuration);
        tween.TweenCallback(Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(beam))
                beam.QueueFree();
        }));
    }

    // ── Projectile Movement Updates ──────────────────────────────────

    private static void UpdateTracer(ActiveProjectile proj)
    {
        // Linear lerp from origin to target
        proj.Node.GlobalPosition = proj.Origin.Lerp(proj.Target, proj.Progress);

        // Keep oriented toward target
        Vector3 direction = (proj.Target - proj.Origin).Normalized();
        if (direction.LengthSquared() > 0.001f)
        {
            proj.Node.LookAt(proj.Target, Vector3.Up);
        }
    }

    private static void UpdateMissile(ActiveProjectile proj)
    {
        // Guided linear movement toward target (slight curve could be added later)
        proj.Node.GlobalPosition = proj.Origin.Lerp(proj.Target, proj.Progress);

        // Keep oriented toward target
        Vector3 direction = (proj.Target - proj.Node.GlobalPosition).Normalized();
        if (direction.LengthSquared() > 0.001f)
        {
            proj.Node.LookAt(proj.Target, Vector3.Up);
        }
    }

    private static void UpdateArcingShell(ActiveProjectile proj)
    {
        // Quadratic bezier arc
        proj.Node.GlobalPosition = ComputeArcPosition(
            proj.Origin, proj.Target, proj.ArcHeight, proj.Progress);
    }

    // ── Projectile Cleanup ───────────────────────────────────────────

    private void FreeProjectile(ActiveProjectile proj)
    {
        if (!GodotObject.IsInstanceValid(proj.Node))
            return;

        // Stop trail particles before freeing
        if (proj.TrailParticles != null && GodotObject.IsInstanceValid(proj.TrailParticles))
        {
            proj.TrailParticles.Emitting = false;
        }

        if (proj.IsPooled && proj.Node is MeshInstance3D meshNode)
        {
            // Return to pool instead of freeing
            meshNode.Visible = false;
            meshNode.GlobalPosition = Vector3.Zero;
            _tracerPool.Enqueue(meshNode);
        }
        else
        {
            proj.Node.QueueFree();
        }
    }

    // ── Utility ──────────────────────────────────────────────────────

    private void AddToWorld(Node3D node)
    {
        Node? root = (_worldRoot as Node) ?? GetParent();
        if (root == null)
        {
            node.QueueFree();
            return;
        }
        root.AddChild(node);
    }

    // ── Internal Data Structures ─────────────────────────────────────

    private struct PendingFire
    {
        public Vector3 Origin;
        public int WeaponType;
        public double Timestamp;
    }

    private struct ActiveProjectile
    {
        public Node3D Node;
        public Vector3 Origin;
        public Vector3 Target;
        public ProjectileVisualType VisualType;
        public float Progress;
        public float FlightDuration;
        public float ElapsedTime;
        public float ArcHeight;
        public bool IsPooled;
        public GpuParticles3D? TrailParticles;
    }
}
