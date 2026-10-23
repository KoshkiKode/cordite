using System;
using System.Collections.Generic;
using Godot;
using CorditeWars.Systems.Graphics;

namespace CorditeWars.Game.World;

/// <summary>
/// Scatters instanced grass blades, pebbles, and debris across the terrain
/// using MultiMeshInstance3D for efficient rendering. Uses Poisson disk sampling
/// for natural-looking distribution and applies LOD fade beyond 80 units.
///
/// Density is controlled by quality tier:
///   - Potato/Low: no detail instances (returns early)
///   - Medium: sparse (~50K instances on a 128×128 map)
///   - High: dense (~200K instances on a 128×128 map)
/// </summary>
public sealed partial class TerrainDetailPass : Node3D
{
    /// <summary>Maximum distance from camera before detail instances are hidden.</summary>
    private const float LodFadeDistance = 80f;

    /// <summary>
    /// Minimum Poisson disk radius for Medium tier on a 128×128 map.
    /// Produces approximately 50K instances.
    /// </summary>
    private const float MediumMinDist = 0.55f;

    /// <summary>
    /// Minimum Poisson disk radius for High tier on a 128×128 map.
    /// Produces approximately 200K instances.
    /// </summary>
    private const float HighMinDist = 0.28f;

    /// <summary>Fraction of instances that are pebbles vs grass.</summary>
    private const float PebbleRatio = 0.2f;

    /// <summary>Maximum slope angle (in radians) for detail placement. ~45 degrees.</summary>
    private static readonly float MaxSlopeAngle = MathF.PI / 4f;

    private MultiMeshInstance3D? _grassMultiMesh;
    private MultiMeshInstance3D? _pebbleMultiMesh;
    private Camera3D? _camera;

    /// <summary>
    /// Scatters detail instances (grass, pebbles, debris) across the terrain.
    /// Uses Poisson disk sampling for natural distribution.
    /// </summary>
    /// <param name="mapData">Map data containing terrain features and biome info.</param>
    /// <param name="elevation">Flat elevation array (row-major, width × height).</param>
    /// <param name="width">Grid width in cells.</param>
    /// <param name="height">Grid height in cells.</param>
    /// <param name="tier">Quality tier controlling density.</param>
    /// <param name="parent">Parent node to attach MultiMesh instances to.</param>
    public void Generate(MapData mapData, float[] elevation, int width, int height,
                          QualityTier tier, Node3D parent)
    {
        // Potato and Low tiers: no detail instances
        if (tier <= QualityTier.Low)
            return;

        float minDist = tier == QualityTier.High ? HighMinDist : MediumMinDist;

        // Build exclusion maps for water and path cells
        bool[] isWater = BuildWaterMap(mapData, width, height);
        bool[] isPath = BuildPathMap(mapData, width, height);

        // Generate Poisson disk sample points
        List<Vector2> points = PoissonDiskSample(width, height, minDist, seed: 12345u);

        // Separate points into grass and pebble lists
        var grassTransforms = new List<Transform3D>();
        var pebbleTransforms = new List<Transform3D>();

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 pt = points[i];
            int cellX = (int)MathF.Floor(pt.X);
            int cellY = (int)MathF.Floor(pt.Y);

            // Clamp to grid bounds
            cellX = Math.Clamp(cellX, 0, width - 1);
            cellY = Math.Clamp(cellY, 0, height - 1);

            int idx = cellY * width + cellX;

            // Skip water cells
            if (isWater[idx])
                continue;

            // Skip path cells
            if (isPath[idx])
                continue;

            // Skip steep slopes (>45°)
            if (IsSteepSlope(elevation, width, height, pt.X, pt.Y))
                continue;

            // Get elevation at this point using bilinear interpolation
            float elev = SampleElevationBilinear(elevation, width, height, pt.X, pt.Y);

            // Deterministic random rotation and scale seeded by position
            uint posHash = HashPosition(pt.X, pt.Y);
            float rotation = (posHash & 0xFFFF) / 65535f * MathF.PI * 2f;
            float scaleVariation = 0.7f + ((posHash >> 16) & 0xFFFF) / 65535f * 0.6f;

            // Determine if this is a pebble or grass instance
            float typeSelector = ((posHash >> 8) & 0xFF) / 255f;

            var position = new Vector3(pt.X, elev, pt.Y);
            var basis = Basis.Identity
                .Rotated(Vector3.Up, rotation)
                .Scaled(new Vector3(scaleVariation, scaleVariation, scaleVariation));
            var transform = new Transform3D(basis, position);

            if (typeSelector < PebbleRatio)
                pebbleTransforms.Add(transform);
            else
                grassTransforms.Add(transform);
        }

        // Create grass MultiMesh
        if (grassTransforms.Count > 0)
        {
            _grassMultiMesh = CreateMultiMeshInstance(
                CreateGrassBladeMesh(),
                grassTransforms,
                parent);
        }

        // Create pebble MultiMesh
        if (pebbleTransforms.Count > 0)
        {
            _pebbleMultiMesh = CreateMultiMeshInstance(
                CreatePebbleMesh(),
                pebbleTransforms,
                parent);
        }

        GD.Print($"[TerrainDetailPass] Generated {grassTransforms.Count} grass + {pebbleTransforms.Count} pebble instances (tier={tier})");
    }

    public override void _Process(double delta)
    {
        // LOD fade: hide/show MultiMesh instances based on camera distance
        if (_grassMultiMesh == null && _pebbleMultiMesh == null)
            return;

        _camera ??= GetViewport()?.GetCamera3D();
        if (_camera == null)
            return;

        Vector3 camPos = _camera.GlobalPosition;

        UpdateLodVisibility(_grassMultiMesh, camPos);
        UpdateLodVisibility(_pebbleMultiMesh, camPos);
    }

    // ── LOD Visibility ─────────────────────────────────────────────────────

    private static void UpdateLodVisibility(MultiMeshInstance3D? instance, Vector3 camPos)
    {
        if (instance == null)
            return;

        // Use the instance's global position (center of the multimesh) for distance check.
        // For large maps, individual instance culling would be per-chunk, but here we
        // use the AABB center as a simple approximation.
        float distance = camPos.DistanceTo(instance.GlobalPosition);

        // Show if within LOD distance, hide if beyond
        instance.Visible = distance <= LodFadeDistance;
    }

    // ── Poisson Disk Sampling ──────────────────────────────────────────────

    /// <summary>
    /// Grid-accelerated Poisson disk sampling. Divides the map into cells of
    /// size minDist and attempts to place one point per cell, rejecting points
    /// that are too close to existing points in neighboring cells.
    /// </summary>
    private static List<Vector2> PoissonDiskSample(int mapWidth, int mapHeight, float minDist, uint seed)
    {
        float cellSize = minDist / MathF.Sqrt(2f);
        int gridW = (int)MathF.Ceiling(mapWidth / cellSize);
        int gridH = (int)MathF.Ceiling(mapHeight / cellSize);

        // Grid stores index into points list (-1 = empty)
        int[] grid = new int[gridW * gridH];
        Array.Fill(grid, -1);

        var points = new List<Vector2>();
        uint rng = seed;

        // Iterate over each grid cell and attempt placement
        for (int gy = 0; gy < gridH; gy++)
        {
            for (int gx = 0; gx < gridW; gx++)
            {
                // Generate candidate point within this cell
                rng = XorShift32(rng);
                float fx = (rng & 0xFFFF) / 65535f;
                rng = XorShift32(rng);
                float fy = (rng & 0xFFFF) / 65535f;

                float px = (gx + fx) * cellSize;
                float py = (gy + fy) * cellSize;

                // Reject if outside map bounds
                if (px < 0f || px >= mapWidth || py < 0f || py >= mapHeight)
                    continue;

                // Check neighboring cells for minimum distance violation
                bool tooClose = false;
                for (int ny = Math.Max(0, gy - 2); ny <= Math.Min(gridH - 1, gy + 2) && !tooClose; ny++)
                {
                    for (int nx = Math.Max(0, gx - 2); nx <= Math.Min(gridW - 1, gx + 2) && !tooClose; nx++)
                    {
                        int neighborIdx = ny * gridW + nx;
                        if (grid[neighborIdx] < 0)
                            continue;

                        Vector2 neighbor = points[grid[neighborIdx]];
                        float dx = px - neighbor.X;
                        float dy = py - neighbor.Y;
                        if (dx * dx + dy * dy < minDist * minDist)
                            tooClose = true;
                    }
                }

                if (tooClose)
                    continue;

                // Accept point
                int pointIdx = points.Count;
                points.Add(new Vector2(px, py));
                grid[gy * gridW + gx] = pointIdx;
            }
        }

        return points;
    }

    // ── Exclusion Maps ─────────────────────────────────────────────────────

    private static bool[] BuildWaterMap(MapData mapData, int width, int height)
    {
        bool[] map = new bool[width * height];

        if (mapData.TerrainFeatures == null)
            return map;

        for (int i = 0; i < mapData.TerrainFeatures.Length; i++)
        {
            TerrainFeature feature = mapData.TerrainFeatures[i];
            if (feature.Points == null || feature.Points.Length < 2)
                continue;

            // Rivers, oases, and sea edges are water features
            if (feature.Type is "river" or "oasis" or "sea_edge")
            {
                PaintFeatureArea(map, width, height, feature);
            }
        }

        return map;
    }

    private static bool[] BuildPathMap(MapData mapData, int width, int height)
    {
        bool[] map = new bool[width * height];

        if (mapData.TerrainFeatures == null)
            return map;

        for (int i = 0; i < mapData.TerrainFeatures.Length; i++)
        {
            TerrainFeature feature = mapData.TerrainFeatures[i];
            if (feature.Points == null || feature.Points.Length < 2)
                continue;

            if (feature.Type == "path")
            {
                PaintFeatureArea(map, width, height, feature);
            }
        }

        return map;
    }

    /// <summary>
    /// Marks cells along a terrain feature's polyline segments with a brush radius.
    /// </summary>
    private static void PaintFeatureArea(bool[] map, int width, int height, TerrainFeature feature)
    {
        const int brushRadius = 4;

        for (int seg = 0; seg < feature.Points.Length - 1; seg++)
        {
            int[] p0 = feature.Points[seg];
            int[] p1 = feature.Points[seg + 1];
            if (p0 == null || p0.Length < 2 || p1 == null || p1.Length < 2)
                continue;

            float dx = p1[0] - p0[0];
            float dy = p1[1] - p0[1];
            float segLength = MathF.Sqrt(dx * dx + dy * dy);
            if (segLength < 0.01f) continue;

            int steps = (int)MathF.Ceiling(segLength);
            for (int s = 0; s <= steps; s++)
            {
                float t = (float)s / steps;
                float cx = p0[0] + dx * t;
                float cy = p0[1] + dy * t;

                int minX = Math.Max(0, (int)(cx - brushRadius));
                int maxX = Math.Min(width - 1, (int)(cx + brushRadius));
                int minY = Math.Max(0, (int)(cy - brushRadius));
                int maxY = Math.Min(height - 1, (int)(cy + brushRadius));

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        float rdx = x - cx;
                        float rdy = y - cy;
                        if (rdx * rdx + rdy * rdy <= brushRadius * brushRadius)
                            map[y * width + x] = true;
                    }
                }
            }
        }
    }

    // ── Slope Detection ────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the terrain slope at the given position exceeds 45 degrees.
    /// Uses central difference on the elevation grid.
    /// </summary>
    private static bool IsSteepSlope(float[] elevation, int width, int height, float fx, float fy)
    {
        int x = (int)MathF.Floor(fx);
        int y = (int)MathF.Floor(fy);

        x = Math.Clamp(x, 1, width - 2);
        y = Math.Clamp(y, 1, height - 2);

        float left = elevation[y * width + (x - 1)];
        float right = elevation[y * width + (x + 1)];
        float up = elevation[(y - 1) * width + x];
        float down = elevation[(y + 1) * width + x];

        // Central difference gradient (over 2 cells)
        float dzdx = (right - left) * 0.5f;
        float dzdy = (down - up) * 0.5f;

        // Slope angle: atan(gradient magnitude)
        float gradientMag = MathF.Sqrt(dzdx * dzdx + dzdy * dzdy);
        float slopeAngle = MathF.Atan(gradientMag);

        return slopeAngle > MaxSlopeAngle;
    }

    // ── Elevation Sampling ─────────────────────────────────────────────────

    /// <summary>
    /// Bilinear interpolation of elevation at a fractional grid position.
    /// </summary>
    private static float SampleElevationBilinear(float[] elevation, int width, int height, float fx, float fy)
    {
        int x0 = (int)MathF.Floor(fx);
        int y0 = (int)MathF.Floor(fy);
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        x0 = Math.Clamp(x0, 0, width - 1);
        y0 = Math.Clamp(y0, 0, height - 1);
        x1 = Math.Clamp(x1, 0, width - 1);
        y1 = Math.Clamp(y1, 0, height - 1);

        float tx = fx - MathF.Floor(fx);
        float ty = fy - MathF.Floor(fy);

        float e00 = elevation[y0 * width + x0];
        float e10 = elevation[y0 * width + x1];
        float e01 = elevation[y1 * width + x0];
        float e11 = elevation[y1 * width + x1];

        float top = e00 + (e10 - e00) * tx;
        float bottom = e01 + (e11 - e01) * tx;
        return top + (bottom - top) * ty;
    }

    // ── Procedural Mesh Generation ─────────────────────────────────────────

    /// <summary>
    /// Creates a simple procedural grass blade mesh: a thin triangle (3 vertices).
    /// The blade points upward along the Y axis.
    /// </summary>
    private static Mesh CreateGrassBladeMesh()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        // Grass blade: thin triangle, ~0.4 units tall, ~0.05 units wide
        float halfWidth = 0.025f;
        float bladeHeight = 0.4f;

        // Green color for grass
        var grassColor = new Color(0.2f, 0.5f, 0.15f);

        // Front face
        st.SetColor(grassColor);
        st.SetNormal(new Vector3(0f, 0f, 1f));
        st.AddVertex(new Vector3(-halfWidth, 0f, 0f));

        st.SetColor(grassColor);
        st.SetNormal(new Vector3(0f, 0f, 1f));
        st.AddVertex(new Vector3(halfWidth, 0f, 0f));

        st.SetColor(grassColor.Lightened(0.2f));
        st.SetNormal(new Vector3(0f, 0f, 1f));
        st.AddVertex(new Vector3(0f, bladeHeight, 0f));

        // Back face (reversed winding for double-sided)
        st.SetColor(grassColor);
        st.SetNormal(new Vector3(0f, 0f, -1f));
        st.AddVertex(new Vector3(halfWidth, 0f, 0f));

        st.SetColor(grassColor);
        st.SetNormal(new Vector3(0f, 0f, -1f));
        st.AddVertex(new Vector3(-halfWidth, 0f, 0f));

        st.SetColor(grassColor.Lightened(0.2f));
        st.SetNormal(new Vector3(0f, 0f, -1f));
        st.AddVertex(new Vector3(0f, bladeHeight, 0f));

        return st.Commit();
    }

    /// <summary>
    /// Creates a simple procedural pebble mesh: a low-poly sphere (icosphere with 1 subdivision).
    /// Scaled small (~0.08 units radius).
    /// </summary>
    private static Mesh CreatePebbleMesh()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        var pebbleColor = new Color(0.45f, 0.42f, 0.38f);
        float radius = 0.08f;

        // Generate an octahedron (6 vertices, 8 triangles) as a low-poly sphere
        Vector3[] verts =
        [
            new(0f, radius, 0f),      // top
            new(radius, 0f, 0f),      // +X
            new(0f, 0f, radius),      // +Z
            new(-radius, 0f, 0f),     // -X
            new(0f, 0f, -radius),     // -Z
            new(0f, -radius, 0f)      // bottom
        ];

        // 8 triangular faces of the octahedron
        int[][] faces =
        [
            [0, 1, 2], [0, 2, 3], [0, 3, 4], [0, 4, 1],
            [5, 2, 1], [5, 3, 2], [5, 4, 3], [5, 1, 4]
        ];

        for (int i = 0; i < faces.Length; i++)
        {
            Vector3 v0 = verts[faces[i][0]];
            Vector3 v1 = verts[faces[i][1]];
            Vector3 v2 = verts[faces[i][2]];

            // Compute face normal
            Vector3 normal = (v1 - v0).Cross(v2 - v0).Normalized();

            st.SetColor(pebbleColor);
            st.SetNormal(normal);
            st.AddVertex(v0);

            st.SetColor(pebbleColor);
            st.SetNormal(normal);
            st.AddVertex(v1);

            st.SetColor(pebbleColor);
            st.SetNormal(normal);
            st.AddVertex(v2);
        }

        return st.Commit();
    }

    // ── MultiMesh Creation ─────────────────────────────────────────────────

    /// <summary>
    /// Creates a MultiMeshInstance3D with the given mesh and transforms.
    /// </summary>
    private static MultiMeshInstance3D CreateMultiMeshInstance(
        Mesh mesh, List<Transform3D> transforms, Node3D parent)
    {
        var multiMesh = new MultiMesh();
        multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        multiMesh.Mesh = mesh;
        multiMesh.InstanceCount = transforms.Count;

        for (int i = 0; i < transforms.Count; i++)
        {
            multiMesh.SetInstanceTransform(i, transforms[i]);
        }

        var instance = new MultiMeshInstance3D();
        instance.Multimesh = multiMesh;
        instance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

        parent.AddChild(instance);
        return instance;
    }

    // ── Hashing / RNG Utilities ────────────────────────────────────────────

    /// <summary>
    /// Deterministic hash of a 2D position for seeded random rotation/scale.
    /// </summary>
    private static uint HashPosition(float x, float y)
    {
        // Combine integer and fractional parts for a good hash
        uint ix = (uint)BitConverter.SingleToInt32Bits(x);
        uint iy = (uint)BitConverter.SingleToInt32Bits(y);

        // Simple mixing function
        uint h = ix ^ (iy * 2654435761u);
        h ^= h >> 16;
        h *= 0x85ebca6bu;
        h ^= h >> 13;
        h *= 0xc2b2ae35u;
        h ^= h >> 16;
        return h;
    }

    /// <summary>
    /// XorShift32 PRNG — fast, deterministic, good distribution.
    /// </summary>
    private static uint XorShift32(uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }
}
