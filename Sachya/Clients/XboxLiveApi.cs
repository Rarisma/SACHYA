using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sachya.Definitions.Xbox;

namespace Sachya.Clients;

public class XboxLiveApiClient
{
    private readonly HttpClient _httpClient;
    private readonly XboxAuthService _authService;
    private readonly ILogger<XboxLiveApiClient> _logger;

    private const string ProfileBaseUrl = "https://profile.xboxlive.com";
    private const string TitleHubBaseUrl = "https://titlehub.xboxlive.com";
    private const string AchievementsBaseUrl = "https://achievements.xboxlive.com";
    private const string UserStatsBaseUrl = "https://userstats.xboxlive.com";

    public XboxLiveApiClient(XboxAuthService authService, HttpClient httpClient, ILogger<XboxLiveApiClient>? logger = null)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? NullLogger<XboxLiveApiClient>.Instance;
    }

    public async Task<XboxProfileResponse> GetProfileAsync()
    {
        if (string.IsNullOrEmpty(_authService.Xuid))
            throw new InvalidOperationException("Not authenticated - XUID is missing");

        var url = $"{ProfileBaseUrl}/users/xuid({_authService.Xuid})/profile/settings?settings=Gamertag,Gamerscore,GameDisplayPicRaw,AccountTier,XboxOneRep,PreferredColor,RealName,Bio,Location,ModernGamertag,ModernGamertagSuffix,UniqueModernGamertag,RealNameOverride,TenureLevel,Watermarks";

        var request = _authService.PrepareAuthenticatedRequest(HttpMethod.Get, url);
        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to get profile: {response.StatusCode} - {content}");
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<XboxProfileResponse>(json, SachyaJsonOptions.Default)
               ?? new XboxProfileResponse();
    }

    public async Task<XboxTitleHistoryResponse> GetTitleHistoryAsync()
    {
        if (string.IsNullOrEmpty(_authService.Xuid))
            throw new InvalidOperationException("Not authenticated - XUID is missing");

        var url = $"{TitleHubBaseUrl}/users/xuid({_authService.Xuid})/titles/titlehistory/decoration/achievement,stats";

        var request = _authService.PrepareAuthenticatedRequest(HttpMethod.Get, url);
        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to get title history: {response.StatusCode} - {content}");
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<XboxTitleHistoryResponse>(json, SachyaJsonOptions.Default)
               ?? new XboxTitleHistoryResponse { Titles = new List<XboxTitle>() };
    }

    public async Task<XboxAchievementsResponse> GetAchievementsAsync(string titleId, int maxItems = 1000)
    {
        if (string.IsNullOrEmpty(_authService.Xuid))
            throw new InvalidOperationException("Not authenticated - XUID is missing");

        bool isXbox360 = IsXbox360Title(titleId);

        var url = isXbox360
            ? $"{AchievementsBaseUrl}/users/xuid({_authService.Xuid})/achievements?titleId={titleId}&maxItems={maxItems}&unlockedOnly=false"
            : $"{AchievementsBaseUrl}/users/xuid({_authService.Xuid})/achievements?titleId={titleId}&maxItems={maxItems}&unearned=true&orderBy=unlockTime";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Clear();
        request.Headers.Add("Authorization", $"XBL3.0 x={_authService.UserHash};{_authService.XstsToken}");
        request.Headers.Add("x-xbl-contract-version", isXbox360 ? "1" : "2");
        request.Headers.Add("Accept-Language", "en-US");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to get achievements for title {TitleId}. Status: {StatusCode}, URL: {Url}", titleId, response.StatusCode, url);
            throw new HttpRequestException($"Failed to get achievements for title {titleId}: {response.StatusCode} - {content}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<XboxAchievementsResponse>(json, SachyaJsonOptions.Default)
               ?? new XboxAchievementsResponse { Achievements = new List<XboxAchievement>() };

        if (result.Achievements == null || result.Achievements.Count == 0)
        {
            _logger.LogDebug("No achievements found for title {TitleId} (response length: {Length})", titleId, json.Length);
        }
        else
        {
            _logger.LogDebug("Found {Count} achievements for title {TitleId}", result.Achievements.Count, titleId);
        }

        return result;
    }

    public async Task<XboxUserStatsResponse> GetUserStatsAsync(string scid)
    {
        if (string.IsNullOrEmpty(_authService.Xuid))
            throw new InvalidOperationException("Not authenticated - XUID is missing");

        var url = $"{UserStatsBaseUrl}/users/xuid({_authService.Xuid})/scids/{scid}/stats";

        var request = _authService.PrepareAuthenticatedRequest(HttpMethod.Get, url);
        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return new XboxUserStatsResponse { Groups = new List<XboxStatGroup>() };

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<XboxUserStatsResponse>(json, SachyaJsonOptions.Default)
               ?? new XboxUserStatsResponse { Groups = new List<XboxStatGroup>() };
    }

    private bool IsXbox360Title(string titleId)
    {
        if (string.IsNullOrEmpty(titleId))
            return false;

        bool isHex = titleId.Length == 8 &&
                     titleId.All(c => (c >= '0' && c <= '9') ||
                                     (c >= 'A' && c <= 'F') ||
                                     (c >= 'a' && c <= 'f'));

        if (isHex)
        {
            _logger.LogDebug("Title {TitleId} identified as Xbox 360 (hex format)", titleId);
            return true;
        }

        if (long.TryParse(titleId, out long numericId))
        {
            bool is360Range = numericId < 1000000000;
            if (is360Range)
            {
                _logger.LogDebug("Title {TitleId} identified as Xbox 360 (numeric range)", titleId);
                return true;
            }
        }

        _logger.LogDebug("Title {TitleId} identified as Xbox One/Series", titleId);
        return false;
    }
}
