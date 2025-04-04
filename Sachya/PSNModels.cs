// PlayStationTrophyClient.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Sachya;

public class PlayStationTrophyClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // Use dependency injection for HttpClient
    public PlayStationTrophyClient(HttpClient httpClient, string baseUrl = "https://m.np.playstation.com/api/trophy/v1")
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUrl = baseUrl.TrimEnd('/');

        // Configure default headers if needed, though Authorization is per-request
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private async Task<T> SendRequestAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            // Consider parsing errorContent if API provides structured errors
            throw new PlaystationApiException($"API request failed with status code {response.StatusCode}.", response.StatusCode, errorContent);
        }

        // Handle potential empty response for certain successful status codes if necessary
        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
        {
            if (typeof(T) == typeof(object)) // Or some other marker for "no content expected"
            {
                // Need to return default or handle appropriately, GetFromJsonAsync throws on empty.
                // Using ReadAsStringAsync and checking emptiness first might be safer.
                return default!; // Or throw, depending on expected behavior
            }
            else
            {
                throw new PlaystationApiException($"API returned {response.StatusCode} but content was expected.", response.StatusCode);
            }
        }

        try
        {
            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
            if (result == null)
            {
                throw new PlaystationApiException("API returned null response.", response.StatusCode);
            }
            return result;
        }
        catch (JsonException jsonEx)
        {
            string rawContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new PlaystationApiException($"Failed to deserialize JSON response: {jsonEx.Message}. Raw content: {rawContent}", response.StatusCode, jsonEx, rawContent);
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUrl, string accessToken)
    {
        var request = new HttpRequestMessage(method, $"{_baseUrl}{relativeUrl}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private string BuildQueryString(Dictionary<string, string> parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return string.Empty;

        var query = HttpUtility.ParseQueryString(string.Empty); // Or use manual building
        foreach (var kvp in parameters)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                query[kvp.Key] = kvp.Value;
            }
        }
        return query.ToString() ?? string.Empty; // query.ToString can be null
    }

    private bool IsLegacyPlatform(string platform)
    {
        if (string.IsNullOrWhiteSpace(platform)) return false;
        var platforms = platform.Split(',');
        return platforms.Any(p => p.Trim().Equals("PS3", StringComparison.OrdinalIgnoreCase) ||
                                  p.Trim().Equals("PS4", StringComparison.OrdinalIgnoreCase) ||
                                  p.Trim().Equals("PSVITA", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Retrieves the list of trophy titles (games) for a user.
    /// </summary>
    /// <param name="accessToken">The authentication token.</param>
    /// <param name="accountId">The user's account ID, or "me" for the authenticating user.</param>
    /// <param name="limit">Maximum number of titles to return.</param>
    /// <param name="offset">Offset for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of trophy titles.</returns>
    public async Task<UserTrophyTitlesResponse> GetUserTrophyTitlesAsync(
        string accessToken,
        string accountId = "me",
        int? limit = null,
        int? offset = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (offset.HasValue) queryParams["offset"] = offset.Value.ToString();

        string queryString = BuildQueryString(queryParams);
        string url = $"/users/{Uri.EscapeDataString(accountId)}/trophyTitles{(string.IsNullOrEmpty(queryString) ? "" : $"?{queryString}")}";

        var request = CreateRequest(HttpMethod.Get, url, accessToken);
        return await SendRequestAsync<UserTrophyTitlesResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Retrieves the definition of trophies for a specific title and group.
    /// </summary>
    /// <param name="accessToken">The authentication token.</param>
    /// <param name="npCommunicationId">The unique ID of the title.</param>
    /// <param name="platform">The platform(s) string (e.g., "PS5", "PS4", "PS4,PSVITA"). Required to determine if npServiceName is needed.</param>
    /// <param name="trophyGroupId">The trophy group ID ("all", "default", "001", etc.).</param>
    /// <param name="limit">Maximum number of trophies to return.</param>
    /// <param name="offset">Offset for pagination.</param>
    /// <param name="acceptLanguage">Optional language code (e.g., "de-DE") for localized trophy details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Details of the trophies.</returns>
    public async Task<TitleTrophiesResponse> GetTitleTrophiesAsync(
        string accessToken,
        string npCommunicationId,
        string platform, // Added platform parameter
        string trophyGroupId = "all",
        int? limit = null,
        int? offset = null,
        string? acceptLanguage = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (offset.HasValue) queryParams["offset"] = offset.Value.ToString();
        if (IsLegacyPlatform(platform)) // Check if npServiceName is needed
        {
            queryParams["npServiceName"] = "trophy";
        }

        string queryString = BuildQueryString(queryParams);
        string url = $"/npCommunicationIds/{Uri.EscapeDataString(npCommunicationId)}/trophyGroups/{Uri.EscapeDataString(trophyGroupId)}/trophies{(string.IsNullOrEmpty(queryString) ? "" : $"?{queryString}")}";

        var request = CreateRequest(HttpMethod.Get, url, accessToken);
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            request.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
        }

        return await SendRequestAsync<TitleTrophiesResponse>(request, cancellationToken);
    }


    /// <summary>
    /// Retrieves the earned status of trophies for a user for a specific title and group.
    /// </summary>
    /// <param name="accessToken">The authentication token.</param>
    /// <param name="npCommunicationId">The unique ID of the title.</param>
    /// <param name="platform">The platform(s) string (e.g., "PS5", "PS4", "PS4,PSVITA"). Required to determine if npServiceName is needed.</param>
    /// <param name="accountId">The user's account ID, or "me" for the authenticating user.</param>
    /// <param name="trophyGroupId">The trophy group ID ("all", "default", "001", etc.).</param>
    /// <param name="limit">Maximum number of trophies to return.</param>
    /// <param name="offset">Offset for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Earned status of trophies.</returns>
    public async Task<UserEarnedTrophiesResponse> GetUserEarnedTrophiesAsync(
        string accessToken,
        string npCommunicationId,
        string platform, // Added platform parameter
        string accountId = "me",
        string trophyGroupId = "all",
        int? limit = null,
        int? offset = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>();
        if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
        if (offset.HasValue) queryParams["offset"] = offset.Value.ToString();
        if (IsLegacyPlatform(platform)) // Check if npServiceName is needed
        {
            queryParams["npServiceName"] = "trophy";
        }

        string queryString = BuildQueryString(queryParams);
        string url = $"/users/{Uri.EscapeDataString(accountId)}/npCommunicationIds/{Uri.EscapeDataString(npCommunicationId)}/trophyGroups/{Uri.EscapeDataString(trophyGroupId)}/trophies{(string.IsNullOrEmpty(queryString) ? "" : $"?{queryString}")}";

        var request = CreateRequest(HttpMethod.Get, url, accessToken);
        return await SendRequestAsync<UserEarnedTrophiesResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Retrieves the overall trophy summary for a user.
    /// </summary>
    /// <param name="accessToken">The authentication token.</param>
    /// <param name="accountId">The user's account ID, or "me" for the authenticating user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's trophy summary.</returns>
    public async Task<TrophySummaryResponse> GetUserTrophySummaryAsync(
        string accessToken,
        string accountId = "me",
        CancellationToken cancellationToken = default)
    {
        string url = $"/users/{Uri.EscapeDataString(accountId)}/trophySummary";
        var request = CreateRequest(HttpMethod.Get, url, accessToken);
        return await SendRequestAsync<TrophySummaryResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Retrieves the definition of trophy groups for a specific title.
    /// </summary>
    /// <param name="accessToken">The authentication token.</param>
    /// <param name="npCommunicationId">The unique ID of the title.</param>
    /// <param name="platform">The platform(s) string (e.g., "PS5", "PS4", "PS4,PSVITA"). Required to determine if npServiceName is needed.</param>
    /// <param name="acceptLanguage">Optional language code (e.g., "de-DE") for localized group details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Details of the trophy groups.</returns>
    public async Task<TitleTrophyGroupsResponse> GetTitleTrophyGroupsAsync(
        string accessToken,
        string npCommunicationId,
        string platform, // Added platform parameter
        string? acceptLanguage = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>();
        if (IsLegacyPlatform(platform)) // Check if npServiceName is needed
        {
            queryParams["npServiceName"] = "trophy";
        }

        string queryString = BuildQueryString(queryParams);
        string url = $"/npCommunicationIds/{Uri.EscapeDataString(npCommunicationId)}/trophyGroups{(string.IsNullOrEmpty(queryString) ? "" : $"?{queryString}")}";

        var request = CreateRequest(HttpMethod.Get, url, accessToken);
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            request.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
        }
        return await SendRequestAsync<TitleTrophyGroupsResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Retrieves the summary of trophies earned by a user, broken down by trophy group within a title.
    /// </summary>
    /// <param name="accessToken">The authentication token.</param>
    /// <param name="npCommunicationId">The unique ID of the title.</param>
    /// <param name="platform">The platform(s) string (e.g., "PS5", "PS4", "PS4,PSVITA"). Required to determine if npServiceName is needed.</param>
    /// <param name="accountId">The user's account ID, or "me" for the authenticating user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary of earned trophies by group.</returns>
    public async Task<UserEarnedTrophyGroupsResponse> GetUserEarnedTrophyGroupsAsync(
        string accessToken,
        string npCommunicationId,
        string platform, // Added platform parameter
        string accountId = "me",
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>();
        if (IsLegacyPlatform(platform)) // Check if npServiceName is needed
        {
            queryParams["npServiceName"] = "trophy";
        }

        string queryString = BuildQueryString(queryParams);
        string url = $"/users/{Uri.EscapeDataString(accountId)}/npCommunicationIds/{Uri.EscapeDataString(npCommunicationId)}/trophyGroups{(string.IsNullOrEmpty(queryString) ? "" : $"?{queryString}")}";

        var request = CreateRequest(HttpMethod.Get, url, accessToken);
        return await SendRequestAsync<UserEarnedTrophyGroupsResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Retrieves the trophy title summary for specific title IDs (e.g., CUSAxxxxx_00, PPSAxxxxx_00).
    /// </summary>
    /// <param name="accessToken">The authentication token.</param>
    /// <param name="npTitleIds">A comma-separated list of npTitleIds (limit 5).</param>
    /// <param name="accountId">The user's account ID, or "me" for the authenticating user.</param>
    /// <param name="includeNotEarnedTrophyIds">If true, includes IDs of unearned trophies in the response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Trophy summary for the specified title IDs.</returns>
    public async Task<UserTitlesTrophySummaryResponse> GetUserTitlesTrophySummaryAsync(
        string accessToken,
        string npTitleIds, // Comma separated string
        string accountId = "me",
        bool includeNotEarnedTrophyIds = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(npTitleIds))
            throw new ArgumentException("npTitleIds cannot be empty.", nameof(npTitleIds));

        // Check limit - simple count, doesn't handle potential empty strings if split incorrectly
        if (npTitleIds.Split(',').Length > 5)
            throw new ArgumentException("Maximum of 5 npTitleIds allowed per request.", nameof(npTitleIds));

        var queryParams = new Dictionary<string, string>
        {
            { "npTitleIds", npTitleIds } // Needs to be comma-separated, URL encoding handled by HttpClient/Uri
        };
        if (includeNotEarnedTrophyIds)
        {
            queryParams["includeNotEarnedTrophyIds"] = "true";
        }

        string queryString = BuildQueryString(queryParams);
        string url = $"/users/{Uri.EscapeDataString(accountId)}/titles/trophyTitles?{queryString}"; // Query string is mandatory here

        var request = CreateRequest(HttpMethod.Get, url, accessToken);
        return await SendRequestAsync<UserTitlesTrophySummaryResponse>(request, cancellationToken);
    }


    // --- Game Help Endpoints (GraphQL) ---

    private HttpRequestMessage CreateGraphQlRequest(string accessToken, string operationName, object variables, string queryHash)
    {
        // Variables need to be JSON serialized for the URL
        string variablesJson = JsonSerializer.Serialize(variables, _jsonOptions);
        string extensionsJson = JsonSerializer.Serialize(new { persistedQuery = new { version = 1, sha256Hash = queryHash } });

        // URL encode the JSON strings
        string encodedVariables = HttpUtility.UrlEncode(variablesJson);
        string encodedExtensions = HttpUtility.UrlEncode(extensionsJson);

        // Base URL for GraphQL is different
        string graphQlUrl = "https://m.np.playstation.com/api/graphql/v1";
        string url = $"{graphQlUrl}/op?operationName={operationName}&variables={encodedVariables}&extensions={encodedExtensions}";

        var request = new HttpRequestMessage(HttpMethod.Get, url); // Game Help uses GET
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Required GraphQL headers
        request.Headers.Add("apollographql-client-name", "PlayStationApp-Android");
        // Content-Type header seems redundant for GET but included in docs examples
        // If issues arise, try removing this or ensuring it's correct for GET context (might be ignored)
        // request.Content = new StringContent("", Encoding.UTF8, "application/json"); // Empty content for GET
        request.Headers.TryAddWithoutValidation("content-type", "application/json"); // Docs show this header even for GET

        return request;
    }

    /// <summary>
    /// Retrieves a list of trophies for a title that have Game Help available.
    /// </summary>
    /// <param name="accessToken">The authentication token.</param>
    /// <param name="npCommunicationId">The unique ID of the title.</param>
    /// <param name="trophyIds">Optional list of specific trophy IDs to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of trophies with available help.</returns>
    public async Task<GameHelpAvailabilityResponse> GetTrophiesWithGameHelpAsync(
        string accessToken,
        string npCommunicationId,
        IEnumerable<string>? trophyIds = null,
        CancellationToken cancellationToken = default)
    {
        const string operationName = "metGetHintAvailability";
        const string queryHash = "71bf26729f2634f4d8cca32ff73aaf42b3b76ad1d2f63b490a809b66483ea5a7";

        object variables;
        if (trophyIds != null && trophyIds.Any())
        {
            variables = new { npCommId = npCommunicationId, trophyIds = trophyIds.ToList() };
        }
        else
        {
            variables = new { npCommId = npCommunicationId };
        }

        var request = CreateGraphQlRequest(accessToken, operationName, variables, queryHash);
        return await SendRequestAsync<GameHelpAvailabilityResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Retrieves the Game Help content (text/video) for specific trophies.
    /// </summary>
    /// <param name="accessToken">The authentication token.</param>
    /// <param name="npCommunicationId">The unique ID of the title the trophies belong to.</param>
    /// <param name="trophies">A collection of trophy identifiers (TrophyId, UdsObjectId, HelpType) to fetch help for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Game Help content for the requested trophies.</returns>
    public async Task<GameHelpTipsResponse> GetGameHelpForTrophiesAsync(
        string accessToken,
        string npCommunicationId,
        IEnumerable<GameHelpRequestTrophy> trophies,
        CancellationToken cancellationToken = default)
    {
        if (trophies == null || !trophies.Any())
            throw new ArgumentException("Trophy list cannot be null or empty.", nameof(trophies));

        const string operationName = "metGetTips";
        const string queryHash = "93768752a9f4ef69922a543e2209d45020784d8781f57b37a5294e6e206c5630";

        // The structure requires npCommId and a 'trophies' array within variables
        var variables = new
        {
            npCommId = npCommunicationId,
            trophies = trophies.ToList()
        };

        var request = CreateGraphQlRequest(accessToken, operationName, variables, queryHash);
        return await SendRequestAsync<GameHelpTipsResponse>(request, cancellationToken);
    }
}