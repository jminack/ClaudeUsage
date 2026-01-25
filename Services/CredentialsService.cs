using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClaudeUsageWidget.Models;

namespace ClaudeUsageWidget.Services;

public class CredentialsService : IDisposable
{
    private readonly string _credentialsPath;
    private ClaudeOAuth? _cachedCredentials;
    private DateTime _lastRead = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromSeconds(30);
    private readonly HttpClient _httpClient;

    // Claude Code's official OAuth client ID
    private const string ClientId = "9d1c250a-e61b-44d9-88ed-5944d1962f5e";
    private const string TokenEndpoint = "https://console.anthropic.com/api/oauth/token";

    public CredentialsService()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _credentialsPath = Path.Combine(userProfile, ".claude", ".credentials.json");
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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
        // Consider expired if within 5 minutes of expiry to avoid edge cases
        return DateTimeOffset.Now >= expiresAt.AddMinutes(-5);
    }

    public async Task<bool> RefreshTokenAsync()
    {
        var credentials = GetCredentials(forceRefresh: true);
        if (credentials == null || string.IsNullOrEmpty(credentials.RefreshToken))
        {
            return false;
        }

        try
        {
            var requestBody = new
            {
                grant_type = "refresh_token",
                refresh_token = credentials.RefreshToken,
                client_id = ClientId
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(TokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenRefreshResponse>(responseJson);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                return false;
            }

            // Update credentials with new tokens
            credentials.AccessToken = tokenResponse.AccessToken;

            if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
            {
                credentials.RefreshToken = tokenResponse.RefreshToken;
            }

            if (tokenResponse.ExpiresIn > 0)
            {
                credentials.ExpiresAt = DateTimeOffset.Now.AddSeconds(tokenResponse.ExpiresIn).ToUnixTimeMilliseconds();
            }

            // Save updated credentials to file
            return await SaveCredentialsAsync(credentials);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<bool> SaveCredentialsAsync(ClaudeOAuth credentials)
    {
        try
        {
            // Read the existing file to preserve any other fields
            var existingJson = await File.ReadAllTextAsync(_credentialsPath);
            var credentialsFile = JsonSerializer.Deserialize<CredentialsFile>(existingJson) ?? new CredentialsFile();

            credentialsFile.ClaudeAiOauth = credentials;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var newJson = JsonSerializer.Serialize(credentialsFile, options);

            await File.WriteAllTextAsync(_credentialsPath, newJson);

            // Update cache
            _cachedCredentials = credentials;
            _lastRead = DateTime.Now;

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public string GetCredentialsPath() => _credentialsPath;

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
