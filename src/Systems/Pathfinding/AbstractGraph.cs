using System;
using System.Buffers;
using System.Collections.Generic;
using CorditeWars.Core;

namespace CorditeWars.Systems.Pathfinding;

// ─────────────────────────────────────────────────────────────────────────────
// Abstract Edge
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Weighted edge in the abstract graph connecting entrance nodes.
/// </summary>
public struct AbstractEdge
{
    /// <summary>Target node index in the abstract graph.</summary>
    public int TargetNode;

    /// <summary>FixedPoint cost (intra-cluster A* distance or inter-cluster crossing cost).</summary>
    public FixedPoint Cost;

    /// <summary>Whether this is an inter-cluster edge (crossing) or intra-cluster edge.</summary>
    public bool IsInterCluster;
}

// ─────────────────────────────────────────────────────────────────────────────
// Abstract Graph
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Stores the high-level graph of entrance nodes and intra/inter-cluster edges,
/// enabling fast cross-map pathfinding via HPA*.
///
/// <para><b>Design decisions:</b></para>
/// <list type="bullet">
///   <item>Uses SortedList for edge storage to guarantee deterministic iteration
///         order across all platforms.</item>
///   <item>All cost arithmetic uses FixedPoint exclusively for lockstep determinism.</item>
///   <item>Array-backed min-heap for A* search (same pattern as AStarPathfinder).</item>
///   <item>Supports temporary node insertion for start/goal positions without
///         rebuilding the entire graph.</item>
///   <item>Each entrance position creates a single node that belongs to both
///         adjacent clusters. Inter-cluster edges connect entrance nodes that
///         share the same border with cost FixedPoint.One.</item>
/// </list>
/// </summary>
public sealed class AbstractGraph
{
    // ── Internal Node Data ───────────────────────────────────────────────

    /// <summary>Position (x, y) of each node, indexed by node index.</summary>
    private readonly List<(int x, int y)> _nodePositions;

    /// <summary>
    /// Adjacency list: for each node, a SortedList of edges keyed by target node index.
    /// SortedList guarantees deterministic iteration order.
    /// </summary>
    private readonly List<SortedList<int, AbstractEdge>> _adjacency;

    /// <summary>Number of permanent nodes (set after Build, before temporary insertions).</summary>
    private int _permanentNodeCount;

    /// <summary>The cluster grid used during the last Build call.</summary>
    private ClusterGrid? _clusterGrid;

    /// <summary>The cluster size used during the last Build call.</summary>
    private int _clusterSize;

    /// <summary>Shared A* pathfinder instance for intra-cluster path computation.</summary>
    private readonly AStarPathfinder _pathfinder;

    // ── Constructor ──────────────────────────────────────────────────────

    public AbstractGraph()
    {
        _nodePositions = new List<(int x, int y)>();
        _adjacency = new List<SortedList<int, AbstractEdge>>();
        _permanentNodeCount = 0;
        _pathfinder = new AStarPathfinder();
    }

    // ── Public Properties ────────────────────────────────────────────────

    /// <summary>Total number of permanent nodes in the graph.</summary>
    public int NodeCount => _permanentNodeCount;

    // ── Public API ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds the abstract graph from a ClusterGrid and TerrainGrid.
    /// Creates a node for each entrance, computes intra-cluster edges via
    /// confined A*, and adds inter-cluster edges with cost FixedPoint.One.
    /// </summary>
    public void Build(ClusterGrid clusters, TerrainGrid grid, MovementProfile profile)
    {
        _clusterGrid = clusters;
        _clusterSize = clusters.ClusterSize;

        _nodePositions.Clear();
        _adjacency.Clear();

        // Step 1: Create a node for each entrance node.
        // Each entrance position gets a unique node. The entrance belongs to
        // both ClusterA and ClusterB.
        var allEntrances = clusters.GetAllEntrances();
        int entranceCount = allEntrances.Length;

        // Map from position to node index (deduplicate same-position entrances).
        var positionToNodeIndex = new Dictionary<(int x, int y), int>();

        // Track which clusters each node belongs to.
        var nodeClusters = new List<HashSet<int>>();

        for (int i = 0; i < entranceCount; i++)
        {
            var entrance = allEntrances[i];
            var pos = (entrance.X, entrance.Y);

            if (!positionToNodeIndex.ContainsKey(pos))
            {
                int nodeIdx = _nodePositions.Count;
                positionToNodeIndex[pos] = nodeIdx;
                _nodePositions.Add(pos);
                _adjacency.Add(new SortedList<int, AbstractEdge>());
                nodeClusters.Add(new HashSet<int> { entrance.ClusterA, entrance.ClusterB });
            }
            else
            {
                int nodeIdx = positionToNodeIndex[pos];
                nodeClusters[nodeIdx].Add(entrance.ClusterA);
                nodeClusters[nodeIdx].Add(entrance.ClusterB);
            }
        }

        _permanentNodeCount = _nodePositions.Count;

        // Step 2: Build a lookup of nodes per cluster for intra-cluster edge computation.
        var nodesPerCluster = new Dictionary<int, List<int>>();
        for (int nodeIdx = 0; nodeIdx < _permanentNodeCount; nodeIdx++)
        {
            foreach (int clusterIdx in nodeClusters[nodeIdx])
            {
                if (!nodesPerCluster.ContainsKey(clusterIdx))
                    nodesPerCluster[clusterIdx] = new List<int>();
                nodesPerCluster[clusterIdx].Add(nodeIdx);
            }
        }

        // Step 3: Compute intra-cluster edges.
        // For each cluster, for each pair of entrance nodes within that cluster,
        // run A* confined to that cluster's bounds (maxNodes = clusterSize^2).
        int maxNodesForCluster = _clusterSize * _clusterSize;

        foreach (var kvp in nodesPerCluster)
        {
            var clusterNodes = kvp.Value;
            for (int i = 0; i < clusterNodes.Count; i++)
            {
                for (int j = i + 1; j < clusterNodes.Count; j++)
                {
                    int nodeA = clusterNodes[i];
                    int nodeB = clusterNodes[j];

                    var (ax, ay) = _nodePositions[nodeA];
                    var (bx, by) = _nodePositions[nodeB];

                    var path = _pathfinder.FindPath(grid, profile, ax, ay, bx, by, maxNodesForCluster);

                    if (path.Count > 0)
                    {
                        FixedPoint cost = ComputePathCost(path);

                        if (cost > FixedPoint.Zero)
                        {
                            AddEdge(nodeA, nodeB, cost, isInterCluster: false);
                            AddEdge(nodeB, nodeA, cost, isInterCluster: false);
                        }
                    }
                }
            }
        }

        // Step 4: Create inter-cluster edges.
        // For each pair of entrance nodes that share a border between two clusters,
        // add an inter-cluster edge with cost = FixedPoint.One.
        // Two nodes share a border if they are adjacent (Chebyshev distance 1)
        // and belong to different clusters on that border.
        for (int i = 0; i < entranceCount; i++)
        {
            var entranceA = allEntrances[i];
            (int x, int y) posA = (entranceA.X, entranceA.Y);
            int nodeIdxA = positionToNodeIndex[posA];

            // For each entrance, find its "partner" on the other side of the border.
            // The partner is the entrance at the adjacent cell in the other cluster.
            // For horizontal borders: partner is at (x, y+1) or (x, y-1)
            // For vertical borders: partner is at (x+1, y) or (x-1, y)
            // Check all 4 adjacent positions for a node.
            (int x, int y)[] adjacentPositions =
            {
                (entranceA.X + 1, entranceA.Y),
                (entranceA.X - 1, entranceA.Y),
                (entranceA.X, entranceA.Y + 1),
                (entranceA.X, entranceA.Y - 1)
            };

            for (int d = 0; d < 4; d++)
            {
                var adjPos = adjacentPositions[d];
                if (positionToNodeIndex.TryGetValue(adjPos, out int nodeIdxB))
                {
                    // Check that these two nodes are in different clusters
                    // (i.e., they represent a border crossing).
                    var (cxA, cyA) = clusters.GetClusterForCell(posA.x, posA.y);
                    var (cxB, cyB) = clusters.GetClusterForCell(adjPos.x, adjPos.y);

                    if (cxA != cxB || cyA != cyB)
                    {
                        // This is an inter-cluster edge.
                        AddEdge(nodeIdxA, nodeIdxB, FixedPoint.One, isInterCluster: true);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Inserts a temporary start/goal node, connecting it to all entrance nodes
    /// in its cluster via intra-cluster A* distances.
    /// Returns the node index of the temporary node.
    /// </summary>
    public int InsertTemporaryNode(int x, int y, TerrainGrid grid, MovementProfile profile)
    {
        if (_clusterGrid == null)
            throw new InvalidOperationException("Must call Build before InsertTemporaryNode.");

        int nodeIdx = _nodePositions.Count;
        _nodePositions.Add((x, y));
        _adjacency.Add(new SortedList<int, AbstractEdge>());

        // Find which cluster this position belongs to.
        var (cx, cy) = _clusterGrid.GetClusterForCell(x, y);

        // Connect to all entrance nodes in this cluster.
        int maxNodesForCluster = _clusterSize * _clusterSize;

        for (int i = 0; i < _permanentNodeCount; i++)
        {
            var (nx, ny) = _nodePositions[i];

            // Check if this node is within the same cluster bounds.
            if (!IsInCluster(nx, ny, cx, cy))
                continue;

            // Run A* from the temporary node to this entrance node.
            var path = _pathfinder.FindPath(grid, profile, x, y, nx, ny, maxNodesForCluster);

            if (path.Count > 0)
            {
                FixedPoint cost = ComputePathCost(path);
                if (cost > FixedPoint.Zero)
                {
                    AddEdge(nodeIdx, i, cost, isInterCluster: false);
                    AddEdge(i, nodeIdx, cost, isInterCluster: false);
                }
            }
        }

        // Also connect to other temporary nodes in the same cluster.
        for (int i = _permanentNodeCount; i < nodeIdx; i++)
        {
            var (nx, ny) = _nodePositions[i];
            if (!IsInCluster(nx, ny, cx, cy))
                continue;

            var path = _pathfinder.FindPath(grid, profile, x, y, nx, ny, maxNodesForCluster);
            if (path.Count > 0)
            {
                FixedPoint cost = ComputePathCost(path);
                if (cost > FixedPoint.Zero)
                {
                    AddEdge(nodeIdx, i, cost, isInterCluster: false);
                    AddEdge(i, nodeIdx, cost, isInterCluster: false);
                }
            }
        }

        return nodeIdx;
    }

    /// <summary>
    /// Removes all temporary nodes and their edges added since the last Build call.
    /// </summary>
    public void RemoveTemporaryNodes()
    {
        int tempCount = _nodePositions.Count - _permanentNodeCount;
        if (tempCount <= 0)
            return;

        // Remove edges from permanent nodes that point to temporary nodes.
        for (int i = 0; i < _permanentNodeCount; i++)
        {
            var edges = _adjacency[i];
            // Collect keys to remove (can't modify SortedList during iteration).
            var keysToRemove = new List<int>();
            for (int k = 0; k < edges.Count; k++)
            {
                if (edges.Keys[k] >= _permanentNodeCount)
                    keysToRemove.Add(edges.Keys[k]);
            }
            for (int k = 0; k < keysToRemove.Count; k++)
            {
                edges.Remove(keysToRemove[k]);
            }
        }

        // Remove temporary node data.
        _nodePositions.RemoveRange(_permanentNodeCount, tempCount);
        _adjacency.RemoveRange(_permanentNodeCount, tempCount);
    }

    /// <summary>
    /// A* search on the abstract graph using FixedPoint costs.
    /// Uses an array-backed min-heap for determinism.
    /// Returns ordered list of node indices from start to goal (inclusive),
    /// or empty list if no path exists.
    /// </summary>
    public List<int> Search(int startNodeIdx, int goalNodeIdx)
    {
        var result = new List<int>();
        int totalNodes = _nodePositions.Count;

        if (startNodeIdx < 0 || startNodeIdx >= totalNodes ||
            goalNodeIdx < 0 || goalNodeIdx >= totalNodes)
            return result;

        if (startNodeIdx == goalNodeIdx)
        {
            result.Add(startNodeIdx);
            return result;
        }

        // A* on the abstract graph using array-backed data structures.
        var gCost = ArrayPool<FixedPoint>.Shared.Rent(totalNodes);
        var fCost = ArrayPool<FixedPoint>.Shared.Rent(totalNodes);
        var parent = ArrayPool<int>.Shared.Rent(totalNodes);
        var closed = ArrayPool<bool>.Shared.Rent(totalNodes);

        // Heap backing array. Each entry is a node index.
        int heapCapacity = totalNodes * 4;
        var heapItems = ArrayPool<int>.Shared.Rent(heapCapacity);

        try
        {
            for (int i = 0; i < totalNodes; i++)
            {
                gCost[i] = FixedPoint.MaxValue;
                fCost[i] = FixedPoint.MaxValue;
                parent[i] = -1;
                closed[i] = false;
            }

            gCost[startNodeIdx] = FixedPoint.Zero;
            FixedPoint startH = Heuristic(startNodeIdx, goalNodeIdx);
            fCost[startNodeIdx] = startH;

            // Min-heap ordered by fCost, tie-break by higher gCost (prefer farther nodes).
            var openHeap = new MinHeap(heapItems, heapCapacity, fCost, gCost);

            openHeap.Push(startNodeIdx);

            while (openHeap.Count > 0)
            {
                int current = openHeap.Pop();

                if (closed[current])
                    continue;

                closed[current] = true;

                if (current == goalNodeIdx)
                {
                    // Reconstruct path.
                    int idx = goalNodeIdx;
                    while (idx != -1)
                    {
                        result.Add(idx);
                        idx = parent[idx];
                    }
                    result.Reverse();
                    return result;
                }

                // Expand neighbors.
                var edges = _adjacency[current];
                for (int e = 0; e < edges.Count; e++)
                {
                    int neighbor = edges.Keys[e];
                    if (closed[neighbor])
                        continue;

                    var edge = edges.Values[e];
                    FixedPoint tentativeG = gCost[current] + edge.Cost;

                    if (tentativeG < gCost[neighbor])
                    {
                        gCost[neighbor] = tentativeG;
                        FixedPoint h = Heuristic(neighbor, goalNodeIdx);
                        fCost[neighbor] = tentativeG + h;
                        parent[neighbor] = current;
                        openHeap.Push(neighbor);
                    }
                }
            }

            // No path found.
            return result;
        }
        finally
        {
            ArrayPool<FixedPoint>.Shared.Return(gCost);
            ArrayPool<FixedPoint>.Shared.Return(fCost);
            ArrayPool<int>.Shared.Return(parent);
            ArrayPool<bool>.Shared.Return(closed);
            ArrayPool<int>.Shared.Return(heapItems);
        }
    }

    /// <summary>
    /// Gets the grid position of a node by index.
    /// </summary>
    public (int x, int y) GetNodePosition(int nodeIdx)
    {
        if (nodeIdx < 0 || nodeIdx >= _nodePositions.Count)
            throw new ArgumentOutOfRangeException(nameof(nodeIdx));
        return _nodePositions[nodeIdx];
    }

    // ── Private Helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Adds a directed edge from sourceNode to targetNode.
    /// Uses SortedList keyed by target node index for deterministic ordering.
    /// </summary>
    private void AddEdge(int sourceNode, int targetNode, FixedPoint cost, bool isInterCluster)
    {
        var edges = _adjacency[sourceNode];
        var edge = new AbstractEdge
        {
            TargetNode = targetNode,
            Cost = cost,
            IsInterCluster = isInterCluster
        };

        // If an edge to this target already exists, keep the cheaper one.
        if (edges.TryGetValue(targetNode, out var existing))
        {
            if (cost < existing.Cost)
                edges[targetNode] = edge;
        }
        else
        {
            edges[targetNode] = edge;
        }
    }

    /// <summary>
    /// Computes the path cost from a list of grid positions.
    /// Uses octile distance (cardinal = 1, diagonal = sqrt(2)) in FixedPoint.
    /// </summary>
    private static FixedPoint ComputePathCost(List<(int x, int y)> path)
    {
        if (path.Count <= 1)
            return FixedPoint.Zero;

        FixedPoint cost = FixedPoint.Zero;
        FixedPoint cardinalCost = FixedPoint.One;
        FixedPoint diagonalCost = FixedPoint.Sqrt(FixedPoint.FromInt(2));

        for (int i = 1; i < path.Count; i++)
        {
            int dx = Math.Abs(path[i].x - path[i - 1].x);
            int dy = Math.Abs(path[i].y - path[i - 1].y);

            if (dx == 1 && dy == 1)
                cost = cost + diagonalCost;
            else
                cost = cost + cardinalCost;
        }

        return cost;
    }

    /// <summary>
    /// Octile distance heuristic between two abstract graph nodes.
    /// Uses grid positions for the estimate.
    /// </summary>
    private FixedPoint Heuristic(int nodeA, int nodeB)
    {
        var (ax, ay) = _nodePositions[nodeA];
        var (bx, by) = _nodePositions[nodeB];

        int dx = Math.Abs(bx - ax);
        int dy = Math.Abs(by - ay);

        FixedPoint fdx = FixedPoint.FromInt(dx);
        FixedPoint fdy = FixedPoint.FromInt(dy);

        FixedPoint maxD = FixedPoint.Max(fdx, fdy);
        FixedPoint minD = FixedPoint.Min(fdx, fdy);
        FixedPoint diagonalExtra = FixedPoint.Sqrt(FixedPoint.FromInt(2)) - FixedPoint.One;

        return maxD + diagonalExtra * minD;
    }

    /// <summary>
    /// Checks if a cell (x, y) is within the bounds of cluster (cx, cy).
    /// </summary>
    private bool IsInCluster(int x, int y, int cx, int cy)
    {
        if (_clusterGrid == null) return false;

        int clusterStartX = cx * _clusterSize;
        int clusterStartY = cy * _clusterSize;
        int clusterEndX = Math.Min(clusterStartX + _clusterSize, _clusterGrid.GridWidth);
        int clusterEndY = Math.Min(clusterStartY + _clusterSize, _clusterGrid.GridHeight);

        return x >= clusterStartX && x < clusterEndX &&
               y >= clusterStartY && y < clusterEndY;
    }

    // ── Array-Backed Min-Heap ────────────────────────────────────────────

    /// <summary>
    /// Array-backed binary min-heap for the abstract graph A* search.
    /// Deterministic ordering: primary sort by fCost, tie-break by higher gCost
    /// (prefer nodes that have travelled farther from start).
    /// No LINQ, no hidden allocations after initial setup.
    /// </summary>
    private sealed class MinHeap
    {
        private readonly int[] _items;
        private readonly int _capacity;
        private readonly FixedPoint[] _fCost;
        private readonly FixedPoint[] _gCost;
        private int _count;

        public int Count => _count;

        public MinHeap(int[] backingArray, int capacity, FixedPoint[] fCost, FixedPoint[] gCost)
        {
            _items = backingArray;
            _capacity = capacity;
            _fCost = fCost;
            _gCost = gCost;
            _count = 0;
        }

        public void Push(int item)
        {
            if (_count >= _capacity)
                throw new InvalidOperationException("MinHeap capacity exceeded.");

            _items[_count] = item;
            SiftUp(_count);
            _count++;
        }

        public int Pop()
        {
            if (_count == 0)
                throw new InvalidOperationException("MinHeap is empty.");

            int min = _items[0];
            _count--;
            _items[0] = _items[_count];

            if (_count > 0)
                SiftDown(0);

            return min;
        }

        private int Compare(int a, int b)
        {
            int cmp = _fCost[a].CompareTo(_fCost[b]);
            if (cmp != 0) return cmp;
            // Tie-break: prefer the node that has travelled farther (higher gCost).
            return _gCost[b].CompareTo(_gCost[a]);
        }

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parentIdx = (index - 1) >> 1;
                if (Compare(_items[index], _items[parentIdx]) < 0)
                {
                    (_items[index], _items[parentIdx]) = (_items[parentIdx], _items[index]);
                    index = parentIdx;
                }
                else
                {
                    break;
                }
            }
        }

        private void SiftDown(int index)
        {
            while (true)
            {
                int left = (index << 1) + 1;
                int right = (index << 1) + 2;
                int smallest = index;

                if (left < _count && Compare(_items[left], _items[smallest]) < 0)
                    smallest = left;

                if (right < _count && Compare(_items[right], _items[smallest]) < 0)
                    smallest = right;

                if (smallest == index)
                    break;

                (_items[index], _items[smallest]) = (_items[smallest], _items[index]);
                index = smallest;
            }
        }
    }
}
