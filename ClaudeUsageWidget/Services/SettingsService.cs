using System.Text.Json;
using ClaudeUsageWidget.Models;

namespace ClaudeUsageWidget.Services;

public class SettingsService
{
    private readonly string _settingsPath;
    private AppSettings _settings;

    public event EventHandler? SettingsChanged;

    public SettingsService()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolder = Path.Combine(appData, "ClaudeUsageWidget");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");
        _settings = Load();
    }

    public AppSettings Settings => _settings;

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                string json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Return default settings on error
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // Silently fail on save error
        }
    }

    public void UpdateAlertThreshold(int percent)
    {
        _settings.AlertThresholdPercent = Math.Clamp(percent, 50, 100);
        Save();
    }

    public void UpdatePollInterval(int minutes)
    {
        _settings.PollIntervalMinutes = Math.Clamp(minutes, 1, 60);
        Save();
    }

    public void SetAlertEnabled(bool enabled)
    {
        _settings.AlertEnabled = enabled;
        Save();
    }

    public void ResetAlertForNewWindow(DateTimeOffset? resetTime)
    {
        if (_settings.LastAlertResetTime != resetTime)
        {
            _settings.AlertShownForCurrentWindow = false;
            _settings.LastAlertResetTime = resetTime;
            Save();
        }
    }

    public void MarkAlertShown()
    {
        _settings.AlertShownForCurrentWindow = true;
        Save();
    }
}
