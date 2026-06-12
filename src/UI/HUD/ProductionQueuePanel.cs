using Godot;
using CorditeWars.Game.Buildings;
using CorditeWars.UI.Input;

namespace CorditeWars.UI.HUD;

/// <summary>
/// Enhanced HUD panel showing production state for selected buildings.
/// Displays current item name, progress bar, ETA countdown, cancel button,
/// and queue depth. Auto-hides when no production building is selected or queue is empty.
/// </summary>
public partial class ProductionQueuePanel : PanelContainer
{
    private const int TickRate = 30; // 30 ticks per second
    private static readonly Color AccentColor = new(0.29f, 0.62f, 0.80f); // #4A9ECC

    private SelectionManager? _selectionManager;
    private BuildingInstance? _trackedBuilding;
    private ProductionQueue? _trackedQueue;

    // UI elements
    private VBoxContainer? _content;
    private Label? _itemNameLabel;
    private ProgressBar? _progressBar;
    private Label? _etaLabel;
    private Button? _cancelButton;
    private Label? _queueDepthLabel;

    // ── Initialization ───────────────────────────────────────────────

    public void Initialize(SelectionManager selectionManager)
    {
        _selectionManager = selectionManager;
        Name = "ProductionQueuePanel";

        // Position above command card area (bottom-right)
        AnchorLeft = 1;
        AnchorTop = 1;
        AnchorRight = 1;
        AnchorBottom = 1;
        OffsetLeft = -270;
        OffsetTop = -260;
        OffsetRight = -8;
        OffsetBottom = -185;

        CustomMinimumSize = new Vector2(260, 80);

        // Dark panel background
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.1f, 0.1f, 0.12f, 0.9f);
        style.BorderWidthBottom = 1;
        style.BorderWidthTop = 1;
        style.BorderWidthLeft = 1;
        style.BorderWidthRight = 1;
        style.BorderColor = new Color(0.165f, 0.165f, 0.227f);
        style.CornerRadiusTopLeft = 4;
        style.CornerRadiusTopRight = 4;
        style.CornerRadiusBottomLeft = 4;
        style.CornerRadiusBottomRight = 4;
        style.ContentMarginLeft = 10;
        style.ContentMarginRight = 10;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        AddThemeStyleboxOverride("panel", style);

        BuildUI();

        Visible = false;
    }

    private void BuildUI()
    {
        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 4);
        AddChild(_content);

        // Item name label
        _itemNameLabel = new Label();
        _itemNameLabel.Text = "";
        _itemNameLabel.AddThemeColorOverride("font_color", new Color(0.878f, 0.878f, 0.910f));
        _itemNameLabel.AddThemeFontSizeOverride("font_size", 14);
        _content.AddChild(_itemNameLabel);

        // Progress bar row
        var progressRow = new HBoxContainer();
        progressRow.AddThemeConstantOverride("separation", 8);
        _content.AddChild(progressRow);

        _progressBar = new ProgressBar();
        _progressBar.CustomMinimumSize = new Vector2(150, 14);
        _progressBar.MaxValue = 100;
        _progressBar.ShowPercentage = false;
        _progressBar.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = new Color(0.165f, 0.165f, 0.227f);
        _progressBar.AddThemeStyleboxOverride("background", bgStyle);

        var fillStyle = new StyleBoxFlat();
        fillStyle.BgColor = AccentColor;
        _progressBar.AddThemeStyleboxOverride("fill", fillStyle);
        progressRow.AddChild(_progressBar);

        // ETA label
        _etaLabel = new Label();
        _etaLabel.Text = "";
        _etaLabel.AddThemeColorOverride("font_color", new Color(0.533f, 0.533f, 0.627f));
        _etaLabel.AddThemeFontSizeOverride("font_size", 12);
        _etaLabel.CustomMinimumSize = new Vector2(40, 0);
        progressRow.AddChild(_etaLabel);

        // Cancel button and queue depth row
        var bottomRow = new HBoxContainer();
        bottomRow.AddThemeConstantOverride("separation", 8);
        _content.AddChild(bottomRow);

        _cancelButton = new Button();
        _cancelButton.Text = "Cancel";
        _cancelButton.CustomMinimumSize = new Vector2(60, 24);
        _cancelButton.AddThemeColorOverride("font_color", new Color(0.878f, 0.878f, 0.910f));
        _cancelButton.AddThemeFontSizeOverride("font_size", 12);

        var cancelNormal = new StyleBoxFlat();
        cancelNormal.BgColor = new Color(0.6f, 0.2f, 0.2f, 0.8f);
        cancelNormal.CornerRadiusTopLeft = 3;
        cancelNormal.CornerRadiusTopRight = 3;
        cancelNormal.CornerRadiusBottomLeft = 3;
        cancelNormal.CornerRadiusBottomRight = 3;
        _cancelButton.AddThemeStyleboxOverride("normal", cancelNormal);

        var cancelHover = new StyleBoxFlat();
        cancelHover.BgColor = new Color(0.7f, 0.25f, 0.25f, 0.9f);
        cancelHover.CornerRadiusTopLeft = 3;
        cancelHover.CornerRadiusTopRight = 3;
        cancelHover.CornerRadiusBottomLeft = 3;
        cancelHover.CornerRadiusBottomRight = 3;
        _cancelButton.AddThemeStyleboxOverride("hover", cancelHover);

        _cancelButton.Pressed += OnCancelPressed;
        bottomRow.AddChild(_cancelButton);

        // Queue depth label
        _queueDepthLabel = new Label();
        _queueDepthLabel.Text = "";
        _queueDepthLabel.AddThemeColorOverride("font_color", AccentColor);
        _queueDepthLabel.AddThemeFontSizeOverride("font_size", 12);
        _queueDepthLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _queueDepthLabel.HorizontalAlignment = HorizontalAlignment.Right;
        bottomRow.AddChild(_queueDepthLabel);
    }

    // ── Public API ───────────────────────────────────────────────────

    /// <summary>
    /// Tracks the specified building's production queue.
    /// </summary>
    public void TrackBuilding(BuildingInstance? building)
    {
        _trackedBuilding = building;
        _trackedQueue = building?.GetNodeOrNull<ProductionQueue>("ProductionQueue");
        UpdateVisibility();
    }

    /// <summary>
    /// Clears the currently tracked building.
    /// </summary>
    public void ClearTracking()
    {
        _trackedBuilding = null;
        _trackedQueue = null;
        Visible = false;
    }

    // ── Frame Update ─────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        if (_trackedBuilding is null || _trackedQueue is null)
        {
            Visible = false;
            return;
        }

        // Check if building is still valid
        if (!IsInstanceValid(_trackedBuilding))
        {
            ClearTracking();
            return;
        }

        UpdateVisibility();

        if (!Visible) return;

        UpdateDisplay();
    }

    // ── Internal ─────────────────────────────────────────────────────

    private void UpdateVisibility()
    {
        if (_trackedQueue is null)
        {
            Visible = false;
            return;
        }

        Visible = _trackedQueue.IsProducing || _trackedQueue.QueueCount > 0;
    }

    private void UpdateDisplay()
    {
        if (_trackedQueue is null) return;

        if (_trackedQueue.IsProducing)
        {
            // Item name
            _itemNameLabel!.Text = $"Building: {_trackedQueue.CurrentUnitTypeId}";

            // Progress bar
            float percent = _trackedQueue.ProgressPercent * 100f;
            _progressBar!.Value = percent;

            // ETA calculation: (buildTime - currentProgress) / tickRate
            float buildTimeFloat = _trackedQueue.CurrentBuildTime.ToFloat();
            float currentProgressFloat = _trackedQueue.CurrentProgress.ToFloat();
            float remainingTicks = buildTimeFloat - currentProgressFloat;
            float etaSeconds = remainingTicks / TickRate;
            if (etaSeconds < 0f) etaSeconds = 0f;
            _etaLabel!.Text = $"{etaSeconds:F0}s";

            // Cancel button visible
            _cancelButton!.Visible = true;
        }
        else
        {
            _itemNameLabel!.Text = "Queued";
            _progressBar!.Value = 0;
            _etaLabel!.Text = "";
            _cancelButton!.Visible = false;
        }

        // Queue depth: count includes items waiting (not current production)
        int queueCount = _trackedQueue.QueueCount;
        int maxQueue = 5;
        int totalInQueue = queueCount + (_trackedQueue.IsProducing ? 1 : 0);
        _queueDepthLabel!.Text = $"Queue: {totalInQueue}/{maxQueue}";
    }

    private void OnCancelPressed()
    {
        _trackedQueue?.CancelCurrent();
    }
}
