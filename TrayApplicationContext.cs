using ClaudeUsageWidget.Models;
using ClaudeUsageWidget.Services;

namespace ClaudeUsageWidget;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly CredentialsService _credentialsService;
    private readonly UsageApiService _usageApiService;
    private readonly SettingsService _settingsService;
    private readonly UsagePopupForm _popupForm;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private readonly System.Windows.Forms.Timer _tooltipUpdateTimer;

    private UsageResponse? _lastUsageData;
    private DateTime _lastUpdated;
    private bool _isRefreshing;

    public TrayApplicationContext()
    {
        _credentialsService = new CredentialsService();
        _usageApiService = new UsageApiService(_credentialsService);
        _settingsService = new SettingsService();
        _popupForm = new UsagePopupForm(_settingsService);

        // Create context menu
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Refresh Now", null, async (s, e) => await RefreshUsageAsync());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Settings", null, (s, e) => ShowSettings());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

        // Create tray icon
        _trayIcon = new NotifyIcon
        {
            Icon = CreateDefaultIcon(),
            Text = "Claude Usage: Loading...",
            Visible = true,
            ContextMenuStrip = contextMenu
        };

        _trayIcon.Click += TrayIcon_Click;
        _trayIcon.DoubleClick += TrayIcon_DoubleClick;

        // Poll timer
        _pollTimer = new System.Windows.Forms.Timer
        {
            Interval = _settingsService.Settings.PollIntervalMinutes * 60 * 1000
        };
        _pollTimer.Tick += async (s, e) => await RefreshUsageAsync();

        // Tooltip update timer (updates "X minutes ago" display)
        _tooltipUpdateTimer = new System.Windows.Forms.Timer
        {
            Interval = 60000 // Every minute
        };
        _tooltipUpdateTimer.Tick += (s, e) => UpdateTooltip();

        // Subscribe to settings changes
        _settingsService.SettingsChanged += (s, e) =>
        {
            _pollTimer.Interval = _settingsService.Settings.PollIntervalMinutes * 60 * 1000;
        };

        // Initial fetch
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await RefreshUsageAsync();
        _pollTimer.Start();
        _tooltipUpdateTimer.Start();
    }

    private async Task RefreshUsageAsync()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;

        try
        {
            _lastUsageData = await _usageApiService.GetUsageAsync();
            _lastUpdated = DateTime.Now;

            UpdateTooltip();
            _popupForm.UpdateUsage(_lastUsageData, _lastUpdated);

            // Check for alerts
            CheckAlerts();
        }
        catch (Exception ex)
        {
            _trayIcon.Text = $"Claude Usage: Error - {ex.Message}";
            _lastUsageData = null;
            _popupForm.UpdateUsage(null, DateTime.Now);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void UpdateTooltip()
    {
        if (_lastUsageData == null)
        {
            _trayIcon.Text = "Claude Usage: No data";
            return;
        }

        var sessionPercent = (int)(_lastUsageData.FiveHour?.Utilization ?? 0);
        var weeklyPercent = (int)(_lastUsageData.SevenDay?.Utilization ?? 0);

        // NotifyIcon.Text is limited to 63 characters
        var tooltip = $"Claude: Session {sessionPercent}% | Week {weeklyPercent}%";
        _trayIcon.Text = tooltip.Length > 63 ? tooltip[..63] : tooltip;

        // Update icon color based on usage
        _trayIcon.Icon = CreateUsageIcon(Math.Max(sessionPercent, weeklyPercent));
    }

    private void CheckAlerts()
    {
        if (!_settingsService.Settings.AlertEnabled || _lastUsageData == null) return;

        var sessionPercent = (int)(_lastUsageData.FiveHour?.Utilization ?? 0);
        var weeklyPercent = (int)(_lastUsageData.SevenDay?.Utilization ?? 0);
        var threshold = _settingsService.Settings.AlertThresholdPercent;

        // Reset alert flag when window resets
        _settingsService.ResetAlertForNewWindow(_lastUsageData.FiveHour?.ResetsAt);

        // Show alert if threshold exceeded and not already shown
        if (!_settingsService.Settings.AlertShownForCurrentWindow)
        {
            if (sessionPercent >= threshold)
            {
                _trayIcon.ShowBalloonTip(
                    5000,
                    "Claude Usage Alert",
                    $"Session usage at {sessionPercent}%! Resets at {_lastUsageData.FiveHour?.ResetsAt?.ToLocalTime():h:mm tt}",
                    ToolTipIcon.Warning);
                _settingsService.MarkAlertShown();
            }
            else if (weeklyPercent >= threshold)
            {
                _trayIcon.ShowBalloonTip(
                    5000,
                    "Claude Usage Alert",
                    $"Weekly usage at {weeklyPercent}%! Resets {_lastUsageData.SevenDay?.ResetsAt?.ToLocalTime():ddd h:mm tt}",
                    ToolTipIcon.Warning);
                _settingsService.MarkAlertShown();
            }
        }
    }

    private void TrayIcon_Click(object? sender, EventArgs e)
    {
        if (e is MouseEventArgs me && me.Button == MouseButtons.Left)
        {
            _popupForm.UpdateUsage(_lastUsageData, _lastUpdated);
            _popupForm.ShowNearCursor();
        }
    }

    private void TrayIcon_DoubleClick(object? sender, EventArgs e)
    {
        _ = RefreshUsageAsync();
    }

    private void ShowSettings()
    {
        using var settingsForm = new SettingsForm(_settingsService);
        settingsForm.ShowDialog();
    }

    private void ExitApplication()
    {
        _pollTimer.Stop();
        _tooltipUpdateTimer.Stop();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _usageApiService.Dispose();
        _popupForm.Dispose();
        Application.Exit();
    }

    // Claude brand colors
    private static readonly Color ClaudeTerracotta = Color.FromArgb(217, 119, 87);  // #D97757 - Claude's signature color
    private static readonly Color ClaudeCoral = Color.FromArgb(232, 131, 99);       // Lighter variant
    private static readonly Color ClaudeWarning = Color.FromArgb(245, 158, 66);     // Warning orange
    private static readonly Color ClaudeCritical = Color.FromArgb(220, 53, 69);     // Critical red

    private Icon CreateDefaultIcon()
    {
        return CreateUsageIcon(0);
    }

    private Icon CreateUsageIcon(int usagePercent)
    {
        var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Background
        g.Clear(Color.Transparent);

        // Determine color based on usage
        Color fillColor;
        if (usagePercent >= 90)
            fillColor = ClaudeCritical;
        else if (usagePercent >= 70)
            fillColor = ClaudeWarning;
        else
            fillColor = ClaudeTerracotta;

        // Draw Claude's sparkle/asterisk logo
        DrawClaudeSparkle(g, fillColor, 8, 8, 6);

        return Icon.FromHandle(bitmap.GetHicon());
    }

    private void DrawClaudeSparkle(Graphics g, Color color, float cx, float cy, float radius)
    {
        using var brush = new SolidBrush(color);
        using var pen = new Pen(color, 2.2f);
        pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
        pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

        // Draw 4-pointed sparkle (Claude's logo shape)
        // Vertical line
        g.DrawLine(pen, cx, cy - radius, cx, cy + radius);
        // Horizontal line
        g.DrawLine(pen, cx - radius, cy, cx + radius, cy);
        // Diagonal lines (shorter)
        var diagRadius = radius * 0.7f;
        g.DrawLine(pen, cx - diagRadius, cy - diagRadius, cx + diagRadius, cy + diagRadius);
        g.DrawLine(pen, cx + diagRadius, cy - diagRadius, cx - diagRadius, cy + diagRadius);

        // Center dot
        g.FillEllipse(brush, cx - 2, cy - 2, 4, 4);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pollTimer.Dispose();
            _tooltipUpdateTimer.Dispose();
            _trayIcon.Dispose();
            _usageApiService.Dispose();
            _popupForm.Dispose();
        }
        base.Dispose(disposing);
    }
}
