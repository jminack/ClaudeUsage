using ClaudeUsageWidget.Models;
using ClaudeUsageWidget.Services;

namespace ClaudeUsageWidget;

/// <summary>
/// Pure data container: services, UI components, timers, and runtime state.
/// No behavior - just construction and disposal of owned resources.
/// </summary>
public class AppState : IDisposable
{
    private const string LogSource = "AppState";

    // Services
    public CredentialsService CredentialsService { get; }
    public UsageApiService UsageApiService { get; }
    public SettingsService SettingsService { get; }

    // UI Components
    public NotifyIcon TrayIcon { get; }
    public UsagePopupForm PopupForm { get; }

    // Timers
    public System.Windows.Forms.Timer PollTimer { get; }
    public System.Windows.Forms.Timer TooltipUpdateTimer { get; }

    // Runtime data
    public UsageResponse? LastUsageData { get; set; }
    public DateTime LastUpdated { get; set; }
    public bool IsRefreshing { get; set; }

    private bool _disposed;

    public AppState()
    {
        LoggingService.Info(LogSource, "Initializing AppState");

        // Initialize services
        LoggingService.Debug(LogSource, "Creating CredentialsService");
        CredentialsService = new CredentialsService();

        LoggingService.Debug(LogSource, "Creating UsageApiService");
        UsageApiService = new UsageApiService(CredentialsService);

        LoggingService.Debug(LogSource, "Creating SettingsService");
        SettingsService = new SettingsService();

        // Initialize UI components
        LoggingService.Debug(LogSource, "Creating UsagePopupForm");
        PopupForm = new UsagePopupForm(SettingsService);

        LoggingService.Debug(LogSource, "Creating NotifyIcon");
        TrayIcon = new NotifyIcon
        {
            Text = "Claude Usage: Loading...",
            Visible = true
        };

        // Initialize timers
        int pollInterval = SettingsService.Settings.PollIntervalMinutes * 60 * 1000;
        LoggingService.Debug(LogSource, $"Creating PollTimer with interval: {pollInterval}ms ({SettingsService.Settings.PollIntervalMinutes}min)");
        PollTimer = new System.Windows.Forms.Timer
        {
            Interval = pollInterval
        };

        LoggingService.Debug(LogSource, "Creating TooltipUpdateTimer with interval: 60000ms (1min)");
        TooltipUpdateTimer = new System.Windows.Forms.Timer
        {
            Interval = 60000 // Every minute
        };

        LoggingService.Info(LogSource, "AppState initialization complete");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        LoggingService.Info(LogSource, "Disposing AppState");

        LoggingService.Debug(LogSource, "Stopping and disposing timers");
        PollTimer.Stop();
        TooltipUpdateTimer.Stop();
        PollTimer.Dispose();
        TooltipUpdateTimer.Dispose();

        LoggingService.Debug(LogSource, "Disposing TrayIcon");
        TrayIcon.Visible = false;
        TrayIcon.Dispose();

        LoggingService.Debug(LogSource, "Disposing services");
        UsageApiService.Dispose();
        CredentialsService.Dispose();

        LoggingService.Debug(LogSource, "Disposing PopupForm");
        PopupForm.Dispose();

        LoggingService.Info(LogSource, "AppState disposal complete");
    }
}
