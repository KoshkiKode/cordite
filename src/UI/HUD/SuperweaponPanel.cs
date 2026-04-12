using System.Collections.Generic;
using Godot;
using CorditeWars.Core;
using CorditeWars.Systems.Superweapon;

namespace CorditeWars.UI.HUD;

/// <summary>
/// HUD panel showing all of the local player's superweapon abilities.
/// Each faction gets two slots: one BuildingSuperweapon and one ActivatedAbility.
/// Each slot has a name label, charge bar, and FIRE button.
///
/// Wire up by calling <see cref="Initialize"/> then update each frame via <see cref="Update"/>.
/// Pressing a FIRE button emits <see cref="EventBus.SuperweaponActivateRequested"/> with the
/// specific weaponId so that the targeting system knows which weapon to arm.
/// </summary>
public partial class SuperweaponPanel : PanelContainer
{
    private SuperweaponSystem? _system;
    private int _playerId;

    // Per-slot controls — populated dynamically in Initialize
    private readonly List<WeaponSlotControls> _slots = new();

    private sealed class WeaponSlotControls
    {
        public string WeaponId = string.Empty;
        public Label?       NameLabel;
        public Label?       TagLabel;    // Displays "BUILDING" or "ABILITY" category
        public ProgressBar? ChargeBar;
        public Label?       StatusLabel;
        public Button?      FireButton;
    }

    // ── Initialization ───────────────────────────────────────────────

    public void Initialize(int playerId, SuperweaponSystem system)
    {
        _playerId = playerId;
        _system   = system;
        Name      = "SuperweaponPanel";

        // Position: top-right, tall enough for two slots
        AnchorLeft   = 1;
        AnchorTop    = 0;
        AnchorRight  = 1;
        AnchorBottom = 0;
        OffsetLeft   = -240;
        OffsetRight  = -8;
        OffsetTop    = 60;
        OffsetBottom = 220;

        var bg = new StyleBoxFlat();
        bg.BgColor = new Color(0.06f, 0.06f, 0.10f, 0.88f);
        bg.SetBorderWidthAll(1);
        bg.BorderColor = new Color(0.2f, 0.2f, 0.35f);
        bg.SetCornerRadiusAll(4);
        bg.ContentMarginLeft = bg.ContentMarginRight = 10;
        bg.ContentMarginTop = bg.ContentMarginBottom = 6;
        AddThemeStyleboxOverride("panel", bg);

        var outerVbox = new VBoxContainer();
        outerVbox.AddThemeConstantOverride("separation", 8);
        AddChild(outerVbox);

        // Build a slot for each weapon in the catalogue for this player's faction.
        // If the system has no weapons yet (pre-registration), slots are built later.
        foreach (var state in system.GetPlayerWeapons(playerId))
            outerVbox.AddChild(BuildSlot(state));
    }

    private Control BuildSlot(PlayerSuperweaponState state)
    {
        var slot = new WeaponSlotControls { WeaponId = state.Data.Id };

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 3);

        // Header row: name + category tag
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 4);

        slot.NameLabel = new Label { Text = state.Data.DisplayName };
        UITheme.StyleLabel(slot.NameLabel, UITheme.FontSizeSmall, UITheme.Accent);
        slot.NameLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand;
        header.AddChild(slot.NameLabel);

        // Coloured tag indicating category
        slot.TagLabel = new Label
        {
            Text = state.Data.Category == SuperweaponCategory.BuildingSuperweapon
                   ? "BUILDING" : "ABILITY"
        };
        UITheme.StyleLabel(slot.TagLabel, UITheme.FontSizeSmall,
            state.Data.Category == SuperweaponCategory.BuildingSuperweapon
                ? new Color(1.0f, 0.65f, 0.0f) // orange for building
                : new Color(0.4f, 0.8f, 1.0f));  // cyan for ability
        header.AddChild(slot.TagLabel);
        vbox.AddChild(header);

        slot.ChargeBar = new ProgressBar();
        slot.ChargeBar.MinValue = 0;
        slot.ChargeBar.MaxValue = 100;
        slot.ChargeBar.Value    = 0;
        slot.ChargeBar.ShowPercentage = false;
        slot.ChargeBar.CustomMinimumSize = new Vector2(190, 10);
        vbox.AddChild(slot.ChargeBar);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);

        slot.StatusLabel = new Label { Text = "Charging…" };
        UITheme.StyleLabel(slot.StatusLabel, UITheme.FontSizeSmall, UITheme.TextMuted);
        slot.StatusLabel.SizeFlagsHorizontal = Control.SizeFlags.Expand;
        row.AddChild(slot.StatusLabel);

        slot.FireButton = new Button { Text = "FIRE" };
        UITheme.StyleButton(slot.FireButton);
        slot.FireButton.Disabled = true;
        string capturedId = state.Data.Id;
        slot.FireButton.Pressed += () => OnFirePressed(capturedId);
        row.AddChild(slot.FireButton);

        vbox.AddChild(row);
        _slots.Add(slot);
        return vbox;
    }

    // ── Update ───────────────────────────────────────────────────────

    /// <summary>
    /// Call once per render frame to refresh the HUD from system state.
    /// </summary>
    public void Update()
    {
        if (_system is null) return;

        bool anyVisible = false;
        foreach (var slot in _slots)
        {
            var state = _system.GetState(_playerId, slot.WeaponId);
            if (state is null) continue;

            anyVisible = true;
            float charge = state.ChargePercent * 100f;

            if (slot.NameLabel != null)
                slot.NameLabel.Text = state.Data.DisplayName;

            if (slot.ChargeBar != null)
                slot.ChargeBar.Value = charge;

            bool ready = state.IsReady;
            if (slot.StatusLabel != null)
            {
                slot.StatusLabel.Text = ready ? "READY" : $"{(int)charge}%";
                slot.StatusLabel.AddThemeColorOverride("font_color",
                    ready ? UITheme.SuccessColor : UITheme.TextMuted);
            }

            if (slot.FireButton != null)
                slot.FireButton.Disabled = !ready;
        }

        Visible = anyVisible;
    }

    // ── Button ───────────────────────────────────────────────────────

    private void OnFirePressed(string weaponId)
    {
        EventBus.Instance?.EmitSuperweaponActivateRequested(_playerId, weaponId);
    }
}
