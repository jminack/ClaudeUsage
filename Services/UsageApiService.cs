using System.Net.Http.Headers;
using System.Text.Json;
using ClaudeUsageWidget.Models;

namespace ClaudeUsageWidget.Services;

public class UsageApiService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly CredentialsService _credentialsService;
    private const string UsageEndpoint = "https://api.anthropic.com/api/oauth/usage";

    public UsageApiService(CredentialsService credentialsService)
    {
        _credentialsService = credentialsService;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("anthropic-beta", "oauth-2025-04-20");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<UsageResponse?> GetUsageAsync()
    {
        var credentials = _credentialsService.GetCredentials();
        if (credentials == null)
        {
            throw new InvalidOperationException("No credentials found. Please ensure Claude Code is authenticated.");
        }

        if (_credentialsService.IsTokenExpired())
        {
            // Try refreshing credentials from file (Claude Code may have refreshed them)
            credentials = _credentialsService.GetCredentials(forceRefresh: true);
            if (credentials == null || _credentialsService.IsTokenExpired())
            {
                throw new InvalidOperationException("Token expired. Please re-authenticate in Claude Code.");
            }
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);

        var response = await _httpClient.GetAsync(UsageEndpoint);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UsageResponse>(json);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
