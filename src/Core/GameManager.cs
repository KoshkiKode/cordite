using Godot;

namespace UnnamedRTS.Core;

/// <summary>
/// Global game manager autoload. Handles game state, tick management,
/// and coordination between all major subsystems.
/// This is the central authority for the deterministic game simulation.
/// </summary>
public partial class GameManager : Node
{
    /// <summary>
    /// The fixed simulation tick rate. All game logic runs at this rate
    /// for deterministic lockstep networking compatibility.
    /// </summary>
    public const int SimTickRate = 30;

    /// <summary>
    /// Current simulation tick number. Monotonically increasing.
    /// Used for deterministic replay and lockstep sync.
    /// </summary>
    public ulong CurrentTick { get; private set; }

    /// <summary>
    /// Current state of the game.
    /// </summary>
    public GameState State { get; private set; } = GameState.Boot;

    /// <summary>
    /// Random number generator seeded per-match for deterministic simulation.
    /// All gameplay randomness MUST use this — never System.Random or GD.Randf().
    /// </summary>
    public DeterministicRng Rng { get; private set; } = new(0);

    public override void _Ready()
    {
        GD.Print("[GameManager] Initialized.");
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (State == GameState.Playing)
        {
            CurrentTick++;
        }
    }

    /// <summary>
    /// Starts a new match with the given RNG seed.
    /// All clients must use the same seed for deterministic lockstep.
    /// </summary>
    public void StartMatch(ulong seed)
    {
        CurrentTick = 0;
        Rng = new DeterministicRng(seed);
        State = GameState.Playing;
        EventBus.Instance?.EmitMatchStarted(seed);
        GD.Print($"[GameManager] Match started with seed {seed}.");
    }

    /// <summary>
    /// Pauses the simulation. Input is still processed.
    /// </summary>
    public void PauseMatch()
    {
        State = GameState.Paused;
        EventBus.Instance?.EmitMatchPaused();
        GD.Print("[GameManager] Match paused.");
    }

    /// <summary>
    /// Resumes a paused match.
    /// </summary>
    public void ResumeMatch()
    {
        State = GameState.Playing;
        EventBus.Instance?.EmitMatchResumed();
        GD.Print("[GameManager] Match resumed.");
    }

    /// <summary>
    /// Ends the current match.
    /// </summary>
    public void EndMatch()
    {
        State = GameState.PostGame;
        EventBus.Instance?.EmitMatchEnded();
        GD.Print($"[GameManager] Match ended at tick {CurrentTick}.");
    }

    /// <summary>
    /// Returns to the main menu state.
    /// </summary>
    public void ReturnToMenu()
    {
        State = GameState.MainMenu;
        CurrentTick = 0;
    }
}

/// <summary>
/// All possible states the game can be in.
/// </summary>
public enum GameState
{
    Boot,
    MainMenu,
    Loading,
    Playing,
    Paused,
    PostGame
}
