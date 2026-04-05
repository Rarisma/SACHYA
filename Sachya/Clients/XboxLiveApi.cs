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

        // Try v2 with orderBy (modern Xbox One/Series titles)
        var v2Result = await TryGetAchievementsAsync(titleId, maxItems, contractVersion: "2",
            extraParams: "&unearned=true&orderBy=unlockTime");
        if (v2Result.Achievements.Count > 0)
        {
            _logger.LogDebug("Found {Count} achievements for title {TitleId} via v2", v2Result.Achievements.Count, titleId);
            return v2Result;
        }

        // Try v2 without orderBy — some 360 titles work with v2 but choke on orderBy=unlockTime
        var v2SimpleResult = await TryGetAchievementsAsync(titleId, maxItems, contractVersion: "2",
            extraParams: "&unearned=true");
        if (v2SimpleResult.Achievements.Count > 0)
        {
            _logger.LogDebug("Found {Count} achievements for title {TitleId} via v2 (no orderBy)", v2SimpleResult.Achievements.Count, titleId);
            return v2SimpleResult;
        }

        // Fallback to v1 (Xbox 360) — returns only unlocked achievements
        _logger.LogDebug("v2 returned empty for title {TitleId}, trying v1 (Xbox 360) contract", titleId);
        var v1Result = await TryGetAchievementsV1Async(titleId, maxItems);
        if (v1Result.Achievements.Count > 0)
        {
            _logger.LogDebug("Found {Count} achievements for title {TitleId} via v1 (unlocked only)", v1Result.Achievements.Count, titleId);
            return v1Result;
        }

        _logger.LogDebug("No achievements found for title {TitleId} from any contract version", titleId);
        return new XboxAchievementsResponse { Achievements = new List<XboxAchievement>() };
    }

    /// <summary>
    /// Tries to fetch achievements using v2 contract format (XboxAchievement model).
    /// </summary>
    private async Task<XboxAchievementsResponse> TryGetAchievementsAsync(
        string titleId, int maxItems, string contractVersion, string extraParams = "")
    {
        var url = $"{AchievementsBaseUrl}/users/xuid({_authService.Xuid})/achievements?titleId={titleId}&maxItems={maxItems}{extraParams}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Clear();
        request.Headers.Add("Authorization", $"XBL3.0 x={_authService.UserHash};{_authService.XstsToken}");
        request.Headers.Add("x-xbl-contract-version", contractVersion);
        request.Headers.Add("Accept-Language", "en-US");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogDebug("v{Version} achievements request failed for title {TitleId}: {StatusCode}",
                contractVersion, titleId, response.StatusCode);
            return new XboxAchievementsResponse { Achievements = new List<XboxAchievement>() };
        }

        var json = await response.Content.ReadAsStringAsync();
        try
        {
            return JsonSerializer.Deserialize<XboxAchievementsResponse>(json, SachyaJsonOptions.Default)
                   ?? new XboxAchievementsResponse { Achievements = new List<XboxAchievement>() };
        }
        catch (JsonException ex)
        {
            // v2 model doesn't fit this response (likely a 360 title returning v1-style JSON)
            _logger.LogDebug(ex, "v{Version} deserialization failed for title {TitleId}, likely wrong format",
                contractVersion, titleId);
            return new XboxAchievementsResponse { Achievements = new List<XboxAchievement>() };
        }
    }

    private async Task<XboxAchievementsResponse> TryGetAchievementsV1Async(string titleId, int maxItems)
    {
        var url = $"{AchievementsBaseUrl}/users/xuid({_authService.Xuid})/achievements?titleId={titleId}&maxItems={maxItems}&unlockedOnly=false";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Clear();
        request.Headers.Add("Authorization", $"XBL3.0 x={_authService.UserHash};{_authService.XstsToken}");
        request.Headers.Add("x-xbl-contract-version", "1");
        request.Headers.Add("Accept-Language", "en-US");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogDebug("v1 achievements request failed for title {TitleId}: {StatusCode}", titleId, response.StatusCode);
            return new XboxAchievementsResponse { Achievements = new List<XboxAchievement>() };
        }

        var json = await response.Content.ReadAsStringAsync();

        // v1 response has different field types (int id, bool unlocked, etc.)
        // Deserialize with v1 model and convert to v2 format
        try
        {
            var v1Response = JsonSerializer.Deserialize<Xbox360AchievementsResponse>(json, SachyaJsonOptions.Default)
                             ?? new Xbox360AchievementsResponse();

            if (v1Response.Achievements.Count == 0)
                return new XboxAchievementsResponse { Achievements = new List<XboxAchievement>() };

            // Convert v1 achievements to v2 format
            var converted = v1Response.Achievements.Select(a => new XboxAchievement
            {
                Id = a.Id.ToString(),
                Name = a.Name,
                Description = a.Description,
                LockedDescription = a.LockedDescription,
                IsSecret = a.IsSecret,
                ProgressState = a.Unlocked ? "Achieved" : "NotStarted",
                Progression = a.TimeUnlocked.HasValue && a.Unlocked
                    ? new XboxProgression { TimeUnlocked = a.TimeUnlocked }
                    : null,
                Rewards = new List<XboxReward>
                {
                    new() { Type = "Gamerscore", Value = a.Gamerscore.ToString() }
                },
                // Xbox 360 achievement images use hex titleId and hex imageId
                MediaAssets = a.ImageId > 0 && long.TryParse(titleId, out var titleIdNum)
                    ? new List<XboxMediaAsset>
                    {
                        new()
                        {
                            Type = "Icon",
                            Url = $"http://image.xboxlive.com/global/t.{titleIdNum:X8}/ach/0/{a.ImageId:X}"
                        }
                    }
                    : null
            }).ToList();

            return new XboxAchievementsResponse { Achievements = converted };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize v1 achievements for title {TitleId}", titleId);
            return new XboxAchievementsResponse { Achievements = new List<XboxAchievement>() };
        }
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

}
