using Godot;

namespace CorditeWars.UI.HUD;

/// <summary>
/// F4-toggled performance overlay displaying real-time metrics:
/// FPS, frame time (ms), draw calls, terrain triangles, active units,
/// pathfinding budget %, and memory (MB).
/// Uses exponential moving average for FPS/frame time stability.
/// Skips all metric collection when hidden (zero overhead).
/// </summary>
public partial class PerformanceOverlay : CanvasLayer
{
    private const float EmaFactor = 0.1f;
    private const string UnavailableMetric = "—";

    private Label? _metricsLabel;
    private ColorRect? _background;
    private bool _isVisible;

    // EMA-smoothed values
    private float _smoothedFps;
    private float _smoothedFrameTimeMs;

    // Optional system references (null = metric unavailable)
    private Node? _gameSession;

    // ── Initialization ───────────────────────────────────────────────

    /// <summary>
    /// Initializes the performance overlay. Pass the GameSession node
    /// for access to unit count and pathfinding metrics, or null if unavailable.
    /// </summary>
    public void Initialize(Node? gameSession = null)
    {
        _gameSession = gameSession;
        Name = "PerformanceOverlay";
        Layer = 50;

        // Background panel for readability
        var control = new Control();
        control.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        control.OffsetLeft = 8;
        control.OffsetTop = 8;
        control.CustomMinimumSize = new Vector2(220, 180);
        AddChild(control);

        _background = new ColorRect();
        _background.Color = new Color(0.05f, 0.05f, 0.08f, 0.75f);
        _background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _background.Size = new Vector2(220, 180);
        control.AddChild(_background);

        // Monospace label for metrics
        _metricsLabel = new Label();
        _metricsLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _metricsLabel.OffsetLeft = 8;
        _metricsLabel.OffsetTop = 6;
        _metricsLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.92f, 0.85f));
        _metricsLabel.AddThemeFontSizeOverride("font_size", 13);
        _metricsLabel.HorizontalAlignment = HorizontalAlignment.Left;
        _metricsLabel.VerticalAlignment = VerticalAlignment.Top;
        control.AddChild(_metricsLabel);

        // Start hidden
        _isVisible = false;
        Visible = false;

        // Initialize EMA values
        _smoothedFps = 60f;
        _smoothedFrameTimeMs = 16.67f;
    }

    // ── Public API ───────────────────────────────────────────────────

    /// <summary>
    /// Toggles the overlay visibility. When hidden, no metrics are collected.
    /// </summary>
    public void Toggle()
    {
        _isVisible = !_isVisible;
        Visible = _isVisible;
    }

    // ── Frame Update ─────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        // Skip all metric collection when hidden (zero overhead)
        if (!_isVisible) return;

        CollectAndDisplay(delta);
    }

    // ── Metric Collection ────────────────────────────────────────────

    private void CollectAndDisplay(double delta)
    {
        // FPS with EMA smoothing
        float currentFps = (float)Engine.GetFramesPerSecond();
        _smoothedFps = _smoothedFps + EmaFactor * (currentFps - _smoothedFps);

        // Frame time with EMA smoothing
        float currentFrameTimeMs = (float)(delta * 1000.0);
        _smoothedFrameTimeMs = _smoothedFrameTimeMs + EmaFactor * (currentFrameTimeMs - _smoothedFrameTimeMs);

        // Draw calls
        string drawCallsStr = GetDrawCalls();

        // Terrain triangles
        string terrainTrisStr = GetTerrainTriangleCount();

        // Active units
        string activeUnitsStr = GetActiveUnitCount();

        // Pathfinding budget
        string pathBudgetStr = GetPathfindingBudgetUsage();

        // Memory
        string memoryStr = GetMemoryUsageMB();

        // Format display
        _metricsLabel!.Text =
            $"FPS: {_smoothedFps:F1}\n" +
            $"Frame: {_smoothedFrameTimeMs:F2} ms\n" +
            $"Draw Calls: {drawCallsStr}\n" +
            $"Terrain Tris: {terrainTrisStr}\n" +
            $"Active Units: {activeUnitsStr}\n" +
            $"Path Budget: {pathBudgetStr}\n" +
            $"Memory: {memoryStr}";
    }

    private string GetDrawCalls()
    {
        try
        {
            ulong drawCalls = RenderingServer.GetRenderingInfo(
                RenderingServer.RenderingInfo.TotalDrawCallsInFrame);
            return drawCalls.ToString();
        }
        catch
        {
            return UnavailableMetric;
        }
    }

    private string GetTerrainTriangleCount()
    {
        // Terrain triangle count is not directly available without a reference
        // to the TerrainEngine. Display unavailable if no source.
        if (_gameSession is null) return UnavailableMetric;

        // Try to read a cached triangle count from the game session
        // via a method or property if available
        try
        {
            var method = _gameSession.GetType().GetProperty("TerrainTriangleCount");
            if (method is not null)
            {
                var value = method.GetValue(_gameSession);
                return value?.ToString() ?? UnavailableMetric;
            }
        }
        catch
        {
            // Fall through
        }

        return UnavailableMetric;
    }

    private string GetActiveUnitCount()
    {
        if (_gameSession is null) return UnavailableMetric;

        try
        {
            var prop = _gameSession.GetType().GetProperty("ActiveUnitCount");
            if (prop is not null)
            {
                var value = prop.GetValue(_gameSession);
                return value?.ToString() ?? UnavailableMetric;
            }
        }
        catch
        {
            // Fall through
        }

        return UnavailableMetric;
    }

    private string GetPathfindingBudgetUsage()
    {
        if (_gameSession is null) return UnavailableMetric;

        try
        {
            var prop = _gameSession.GetType().GetProperty("PathfindingBudgetPercent");
            if (prop is not null)
            {
                var value = prop.GetValue(_gameSession);
                if (value is float f)
                    return $"{f:F1}%";
                return value?.ToString() ?? UnavailableMetric;
            }
        }
        catch
        {
            // Fall through
        }

        return UnavailableMetric;
    }

    private string GetMemoryUsageMB()
    {
        try
        {
            ulong bytes = OS.GetStaticMemoryUsage();
            float mb = bytes / (1024f * 1024f);
            return $"{mb:F1} MB";
        }
        catch
        {
            return UnavailableMetric;
        }
    }
}
