using CorditeWars.Core;
using CorditeWars.Game;
using CorditeWars.Game.Campaign;
using CorditeWars.Systems.Networking;
using CorditeWars.Systems.Pathfinding;
using Xunit;

namespace CorditeWars.Tests.Integration;

/// <summary>
/// Integration tests verifying determinism of the GameSession harness.
/// Same inputs must produce identical outputs across multiple runs.
/// </summary>
public class GameSessionDeterminismTests
{
    [Fact]
    public void Same_100_Tick_Scenario_Produces_Identical_Final_Tick()
    {
        var config = CreateConfig(seed: 42);

        // Run 1
        using var harness1 = new GameSessionHarness(config);
        harness1.SpawnUnit("tank", playerId: 1, x: 5, y: 5);
        harness1.SpawnUnit("infantry", playerId: 2, x: 20, y: 20);
        harness1.AdvanceTicks(100);

        // Run 2
        using var harness2 = new GameSessionHarness(config);
        harness2.SpawnUnit("tank", playerId: 1, x: 5, y: 5);
        harness2.SpawnUnit("infantry", playerId: 2, x: 20, y: 20);
        harness2.AdvanceTicks(100);

        Assert.Equal(harness1.CurrentTick, harness2.CurrentTick);
        Assert.Equal(100UL, harness1.CurrentTick);
    }

    [Fact]
    public void Same_Config_And_Commands_Produce_Identical_Unit_Positions()
    {
        var config = CreateConfig(seed: 99);

        // Run 1
        using var harness1 = new GameSessionHarness(config);
        int unit1a = harness1.SpawnUnit("infantry", playerId: 1, x: 2, y: 2);
        int unit1b = harness1.SpawnUnit("tank", playerId: 1, x: 10, y: 10);
        InjectMoveCommand(harness1, playerId: 1, unitIds: new[] { unit1a }, targetX: 15, targetY: 15, tick: 5);
        InjectMoveCommand(harness1, playerId: 1, unitIds: new[] { unit1b }, targetX: 25, targetY: 25, tick: 10);
        harness1.AdvanceTicks(50);

        // Run 2
        using var harness2 = new GameSessionHarness(config);
        int unit2a = harness2.SpawnUnit("infantry", playerId: 1, x: 2, y: 2);
        int unit2b = harness2.SpawnUnit("tank", playerId: 1, x: 10, y: 10);
        InjectMoveCommand(harness2, playerId: 1, unitIds: new[] { unit2a }, targetX: 15, targetY: 15, tick: 5);
        InjectMoveCommand(harness2, playerId: 1, unitIds: new[] { unit2b }, targetX: 25, targetY: 25, tick: 10);
        harness2.AdvanceTicks(50);

        // Verify positions are bit-identical
        var state1a = harness1.GetUnitState(unit1a)!.Value;
        var state2a = harness2.GetUnitState(unit2a)!.Value;
        Assert.Equal(state1a.X, state2a.X);
        Assert.Equal(state1a.Y, state2a.Y);

        var state1b = harness1.GetUnitState(unit1b)!.Value;
        var state2b = harness2.GetUnitState(unit2b)!.Value;
        Assert.Equal(state1b.X, state2b.X);
        Assert.Equal(state1b.Y, state2b.Y);
    }

    [Fact]
    public void Pathfinding_Results_Are_Deterministic()
    {
        var config = CreateConfig(seed: 777);

        // Run pathfinding twice with identical setup
        var paths1 = new List<List<(int, int)>>();
        var paths2 = new List<List<(int, int)>>();

        // Run 1
        using (var harness1 = new GameSessionHarness(config))
        {
            var profile = MovementProfile.Infantry();
            harness1.PathRequests.RequestPath(
                unitId: 1,
                profile: profile,
                start: new FixedVector2(FixedPoint.FromInt(2), FixedPoint.FromInt(2)),
                goal: new FixedVector2(FixedPoint.FromInt(20), FixedPoint.FromInt(20)),
                callback: path => paths1.Add(new List<(int, int)>(path)));
            harness1.PathRequests.ProcessRequests(harness1.Grid, maxPathsPerTick: 4);
        }

        // Run 2
        using (var harness2 = new GameSessionHarness(config))
        {
            var profile = MovementProfile.Infantry();
            harness2.PathRequests.RequestPath(
                unitId: 1,
                profile: profile,
                start: new FixedVector2(FixedPoint.FromInt(2), FixedPoint.FromInt(2)),
                goal: new FixedVector2(FixedPoint.FromInt(20), FixedPoint.FromInt(20)),
                callback: path => paths2.Add(new List<(int, int)>(path)));
            harness2.PathRequests.ProcessRequests(harness2.Grid, maxPathsPerTick: 4);
        }

        // Both runs should produce identical paths
        Assert.Equal(paths1.Count, paths2.Count);
        for (int i = 0; i < paths1.Count; i++)
        {
            Assert.Equal(paths1[i].Count, paths2[i].Count);
            for (int j = 0; j < paths1[i].Count; j++)
            {
                Assert.Equal(paths1[i][j], paths2[i][j]);
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static MatchConfig CreateConfig(ulong seed)
    {
        return new MatchConfig
        {
            MapId = "test_map",
            MatchSeed = seed,
            PlayerConfigs = new[]
            {
                new PlayerConfig { PlayerId = 1, FactionId = "arcloft", IsAI = false, PlayerName = "Player1" },
                new PlayerConfig { PlayerId = 2, FactionId = "ironveil", IsAI = true, PlayerName = "AI" }
            },
            WinCondition = WinCondition.DestroyHQ
        };
    }

    private static void InjectMoveCommand(GameSessionHarness harness, int playerId, int[] unitIds, int targetX, int targetY, ulong tick)
    {
        var cmd = new MoveCommand
        {
            PlayerId = playerId,
            UnitIds = new List<int>(unitIds),
            TargetPosition = new FixedVector2(FixedPoint.FromInt(targetX), FixedPoint.FromInt(targetY))
        };
        harness.InjectCommand(cmd, tick);
    }
}
