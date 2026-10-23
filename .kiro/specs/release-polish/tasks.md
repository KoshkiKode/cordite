# Tasks: Release Polish

## Task 1: Unit Death/Destruction VFX

- [x] 1.1 Create `DeathVFXController` class in `src/Game/VFX/DeathVFXController.cs` with Initialize method that subscribes to EventBus.UnitDeath and EventBus.BuildingDestroyed signals
- [ ] 1.2 Implement `GetDeathVFXScale()` mapping that returns explosion type, debris count, wreckage duration, and smoke intensity based on UnitCategory (Infantry=small puff only, LightVehicle=medium+3 debris, HeavyVehicle=large+6 debris)
- [ ] 1.3 Implement `SpawnUnitDebris()` that creates simple mesh fragments (BoxMesh/SphereMesh) with random velocity and gravity, scaled by QualityManager.ParticleMultiplier
- [ ] 1.4 Implement `SpawnUnitWreckage()` that creates a darkened/damaged copy mesh at the death position with a configurable lifetime timer and fade-out tween
- [ ] 1.5 Implement `SpawnBuildingCollapse()` that triggers a scale-to-zero tween over 1 second, spawns rubble mesh at footprint, smoke column (10s), and fire particles (6s)
- [ ] 1.6 Implement large building staggered explosions (3 explosions over 0.5s for footprint > 2×2)
- [ ] 1.7 Implement wreckage/rubble lifetime management with periodic sweep timer (every 2s) and fade-out tween before QueueFree
- [ ] 1.8 Add Potato quality tier scaling: halve debris count, shorten wreckage lifetime by 50%
- [ ] 1.9 Wire DeathVFXController into GameSession setup (after CombatVFXBridge initialization)
- [ ] 1.10 Add new VFXEffectType entries if needed (FireEffect, SmokeColumn) and corresponding ParticleFactory methods

## Task 2: Projectile Visuals

- [ ] 2.1 Create `ProjectileVisualizer` class in `src/Game/VFX/ProjectileVisualizer.cs` with Initialize method subscribing to EventBus.AttackFired
- [ ] 2.2 Create `ProjectileConfig` record and `ProjectileVisualType` enum in `src/Game/VFX/ProjectileConfig.cs`
- [ ] 2.3 Implement WeaponType → ProjectileVisualType mapping function covering all WeaponType enum values
- [ ] 2.4 Implement `CreateTracerProjectile()` — thin bright quad mesh, linear lerp at ≥50 units/s, no trail, auto-free on arrival
- [ ] 2.5 Implement `CreateMissileProjectile()` — small mesh with attached GpuParticles3D smoke trail, moderate speed, guided toward target
- [ ] 2.6 Implement `CreateArcingShellProjectile()` — mesh following quadratic bezier arc (ComputeArcPosition algorithm), configurable arc height
- [ ] 2.7 Implement `CreateBeamProjectile()` — instant line (MeshInstance3D with thin cylinder or ImmediateMesh) between origin and target, fades after 0.1s
- [ ] 2.8 Implement projectile pooling for tracers (pool size 64) to avoid allocation pressure from high-fire-rate weapons
- [ ] 2.9 Implement target resolution: look up target unit's current visual position; fall back to last known position if unit is freed
- [ ] 2.10 Implement maximum lifetime safety (5s) — any projectile that hasn't arrived self-destructs
- [ ] 2.11 Ensure all projectile nodes have no collision layer (CollisionLayer = 0, CollisionMask = 0) and no physics body
- [ ] 2.12 Add Potato quality tier: disable trail particles on missiles, keep mesh only
- [ ] 2.13 Wire ProjectileVisualizer into GameSession setup

## Task 3: Production Queue UI Enhancement

- [ ] 3.1 Enhance `ProductionQueueDisplay` (or create new `ProductionQueuePanel`) to show current item name, progress bar, and ETA countdown in seconds
- [ ] 3.2 Implement ETA calculation: (buildTime - currentProgress) converted to seconds using tick rate (30 tps)
- [ ] 3.3 Add cancel button for the currently producing item that calls ProductionQueue.CancelCurrent()
- [ ] 3.4 Enhance queue icon row to show all queued items with individual cancel buttons
- [ ] 3.5 Implement multi-building selection handling: display first selected production building's queue
- [ ] 3.6 Implement auto-hide logic: panel hidden when no production building selected or queue empty and not producing
- [ ] 3.7 Style panel to match existing HUD (dark panel bg, accent #4A9ECC, border color, corner radius, font sizes 12-14px)

## Task 4: Performance Profiling Overlay

- [ ] 4.1 Create `PerformanceOverlay` class in `src/UI/HUD/PerformanceOverlay.cs` as a CanvasLayer (Layer 50, same as DebugOverlay)
- [ ] 4.2 Implement F4 toggle input action in project.godot and wire to PerformanceOverlay.Toggle()
- [ ] 4.3 Implement metric collection: FPS via Engine.GetFramesPerSecond(), frame time via delta smoothing (EMA factor 0.1)
- [ ] 4.4 Implement draw call metric via RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame)
- [ ] 4.5 Implement terrain triangle count metric (query from TerrainEngine or cache at generation time)
- [ ] 4.6 Implement active unit count metric from GameSession/UnitSpawner
- [ ] 4.7 Implement pathfinding budget usage metric from PathfindingSystem (percentage of frame budget consumed)
- [ ] 4.8 Implement memory usage metric via OS.GetStaticMemoryUsage() converted to MB
- [ ] 4.9 Implement "—" fallback display for unavailable metrics (null system references)
- [ ] 4.10 Skip all metric collection when overlay is not visible (zero overhead when hidden)
- [ ] 4.11 Style overlay with semi-transparent dark background, monospace font, left-aligned single column
- [ ] 4.12 Wire PerformanceOverlay into GameSession (created alongside DebugOverlay)

## Task 5: Terrain Generation Benchmark

- [ ] 5.1 Create `TerrainBenchmark` static class in `src/Game/World/TerrainBenchmark.cs`
- [ ] 5.2 Create `BenchmarkResult` record with timing fields for each pipeline stage plus triangle/vertex counts
- [ ] 5.3 Implement RunBenchmark() that creates a TerrainEngine, instruments each Generate() stage with Stopwatch timing
- [ ] 5.4 Log structured results via GD.Print (stage name, milliseconds, total, triangle count, vertex count)
- [ ] 5.5 Add a way to trigger the benchmark (e.g., console command, debug menu button, or F5 key in debug builds)

## Task 6: LAN Multiplayer Validation — LoopbackTransport

- [ ] 6.1 Create `LoopbackTransport` class in `tests/Networking/LoopbackTransport.cs` implementing the same event interface as NetworkTransport
- [ ] 6.2 Implement CreatePair() static method returning two connected LoopbackTransport instances
- [ ] 6.3 Implement BroadcastCommand/SendCommand that synchronously invokes the paired transport's CommandReceived event
- [ ] 6.4 Implement BroadcastChecksum/SendChecksum that synchronously invokes the paired transport's ChecksumReceived event
- [ ] 6.5 Implement SimulateConnect() that fires PeerConnected/ConnectedToHost events on both sides
- [ ] 6.6 Implement SimulateDisconnect() that fires PeerDisconnected on the paired transport

## Task 7: LAN Multiplayer Validation — Test Suite

- [ ] 7.1 Create test project structure (if not existing) with NUnit references for `tests/Networking/LockstepIntegrationTests.cs`
- [ ] 7.2 Write test: LobbyCreation_HostAndClientConnect — verify PeerConnected fires on both sides via LoopbackTransport
- [ ] 7.3 Write test: CommandSynchronization_HostCommandReceivedByClient — submit command on host, verify client receives at correct scheduled tick
- [ ] 7.4 Write test: CommandSynchronization_ClientCommandReceivedByHost — submit command on client, verify host receives at correct scheduled tick
- [ ] 7.5 Write test: TickAdvancementGating_BlockedUntilAllConfirm — verify CanAdvanceTick returns false until all players confirm, then true
- [ ] 7.6 Write test: ChecksumExchange_MatchingChecksums_NoDesync — submit identical checksums from both peers, verify no DesyncDetected
- [ ] 7.7 Write test: ChecksumExchange_MismatchedChecksums_DesyncDetected — submit different checksums, verify DesyncDetected signal fires
- [ ] 7.8 Write test: GracefulDisconnect_PeerDrops_NoException — simulate disconnect mid-match, verify no crash and PeerDisconnected fires
- [ ] 7.9 Write test: MultipleCommands_DeterministicOrdering — submit commands from multiple players, verify GetCommandsForTick returns them in deterministic order (PlayerId → CommandType → InsertionOrder)
