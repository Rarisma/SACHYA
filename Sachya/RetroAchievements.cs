namespace Sachya;


using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web; // Add reference to System.Web if needed, or use HttpUtility from Microsoft.AspNetCore.WebUtilities for netstandard/core
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
            response.EnsureSuccessStatusCode(); // Throws if not 2xx

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
                _httpClient?.Dispose();
            }
        }
    }

    // --- Enums ---
    public enum ClaimKind
    {
        Completed = 1,
        Dropped = 2,
        Expired = 3
    }

    public enum GameRankType
    {
        HighScores = 0, // Top points earners (non-master?) or first masterers? Doc unclear. Check behavior. Based on Node lib, seems like high scores non-master.
        LatestMasters = 1
    }
     public enum GameAwardKind
    {
        [JsonPropertyName("beaten-softcore")] Beaten_Softcore,
        [JsonPropertyName("beaten-hardcore")] Beaten_Hardcore,
        [JsonPropertyName("completed")] Completed,
        [JsonPropertyName("mastered")] Mastered
    }

    // --- Response Models ---
    // Note: Using [JsonPropertyName] to map JSON fields to C# property names.

    public record PaginatedResponse<T>
    {
        public int Count { get; init; }
        public int Total { get; init; }
        public List<T> Results { get; init; } = new List<T>();
    }


    // Connect API Models
    public record ConnectLoginResponse
    {
        public bool Success { get; init; }
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? Token { get; init; }
        public int Score { get; init; }
        public int SoftcoreScore { get; init; }
        public int Messages { get; init; }
        public int Permissions { get; init; } // Consider enum if values are known
        public string? AccountType { get; init; } // Consider enum
    }

    public record HardcoreUnlockInfo
    {
        public int ID { get; init; }
        public long When { get; init; } // Unix timestamp
        [JsonIgnore]
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTimeOffset WhenDateTime => DateTimeOffset.FromUnixTimeSeconds(When);
    }

    public record StartSessionResponse
    {
        public bool Success { get; init; }
        public List<HardcoreUnlockInfo> HardcoreUnlocks { get; init; } = new List<HardcoreUnlockInfo>();
        public long ServerNow { get; init; } // Unix timestamp
        [JsonIgnore]
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTimeOffset ServerNowDateTime => DateTimeOffset.FromUnixTimeSeconds(ServerNow);
    }

    public record PingResponse
    {
        public bool Success { get; init; }
    }

    public record AwardAchievementResponse
    {
        public bool Success { get; init; }
        public int AchievementsRemaining { get; init; }
        public int Score { get; init; }
        public int SoftcoreScore { get; init; }
        public int AchievementID { get; init; }
    }
     public record AwardAchievementsResponse
    {
        public bool Success { get; init; }
        public int Score { get; init; }
        public int SoftcoreScore { get; init; }
        public List<int> ExistingIDs { get; init; } = new List<int>();
        public List<int> SuccessfulIDs { get; init; } = new List<int>();
    }

    // Ticket Models
    public record AchievementTicketStats
    {
        public int AchievementID { get; init; }
        public string? AchievementTitle { get; init; }
        public string? AchievementDescription { get; init; }
        public string? AchievementType { get; init; }
        public string? URL { get; init; }
        public int OpenTickets { get; init; }
    }

    public record DeveloperTicketStats
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public int Open { get; init; }
        public int Closed { get; init; }
        public int Resolved { get; init; }
        public int Total { get; init; }
        public string? URL { get; init; }
    }

    public record GameTicketStats // Define 'Tickets' array type if 'd=1' is used and structure is known
    {
        public int GameID { get; init; }
        public string? GameTitle { get; init; }
        public string? ConsoleName { get; init; }
        public int OpenTickets { get; init; }
        public string? URL { get; init; }
        // public List<TicketData>? Tickets { get; init; } // If d=1 is used
    }

     public record TicketData
    {
        public int ID { get; init; }
        public int AchievementID { get; init; }
        public string? AchievementTitle { get; init; }
        [JsonPropertyName("AchievementDesc")]
        public string? AchievementDescription { get; init; }
        public string? AchievementType { get; init; } // Often null in examples
        public int Points { get; init; }
        public string? BadgeName { get; init; }
        public string? AchievementAuthor { get; init; }
        public string? AchievementAuthorULID { get; init; }
        public int GameID { get; init; }
        public string? ConsoleName { get; init; }
        public string? GameTitle { get; init; }
        public string? GameIcon { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime ReportedAt { get; init; }
        public int ReportType { get; init; }
        public bool? Hardcore { get; init; } // Nullable bool (or int if 0/1)
        public string? ReportNotes { get; init; }
        public string? ReportedBy { get; init; }
        public string? ReportedByULID { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime? ResolvedAt { get; init; }
        public string? ResolvedBy { get; init; }
        public string? ResolvedByULID { get; init; }
        public int ReportState { get; init; }
        public string? ReportStateDescription { get; init; }
        public string? ReportTypeDescription { get; init; }
        public string? URL { get; init; }
    }

    public record MostRecentTicketsResponse
    {
        public List<TicketData> RecentTickets { get; init; } = new List<TicketData>();
        public int OpenTickets { get; init; }
        public string? URL { get; init; }
    }

    public record MostTicketedGameInfo
    {
        public int GameID { get; init; }
        public string? GameTitle { get; init; }
        public string? GameIcon { get; init; }
        public string? Console { get; init; } // Different name than ConsoleName
        public int OpenTickets { get; init; }
    }

    public record MostTicketedGamesResponse
    {
        public List<MostTicketedGameInfo> MostReportedGames { get; init; } = new List<MostTicketedGameInfo>();
        public string? URL { get; init; }
    }


    // Achievement Models
    public record AchievementCountResponse
    {
        public int GameID { get; init; }
        public List<int> AchievementIDs { get; init; } = new List<int>();
    }

    public record AchievementCoreInfo
    {
        public int ID { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public int Points { get; init; }
        public int TrueRatio { get; init; }
        public string? Type { get; init; }
        public string? Author { get; init; }
        public string? AuthorULID { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime DateCreated { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime DateModified { get; init; }
         public string? BadgeName { get; init; }
         public int DisplayOrder { get; init; }
         public string? MemAddr { get; init; }
    }

     public record AchievementInfo : AchievementCoreInfo
    {
        // Inherits all props from AchievementCoreInfo
    }


    public record AchievementOfTheWeekResponse
    {
        public AchievementInfo? Achievement { get; init; }
        public ConsoleIDName? Console { get; init; }
        public ForumTopicInfo? ForumTopic { get; init; }
        public GameIDTitle? Game { get; init; }
        public DateTimeOffset StartAt { get; init; } // Assuming Z means UTC
        public int TotalPlayers { get; init; }
        public List<AotwUnlock> Unlocks { get; init; } = new List<AotwUnlock>();
        public int UnlocksCount { get; init; }
        public int UnlocksHardcoreCount { get; init; }
    }

    public record AotwUnlock
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public int RAPoints { get; init; }
        public int RASoftcorePoints { get; init; }
        public DateTimeOffset DateAwarded { get; init; } // Assuming Z means UTC
        public int HardcoreMode { get; init; } // 0 or 1
        [JsonIgnore]
        public bool IsHardcore => HardcoreMode == 1;
    }

    public record ConsoleIDName {
        public int ID { get; init; }
        public string? Title { get; init; }
    }
     public record ForumTopicInfo {
        public int ID { get; init; }
    }
    public record GameIDTitle {
        public int ID { get; init; }
        public string? Title { get; init; }
    }

    public record AchievementUnlocksResponse
    {
        public AchievementInfo? Achievement { get; init; }
        public ConsoleIDName? Console { get; init; }
        public GameIDTitle? Game { get; init; }
        public int UnlocksCount { get; init; }
        public int UnlocksHardcoreCount { get; init; }
        public int TotalPlayers { get; init; }
        public List<AchievementUnlockEntry> Unlocks { get; init; } = new List<AchievementUnlockEntry>();
    }

    public record AchievementUnlockEntry
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public int RAPoints { get; init; }
        public int RASoftcorePoints { get; init; }
        public DateTimeOffset DateAwarded { get; init; } // Assuming Z means UTC
        public int HardcoreMode { get; init; } // 0 or 1
        [JsonIgnore]
        public bool IsHardcore => HardcoreMode == 1;
    }


    // Claim Models
    public record Claim
    {
        public int ID { get; init; }
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public int GameID { get; init; }
        public string? GameTitle { get; init; }
        public string? GameIcon { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public int ClaimType { get; init; } // Enum? 0=Primary, 1=Collaboration
        public int SetType { get; init; } // Enum? 0=New, 1=Revision, 2=Rescore
        public int Status { get; init; } // Enum? 0=Active, 1=Completed, 2=Dropped
        public int Extension { get; init; } // Count of extensions
        public int Special { get; init; } // 0 or 1, meaning?
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime Created { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime DoneTime { get; init; } // Expiry or completion time
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime Updated { get; init; }
        public int UserIsJrDev { get; init; } // 0 or 1
        [JsonIgnore]
        public bool IsUserJrDev => UserIsJrDev == 1;
        public int MinutesLeft { get; init; } // Can be negative if expired/done
    }

    // Comment Models
    public record Comment
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime Submitted { get; init; }
        public string? CommentText { get; init; }
    }


    // Console/System Models
    public record ConsoleInfo
    {
        public int ID { get; init; }
        public string? Name { get; init; }
        public string? IconURL { get; init; }
        public bool Active { get; init; }
        public bool IsGameSystem { get; init; }
    }

    // Game Models
    public record GameInfoBasic
    {
        public string? Title { get; init; }
        public int ID { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public string? ImageIcon { get; init; }
        public int NumAchievements { get; init; }
        public int NumLeaderboards { get; init; }
        public int Points { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime DateModified { get; init; }
        public int? ForumTopicID { get; init; } // Nullable?
        public List<string>? Hashes { get; init; } // Only present if h=1
    }

     public record GameInfo : GameIDTitle
    {
        // GameTitle is redundant if Title exists
        // public string? GameTitle { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        // Console seems redundant if ConsoleName exists
        // public string? Console { get; init; }
        public int? ForumTopicID { get; init; }
        public int? Flags { get; init; } // Usually 0 or null, meaning?
        [JsonPropertyName("GameIcon")] // Use GameIcon or ImageIcon? Doc shows both, sometimes redundant
        public string? ImageIcon { get; init; }
        // public string? ImageIcon { get; init; }
        public string? ImageTitle { get; init; }
        public string? ImageIngame { get; init; }
        public string? ImageBoxArt { get; init; }
        public string? Publisher { get; init; }
        public string? Developer { get; init; }
        public string? Genre { get; init; }
        public string? Released { get; init; } // Can be YYYY, YYYY-MM, YYYY-MM-DD HH:MM:SS. Parse carefully.
        public string? ReleasedAtGranularity {get; init; } // year, month, day
    }

    public record GameInfoExtended : GameInfo
    {
        // Inherits base GameInfo properties
        public bool IsFinal { get; init; } // Deprecated, always false
        public string? RichPresencePatch { get; init; }
        public string? GuideURL { get; init; }
        public DateTimeOffset Updated { get; init; } // More precise than DateModified
        public int? ParentGameID { get; init; }
        public int NumDistinctPlayers { get; init; } // Might be same as NumDistinctPlayersCasual?
        public int NumAchievements { get; init; }
        public Dictionary<string, AchievementCoreInfo> Achievements { get; init; } = new Dictionary<string, AchievementCoreInfo>(); // Key is Achievement ID as string
        public List<Claim> Claims { get; init; } = new List<Claim>(); // Usually empty in examples
        public int NumDistinctPlayersCasual { get; init; }
        public int NumDistinctPlayersHardcore { get; init; }

    }

    public record GameHashEntry
    {
        public string? MD5 { get; init; }
        public string? Name { get; init; }
        public List<string> Labels { get; init; } = new List<string>();
        public string? PatchUrl { get; init; }
    }

     public record GameHashesResponse
    {
        public List<GameHashEntry> Results { get; init; } = new List<GameHashEntry>();
    }

    public record GameRank
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public int? NumAchievements { get; init; } // Only in LatestMasters?
        public int TotalScore { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime LastAward { get; init; }
        public int? Rank { get; init; } // Only in high-scores non-master?
    }

     public record GameLeaderboardInfo
    {
        public int ID { get; init; }
        public bool RankAsc { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public string? Format { get; init; } // VALUE, SCORE, TIME, MILLISECS etc. Enum?
        public LeaderboardEntry? TopEntry { get; init; }
    }

    public record GameLeaderboardsResponse : PaginatedResponse<GameLeaderboardInfo> { }


    // Leaderboard Models
     public record LeaderboardEntry
    {
        public int? Rank { get; init; } // Null if requesting UserGameLeaderboards and user has no entry
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public long Score { get; init; } // Use long for SCORE/VALUE, maybe specific types based on Format?
        public string? FormattedScore { get; init; }
        public DateTimeOffset? DateSubmitted { get; init; } // Null if requesting UserGameLeaderboards and user has no entry
        public DateTimeOffset? DateUpdated { get; init; } // Only in UserGameLeaderboards?
    }

    public record LeaderboardEntriesResponse : PaginatedResponse<LeaderboardEntry> { }


    // User Models

    public record UserRecentAchievement
    {
        public DateTime Date { get; init; }
        public int HardcoreMode { get; init; } // 0 or 1
        [JsonIgnore]
        public bool IsHardcore => HardcoreMode == 1;
        public int AchievementID { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public string? BadgeName { get; init; }
        public int Points { get; init; }
        public int TrueRatio { get; init; }
        public string? Type { get; init; }
        public string? Author { get; init; }
        public string? AuthorULID { get; init; }
        public string? GameTitle { get; init; }
        public string? GameIcon { get; init; }
        public int GameID { get; init; }
        public string? ConsoleName { get; init; }
        public int? CumulScore { get; init; } // Seems to be sum of points earned *in this batch*? Use with caution.
        public string? BadgeURL { get; init; }
        public string? GameURL { get; init; }
    }

    public record UserProfile
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public string? UserPic { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]

        public DateTime MemberSince { get; init; }
        public string? RichPresenceMsg { get; init; }
        public int? LastGameID { get; init; }
        public int ContribCount { get; init; } // Deprecated? Meaning?
        public int ContribYield { get; init; } // Deprecated? Meaning?
        public int TotalPoints { get; init; }
        public int TotalSoftcorePoints { get; init; }
        public int TotalTruePoints { get; init; }
        public int Permissions { get; init; } // 1=Normal, 2=JrDev, 3=Dev, 4=?, 5=Admin? Enum?
        public int Untracked { get; init; } // 0 or 1
        [JsonIgnore]
        public bool IsUntracked => Untracked == 1;
        public int ID { get; init; }
        public bool UserWallActive { get; init; }
        public string? Motto { get; init; }
    }

    public record TopTenUser
    {
        [JsonPropertyName("1")]
        public string? Username { get; init; }
        [JsonPropertyName("2")]
        public int TotalPoints { get; init; }
        [JsonPropertyName("3")]
        public int TotalRatioPoints { get; init; } // RetroPoints (white points)
        [JsonPropertyName("4")]
        public string? ULID { get; init; }
    }

     public record UserAward
    {
        public DateTimeOffset AwardedAt { get; init; }
        public string? AwardType { get; init; } // Enum? "Mastery/Completion", "Game Beaten", etc.
        public int AwardData { get; init; } // Game ID for Mastery/Beaten, Amount for Yield?
        public int AwardDataExtra { get; init; } // 1 for hardcore beaten?
        public int DisplayOrder { get; init; } // Or string?
        public string? Title { get; init; } // Game Title for game awards
        public int? ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public int? Flags { get; init; }
        public string? ImageIcon { get; init; } // Game icon for game awards
    }

    public record UserAwardsResponse
    {
        public int TotalAwardsCount { get; init; }
        public int HiddenAwardsCount { get; init; }
        public int MasteryAwardsCount { get; init; }
        public int CompletionAwardsCount { get; init; } // Usually 0?
        public int BeatenHardcoreAwardsCount { get; init; }
        public int BeatenSoftcoreAwardsCount { get; init; }
        public int EventAwardsCount { get; init; }
        public int SiteAwardsCount { get; init; }
        public List<UserAward> VisibleUserAwards { get; init; } = new List<UserAward>();
    }

    public record UserCompletedGame
    {
        public int GameID { get; init; }
        public string? Title { get; init; }
        public string? ImageIcon { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public int MaxPossible { get; init; }
        public int NumAwarded { get; init; }
        public string? PctWon { get; init; } // Changed from decimal to string
        [JsonIgnore]
        public decimal PctWonDecimal => decimal.TryParse(PctWon, out var result) ? result : 0m;
        public string? HardcoreMode { get; init; }
        [JsonIgnore]
        public bool IsHardcore => HardcoreMode == "1";
    }

    public record UserCompletionProgressGame
    {
        public int GameID { get; init; }
        public string? Title { get; init; }
        public string? ImageIcon { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public int MaxPossible { get; init; } // Achievement count
        public int NumAwarded { get; init; } // Softcore achievements awarded
        public int NumAwardedHardcore { get; init; }
        public DateTimeOffset? MostRecentAwardedDate { get; init; } // Seems to be last time *any* cheevo was earned?
        public string? HighestAwardKind { get; init; } // "mastered", "completed", "beaten-hardcore", "beaten-softcore" etc. Enum?
        public DateTimeOffset? HighestAwardDate { get; init; }
    }


    public record AchievementUserProgress : AchievementCoreInfo
    {
        // Inherits core achievement properties
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime? DateEarned { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime? DateEarnedHardcore { get; init; }
    }

    public record GameInfoExtendedUserProgress : GameInfoExtended
    {
        // Inherits GameInfoExtended properties
        // Note: Achievements dictionary now contains AchievementUserProgress
        public new Dictionary<string, AchievementUserProgress> Achievements { get; init; } = new Dictionary<string, AchievementUserProgress>();

        public int NumAwardedToUser { get; init; } // Softcore
        public int NumAwardedToUserHardcore { get; init; }
        public string? UserCompletion { get; init; } // e.g., "100.00%"
        public string? UserCompletionHardcore { get; init; } // e.g., "100.00%"
         public string? HighestAwardKind { get; init; } // "mastered", etc.
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime HighestAwardDate { get; init; }
    }

    public record UserGameRank
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public int UserRank { get; init; }
        public int TotalScore { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime LastAward { get; init; }
    }

     public record UserGameLeaderboardEntry
    {
        public int ID { get; init; }
        public bool RankAsc { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public string? Format { get; init; }
        public LeaderboardEntry? UserEntry { get; init; } // User's entry, Rank/DateSubmitted might be null if no entry
    }


    public record UserPoints
    {
        public int Points { get; init; } // Hardcore
        public int SoftcorePoints { get; init; }
    }

    public record UserGameProgress
    {
        public int NumPossibleAchievements { get; init; }
        public int PossibleScore { get; init; }
        public int NumAchieved { get; init; } // Softcore
        public int ScoreAchieved { get; init; } // Softcore
        public int NumAchievedHardcore { get; init; }
        public int ScoreAchievedHardcore { get; init; }
    }

    public record UserRecentlyPlayedGame : UserGameProgress
    {
         // Inherits UserGameProgress properties
        public int GameID { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public string? Title { get; init; }
        public string? ImageIcon { get; init; }
        public string? ImageTitle { get; init; } // Included in response, but maybe not needed here?
        public string? ImageIngame { get; init; }
        public string? ImageBoxArt { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime LastPlayed { get; init; }
        public int AchievementsTotal { get; init; } // Same as NumPossibleAchievements? Check consistency.

    }

    public record SetRequestGameInfo
    {
        public int GameID { get; init; }
        public string? Title { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public string? ImageIcon { get; init; }
    }

     public record UserSetRequestsResponse
    {
        public List<SetRequestGameInfo> RequestedSets { get; init; } = new List<SetRequestGameInfo>();
        public int TotalRequests { get; init; }
        public int PointsForNext { get; init; }
    }


    public record UserLastActivity
    {
        public int ID { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime? timestamp { get; init; } // Nullable? Format?
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime? lastupdate { get; init; } // Nullable? Format?
        public int? activitytype { get; init; } // Nullable? Enum?
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? data { get; init; } // Meaning?
        public string? data2 { get; init; } // Meaning?
    }

    public record UserSummaryAchievement : AchievementCoreInfo
    {
         // Inherits core achievement properties
         public int GameID { get; init; }
         public string? GameTitle { get; init; }
         [JsonPropertyName("IsAwarded")] // "1" or null/missing? Or "0"? Need to test. Let's assume string.
         public string? IsAwardedString { get; init; }
         [JsonIgnore]
         public bool IsAwarded => IsAwardedString == "1";
         public DateTime? DateAwarded { get; init; }
         public int? HardcoreAchieved { get; init; } // 0 or 1 or null?
          [JsonIgnore]
         public bool? IsHardcoreAchieved => HardcoreAchieved.HasValue ? HardcoreAchieved == 1 : null;
    }


    public record UserSummaryRecentlyPlayed
    {
        public int GameID { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public string? Title { get; init; }
        public string? ImageIcon { get; init; }
        public string? ImageTitle { get; init; }
        public string? ImageIngame { get; init; }
        public string? ImageBoxArt { get; init; }
        public DateTime LastPlayed { get; init; }
        public int AchievementsTotal { get; init; } // NumPossibleAchievements?
    }

    public record UserSummaryLastGame : GameInfo
    {
        // Inherits GameInfo
        public int IsFinal { get; init; } // 0 or 1? bool?
    }

    public record UserSummary : UserProfile
    {
        // Inherits UserProfile properties
        public UserLastActivity? LastActivity { get; init; }
        public int Rank { get; init; }
        public int RecentlyPlayedCount { get; init; }
        public List<UserSummaryRecentlyPlayed> RecentlyPlayed { get; init; } = new List<UserSummaryRecentlyPlayed>();
        // Key is GameID as string, Value is UserGameProgress
        public Dictionary<string, UserGameProgress> Awarded { get; init; } = new Dictionary<string, UserGameProgress>();
        // Outer key is GameID as string, Inner key is AchievementID as string
        public Dictionary<string, Dictionary<string, UserSummaryAchievement>> RecentAchievements { get; init; } = new Dictionary<string, Dictionary<string, UserSummaryAchievement>>();
        public UserSummaryLastGame? LastGame { get; init; }
        public int TotalRanked { get; init; } // Total ranked users on site?
        public string? Status { get; init; } // "Offline", "Online", etc? Enum?
    }

     public record WantToPlayGame
    {
        public int ID { get; init; }
        public string? Title { get; init; }
        public string? ImageIcon { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public int PointsTotal { get; init; }
        public int AchievementsPublished { get; init; }
    }

    public record FollowingUser
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public int Points { get; init; } // Hardcore
        public int PointsSoftcore { get; init; }
        public bool AmIFollowing { get; init; } // Always true for this endpoint? Verify.
    }

     public record FollowedUser
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public int Points { get; init; } // Hardcore
        public int PointsSoftcore { get; init; }
        public bool IsFollowingMe { get; init; }
    }

     public record RecentGameAward
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public string? AwardKind { get; init; } // "mastered", etc. Enum?
        public DateTimeOffset AwardDate { get; init; }
        public int GameID { get; init; }
        public string? GameTitle { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
    }

    public record RecentGameAwardsResponse : PaginatedResponse<RecentGameAward> { }

    // --- Custom Json Converters (Example - Adapt if needed) ---
    // public class CustomDateTimeConverter : JsonConverter<DateTime>
    // {
    //     public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    //     {
    //         if (reader.TokenType == JsonTokenType.String)
    //         {
    //             // Try parsing multiple formats if necessary
    //             if (DateTime.TryParse(reader.GetString(), out DateTime result))
    //             {
    //                 return result;
    //             }
    //             if (DateTime.TryParseExact(reader.GetString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result))
    //             {
    //                 return result;
    //             }
                 // Add more formats if needed
    //         }
    //         // Handle other token types or throw exception
    //         throw new JsonException($"Unexpected token type {reader.TokenType} when parsing DateTime.");
    //     }

    //     public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    //     {
             // Default writing is usually fine (ISO 8601)
    //         writer.WriteStringValue(value.ToString("o"));
    //     }
    // }