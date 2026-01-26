using System.Text.Json;
using ClaudeUsageWidget.Models;

namespace ClaudeUsageWidget.Services;

public class SettingsService
{
    private const string LogSource = "SettingsService";
    private readonly string _settingsPath;
    private AppSettings _settings;

    public event EventHandler? SettingsChanged;

    public SettingsService()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolder = Path.Combine(appData, "ClaudeUsageWidget");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");

        LoggingService.Info(LogSource, $"Settings path: {_settingsPath}");
        _settings = Load();
    }

    public AppSettings Settings => _settings;

    private AppSettings Load()
    {
        LoggingService.Debug(LogSource, "Loading settings");

        try
        {
            if (File.Exists(_settingsPath))
            {
                string json = File.ReadAllText(_settingsPath);
                AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    LoggingService.Info(LogSource, $"Settings loaded: PollInterval={settings.PollIntervalMinutes}min, AlertThreshold={settings.AlertThresholdPercent}%, AlertEnabled={settings.AlertEnabled}");
                    return settings;
                }
            }
            else
            {
                LoggingService.Info(LogSource, "Settings file not found, using defaults");
            }
        }
        catch (Exception ex)
        {
            LoggingService.Exception(LogSource, ex, "Failed to load settings");
        }

        AppSettings defaults = new AppSettings();
        LoggingService.Info(LogSource, $"Using default settings: PollInterval={defaults.PollIntervalMinutes}min, AlertThreshold={defaults.AlertThresholdPercent}%, AlertEnabled={defaults.AlertEnabled}");
        return defaults;
    }

    public void Save()
    {
        LoggingService.Debug(LogSource, "Saving settings");

        try
        {
            string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
            LoggingService.Info(LogSource, "Settings saved successfully");
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            LoggingService.Exception(LogSource, ex, "Failed to save settings");
        }
    }

    public void UpdateAlertThreshold(int percent)
    {
        int clamped = Math.Clamp(percent, 50, 100);
        LoggingService.Info(LogSource, $"Updating alert threshold: {_settings.AlertThresholdPercent}% -> {clamped}%");
        _settings.AlertThresholdPercent = clamped;
        Save();
    }

    public void UpdatePollInterval(int minutes)
    {
        int clamped = Math.Clamp(minutes, 1, 60);
        LoggingService.Info(LogSource, $"Updating poll interval: {_settings.PollIntervalMinutes}min -> {clamped}min");
        _settings.PollIntervalMinutes = clamped;
        Save();
    }

    public void SetAlertEnabled(bool enabled)
    {
        LoggingService.Info(LogSource, $"Setting alert enabled: {_settings.AlertEnabled} -> {enabled}");
        _settings.AlertEnabled = enabled;
        Save();
    }

    public void ResetAlertForNewWindow(DateTimeOffset? resetTime)
    {
        if (_settings.LastAlertResetTime != resetTime)
        {
            LoggingService.Debug(LogSource, $"Resetting alert for new window (reset time: {resetTime})");
            _settings.AlertShownForCurrentWindow = false;
            _settings.LastAlertResetTime = resetTime;
            Save();
        }
    }

    public void MarkAlertShown()
    {
        LoggingService.Debug(LogSource, "Marking alert as shown for current window");
        _settings.AlertShownForCurrentWindow = true;
        Save();
    }
}
