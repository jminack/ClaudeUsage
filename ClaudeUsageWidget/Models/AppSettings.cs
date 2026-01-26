using System.Text.Json.Serialization;

namespace ClaudeUsageWidget.Models;

public class AppSettings
{
    [JsonPropertyName("pollIntervalMinutes")]
    public int PollIntervalMinutes { get; set; } = 5;

    [JsonPropertyName("alertThresholdPercent")]
    public int AlertThresholdPercent { get; set; } = 90;

    [JsonPropertyName("alertEnabled")]
    public bool AlertEnabled { get; set; } = true;

    [JsonPropertyName("alertShownForCurrentWindow")]
    public bool AlertShownForCurrentWindow { get; set; } = false;

    [JsonPropertyName("lastAlertResetTime")]
    public DateTimeOffset? LastAlertResetTime { get; set; }
}
