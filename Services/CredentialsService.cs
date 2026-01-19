using System.Text.Json;
using ClaudeUsageWidget.Models;

namespace ClaudeUsageWidget.Services;

public class CredentialsService
{
    private readonly string _credentialsPath;
    private ClaudeOAuth? _cachedCredentials;
    private DateTime _lastRead = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromSeconds(30);

    public CredentialsService()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _credentialsPath = Path.Combine(userProfile, ".claude", ".credentials.json");
    }

    public ClaudeOAuth? GetCredentials(bool forceRefresh = false)
    {
        if (!forceRefresh && _cachedCredentials != null && DateTime.Now - _lastRead < _cacheExpiry)
        {
            return _cachedCredentials;
        }

        try
        {
            if (!File.Exists(_credentialsPath))
            {
                return null;
            }

            var json = File.ReadAllText(_credentialsPath);
            var credentialsFile = JsonSerializer.Deserialize<CredentialsFile>(json);
            _cachedCredentials = credentialsFile?.ClaudeAiOauth;
            _lastRead = DateTime.Now;
            return _cachedCredentials;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public bool IsTokenExpired()
    {
        var credentials = GetCredentials();
        if (credentials == null) return true;

        var expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(credentials.ExpiresAt);
        return DateTimeOffset.Now >= expiresAt;
    }

    public string GetCredentialsPath() => _credentialsPath;
}
