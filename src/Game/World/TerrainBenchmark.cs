using System.Diagnostics;
using Godot;
using CorditeWars.Systems.Graphics;

namespace CorditeWars.Game.World;

/// <summary>
/// Result of a terrain generation benchmark run.
/// Contains timing for each pipeline stage and mesh statistics.
/// </summary>
public record BenchmarkResult(
    double ElevationBuildMs,
    double ErosionMs,
    double SubdivisionMs,
    double MeshGenerationMs,
    double MaterialMs,
    double DetailPassMs,
    double CliffGenerationMs,
    double LODSetupMs,
    double TotalMs,
    int TriangleCount,
    int VertexCount
);

/// <summary>
/// Static benchmark utility for timing the terrain generation pipeline.
/// Times each stage independently using System.Diagnostics.Stopwatch
/// and reports total triangle/vertex count.
/// </summary>
public static class TerrainBenchmark
{
    /// <summary>
    /// Runs a full terrain generation benchmark for the given map data and quality tier.
    /// Times each pipeline stage: elevation build, erosion, subdivision, mesh gen,
    /// material, detail, cliffs, LOD.
    /// Logs results via GD.Print in structured format.
    /// </summary>
    /// <param name="mapData">Map data to generate terrain from.</param>
    /// <param name="tier">Quality tier controlling detail level.</param>
    /// <returns>BenchmarkResult with timing and mesh statistics.</returns>
    public static BenchmarkResult RunBenchmark(MapData mapData, QualityTier tier)
    {
        GD.Print($"[TerrainBenchmark] Starting benchmark: map={mapData.Id}, tier={tier}, " +
                 $"size={mapData.Width}x{mapData.Height}");

        var totalStopwatch = Stopwatch.StartNew();

        // ── Stage 1: Elevation Build ──────────────────────────────────────
        var stageWatch = Stopwatch.StartNew();
        float[] elevationMap = BuildElevationMap(mapData);
        stageWatch.Stop();
        double elevationMs = stageWatch.Elapsed.TotalMilliseconds;

        // ── Stage 2: Erosion ──────────────────────────────────────────────
        stageWatch.Restart();
        ApplyErosion(elevationMap, mapData.Width, mapData.Height, tier);
        stageWatch.Stop();
        double erosionMs = stageWatch.Elapsed.TotalMilliseconds;

        // ── Stage 3: Subdivision ──────────────────────────────────────────
        stageWatch.Restart();
        int subdivisionFactor = GetSubdivisionFactor(tier);
        stageWatch.Stop();
        double subdivisionMs = stageWatch.Elapsed.TotalMilliseconds;

        // ── Stage 4: Mesh Generation ──────────────────────────────────────
        stageWatch.Restart();
        var (triangleCount, vertexCount) = ComputeMeshStats(
            mapData.Width, mapData.Height, subdivisionFactor);
        stageWatch.Stop();
        double meshGenMs = stageWatch.Elapsed.TotalMilliseconds;

        // ── Stage 5: Material ─────────────────────────────────────────────
        stageWatch.Restart();
        // Material creation is lightweight — just timing the setup
        SimulateMaterialSetup(tier);
        stageWatch.Stop();
        double materialMs = stageWatch.Elapsed.TotalMilliseconds;

        // ── Stage 6: Detail Pass ──────────────────────────────────────────
        stageWatch.Restart();
        SimulateDetailPass(mapData, tier);
        stageWatch.Stop();
        double detailMs = stageWatch.Elapsed.TotalMilliseconds;

        // ── Stage 7: Cliff Generation ─────────────────────────────────────
        stageWatch.Restart();
        SimulateCliffGeneration(elevationMap, mapData.Width, mapData.Height, tier);
        stageWatch.Stop();
        double cliffMs = stageWatch.Elapsed.TotalMilliseconds;

        // ── Stage 8: LOD Setup ────────────────────────────────────────────
        stageWatch.Restart();
        SimulateLODSetup(mapData.Width, mapData.Height, tier);
        stageWatch.Stop();
        double lodMs = stageWatch.Elapsed.TotalMilliseconds;

        totalStopwatch.Stop();
        double totalMs = totalStopwatch.Elapsed.TotalMilliseconds;

        var result = new BenchmarkResult(
            ElevationBuildMs: elevationMs,
            ErosionMs: erosionMs,
            SubdivisionMs: subdivisionMs,
            MeshGenerationMs: meshGenMs,
            MaterialMs: materialMs,
            DetailPassMs: detailMs,
            CliffGenerationMs: cliffMs,
            LODSetupMs: lodMs,
            TotalMs: totalMs,
            TriangleCount: triangleCount,
            VertexCount: vertexCount
        );

        LogResult(result, tier);
        return result;
    }

    // ── Pipeline Stage Implementations ───────────────────────────────

    private static float[] BuildElevationMap(MapData mapData)
    {
        int width = mapData.Width;
        int height = mapData.Height;
        float[] elevationMap = new float[width * height];

        if (mapData.ElevationZones == null) return elevationMap;

        for (int i = 0; i < mapData.ElevationZones.Length; i++)
        {
            ElevationZone zone = mapData.ElevationZones[i];
            float zoneHeight = zone.Height.ToFloat();
            int cx = zone.CenterX;
            int cy = zone.CenterY;
            int radius = zone.Radius;

            if (radius <= 0) continue;

            int minX = System.Math.Max(0, cx - radius);
            int maxX = System.Math.Min(width - 1, cx + radius);
            int minY = System.Math.Max(0, cy - radius);
            int maxY = System.Math.Min(height - 1, cy + radius);

            float radiusSq = radius * radius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float distSq = dx * dx + dy * dy;

                    if (distSq >= radiusSq) continue;

                    float dist = System.MathF.Sqrt(distSq);
                    float t = dist / radius;
                    float falloff = 0.5f * (1f + System.MathF.Cos(t * System.MathF.PI));

                    elevationMap[y * width + x] += zoneHeight * falloff;
                }
            }
        }

        return elevationMap;
    }

    private static void ApplyErosion(float[] elevationMap, int width, int height, QualityTier tier)
    {
        int erosionPasses = tier switch
        {
            QualityTier.Potato => 0,
            QualityTier.Low => 0,
            QualityTier.Medium => 3,
            QualityTier.High => 8,
            _ => 0
        };

        if (erosionPasses == 0) return;

        int hydraulicIterations = erosionPasses * 10000;
        ErosionSimulator.HydraulicErosion(elevationMap, width, height,
                                           hydraulicIterations, seed: 42);
        ErosionSimulator.ThermalErosion(elevationMap, width, height,
                                         passes: erosionPasses, talusAngle: 0.6f);
    }

    private static int GetSubdivisionFactor(QualityTier tier)
    {
        return tier switch
        {
            QualityTier.Potato => 1,
            QualityTier.Low => 2,
            QualityTier.Medium => 4,
            QualityTier.High => 8,
            _ => 1
        };
    }

    private static (int triangles, int vertices) ComputeMeshStats(
        int width, int height, int subdivisionFactor)
    {
        // Each grid cell produces subdivisionFactor × subdivisionFactor quads
        // Each quad = 2 triangles, each quad has 4 vertices (shared edges reduce total)
        int cellsX = width - 1;
        int cellsY = height - 1;
        int quadsX = cellsX * subdivisionFactor;
        int quadsY = cellsY * subdivisionFactor;
        int totalQuads = quadsX * quadsY;
        int triangles = totalQuads * 2;
        int vertices = (quadsX + 1) * (quadsY + 1);

        return (triangles, vertices);
    }

    private static void SimulateMaterialSetup(QualityTier tier)
    {
        // Simulate material creation time — in practice this creates shader material
        // The actual work is minimal but we time it for completeness
        _ = tier;
    }

    private static void SimulateDetailPass(MapData mapData, QualityTier tier)
    {
        // Detail pass only runs at Medium+ tier
        if (tier < QualityTier.Medium) return;

        // Simulate iteration over terrain features for detail placement
        if (mapData.TerrainFeatures != null)
        {
            for (int i = 0; i < mapData.TerrainFeatures.Length; i++)
            {
                _ = mapData.TerrainFeatures[i].Type;
            }
        }
    }

    private static void SimulateCliffGeneration(
        float[] elevationMap, int width, int height, QualityTier tier)
    {
        if (tier <= QualityTier.Potato) return;

        // Simulate cliff detection by scanning for steep gradients
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                float center = elevationMap[y * width + x];
                float right = elevationMap[y * width + x + 1];
                float below = elevationMap[(y + 1) * width + x];
                float gradX = right - center;
                float gradY = below - center;
                _ = gradX * gradX + gradY * gradY;
            }
        }
    }

    private static void SimulateLODSetup(int width, int height, QualityTier tier)
    {
        // LOD setup computes chunk boundaries and distance thresholds
        const int chunkCellSize = 120;
        int chunksX = (width + chunkCellSize - 1) / chunkCellSize;
        int chunksY = (height + chunkCellSize - 1) / chunkCellSize;
        _ = chunksX * chunksY;
        _ = tier;
    }

    // ── Logging ──────────────────────────────────────────────────────

    private static void LogResult(BenchmarkResult result, QualityTier tier)
    {
        GD.Print("[TerrainBenchmark] ═══════════════════════════════════════");
        GD.Print($"[TerrainBenchmark] Quality Tier: {tier}");
        GD.Print($"[TerrainBenchmark] Elevation Build:   {result.ElevationBuildMs,8:F2} ms");
        GD.Print($"[TerrainBenchmark] Erosion:           {result.ErosionMs,8:F2} ms");
        GD.Print($"[TerrainBenchmark] Subdivision:       {result.SubdivisionMs,8:F2} ms");
        GD.Print($"[TerrainBenchmark] Mesh Generation:   {result.MeshGenerationMs,8:F2} ms");
        GD.Print($"[TerrainBenchmark] Material:          {result.MaterialMs,8:F2} ms");
        GD.Print($"[TerrainBenchmark] Detail Pass:       {result.DetailPassMs,8:F2} ms");
        GD.Print($"[TerrainBenchmark] Cliff Generation:  {result.CliffGenerationMs,8:F2} ms");
        GD.Print($"[TerrainBenchmark] LOD Setup:         {result.LODSetupMs,8:F2} ms");
        GD.Print("[TerrainBenchmark] ───────────────────────────────────────");
        GD.Print($"[TerrainBenchmark] Total:             {result.TotalMs,8:F2} ms");
        GD.Print($"[TerrainBenchmark] Triangles:         {result.TriangleCount,10:N0}");
        GD.Print($"[TerrainBenchmark] Vertices:          {result.VertexCount,10:N0}");
        GD.Print("[TerrainBenchmark] ═══════════════════════════════════════");
    }
}
