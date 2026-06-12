using CorditeWars.Core;
using CorditeWars.Game;
using CorditeWars.Game.Campaign;
using CorditeWars.Systems.Networking;
using Xunit;

namespace CorditeWars.Tests.Integration;

/// <summary>
/// Integration tests for command processing within the GameSession harness.
/// Verifies that commands are executed at the correct tick and produce
/// expected state changes.
/// </summary>
public class GameSessionCommandTests : IDisposable
{
    private readonly GameSessionHarness _harness;

    public GameSessionCommandTests()
    {
        _harness = new GameSessionHarness(CreateConfig());
    }

    public void Dispose()
    {
        _harness.Dispose();
    }

    [Fact]
    public void Move_Command_Causes_Unit_Position_To_Change()
    {
        // Spawn a unit at (5, 5)
        int unitId = _harness.SpawnUnit("infantry", playerId: 1, x: 5, y: 5);

        // Inject a move command to (15, 15) at tick 3
        var moveCmd = new MoveCommand
        {
            PlayerId = 1,
            UnitIds = new List<int> { unitId },
            TargetPosition = new FixedVector2(FixedPoint.FromInt(15), FixedPoint.FromInt(15))
        };
        _harness.InjectCommand(moveCmd, targetTick: 3);

        // Record initial position
        var initialState = _harness.GetUnitState(unitId)!.Value;
        var initialX = initialState.X;
        var initialY = initialState.Y;

        // Advance past the command tick and give time for movement
        _harness.AdvanceTicks(20);

        // Unit should have moved from its initial position
        var finalState = _harness.GetUnitState(unitId)!.Value;
        Assert.True(
            finalState.X != initialX || finalState.Y != initialY,
            "Unit position should have changed after move command");
    }

    [Fact]
    public void Spawned_Unit_Exists_With_Correct_Initial_Position()
    {
        int unitId = _harness.SpawnUnit("tank", playerId: 2, x: 10, y: 20);

        var state = _harness.GetUnitState(unitId);
        Assert.NotNull(state);
        Assert.Equal(FixedPoint.FromInt(10), state.Value.X);
        Assert.Equal(FixedPoint.FromInt(20), state.Value.Y);
        Assert.True(state.Value.IsAlive);
        Assert.Equal(2, state.Value.PlayerId);
        Assert.Equal("tank", state.Value.UnitTypeId);
    }

    [Fact]
    public void Win_Condition_EndMatch_Transitions_State()
    {
        // Spawn some units and advance a few ticks
        _harness.SpawnUnit("infantry", playerId: 1, x: 5, y: 5);
        _harness.SpawnUnit("infantry", playerId: 2, x: 20, y: 20);
        _harness.AdvanceTicks(10);

        Assert.Equal(MatchState.Playing, _harness.CurrentState);

        // Trigger win condition
        _harness.EndMatch(winnerId: 1, reason: "All enemy units destroyed");

        Assert.Equal(MatchState.Ended, _harness.CurrentState);

        // Further ticks should not advance (match is ended)
        ulong tickBeforeAdvance = _harness.CurrentTick;
        _harness.AdvanceTicks(5);
        Assert.Equal(tickBeforeAdvance, _harness.CurrentTick);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static MatchConfig CreateConfig()
    {
        return new MatchConfig
        {
            MapId = "test_map",
            MatchSeed = 54321,
            PlayerConfigs = new[]
            {
                new PlayerConfig { PlayerId = 1, FactionId = "arcloft", IsAI = false, PlayerName = "Player1" },
                new PlayerConfig { PlayerId = 2, FactionId = "ironveil", IsAI = true, PlayerName = "AI" }
            },
            WinCondition = WinCondition.DestroyHQ
        };
    }
}
