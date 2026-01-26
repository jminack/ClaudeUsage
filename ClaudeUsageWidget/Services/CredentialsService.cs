using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClaudeUsageWidget.Models;

namespace ClaudeUsageWidget.Services;

public class CredentialsService : IDisposable
{
    private const string LogSource = "CredentialsService";
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
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _credentialsPath = Path.Combine(userProfile, ".claude", ".credentials.json");
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        LoggingService.Info(LogSource, $"Initialized with credentials path: {_credentialsPath}");
    }

    public ClaudeOAuth? GetCredentials(bool forceRefresh = false)
    {
        LoggingService.Debug(LogSource, $"GetCredentials called (forceRefresh={forceRefresh})");

        if (!forceRefresh && _cachedCredentials != null && DateTime.Now - _lastRead < _cacheExpiry)
        {
            LoggingService.Debug(LogSource, "Returning cached credentials");
            return _cachedCredentials;
        }

        try
        {
            if (!File.Exists(_credentialsPath))
            {
                LoggingService.Warning(LogSource, $"Credentials file not found: {_credentialsPath}");
                return null;
            }

            LoggingService.Debug(LogSource, "Reading credentials from file");
            string json = File.ReadAllText(_credentialsPath);
            CredentialsFile? credentialsFile = JsonSerializer.Deserialize<CredentialsFile>(json);
            _cachedCredentials = credentialsFile?.ClaudeAiOauth;
            _lastRead = DateTime.Now;

            if (_cachedCredentials != null)
            {
                DateTimeOffset expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(_cachedCredentials.ExpiresAt);
                LoggingService.Info(LogSource, $"Credentials loaded successfully, expires at: {expiresAt:yyyy-MM-dd HH:mm:ss}");
            }
            else
            {
                LoggingService.Warning(LogSource, "Credentials file exists but ClaudeAiOauth section is missing");
            }

            return _cachedCredentials;
        }
        catch (Exception ex)
        {
            LoggingService.Exception(LogSource, ex, "Failed to read credentials file");
            return null;
        }
    }

    public bool IsTokenExpired()
    {
        LoggingService.Debug(LogSource, "Checking if token is expired");
        ClaudeOAuth? credentials = GetCredentials();
        if (credentials == null)
        {
            LoggingService.Debug(LogSource, "No credentials found, treating as expired");
            return true;
        }

        DateTimeOffset expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(credentials.ExpiresAt);
        // Consider expired if within 5 minutes of expiry to avoid edge cases
        bool isExpired = DateTimeOffset.Now >= expiresAt.AddMinutes(-5);
        LoggingService.Debug(LogSource, $"Token expires at: {expiresAt:yyyy-MM-dd HH:mm:ss}, isExpired={isExpired}");
        return isExpired;
    }

    public async Task<bool> RefreshTokenAsync()
    {
        LoggingService.Info(LogSource, "Starting token refresh");

        ClaudeOAuth? credentials = GetCredentials(forceRefresh: true);
        if (credentials == null || string.IsNullOrEmpty(credentials.RefreshToken))
        {
            LoggingService.Warning(LogSource, "Cannot refresh: no credentials or refresh token");
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

            string jsonContent = JsonSerializer.Serialize(requestBody);
            StringContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            LoggingService.Debug(LogSource, $"Sending refresh token request to: {TokenEndpoint}");
            HttpResponseMessage response = await _httpClient.PostAsync(TokenEndpoint, content);

            LoggingService.Debug(LogSource, $"Refresh response status: {(int)response.StatusCode} {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                LoggingService.Error(LogSource, $"Token refresh failed: {(int)response.StatusCode} {response.StatusCode}");
                LoggingService.Debug(LogSource, $"Refresh error response body: {errorBody}");
                return false;
            }

            string responseJson = await response.Content.ReadAsStringAsync();
            LoggingService.Debug(LogSource, "Parsing token refresh response");
            TokenRefreshResponse? tokenResponse = JsonSerializer.Deserialize<TokenRefreshResponse>(responseJson);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                LoggingService.Error(LogSource, "Token refresh response is invalid or missing access token");
                return false;
            }

            // Update credentials with new tokens
            credentials.AccessToken = tokenResponse.AccessToken;

            if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
            {
                LoggingService.Debug(LogSource, "New refresh token received");
                credentials.RefreshToken = tokenResponse.RefreshToken;
            }

            if (tokenResponse.ExpiresIn > 0)
            {
                credentials.ExpiresAt = DateTimeOffset.Now.AddSeconds(tokenResponse.ExpiresIn).ToUnixTimeMilliseconds();
                DateTimeOffset newExpiry = DateTimeOffset.FromUnixTimeMilliseconds(credentials.ExpiresAt);
                LoggingService.Info(LogSource, $"Token refreshed successfully, new expiry: {newExpiry:yyyy-MM-dd HH:mm:ss}");
            }

            // Save updated credentials to file
            return await SaveCredentialsAsync(credentials);
        }
        catch (Exception ex)
        {
            LoggingService.Exception(LogSource, ex, "Token refresh failed with exception");
            return false;
        }
    }

    private async Task<bool> SaveCredentialsAsync(ClaudeOAuth credentials)
    {
        LoggingService.Debug(LogSource, "Saving credentials to file");

        try
        {
            // Read the existing file to preserve any other fields
            string existingJson = await File.ReadAllTextAsync(_credentialsPath);
            CredentialsFile credentialsFile = JsonSerializer.Deserialize<CredentialsFile>(existingJson) ?? new CredentialsFile();

            credentialsFile.ClaudeAiOauth = credentials;

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string newJson = JsonSerializer.Serialize(credentialsFile, options);

            await File.WriteAllTextAsync(_credentialsPath, newJson);

            // Update cache
            _cachedCredentials = credentials;
            _lastRead = DateTime.Now;

            LoggingService.Info(LogSource, "Credentials saved successfully");
            return true;
        }
        catch (Exception ex)
        {
            LoggingService.Exception(LogSource, ex, "Failed to save credentials");
            return false;
        }
    }

    public string GetCredentialsPath() => _credentialsPath;

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
