using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;

namespace Sachya.PSN;

public partial class PSNClient : IDisposable
{
    private HttpClient _httpClient;
    private string _baseUrl;
    private string _accessToken;

    private static string GetNpServiceName(string platform) => platform.ToUpper() switch
    {
        "PS5" => "trophy2",
        _ => "trophy"
    };

    private async Task<T> SendRequestAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new PlaystationApiException($"API request failed with status code {response.StatusCode}.", response.StatusCode, errorContent);
        }

        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            return default(T)!;

        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<T>(content, SachyaJsonOptions.Default)!;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
        }
    }

    public async Task<UserTrophyTitlesResponse> GetUserTrophyTitlesAsync(string accountId, int limit = 200, int offset = 0, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/users/{accountId}/trophyTitles?limit={limit}&offset={offset}");
        return await SendRequestAsync<UserTrophyTitlesResponse>(request, cancellationToken);
    }

    public async Task<TitleTrophiesResponse> GetTitleTrophiesAsync(string npCommunicationId, string platform, string trophyGroupId = "all", string? acceptLanguage = null, CancellationToken cancellationToken = default)
    {
        string npServiceName = GetNpServiceName(platform);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/npCommunicationIds/{npCommunicationId}/trophyGroups/{trophyGroupId}/trophies?npServiceName={npServiceName}");

        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            request.Headers.AcceptLanguage.Clear();
            request.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
        }

        return await SendRequestAsync<TitleTrophiesResponse>(request, cancellationToken);
    }

    public async Task<UserEarnedTrophiesResponse> GetUserEarnedTrophiesAsync(string npCommunicationId, string platform, string accountId, string trophyGroupId = "all", CancellationToken cancellationToken = default)
    {
        string npServiceName = GetNpServiceName(platform);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/users/{accountId}/npCommunicationIds/{npCommunicationId}/trophyGroups/{trophyGroupId}/trophies?npServiceName={npServiceName}");
        return await SendRequestAsync<UserEarnedTrophiesResponse>(request, cancellationToken);
    }

    public async Task<TrophySummaryResponse> GetUserTrophySummaryAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/users/{accountId}/trophySummary");
        return await SendRequestAsync<TrophySummaryResponse>(request, cancellationToken);
    }

    public async Task<TitleTrophyGroupsResponse> GetTitleTrophyGroupsAsync(string npCommunicationId, string platform, CancellationToken cancellationToken = default)
    {
        string npServiceName = GetNpServiceName(platform);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/npCommunicationIds/{npCommunicationId}/trophyGroups?npServiceName={npServiceName}");
        return await SendRequestAsync<TitleTrophyGroupsResponse>(request, cancellationToken);
    }

    public async Task<UserEarnedTrophyGroupsResponse> GetUserEarnedTrophyGroupsAsync(string npCommunicationId, string platform, string accountId, CancellationToken cancellationToken = default)
    {
        string npServiceName = GetNpServiceName(platform);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/users/{accountId}/npCommunicationIds/{npCommunicationId}/trophyGroups?npServiceName={npServiceName}");
        return await SendRequestAsync<UserEarnedTrophyGroupsResponse>(request, cancellationToken);
    }

    public async Task<UserTitlesTrophySummaryResponse> GetUserTitlesTrophySummaryAsync(string accountId, string npCommunicationId, string platform, CancellationToken cancellationToken = default)
    {
        string npServiceName = GetNpServiceName(platform);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/users/{accountId}/npCommunicationIds/{npCommunicationId}/trophySummary?npServiceName={npServiceName}");
        return await SendRequestAsync<UserTitlesTrophySummaryResponse>(request, cancellationToken);
    }

    public async Task<TrophiesWithGameHelpResponse> GetTrophiesWithGameHelpAsync(string npCommunicationId, string platform, string trophyGroupId = "all", CancellationToken cancellationToken = default)
    {
        string npServiceName = GetNpServiceName(platform);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/npCommunicationIds/{npCommunicationId}/trophyGroups/{trophyGroupId}/trophies?npServiceName={npServiceName}&includeGameHelp=true");
        return await SendRequestAsync<TrophiesWithGameHelpResponse>(request, cancellationToken);
    }

    public async Task<GameHelpForTrophiesResponse> GetGameHelpForTrophiesAsync(string npCommunicationId, string platform, List<int> trophyIds, CancellationToken cancellationToken = default)
    {
        string npServiceName = GetNpServiceName(platform);
        var trophyIdsParam = string.Join(",", trophyIds);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/npCommunicationIds/{npCommunicationId}/gameHelp?npServiceName={npServiceName}&trophyIds={trophyIdsParam}");
        return await SendRequestAsync<GameHelpForTrophiesResponse>(request, cancellationToken);
    }
}
