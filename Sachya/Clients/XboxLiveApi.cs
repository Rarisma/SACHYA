using System.Text.Json;
using System.Text.Json.Serialization;
using Sachya.Definitions.Xbox;

namespace Sachya.Clients;

public class XboxLiveApiClient
{
    private readonly HttpClient _httpClient;
    private readonly XboxAuthService _authService;
    
    // Xbox Live API endpoints
    private const string ProfileBaseUrl = "https://profile.xboxlive.com";
    private const string TitleHubBaseUrl = "https://titlehub.xboxlive.com";
    private const string AchievementsBaseUrl = "https://achievements.xboxlive.com";
    private const string UserStatsBaseUrl = "https://userstats.xboxlive.com";
    
    public XboxLiveApiClient(XboxAuthService authService, HttpClient? httpClient = null)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <summary>
    /// Gets the user's profile data
    /// </summary>
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
        return JsonSerializer.Deserialize<XboxProfileResponse>(json, GetJsonOptions()) 
               ?? new XboxProfileResponse();
    }

    /// <summary>
    /// Gets the user's game library with achievement statistics
    /// </summary>
    public async Task<XboxTitleHistoryResponse> GetTitleHistoryAsync()
    {
        if (string.IsNullOrEmpty(_authService.Xuid))
            throw new InvalidOperationException("Not authenticated - XUID is missing");

        // This endpoint returns games with achievement summaries
        var url = $"{TitleHubBaseUrl}/users/xuid({_authService.Xuid})/titles/titlehistory/decoration/achievement,stats";
        
        var request = _authService.PrepareAuthenticatedRequest(HttpMethod.Get, url);
        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to get title history: {response.StatusCode} - {content}");
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<XboxTitleHistoryResponse>(json, GetJsonOptions()) 
               ?? new XboxTitleHistoryResponse { Titles = new List<XboxTitle>() };
    }

    /// <summary>
    /// Gets detailed achievements for a specific game
    /// </summary>
    public async Task<XboxAchievementsResponse> GetAchievementsAsync(string titleId, int maxItems = 1000)
    {
        if (string.IsNullOrEmpty(_authService.Xuid))
            throw new InvalidOperationException("Not authenticated - XUID is missing");

        // Check if this is an Xbox 360 title to use appropriate API version
        bool isXbox360 = IsXbox360Title(titleId);
        
        // Build URL - for Xbox 360, use unlockedOnly=false to get ALL achievements
        var url = isXbox360 
            ? $"{AchievementsBaseUrl}/users/xuid({_authService.Xuid})/achievements?titleId={titleId}&maxItems={maxItems}&unlockedOnly=false"
            : $"{AchievementsBaseUrl}/users/xuid({_authService.Xuid})/achievements?titleId={titleId}&maxItems={maxItems}&unearned=true&orderBy=unlockTime";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Clear();
        request.Headers.Add("Authorization", $"XBL3.0 x={_authService.UserHash};{_authService.XstsToken}");
        
        // CRITICAL: Use API v1 for Xbox 360 titles, v2 for modern titles
        request.Headers.Add("x-xbl-contract-version", isXbox360 ? "1" : "2");
        request.Headers.Add("Accept-Language", "en-US");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        
        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[XboxLiveApi] Failed to get achievements for title {titleId}. Status: {response.StatusCode}");
            Console.WriteLine($"[XboxLiveApi] Response content: {content}");
            Console.WriteLine($"[XboxLiveApi] Request URL: {url}");
            throw new HttpRequestException($"Failed to get achievements for title {titleId}: {response.StatusCode} - {content}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<XboxAchievementsResponse>(json, GetJsonOptions()) 
               ?? new XboxAchievementsResponse { Achievements = new List<XboxAchievement>() };
        
        if (result.Achievements == null || result.Achievements.Count == 0)
        {
            Console.WriteLine($"[XboxLiveApi] No achievements found for title {titleId}");
            Console.WriteLine($"[XboxLiveApi] Response JSON length: {json.Length} characters");
            if (json.Length < 1000)
            {
                Console.WriteLine($"[XboxLiveApi] Response JSON: {json}");
            }
        }
        else
        {
            Console.WriteLine($"[XboxLiveApi] Successfully found {result.Achievements.Count} achievements for title {titleId}");
            // Log first achievement details to debug
            if (result.Achievements.Count > 0)
            {
                var first = result.Achievements[0];
                Console.WriteLine($"[XboxLiveApi] First achievement - Name: '{first.Name ?? "null"}', Desc: '{first.Description ?? "null"}', ID: '{first.Id ?? "null"}'");
                Console.WriteLine($"[XboxLiveApi] ImageLocked URL: '{first.ImageLocked?.Url ?? "null"}', ImageUnlocked URL: '{first.ImageUnlocked?.Url ?? "null"}'");
                
                // Log a sample of the raw JSON to see what's actually being returned
                if (json.Length > 0)
                {
                    var sample = json.Length > 500 ? json.Substring(0, 500) + "..." : json;
                    Console.WriteLine($"[XboxLiveApi] Raw JSON sample: {sample}");
                }
            }
        }
        
        return result;
    }

    /// <summary>
    /// Gets game-specific statistics (like playtime)
    /// </summary>
    public async Task<XboxUserStatsResponse> GetUserStatsAsync(string scid)
    {
        if (string.IsNullOrEmpty(_authService.Xuid))
            throw new InvalidOperationException("Not authenticated - XUID is missing");

        var url = $"{UserStatsBaseUrl}/users/xuid({_authService.Xuid})/scids/{scid}/stats";
        
        var request = _authService.PrepareAuthenticatedRequest(HttpMethod.Get, url);
        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            // Stats endpoint may not be available for all games
            return new XboxUserStatsResponse { Groups = new List<XboxStatGroup>() };
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<XboxUserStatsResponse>(json, GetJsonOptions()) 
               ?? new XboxUserStatsResponse { Groups = new List<XboxStatGroup>() };
    }

    /// <summary>
    /// Determines if a title ID belongs to an Xbox 360 game
    /// </summary>
    private bool IsXbox360Title(string titleId)
    {
        // Xbox 360 title IDs are typically 8 hex characters (e.g., "584111F7" for Banjo-Kazooie)
        // Xbox One/Series titles are typically 9-10 digit numbers
        
        if (string.IsNullOrEmpty(titleId))
            return false;
        
        // Check if it's a hex string (Xbox 360 format)
        bool isHex = titleId.Length == 8 && 
                     titleId.All(c => (c >= '0' && c <= '9') || 
                                     (c >= 'A' && c <= 'F') || 
                                     (c >= 'a' && c <= 'f'));
        
        if (isHex)
        {
            Console.WriteLine($"[XboxLiveApi] Title {titleId} identified as Xbox 360 (hex format)");
            return true;
        }
        
        // Some Xbox 360 titles might use numeric IDs in certain ranges
        if (long.TryParse(titleId, out long numericId))
        {
            // Xbox 360 numeric IDs are typically in specific ranges
            bool is360Range = numericId < 1000000000; // Xbox One/Series titles typically start at 1000000000+
            if (is360Range)
            {
                Console.WriteLine($"[XboxLiveApi] Title {titleId} identified as Xbox 360 (numeric range)");
                return true;
            }
        }
        
        Console.WriteLine($"[XboxLiveApi] Title {titleId} identified as Xbox One/Series");
        return false;
    }
    
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
}

// Response models for Xbox Live API
public class XboxProfileResponse
{
    [JsonPropertyName("profileUsers")]
    public List<XboxProfileUser> ProfileUsers { get; set; } = new();
}

public class XboxProfileUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty; // XUID
    
    [JsonPropertyName("settings")]
    public List<XboxProfileSetting> Settings { get; set; } = new();
}

public class XboxProfileSetting
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class XboxTitleHistoryResponse
{
    [JsonPropertyName("titles")]
    public List<XboxTitle> Titles { get; set; } = new();
}

public class XboxTitle
{
    [JsonPropertyName("titleId")]
    public string TitleId { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("displayImage")]
    public string? DisplayImage { get; set; }
    
    [JsonPropertyName("modernTitleId")]
    public string? ModernTitleId { get; set; }
    
    [JsonPropertyName("achievement")]
    public XboxTitleAchievementInfo? Achievement { get; set; }
    
    [JsonPropertyName("titleHistory")]
    public XboxTitleHistoryInfo? TitleHistory { get; set; }
    
    [JsonPropertyName("stats")]
    public XboxTitleStats? Stats { get; set; }
}

public class XboxTitleAchievementInfo
{
    [JsonPropertyName("currentAchievements")]
    public int CurrentAchievements { get; set; }
    
    [JsonPropertyName("totalAchievements")]
    public int TotalAchievements { get; set; }
    
    [JsonPropertyName("currentGamerscore")]
    public int CurrentGamerscore { get; set; }
    
    [JsonPropertyName("totalGamerscore")]
    public int TotalGamerscore { get; set; }
    
    [JsonPropertyName("progressPercentage")]
    public double ProgressPercentage { get; set; }
}

public class XboxTitleHistoryInfo  
{
    [JsonPropertyName("lastTimePlayed")]
    public DateTime? LastTimePlayed { get; set; }
    
    [JsonPropertyName("visible")]
    public bool Visible { get; set; }
}

public class XboxTitleStats
{
    [JsonPropertyName("sourceVersion")]
    public int SourceVersion { get; set; }
}

public class XboxAchievementsResponse
{
    [JsonPropertyName("achievements")]
    public List<XboxAchievement> Achievements { get; set; } = new();
    
    [JsonPropertyName("pagingInfo")]
    public XboxPagingInfo? PagingInfo { get; set; }
}

public class XboxAchievement
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("serviceConfigId")]
    public string? ServiceConfigId { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("lockedDescription")]
    public string? LockedDescription { get; set; }
    
    [JsonPropertyName("unlockedDescription")]
    public string? UnlockedDescription { get; set; }
    
    [JsonPropertyName("imageUnlocked")]
    public XboxMediaAsset? ImageUnlocked { get; set; }
    
    [JsonPropertyName("imageLocked")]  
    public XboxMediaAsset? ImageLocked { get; set; }
    
    [JsonPropertyName("isSecret")]
    public bool IsSecret { get; set; }
    
    [JsonPropertyName("rewards")]
    public List<XboxReward>? Rewards { get; set; }
    
    [JsonPropertyName("rarity")]
    public XboxRarity? Rarity { get; set; }
    
    [JsonPropertyName("progressState")]
    public string? ProgressState { get; set; } // "Achieved", "NotStarted", "InProgress"
    
    [JsonPropertyName("progression")]
    public XboxProgression? Progression { get; set; }
}

public class XboxMediaAsset
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("width")]
    public int Width { get; set; }
    
    [JsonPropertyName("height")]
    public int Height { get; set; }
}

public class XboxReward
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string? Type { get; set; } // "Gamerscore"
}

public class XboxRarity
{
    [JsonPropertyName("currentCategory")]
    public string? CurrentCategory { get; set; } // "Rare", "Common", etc.
    
    [JsonPropertyName("currentPercentage")]
    public double CurrentPercentage { get; set; }
}

public class XboxProgression
{
    [JsonPropertyName("requirements")]
    public List<XboxRequirement>? Requirements { get; set; }
    
    [JsonPropertyName("timeUnlocked")]
    public DateTime? TimeUnlocked { get; set; }
}

public class XboxRequirement
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("current")]
    public string? Current { get; set; }
    
    [JsonPropertyName("target")]
    public string? Target { get; set; }
}

public class XboxPagingInfo
{
    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }
    
    [JsonPropertyName("totalRecords")]
    public int TotalRecords { get; set; }
}

public class XboxUserStatsResponse
{
    [JsonPropertyName("groups")]
    public List<XboxStatGroup> Groups { get; set; } = new();
}

public class XboxStatGroup
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("titleId")]
    public string? TitleId { get; set; }
    
    [JsonPropertyName("statlistscollection")]
    public List<XboxStatCollection>? StatListsCollection { get; set; }
}

public class XboxStatCollection
{
    [JsonPropertyName("stats")]
    public List<XboxStat> Stats { get; set; } = new();
}

public class XboxStat
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}