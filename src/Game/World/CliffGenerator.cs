using System;
using System.Collections.Generic;
using Godot;
using CorditeWars.Systems.Graphics;

namespace CorditeWars.Game.World;

/// <summary>
/// Generates procedural cliff face geometry and boulder scatter at locations
/// where terrain slope exceeds the cliff threshold (55 degrees).
/// Produces layered rock strata, overhangs, and scattered boulders at cliff bases.
/// All generation is deterministic via position-seeded noise (no System.Random).
/// </summary>
public sealed class CliffGenerator
{
    /// <summary>Minimum slope (in degrees) to trigger cliff face generation.</summary>
    public const float CliffSlopeThreshold = 55f;

    // Slope threshold in radians for internal calculations
    private static readonly float SlopeThresholdRadians = CliffSlopeThreshold * MathF.PI / 180f;

    // Quality tier configuration
    private struct TierConfig
    {
        public int Subdivisions;
        public int StrataLayers;
        public bool HasOverhangs;
        public bool DetailedOverhangs;
        public int BoulderCount;
    }

    private static readonly TierConfig[] TierConfigs =
    {
        new() { Subdivisions = 2, StrataLayers = 0, HasOverhangs = false, DetailedOverhangs = false, BoulderCount = 0 },  // Potato
        new() { Subdivisions = 4, StrataLayers = 2, HasOverhangs = false, DetailedOverhangs = false, BoulderCount = 1 },  // Low
        new() { Subdivisions = 8, StrataLayers = 5, HasOverhangs = true, DetailedOverhangs = false, BoulderCount = 3 },   // Medium
        new() { Subdivisions = 16, StrataLayers = 8, HasOverhangs = true, DetailedOverhangs = true, BoulderCount = 5 },   // High
    };

    /// <summary>
    /// Analyzes the elevation map and generates cliff meshes where slopes
    /// are steep enough. Adds layered rock strata, overhangs, and
    /// scattered boulders at the base.
    /// </summary>
    public void Generate(float[] elevation, int width, int height,
                          string biome, QualityTier tier, Node3D parent)
    {
        if (elevation == null) throw new ArgumentNullException(nameof(elevation));
        if (width < 2 || height < 2) throw new ArgumentException("Grid must be at least 2x2.");
        if (elevation.Length != width * height)
            throw new ArgumentException("Elevation array length must equal width * height.");
        if (parent == null) throw new ArgumentNullException(nameof(parent));

        var config = TierConfigs[(int)tier];
        var biomeColors = GetBiomeColors(biome ?? "temperate");

        // Detect cliff edges and generate geometry
        var cliffEdges = DetectCliffEdges(elevation, width, height);

        if (cliffEdges.Count == 0)
            return;

        // Generate cliff face meshes
        var cliffMesh = GenerateCliffFaceMesh(cliffEdges, elevation, width, height, config, biomeColors);
        if (cliffMesh != null)
        {
            var meshInstance = new MeshInstance3D();
            meshInstance.Mesh = cliffMesh;
            meshInstance.Name = "CliffFaces";
            parent.AddChild(meshInstance);
        }

        // Generate boulders at cliff bases
        if (config.BoulderCount > 0)
        {
            GenerateBoulders(cliffEdges, elevation, width, height, config, biomeColors, parent);
        }

        GD.Print($"[CliffGenerator] Generated {cliffEdges.Count} cliff edges, tier={tier}, biome={biome}");
    }

    // ── Cliff Edge Detection ───────────────────────────────────────────────

    /// <summary>
    /// Represents a detected cliff edge between two adjacent cells.
    /// </summary>
    private struct CliffEdge
    {
        public int X;           // Lower cell X
        public int Y;           // Lower cell Y
        public int NeighborX;   // Upper cell X
        public int NeighborY;   // Upper cell Y
        public float LowElev;   // Lower elevation
        public float HighElev;  // Upper elevation
    }

    /// <summary>
    /// Scans the elevation map for adjacent cells where slope exceeds the threshold.
    /// </summary>
    private static List<CliffEdge> DetectCliffEdges(float[] elevation, int width, int height)
    {
        var edges = new List<CliffEdge>();

        // Check horizontal neighbors
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                float e0 = elevation[y * width + x];
                float e1 = elevation[y * width + (x + 1)];
                float heightDiff = MathF.Abs(e1 - e0);

                // Slope angle: atan(heightDiff / horizontalDist) where horizontal dist = 1.0
                float slopeAngle = MathF.Atan(heightDiff);

                if (slopeAngle > SlopeThresholdRadians)
                {
                    if (e0 < e1)
                        edges.Add(new CliffEdge { X = x, Y = y, NeighborX = x + 1, NeighborY = y, LowElev = e0, HighElev = e1 });
                    else
                        edges.Add(new CliffEdge { X = x + 1, Y = y, NeighborX = x, NeighborY = y, LowElev = e1, HighElev = e0 });
                }
            }
        }

        // Check vertical neighbors
        for (int y = 0; y < height - 1; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float e0 = elevation[y * width + x];
                float e1 = elevation[(y + 1) * width + x];
                float heightDiff = MathF.Abs(e1 - e0);

                float slopeAngle = MathF.Atan(heightDiff);

                if (slopeAngle > SlopeThresholdRadians)
                {
                    if (e0 < e1)
                        edges.Add(new CliffEdge { X = x, Y = y, NeighborX = x, NeighborY = y + 1, LowElev = e0, HighElev = e1 });
                    else
                        edges.Add(new CliffEdge { X = x, Y = y + 1, NeighborX = x, NeighborY = y, LowElev = e1, HighElev = e0 });
                }
            }
        }

        return edges;
    }

    // ── Cliff Face Mesh Generation ─────────────────────────────────────────

    /// <summary>
    /// Generates the cliff face mesh as a vertical quad strip with strata layers
    /// and optional overhang geometry.
    /// </summary>
    private static Mesh? GenerateCliffFaceMesh(
        List<CliffEdge> edges, float[] elevation, int width, int height,
        TierConfig config, BiomeColors colors)
    {
        if (edges.Count == 0)
            return null;

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        for (int edgeIdx = 0; edgeIdx < edges.Count; edgeIdx++)
        {
            CliffEdge edge = edges[edgeIdx];
            GenerateCliffFaceForEdge(st, edge, config, colors);
        }

        st.GenerateNormals();
        return st.Commit();
    }

    /// <summary>
    /// Generates a vertical quad strip for a single cliff edge, with strata layers
    /// and optional overhangs.
    /// </summary>
    private static void GenerateCliffFaceForEdge(
        SurfaceTool st, CliffEdge edge, TierConfig config, BiomeColors colors)
    {
        float baseX = (edge.X + edge.NeighborX) * 0.5f;
        float baseZ = (edge.Y + edge.NeighborY) * 0.5f;
        float lowY = edge.LowElev;
        float highY = edge.HighElev;
        float cliffHeight = highY - lowY;

        if (cliffHeight < 0.01f)
            return;

        // Determine face direction (normal pointing away from the high side)
        float dirX = edge.X - edge.NeighborX;
        float dirZ = edge.Y - edge.NeighborY;
        float dirLen = MathF.Sqrt(dirX * dirX + dirZ * dirZ);
        if (dirLen > 0.001f)
        {
            dirX /= dirLen;
            dirZ /= dirLen;
        }

        // Perpendicular direction for face width
        float perpX = -dirZ;
        float perpZ = dirX;

        float faceHalfWidth = 0.5f;
        int subdivisions = config.Subdivisions;
        int strataLayers = config.StrataLayers;

        // Generate vertical quad strip subdivided vertically
        for (int row = 0; row < subdivisions; row++)
        {
            float t0 = (float)row / subdivisions;
            float t1 = (float)(row + 1) / subdivisions;
            float y0 = lowY + cliffHeight * t0;
            float y1 = lowY + cliffHeight * t1;

            // Determine strata layer for vertex color encoding
            int strataIndex = strataLayers > 0 ? (row * strataLayers / subdivisions) : 0;
            float strataT = strataLayers > 0 ? (float)strataIndex / strataLayers : 0f;

            // Get strata color
            Color layerColor = LerpColor(colors.BaseColor, colors.StrataColor, strataT * 0.5f + 0.25f);

            // Add slight color variation per layer using position-seeded noise
            uint layerHash = HashPosition2D(baseX + row * 7.13f, baseZ + row * 3.71f);
            float colorVariation = ((layerHash & 0xFFFF) / 65535f - 0.5f) * 0.1f;
            layerColor = new Color(
                Math.Clamp(layerColor.R + colorVariation, 0f, 1f),
                Math.Clamp(layerColor.G + colorVariation * 0.8f, 0f, 1f),
                Math.Clamp(layerColor.B + colorVariation * 0.6f, 0f, 1f));

            // Compute overhang offset for upper portions
            float overhangOffset = 0f;
            if (config.HasOverhangs && t0 > 0.6f)
            {
                uint overhangHash = HashPosition2D(baseX * 17.3f, baseZ * 23.7f);
                // Only generate overhangs at seeded intervals
                if ((overhangHash & 0x7) < 3) // ~37.5% chance per cliff edge
                {
                    float overhangStrength = config.DetailedOverhangs ? 0.8f : 0.5f;
                    float overhangT = (t0 - 0.6f) / 0.4f; // 0..1 in the upper 40%
                    // Seed the extrusion amount by position
                    float extrudeAmount = 0.3f + ((overhangHash >> 8) & 0xFFFF) / 65535f * overhangStrength;
                    overhangOffset = extrudeAmount * overhangT * overhangT;
                }
            }

            float overhangOffset1 = 0f;
            if (config.HasOverhangs && t1 > 0.6f)
            {
                uint overhangHash = HashPosition2D(baseX * 17.3f, baseZ * 23.7f);
                if ((overhangHash & 0x7) < 3)
                {
                    float overhangStrength = config.DetailedOverhangs ? 0.8f : 0.5f;
                    float overhangT = (t1 - 0.6f) / 0.4f;
                    float extrudeAmount = 0.3f + ((overhangHash >> 8) & 0xFFFF) / 65535f * overhangStrength;
                    overhangOffset1 = extrudeAmount * overhangT * overhangT;
                }
            }

            // Four corners of this quad strip segment
            Vector3 bl = new(baseX + perpX * (-faceHalfWidth) + dirX * overhangOffset,
                             y0,
                             baseZ + perpZ * (-faceHalfWidth) + dirZ * overhangOffset);
            Vector3 br = new(baseX + perpX * faceHalfWidth + dirX * overhangOffset,
                             y0,
                             baseZ + perpZ * faceHalfWidth + dirZ * overhangOffset);
            Vector3 tl = new(baseX + perpX * (-faceHalfWidth) + dirX * overhangOffset1,
                             y1,
                             baseZ + perpZ * (-faceHalfWidth) + dirZ * overhangOffset1);
            Vector3 tr = new(baseX + perpX * faceHalfWidth + dirX * overhangOffset1,
                             y1,
                             baseZ + perpZ * faceHalfWidth + dirZ * overhangOffset1);

            // Add strata ledge geometry (slight inset at layer boundaries)
            if (strataLayers > 0 && row > 0 && (row % Math.Max(1, subdivisions / strataLayers)) == 0)
            {
                float ledgeDepth = 0.05f;
                bl += new Vector3(dirX * ledgeDepth, 0f, dirZ * ledgeDepth);
                br += new Vector3(dirX * ledgeDepth, 0f, dirZ * ledgeDepth);
            }

            // Triangle 1: bl, tl, tr
            st.SetColor(layerColor);
            st.AddVertex(bl);
            st.SetColor(layerColor);
            st.AddVertex(tl);
            st.SetColor(layerColor);
            st.AddVertex(tr);

            // Triangle 2: bl, tr, br
            st.SetColor(layerColor);
            st.AddVertex(bl);
            st.SetColor(layerColor);
            st.AddVertex(tr);
            st.SetColor(layerColor);
            st.AddVertex(br);
        }
    }

    // ── Boulder Generation ─────────────────────────────────────────────────

    /// <summary>
    /// Scatters procedural boulder meshes at the base of cliff faces using MultiMeshInstance3D.
    /// </summary>
    private static void GenerateBoulders(
        List<CliffEdge> edges, float[] elevation, int width, int height,
        TierConfig config, BiomeColors colors, Node3D parent)
    {
        var boulderTransforms = new List<Transform3D>();
        var processedPositions = new HashSet<long>();

        for (int i = 0; i < edges.Count; i++)
        {
            CliffEdge edge = edges[i];

            // Use position hash to determine boulder count for this edge (2-5 range)
            uint edgeHash = HashPosition2D(edge.X * 13.7f, edge.Y * 19.3f);
            int boulderCount = 2 + (int)(edgeHash % 4); // 2..5
            boulderCount = Math.Min(boulderCount, config.BoulderCount);

            // Avoid duplicate boulders at the same cell
            long posKey = (long)edge.X * 65536 + edge.Y;
            if (!processedPositions.Add(posKey))
                continue;

            float baseX = edge.X;
            float baseZ = edge.Y;
            float baseY = edge.LowElev;

            // Direction away from cliff face
            float awayX = edge.X - edge.NeighborX;
            float awayZ = edge.Y - edge.NeighborY;
            float awayLen = MathF.Sqrt(awayX * awayX + awayZ * awayZ);
            if (awayLen > 0.001f)
            {
                awayX /= awayLen;
                awayZ /= awayLen;
            }

            for (int b = 0; b < boulderCount; b++)
            {
                // Position-seeded placement
                uint bHash = HashPosition2D(baseX + b * 5.17f, baseZ + b * 7.31f);

                float offsetX = ((bHash & 0xFFFF) / 65535f - 0.5f) * 1.5f + awayX * 0.5f;
                float offsetZ = (((bHash >> 16) & 0xFFFF) / 65535f - 0.5f) * 1.5f + awayZ * 0.5f;

                float boulderX = baseX + offsetX;
                float boulderZ = baseZ + offsetZ;

                // Clamp to grid bounds for elevation sampling
                int sampleX = Math.Clamp((int)boulderX, 0, width - 1);
                int sampleZ = Math.Clamp((int)boulderZ, 0, height - 1);
                float boulderY = elevation[sampleZ * width + sampleX];

                // Random scale and rotation
                uint scaleHash = HashPosition2D(boulderX * 31.1f, boulderZ * 37.9f);
                float scale = 0.15f + ((scaleHash & 0xFFFF) / 65535f) * 0.35f;
                float rotY = ((scaleHash >> 16) & 0xFFFF) / 65535f * MathF.PI * 2f;

                var basis = Basis.Identity
                    .Rotated(Vector3.Up, rotY)
                    .Scaled(new Vector3(scale, scale * 0.8f, scale));
                var transform = new Transform3D(basis, new Vector3(boulderX, boulderY, boulderZ));
                boulderTransforms.Add(transform);
            }
        }

        if (boulderTransforms.Count == 0)
            return;

        // Create boulder mesh (procedural icosphere with noise displacement)
        Mesh boulderMesh = CreateBoulderMesh(config, colors);

        // Create MultiMeshInstance3D
        var multiMesh = new MultiMesh();
        multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        multiMesh.UseColors = true;
        multiMesh.Mesh = boulderMesh;
        multiMesh.InstanceCount = boulderTransforms.Count;

        for (int i = 0; i < boulderTransforms.Count; i++)
        {
            multiMesh.SetInstanceTransform(i, boulderTransforms[i]);
            // Slight color variation per boulder
            uint colorHash = HashPosition2D(i * 11.3f, i * 17.7f);
            float variation = ((colorHash & 0xFFFF) / 65535f - 0.5f) * 0.15f;
            Color boulderColor = new(
                Math.Clamp(colors.BaseColor.R + variation, 0f, 1f),
                Math.Clamp(colors.BaseColor.G + variation * 0.8f, 0f, 1f),
                Math.Clamp(colors.BaseColor.B + variation * 0.6f, 0f, 1f));
            multiMesh.SetInstanceColor(i, boulderColor);
        }

        var instance = new MultiMeshInstance3D();
        instance.Multimesh = multiMesh;
        instance.Name = "CliffBoulders";
        instance.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
        parent.AddChild(instance);

        GD.Print($"[CliffGenerator] Placed {boulderTransforms.Count} boulders at cliff bases");
    }

    /// <summary>
    /// Creates a procedural icosphere mesh with noise displacement for irregular boulder shape.
    /// Subdivision level scales with quality tier.
    /// </summary>
    private static Mesh CreateBoulderMesh(TierConfig config, BiomeColors colors)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        // Start with icosahedron vertices
        float phi = (1f + MathF.Sqrt(5f)) / 2f;
        float invLen = 1f / MathF.Sqrt(1f + phi * phi);

        Vector3[] baseVerts =
        {
            new Vector3(-1, phi, 0) * invLen,
            new Vector3(1, phi, 0) * invLen,
            new Vector3(-1, -phi, 0) * invLen,
            new Vector3(1, -phi, 0) * invLen,
            new Vector3(0, -1, phi) * invLen,
            new Vector3(0, 1, phi) * invLen,
            new Vector3(0, -1, -phi) * invLen,
            new Vector3(0, 1, -phi) * invLen,
            new Vector3(phi, 0, -1) * invLen,
            new Vector3(phi, 0, 1) * invLen,
            new Vector3(-phi, 0, -1) * invLen,
            new Vector3(-phi, 0, 1) * invLen,
        };

        int[][] baseFaces =
        {
            new[] {0, 11, 5}, new[] {0, 5, 1}, new[] {0, 1, 7}, new[] {0, 7, 10}, new[] {0, 10, 11},
            new[] {1, 5, 9}, new[] {5, 11, 4}, new[] {11, 10, 2}, new[] {10, 7, 6}, new[] {7, 1, 8},
            new[] {3, 9, 4}, new[] {3, 4, 2}, new[] {3, 2, 6}, new[] {3, 6, 8}, new[] {3, 8, 9},
            new[] {4, 9, 5}, new[] {2, 4, 11}, new[] {6, 2, 10}, new[] {8, 6, 7}, new[] {9, 8, 1},
        };

        // Subdivide once for Low, twice for Medium/High
        int subdivLevel = config.BoulderCount <= 1 ? 0 : (config.BoulderCount <= 3 ? 1 : 2);

        var triangles = new List<(Vector3 a, Vector3 b, Vector3 c)>();
        for (int i = 0; i < baseFaces.Length; i++)
        {
            triangles.Add((baseVerts[baseFaces[i][0]], baseVerts[baseFaces[i][1]], baseVerts[baseFaces[i][2]]));
        }

        // Subdivide
        for (int s = 0; s < subdivLevel; s++)
        {
            var newTriangles = new List<(Vector3, Vector3, Vector3)>();
            for (int i = 0; i < triangles.Count; i++)
            {
                var (a, b, c) = triangles[i];
                Vector3 ab = ((a + b) * 0.5f).Normalized();
                Vector3 bc = ((b + c) * 0.5f).Normalized();
                Vector3 ca = ((c + a) * 0.5f).Normalized();

                newTriangles.Add((a, ab, ca));
                newTriangles.Add((b, bc, ab));
                newTriangles.Add((c, ca, bc));
                newTriangles.Add((ab, bc, ca));
            }
            triangles = newTriangles;
        }

        // Apply noise displacement and emit triangles
        float radius = 1.0f;
        for (int i = 0; i < triangles.Count; i++)
        {
            var (a, b, c) = triangles[i];

            // Displace each vertex along its normal using position-seeded noise
            Vector3 va = DisplaceVertex(a, radius);
            Vector3 vb = DisplaceVertex(b, radius);
            Vector3 vc = DisplaceVertex(c, radius);

            Vector3 normal = (vb - va).Cross(vc - va).Normalized();

            st.SetColor(colors.BaseColor);
            st.SetNormal(normal);
            st.AddVertex(va);

            st.SetColor(colors.BaseColor);
            st.SetNormal(normal);
            st.AddVertex(vb);

            st.SetColor(colors.BaseColor);
            st.SetNormal(normal);
            st.AddVertex(vc);
        }

        return st.Commit();
    }

    /// <summary>
    /// Displaces a normalized vertex along its direction using position-seeded noise.
    /// Creates irregular boulder shapes.
    /// </summary>
    private static Vector3 DisplaceVertex(Vector3 normalizedPos, float baseRadius)
    {
        // Use position-seeded noise for displacement
        uint hash = HashPosition3D(normalizedPos.X, normalizedPos.Y, normalizedPos.Z);
        float noise = ((hash & 0xFFFF) / 65535f - 0.5f) * 0.3f; // +/- 15% displacement
        float displacement = baseRadius + noise * baseRadius;
        return normalizedPos * displacement;
    }

    // ── Biome Color System ─────────────────────────────────────────────────

    private struct BiomeColors
    {
        public Color BaseColor;
        public Color StrataColor;
        public Color AccentColor;
    }

    /// <summary>
    /// Returns rock colors adapted to the map biome.
    /// </summary>
    private static BiomeColors GetBiomeColors(string biome)
    {
        return biome.ToLowerInvariant() switch
        {
            "temperate" => new BiomeColors
            {
                BaseColor = new Color(0.55f, 0.50f, 0.42f),   // grey-brown limestone
                StrataColor = new Color(0.45f, 0.42f, 0.35f),
                AccentColor = new Color(0.3f, 0.45f, 0.2f),   // moss green
            },
            "desert" or "volcanic" => new BiomeColors
            {
                BaseColor = new Color(0.72f, 0.42f, 0.22f),   // red/orange sandstone
                StrataColor = new Color(0.65f, 0.35f, 0.18f),
                AccentColor = new Color(0.8f, 0.55f, 0.3f),   // wind-carved lighter bands
            },
            "rocky" or "mountain" => new BiomeColors
            {
                BaseColor = new Color(0.3f, 0.3f, 0.32f),     // dark granite
                StrataColor = new Color(0.25f, 0.25f, 0.27f),
                AccentColor = new Color(0.85f, 0.82f, 0.78f), // quartz veins
            },
            "coastal" or "archipelago" => new BiomeColors
            {
                BaseColor = new Color(0.9f, 0.88f, 0.85f),    // chalk-white
                StrataColor = new Color(0.82f, 0.80f, 0.77f),
                AccentColor = new Color(0.2f, 0.2f, 0.22f),   // flint bands
            },
            "tropical" => new BiomeColors
            {
                BaseColor = new Color(0.22f, 0.22f, 0.25f),   // dark basalt
                StrataColor = new Color(0.18f, 0.18f, 0.2f),
                AccentColor = new Color(0.2f, 0.4f, 0.15f),   // vine overgrowth hints
            },
            _ => new BiomeColors
            {
                BaseColor = new Color(0.5f, 0.48f, 0.44f),    // generic grey rock
                StrataColor = new Color(0.42f, 0.40f, 0.37f),
                AccentColor = new Color(0.55f, 0.52f, 0.48f),
            },
        };
    }

    // ── Utility Functions ──────────────────────────────────────────────────

    /// <summary>
    /// Deterministic hash of a 2D position for seeded random values.
    /// </summary>
    private static uint HashPosition2D(float x, float y)
    {
        uint ix = (uint)BitConverter.SingleToInt32Bits(x);
        uint iy = (uint)BitConverter.SingleToInt32Bits(y);

        uint h = ix ^ (iy * 2654435761u);
        h ^= h >> 16;
        h *= 0x85ebca6bu;
        h ^= h >> 13;
        h *= 0xc2b2ae35u;
        h ^= h >> 16;
        return h;
    }

    /// <summary>
    /// Deterministic hash of a 3D position for seeded random values.
    /// </summary>
    private static uint HashPosition3D(float x, float y, float z)
    {
        uint ix = (uint)BitConverter.SingleToInt32Bits(x);
        uint iy = (uint)BitConverter.SingleToInt32Bits(y);
        uint iz = (uint)BitConverter.SingleToInt32Bits(z);

        uint h = ix ^ (iy * 2654435761u) ^ (iz * 2246822519u);
        h ^= h >> 16;
        h *= 0x85ebca6bu;
        h ^= h >> 13;
        h *= 0xc2b2ae35u;
        h ^= h >> 16;
        return h;
    }

    /// <summary>
    /// Linearly interpolates between two colors.
    /// </summary>
    private static Color LerpColor(Color a, Color b, float t)
    {
        return new Color(
            a.R + (b.R - a.R) * t,
            a.G + (b.G - a.G) * t,
            a.B + (b.B - a.B) * t,
            a.A + (b.A - a.A) * t);
    }
}
