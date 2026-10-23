using System;
using System.Collections.Generic;
using CorditeWars.Core;
using CorditeWars.Game;
using CorditeWars.Game.Campaign;
using CorditeWars.Game.Units;
using CorditeWars.Systems.Networking;
using CorditeWars.Systems.Pathfinding;

namespace CorditeWars.Tests.Integration;

/// <summary>
/// Headless, Godot-free wrapper around simulation systems for integration testing.
/// Tracks units, processes ticks, handles commands, and manages match state
/// without requiring the Godot scene tree.
/// </summary>
public sealed class GameSessionHarness : IDisposable
{
    // ── Public Properties ────────────────────────────────────────────────

    /// <summary>The terrain grid used by this session.</summary>
    public TerrainGrid Grid { get; }

    /// <summary>The path request manager for pathfinding operations.</summary>
    public PathRequestManager PathRequests { get; }

    /// <summary>The unit interaction system for combat resolution.</summary>
    public UnitInteractionSystem UnitInteraction { get; }

    /// <summary>Current simulation tick.</summary>
    public ulong CurrentTick { get; private set; }

    /// <summary>Current match state.</summary>
    public MatchState CurrentState { get; private set; }

    // ── Internal State ───────────────────────────────────────────────────

    private readonly CommandBuffer _commandBuffer = new();
    private readonly Dictionary<int, UnitState> _units = new();
    private readonly MatchConfig _config;
    private int _nextUnitId = 1;
    private bool _disposed;

    /// <summary>
    /// Internal unit state tracked by the harness.
    /// Uses FixedPoint arithmetic exclusively for determinism.
    /// </summary>
    public struct UnitState
    {
        public int UnitId;
        public string UnitTypeId;
        public int PlayerId;
        public FixedPoint X;
        public FixedPoint Y;
        public FixedPoint Health;
        public bool IsAlive;
    }

    // ── Constructor ──────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new GameSessionHarness with the given match configuration.
    /// Initializes all simulation systems without Godot scene tree.
    /// </summary>
    public GameSessionHarness(MatchConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // Initialize terrain grid (32x32 default for tests, cell size 1)
        Grid = new TerrainGrid(32, 32, FixedPoint.One);

        // Initialize pathfinding systems
        PathRequests = new PathRequestManager();

        // Initialize subsystems needed by UnitInteractionSystem
        var spatialHash = new SpatialHash(32, 32, 8);
        var occupancyGrid = new OccupancyGrid(32, 32);
        var collisionResolver = new CollisionResolver();
        var formationManager = new FormationManager();
        var combatResolver = new CombatResolver();
        var combatRng = new DeterministicRng(config.MatchSeed);

        UnitInteraction = new UnitInteractionSystem(
            spatialHash,
            occupancyGrid,
            collisionResolver,
            PathRequests,
            formationManager,
            combatResolver,
            combatRng,
            maxPathsPerTick: 4);

        // Start in Playing state
        CurrentState = MatchState.Playing;
        CurrentTick = 0;
    }

    // ── Public API ───────────────────────────────────────────────────────

    /// <summary>
    /// Advances the simulation by one tick:
    /// 1. Increment CurrentTick
    /// 2. Process commands scheduled for this tick
    /// 3. Run pathfinding (up to 4 paths)
    /// 4. Apply combat resolution (placeholder)
    /// 5. Evaluate win conditions
    /// </summary>
    public void ProcessTick()
    {
        if (CurrentState != MatchState.Playing)
            return;

        CurrentTick++;

        // Process commands for this tick
        var commands = _commandBuffer.GetCommandsForTick(CurrentTick);
        for (int i = 0; i < commands.Count; i++)
        {
            ExecuteCommand(commands[i]);
        }

        // Run pathfinding (up to 4 paths per tick)
        PathRequests.ProcessRequests(Grid, maxPathsPerTick: 4);

        // Move units toward their targets (simplified movement)
        ProcessUnitMovement();
    }

    /// <summary>
    /// Advances the simulation by exactly N ticks.
    /// </summary>
    public void AdvanceTicks(int count)
    {
        for (int i = 0; i < count; i++)
        {
            ProcessTick();
        }
    }

    /// <summary>
    /// Injects a command to be processed on the specified tick.
    /// </summary>
    public void InjectCommand(GameCommand command, ulong targetTick)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        command.ScheduledTick = targetTick;
        _commandBuffer.AddCommand(command);
    }

    /// <summary>
    /// Spawns a unit at the given grid position. Returns a unique unit ID.
    /// Uses FixedPoint arithmetic for position storage.
    /// </summary>
    public int SpawnUnit(string unitTypeId, int playerId, int x, int y)
    {
        int id = _nextUnitId++;
        _units[id] = new UnitState
        {
            UnitId = id,
            UnitTypeId = unitTypeId,
            PlayerId = playerId,
            X = FixedPoint.FromInt(x),
            Y = FixedPoint.FromInt(y),
            Health = FixedPoint.FromInt(100),
            IsAlive = true
        };
        return id;
    }

    /// <summary>
    /// Transitions the match to MatchState.Ended. Rejects if already ended.
    /// </summary>
    public void EndMatch(int winnerId, string reason)
    {
        if (CurrentState == MatchState.Ended)
            return; // Reject double-end

        CurrentState = MatchState.Ended;
    }

    /// <summary>
    /// Returns the current state of a unit by ID, or null if not found.
    /// </summary>
    public UnitState? GetUnitState(int unitId)
    {
        if (_units.TryGetValue(unitId, out var state))
            return state;
        return null;
    }

    /// <summary>
    /// Returns all unit states for inspection.
    /// </summary>
    public IReadOnlyDictionary<int, UnitState> GetAllUnits() => _units;

    // ── IDisposable ──────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _units.Clear();
    }

    // ── Private Helpers ──────────────────────────────────────────────────

    private void ExecuteCommand(GameCommand command)
    {
        // Build a minimal context for command execution
        var ctx = new GameCommandContext
        {
            Terrain = Grid,
            CurrentTick = CurrentTick,
            GetUnit = id =>
            {
                if (_units.TryGetValue(id, out var unit) && unit.IsAlive)
                {
                    return new UnitCommandView
                    {
                        UnitId = unit.UnitId,
                        PlayerId = unit.PlayerId,
                        Position = new FixedVector2(unit.X, unit.Y),
                        IsAlive = true
                    };
                }
                return null;
            },
            IssueOrder = (unitId, order) =>
            {
                // For move orders, set the unit's target position
                if (order.Type == UnitOrderType.Move && _units.ContainsKey(unitId))
                {
                    var u = _units[unitId];
                    // Store target in a simple way - move unit toward target
                    _pendingMoves[unitId] = order.TargetPosition;
                }
            },
            Rng = new DeterministicRng(_config.MatchSeed + CurrentTick)
        };

        command.Execute(ctx);
    }

    private readonly Dictionary<int, FixedVector2> _pendingMoves = new();

    /// <summary>
    /// Simplified unit movement: moves units one step toward their target each tick.
    /// Uses FixedPoint arithmetic exclusively.
    /// </summary>
    private void ProcessUnitMovement()
    {
        var completedMoves = new List<int>();

        foreach (var kvp in _pendingMoves)
        {
            int unitId = kvp.Key;
            var target = kvp.Value;

            if (!_units.TryGetValue(unitId, out var unit) || !unit.IsAlive)
            {
                completedMoves.Add(unitId);
                continue;
            }

            // Calculate direction toward target
            var currentPos = new FixedVector2(unit.X, unit.Y);
            var diff = target - currentPos;
            var distSq = diff.LengthSquared;

            // If close enough, snap to target and complete
            if (distSq <= FixedPoint.One)
            {
                unit.X = target.X;
                unit.Y = target.Y;
                _units[unitId] = unit;
                completedMoves.Add(unitId);
                continue;
            }

            // Move one unit toward target per tick
            var direction = diff.Normalized();
            unit.X = unit.X + direction.X;
            unit.Y = unit.Y + direction.Y;
            _units[unitId] = unit;
        }

        foreach (var id in completedMoves)
        {
            _pendingMoves.Remove(id);
        }
    }
}
