using Godot;

namespace UnnamedRTS.Core;

/// <summary>
/// Boot scene script. Runs once at application start.
/// Handles initialization order, loading screen, and
/// transitions to the main menu.
/// </summary>
public partial class BootLoader : Node
{
    public override void _Ready()
    {
        GD.Print("╔════════════════════════════════════════╗");
        GD.Print("║        Unnamed RTS — v0.0.1            ║");
        GD.Print("║   Godot 4.4 + C# | Forward Plus       ║");
        GD.Print("╚════════════════════════════════════════╝");
        GD.Print("");

        // Verify autoloads are available
        var gameManager = GetNode<GameManager>("/root/GameManager");
        var eventBus = GetNode<EventBus>("/root/EventBus");

        if (gameManager == null || eventBus == null)
        {
            GD.PrintErr("[Boot] FATAL: Autoloads not found. Check project.godot.");
            GetTree().Quit(1);
            return;
        }

        GD.Print("[Boot] All core systems initialized.");
        GD.Print("[Boot] Ready for main menu scene.");

        // TODO: Transition to main menu scene once it exists.
        // GetTree().ChangeSceneToFile("res://scenes/UI/MainMenu.tscn");
    }
}
