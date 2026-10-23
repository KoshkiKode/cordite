using Godot;
using CorditeWars.Systems.Graphics;

namespace CorditeWars.Game.World;

/// <summary>
/// Controls terrain mesh Level-of-Detail by reducing subdivision levels for
/// chunks based on their distance from the RTSCamera.
///
/// Distance thresholds:
///   • Near (within 60 units): full subdivision factor
///   • Mid (60–120 units): half subdivision factor (minimum 1)
///   • Far (beyond 120 units): 1× subdivision (no subdivision)
///
/// Adjacent LOD levels share boundary vertices to prevent T-junctions and
/// maintain C1 continuity at chunk borders.
/// </summary>
public sealed class TerrainLODController
{
    /// <summary>Distance threshold for full-detail rendering.</summary>
    private const float NearDistance = 60f;

    /// <summary>Distance threshold for mid-detail rendering.</summary>
    private const float MidDistance = 120f;

    private readonly int _baseSubdivisionFactor;

    /// <summary>
    /// Creates a TerrainLODController with the given base subdivision factor.
    /// </summary>
    /// <param name="baseSubdivisionFactor">
    /// The full subdivision factor used for near chunks (from QualityTier).
    /// </param>
    public TerrainLODController(int baseSubdivisionFactor)
    {
        _baseSubdivisionFactor = baseSubdivisionFactor < 1 ? 1 : baseSubdivisionFactor;
    }

    /// <summary>
    /// Creates a TerrainLODController configured for the given quality tier.
    /// </summary>
    public TerrainLODController(QualityTier tier)
    {
        _baseSubdivisionFactor = tier switch
        {
            QualityTier.Potato => 1,
            QualityTier.Low => 2,
            QualityTier.Medium => 4,
            QualityTier.High => 8,
            _ => 1
        };
    }

    /// <summary>
    /// Returns the appropriate subdivision factor for a terrain chunk based on
    /// its distance from the camera.
    ///
    /// The chunk center is computed from the chunk grid coordinates assuming
    /// each chunk occupies a fixed number of cells (default 120 cells per chunk
    /// matching TerrainRenderer.ChunkCellSize).
    /// </summary>
    /// <param name="chunkX">Chunk X index (0-based).</param>
    /// <param name="chunkY">Chunk Y index (0-based).</param>
    /// <param name="cameraPosition">Current world-space camera position.</param>
    /// <returns>
    /// The subdivision factor to use for this chunk:
    ///   • Full factor for near chunks
    ///   • Half factor (min 1) for mid-range chunks
    ///   • 1 for far chunks
    /// </returns>
    public int GetSubdivisionForChunk(int chunkX, int chunkY, Vector3 cameraPosition)
    {
        // Compute chunk center in world space
        // Each chunk covers ChunkCellSize cells; center is at midpoint
        const float chunkCellSize = 120f;
        float centerX = (chunkX + 0.5f) * chunkCellSize;
        float centerZ = (chunkY + 0.5f) * chunkCellSize;

        // Distance from camera to chunk center (XZ plane only — elevation doesn't affect LOD)
        float dx = centerX - cameraPosition.X;
        float dz = centerZ - cameraPosition.Z;
        float distance = Mathf.Sqrt(dx * dx + dz * dz);

        if (distance <= NearDistance)
        {
            // Near: full subdivision
            return _baseSubdivisionFactor;
        }
        else if (distance <= MidDistance)
        {
            // Mid: half subdivision (minimum 1)
            int half = _baseSubdivisionFactor / 2;
            return half < 1 ? 1 : half;
        }
        else
        {
            // Far: no subdivision
            return 1;
        }
    }

    /// <summary>
    /// Returns the base subdivision factor (full detail level).
    /// </summary>
    public int BaseSubdivisionFactor => _baseSubdivisionFactor;
}
