using System;
using System.Collections.Generic;
using Godot;
using CorditeWars.Core;
using CorditeWars.Game.Buildings;
using CorditeWars.Game.Units;
using CorditeWars.Systems.Graphics;

namespace CorditeWars.Game.VFX;

/// <summary>
/// Manages persistent death effects (debris, wreckage, rubble, smoke columns)
/// that outlive the instant particle burst already handled by <see cref="CombatVFXBridge"/>.
///
/// Subscribes to EventBus.UnitDeath and EventBus.BuildingDestroyed.
/// Never modifies simulation state.
/// </summary>
public partial class DeathVFXController : Node3D
{
    // ── Constants ────────────────────────────────────────────────────

    private const float DebrisGravity = 9.8f;
    private const float GroundY = 0f;
    private const float SweepIntervalSeconds = 2.0f;

    // Wreckage durations by category tier
    private const float WreckageDurationMedium = 8f;   // LightVehicle/APC/Support
    private const float WreckageDurationHeavy = 12f;   // HeavyVehicle/Tank/Artillery
    private const float WreckageDurationAir = 10f;     // Helicopter/Jet/Naval

    private const float SmokeColumnDuration = 10f;
    private const float FireDuration = 6f;
    private const float BuildingCollapseDuration = 1f;
    private const float StaggeredExplosionInterval = 0.5f / 3f; // 3 explosions over 0.5s

    // ── State ────────────────────────────────────────────────────────

    private Node3D? _worldRoot;
    private readonly List<DebrisFragment> _activeDebris = new();
    private readonly List<WreckageEntry> _activeWreckage = new();
    private float _sweepTimer;

    // ── Initialization ───────────────────────────────────────────────

    /// <summary>
    /// Subscribes to EventBus signals and stores the world root for spawning.
    /// </summary>
    public void Initialize(Node3D? worldRoot = null)
    {
        _worldRoot = worldRoot;

        var bus = EventBus.Instance;
        if (bus == null)
        {
            GD.PushError("[DeathVFXController] EventBus not available.");
            return;
        }

        bus.UnitDeath += OnUnitDeath;
        bus.BuildingDestroyed += OnBuildingDestroyed;

        GD.Print("[DeathVFXController] Initialized — listening for death/destruction events.");
    }

    public override void _ExitTree()
    {
        var bus = EventBus.Instance;
        if (bus != null)
        {
            bus.UnitDeath -= OnUnitDeath;
            bus.BuildingDestroyed -= OnBuildingDestroyed;
        }
    }

    // ── Per-frame update (debris physics + sweep timer) ──────────────

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // Update debris physics
        ProcessDebris(dt);

        // Periodic wreckage sweep
        _sweepTimer += dt;
        if (_sweepTimer >= SweepIntervalSeconds)
        {
            _sweepTimer = 0f;
            SweepWreckage();
        }
    }

    // ── Signal Handlers ──────────────────────────────────────────────

    private void OnUnitDeath(int unitId, int unitCategory, Vector3 position)
    {
        var category = (UnitCategory)unitCategory;
        var (debrisCount, wreckageDuration) = GetDeathVFXScale(category);

        // Spawn debris fragments (not for infantry)
        if (debrisCount > 0)
        {
            SpawnUnitDebris(debrisCount, position);
        }

        // Spawn wreckage (not for infantry)
        if (wreckageDuration > 0f)
        {
            SpawnUnitWreckage(position, wreckageDuration);
        }
    }

    private void OnBuildingDestroyed(Node building)
    {
        if (building is not BuildingInstance buildingInstance)
            return;

        SpawnBuildingCollapse(buildingInstance);
    }

    // ── VFX Scale Mapping ────────────────────────────────────────────

    /// <summary>
    /// Returns (debrisCount, wreckageDuration) scaled by unit category.
    /// Infantry: no debris, no wreckage (small dust puff only — handled by CombatVFXBridge).
    /// </summary>
    private static (int debrisCount, float wreckageDuration) GetDeathVFXScale(UnitCategory category)
    {
        return category switch
        {
            // Infantry: small dust puff only, no debris, no wreckage
            UnitCategory.Infantry or UnitCategory.Special =>
                (0, 0f),

            // LightVehicle/APC/Support (categories 1-3): medium explosion, 2-4 debris, wreckage 8s
            UnitCategory.LightVehicle or UnitCategory.APC or UnitCategory.Support =>
                (3, WreckageDurationMedium),

            // HeavyVehicle/Tank/Artillery (categories 4-6): large explosion, 4-6 debris, wreckage 12s
            UnitCategory.HeavyVehicle or UnitCategory.Tank or UnitCategory.Artillery =>
                (5, WreckageDurationHeavy),

            // Helicopter/Jet/Naval (categories 7-9): large explosion, 3-5 debris, wreckage 10s
            UnitCategory.Helicopter or UnitCategory.Jet or UnitCategory.Destroyer
                or UnitCategory.Submarine or UnitCategory.CapitalShip =>
                (4, WreckageDurationAir),

            // Fallback: treat like light vehicle
            _ => (3, WreckageDurationMedium),
        };
    }

    // ── Debris Spawning ──────────────────────────────────────────────

    /// <summary>
    /// Creates simple mesh fragments (BoxMesh/SphereMesh) with random velocity
    /// and gravity. Debris count is scaled by QualityManager.ParticleMultiplier
    /// and halved on Potato tier.
    /// </summary>
    private void SpawnUnitDebris(int baseCount, Vector3 position)
    {
        int count = ScaleDebrisCount(baseCount);

        for (int i = 0; i < count; i++)
        {
            var fragment = new MeshInstance3D();

            // Alternate between box and sphere meshes
            if (i % 2 == 0)
            {
                var box = new BoxMesh();
                float size = RandRange(0.15f, 0.4f);
                box.Size = new Vector3(size, size, size);
                fragment.Mesh = box;
            }
            else
            {
                var sphere = new SphereMesh();
                sphere.Radius = RandRange(0.1f, 0.25f);
                sphere.Height = sphere.Radius * 2f;
                fragment.Mesh = sphere;
            }

            // Dark metallic material for debris
            var mat = new StandardMaterial3D();
            mat.AlbedoColor = new Color(
                RandRange(0.15f, 0.35f),
                RandRange(0.12f, 0.25f),
                RandRange(0.1f, 0.2f));
            mat.Roughness = 0.8f;
            fragment.MaterialOverride = mat;

            // Position at death location with slight random offset
            fragment.GlobalPosition = position + new Vector3(
                RandRange(-0.5f, 0.5f),
                RandRange(0.2f, 0.8f),
                RandRange(-0.5f, 0.5f));

            // Random velocity: upward + outward
            var velocity = new Vector3(
                RandRange(-4f, 4f),
                RandRange(3f, 8f),
                RandRange(-4f, 4f));

            AddToWorld(fragment);

            _activeDebris.Add(new DebrisFragment
            {
                Node = fragment,
                Velocity = velocity
            });
        }
    }

    // ── Wreckage Spawning ────────────────────────────────────────────

    /// <summary>
    /// Creates a darkened mesh at the death position that persists for the
    /// configured duration, then fades out via tween before QueueFree.
    /// Lifetime is halved on Potato tier.
    /// </summary>
    private void SpawnUnitWreckage(Vector3 position, float baseDuration)
    {
        float duration = ScaleWreckageLifetime(baseDuration);

        var wreckage = new MeshInstance3D();
        var box = new BoxMesh();
        box.Size = new Vector3(
            RandRange(1.0f, 1.8f),
            RandRange(0.3f, 0.6f),
            RandRange(1.0f, 1.8f));
        wreckage.Mesh = box;

        // Darkened, burnt material
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = new Color(0.1f, 0.08f, 0.06f);
        mat.Roughness = 1.0f;
        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        wreckage.MaterialOverride = mat;

        // Place at ground level, slightly sunk
        wreckage.GlobalPosition = new Vector3(position.X, GroundY + 0.15f, position.Z);
        // Slight random rotation for variety
        wreckage.RotateY(RandRange(0f, Mathf.Tau));

        AddToWorld(wreckage);

        _activeWreckage.Add(new WreckageEntry
        {
            Node = wreckage,
            Material = mat,
            SpawnTime = Time.GetTicksMsec() / 1000.0,
            Lifetime = duration,
            FadingOut = false
        });
    }

    // ── Building Collapse ────────────────────────────────────────────

    /// <summary>
    /// Triggers a scale-to-zero collapse over 1 second, spawns rubble mesh
    /// at footprint, smoke column (10s), and fire particles (6s).
    /// Large buildings (footprint > 2×2) get 3 staggered explosions over 0.5s.
    /// </summary>
    private void SpawnBuildingCollapse(BuildingInstance building)
    {
        var position = building.GlobalPosition;
        int footprintWidth = building.Data?.FootprintWidth ?? 3;
        int footprintHeight = building.Data?.FootprintHeight ?? 3;
        bool isLarge = footprintWidth > 2 || footprintHeight > 2;

        // Staggered explosions for large buildings
        if (isLarge)
        {
            SpawnStaggeredExplosions(position, footprintWidth, footprintHeight);
        }

        // Spawn rubble at footprint
        SpawnRubble(position, footprintWidth, footprintHeight);

        // Spawn smoke column
        SpawnSmokeColumn(position, SmokeColumnDuration);

        // Spawn fire particles
        SpawnFire(position, FireDuration);
    }

    // ── Staggered Explosions ─────────────────────────────────────────

    /// <summary>
    /// Spawns 3 explosions over 0.5s at random positions within the building footprint.
    /// </summary>
    private void SpawnStaggeredExplosions(Vector3 center, int width, int height)
    {
        for (int i = 0; i < 3; i++)
        {
            float delay = i * StaggeredExplosionInterval;
            Vector3 offset = new Vector3(
                RandRange(-width * 0.4f, width * 0.4f),
                RandRange(0f, 2f),
                RandRange(-height * 0.4f, height * 0.4f));

            // Use a timer to stagger the explosions
            var timer = GetTree().CreateTimer(delay);
            var explosionPos = center + offset;
            timer.Timeout += () =>
            {
                var explosion = ParticleFactory.CreateExplosionLarge();
                explosion.GlobalPosition = explosionPos;
                AddToWorld(explosion);
            };
        }
    }

    // ── Rubble Spawning ──────────────────────────────────────────────

    /// <summary>
    /// Creates a rubble mesh at the building footprint. Rubble is managed
    /// by the wreckage sweep system.
    /// </summary>
    private void SpawnRubble(Vector3 position, int footprintWidth, int footprintHeight)
    {
        float duration = ScaleWreckageLifetime(20f); // Rubble lasts 20s base

        var rubble = new MeshInstance3D();
        var box = new BoxMesh();
        box.Size = new Vector3(
            footprintWidth * 0.8f,
            RandRange(0.3f, 0.7f),
            footprintHeight * 0.8f);
        rubble.Mesh = box;

        // Grey rubble material
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = new Color(0.25f, 0.22f, 0.2f);
        mat.Roughness = 1.0f;
        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        rubble.MaterialOverride = mat;

        rubble.GlobalPosition = new Vector3(position.X, GroundY + 0.2f, position.Z);
        rubble.RotateY(RandRange(0f, Mathf.Tau));

        AddToWorld(rubble);

        _activeWreckage.Add(new WreckageEntry
        {
            Node = rubble,
            Material = mat,
            SpawnTime = Time.GetTicksMsec() / 1000.0,
            Lifetime = duration,
            FadingOut = false
        });
    }

    // ── Smoke Column ─────────────────────────────────────────────────

    /// <summary>
    /// Creates a GpuParticles3D smoke column that emits for the specified duration.
    /// Particle count scaled by QualityManager.ParticleMultiplier.
    /// </summary>
    private void SpawnSmokeColumn(Vector3 position, float duration)
    {
        int baseCount = 30;
        int count = ScaleParticleCount(baseCount);

        var material = new ParticleProcessMaterial();
        material.Direction = new Vector3(0, 1, 0);
        material.Spread = 15f;
        material.InitialVelocityMin = 1.5f;
        material.InitialVelocityMax = 3f;
        material.Gravity = new Vector3(0, 0.2f, 0);
        material.ScaleMin = 0.5f;
        material.ScaleMax = 1.5f;
        material.DampingMin = 1f;
        material.DampingMax = 1f;

        var colorRamp = new GradientTexture1D();
        var gradient = new Gradient();
        gradient.SetColor(0, new Color(0.3f, 0.3f, 0.3f, 0.7f));
        gradient.SetColor(1, new Color(0.2f, 0.2f, 0.2f, 0.0f));
        colorRamp.Gradient = gradient;
        material.ColorRamp = colorRamp;

        var particles = new GpuParticles3D();
        particles.Amount = count > 0 ? count : 1;
        particles.Lifetime = 3.0f;
        particles.OneShot = false;
        particles.Explosiveness = 0f;
        particles.ProcessMaterial = material;
        particles.Emitting = true;

        var drawMesh = new QuadMesh();
        drawMesh.Size = new Vector2(0.5f, 0.5f);
        var drawMat = new StandardMaterial3D();
        drawMat.BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles;
        drawMat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        drawMat.VertexColorUseAsAlbedo = true;
        drawMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        drawMesh.Material = drawMat;
        particles.DrawPass1 = drawMesh;

        particles.GlobalPosition = position + new Vector3(0, 0.5f, 0);
        AddToWorld(particles);

        // Stop emitting after duration, then free after particles finish
        var timer = GetTree().CreateTimer(duration);
        timer.Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(particles))
            {
                particles.Emitting = false;
                // Give remaining particles time to fade
                var cleanupTimer = GetTree().CreateTimer(particles.Lifetime + 0.5f);
                cleanupTimer.Timeout += () =>
                {
                    if (GodotObject.IsInstanceValid(particles))
                        particles.QueueFree();
                };
            }
        };
    }

    // ── Fire Particles ───────────────────────────────────────────────

    /// <summary>
    /// Creates GpuParticles3D fire effect that emits for the specified duration.
    /// Particle count scaled by QualityManager.ParticleMultiplier.
    /// </summary>
    private void SpawnFire(Vector3 position, float duration)
    {
        int baseCount = 25;
        int count = ScaleParticleCount(baseCount);

        var material = new ParticleProcessMaterial();
        material.Direction = new Vector3(0, 1, 0);
        material.Spread = 25f;
        material.InitialVelocityMin = 1f;
        material.InitialVelocityMax = 2.5f;
        material.Gravity = new Vector3(0, 0.5f, 0);
        material.ScaleMin = 0.2f;
        material.ScaleMax = 0.6f;

        var colorRamp = new GradientTexture1D();
        var gradient = new Gradient();
        gradient.SetColor(0, new Color(1.0f, 0.6f, 0.0f, 0.9f));
        gradient.SetColor(1, new Color(1.0f, 0.1f, 0.0f, 0.0f));
        colorRamp.Gradient = gradient;
        material.ColorRamp = colorRamp;

        var particles = new GpuParticles3D();
        particles.Amount = count > 0 ? count : 1;
        particles.Lifetime = 1.5f;
        particles.OneShot = false;
        particles.Explosiveness = 0f;
        particles.ProcessMaterial = material;
        particles.Emitting = true;

        var drawMesh = new QuadMesh();
        drawMesh.Size = new Vector2(0.3f, 0.3f);
        var drawMat = new StandardMaterial3D();
        drawMat.BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles;
        drawMat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        drawMat.VertexColorUseAsAlbedo = true;
        drawMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        drawMesh.Material = drawMat;
        particles.DrawPass1 = drawMesh;

        particles.GlobalPosition = position + new Vector3(0, 0.3f, 0);
        AddToWorld(particles);

        // Stop emitting after duration, then free after particles finish
        var timer = GetTree().CreateTimer(duration);
        timer.Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(particles))
            {
                particles.Emitting = false;
                var cleanupTimer = GetTree().CreateTimer(particles.Lifetime + 0.5f);
                cleanupTimer.Timeout += () =>
                {
                    if (GodotObject.IsInstanceValid(particles))
                        particles.QueueFree();
                };
            }
        };
    }

    // ── Debris Physics ───────────────────────────────────────────────

    /// <summary>
    /// Applies gravity to active debris fragments and removes them when they
    /// hit the ground (Y ≤ 0).
    /// </summary>
    private void ProcessDebris(float dt)
    {
        for (int i = _activeDebris.Count - 1; i >= 0; i--)
        {
            var debris = _activeDebris[i];

            if (!GodotObject.IsInstanceValid(debris.Node))
            {
                _activeDebris.RemoveAt(i);
                continue;
            }

            // Apply gravity
            debris.Velocity += new Vector3(0, -DebrisGravity * dt, 0);
            debris.Node.GlobalPosition += debris.Velocity * dt;

            // Rotate for visual interest
            debris.Node.RotateX(dt * 3f);
            debris.Node.RotateZ(dt * 2f);

            // Check if landed
            if (debris.Node.GlobalPosition.Y <= GroundY)
            {
                debris.Node.QueueFree();
                _activeDebris.RemoveAt(i);
            }

            _activeDebris[i] = debris;
        }
    }

    // ── Wreckage Lifetime Sweep ──────────────────────────────────────

    /// <summary>
    /// Periodic sweep (every 2s) checks wreckage ages and starts fade tweens
    /// for expired entries.
    /// </summary>
    private void SweepWreckage()
    {
        double now = Time.GetTicksMsec() / 1000.0;

        for (int i = _activeWreckage.Count - 1; i >= 0; i--)
        {
            var entry = _activeWreckage[i];

            if (!GodotObject.IsInstanceValid(entry.Node))
            {
                _activeWreckage.RemoveAt(i);
                continue;
            }

            double age = now - entry.SpawnTime;

            if (age >= entry.Lifetime && !entry.FadingOut)
            {
                // Start fade-out tween
                entry.FadingOut = true;
                _activeWreckage[i] = entry;

                var node = entry.Node;
                var mat = entry.Material;

                if (mat != null)
                {
                    // Fade alpha from current to 0 over 1 second
                    var tween = CreateTween();
                    var startColor = mat.AlbedoColor;
                    var endColor = new Color(startColor.R, startColor.G, startColor.B, 0f);
                    tween.TweenProperty(mat, "albedo_color", endColor, 1.0f);
                    tween.TweenCallback(Callable.From(() =>
                    {
                        if (GodotObject.IsInstanceValid(node))
                            node.QueueFree();
                    }));
                }
                else
                {
                    node.QueueFree();
                    _activeWreckage.RemoveAt(i);
                }
            }
        }
    }

    // ── Quality Scaling Helpers ──────────────────────────────────────

    /// <summary>
    /// Scales debris count by ParticleMultiplier. On Potato tier, halves the count.
    /// </summary>
    private static int ScaleDebrisCount(int baseCount)
    {
        float multiplier = QualityManager.Instance?.ParticleMultiplier ?? 1f;
        int scaled = (int)(baseCount * multiplier);

        // Potato tier: halve debris count
        if (QualityManager.Instance?.CurrentTier == QualityTier.Potato)
        {
            scaled = scaled / 2;
        }

        return scaled > 0 ? scaled : 1;
    }

    /// <summary>
    /// Scales particle count by QualityManager.ParticleMultiplier.
    /// </summary>
    private static int ScaleParticleCount(int baseCount)
    {
        float multiplier = QualityManager.Instance?.ParticleMultiplier ?? 1f;
        int scaled = (int)(baseCount * multiplier);
        return scaled > 0 ? scaled : 1;
    }

    /// <summary>
    /// Scales wreckage lifetime. On Potato tier, shortens by 50%.
    /// </summary>
    private static float ScaleWreckageLifetime(float baseDuration)
    {
        if (QualityManager.Instance?.CurrentTier == QualityTier.Potato)
        {
            return baseDuration * 0.5f;
        }
        return baseDuration;
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

    private static float RandRange(float min, float max)
    {
        return (float)(GD.Randf() * (max - min) + min);
    }

    // ── Internal Data Structures ─────────────────────────────────────

    private struct DebrisFragment
    {
        public MeshInstance3D Node;
        public Vector3 Velocity;
    }

    private struct WreckageEntry
    {
        public MeshInstance3D Node;
        public StandardMaterial3D? Material;
        public double SpawnTime;
        public float Lifetime;
        public bool FadingOut;
    }
}
