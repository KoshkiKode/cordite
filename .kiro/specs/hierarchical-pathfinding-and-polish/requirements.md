# Requirements Document

## Introduction

This document specifies the requirements for the "Hierarchical Pathfinding and Polish" feature bundle for Cordite Wars: Six Fronts. The feature adds four improvements: a two-level hierarchical A* pathfinding system (HPA*) for efficient long-distance routing on maps up to 512×512, a AAA-grade procedural terrain rendering engine with erosion simulation and PBR materials, a headless integration test harness for full match lifecycle validation, and a branding text correction.

All components operate within the existing Godot 4.6 / C# / gl_compatibility / FixedPoint deterministic simulation architecture.

## Glossary

- **HPA_Star**: Hierarchical Pathfinding A* — a two-level pathfinding algorithm that partitions the grid into clusters, precomputes an abstract graph of border crossings, and resolves long paths on the abstract graph before refining locally.
- **ClusterGrid**: The component that partitions the TerrainGrid into fixed-size (16×16) clusters and identifies entrance nodes between adjacent clusters.
- **AbstractGraph**: The high-level graph of entrance nodes and weighted edges (intra-cluster A* distances) used for fast cross-map pathfinding.
- **HierarchicalPathfinder**: The orchestrator that decides between direct A* (same-cluster) and hierarchical decomposition (cross-cluster), manages temporary nodes, and concatenates refined path segments.
- **EntranceNode**: A border crossing point between two adjacent clusters, placed at traversable cells along the shared border.
- **TerrainEngine**: The AAA procedural terrain rendering orchestrator that coordinates subdivision, erosion, material blending, detail instancing, and LOD.
- **ErosionSimulator**: The component that applies hydraulic and thermal erosion to the elevation map before mesh generation.
- **TerrainMaterialSystem**: The 5-layer PBR uber-shader that procedurally blends materials based on elevation, slope, moisture, and noise.
- **TerrainDetailPass**: The component that scatters instanced grass, pebbles, and debris via MultiMesh.
- **TerrainDeformationSystem**: The visual-only system that renders craters, scorch marks, and vehicle tracks from combat events, fading over time.
- **CliffGenerator**: The component that generates procedural cliff face geometry with rock strata, overhangs, and boulder scatter where slopes exceed 55°.
- **TerrainLODController**: The component that adjusts subdivision level based on camera distance.
- **GameSessionHarness**: A headless, Godot-free wrapper around simulation systems for integration testing.
- **QualityTier**: One of Potato, Low, Medium, or High — controls rendering fidelity.
- **FixedPoint**: The project's deterministic fixed-point arithmetic type used for all simulation math.
- **MovementProfile**: Defines traversability rules (terrain types, slope limits, footprint size) for a unit class.
- **MatchConfig**: Configuration object specifying map, players, seed, and win conditions for a game session.

## Requirements

### Requirement 1: Cluster Grid Partitioning

**User Story:** As a pathfinding system, I want the map divided into fixed-size clusters with identified border crossings, so that long-distance paths can be resolved on a smaller abstract graph.

#### Acceptance Criteria

1. WHEN a TerrainGrid is provided, THE ClusterGrid SHALL partition it into 16×16-cell clusters, rounding up for grids not evenly divisible by 16.
2. WHEN two adjacent clusters share a border, THE ClusterGrid SHALL identify contiguous runs of mutually-traversable border cells as entrance candidates.
3. WHEN a contiguous traversable run has length 3 or fewer, THE ClusterGrid SHALL place one EntranceNode at the midpoint of the run.
4. WHEN a contiguous traversable run has length greater than 3, THE ClusterGrid SHALL place two EntranceNodes at the endpoints of the run.
5. WHEN a cell coordinate is provided, THE ClusterGrid SHALL return the correct cluster index (cx, cy) for that cell.
6. IF a grid dimension is not evenly divisible by the cluster size, THEN THE ClusterGrid SHALL handle the smaller boundary clusters by clamping border scans to actual grid bounds.

### Requirement 2: Abstract Graph Construction

**User Story:** As a pathfinding system, I want a precomputed abstract graph of entrance nodes with weighted edges, so that cross-map searches operate on a small node set instead of the full grid.

#### Acceptance Criteria

1. WHEN Preprocess is called, THE AbstractGraph SHALL create a node for each EntranceNode identified by the ClusterGrid.
2. WHEN two EntranceNodes exist within the same cluster, THE AbstractGraph SHALL compute an intra-cluster edge with cost equal to the A* distance between them confined to that cluster, using FixedPoint arithmetic.
3. WHEN two EntranceNodes are on opposite sides of the same border, THE AbstractGraph SHALL create an inter-cluster edge with a crossing cost of FixedPoint.One.
4. WHEN InsertTemporaryNode is called with a grid position, THE AbstractGraph SHALL connect the temporary node to all entrance nodes in its cluster via intra-cluster A* distances.
5. WHEN RemoveTemporaryNodes is called, THE AbstractGraph SHALL remove all temporary nodes and their edges added since the last Build call.
6. WHEN Search is called with valid start and goal node indices, THE AbstractGraph SHALL return the optimal path on the abstract graph using A* with FixedPoint costs.
7. THE AbstractGraph SHALL use SortedList for edge storage to guarantee deterministic iteration order.

### Requirement 3: Hierarchical Path Resolution

**User Story:** As a unit requesting a path, I want the pathfinder to efficiently resolve long-distance routes using hierarchical decomposition, so that cross-map paths complete within acceptable time budgets.

#### Acceptance Criteria

1. WHEN start and goal are in the same cluster, THE HierarchicalPathfinder SHALL delegate directly to AStarPathfinder with maxNodes=512 without using the abstract graph.
2. WHEN start and goal are in different clusters, THE HierarchicalPathfinder SHALL insert temporary nodes, search the abstract graph, then refine each segment with local A*.
3. WHEN the abstract graph search returns an empty path, THE HierarchicalPathfinder SHALL return an empty path without attempting segment refinement.
4. WHEN segment refinement between two abstract nodes fails, THE HierarchicalPathfinder SHALL return an empty path and mark affected clusters as dirty.
5. THE HierarchicalPathfinder SHALL concatenate refined segments into a single contiguous path, removing duplicate junction nodes at segment boundaries.
6. THE HierarchicalPathfinder SHALL remove all temporary nodes from the abstract graph before returning, regardless of success or failure.
7. WHEN InvalidateCell is called, THE HierarchicalPathfinder SHALL mark the containing cluster and adjacent clusters sharing a border with the affected cell as dirty.
8. WHEN RebuildInvalidated is called, THE HierarchicalPathfinder SHALL rebuild only the dirty clusters and their associated abstract graph edges.

### Requirement 4: Pathfinding Correctness Invariants

**User Story:** As a game developer, I want guarantees that hierarchical pathfinding produces valid, deterministic results, so that multiplayer lockstep simulation remains correct.

#### Acceptance Criteria

1. THE HierarchicalPathfinder SHALL produce paths where every consecutive pair of cells has Chebyshev distance of exactly 1 and both cells are traversable for the given MovementProfile.
2. IF AStarPathfinder returns a non-empty path between two cells, THEN THE HierarchicalPathfinder SHALL also return a non-empty path between those cells (completeness guarantee).
3. THE HierarchicalPathfinder SHALL return bit-identical paths for identical inputs (same grid state, same MovementProfile, same start and goal) across all invocations and platforms.
4. THE AbstractGraph SHALL produce bit-identical search results for identical inputs across all invocations and platforms.
5. WHEN an EntranceNode is created, THE ClusterGrid SHALL verify that the cell is traversable from both adjacent clusters for the preprocessed MovementProfile.

### Requirement 5: Terrain Mesh Subdivision

**User Story:** As a player, I want smooth, high-fidelity terrain geometry that eliminates visible polygon faceting, so that the game looks visually polished.

#### Acceptance Criteria

1. WHEN QualityTier is Potato, THE TerrainEngine SHALL use 1× subdivision (identical vertex density to the current TerrainRenderer).
2. WHEN QualityTier is Low, THE TerrainEngine SHALL use 2× subdivision per grid cell edge.
3. WHEN QualityTier is Medium, THE TerrainEngine SHALL use 4× subdivision per grid cell edge.
4. WHEN QualityTier is High, THE TerrainEngine SHALL use 8× subdivision per grid cell edge.
5. THE TerrainEngine SHALL interpolate elevation at subdivision vertices using Catmull-Rom bicubic interpolation.
6. THE TerrainEngine SHALL produce a mesh that is C1 continuous across chunk boundaries and cell boundaries with no visible seams, cracks, or T-junctions.
7. THE TerrainEngine SHALL provide a GetElevationAtWorld method that returns interpolated elevation using the same Catmull-Rom spline as the mesh, within 0.01 units of the actual mesh vertex elevation.

### Requirement 6: Erosion Simulation

**User Story:** As a player, I want terrain that looks naturally weathered with gullies and sediment deposits, so that maps feel realistic rather than artificially flat.

#### Acceptance Criteria

1. WHEN QualityTier is Medium, THE ErosionSimulator SHALL apply 3 passes of hydraulic erosion (50,000 droplets per pass) to the elevation map.
2. WHEN QualityTier is High, THE ErosionSimulator SHALL apply 8 passes of hydraulic erosion (200,000 droplets total) to the elevation map.
3. WHEN QualityTier is Potato or Low, THE ErosionSimulator SHALL not be invoked (zero erosion passes).
4. THE ErosionSimulator SHALL produce bit-identical results given the same elevation map and seed across all platforms and invocations.
5. THE ErosionSimulator SHALL apply thermal erosion after hydraulic erosion, redistributing material from slopes exceeding the talus angle threshold to adjacent lower cells.
6. THE ErosionSimulator SHALL operate on the base grid resolution before subdivision is applied.

### Requirement 7: Procedural PBR Material System

**User Story:** As a player, I want rich, varied terrain surfaces with natural material transitions, so that the ground looks convincing without requiring external texture files.

#### Acceptance Criteria

1. THE TerrainMaterialSystem SHALL blend up to 5 material layers (bedrock, rocky soil, grass/vegetation, sand/dirt, river sediment) based on elevation, slope, moisture, and noise.
2. THE TerrainMaterialSystem SHALL generate all surface detail procedurally in the shader with no external texture files.
3. WHEN terrain slope exceeds 45 degrees, THE TerrainMaterialSystem SHALL use triplanar projection to prevent texture stretching.
4. WHEN terrain slope is below 45 degrees, THE TerrainMaterialSystem SHALL use standard UV projection.
5. THE TerrainMaterialSystem SHALL blend smoothly between triplanar and standard projection at the transition angle.
6. WHEN QualityTier is Potato, THE TerrainMaterialSystem SHALL use 2 shader layers with flat shading.
7. WHEN QualityTier is Low, THE TerrainMaterialSystem SHALL use 3 shader layers.
8. WHEN QualityTier is Medium or High, THE TerrainMaterialSystem SHALL use 5 shader layers with triplanar blending.
9. THE TerrainMaterialSystem SHALL accept per-vertex biome hints via vertex color channels to adapt layer weights per map biome.
10. THE TerrainMaterialSystem SHALL be compatible with Godot's gl_compatibility renderer (no tessellation or compute shaders).

### Requirement 8: Terrain Detail Instancing

**User Story:** As a player, I want grass, pebbles, and debris scattered across the terrain, so that the ground has micro-detail and visual richness.

#### Acceptance Criteria

1. WHEN QualityTier is Medium, THE TerrainDetailPass SHALL scatter sparse detail instances (grass, pebbles) using MultiMeshInstance3D.
2. WHEN QualityTier is High, THE TerrainDetailPass SHALL scatter dense detail instances using MultiMeshInstance3D.
3. WHEN QualityTier is Potato or Low, THE TerrainDetailPass SHALL not generate any detail instances.
4. THE TerrainDetailPass SHALL use Poisson disk sampling for natural-looking distribution of detail instances.
5. THE TerrainDetailPass SHALL apply LOD fade, removing detail instances beyond 80 units from the camera.

### Requirement 9: Terrain Combat Deformation

**User Story:** As a player, I want explosions and combat to leave visible marks on the terrain (craters, scorch, tracks), so that battles feel impactful and the battlefield tells a story.

#### Acceptance Criteria

1. WHEN an AttackImpact event fires, THE TerrainDeformationSystem SHALL create a crater by displacing mesh vertices downward in a radius using a smooth cosine-bell falloff.
2. WHEN a SuperweaponFired event fires, THE TerrainDeformationSystem SHALL create a large crater at the impact position.
3. WHEN a fire weapon impacts terrain, THE TerrainDeformationSystem SHALL scorch the terrain by darkening and desaturating albedo and increasing roughness in a radius.
4. WHEN a vehicle moves, THE TerrainDeformationSystem SHALL add track segments between consecutive positions as darkened strips with slight displacement.
5. THE TerrainDeformationSystem SHALL fade all deformations over 120 seconds by multiplying intensity by (1.0 - age/FadeDuration).
6. WHEN the deformation ring buffer is full (256 slots), THE TerrainDeformationSystem SHALL overwrite the oldest deformation without heap allocation.
7. THE TerrainDeformationSystem SHALL never affect the simulation grid, pathfinding costs, or unit movement logic (visual-only guarantee).
8. THE TerrainDeformationSystem SHALL pre-allocate all 256 deformation slots at initialization and perform zero heap allocations during gameplay.

### Requirement 10: Procedural Cliff Faces and Rock Formations

**User Story:** As a player, I want steep terrain to display detailed cliff faces with visible rock strata and scattered boulders, so that elevation changes look dramatic and natural.

#### Acceptance Criteria

1. WHEN terrain slope exceeds 55 degrees, THE CliffGenerator SHALL generate cliff face geometry at that location.
2. THE CliffGenerator SHALL produce layered horizontal rock strata visible in cliff cross-sections, with per-layer color variation.
3. THE CliffGenerator SHALL generate overhang geometry at position-seeded random intervals along cliff faces.
4. THE CliffGenerator SHALL scatter 2–5 boulder meshes (procedural icosphere with noise displacement) at the base of each cliff face using MultiMeshInstance3D.
5. THE CliffGenerator SHALL adapt rock colors and strata patterns based on the map biome (temperate limestone, desert sandstone, mountain granite, coastal chalk, tropical basalt).
6. THE CliffGenerator SHALL produce identical geometry given the same elevation map and biome across all platforms (position-seeded noise, no System.Random).
7. WHEN QualityTier is Potato, THE CliffGenerator SHALL use minimal cliff face subdivisions (2), no strata layers, no overhangs, and no boulders.
8. WHEN QualityTier is High, THE CliffGenerator SHALL use 16 cliff face subdivisions, 8 strata layers, detailed overhangs, and 5 boulders per cliff.

### Requirement 11: Terrain Quality Tier Backward Compatibility

**User Story:** As a player on low-end hardware, I want the Potato quality tier to produce output identical to the current terrain renderer, so that performance is not regressed.

#### Acceptance Criteria

1. WHEN QualityTier is Potato, THE TerrainEngine SHALL produce visually identical output to the current TerrainRenderer (1× subdivision, no erosion, 2-layer flat shading, no detail instances, no cliff geometry beyond flat color).
2. WHEN QualityTier is Potato, THE TerrainEngine SHALL not invoke ErosionSimulator, TerrainDetailPass, or advanced CliffGenerator features.
3. THE TerrainEngine SHALL scale rendering cost proportionally to quality tier without degrading Potato tier performance.

### Requirement 12: Terrain LOD System

**User Story:** As a player on large maps, I want distant terrain to use reduced detail, so that frame rate remains stable regardless of map size.

#### Acceptance Criteria

1. THE TerrainLODController SHALL reduce subdivision level for terrain chunks based on distance from the RTSCamera.
2. WHEN a chunk transitions between LOD levels, THE TerrainLODController SHALL blend smoothly to avoid visible popping.
3. THE TerrainLODController SHALL maintain C1 continuity at boundaries between chunks at different LOD levels.

### Requirement 13: GameSession Integration Test Harness

**User Story:** As a developer, I want a headless test harness that exercises the full match lifecycle without requiring Godot runtime, so that simulation correctness can be validated in CI.

#### Acceptance Criteria

1. THE GameSessionHarness SHALL initialize all simulation systems (TerrainGrid, PathRequestManager, UnitInteractionSystem) without requiring the Godot scene tree.
2. WHEN ProcessTick is called, THE GameSessionHarness SHALL advance CurrentTick by 1, process all commands scheduled for that tick, run pathfinding (up to 4 paths), apply combat resolution, and evaluate win conditions.
3. WHEN InjectCommand is called with a command and target tick, THE GameSessionHarness SHALL queue the command for processing at the specified tick.
4. WHEN SpawnUnit is called, THE GameSessionHarness SHALL create a unit of the specified type at the given position for the specified player and return its unique ID.
5. WHEN EndMatch is called, THE GameSessionHarness SHALL transition CurrentState to MatchState.Ended.
6. THE GameSessionHarness SHALL implement IDisposable and release all resources on disposal.

### Requirement 14: Integration Test Determinism

**User Story:** As a developer, I want integration tests to produce bit-identical results across runs, so that flaky tests do not undermine confidence in the simulation.

#### Acceptance Criteria

1. THE GameSessionHarness SHALL produce bit-identical simulation state (unit positions, health, match outcome) for identical MatchConfig and command sequences across all runs and platforms.
2. WHEN the same 100-tick scenario is executed twice with the same seed, THE GameSessionHarness SHALL produce identical StateChecksum values at every tick.
3. THE GameSessionHarness SHALL use FixedPoint arithmetic exclusively for all simulation calculations (no float or double).

### Requirement 15: Integration Test Match Lifecycle

**User Story:** As a developer, I want to validate that matches transition correctly through their lifecycle states, so that session management bugs are caught early.

#### Acceptance Criteria

1. WHEN a GameSessionHarness is created with a valid MatchConfig, THE GameSessionHarness SHALL initialize in MatchState.Playing.
2. WHEN EndMatch is called with a winner ID and reason, THE GameSessionHarness SHALL transition to MatchState.Ended exactly once.
3. IF EndMatch is called when CurrentState is already MatchState.Ended, THEN THE GameSessionHarness SHALL reject the call (no double-end).
4. WHEN AdvanceTicks is called with count N, THE GameSessionHarness SHALL call ProcessTick exactly N times in sequence.

### Requirement 16: Branding Text Correction

**User Story:** As a player, I want to see the correct KoshkiKode tagline on the branding screen, so that the game presents a polished, professional identity.

#### Acceptance Criteria

1. THE KoshkiKodeBrandingScreen SHALL display "Precision-Crafted Warfare" as the subtitle text.
2. THE KoshkiKodeBrandingScreen SHALL not display "BRANDING PLACEHOLDER" in any build configuration.
