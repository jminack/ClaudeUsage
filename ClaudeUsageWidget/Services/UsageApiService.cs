using System.Net.Http.Headers;
using System.Text.Json;
using ClaudeUsageWidget.Models;

namespace ClaudeUsageWidget.Services;

public class UsageApiService : IDisposable
{
    private const string LogSource = "UsageApiService";
    private readonly HttpClient _httpClient;
    private readonly CredentialsService _credentialsService;
    private const string UsageEndpoint = "https://api.anthropic.com/api/oauth/usage";

    public UsageApiService(CredentialsService credentialsService)
    {
        _credentialsService = credentialsService;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("anthropic-beta", "oauth-2025-04-20");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        LoggingService.Info(LogSource, $"Initialized with endpoint: {UsageEndpoint}");
    }

    public async Task<UsageResponse?> GetUsageAsync()
    {
        LoggingService.Info(LogSource, "Fetching usage data");

        ClaudeOAuth? credentials = _credentialsService.GetCredentials();
        if (credentials == null)
        {
            LoggingService.Error(LogSource, "No credentials found");
            throw new InvalidOperationException("No credentials found. Please ensure Claude Code is authenticated.");
        }

        if (_credentialsService.IsTokenExpired())
        {
            LoggingService.Info(LogSource, "Token expired, attempting refresh");
            // Try refreshing the token automatically
            bool refreshed = await _credentialsService.RefreshTokenAsync();
            if (refreshed)
            {
                LoggingService.Info(LogSource, "Token refresh successful");
                credentials = _credentialsService.GetCredentials(forceRefresh: true);
            }
            else
            {
                // Refresh failed - session may have expired server-side
                LoggingService.Error(LogSource, "Token refresh failed");
                throw new InvalidOperationException("Token expired and refresh failed. Please re-authenticate by running 'claude' in your terminal.");
            }
        }

        if (credentials == null)
        {
            LoggingService.Error(LogSource, "No credentials after refresh");
            throw new InvalidOperationException("Failed to get credentials after refresh.");
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);

        LoggingService.Debug(LogSource, $"Sending GET request to: {UsageEndpoint}");
        HttpResponseMessage response = await _httpClient.GetAsync(UsageEndpoint);
        LoggingService.Debug(LogSource, $"Response status: {(int)response.StatusCode} {response.StatusCode}");

        // Handle 401 by attempting token refresh
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            LoggingService.Warning(LogSource, "Received 401 Unauthorized, attempting token refresh");
            bool refreshed = await _credentialsService.RefreshTokenAsync();
            if (refreshed)
            {
                credentials = _credentialsService.GetCredentials(forceRefresh: true);
                if (credentials != null)
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);
                    LoggingService.Debug(LogSource, "Retrying request with new token");
                    response = await _httpClient.GetAsync(UsageEndpoint);
                    LoggingService.Debug(LogSource, $"Retry response status: {(int)response.StatusCode} {response.StatusCode}");
                }
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync();
            LoggingService.Error(LogSource, $"API request failed: {(int)response.StatusCode} {response.StatusCode}");
            LoggingService.Debug(LogSource, $"Error response body: {errorBody}");
        }

        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        LoggingService.Debug(LogSource, $"Response body: {json}");

        UsageResponse? usage = JsonSerializer.Deserialize<UsageResponse>(json);

        if (usage != null)
        {
            LoggingService.Info(LogSource, $"Usage fetched: 5hr={usage.FiveHour?.Utilization:F1}%, 7day={usage.SevenDay?.Utilization:F1}%");
        }

        return usage;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
