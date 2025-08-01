using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Sachya.Definitions.RetroAchievements;

namespace Sachya.Clients;

/// <summary>
/// Client for interacting with the RetroAchievements Web and Connect APIs.
/// </summary>
public class RetroAchievements : IDisposable
{
    private const string WebApiBaseUrl = "https://retroachievements.org/API/";
    private const string ConnectApiBaseUrl = "https://retroachievements.org/dorequest.php";

    private readonly HttpClient _httpClient;
    private readonly string _userName;
    private readonly string _webApiKey;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    // Required for Connect API calls
    public string? ConnectApiUserAgent { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RetroAchievementsApiClient"/> class.
    /// </summary>
    /// <param name="userName">Your RetroAchievements username.</param>
    /// <param name="webApiKey">Your RetroAchievements Web API Key.</param>
    /// <param name="connectApiUserAgent">Required User-Agent string for Connect API calls (e.g., "YourApp/1.0 (Platform) Integration/1.0").</param>
    /// <param name="httpClient">Optional custom HttpClient instance.</param>
    public RetroAchievements(string userName, string webApiKey, string? connectApiUserAgent = null, HttpClient? httpClient = null)
    {
        _userName = userName ?? throw new ArgumentNullException(nameof(userName));
        _webApiKey = webApiKey ?? throw new ArgumentNullException(nameof(webApiKey));
        ConnectApiUserAgent = connectApiUserAgent; // Can be null if only using Web API

        // Use provided HttpClient or create a new one
        _httpClient = httpClient ?? new HttpClient();
        // BaseAddress is set per request type (Web vs Connect)

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            // Add converters here if needed for specific types (like dates if default parsing fails)
        };
        // Example: How to add a custom date converter if needed
        // _jsonSerializerOptions.Converters.Add(new CustomDateTimeConverter());
    }

    private string BuildUrl(string baseUrl, string endpoint, Dictionary<string, string?>? queryParams = null)
    {
        var builder = new UriBuilder(baseUrl);
        var query = HttpUtility.ParseQueryString(builder.Query);

        if (queryParams != null)
        {
            foreach (var kvp in queryParams)
            {
                if (kvp.Value != null)
                {
                    query[kvp.Key] = kvp.Value;
                }
            }
        }

        // Append endpoint if it's the Web API
        if (baseUrl == WebApiBaseUrl)
        {
             builder.Path += endpoint;
        }


        builder.Query = query.ToString();
        return builder.ToString();
    }

    private async Task<T> GetApiAsync<T>(string baseUrl, string endpoint, Dictionary<string, string?> queryParams, CancellationToken cancellationToken = default)
    {
        // Add Web API authentication
        if (baseUrl == WebApiBaseUrl)
        {
            // Web API uses z=user, y=key
            // queryParams["z"] = _userName; // Documentation examples don't show 'z', only 'y'. Verify if 'z' is needed.
            queryParams["y"] = _webApiKey;
        }
        // Connect API auth params (u, t, p) are added specifically in the calling methods

        var url = BuildUrl(baseUrl, endpoint, queryParams);

        int maxRetries = 3;
        int retryCount = 0;
        TimeSpan delay = TimeSpan.FromSeconds(1);
        TimeSpan maxDelay = TimeSpan.FromSeconds(30);
        Random jitter = new Random();

        while (true)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                if (baseUrl == ConnectApiBaseUrl)
                {
                    if (string.IsNullOrWhiteSpace(ConnectApiUserAgent))
                    {
                        throw new InvalidOperationException($"{nameof(ConnectApiUserAgent)} must be set for Connect API calls.");
                    }
                    request.Headers.UserAgent.ParseAdd(ConnectApiUserAgent);
                }

                var response = await _httpClient.SendAsync(request, cancellationToken);
                
                // If we get a rate limit or server error, retry with backoff
                if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                {
                    if (retryCount >= maxRetries)
                        response.EnsureSuccessStatusCode(); // Will throw if we're out of retries
                    
                    retryCount++;
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    
                    // Exponential backoff with jitter
                    delay = TimeSpan.FromMilliseconds(
                        Math.Min(maxDelay.TotalMilliseconds, delay.TotalMilliseconds * 2) * 
                        (0.8 + jitter.NextDouble() * 0.4));
                    
                    continue;
                }
                
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                try
                {
                    // Handle endpoints returning an array at the root
                    if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
                    {
                         return JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions) ?? Activator.CreateInstance<T>();
                    }
                     // Handle endpoints returning a dictionary at the root
                    if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    {
                         return JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions) ?? Activator.CreateInstance<T>();
                    }

                    // Most endpoints return an object
                    return JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions)
                           ?? throw new InvalidOperationException($"Failed to deserialize JSON response to type {typeof(T).Name}. Response was: {json}");
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException($"JSON deserialization failed: {ex.Message}. Response was: {json}", ex);
                }
            }
            catch (HttpRequestException ex) when (retryCount < maxRetries && 
                                                (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests || 
                                                 (ex.StatusCode >= System.Net.HttpStatusCode.InternalServerError)))
            {
                retryCount++;
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                
                // Exponential backoff with jitter
                delay = TimeSpan.FromMilliseconds(
                    Math.Min(maxDelay.TotalMilliseconds, delay.TotalMilliseconds * 2) * 
                    (0.8 + jitter.NextDouble() * 0.4));
            }
            catch (Exception) when (retryCount < maxRetries)
            {
                // For other exceptions that might be transient
                retryCount++;
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay = TimeSpan.FromMilliseconds(Math.Min(maxDelay.TotalMilliseconds, delay.TotalMilliseconds * 2));
            }
        }
    }


    private async Task<T> PostConnectApiAsync<T>(Dictionary<string, string?> queryParams, HttpContent? content = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ConnectApiUserAgent))
        {
            throw new InvalidOperationException($"{nameof(ConnectApiUserAgent)} must be set for Connect API calls.");
        }

        var url = BuildUrl(ConnectApiBaseUrl, string.Empty, queryParams); // No endpoint path for dorequest.php

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.UserAgent.ParseAdd(ConnectApiUserAgent);

        if (content != null)
        {
            request.Content = content;
        }
        else
        {
            // Required for POST even with empty body sometimes
            request.Content = new StringContent("", Encoding.UTF8, "application/x-www-form-urlencoded");
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
             return JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions)
                   ?? throw new InvalidOperationException($"Failed to deserialize JSON response to type {typeof(T).Name}. Response was: {json}");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON deserialization failed: {ex.Message}. Response was: {json}", ex);
        }
    }


    private string CalculateMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        byte[] inputBytes = Encoding.ASCII.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    // --- Connect API Methods (from Standalones Guide) ---

    /// <summary>
    /// Retrieves the Connect API token using integration account credentials.
    /// Treat the returned token like a password.
    /// </summary>
    /// <param name="integrationUsername">The username of the dedicated integration account.</param>
    /// <param name="integrationPassword">The password of the dedicated integration account.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login information including the Connect API token.</returns>
    public async Task<ConnectLoginResponse> GetConnectApiTokenAsync(string integrationUsername, string integrationPassword, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"u", integrationUsername},
            {"p", integrationPassword},
            {"r", "login2"}
        };
        // This is a GET request according to the documentation
        return await GetApiAsync<ConnectLoginResponse>(ConnectApiBaseUrl, string.Empty, queryParams, cancellationToken);
    }

    /// <summary>
    /// Starts a game session for a player.
    /// </summary>
    /// <param name="integrationUsername">The username of the dedicated integration account.</param>
    /// <param name="connectToken">The Connect API token obtained via GetConnectApiTokenAsync.</param>
    /// <param name="gameId">The game's primary RetroAchievements ID.</param>
    /// <param name="playerUsername">The player's RetroAchievements username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session start information.</returns>
    public async Task<StartSessionResponse> StartPlayerSessionAsync(string integrationUsername, string connectToken, int gameId, string playerUsername, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"u", integrationUsername},
            {"t", connectToken},
            {"r", "startsession"},
            {"g", gameId.ToString()},
            {"k", playerUsername}
        };
        // Documentation shows this as POST
        return await PostConnectApiAsync<StartSessionResponse>(queryParams, null, cancellationToken);
    }

    /// <summary>
    /// Sends a heartbeat ping for an active player session, optionally updating Rich Presence.
    /// </summary>
    /// <param name="integrationUsername">The username of the dedicated integration account.</param>
    /// <param name="connectToken">The Connect API token obtained via GetConnectApiTokenAsync.</param>
    /// <param name="gameId">The game's primary RetroAchievements ID.</param>
    /// <param name="playerUsername">The player's RetroAchievements username.</param>
    /// <param name="richPresence">Optional Rich Presence string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ping response.</returns>
    public async Task<PingResponse> SendHeartbeatPingAsync(string integrationUsername, string connectToken, int gameId, string playerUsername, string? richPresence = null, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"u", integrationUsername},
            {"t", connectToken},
            {"r", "ping"},
            {"g", gameId.ToString()},
            {"k", playerUsername}
        };

        HttpContent? content = null;
        if (!string.IsNullOrEmpty(richPresence))
        {
            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new StringContent(richPresence), "m");
            content = multipartContent;
        }

        // Documentation shows this as POST
        return await PostConnectApiAsync<PingResponse>(queryParams, content, cancellationToken);
    }

    /// <summary>
    /// Awards a single achievement to a player.
    /// </summary>
    /// <param name="integrationUsername">The username of the dedicated integration account.</param>
    /// <param name="connectToken">The Connect API token obtained via GetConnectApiTokenAsync.</param>
    /// <param name="playerUsername">The player's RetroAchievements username.</param>
    /// <param name="achievementId">The ID of the achievement to award.</param>
    /// <param name="isHardcore">True for hardcore mode, false for softcore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Award response.</returns>
    public async Task<AwardAchievementResponse> AwardSingleAchievementAsync(string integrationUsername, string connectToken, string playerUsername, int achievementId, bool isHardcore, CancellationToken cancellationToken = default)
    {
        string hardcoreFlag = isHardcore ? "1" : "0";
        string verificationHashInput = $"{achievementId}{playerUsername}{hardcoreFlag}{achievementId}";
        string verificationHash = CalculateMd5Hash(verificationHashInput);

        var queryParams = new Dictionary<string, string?>
        {
            {"u", integrationUsername},
            {"t", connectToken},
            {"r", "awardachievement"},
            {"k", playerUsername},
            {"a", achievementId.ToString()},
            {"v", verificationHash},
            {"h", hardcoreFlag}
        };
        // Documentation shows this as POST
        return await PostConnectApiAsync<AwardAchievementResponse>(queryParams, null, cancellationToken);
    }

    /// <summary>
    /// Awards multiple achievements to a player simultaneously.
    /// </summary>
    /// <param name="integrationUsername">The username of the dedicated integration account.</param>
    /// <param name="connectToken">The Connect API token obtained via GetConnectApiTokenAsync.</param>
    /// <param name="playerUsername">The player's RetroAchievements username.</param>
    /// <param name="achievementIds">A list of achievement IDs to award.</param>
    /// <param name="isHardcore">True for hardcore mode, false for softcore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Award response detailing which achievements were newly awarded.</returns>
    public async Task<AwardAchievementsResponse> AwardMultipleAchievementsAsync(string integrationUsername, string connectToken, string playerUsername, IEnumerable<int> achievementIds, bool isHardcore, CancellationToken cancellationToken = default)
    {
        string achievementIdList = string.Join(",", achievementIds);
        string hardcoreFlag = isHardcore ? "1" : "0";
        string verificationHashInput = $"{achievementIdList}{playerUsername}{hardcoreFlag}";
        string verificationHash = CalculateMd5Hash(verificationHashInput);

        var queryParams = new Dictionary<string, string?>
        {
            {"u", integrationUsername},
            {"t", connectToken},
            {"r", "awardachievements"},
            {"k", playerUsername}
            // Note: a, h, v are sent in the body
        };

        var multipartContent = new MultipartFormDataContent
        {
            { new StringContent(achievementIdList), "a" },
            { new StringContent(hardcoreFlag), "h" },
            { new StringContent(verificationHash), "v" }
        };

        return await PostConnectApiAsync<AwardAchievementsResponse>(queryParams, multipartContent, cancellationToken);
    }


    // --- Web API Methods ---

    // --- Ticket Endpoints (API_GetTicketData.php variations) ---

    /// <summary>
    /// Retrieves ticket stats for a specific achievement.
    /// </summary>
    /// <param name="achievementId">The target achievement ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AchievementTicketStats> GetAchievementTicketStatsAsync(int achievementId, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "a", achievementId.ToString() } };
        return await GetApiAsync<AchievementTicketStats>(WebApiBaseUrl, "API_GetTicketData.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves ticket stats for a developer by username.
    /// </summary>
    /// <param name="developerUsername">The target developer's username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<DeveloperTicketStats> GetDeveloperTicketStatsAsync(string developerUsername, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "u", developerUsername } };
        return await GetApiAsync<DeveloperTicketStats>(WebApiBaseUrl, "API_GetTicketData.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves ticket stats for a developer by ULID.
    /// </summary>
    /// <param name="developerUlid">The target developer's ULID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<DeveloperTicketStats> GetDeveloperTicketStatsByUlidAsync(string developerUlid, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "i", developerUlid } };
        return await GetApiAsync<DeveloperTicketStats>(WebApiBaseUrl, "API_GetTicketData.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves ticket stats for a game.
    /// </summary>
    /// <param name="gameId">The target game ID.</param>
    /// <param name="unofficialAchievements">Set to true to get data for unofficial achievements.</param>
    /// <param name="includeDeepMetadata">Set to true to include deep ticket metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<GameTicketStats> GetGameTicketStatsAsync(int gameId, bool unofficialAchievements = false, bool includeDeepMetadata = false, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "g", gameId.ToString() } };
        if (unofficialAchievements) queryParams["f"] = "5";
        if (includeDeepMetadata) queryParams["d"] = "1";
        return await GetApiAsync<GameTicketStats>(WebApiBaseUrl, "API_GetTicketData.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves the most recently opened achievement tickets.
    /// </summary>
    /// <param name="offset">Number of entries to skip (default: 0).</param>
    /// <param name="count">Number of records to return (default: 10, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<MostRecentTicketsResponse> GetMostRecentTicketsAsync(int offset = 0, int count = 10, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"o", offset.ToString()},
            {"c", count.ToString()}
        };
        return await GetApiAsync<MostRecentTicketsResponse>(WebApiBaseUrl, "API_GetTicketData.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves the games with the most open achievement tickets.
    /// </summary>
    /// <param name="offset">Number of entries to skip (default: 0).</param>
    /// <param name="count">Number of records to return (default: 10, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<MostTicketedGamesResponse> GetMostTicketedGamesAsync(int offset = 0, int count = 10, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"f", "1"}, // Required parameter
            {"o", offset.ToString()},
            {"c", count.ToString()}
        };
        return await GetApiAsync<MostTicketedGamesResponse>(WebApiBaseUrl, "API_GetTicketData.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves metadata for a single achievement ticket by its ID.
    /// </summary>
    /// <param name="ticketId">The target ticket ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<TicketData> GetTicketByIdAsync(int ticketId, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "i", ticketId.ToString() } };
        return await GetApiAsync<TicketData>(WebApiBaseUrl, "API_GetTicketData.php", queryParams, cancellationToken);
    }

    // --- Achievement Endpoints ---

    /// <summary>
    /// Retrieves the list of achievement IDs for a game.
    /// </summary>
    /// <param name="gameId">The target game ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AchievementCountResponse> GetAchievementIdsForGameAsync(int gameId, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "i", gameId.ToString() } };
        return await GetApiAsync<AchievementCountResponse>(WebApiBaseUrl, "API_GetAchievementCount.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves the distribution of players based on the number of achievements earned for a game.
    /// </summary>
    /// <param name="gameId">The target game ID.</param>
    /// <param name="hardcoreOnly">True to only query hardcore unlocks.</param>
    /// <param name="unofficial">True to query unofficial achievements.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary where the key is the number of achievements earned and the value is the count of players.</returns>
    public async Task<Dictionary<string, int>> GetAchievementDistributionAsync(int gameId, bool hardcoreOnly = false, bool unofficial = false, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "i", gameId.ToString() } };
        if (hardcoreOnly) queryParams["h"] = "1";
        if (unofficial) queryParams["f"] = "5"; else queryParams["f"] = "3"; // Default seems to be 3? Doc says 'Defaults to 3'.
        return await GetApiAsync<Dictionary<string, int>>(WebApiBaseUrl, "API_GetAchievementDistribution.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves comprehensive metadata about the current Achievement of the Week.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AchievementOfTheWeekResponse> GetAchievementOfTheWeekAsync(CancellationToken cancellationToken = default)
    {
        return await GetApiAsync<AchievementOfTheWeekResponse>(WebApiBaseUrl, "API_GetAchievementOfTheWeek.php", new Dictionary<string, string?>(), cancellationToken);
    }

    /// <summary>
    /// Retrieves a list of users who have earned a specific achievement.
    /// </summary>
    /// <param name="achievementId">The target achievement ID.</param>
    /// <param name="offset">Number of entries to skip (default: 0).</param>
    /// <param name="count">Number of records to return (default: 50, max: 500).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AchievementUnlocksResponse> GetAchievementUnlocksAsync(int achievementId, int offset = 0, int count = 50, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"a", achievementId.ToString()},
            {"o", offset.ToString()},
            {"c", count.ToString()}
        };
        return await GetApiAsync<AchievementUnlocksResponse>(WebApiBaseUrl, "API_GetAchievementUnlocks.php", queryParams, cancellationToken);
    }

    // --- Feed Endpoints ---

    /// <summary>
    /// Retrieves information about all active achievement set claims (max 1000).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<List<Claim>> GetActiveClaimsAsync(CancellationToken cancellationToken = default)
    {
         return await GetApiAsync<List<Claim>>(WebApiBaseUrl, "API_GetActiveClaims.php", new Dictionary<string, string?>(), cancellationToken);
    }

    /// <summary>
    /// Retrieves information about inactive achievement set claims (completed, dropped, or expired).
    /// </summary>
    /// <param name="kind">The type of inactive claim to retrieve (Completed, Dropped, Expired).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<List<Claim>> GetInactiveClaimsAsync(ClaimKind kind = ClaimKind.Completed, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "k", ((int)kind).ToString() } };
        return await GetApiAsync<List<Claim>>(WebApiBaseUrl, "API_GetClaims.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves all recently granted game awards (mastered, completed, etc.).
    /// </summary>
    /// <param name="startDate">Starting date (YYYY-MM-DD) (default: today).</param>
    /// <param name="offset">Offset (default: 0).</param>
    /// <param name="count">Count (default: 25, max: 100).</param>
    /// <param name="kinds">Specific kinds of awards to retrieve (default: all).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<RecentGameAwardsResponse> GetRecentGameAwardsAsync(DateTime? startDate = null, int offset = 0, int count = 25, IEnumerable<GameAwardKind>? kinds = null, CancellationToken cancellationToken = default)
    {
         var queryParams = new Dictionary<string, string?>
        {
            {"o", offset.ToString()},
            {"c", count.ToString()}
        };
         if(startDate.HasValue) queryParams["d"] = startDate.Value.ToString("yyyy-MM-dd");
         if(kinds != null) queryParams["k"] = string.Join(",", kinds.Select(k => k.ToString().ToLowerInvariant().Replace("_", "-"))); // e.g., beaten-hardcore

        return await GetApiAsync<RecentGameAwardsResponse>(WebApiBaseUrl, "API_GetRecentGameAwards.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves the current top ten users ranked by hardcore points.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of top ten users.</returns>
    public async Task<List<TopTenUser>> GetTopTenUsersAsync(CancellationToken cancellationToken = default)
    {
        // The response is a JSON array where each object has numerical keys "1", "2", "3", "4".
        // This requires custom handling or a less specific type like List<Dictionary<string, JsonElement>>.
        // Let's define a specific DTO and use JsonPropertyName.

        return await GetApiAsync<List<TopTenUser>>(WebApiBaseUrl, "API_GetTopTenUsers.php", new Dictionary<string, string?>(), cancellationToken);
    }


    // --- Console/System Endpoints ---

    /// <summary>
    /// Retrieves the complete list of system IDs and names.
    /// </summary>
    /// <param name="onlyActive">If true, only return active systems.</param>
    /// <param name="onlyGameSystems">If true, only return gaming systems (not Hubs, Events).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<List<ConsoleInfo>> GetConsoleIdsAsync(bool onlyActive = false, bool onlyGameSystems = false, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>();
        if (onlyActive) queryParams["a"] = "1";
        if (onlyGameSystems) queryParams["g"] = "1";
        return await GetApiAsync<List<ConsoleInfo>>(WebApiBaseUrl, "API_GetConsoleIDs.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves the list of games for a specific console.
    /// Consider caching this aggressively.
    /// </summary>
    /// <param name="consoleId">The target system ID.</param>
    /// <param name="onlyWithAchievements">If true, only return games that have achievements.</param>
    /// <param name="includeHashes">If true, also return supported hashes for games.</param>
    /// <param name="offset">Offset for pagination.</param>
    /// <param name="count">Number of results for pagination (0 for all - potentially huge response!).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<List<GameInfoBasic>> GetGameListAsync(int consoleId, bool onlyWithAchievements = false, bool includeHashes = false, int offset = 0, int count = 0, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "i", consoleId.ToString() } };
        if (onlyWithAchievements) queryParams["f"] = "1";
        if (includeHashes) queryParams["h"] = "1";
        if (offset > 0) queryParams["o"] = offset.ToString();
        if (count > 0) queryParams["c"] = count.ToString();

        return await GetApiAsync<List<GameInfoBasic>>(WebApiBaseUrl, "API_GetGameList.php", queryParams, cancellationToken);
    }

    // --- Game Endpoints ---

    /// <summary>
    /// Retrieves basic metadata about a game.
    /// </summary>
    /// <param name="gameId">The target game ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<GameInfo> GetGameAsync(int gameId, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "i", gameId.ToString() } };
        return await GetApiAsync<GameInfo>(WebApiBaseUrl, "API_GetGame.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves extended metadata about a game, including its achievements.
    /// </summary>
    /// <param name="gameId">The target game ID.</param>
    /// <param name="unofficialAchievements">Set true to include unofficial/demoted achievements.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<GameInfoExtended> GetGameExtendedAsync(int gameId, bool unofficialAchievements = false, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "i", gameId.ToString() } };
         // Default is 3 (official), 5 is unofficial
        if (unofficialAchievements) queryParams["f"] = "5"; else queryParams["f"] = "3";
        return await GetApiAsync<GameInfoExtended>(WebApiBaseUrl, "API_GetGameExtended.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves the hashes linked to a game.
    /// </summary>
    /// <param name="gameId">The target game ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<GameHashesResponse> GetGameHashesAsync(int gameId, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "i", gameId.ToString() } };
        return await GetApiAsync<GameHashesResponse>(WebApiBaseUrl, "API_GetGameHashes.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves metadata about the highest scores or latest masters for a game.
    /// </summary>
    /// <param name="gameId">The target game ID.</param>
    /// <param name="type">Type of ranking to retrieve (HighScores or LatestMasters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<List<GameRank>> GetGameRankAndScoreAsync(int gameId, GameRankType type = GameRankType.HighScores, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"g", gameId.ToString()},
            {"t", ((int)type).ToString()}
        };
        return await GetApiAsync<List<GameRank>>(WebApiBaseUrl, "API_GetGameRankAndScore.php", queryParams, cancellationToken);
    }

     /// <summary>
    /// Retrieves the list of leaderboards for a game.
    /// </summary>
    /// <param name="gameId">The target game ID.</param>
    /// <param name="offset">Number of entries to skip (default: 0).</param>
    /// <param name="count">Number of records to return (default: 100, max: 500).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<GameLeaderboardsResponse> GetGameLeaderboardsAsync(int gameId, int offset = 0, int count = 100, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"i", gameId.ToString()},
            {"o", offset.ToString()},
            {"c", count.ToString()}
        };
        return await GetApiAsync<GameLeaderboardsResponse>(WebApiBaseUrl, "API_GetGameLeaderboards.php", queryParams, cancellationToken);
    }

    // --- Leaderboard Endpoints ---

    /// <summary>
    /// Retrieves the entries for a specific leaderboard.
    /// </summary>
    /// <param name="leaderboardId">The target leaderboard ID.</param>
    /// <param name="offset">Number of entries to skip (default: 0).</param>
    /// <param name="count">Number of records to return (default: 100, max: 500).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<LeaderboardEntriesResponse> GetLeaderboardEntriesAsync(int leaderboardId, int offset = 0, int count = 100, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"i", leaderboardId.ToString()},
            {"o", offset.ToString()},
            {"c", count.ToString()}
        };
        return await GetApiAsync<LeaderboardEntriesResponse>(WebApiBaseUrl, "API_GetLeaderboardEntries.php", queryParams, cancellationToken);
    }


    // --- User Endpoints ---

    /// <summary>
    /// Retrieves minimal user profile information by username.
    /// </summary>
    /// <param name="username">The target username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<UserProfile> GetUserProfileAsync(string username, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "u", username } };
        return await GetApiAsync<UserProfile>(WebApiBaseUrl, "API_GetUserProfile.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves minimal user profile information by ULID.
    /// </summary>
    /// <param name="ulid">The target user's ULID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<UserProfile> GetUserProfileByUlidAsync(string ulid, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "i", ulid } };
        return await GetApiAsync<UserProfile>(WebApiBaseUrl, "API_GetUserProfile.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves achievements unlocked by a user between two dates.
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="fromDate">Start date/time.</param>
    /// <param name="toDate">End date/time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<List<UserRecentAchievement>> GetAchievementsEarnedBetweenAsync(string usernameOrUlid, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"u", usernameOrUlid},
            {"f", ((DateTimeOffset)fromDate.ToUniversalTime()).ToUnixTimeSeconds().ToString()},
            {"t", ((DateTimeOffset)toDate.ToUniversalTime()).ToUnixTimeSeconds().ToString()}
        };
        return await GetApiAsync<List<UserRecentAchievement>>(WebApiBaseUrl, "API_GetAchievementsEarnedBetween.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves achievements unlocked by a user on a specific date.
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="date">The target date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<List<UserRecentAchievement>> GetAchievementsEarnedOnDayAsync(string usernameOrUlid, DateTime date, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"u", usernameOrUlid},
            {"d", date.ToString("yyyy-MM-dd")}
        };
        return await GetApiAsync<List<UserRecentAchievement>>(WebApiBaseUrl, "API_GetAchievementsEarnedOnDay.php", queryParams, cancellationToken);
    }


    /// <summary>
    /// Retrieves a user's site awards/badges.
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<UserAwardsResponse> GetUserAwardsAsync(string usernameOrUlid, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "u", usernameOrUlid } };
        return await GetApiAsync<UserAwardsResponse>(WebApiBaseUrl, "API_GetUserAwards.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves a list of achievement set claims made by a user.
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<List<Claim>> GetUserClaimsAsync(string usernameOrUlid, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "u", usernameOrUlid } };
        return await GetApiAsync<List<Claim>>(WebApiBaseUrl, "API_GetUserClaims.php", queryParams, cancellationToken);
    }


    /// <summary>
    /// [Legacy] Retrieves completion metadata about games a user has played. Returns softcore and hardcore entries separately.
    /// Prefer GetUserCompletionProgressAsync for most use cases.
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<List<UserCompletedGame>> GetUserCompletedGamesLegacyAsync(string usernameOrUlid, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "u", usernameOrUlid } };
        return await GetApiAsync<List<UserCompletedGame>>(WebApiBaseUrl, "API_GetUserCompletedGames.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves a user's completion progress across all games they've played.
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="offset">Offset for pagination (default: 0).</param>
    /// <param name="count">Count for pagination (default: 100, max: 500).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<PaginatedResponse<UserCompletionProgressGame>> GetUserCompletionProgressAsync(string usernameOrUlid, int offset = 0, int count = 100, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"u", usernameOrUlid},
            {"o", offset.ToString()},
            {"c", count.ToString()}
        };
        return await GetApiAsync<PaginatedResponse<UserCompletionProgressGame>>(WebApiBaseUrl, "API_GetUserCompletionProgress.php", queryParams, cancellationToken);
    }


    /// <summary>
    /// Retrieves extended metadata about a game, including the specified user's progress.
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="gameId">The target game ID.</param>
    /// <param name="includeAwardMetadata">Set true to include user award metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<GameInfoExtendedUserProgress> GetGameInfoAndUserProgressAsync(string usernameOrUlid, int gameId, bool includeAwardMetadata = false, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"u", usernameOrUlid},
            {"g", gameId.ToString()}
        };
        if(includeAwardMetadata) queryParams["a"] = "1";
        return await GetApiAsync<GameInfoExtendedUserProgress>(WebApiBaseUrl, "API_GetGameInfoAndUserProgress.php", queryParams, cancellationToken);
    }


    /// <summary>
    /// Retrieves metadata about how a user has performed/ranked on a specific game.
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="gameId">The target game ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list containing the user's rank and score for the game, or an empty list if no progress.</returns>
    public async Task<List<UserGameRank>> GetUserGameRankAndScoreAsync(string usernameOrUlid, int gameId, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"u", usernameOrUlid},
            {"g", gameId.ToString()}
        };
        // Returns array, potentially empty
        return await GetApiAsync<List<UserGameRank>>(WebApiBaseUrl, "API_GetUserGameRankAndScore.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves a user's leaderboard entries for a given game.
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="gameId">The target game ID.</param>
    /// <param name="offset">Number of entries to skip (default: 0).</param>
    /// <param name="count">Number of records to return (default: 200, max: 500).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<PaginatedResponse<UserGameLeaderboardEntry>> GetUserGameLeaderboardsAsync(string usernameOrUlid, int gameId, int offset = 0, int count = 200, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"u", usernameOrUlid},
            {"i", gameId.ToString()}, // Doc uses 'i' here, not 'g'
            {"o", offset.ToString()},
            {"c", count.ToString()}
        };
        return await GetApiAsync<PaginatedResponse<UserGameLeaderboardEntry>>(WebApiBaseUrl, "API_GetUserGameLeaderboards.php", queryParams, cancellationToken);
    }


    /// <summary>
    /// Retrieves a user's hardcore and softcore points.
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<UserPoints> GetUserPointsAsync(string usernameOrUlid, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "u", usernameOrUlid } };
        return await GetApiAsync<UserPoints>(WebApiBaseUrl, "API_GetUserPoints.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves a user's progress on a specific list of games.
    /// Prefer GetUserCompletionProgressAsync for general progress.
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="gameIds">A collection of target game IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary where the key is the game ID (as string) and the value is the progress.</returns>
    public async Task<Dictionary<string, UserGameProgress>> GetUserProgressForGamesAsync(string usernameOrUlid, IEnumerable<int> gameIds, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"u", usernameOrUlid},
            {"i", string.Join(",", gameIds)}
        };
        return await GetApiAsync<Dictionary<string, UserGameProgress>>(WebApiBaseUrl, "API_GetUserProgress.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves a user's recently unlocked achievements (default: last 60 minutes).
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="minutesAgo">How many minutes back to look (default: 60).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<List<UserRecentAchievement>> GetUserRecentAchievementsAsync(string usernameOrUlid, int minutesAgo = 60, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?> { { "u", usernameOrUlid } };
        if (minutesAgo != 60) queryParams["m"] = minutesAgo.ToString();
        return await GetApiAsync<List<UserRecentAchievement>>(WebApiBaseUrl, "API_GetUserRecentAchievements.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves a list of a user's recently played games.
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="offset">Offset for pagination (default: 0).</param>
    /// <param name="count">Count for pagination (default: 10, max: 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<List<UserRecentlyPlayedGame>> GetUserRecentlyPlayedGamesAsync(string usernameOrUlid, int offset = 0, int count = 10, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"u", usernameOrUlid},
            {"o", offset.ToString()},
            {"c", count.ToString()}
        };
        return await GetApiAsync<List<UserRecentlyPlayedGame>>(WebApiBaseUrl, "API_GetUserRecentlyPlayedGames.php", queryParams, cancellationToken);
    }

     /// <summary>
    /// Retrieves a user's requested achievement sets.
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="onlyActive">True to get only active requests, false for all requests (default: true).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<UserSetRequestsResponse> GetUserSetRequestsAsync(string usernameOrUlid, bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"u", usernameOrUlid},
            {"t", onlyActive ? "0" : "1"}
        };
        return await GetApiAsync<UserSetRequestsResponse>(WebApiBaseUrl, "API_GetUserSetRequests.php", queryParams, cancellationToken);
    }


    /// <summary>
    /// [Slow/Overfetching] Retrieves summary information about a user, including recent games and achievements.
    /// Prefer specific endpoints like GetUserProfileAsync or GetUserCompletionProgressAsync.
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="numRecentGames">Number of recent games to return (default: 0).</param>
    /// <param name="numRecentAchievements">Number of recent achievements per game to return (default: 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<UserSummary> GetUserSummaryAsync(string usernameOrUlid, int numRecentGames = 0, int numRecentAchievements = 10, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"u", usernameOrUlid},
            {"g", numRecentGames.ToString()},
            {"a", numRecentAchievements.ToString()}
        };
        return await GetApiAsync<UserSummary>(WebApiBaseUrl, "API_GetUserSummary.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves a user's "Want to Play" list. Requires you to be the user or mutual followers.
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="offset">Offset for pagination (default: 0).</param>
    /// <param name="count">Count for pagination (default: 100, max: 500).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<PaginatedResponse<WantToPlayGame>> GetUserWantToPlayListAsync(string usernameOrUlid, int offset = 0, int count = 100, CancellationToken cancellationToken = default)
    {
         var queryParams = new Dictionary<string, string?>
        {
            {"u", usernameOrUlid},
            {"o", offset.ToString()},
            {"c", count.ToString()}
        };
        return await GetApiAsync<PaginatedResponse<WantToPlayGame>>(WebApiBaseUrl, "API_GetUserWantToPlayList.php", queryParams, cancellationToken);
    }

     /// <summary>
    /// Retrieves the list of users who follow the authenticated user (your followers).
    /// </summary>
    /// <param name="offset">Offset for pagination (default: 0).</param>
    /// <param name="count">Count for pagination (default: 100, max: 500).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<PaginatedResponse<FollowingUser>> GetUsersFollowingMeAsync(int offset = 0, int count = 100, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"o", offset.ToString()},
            {"c", count.ToString()}
        };
        return await GetApiAsync<PaginatedResponse<FollowingUser>>(WebApiBaseUrl, "API_GetUsersFollowingMe.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves the list of users the authenticated user follows.
    /// </summary>
    /// <param name="offset">Offset for pagination (default: 0).</param>
    /// <param name="count">Count for pagination (default: 100, max: 500).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<PaginatedResponse<FollowedUser>> GetUsersIFollowAsync(int offset = 0, int count = 100, CancellationToken cancellationToken = default)
    {
         var queryParams = new Dictionary<string, string?>
        {
            {"o", offset.ToString()},
            {"c", count.ToString()}
        };
        return await GetApiAsync<PaginatedResponse<FollowedUser>>(WebApiBaseUrl, "API_GetUsersIFollow.php", queryParams, cancellationToken);
    }

    // --- Comment Endpoints ---

    /// <summary>
    /// Retrieves comments posted on a game's wall.
    /// </summary>
    /// <param name="gameId">The target game ID.</param>
    /// <param name="offset">Offset (default: 0).</param>
    /// <param name="count">Count (default: 100, max: 500).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<PaginatedResponse<Comment>> GetGameCommentsAsync(int gameId, int offset = 0, int count = 100, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"t", "1"}, // Type 1 for game
            {"i", gameId.ToString()},
            {"o", offset.ToString()},
            {"c", count.ToString()}
        };
        return await GetApiAsync<PaginatedResponse<Comment>>(WebApiBaseUrl, "API_GetComments.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves comments posted on an achievement's wall.
    /// </summary>
    /// <param name="achievementId">The target achievement ID.</param>
    /// <param name="offset">Offset (default: 0).</param>
    /// <param name="count">Count (default: 100, max: 500).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<PaginatedResponse<Comment>> GetAchievementCommentsAsync(int achievementId, int offset = 0, int count = 100, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"t", "2"}, // Type 2 for achievement
            {"i", achievementId.ToString()},
            {"o", offset.ToString()},
            {"c", count.ToString()}
        };
        return await GetApiAsync<PaginatedResponse<Comment>>(WebApiBaseUrl, "API_GetComments.php", queryParams, cancellationToken);
    }

    /// <summary>
    /// Retrieves comments posted on a user's wall.
    /// </summary>
    /// <param name="usernameOrUlid">The target username or ULID.</param>
    /// <param name="offset">Offset (default: 0).</param>
    /// <param name="count">Count (default: 100, max: 500).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<PaginatedResponse<Comment>> GetUserCommentsAsync(string usernameOrUlid, int offset = 0, int count = 100, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"t", "3"}, // Type 3 for user
            {"i", usernameOrUlid}, // User identifier
            {"o", offset.ToString()},
            {"c", count.ToString()}
        };
        return await GetApiAsync<PaginatedResponse<Comment>>(WebApiBaseUrl, "API_GetComments.php", queryParams, cancellationToken);
    }


    // --- Dispose ---
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient.Dispose();
        }
    }
}
