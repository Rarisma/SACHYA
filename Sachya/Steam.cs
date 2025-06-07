using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
namespace Sachya;
public class SteamWebApiClient
{
    private readonly HttpClient _client;
    private readonly string _apiKey;

    /// <summary>
    /// Initialises a new Steam Web API client
    /// </summary>
    /// <param name="apiKey"></param>
    public SteamWebApiClient(string apiKey = null)
    {
        _client = new HttpClient();
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (X11; Linux x86_64; rv:136.0) Gecko/20100101 Firefox/136.0");
        _client.BaseAddress = new Uri("http://api.steampowered.com/");
        _apiKey = apiKey;
    }

    private async Task<T> GetAsync<T>(string url, string suffix = "&format=json")
    {
        int maxRetries = 3;
        int retryCount = 0;
        TimeSpan delay = TimeSpan.FromSeconds(1);
        TimeSpan maxDelay = TimeSpan.FromSeconds(30);
        Random jitter = new Random();

        while (true)
        {
            try
            {
                using HttpResponseMessage response = await _client.GetAsync(url + suffix).ConfigureAwait(false);
                
                // If we get a rate limit or server error, retry with backoff
                if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                {
                    if (retryCount >= maxRetries)
                        response.EnsureSuccessStatusCode(); // Will throw if we're out of retries
                    
                    retryCount++;
                    await Task.Delay(delay).ConfigureAwait(false);
                    
                    // Exponential backoff with jitter
                    delay = TimeSpan.FromMilliseconds(
                        Math.Min(maxDelay.TotalMilliseconds, delay.TotalMilliseconds * 2) * 
                        (0.8 + jitter.NextDouble() * 0.4));
                    
                    continue;
                }
                
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (HttpRequestException ex) when (retryCount < maxRetries && 
                                                (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests || 
                                                 (ex.StatusCode >= System.Net.HttpStatusCode.InternalServerError)))
            {
                retryCount++;
                await Task.Delay(delay).ConfigureAwait(false);
                
                // Exponential backoff with jitter
                delay = TimeSpan.FromMilliseconds(
                    Math.Min(maxDelay.TotalMilliseconds, delay.TotalMilliseconds * 2) * 
                    (0.8 + jitter.NextDouble() * 0.4));
            }
            catch (Exception) when (retryCount < maxRetries)
            {
                // For other exceptions that might be transient
                retryCount++;
                await Task.Delay(delay).ConfigureAwait(false);
                delay = TimeSpan.FromMilliseconds(Math.Min(maxDelay.TotalMilliseconds, delay.TotalMilliseconds * 2));
            }
        }
    }
    
    public Task<GameSchemaResult> GetSchemaForGameAsync(int appid)
    {
        string keyParam = !string.IsNullOrWhiteSpace(_apiKey) ? $"key={_apiKey}&" : "";
        var url = $"ISteamUserStats/GetSchemaForGame/v2/?{keyParam}appid={appid}";
        return GetAsync<GameSchemaResult>(url);
    }
    
    public Task<NewsForAppResult> GetNewsForAppAsync(int appid, int count = 3, int maxLength = 300)
    {
        var url = $"ISteamNews/GetNewsForApp/v0002/?appid={appid}&count={count}&maxlength={maxLength}";
        return GetAsync<NewsForAppResult>(url);
    }

    public Task<GlobalAchievementPercentagesResult> GetGlobalAchievementPercentagesForAppAsync(int gameid)
    {
        var url = $"ISteamUserStats/GetGlobalAchievementPercentagesForApp/v0002/?gameid={gameid}";
        return GetAsync<GlobalAchievementPercentagesResult>(url);
    }

    public Task<PlayerSummariesResult> GetPlayerSummariesAsync(string steamids)
    {
        string keyParam = !string.IsNullOrWhiteSpace(_apiKey) ? $"key={_apiKey}&" : "";
        var url = $"ISteamUser/GetPlayerSummaries/v0002/?{keyParam}steamids={steamids}";
        return GetAsync<PlayerSummariesResult>(url);
    }

    public Task<FriendListResult> GetFriendListAsync(string steamid, string relationship = "friend")
    {
        string keyParam = !string.IsNullOrWhiteSpace(_apiKey) ? $"key={_apiKey}&" : "";
        var url = $"ISteamUser/GetFriendList/v0001/?{keyParam}steamid={steamid}&relationship={relationship}";
        return GetAsync<FriendListResult>(url);
    }

    public Task<PlayerAchievementsResult> GetPlayerAchievementsAsync(string steamid, int appid, string language = null)
    {
        string keyParam = !string.IsNullOrWhiteSpace(_apiKey) ? $"key={_apiKey}&" : "";
        var url = $"ISteamUserStats/GetPlayerAchievements/v0001/?{keyParam}steamid={steamid}&appid={appid}";
        if (!string.IsNullOrWhiteSpace(language))
            url += $"&l={language}";
        return GetAsync<PlayerAchievementsResult>(url);
    }

    public Task<PlayerAchievementsResult> GetUserStatsForGameAsync(string steamid, int appid, string language = null)
    {
        // This endpoint returns similar structure as GetPlayerAchievements
        string keyParam = !string.IsNullOrWhiteSpace(_apiKey) ? $"key={_apiKey}&" : "";
        var url = $"ISteamUserStats/GetUserStatsForGame/v0002/?{keyParam}steamid={steamid}&appid={appid}";
        if (!string.IsNullOrWhiteSpace(language))
            url += $"&l={language}";
        return GetAsync<PlayerAchievementsResult>(url);
    }

    public Task<OwnedGamesResult> GetOwnedGamesAsync(string steamid, bool includeAppInfo = false, bool includePlayedFreeGames = false, int[] appidsFilter = null)
    {
        string keyParam = !string.IsNullOrWhiteSpace(_apiKey) ? $"key={_apiKey}&" : "";
        var url = $"IPlayerService/GetOwnedGames/v0001/?{keyParam}steamid={steamid}&include_appinfo={includeAppInfo}&include_played_free_games={includePlayedFreeGames}";
        if (appidsFilter != null && appidsFilter.Length > 0)
        {
            var jsonFilter = JsonSerializer.Serialize(new { appids_filter = appidsFilter });
            url += $"&input_json={Uri.EscapeDataString(jsonFilter)}";
        }
        return GetAsync<OwnedGamesResult>(url);
    }

    public Task<RecentlyPlayedGamesResult> GetRecentlyPlayedGamesAsync(string steamid, int count = 0)
    {
        string keyParam = !string.IsNullOrWhiteSpace(_apiKey) ? $"key={_apiKey}&" : "";
        var url = $"IPlayerService/GetRecentlyPlayedGames/v0001/?{keyParam}steamid={steamid}";
        if (count > 0)
            url += $"&count={count}";
        return GetAsync<RecentlyPlayedGamesResult>(url);
    }
}

//Mapping Classes.
#region Model Classes
/// <summary>
/// Response from the Steam ResolveVanityURL API
/// </summary>
public class VanityUrlResponse
{
    [JsonPropertyName("response")]
    public VanityUrlResult? response { get; set; }

    public class VanityUrlResult
    {
        [JsonPropertyName("success")]
        public int success { get; set; }

        [JsonPropertyName("steamid")]
        public string? steamid { get; set; }

        [JsonPropertyName("message")]
        public string? message { get; set; }
    }
}
// GetNewsForApp
public class NewsForAppResult
{
    public AppNews appnews { get; set; }
}

public class AppNews
{
    public int appid { get; set; }
    public int count { get; set; }
    public List<NewsItem> newsitems { get; set; }
}

public class NewsItem
{
    public string gid { get; set; }
    public string title { get; set; }
    public string url { get; set; }
    public bool is_external_url { get; set; }
    public string author { get; set; }
    public string contents { get; set; }
    public string feedlabel { get; set; }
    public long date { get; set; }
    public string feedname { get; set; }
    public int feed_type { get; set; }
    public int appid { get; set; }
    public List<string> tags { get; set; }
}
// GetGlobalAchievementPercentagesForApp
public class GlobalAchievementPercentagesResult
{
    public AchievementPercentages achievementpercentages { get; set; }
}


// GetPlayerSummaries
public class PlayerSummariesResult
{
    public PlayerSummariesResponse response { get; set; }
}

public class PlayerSummariesResponse
{
    public List<Player> players { get; set; }
}

public class Player
{
    public string steamid { get; set; }
    public string personaname { get; set; }
    public string profileurl { get; set; }
    public string avatar { get; set; }
    public string avatarmedium { get; set; }
    public string avatarfull { get; set; }
    public int personastate { get; set; }
    public int communityvisibilitystate { get; set; }
    public int profilestate { get; set; }
    public long lastlogoff { get; set; }
    public int commentpermission { get; set; }
    // Private fields (if available)
    public string realname { get; set; }
    public string primaryclanid { get; set; }
    public long timecreated { get; set; }
    public int gameid { get; set; }
    public string gameserverip { get; set; }
    public string gameextrainfo { get; set; }
    public int cityid { get; set; }
    public string loccountrycode { get; set; }
    public string locstatecode { get; set; }
    public int loccityid { get; set; }
}

// GetFriendList
public class FriendListResult
{
    public FriendListResponse friendslist { get; set; }
}

public class FriendListResponse
{
    public List<Friend> friends { get; set; }
}

public class Friend
{
    public string steamid { get; set; }
    public string relationship { get; set; }
    public long friend_since { get; set; }
}

// GetPlayerAchievements / GetUserStatsForGame
public class PlayerAchievementsResult
{
    public PlayerStats playerstats { get; set; }
}

public class PlayerStats
{
    public string steamID { get; set; }
    public string gameName { get; set; }
    public bool success { get; set; }
    public List<PlayerAchievement> achievements { get; set; }
}

public class PlayerAchievement
{
    public string apiname { get; set; }
    public int achieved { get; set; }
    public long unlocktime { get; set; }
    public string name { get; set; }
    public string description { get; set; }
}

// GetOwnedGames
public class OwnedGamesResult
{
    public OwnedGamesResponse response { get; set; }
}

public class OwnedGamesResponse
{
    public int game_count { get; set; }
    public List<Game> games { get; set; }
}

public class Game
{
    public int appid { get; set; }
    public string name { get; set; }
    public int playtime_forever { get; set; }
    public int playtime_2weeks { get; set; }
    public string img_icon_url { get; set; }
    public string img_logo_url { get; set; }
    public bool has_community_visible_stats { get; set; }
}

// GetRecentlyPlayedGames
public class RecentlyPlayedGamesResult
{
    public RecentlyPlayedGamesResponse response { get; set; }
}

public class RecentlyPlayedGamesResponse
{
    public int total_count { get; set; }
    public List<Game> games { get; set; }
}
public class GameSchemaResult
{
    public GameSchema game { get; set; }
}

public class GameSchema
{
    public AvailableGameStats availableGameStats { get; set; }
}

public class AvailableGameStats
{
    public List<AchievementDefinition> achievements { get; set; }
}

public class AchievementDefinition
{
    public string name { get; set; }
    public string displayName { get; set; }
    public string description { get; set; }
    public string icon { get; set; }
    
    public int hidden { get; set; }
    public string icongray { get; set; }
}

public class AchievementPercentagesResult
{
    public AchievementPercentages achievementpercentages { get; set; }
}

public class AchievementPercentages
{
    public List<GlobalAchievement> achievements { get; set; }
}

public class GlobalAchievement
{
    public string name { get; set; }

    [JsonConverter(typeof(StringToFloatConverter))]
    public float percent { get; set; }
}

public class StringToFloatConverter : JsonConverter<float>
{
    public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // If the value is a string, attempt to parse it as a float.
        if (reader.TokenType == JsonTokenType.String)
        {
            string strValue = reader.GetString();
            if (float.TryParse(strValue, out float value))
            {
                return value;
            }
            throw new JsonException($"Unable to convert \"{strValue}\" to float.");
        }
        // Otherwise, assume it's already a number.
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetSingle();
        }
        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
#endregion