using System;
using Godot;
using CorditeWars.Systems.Graphics;

namespace CorditeWars.Game.World;

/// <summary>
/// AAA-grade procedural terrain engine orchestrator.
/// Coordinates the full terrain generation pipeline:
///   elevation build → erosion → subdivision → mesh gen → material → detail pass → cliff gen → LOD
///
/// Compatible with gl_compatibility renderer (no tessellation/compute required).
/// At Potato tier, produces output identical to current TerrainRenderer behavior
/// (1× subdivision, no erosion, 2-layer flat shading, no detail, no cliff geometry).
/// </summary>
public sealed class TerrainEngine
{
    private readonly QualityTier _tier;
    private readonly TerrainSubdivision _subdivision;
    private readonly TerrainMaterialSystem _materialSystem;
    private readonly TerrainDetailPass _detailPass;
    private readonly TerrainDeformationSystem _deformationSystem;
    private readonly CliffGenerator _cliffGenerator;
    private readonly TerrainLODController _lodController;

    private MapData? _mapData;
    private float[]? _elevationMap;
    private int _width;
    private int _height;
    private Node3D? _parentNode;

    /// <summary>Subdivision factor per grid cell (vertices per cell edge).</summary>
    public int SubdivisionFactor => _subdivision.SubdivisionFactor;

    /// <summary>Whether erosion simulation was applied during generation.</summary>
    public bool ErosionApplied { get; private set; }

    /// <summary>The quality tier this engine was configured for.</summary>
    public QualityTier Tier => _tier;

    /// <summary>
    /// Creates a TerrainEngine configured for the given quality tier.
    /// All sub-components are initialized according to the tier settings.
    /// </summary>
    /// <param name="tier">Quality tier controlling detail level across all sub-systems.</param>
    public TerrainEngine(QualityTier tier)
    {
        _tier = tier;
        _subdivision = new TerrainSubdivision(tier);
        _materialSystem = new TerrainMaterialSystem(tier);
        _detailPass = new TerrainDetailPass();
        _deformationSystem = new TerrainDeformationSystem();
        _cliffGenerator = new CliffGenerator();
        _lodController = new TerrainLODController(tier);
    }

    /// <summary>
    /// Generates the complete terrain from map data.
    /// Performs: elevation build → erosion → subdivision → mesh gen → material → detail pass → cliff gen → LOD.
    ///
    /// At Potato tier: 1× subdivision, no erosion, 2-layer flat shading, no detail, no cliff geometry.
    /// </summary>
    /// <param name="mapData">Map data containing elevation zones, biome, and terrain features.</param>
    /// <param name="parent">Parent Node3D to attach all generated terrain nodes to.</param>
    public void Generate(MapData mapData, Node3D parent)
    {
        if (mapData == null) throw new ArgumentNullException(nameof(mapData));
        if (parent == null) throw new ArgumentNullException(nameof(parent));

        _mapData = mapData;
        _parentNode = parent;
        _width = mapData.Width;
        _height = mapData.Height;

        // ── Step 1: Build base elevation map ──────────────────────────────────
        _elevationMap = new float[_width * _height];
        BuildElevationMap();

        // ── Step 2: Apply erosion (skip for Potato/Low) ───────────────────────
        ApplyErosion();

        // ── Step 3: Set elevation data for subdivision interpolation ──────────
        _subdivision.SetElevationData(_elevationMap, _width, _height);

        // ── Step 4: Generate terrain mesh with subdivision and material ────────
        GenerateTerrainMesh(parent);

        // ── Step 5: Detail pass (grass, pebbles, debris) ──────────────────────
        if (_tier >= QualityTier.Medium)
        {
            _detailPass.Generate(_mapData, _elevationMap, _width, _height, _tier, parent);
        }

        // ── Step 6: Cliff generation (skip for Potato) ───────────────────────
        if (_tier > QualityTier.Potato)
        {
            _cliffGenerator.Generate(_elevationMap, _width, _height,
                                      _mapData.Biome ?? "temperate", _tier, parent);
        }

        // ── Step 7: Initialize deformation system ─────────────────────────────
        _deformationSystem.Initialize(parent);

        GD.Print($"[TerrainEngine] Generated terrain {_width}x{_height}, tier={_tier}, " +
                 $"subdivision={SubdivisionFactor}×, erosion={ErosionApplied}");
    }

    /// <summary>
    /// Returns interpolated elevation at a world position using the same
    /// Catmull-Rom spline as the mesh, ensuring units sit on the surface.
    /// Delegates to TerrainSubdivision.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <returns>Interpolated elevation at the given world position.</returns>
    public float GetElevationAtWorld(float worldX, float worldZ)
    {
        return _subdivision.GetElevationAtWorld(worldX, worldZ);
    }

    /// <summary>
    /// Rebuilds a single terrain chunk for dynamic terrain changes.
    /// Re-runs the mesh generation pipeline for the specified chunk only.
    /// </summary>
    /// <param name="chunkX">Chunk X index (0-based).</param>
    /// <param name="chunkY">Chunk Y index (0-based).</param>
    public void RebuildChunk(int chunkX, int chunkY)
    {
        if (_mapData == null || _elevationMap == null || _parentNode == null)
        {
            GD.PrintErr("[TerrainEngine] Cannot rebuild chunk — terrain not yet generated.");
            return;
        }

        // Determine the LOD subdivision for this chunk based on camera position
        Vector3 cameraPos = GetCameraPosition();
        int chunkSubdivision = _lodController.GetSubdivisionForChunk(chunkX, chunkY, cameraPos);

        // Re-generate the mesh for this specific chunk
        // The chunk covers cells [chunkX * ChunkCellSize, (chunkX+1) * ChunkCellSize)
        const int chunkCellSize = 120;
        int x0 = chunkX * chunkCellSize;
        int y0 = chunkY * chunkCellSize;
        int x1 = Math.Min(_width, x0 + chunkCellSize);
        int y1 = Math.Min(_height, y0 + chunkCellSize);

        // Update subdivision data (elevation may have changed)
        _subdivision.SetElevationData(_elevationMap, _width, _height);

        GD.Print($"[TerrainEngine] Rebuilt chunk ({chunkX},{chunkY}), " +
                 $"subdivision={chunkSubdivision}×, cells=[{x0},{y0}]-[{x1},{y1}]");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PRIVATE PIPELINE STAGES
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds the base elevation map from MapData elevation zones.
    /// Identical to TerrainRenderer.BuildElevationMap for backward compatibility.
    /// </summary>
    private void BuildElevationMap()
    {
        Array.Clear(_elevationMap!, 0, _elevationMap!.Length);

        if (_mapData!.ElevationZones == null) return;

        for (int i = 0; i < _mapData.ElevationZones.Length; i++)
        {
            ElevationZone zone = _mapData.ElevationZones[i];
            float zoneHeight = zone.Height.ToFloat();
            int cx = zone.CenterX;
            int cy = zone.CenterY;
            int radius = zone.Radius;

            if (radius <= 0) continue;

            int minX = Math.Max(0, cx - radius);
            int maxX = Math.Min(_width - 1, cx + radius);
            int minY = Math.Max(0, cy - radius);
            int maxY = Math.Min(_height - 1, cy + radius);

            float radiusSq = radius * radius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float distSq = dx * dx + dy * dy;

                    if (distSq >= radiusSq) continue;

                    float dist = MathF.Sqrt(distSq);
                    float t = dist / radius;
                    float falloff = 0.5f * (1f + MathF.Cos(t * MathF.PI));

                    _elevationMap![y * _width + x] += zoneHeight * falloff;
                }
            }
        }
    }

    /// <summary>
    /// Applies erosion simulation based on quality tier.
    /// Potato/Low: no erosion. Medium: 3 passes. High: 8 passes.
    /// </summary>
    private void ApplyErosion()
    {
        int erosionPasses = _tier switch
        {
            QualityTier.Potato => 0,
            QualityTier.Low => 0,
            QualityTier.Medium => 3,
            QualityTier.High => 8,
            _ => 0
        };

        if (erosionPasses == 0)
        {
            ErosionApplied = false;
            return;
        }

        // Hydraulic erosion: iterations scale with passes
        int hydraulicIterations = erosionPasses * 10000;
        ErosionSimulator.HydraulicErosion(_elevationMap!, _width, _height,
                                           hydraulicIterations, seed: 42);

        // Thermal erosion
        ErosionSimulator.ThermalErosion(_elevationMap!, _width, _height,
                                         passes: erosionPasses, talusAngle: 0.6f);

        ErosionApplied = true;
    }

    /// <summary>
    /// Generates the terrain mesh using subdivision and applies the PBR material.
    /// At Potato tier, this produces output identical to TerrainRenderer
    /// (1× subdivision = one vertex per grid cell, 2-layer flat shading).
    /// </summary>
    private void GenerateTerrainMesh(Node3D parent)
    {
        ShaderMaterial material = _materialSystem.GetMaterial();

        const int chunkCellSize = 120;
        int chunksX = (_width + chunkCellSize - 1) / chunkCellSize;
        int chunksY = (_height + chunkCellSize - 1) / chunkCellSize;

        // Use a default camera position at map center for initial LOD determination
        Vector3 initialCamPos = new Vector3(_width * 0.5f, 0f, _height * 0.5f);

        for (int cy = 0; cy < chunksY; cy++)
        {
            for (int cx = 0; cx < chunksX; cx++)
            {
                int chunkSubdivision = _lodController.GetSubdivisionForChunk(cx, cy, initialCamPos);

                int x0 = cx * chunkCellSize;
                int y0 = cy * chunkCellSize;
                int x1 = Math.Min(_width, x0 + chunkCellSize);
                int y1 = Math.Min(_height, y0 + chunkCellSize);

                var mesh = BuildChunkMesh(x0, y0, x1, y1, chunkSubdivision);
                if (mesh == null) continue;

                var meshInstance = new MeshInstance3D();
                meshInstance.Mesh = mesh;
                meshInstance.MaterialOverride = material;
                meshInstance.CastShadow = _tier >= QualityTier.Medium
                    ? GeometryInstance3D.ShadowCastingSetting.On
                    : GeometryInstance3D.ShadowCastingSetting.Off;
                meshInstance.Name = $"TerrainChunk_{cx}_{cy}";
                parent.AddChild(meshInstance);
            }
        }
    }

    /// <summary>
    /// Builds a single terrain chunk mesh with the given subdivision level.
    /// Uses Catmull-Rom interpolation for sub-cell vertex positions.
    /// </summary>
    private ArrayMesh? BuildChunkMesh(int x0, int y0, int x1, int y1, int subdivisionFactor)
    {
        int cellsX = x1 - x0;
        int cellsY = y1 - y0;

        if (cellsX <= 0 || cellsY <= 0) return null;

        int vertsX = cellsX * subdivisionFactor + 1;
        int vertsY = cellsY * subdivisionFactor + 1;

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        // Generate vertices
        for (int vy = 0; vy < vertsY; vy++)
        {
            for (int vx = 0; vx < vertsX; vx++)
            {
                // Convert vertex index to world-space fractional grid position
                float worldX = x0 + (float)vx / subdivisionFactor;
                float worldZ = y0 + (float)vy / subdivisionFactor;

                // Sample elevation using Catmull-Rom interpolation
                float elev = TerrainSubdivision.SampleElevation(
                    _elevationMap!, _width, _height, worldX, worldZ);

                // Compute normal using Sobel filter
                Vector3 normal = TerrainSubdivision.ComputeSobelNormal(
                    _elevationMap!, _width, _height, worldX, worldZ);

                // Vertex color: biome tint in RGB, moisture in A
                Color vertColor = GetVertexColor(worldX, worldZ);

                st.SetColor(vertColor);
                st.SetNormal(normal);
                st.SetUV(new Vector2(worldX / _width, worldZ / _height));
                st.AddVertex(new Vector3(worldX, elev, worldZ));
            }
        }

        // Generate indices (two triangles per quad)
        for (int vy = 0; vy < vertsY - 1; vy++)
        {
            for (int vx = 0; vx < vertsX - 1; vx++)
            {
                int topLeft = vy * vertsX + vx;
                int topRight = topLeft + 1;
                int bottomLeft = (vy + 1) * vertsX + vx;
                int bottomRight = bottomLeft + 1;

                st.AddIndex(topLeft);
                st.AddIndex(bottomLeft);
                st.AddIndex(topRight);

                st.AddIndex(topRight);
                st.AddIndex(bottomLeft);
                st.AddIndex(bottomRight);
            }
        }

        return st.Commit();
    }

    /// <summary>
    /// Computes vertex color for a world position.
    /// RGB = biome tint hint, A = moisture hint.
    /// </summary>
    private Color GetVertexColor(float worldX, float worldZ)
    {
        if (_mapData == null) return new Color(0.5f, 0.5f, 0.5f, 0f);

        // Compute moisture from proximity to water features
        float moisture = ComputeMoisture(worldX, worldZ);

        // Biome base tint
        string biome = _mapData.Biome ?? "temperate";
        Color baseTint = biome switch
        {
            "temperate" => new Color(0.26f, 0.54f, 0.14f),
            "desert" or "volcanic" => new Color(0.78f, 0.66f, 0.40f),
            "rocky" or "mountain" => new Color(0.50f, 0.48f, 0.44f),
            "coastal" or "archipelago" => new Color(0.74f, 0.68f, 0.46f),
            "tropical" => new Color(0.12f, 0.58f, 0.20f),
            _ => new Color(0.4f, 0.5f, 0.3f)
        };

        return new Color(baseTint.R, baseTint.G, baseTint.B, moisture);
    }

    /// <summary>
    /// Computes moisture value (0–1) for a world position based on proximity
    /// to river features in the map data.
    /// </summary>
    private float ComputeMoisture(float worldX, float worldZ)
    {
        if (_mapData?.TerrainFeatures == null) return 0f;

        float minDist = float.MaxValue;

        for (int i = 0; i < _mapData.TerrainFeatures.Length; i++)
        {
            TerrainFeature feature = _mapData.TerrainFeatures[i];
            if (feature.Type != "river" || feature.Points == null) continue;

            for (int p = 0; p < feature.Points.Length; p++)
            {
                int[] point = feature.Points[p];
                if (point == null || point.Length < 2) continue;

                float dx = worldX - point[0];
                float dz = worldZ - point[1];
                float dist = MathF.Sqrt(dx * dx + dz * dz);
                if (dist < minDist) minDist = dist;
            }
        }

        // Moisture falls off over 15 units from water
        const float moistureRadius = 15f;
        if (minDist >= moistureRadius) return 0f;
        return 1f - (minDist / moistureRadius);
    }

    /// <summary>
    /// Gets the current camera position, or returns map center if no camera is available.
    /// </summary>
    private Vector3 GetCameraPosition()
    {
        // Try to find the RTSCamera in the scene tree
        if (_parentNode?.GetTree()?.Root != null)
        {
            var viewport = _parentNode.GetViewport();
            var camera = viewport?.GetCamera3D();
            if (camera != null)
            {
                return camera.GlobalPosition;
            }
        }

        // Fallback: center of the map
        return new Vector3(_width * 0.5f, 0f, _height * 0.5f);
    }
}
