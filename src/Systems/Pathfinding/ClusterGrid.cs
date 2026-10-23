using System;
using System.Collections.Generic;
using CorditeWars.Core;

namespace CorditeWars.Systems.Pathfinding;

// ─────────────────────────────────────────────────────────────────────────────
// Entrance Node
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Represents a border crossing point between two adjacent clusters.
/// Placed at traversable cells along the shared border where both sides
/// are passable for the given movement profile.
/// </summary>
public struct EntranceNode
{
    /// <summary>Grid X coordinate of this entrance.</summary>
    public int X;

    /// <summary>Grid Y coordinate of this entrance.</summary>
    public int Y;

    /// <summary>Index of the cluster on side A (clusterAy * ClustersX + clusterAx).</summary>
    public int ClusterA;

    /// <summary>Index of the cluster on side B (clusterBy * ClustersX + clusterBx).</summary>
    public int ClusterB;

    /// <summary>Unique node index in the AbstractGraph.</summary>
    public int NodeIndex;
}

// ─────────────────────────────────────────────────────────────────────────────
// Cluster Grid
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Partitions the TerrainGrid into fixed-size clusters and identifies border
/// cells (entrances) between adjacent clusters.
///
/// <para><b>Design philosophy:</b> The cluster grid is pathfinding infrastructure
/// that enables hierarchical A* (HPA*). By dividing the map into 16×16 clusters,
/// we reduce the search space for long-distance paths from ~262k cells to ~1k
/// abstract nodes on a 512×512 map.</para>
///
/// <para><b>Entrance detection:</b> For each pair of adjacent clusters, we scan
/// the shared border for contiguous runs of mutually-traversable cells (both
/// sides must be passable). Short runs (≤3) get one entrance at the midpoint;
/// longer runs (>3) get two entrances at the endpoints. This balances graph
/// density against path quality.</para>
///
/// <para><b>Boundary handling:</b> Grids not evenly divisible by the cluster
/// size produce smaller boundary clusters. Border scans are clamped to actual
/// grid bounds to avoid out-of-range access.</para>
/// </summary>
public sealed class ClusterGrid
{
    // ── Public Properties ────────────────────────────────────────────────

    /// <summary>Number of cells per cluster side (default 16).</summary>
    public int ClusterSize { get; }

    /// <summary>Number of clusters along the X axis (ceiling division).</summary>
    public int ClustersX { get; }

    /// <summary>Number of clusters along the Y axis (ceiling division).</summary>
    public int ClustersY { get; }

    /// <summary>Width of the underlying terrain grid in cells.</summary>
    public int GridWidth { get; }

    /// <summary>Height of the underlying terrain grid in cells.</summary>
    public int GridHeight { get; }

    // ── Internal Storage ─────────────────────────────────────────────────

    /// <summary>
    /// All entrance nodes discovered during the last BuildEntrances call.
    /// Stored in a flat list; indexed by the per-border-pair lookup dictionary.
    /// </summary>
    private readonly List<EntranceNode> _allEntrances;

    /// <summary>
    /// Maps a border pair key to the range [startIndex, count) in _allEntrances.
    /// Key = GetBorderPairKey(clusterAx, clusterAy, clusterBx, clusterBy).
    /// </summary>
    private readonly Dictionary<long, (int startIndex, int count)> _borderEntranceMap;

    // ── Constructor ──────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new cluster grid partitioning for the given grid dimensions.
    /// Does not detect entrances — call <see cref="BuildEntrances"/> after
    /// construction to populate entrance nodes.
    /// </summary>
    /// <param name="gridWidth">Width of the terrain grid in cells.</param>
    /// <param name="gridHeight">Height of the terrain grid in cells.</param>
    /// <param name="clusterSize">Cells per cluster side. Default 16.</param>
    public ClusterGrid(int gridWidth, int gridHeight, int clusterSize = 16)
    {
        if (gridWidth < 1)
            throw new ArgumentException("Grid width must be at least 1.", nameof(gridWidth));
        if (gridHeight < 1)
            throw new ArgumentException("Grid height must be at least 1.", nameof(gridHeight));
        if (clusterSize < 1)
            throw new ArgumentException("Cluster size must be at least 1.", nameof(clusterSize));

        GridWidth = gridWidth;
        GridHeight = gridHeight;
        ClusterSize = clusterSize;

        // Ceiling division: (n + d - 1) / d
        ClustersX = (gridWidth + clusterSize - 1) / clusterSize;
        ClustersY = (gridHeight + clusterSize - 1) / clusterSize;

        _allEntrances = new List<EntranceNode>();
        _borderEntranceMap = new Dictionary<long, (int startIndex, int count)>();
    }

    // ── Public API ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns the cluster index (cx, cy) for a given grid cell coordinate.
    /// Uses integer division (floor) to map cell → cluster.
    /// </summary>
    public (int cx, int cy) GetClusterForCell(int x, int y)
    {
        return (x / ClusterSize, y / ClusterSize);
    }

    /// <summary>
    /// Returns all entrance nodes between two adjacent clusters.
    /// The clusters must be adjacent (Manhattan distance of cluster indices == 1).
    /// Returns an empty span if no entrances exist or the clusters are not adjacent.
    /// </summary>
    public ReadOnlySpan<EntranceNode> GetEntrances(int clusterAx, int clusterAy,
                                                    int clusterBx, int clusterBy)
    {
        long key = GetBorderPairKey(clusterAx, clusterAy, clusterBx, clusterBy);
        if (_borderEntranceMap.TryGetValue(key, out var range))
        {
            return System.Runtime.InteropServices.CollectionsMarshal
                .AsSpan(_allEntrances)
                .Slice(range.startIndex, range.count);
        }
        return ReadOnlySpan<EntranceNode>.Empty;
    }

    /// <summary>
    /// Returns all entrance nodes in the grid as a read-only span.
    /// Useful for building the abstract graph.
    /// </summary>
    public ReadOnlySpan<EntranceNode> GetAllEntrances()
    {
        return System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_allEntrances);
    }

    /// <summary>
    /// Total number of entrance nodes discovered.
    /// </summary>
    public int EntranceCount => _allEntrances.Count;

    /// <summary>
    /// Scans all adjacent cluster borders and identifies entrance nodes based
    /// on traversability. Must be called after construction and whenever
    /// terrain changes require re-evaluation.
    /// </summary>
    /// <param name="grid">The terrain grid to check traversability against.</param>
    /// <param name="profile">The movement profile determining what is traversable.</param>
    public void BuildEntrances(TerrainGrid grid, MovementProfile profile)
    {
        _allEntrances.Clear();
        _borderEntranceMap.Clear();

        // Scan horizontal borders (between vertically adjacent clusters).
        // A horizontal border is the row of cells at the bottom edge of cluster (cx, cy)
        // and the top edge of cluster (cx, cy+1).
        for (int cy = 0; cy < ClustersY - 1; cy++)
        {
            for (int cx = 0; cx < ClustersX; cx++)
            {
                ScanHorizontalBorder(grid, profile, cx, cy);
            }
        }

        // Scan vertical borders (between horizontally adjacent clusters).
        // A vertical border is the column of cells at the right edge of cluster (cx, cy)
        // and the left edge of cluster (cx+1, cy).
        for (int cy = 0; cy < ClustersY; cy++)
        {
            for (int cx = 0; cx < ClustersX - 1; cx++)
            {
                ScanVerticalBorder(grid, profile, cx, cy);
            }
        }
    }

    // ── Private: Border Scanning ─────────────────────────────────────────

    /// <summary>
    /// Scans the horizontal border between cluster (cx, cy) and cluster (cx, cy+1).
    /// The border row in cluster A is at y = (cy+1)*ClusterSize - 1 (bottom row of A).
    /// The border row in cluster B is at y = (cy+1)*ClusterSize (top row of B).
    /// </summary>
    private void ScanHorizontalBorder(TerrainGrid grid, MovementProfile profile,
                                       int cx, int cy)
    {
        int clusterAx = cx;
        int clusterAy = cy;
        int clusterBx = cx;
        int clusterBy = cy + 1;

        // The border lies between the last row of cluster A and the first row of cluster B.
        int borderYA = (cy + 1) * ClusterSize - 1; // last row of cluster A
        int borderYB = (cy + 1) * ClusterSize;     // first row of cluster B

        // Clamp to actual grid bounds (boundary clusters may be smaller).
        if (borderYA >= GridHeight || borderYB >= GridHeight)
            return;

        // X range for this cluster column, clamped to grid bounds.
        int xStart = cx * ClusterSize;
        int xEnd = Math.Min((cx + 1) * ClusterSize, GridWidth); // exclusive

        int startIndex = _allEntrances.Count;

        // Scan for contiguous runs of mutually-traversable border cells.
        int runStart = -1;
        for (int x = xStart; x < xEnd; x++)
        {
            bool traversable = TerrainCostCalculator.CanTraverse(grid, profile, x, borderYA)
                            && TerrainCostCalculator.CanTraverse(grid, profile, x, borderYB);

            if (traversable)
            {
                if (runStart == -1)
                    runStart = x;
            }
            else
            {
                if (runStart != -1)
                {
                    PlaceEntrancesForRun(runStart, x - 1, borderYA, borderYB,
                                         clusterAx, clusterAy, clusterBx, clusterBy,
                                         isHorizontalBorder: true);
                    runStart = -1;
                }
            }
        }

        // Close any open run at the end of the scan.
        if (runStart != -1)
        {
            PlaceEntrancesForRun(runStart, xEnd - 1, borderYA, borderYB,
                                 clusterAx, clusterAy, clusterBx, clusterBy,
                                 isHorizontalBorder: true);
        }

        int count = _allEntrances.Count - startIndex;
        if (count > 0)
        {
            long key = GetBorderPairKey(clusterAx, clusterAy, clusterBx, clusterBy);
            _borderEntranceMap[key] = (startIndex, count);
        }
    }

    /// <summary>
    /// Scans the vertical border between cluster (cx, cy) and cluster (cx+1, cy).
    /// The border column in cluster A is at x = (cx+1)*ClusterSize - 1 (right col of A).
    /// The border column in cluster B is at x = (cx+1)*ClusterSize (left col of B).
    /// </summary>
    private void ScanVerticalBorder(TerrainGrid grid, MovementProfile profile,
                                     int cx, int cy)
    {
        int clusterAx = cx;
        int clusterAy = cy;
        int clusterBx = cx + 1;
        int clusterBy = cy;

        // The border lies between the last column of cluster A and the first column of cluster B.
        int borderXA = (cx + 1) * ClusterSize - 1; // last column of cluster A
        int borderXB = (cx + 1) * ClusterSize;     // first column of cluster B

        // Clamp to actual grid bounds.
        if (borderXA >= GridWidth || borderXB >= GridWidth)
            return;

        // Y range for this cluster row, clamped to grid bounds.
        int yStart = cy * ClusterSize;
        int yEnd = Math.Min((cy + 1) * ClusterSize, GridHeight); // exclusive

        int startIndex = _allEntrances.Count;

        // Scan for contiguous runs of mutually-traversable border cells.
        int runStart = -1;
        for (int y = yStart; y < yEnd; y++)
        {
            bool traversable = TerrainCostCalculator.CanTraverse(grid, profile, borderXA, y)
                            && TerrainCostCalculator.CanTraverse(grid, profile, borderXB, y);

            if (traversable)
            {
                if (runStart == -1)
                    runStart = y;
            }
            else
            {
                if (runStart != -1)
                {
                    PlaceEntrancesForRun(runStart, y - 1, borderXA, borderXB,
                                         clusterAx, clusterAy, clusterBx, clusterBy,
                                         isHorizontalBorder: false);
                    runStart = -1;
                }
            }
        }

        // Close any open run at the end of the scan.
        if (runStart != -1)
        {
            PlaceEntrancesForRun(runStart, yEnd - 1, borderXA, borderXB,
                                 clusterAx, clusterAy, clusterBx, clusterBy,
                                 isHorizontalBorder: false);
        }

        int count = _allEntrances.Count - startIndex;
        if (count > 0)
        {
            long key = GetBorderPairKey(clusterAx, clusterAy, clusterBx, clusterBy);
            _borderEntranceMap[key] = (startIndex, count);
        }
    }

    // ── Private: Entrance Placement ──────────────────────────────────────

    /// <summary>
    /// Places entrance nodes for a contiguous run of traversable border cells.
    ///
    /// <para><b>Rules:</b></para>
    /// <list type="bullet">
    ///   <item>Run length ≤ 3: place 1 entrance at the midpoint.</item>
    ///   <item>Run length > 3: place 2 entrances at the endpoints.</item>
    /// </list>
    ///
    /// <para>For horizontal borders, the run is along the X axis (runStart/runEnd
    /// are X coordinates). For vertical borders, the run is along the Y axis
    /// (runStart/runEnd are Y coordinates).</para>
    /// </summary>
    /// <param name="runStart">Start coordinate of the run (X for horizontal, Y for vertical).</param>
    /// <param name="runEnd">End coordinate of the run (inclusive).</param>
    /// <param name="borderCoordA">The border coordinate on side A (Y for horizontal, X for vertical).</param>
    /// <param name="borderCoordB">The border coordinate on side B (Y for horizontal, X for vertical).</param>
    /// <param name="clusterAx">Cluster A X index.</param>
    /// <param name="clusterAy">Cluster A Y index.</param>
    /// <param name="clusterBx">Cluster B X index.</param>
    /// <param name="clusterBy">Cluster B Y index.</param>
    /// <param name="isHorizontalBorder">True if scanning a horizontal border (run along X).</param>
    private void PlaceEntrancesForRun(int runStart, int runEnd,
                                       int borderCoordA, int borderCoordB,
                                       int clusterAx, int clusterAy,
                                       int clusterBx, int clusterBy,
                                       bool isHorizontalBorder)
    {
        int runLength = runEnd - runStart + 1;
        int clusterA = clusterAy * ClustersX + clusterAx;
        int clusterB = clusterBy * ClustersX + clusterBx;

        if (runLength <= 3)
        {
            // Place one entrance at the midpoint of the run.
            int mid = runStart + runLength / 2;

            int entranceX, entranceY;
            if (isHorizontalBorder)
            {
                // Run is along X axis; border is between two Y rows.
                // Place entrance on the side-A row (the cell in cluster A).
                entranceX = mid;
                entranceY = borderCoordA;
            }
            else
            {
                // Run is along Y axis; border is between two X columns.
                // Place entrance on the side-A column (the cell in cluster A).
                entranceX = borderCoordA;
                entranceY = mid;
            }

            _allEntrances.Add(new EntranceNode
            {
                X = entranceX,
                Y = entranceY,
                ClusterA = clusterA,
                ClusterB = clusterB,
                NodeIndex = -1 // Assigned later by AbstractGraph
            });
        }
        else
        {
            // Place two entrances at the endpoints of the run.
            int entranceX1, entranceY1, entranceX2, entranceY2;
            if (isHorizontalBorder)
            {
                entranceX1 = runStart;
                entranceY1 = borderCoordA;
                entranceX2 = runEnd;
                entranceY2 = borderCoordA;
            }
            else
            {
                entranceX1 = borderCoordA;
                entranceY1 = runStart;
                entranceX2 = borderCoordA;
                entranceY2 = runEnd;
            }

            _allEntrances.Add(new EntranceNode
            {
                X = entranceX1,
                Y = entranceY1,
                ClusterA = clusterA,
                ClusterB = clusterB,
                NodeIndex = -1
            });

            _allEntrances.Add(new EntranceNode
            {
                X = entranceX2,
                Y = entranceY2,
                ClusterA = clusterA,
                ClusterB = clusterB,
                NodeIndex = -1
            });
        }
    }

    // ── Private: Utility ─────────────────────────────────────────────────

    /// <summary>
    /// Computes a unique key for a border pair. The key is symmetric-agnostic:
    /// we always store with the lower cluster index first to ensure
    /// GetEntrances(A, B) and GetEntrances(B, A) return the same result.
    /// </summary>
    private long GetBorderPairKey(int clusterAx, int clusterAy,
                                   int clusterBx, int clusterBy)
    {
        int idxA = clusterAy * ClustersX + clusterAx;
        int idxB = clusterBy * ClustersX + clusterBx;

        // Ensure consistent ordering: lower index first.
        if (idxA > idxB)
            (idxA, idxB) = (idxB, idxA);

        return ((long)idxA << 32) | (uint)idxB;
    }
}


