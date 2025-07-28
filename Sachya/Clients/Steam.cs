using System.Text.Json;
using Sachya.Definitions.Steam;

namespace Sachya.Clients;
public class SteamWebApiClient
{
    private readonly HttpClient _client;
    private readonly string _apiKey;

    /// <summary>
    /// Initialises a new Steam Web API client
    /// </summary>
    /// <param name="apiKey"></param>
    public SteamWebApiClient(string apiKey)
    {
        _client = new HttpClient();
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (X11; Linux x86_64; rv:136.0) Gecko/20100101 Firefox/136.0");
        _client.BaseAddress = new Uri("https://api.steampowered.com/");
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

    public Task<PlayerAchievementsResult> GetPlayerAchievementsAsync(string steamid, int appid, string? language = null)
    {
        string keyParam = !string.IsNullOrWhiteSpace(_apiKey) ? $"key={_apiKey}&" : "";
        var url = $"ISteamUserStats/GetPlayerAchievements/v0001/?{keyParam}steamid={steamid}&appid={appid}";
        if (!string.IsNullOrWhiteSpace(language))
            url += $"&l={language}";
        return GetAsync<PlayerAchievementsResult>(url);
    }

    public Task<PlayerAchievementsResult> GetUserStatsForGameAsync(string steamid, int appid, string? language = null)
    {
        // This endpoint returns similar structure as GetPlayerAchievements
        string keyParam = !string.IsNullOrWhiteSpace(_apiKey) ? $"key={_apiKey}&" : "";
        var url = $"ISteamUserStats/GetUserStatsForGame/v0002/?{keyParam}steamid={steamid}&appid={appid}";
        if (!string.IsNullOrWhiteSpace(language))
            url += $"&l={language}";
        return GetAsync<PlayerAchievementsResult>(url);
    }

    public Task<OwnedGamesResult> GetOwnedGamesAsync(string steamid, bool includeAppInfo = false,
        bool includePlayedFreeGames = false, int[]? appidsFilter = null)
    {
        string keyParam = !string.IsNullOrWhiteSpace(_apiKey) ? $"key={_apiKey}&" : "";
        var url = $"IPlayerService/GetOwnedGames/v0001/?{keyParam}steamid={steamid}" +
                  $"&include_appinfo={includeAppInfo}&include_played_free_games={includePlayedFreeGames}";
        
        //set appid filter
        if (appidsFilter != null && appidsFilter.Length != 0)
        {
            for (int index = 0; index < appidsFilter.Length; index++) { url += $"&appids_filter[{index}]={appidsFilter[index]}"; }
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

    /// <summary>
    /// Resolves a Steam vanity URL to a Steam ID.
    /// </summary>
    /// <param name="vanityUrl">The user's vanity URL name.</param>
    /// <returns>A VanityUrlResponse containing the resolution result.</returns>
    public async Task<VanityUrlResponse> ResolveVanityUrlAsync(string vanityUrl)
    {
        string keyParam = !string.IsNullOrWhiteSpace(_apiKey) ? $"key={_apiKey}&" : "";
        var url = $"ISteamUser/ResolveVanityURL/v1/?{keyParam}vanityurl={vanityUrl}";
        return await GetAsync<VanityUrlResponse>(url);
    }
}
