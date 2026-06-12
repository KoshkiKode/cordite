# Implementation Plan: Hierarchical Pathfinding and Polish

## Overview

This plan implements four components: HPA* hierarchical pathfinding, a AAA procedural terrain engine, GameSession integration tests, and a branding text fix. Tasks are ordered so foundational types are built first, then composed into higher-level systems, with integration wiring at the end. The branding fix is standalone and can be done at any point.

## Tasks

- [x] 1. Branding text fix
  - Replace `"BRANDING PLACEHOLDER"` with `"Precision-Crafted Warfare"` in `src/UI/KoshkiKodeBrandingScreen.cs`
  - _Requirements: 16.1, 16.2_

- [x] 2. HPA* — ClusterGrid implementation
  - [x] 2.1 Create `src/Systems/Pathfinding/ClusterGrid.cs`
    - Implement constructor accepting gridWidth, gridHeight, clusterSize (default 16)
    - Compute ClustersX/ClustersY with ceiling division for non-evenly-divisible grids
    - Implement `GetClusterForCell(int x, int y)` returning (cx, cy) via integer division
    - Implement entrance detection: scan horizontal and vertical borders between adjacent clusters
    - Identify contiguous runs of mutually-traversable border cells (both sides must be traversable)
    - Place 1 EntranceNode at midpoint for runs ≤ 3, 2 EntranceNodes at endpoints for runs > 3
    - Clamp border scans to actual grid bounds for boundary clusters
    - Implement `GetEntrances(clusterAx, clusterAy, clusterBx, clusterBy)` returning entrance nodes
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 4.5_

  - [ ]* 2.2 Write property test: Cluster Index Correctness (Property 6)
    - **Property 6: Cluster Index Correctness**
    - For random valid cell coordinates, verify GetClusterForCell returns (x/16, y/16) integer division
    - Verify total cluster count equals ceil(W/16) × ceil(H/16)
    - **Validates: Requirements 1.5, 1.1, 1.6**

  - [ ]* 2.3 Write property test: Entrance Placement Rules (Property 5)
    - **Property 5: Entrance Placement Rules**
    - Generate random grids with known traversable/blocked patterns
    - Verify runs ≤ 3 produce exactly 1 entrance at midpoint, runs > 3 produce exactly 2 at endpoints
    - **Validates: Requirements 1.3, 1.4**

  - [ ]* 2.4 Write property test: Entrance Boundary Correctness (Property 4)
    - **Property 4: Entrance Boundary Correctness**
    - For all EntranceNodes created, verify the cell is traversable from both adjacent clusters
    - **Validates: Requirements 4.5, 1.2**

- [x] 3. HPA* — AbstractGraph implementation
  - [x] 3.1 Create `src/Systems/Pathfinding/AbstractGraph.cs`
    - Define `EntranceNode` and `AbstractEdge` structs as specified in design
    - Implement `Build(ClusterGrid, TerrainGrid, MovementProfile)` — create nodes for each entrance, compute intra-cluster edges via confined A*, add inter-cluster edges with cost FixedPoint.One
    - Use SortedList for edge storage to guarantee deterministic iteration order
    - Implement `InsertTemporaryNode(x, y, grid, profile)` — connect to all entrances in the cluster via intra-cluster A* distances
    - Implement `RemoveTemporaryNodes()` — remove all temporary nodes and edges added since last Build
    - Implement `Search(startNodeIdx, goalNodeIdx)` — A* on abstract graph using FixedPoint costs, array-backed min-heap
    - Implement `GetNodePosition(nodeIdx)` returning (x, y)
    - All arithmetic must use FixedPoint exclusively
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 4.4_

  - [ ]* 3.2 Write property test: Temporary Node Cleanup (Property 7)
    - **Property 7: Temporary Node Cleanup**
    - Insert N temporary nodes, call RemoveTemporaryNodes, verify NodeCount returns to pre-insertion value
    - Test with varying numbers of insertions and interleaved Search calls
    - **Validates: Requirements 2.5, 3.6**

  - [ ]* 3.3 Write unit tests for AbstractGraph
    - Test Build produces correct node count for known grid configurations
    - Test intra-cluster edge costs match direct A* distances
    - Test inter-cluster edges have cost FixedPoint.One
    - Test Search returns optimal path on small known graphs
    - _Requirements: 2.1, 2.2, 2.3, 2.6_

- [x] 4. HPA* — HierarchicalPathfinder implementation
  - [x] 4.1 Create `src/Systems/Pathfinding/HierarchicalPathfinder.cs`
    - Implement `Preprocess(TerrainGrid, MovementProfile)` — build ClusterGrid and AbstractGraph
    - Implement `FindPath(grid, profile, startX, startY, goalX, goalY, maxNodes)`:
      - Same-cluster: delegate to AStarPathfinder with maxNodes=512
      - Cross-cluster: insert temporary nodes, search abstract graph, refine segments with local A*
      - Concatenate segments removing duplicate junction nodes
      - Always remove temporary nodes in finally block
      - Return empty path if abstract search or any refinement fails
    - Implement `InvalidateCell(x, y)` — mark containing cluster and adjacent border-sharing clusters as dirty
    - Implement `RebuildInvalidated(grid, profile)` — rebuild only dirty clusters and their abstract graph edges
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

  - [ ]* 4.2 Write property test: Path Validity (Property 1)
    - **Property 1: Path Validity**
    - For random grids and start/goal pairs, verify every consecutive cell pair has Chebyshev distance 1, both cells are traversable, and no consecutive duplicates exist
    - **Validates: Requirements 4.1, 3.5**

  - [ ]* 4.3 Write property test: Path Completeness (Property 2)
    - **Property 2: Path Completeness**
    - For random grids where AStarPathfinder finds a path, verify HierarchicalPathfinder also finds a non-empty path
    - **Validates: Requirements 4.2, 3.1**

  - [ ]* 4.4 Write property test: Pathfinding Determinism (Property 3)
    - **Property 3: Pathfinding Determinism**
    - Run identical FindPath calls multiple times, verify bit-identical results
    - **Validates: Requirements 4.3, 4.4**

- [x] 5. HPA* — Integration with PathRequestManager
  - [x] 5.1 Wire HierarchicalPathfinder into PathRequestManager
    - Add HierarchicalPathfinder as the primary pathfinding strategy for cross-cluster paths
    - Call Preprocess on map load
    - Call InvalidateCell/RebuildInvalidated on terrain changes (building placement, destruction)
    - Maintain the existing 4-paths-per-tick budget
    - _Requirements: 3.1, 3.7, 3.8_

- [x] 6. Checkpoint — HPA* pathfinding complete
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Terrain Engine — ErosionSimulator
  - [x] 7.1 Create `src/Game/World/ErosionSimulator.cs`
    - Implement `HydraulicErosion(float[] elevation, int width, int height, int iterations, uint seed)`
    - Each droplet traces downhill path, erodes material based on velocity, deposits sediment based on carrying capacity
    - Use deterministic seeded RNG (not System.Random) for droplet placement
    - Implement `ThermalErosion(float[] elevation, int width, int height, int passes, float talusAngle)`
    - Redistribute material from slopes exceeding talus angle to adjacent lower cells
    - Operate on base grid resolution (before subdivision)
    - Must produce bit-identical results for same inputs across platforms
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

  - [ ]* 7.2 Write property test: Erosion Determinism (Property 10)
    - **Property 10: Erosion Determinism**
    - Run HydraulicErosion and ThermalErosion with same inputs multiple times, verify bit-identical output
    - **Validates: Requirements 6.4**

  - [ ]* 7.3 Write property test: Thermal Erosion Slope Reduction (Property 11)
    - **Property 11: Thermal Erosion Slope Reduction**
    - After thermal erosion, verify maximum slope between adjacent cells is reduced compared to input
    - **Validates: Requirements 6.5**

- [x] 8. Terrain Engine — TerrainSubdivision
  - [x] 8.1 Create `src/Game/World/TerrainSubdivision.cs`
    - Implement Catmull-Rom bicubic interpolation for elevation at subdivision vertices
    - Support subdivision factors: 1× (Potato), 2× (Low), 4× (Medium), 8× (High)
    - Generate mesh vertices with interpolated positions ensuring C1 continuity across chunk and cell boundaries
    - Implement `GetElevationAtWorld(float worldX, float worldZ)` using same Catmull-Rom spline as mesh
    - Ensure no seams, cracks, or T-junctions at chunk boundaries
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7_

  - [ ]* 8.2 Write property test: Terrain-Simulation Elevation Consistency (Property 8)
    - **Property 8: Terrain-Simulation Elevation Consistency**
    - For random world positions corresponding to mesh vertices, verify GetElevationAtWorld returns value within 0.01 of actual mesh vertex Y
    - **Validates: Requirements 5.7, 5.5**

  - [ ]* 8.3 Write property test: Terrain Chunk Boundary Continuity (Property 9)
    - **Property 9: Terrain Chunk Boundary Continuity**
    - For adjacent chunk pairs, verify vertex positions and normals at shared boundaries are C1 continuous
    - **Validates: Requirements 5.6, 12.3**

- [x] 9. Terrain Engine — TerrainMaterialSystem (PBR uber-shader)
  - [x] 9.1 Create `src/Game/World/Shaders/terrain_pbr.gdshader`
    - Implement 5-layer procedural PBR material blending (bedrock, rocky soil, grass/vegetation, sand/dirt, river sediment)
    - Blend based on elevation, slope, moisture, and noise — no external texture files
    - Implement triplanar projection for slopes > 45°, standard UV for flat areas, smooth blend at transition
    - Scale layer count by quality tier: 2 layers (Potato), 3 layers (Low), 5 layers (Medium/High)
    - Accept per-vertex biome hints via vertex color channels
    - Ensure compatibility with gl_compatibility renderer (no tessellation or compute shaders)
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8, 7.9, 7.10_

  - [x] 9.2 Create `src/Game/World/TerrainMaterialSystem.cs`
    - C# class that configures the shader material per quality tier
    - Set uniform values for layer count, triplanar toggle, biome parameters
    - Provide API to set per-chunk biome hints via vertex colors
    - _Requirements: 7.6, 7.7, 7.8, 7.9_

- [x] 10. Terrain Engine — TerrainDetailPass
  - [x] 10.1 Create `src/Game/World/TerrainDetailPass.cs`
    - Implement Poisson disk sampling for natural distribution of detail instances
    - Scatter grass, pebbles, debris using MultiMeshInstance3D
    - Density: sparse for Medium, dense for High, none for Potato/Low
    - Implement LOD fade removing instances beyond 80 units from camera
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

  - [ ]* 10.2 Write property test: Detail Instance Poisson Disk Spacing (Property 17)
    - **Property 17: Detail Instance Poisson Disk Spacing**
    - Verify no two generated instances are closer than the minimum Poisson disk radius
    - **Validates: Requirements 8.4**

- [x] 11. Terrain Engine — TerrainDeformationSystem
  - [x] 11.1 Create `src/Game/World/TerrainDeformationSystem.cs`
    - Define `TerrainDeformation` struct (Position, Radius, Depth, Intensity, Type, BirthTime)
    - Pre-allocate 256-slot ring buffer at initialization — zero heap allocations during gameplay
    - Implement `CreateCrater(worldPosition, radius, depth)` — displace vertices downward with cosine-bell falloff
    - Implement `ScorchTerrain(worldPosition, radius)` — darken/desaturate albedo, increase roughness
    - Implement `AddVehicleTrack(from, to, width)` — darkened strip with slight displacement
    - Implement `_Process(delta)` — fade deformations using intensity = 1.0 - age/120.0
    - Overwrite oldest deformation when ring buffer is full
    - Wire to EventBus signals: AttackImpact, SuperweaponFired, fire weapon impacts, vehicle movement
    - Never affect simulation grid, pathfinding costs, or unit movement (visual-only)
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 9.8_

  - [ ]* 11.2 Write property test: Deformation Visual-Only Guarantee (Property 12)
    - **Property 12: Deformation Visual-Only Guarantee**
    - Apply deformations and verify TerrainGrid values, pathfinding costs, and movement logic are unchanged
    - **Validates: Requirements 9.7**

  - [ ]* 11.3 Write property test: Deformation Ring Buffer Safety (Property 13)
    - **Property 13: Deformation Ring Buffer Safety**
    - Apply > 256 deformations, verify buffer stays at 256 slots, no heap allocations, oldest overwritten
    - **Validates: Requirements 9.6, 9.8**

  - [ ]* 11.4 Write property test: Deformation Fade Formula (Property 14)
    - **Property 14: Deformation Fade Formula**
    - For deformations at various ages, verify intensity = max(0, 1.0 - age/120.0)
    - **Validates: Requirements 9.5**

- [x] 12. Terrain Engine — CliffGenerator
  - [x] 12.1 Create `src/Game/World/CliffGenerator.cs`
    - Detect cliff edges where slope exceeds 55°
    - Generate vertical cliff face mesh with horizontal strata layers
    - Generate overhang geometry at position-seeded random intervals
    - Scatter 2–5 boulder meshes (procedural icosphere + noise displacement) at cliff base via MultiMeshInstance3D
    - Adapt rock colors/strata by biome (temperate limestone, desert sandstone, mountain granite, coastal chalk, tropical basalt)
    - Scale detail by quality tier (Potato: 2 subdivisions, no strata/overhangs/boulders; High: 16 subdivisions, 8 strata, detailed overhangs, 5 boulders)
    - Use position-seeded noise for determinism (no System.Random)
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7, 10.8_

  - [x] 12.2 Create `src/Game/World/Shaders/cliff_face.gdshader`
    - Horizontal rock strata with per-layer color variation
    - Procedural cracks along layer boundaries
    - Weathering darkening at exposed edges
    - Moss/lichen on sheltered overhangs
    - Roughness variation (smooth worn vs rough fractures)
    - Compatible with gl_compatibility renderer
    - _Requirements: 10.2, 10.5_

  - [ ]* 12.3 Write property test: Cliff Generation Determinism (Property 15)
    - **Property 15: Cliff Generation Determinism**
    - Run Generate with same elevation map and biome multiple times, verify identical geometry output
    - **Validates: Requirements 10.6, 10.3**

  - [ ]* 12.4 Write property test: Cliff Boulder Count Bounds (Property 16)
    - **Property 16: Cliff Boulder Count Bounds**
    - At High quality tier, verify each cliff face has 2–5 boulders at its base
    - **Validates: Requirements 10.4**

- [x] 13. Terrain Engine — TerrainLODController
  - [x] 13.1 Create `src/Game/World/TerrainLODController.cs`
    - Reduce subdivision level for chunks based on distance from RTSCamera
    - Blend smoothly between LOD levels to avoid visible popping
    - Maintain C1 continuity at boundaries between chunks at different LOD levels
    - _Requirements: 12.1, 12.2, 12.3_

- [x] 14. Terrain Engine — TerrainEngine orchestrator
  - [x] 14.1 Create `src/Game/World/TerrainEngine.cs`
    - Orchestrate the full pipeline: elevation build → erosion → subdivision → mesh gen → material → detail pass → cliff gen → LOD
    - Accept QualityTier in constructor, configure all sub-components accordingly
    - Implement `Generate(MapData, Node3D parent)` — full terrain generation
    - Implement `GetElevationAtWorld(float worldX, float worldZ)` — delegate to TerrainSubdivision
    - Implement `RebuildChunk(int chunkX, int chunkY)` — for dynamic terrain changes
    - Ensure Potato tier produces output identical to current TerrainRenderer (1× subdivision, no erosion, 2-layer flat shading, no detail, no cliff geometry)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 11.1, 11.2, 11.3_

  - [ ]* 14.2 Write property test: Potato Tier Backward Compatibility (Property 22)
    - **Property 22: Potato Tier Backward Compatibility**
    - For random MapData, verify TerrainEngine at Potato produces mesh vertex positions identical to current TerrainRenderer
    - **Validates: Requirements 11.1, 11.2**

- [x] 15. Checkpoint — Terrain engine complete
  - Ensure all tests pass, ask the user if questions arise.

- [x] 16. GameSession Integration Tests — Harness
  - [x] 16.1 Create `tests/CorditeWars.Tests/Integration/GameSessionHarness.cs`
    - Initialize TerrainGrid, PathRequestManager, UnitInteractionSystem without Godot scene tree
    - Implement `ProcessTick()` — advance CurrentTick by 1, process commands, run pathfinding (up to 4 paths), apply combat resolution, evaluate win conditions
    - Implement `AdvanceTicks(int count)` — call ProcessTick exactly N times
    - Implement `InjectCommand(ICommand, ulong targetTick)` — queue command for specified tick
    - Implement `SpawnUnit(string unitTypeId, int playerId, int x, int y)` — create unit, return unique ID
    - Implement `EndMatch(int winnerId, string reason)` — transition to MatchState.Ended (reject if already ended)
    - Implement IDisposable for resource cleanup
    - Use FixedPoint arithmetic exclusively (no float/double in simulation)
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 14.3, 15.1, 15.2, 15.3_

- [x] 17. GameSession Integration Tests — Lifecycle tests
  - [x] 17.1 Create `tests/CorditeWars.Tests/Integration/GameSessionLifecycleTests.cs`
    - Test: harness initializes in MatchState.Playing with valid MatchConfig
    - Test: EndMatch transitions to MatchState.Ended exactly once
    - Test: double EndMatch call is rejected (no double-end)
    - Test: AdvanceTicks(N) calls ProcessTick exactly N times
    - Test: SpawnUnit returns unique IDs for multiple spawns
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 13.4_

  - [ ]* 17.2 Write property test: Tick Advancement Correctness (Property 19)
    - **Property 19: Tick Advancement Correctness**
    - For random initial tick and count N, verify CurrentTick increases by exactly N
    - **Validates: Requirements 13.2, 15.4**

  - [ ]* 17.3 Write property test: Unit Spawn ID Uniqueness (Property 21)
    - **Property 21: Unit Spawn ID Uniqueness**
    - Spawn many units, verify all returned IDs are distinct
    - **Validates: Requirements 13.4**

- [x] 18. GameSession Integration Tests — Determinism tests
  - [x] 18.1 Create `tests/CorditeWars.Tests/Integration/GameSessionDeterminismTests.cs`
    - Test: same 100-tick scenario executed twice produces identical StateChecksum at every tick
    - Test: same MatchConfig and command sequence produces bit-identical unit positions and health
    - Test: pathfinding results are deterministic within the harness
    - _Requirements: 14.1, 14.2, 14.3_

  - [ ]* 18.2 Write property test: Integration Test Tick-Level Determinism (Property 18)
    - **Property 18: Integration Test Tick-Level Determinism**
    - For random MatchConfig and command sequences, verify identical StateChecksum at every tick across runs
    - **Validates: Requirements 14.1, 14.2**

  - [ ]* 18.3 Write property test: Command Scheduling Correctness (Property 20)
    - **Property 20: Command Scheduling Correctness**
    - Inject commands at various target ticks, verify each is processed exactly at its target tick
    - **Validates: Requirements 13.3**

- [x] 19. GameSession Integration Tests — Command processing tests
  - [x] 19.1 Create `tests/CorditeWars.Tests/Integration/GameSessionCommandTests.cs`
    - Test: move command causes unit position to change over ticks
    - Test: attack command triggers combat resolution
    - Test: win condition (destroy HQ) ends match correctly
    - Test: 50+ queued path requests are processed within expected tick budget
    - _Requirements: 13.2, 13.3, 14.1, 15.2_

- [x] 20. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The branding fix (task 1) is independent and can be completed at any time
- Terrain engine tasks (7–14) are ordered by dependency: erosion → subdivision → shader → detail → deformation → cliffs → LOD → orchestrator
- HPA* tasks (2–5) build bottom-up: ClusterGrid → AbstractGraph → HierarchicalPathfinder → PathRequestManager integration
