using System;
using System.Collections.Generic;
using CorditeWars.Core;

namespace CorditeWars.Systems.Pathfinding;

// ─────────────────────────────────────────────────────────────────────────────
// Hierarchical Pathfinder (HPA*)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Orchestrates two-level hierarchical A* pathfinding: abstract graph search
/// followed by local segment refinement.
///
/// <para><b>Design philosophy:</b> Long-distance paths on large maps (512×512)
/// are expensive with flat A*. By partitioning the grid into 16×16 clusters and
/// precomputing an abstract graph of border crossings, we reduce the search
/// space from ~262k cells to ~1k abstract nodes. Short paths (same cluster)
/// bypass the hierarchy entirely and use direct A* with a tight node budget.</para>
///
/// <para><b>Incremental updates:</b> When terrain changes (building placement,
/// destruction), only the affected clusters and their neighbors need rebuilding.
/// Call <see cref="InvalidateCell"/> for each changed cell, then
/// <see cref="RebuildInvalidated"/> once per batch.</para>
///
/// <para><b>Temporary nodes:</b> Start and goal positions are inserted as
/// temporary nodes into the abstract graph for each query, then removed in a
/// finally block to guarantee cleanup regardless of success or failure.</para>
/// </summary>
public sealed class HierarchicalPathfinder
{
    // ── Configuration ────────────────────────────────────────────────────

    private readonly int _clusterSize;

    // ── Precomputed State ────────────────────────────────────────────────

    private ClusterGrid? _clusterGrid;
    private AbstractGraph? _abstractGraph;
    private readonly AStarPathfinder _localPathfinder;

    // ── Dirty Cluster Tracking ───────────────────────────────────────────

    /// <summary>
    /// Set of cluster indices (cy * ClustersX + cx) that need rebuilding.
    /// </summary>
    private readonly HashSet<int> _dirtyClusters;

    // ── Constructor ──────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new hierarchical pathfinder with the specified cluster size.
    /// Call <see cref="Preprocess"/> before using <see cref="FindPath"/>.
    /// </summary>
    /// <param name="clusterSize">Cells per cluster side. Default 16.</param>
    public HierarchicalPathfinder(int clusterSize = 16)
    {
        if (clusterSize < 1)
            throw new ArgumentException("Cluster size must be at least 1.", nameof(clusterSize));

        _clusterSize = clusterSize;
        _localPathfinder = new AStarPathfinder();
        _dirtyClusters = new HashSet<int>();
    }

    // ── Public API ───────────────────────────────────────────────────────

    /// <summary>
    /// Precomputes the abstract graph for the given grid and movement profile.
    /// Must be called once per map load and after bulk terrain changes.
    /// </summary>
    /// <param name="grid">The terrain grid to partition and analyze.</param>
    /// <param name="profile">The movement profile determining traversability.</param>
    public void Preprocess(TerrainGrid grid, MovementProfile profile)
    {
        // Step 1: Build the cluster grid and detect entrances.
        _clusterGrid = new ClusterGrid(grid.Width, grid.Height, _clusterSize);
        _clusterGrid.BuildEntrances(grid, profile);

        // Step 2: Build the abstract graph from the cluster grid.
        _abstractGraph = new AbstractGraph();
        _abstractGraph.Build(_clusterGrid, grid, profile);

        // Clear any dirty state from previous preprocessing.
        _dirtyClusters.Clear();
    }

    /// <summary>
    /// Finds a path using hierarchical decomposition.
    /// Falls back to direct A* for same-cluster paths.
    ///
    /// <para><b>Same-cluster:</b> Delegates directly to AStarPathfinder with
    /// maxNodes=512 without using the abstract graph.</para>
    ///
    /// <para><b>Cross-cluster:</b> Inserts temporary nodes for start/goal,
    /// searches the abstract graph, then refines each segment with local A*.
    /// Concatenates refined segments into a single contiguous path, removing
    /// duplicate junction nodes at segment boundaries.</para>
    /// </summary>
    /// <param name="grid">The terrain grid.</param>
    /// <param name="profile">The movement profile.</param>
    /// <param name="startX">Start X coordinate.</param>
    /// <param name="startY">Start Y coordinate.</param>
    /// <param name="goalX">Goal X coordinate.</param>
    /// <param name="goalY">Goal Y coordinate.</param>
    /// <param name="maxNodes">Maximum nodes for local A* refinement. Default 8192.</param>
    /// <returns>
    /// Ordered list of (x, y) grid cells from start to goal, or empty if no path exists.
    /// </returns>
    public List<(int x, int y)> FindPath(
        TerrainGrid grid, MovementProfile profile,
        int startX, int startY, int goalX, int goalY,
        int maxNodes = 8192)
    {
        if (_clusterGrid == null || _abstractGraph == null)
            return new List<(int x, int y)>();

        // Trivial case: already at the goal.
        if (startX == goalX && startY == goalY)
            return new List<(int x, int y)> { (startX, startY) };

        // Determine which clusters start and goal belong to.
        var (scx, scy) = _clusterGrid.GetClusterForCell(startX, startY);
        var (gcx, gcy) = _clusterGrid.GetClusterForCell(goalX, goalY);

        // Same cluster: use direct A* (cheaper than hierarchical overhead).
        if (scx == gcx && scy == gcy)
            return _localPathfinder.FindPath(grid, profile, startX, startY,
                                              goalX, goalY, maxNodes: 512);

        // Cross-cluster: hierarchical decomposition.
        int startNode = -1;
        int goalNode = -1;

        try
        {
            // Insert temporary start and goal nodes into the abstract graph.
            startNode = _abstractGraph.InsertTemporaryNode(startX, startY, grid, profile);
            goalNode = _abstractGraph.InsertTemporaryNode(goalX, goalY, grid, profile);

            // Search the abstract graph for a high-level path.
            List<int> abstractPath = _abstractGraph.Search(startNode, goalNode);

            if (abstractPath.Count == 0)
                return new List<(int x, int y)>();

            // Refine: for each consecutive pair of abstract nodes, run local A*.
            var fullPath = new List<(int x, int y)>();
            int refinementMaxNodes = _clusterSize * _clusterSize * 2;

            for (int i = 0; i < abstractPath.Count - 1; i++)
            {
                var (ax, ay) = _abstractGraph.GetNodePosition(abstractPath[i]);
                var (bx, by) = _abstractGraph.GetNodePosition(abstractPath[i + 1]);

                var segment = _localPathfinder.FindPath(grid, profile, ax, ay, bx, by,
                                                         maxNodes: refinementMaxNodes);

                if (segment.Count == 0)
                {
                    // Refinement failed — mark affected clusters as dirty.
                    MarkClusterDirty(ax, ay);
                    MarkClusterDirty(bx, by);
                    return new List<(int x, int y)>();
                }

                // Append segment, skipping the first node (duplicate junction)
                // for all segments after the first.
                int startIdx = (i == 0) ? 0 : 1;
                for (int j = startIdx; j < segment.Count; j++)
                {
                    fullPath.Add(segment[j]);
                }
            }

            return fullPath;
        }
        finally
        {
            // Always remove temporary nodes, regardless of success or failure.
            _abstractGraph.RemoveTemporaryNodes();
        }
    }

    /// <summary>
    /// Marks the cluster containing (x, y) and adjacent clusters sharing a
    /// border with the affected cell as dirty. Call this when terrain changes
    /// at a specific cell (building placement, destruction, etc.).
    /// </summary>
    /// <param name="x">Grid X coordinate of the changed cell.</param>
    /// <param name="y">Grid Y coordinate of the changed cell.</param>
    public void InvalidateCell(int x, int y)
    {
        if (_clusterGrid == null)
            return;

        var (cx, cy) = _clusterGrid.GetClusterForCell(x, y);
        int clustersX = _clusterGrid.ClustersX;
        int clustersY = _clusterGrid.ClustersY;

        // Mark the containing cluster as dirty.
        _dirtyClusters.Add(cy * clustersX + cx);

        // Determine if the cell is on a cluster border and mark adjacent
        // clusters as dirty if so.
        int localX = x - cx * _clusterSize;
        int localY = y - cy * _clusterSize;

        // Left border: if localX == 0 and there's a cluster to the left.
        if (localX == 0 && cx > 0)
            _dirtyClusters.Add(cy * clustersX + (cx - 1));

        // Right border: if localX == clusterSize - 1 (or at the edge of a
        // smaller boundary cluster) and there's a cluster to the right.
        int clusterWidth = Math.Min(_clusterSize, _clusterGrid.GridWidth - cx * _clusterSize);
        if (localX == clusterWidth - 1 && cx < clustersX - 1)
            _dirtyClusters.Add(cy * clustersX + (cx + 1));

        // Top border: if localY == 0 and there's a cluster above.
        if (localY == 0 && cy > 0)
            _dirtyClusters.Add((cy - 1) * clustersX + cx);

        // Bottom border: if localY == clusterSize - 1 (or at the edge of a
        // smaller boundary cluster) and there's a cluster below.
        int clusterHeight = Math.Min(_clusterSize, _clusterGrid.GridHeight - cy * _clusterSize);
        if (localY == clusterHeight - 1 && cy < clustersY - 1)
            _dirtyClusters.Add((cy + 1) * clustersX + cx);
    }

    /// <summary>
    /// Rebuilds only the dirty clusters and their associated abstract graph edges.
    /// Call this after a batch of <see cref="InvalidateCell"/> calls to update
    /// the hierarchical pathfinding data structures.
    /// </summary>
    /// <param name="grid">The terrain grid (with updated terrain data).</param>
    /// <param name="profile">The movement profile.</param>
    public void RebuildInvalidated(TerrainGrid grid, MovementProfile profile)
    {
        if (_clusterGrid == null || _abstractGraph == null)
            return;

        if (_dirtyClusters.Count == 0)
            return;

        // For simplicity and correctness, rebuild the entire cluster grid
        // entrances and abstract graph when clusters are dirty.
        // A more sophisticated implementation could rebuild only affected
        // borders, but the full rebuild is still fast for typical use cases
        // (building placement affects 1-4 clusters).
        _clusterGrid.BuildEntrances(grid, profile);
        _abstractGraph.Build(_clusterGrid, grid, profile);

        _dirtyClusters.Clear();
    }

    // ── Private Helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Marks the cluster containing the given cell as dirty.
    /// </summary>
    private void MarkClusterDirty(int x, int y)
    {
        if (_clusterGrid == null)
            return;

        var (cx, cy) = _clusterGrid.GetClusterForCell(x, y);
        _dirtyClusters.Add(cy * _clusterGrid.ClustersX + cx);
    }
}
