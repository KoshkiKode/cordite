# Requirements: Release Polish

## Functional Requirements

### FR-1: Unit Death/Destruction VFX

- FR-1.1: When a unit dies, the system SHALL spawn explosion particle effects at the unit's world position, scaled by UnitCategory.
- FR-1.2: Infantry death SHALL produce only a small puff (ExplosionSmall + DustCloud) with no persistent debris or wreckage.
- FR-1.3: Light vehicle death (LightVehicle, APC, Support) SHALL produce a medium explosion, 2-4 debris mesh fragments, and wreckage that persists for 8 seconds before fading.
- FR-1.4: Heavy vehicle death (HeavyVehicle, Tank, Artillery, Helicopter, Jet) SHALL produce a large explosion, 4-6 debris mesh fragments, and wreckage that persists for 12 seconds before fading.
- FR-1.5: Building destruction SHALL produce a collapse animation (model scales to zero over 1 second), rubble mesh at the footprint, a smoke column lasting 10 seconds, and fire particles lasting 6 seconds.
- FR-1.6: Large buildings (footprint > 2×2) SHALL produce multiple staggered explosions (3 explosions over 0.5 seconds) and more rubble than small buildings.
- FR-1.7: All death VFX particle counts SHALL be scaled by QualityManager.ParticleMultiplier.
- FR-1.8: Wreckage and rubble nodes SHALL auto-free after their configured lifetime via a fade-out tween.
- FR-1.9: Death VFX SHALL never modify simulation state (unit positions, health, RNG, or any FixedPoint data).

### FR-2: Projectile Visuals

- FR-2.1: When EventBus.AttackFired is emitted, the system SHALL spawn a visual projectile node at the attacker's position.
- FR-2.2: Bullet-type weapons (MachineGun, Autocannon, AntiAir) SHALL display as bright tracer lines moving at high speed (≥50 units/s).
- FR-2.3: Missile-type weapons (Missile, Rockets, SAM, Torpedo) SHALL display as a mesh with an attached smoke trail (GpuParticles3D), moving at moderate speed.
- FR-2.4: Artillery-type weapons (Cannon, Artillery, Mortar, NavalGun) SHALL display as arcing shells following a quadratic bezier curve with configurable arc height.
- FR-2.5: Beam-type weapons (Laser, RailGun) SHALL display as an instant line between origin and target (no travel time).
- FR-2.6: All projectile nodes SHALL have no collision layer, no physics body, and no interaction with the simulation.
- FR-2.7: Projectile nodes SHALL self-destruct (QueueFree) upon reaching the target position or after a maximum lifetime of 5 seconds (whichever comes first).
- FR-2.8: If the target unit is already freed when the projectile is created, the projectile SHALL fly toward the last known position and self-destruct on arrival.
- FR-2.9: For high-fire-rate weapons, projectile nodes SHALL be pooled (pool size: 64) to avoid allocation pressure.

### FR-3: Production Queue UI

- FR-3.1: When a production building is selected, the HUD SHALL display a Production Queue Panel showing the current production item name.
- FR-3.2: The panel SHALL display a progress bar showing build completion percentage (0-100%).
- FR-3.3: The panel SHALL display an ETA countdown in seconds (buildTime - currentProgress) / tickRate, updating every frame.
- FR-3.4: The panel SHALL display queue depth as a row of icon buttons, one per queued item (max 5).
- FR-3.5: Each queued item icon SHALL have a cancel button that calls ProductionQueue.RemoveFromQueue(index).
- FR-3.6: The panel SHALL have a cancel button for the currently producing item that calls ProductionQueue.CancelCurrent().
- FR-3.7: When no production building is selected or the queue is empty and nothing is producing, the panel SHALL be hidden.
- FR-3.8: The panel SHALL integrate with the existing GameHUD layout, positioned above the command card area.
- FR-3.9: When multiple production buildings are selected, the panel SHALL display the production state of the first selected building.

### FR-4: Performance Profiling Overlay

- FR-4.1: The system SHALL provide a performance overlay toggled by the F4 key (separate from the existing F3 debug overlay).
- FR-4.2: The overlay SHALL display: FPS (frames per second), frame time (milliseconds), draw calls, terrain triangle count, active unit count, pathfinding budget usage (percentage), and memory usage (MB).
- FR-4.3: FPS and frame time SHALL use exponential moving average smoothing (factor 0.1) to avoid jitter.
- FR-4.4: The overlay SHALL have a semi-transparent dark background for readability against any terrain.
- FR-4.5: The overlay SHALL have minimal performance impact (metric collection < 0.1ms per frame).
- FR-4.6: When the overlay is hidden, metric collection SHALL be skipped entirely (zero overhead).
- FR-4.7: Unavailable metrics (system not yet initialized) SHALL display "—" rather than causing errors.

### FR-5: Terrain Generation Benchmark

- FR-5.1: The system SHALL provide a TerrainBenchmark utility that times each terrain generation pipeline stage independently.
- FR-5.2: The benchmark SHALL report timing for: elevation build, erosion, subdivision, mesh generation, material setup, detail pass, cliff generation, and LOD setup.
- FR-5.3: The benchmark SHALL report total triangle count and vertex count for the generated terrain.
- FR-5.4: The benchmark SHALL log results via GD.Print in a structured format.
- FR-5.5: The benchmark SHALL be runnable on demand (not every frame) and SHALL not affect normal gameplay.

### FR-6: LAN Multiplayer Validation

- FR-6.1: The system SHALL provide a LoopbackTransport class that implements the same event interface as NetworkTransport without using real networking.
- FR-6.2: LoopbackTransport.CreatePair() SHALL return two connected transport instances that route packets between each other synchronously.
- FR-6.3: The test suite SHALL validate lobby creation: host creates lobby, client connects, both receive PeerConnected events.
- FR-6.4: The test suite SHALL validate command synchronization: a command submitted on one peer is received by the other peer at the correct scheduled tick.
- FR-6.5: The test suite SHALL validate tick advancement gating: CanAdvanceTick returns false until all peers have confirmed, then returns true.
- FR-6.6: The test suite SHALL validate checksum exchange: matching checksums produce no DesyncDetected signal; mismatched checksums produce exactly one DesyncDetected signal.
- FR-6.7: The test suite SHALL validate graceful disconnect: when a peer disconnects, the remaining peer's PeerDisconnected event fires without crash.
- FR-6.8: The test suite SHALL use LoopbackTransport exclusively (no real network, no timing dependencies, deterministic execution).

## Non-Functional Requirements

### NFR-1: Simulation Integrity

- NFR-1.1: All VFX, projectile visuals, UI panels, and performance overlays SHALL be purely visual/diagnostic — they SHALL NOT read from or write to any FixedPoint simulation state.
- NFR-1.2: The lockstep protocol SHALL remain unchanged. No new network packet types or protocol modifications are introduced by these features.

### NFR-2: Performance

- NFR-2.1: Death VFX and projectile systems SHALL not cause frame drops below 30 FPS on Medium quality tier with 200 active units.
- NFR-2.2: Projectile pooling SHALL prevent GC pressure from high-fire-rate weapons (≥10 shots/second per unit).
- NFR-2.3: Wreckage cleanup SHALL use a periodic sweep (every 2 seconds) rather than per-frame lifetime checks.

### NFR-3: Compatibility

- NFR-3.1: All particle effects SHALL work with the gl_compatibility renderer (no compute shaders, no tessellation).
- NFR-3.2: All new UI elements SHALL follow the existing HUD styling (dark panels, accent color #4A9ECC, font sizes 12-14px).

### NFR-4: Quality Scaling

- NFR-4.1: At Potato quality tier, debris count SHALL be halved and wreckage lifetime shortened by 50%.
- NFR-4.2: At Potato quality tier, projectile trails SHALL be disabled (projectile mesh only, no GpuParticles3D trail).
- NFR-4.3: At High quality tier, all effects SHALL run at full fidelity with no reduction.

### NFR-5: Testability

- NFR-5.1: LoopbackTransport SHALL be compiled only in debug/test configurations (not included in release builds).
- NFR-5.2: All LAN validation tests SHALL complete in under 5 seconds total (no real network delays).
- NFR-5.3: Tests SHALL be deterministic — running the same test twice produces the same result.
