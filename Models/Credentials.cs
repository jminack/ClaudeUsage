using System.Text.Json.Serialization;

namespace ClaudeUsageWidget.Models;

public class CredentialsFile
{
    [JsonPropertyName("claudeAiOauth")]
    public ClaudeOAuth? ClaudeAiOauth { get; set; }
}

public class ClaudeOAuth
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("expiresAt")]
    public long ExpiresAt { get; set; }

    [JsonPropertyName("scopes")]
    public List<string> Scopes { get; set; } = new();

    [JsonPropertyName("subscriptionType")]
    public string SubscriptionType { get; set; } = string.Empty;

    [JsonPropertyName("rateLimitTier")]
    public string RateLimitTier { get; set; } = string.Empty;
}
