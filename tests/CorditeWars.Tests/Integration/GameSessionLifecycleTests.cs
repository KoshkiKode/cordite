using CorditeWars.Game;
using CorditeWars.Game.Campaign;
using Xunit;

namespace CorditeWars.Tests.Integration;

/// <summary>
/// Integration tests for GameSession lifecycle: initialization, state transitions,
/// tick advancement, and unit spawning.
/// </summary>
public class GameSessionLifecycleTests : IDisposable
{
    private readonly GameSessionHarness _harness;

    public GameSessionLifecycleTests()
    {
        _harness = new GameSessionHarness(CreateDefaultConfig());
    }

    public void Dispose()
    {
        _harness.Dispose();
    }

    [Fact]
    public void Harness_Initializes_In_Playing_State()
    {
        Assert.Equal(MatchState.Playing, _harness.CurrentState);
        Assert.Equal(0UL, _harness.CurrentTick);
        Assert.NotNull(_harness.Grid);
        Assert.NotNull(_harness.PathRequests);
        Assert.NotNull(_harness.UnitInteraction);
    }

    [Fact]
    public void EndMatch_Transitions_To_Ended_Exactly_Once()
    {
        _harness.EndMatch(winnerId: 1, reason: "HQ destroyed");

        Assert.Equal(MatchState.Ended, _harness.CurrentState);
    }

    [Fact]
    public void Double_EndMatch_Is_Rejected()
    {
        _harness.EndMatch(winnerId: 1, reason: "HQ destroyed");
        Assert.Equal(MatchState.Ended, _harness.CurrentState);

        // Second call should be rejected — state stays Ended, no exception
        _harness.EndMatch(winnerId: 2, reason: "All units killed");
        Assert.Equal(MatchState.Ended, _harness.CurrentState);
    }

    [Fact]
    public void AdvanceTicks_Calls_ProcessTick_Exactly_N_Times()
    {
        int n = 50;
        _harness.AdvanceTicks(n);

        Assert.Equal((ulong)n, _harness.CurrentTick);
    }

    [Fact]
    public void SpawnUnit_Returns_Unique_IDs()
    {
        var ids = new HashSet<int>();

        for (int i = 0; i < 100; i++)
        {
            int id = _harness.SpawnUnit("infantry", playerId: 1, x: i % 30, y: i / 30);
            Assert.True(ids.Add(id), $"Duplicate unit ID: {id}");
        }

        Assert.Equal(100, ids.Count);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static MatchConfig CreateDefaultConfig()
    {
        return new MatchConfig
        {
            MapId = "test_map",
            MatchSeed = 12345,
            PlayerConfigs = new[]
            {
                new PlayerConfig { PlayerId = 1, FactionId = "arcloft", IsAI = false, PlayerName = "Player1" },
                new PlayerConfig { PlayerId = 2, FactionId = "ironveil", IsAI = true, PlayerName = "AI" }
            },
            WinCondition = WinCondition.DestroyHQ
        };
    }
}
