using ClaudeUsageWidget.Services;

namespace ClaudeUsageWidget;

/// <summary>
/// Orchestrates application behavior: polling, refreshing, alerts, and UI updates.
/// </summary>
public class UsageController
{
    private const string LogSource = "UsageController";
    private readonly AppState _state;

    public UsageController(AppState state)
    {
        _state = state;
        LoggingService.Debug(LogSource, "UsageController created");
    }

    public void Initialize()
    {
        LoggingService.Info(LogSource, "Initializing UsageController");

        // Configure tray icon
        LoggingService.Debug(LogSource, "Configuring tray icon");
        _state.TrayIcon.Icon = CreateDefaultIcon();
        _state.TrayIcon.ContextMenuStrip = CreateContextMenu();
        _state.TrayIcon.Click += TrayIcon_Click;
        _state.TrayIcon.DoubleClick += TrayIcon_DoubleClick;

        // Wire up timer events
        LoggingService.Debug(LogSource, "Wiring up timer events");
        _state.PollTimer.Tick += async (s, e) => await RefreshUsageAsync();
        _state.TooltipUpdateTimer.Tick += (s, e) => UpdateTooltip();

        // Subscribe to settings changes
        _state.SettingsService.SettingsChanged += (s, e) =>
        {
            int newInterval = _state.SettingsService.Settings.PollIntervalMinutes * 60 * 1000;
            LoggingService.Info(LogSource, $"Settings changed, updating poll interval to {newInterval}ms");
            _state.PollTimer.Interval = newInterval;
        };

        // Initial fetch and start timers
        LoggingService.Debug(LogSource, "Starting initial fetch and timers");
        _ = InitializeAsync();
    }

    private ContextMenuStrip CreateContextMenu()
    {
        LoggingService.Debug(LogSource, "Creating context menu");
        ContextMenuStrip contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Refresh Now", null, async (s, e) =>
        {
            LoggingService.Info(LogSource, "Manual refresh requested from context menu");
            await RefreshUsageAsync();
        });
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Settings", null, (s, e) =>
        {
            LoggingService.Info(LogSource, "Settings requested from context menu");
            ShowSettings();
        });
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, (s, e) =>
        {
            LoggingService.Info(LogSource, "Exit requested from context menu");
            OnExitRequested?.Invoke();
        });
        return contextMenu;
    }

    public event Action? OnExitRequested;

    private async Task InitializeAsync()
    {
        LoggingService.Info(LogSource, "InitializeAsync starting");
        await RefreshUsageAsync();

        LoggingService.Debug(LogSource, "Starting PollTimer");
        _state.PollTimer.Start();

        LoggingService.Debug(LogSource, "Starting TooltipUpdateTimer");
        _state.TooltipUpdateTimer.Start();

        LoggingService.Info(LogSource, "InitializeAsync complete");
    }

    public async Task RefreshUsageAsync()
    {
        if (_state.IsRefreshing)
        {
            LoggingService.Debug(LogSource, "RefreshUsageAsync skipped - already refreshing");
            return;
        }
        _state.IsRefreshing = true;

        LoggingService.Info(LogSource, "RefreshUsageAsync starting");

        try
        {
            _state.LastUsageData = await _state.UsageApiService.GetUsageAsync();
            _state.LastUpdated = DateTime.Now;

            LoggingService.Debug(LogSource, "Updating tooltip and popup form");
            UpdateTooltip();
            _state.PopupForm.UpdateUsage(_state.LastUsageData, _state.LastUpdated);

            CheckAlerts();

            LoggingService.Info(LogSource, "RefreshUsageAsync completed successfully");
        }
        catch (Exception ex)
        {
            LoggingService.Exception(LogSource, ex, "RefreshUsageAsync failed");
            _state.TrayIcon.Text = $"Claude Usage: Error - {ex.Message}";
            _state.LastUsageData = null;
            _state.PopupForm.UpdateUsage(null, DateTime.Now);
        }
        finally
        {
            _state.IsRefreshing = false;
        }
    }

    private void UpdateTooltip()
    {
        if (_state.LastUsageData == null)
        {
            LoggingService.Debug(LogSource, "UpdateTooltip: No data available");
            _state.TrayIcon.Text = "Claude Usage: No data";
            return;
        }

        int sessionPercent = (int)(_state.LastUsageData.FiveHour?.Utilization ?? 0);
        int weeklyPercent = (int)(_state.LastUsageData.SevenDay?.Utilization ?? 0);

        // NotifyIcon.Text is limited to 63 characters
        string tooltip = $"Claude: Session {sessionPercent}% | Week {weeklyPercent}%";
        _state.TrayIcon.Text = tooltip.Length > 63 ? tooltip[..63] : tooltip;

        LoggingService.Debug(LogSource, $"UpdateTooltip: Session={sessionPercent}%, Weekly={weeklyPercent}%");

        // Update icon color based on usage
        _state.TrayIcon.Icon = CreateUsageIcon(Math.Max(sessionPercent, weeklyPercent));
    }

    private void CheckAlerts()
    {
        if (!_state.SettingsService.Settings.AlertEnabled || _state.LastUsageData == null)
        {
            LoggingService.Debug(LogSource, "CheckAlerts: Alerts disabled or no data");
            return;
        }

        int sessionPercent = (int)(_state.LastUsageData.FiveHour?.Utilization ?? 0);
        int weeklyPercent = (int)(_state.LastUsageData.SevenDay?.Utilization ?? 0);
        int threshold = _state.SettingsService.Settings.AlertThresholdPercent;

        LoggingService.Debug(LogSource, $"CheckAlerts: Session={sessionPercent}%, Weekly={weeklyPercent}%, Threshold={threshold}%");

        // Reset alert flag when window resets
        _state.SettingsService.ResetAlertForNewWindow(_state.LastUsageData.FiveHour?.ResetsAt);

        // Show alert if threshold exceeded and not already shown
        if (!_state.SettingsService.Settings.AlertShownForCurrentWindow)
        {
            if (sessionPercent >= threshold)
            {
                LoggingService.Warning(LogSource, $"Session usage alert triggered: {sessionPercent}% >= {threshold}%");
                _state.TrayIcon.ShowBalloonTip(
                    5000,
                    "Claude Usage Alert",
                    $"Session usage at {sessionPercent}%! Resets at {_state.LastUsageData.FiveHour?.ResetsAt?.ToLocalTime():h:mm tt}",
                    ToolTipIcon.Warning);
                _state.SettingsService.MarkAlertShown();
            }
            else if (weeklyPercent >= threshold)
            {
                LoggingService.Warning(LogSource, $"Weekly usage alert triggered: {weeklyPercent}% >= {threshold}%");
                _state.TrayIcon.ShowBalloonTip(
                    5000,
                    "Claude Usage Alert",
                    $"Weekly usage at {weeklyPercent}%! Resets {_state.LastUsageData.SevenDay?.ResetsAt?.ToLocalTime():ddd h:mm tt}",
                    ToolTipIcon.Warning);
                _state.SettingsService.MarkAlertShown();
            }
        }
    }

    private void TrayIcon_Click(object? sender, EventArgs e)
    {
        if (e is MouseEventArgs me && me.Button == MouseButtons.Left)
        {
            LoggingService.Debug(LogSource, "Tray icon left-clicked, showing popup");
            _state.PopupForm.UpdateUsage(_state.LastUsageData, _state.LastUpdated);
            _state.PopupForm.ShowNearCursor();
        }
    }

    private void TrayIcon_DoubleClick(object? sender, EventArgs e)
    {
        LoggingService.Info(LogSource, "Tray icon double-clicked, refreshing usage");
        _ = RefreshUsageAsync();
    }

    private void ShowSettings()
    {
        LoggingService.Info(LogSource, "Opening settings dialog");
        using SettingsForm settingsForm = new SettingsForm(_state.SettingsService);
        settingsForm.ShowDialog();
        LoggingService.Debug(LogSource, "Settings dialog closed");
    }

    // Claude brand colors
    private static readonly Color ClaudeTerracotta = Color.FromArgb(217, 119, 87);
    private static readonly Color ClaudeWarning = Color.FromArgb(245, 158, 66);
    private static readonly Color ClaudeCritical = Color.FromArgb(220, 53, 69);

    private static Icon CreateDefaultIcon()
    {
        return CreateUsageIcon(0);
    }

    private static Icon CreateUsageIcon(int usagePercent)
    {
        Bitmap bitmap = new Bitmap(16, 16);
        using Graphics g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        g.Clear(Color.Transparent);

        Color fillColor;
        if (usagePercent >= 90)
            fillColor = ClaudeCritical;
        else if (usagePercent >= 70)
            fillColor = ClaudeWarning;
        else
            fillColor = ClaudeTerracotta;

        DrawClaudeSparkle(g, fillColor, 8, 8, 6);

        return Icon.FromHandle(bitmap.GetHicon());
    }

    private static void DrawClaudeSparkle(Graphics g, Color color, float cx, float cy, float radius)
    {
        using SolidBrush brush = new SolidBrush(color);
        using Pen pen = new Pen(color, 2.2f);
        pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
        pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

        g.DrawLine(pen, cx, cy - radius, cx, cy + radius);
        g.DrawLine(pen, cx - radius, cy, cx + radius, cy);
        float diagRadius = radius * 0.7f;
        g.DrawLine(pen, cx - diagRadius, cy - diagRadius, cx + diagRadius, cy + diagRadius);
        g.DrawLine(pen, cx + diagRadius, cy - diagRadius, cx - diagRadius, cy + diagRadius);

        g.FillEllipse(brush, cx - 2, cy - 2, 4, 4);
    }
}
