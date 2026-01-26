using ClaudeUsageWidget.Models;
using ClaudeUsageWidget.Services;

namespace ClaudeUsageWidget;

/// <summary>
/// Pure data container: services, UI components, timers, and runtime state.
/// No behavior - just construction and disposal of owned resources.
/// </summary>
public class AppState : IDisposable
{
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

    public AppState()
    {
        // Initialize services
        CredentialsService = new CredentialsService();
        UsageApiService = new UsageApiService(CredentialsService);
        SettingsService = new SettingsService();

        // Initialize UI components
        PopupForm = new UsagePopupForm(SettingsService);
        TrayIcon = new NotifyIcon
        {
            Text = "Claude Usage: Loading...",
            Visible = true
        };

        // Initialize timers
        PollTimer = new System.Windows.Forms.Timer
        {
            Interval = SettingsService.Settings.PollIntervalMinutes * 60 * 1000
        };

        TooltipUpdateTimer = new System.Windows.Forms.Timer
        {
            Interval = 60000 // Every minute
        };
    }

    public void Dispose()
    {
        PollTimer.Stop();
        TooltipUpdateTimer.Stop();
        PollTimer.Dispose();
        TooltipUpdateTimer.Dispose();
        TrayIcon.Visible = false;
        TrayIcon.Dispose();
        UsageApiService.Dispose();
        CredentialsService.Dispose();
        PopupForm.Dispose();
    }
}
