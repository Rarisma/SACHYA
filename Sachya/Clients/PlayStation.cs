using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;

namespace Sachya.PSN;

public class PSNClient
{
    private HttpClient _httpClient;
    private string _baseUrl;
    private string _accessToken;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true};
    
    /// <summary>
    /// Creates from NPSSO token
    /// </summary>
    /// <param name="npsso">Account token</param>
    /// <param name="baseUrl">Base URL (Optional)</param>
    /// <returns></returns>
    public static async Task<PSNClient> CreateFromNpsso(string npsso, string baseUrl = "https://m.np.playstation.com/api/trophy/v1")
    {

        // Step 1: Exchange NPSSO for access code
        var accessCode = await ExchangeNpssoForAccessCodeAsync(npsso);
            
        // Step 2: Exchange access code for access token
        var _accessToken = await ExchangeAccessCodeForTokenAsync(accessCode);
        PSNClient client = new();
        client._baseUrl = baseUrl;
        client._httpClient = new();
        client._httpClient = new HttpClient();
        client._baseUrl = baseUrl.TrimEnd('/');
        client._accessToken = _accessToken;

        // Configure default headers
        client._httpClient.DefaultRequestHeaders.Accept.Clear();
        client._httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return client;
    }
    
        
    /// <summary>
    /// Exchange NPSSO token for access code using PlayStation's OAuth2 flow
    /// Based on the official PSN API implementation
    /// </summary>
    private static async Task<string> ExchangeNpssoForAccessCodeAsync(string npsso)
    {
        using var client = new HttpClient();
        
        // Configure client to not follow redirects automatically
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        client.DefaultRequestHeaders.Add("Cookie", $"npsso={npsso}");

        // PlayStation OAuth2 authorization endpoint with correct parameters (matching the JS implementation)
        var authUrl = "https://ca.account.sony.com/api/authz/v3/oauth/authorize";
        var parameters = new Dictionary<string, string>
        {
            ["access_type"] = "offline",
            ["client_id"] = "09515159-7237-4370-9b40-3806e67c0891",
            ["redirect_uri"] = "com.scee.psxandroid.scecompcall://redirect",
            ["response_type"] = "code",
            ["scope"] = "psn:mobile.v2.core psn:clientapp"
        };

        var queryString = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        var fullUrl = $"{authUrl}?{queryString}";

        // Make request without following redirects (similar to fetch with redirect: "manual")
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false
        };
        using var noRedirectClient = new HttpClient(handler);
        noRedirectClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        noRedirectClient.DefaultRequestHeaders.Add("Cookie", $"npsso={npsso}");

        var response = await noRedirectClient.GetAsync(fullUrl);
        
        // Check for redirect response (302, 301, etc.)
        if (response.StatusCode == HttpStatusCode.Found || 
            response.StatusCode == HttpStatusCode.Moved || 
            response.StatusCode == HttpStatusCode.Redirect ||
            response.Headers.Location != null)
        {
            var location = response.Headers.Location?.ToString();
            
            if (string.IsNullOrEmpty(location) || !location.Contains("?code="))
            {
                throw new InvalidOperationException(
                    "There was a problem retrieving your PSN access code. Is your NPSSO code valid? " +
                    "To get a new NPSSO code, visit https://ca.account.sony.com/api/v1/ssocookie.");
            }

            // Extract the code from the redirect location
            // Location format: "com.scee.psxandroid.scecompcall://redirect?code=v3.XXXXXX"
            var redirectParams = location.Split("redirect")[1]; // Get everything after "redirect"
            if (redirectParams.StartsWith("/"))
            {
                redirectParams = redirectParams.Substring(1); // Remove leading slash if present
            }
            
            var queryParams = HttpUtility.ParseQueryString(redirectParams);
            var code = queryParams["code"];
            
            if (string.IsNullOrEmpty(code))
            {
                throw new InvalidOperationException("Failed to extract access code from redirect location.");
            }
            
            return code;
        }
        
        var responseContent = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Unexpected response from PSN authorization endpoint. Status: {response.StatusCode}, Response: {responseContent}");
    }

    /// <summary>
    /// Exchange access code for access token and refresh token
    /// Based on the working PowerShell implementation
    /// </summary>
    private static async Task<string> ExchangeAccessCodeForTokenAsync(string accessCode)
    {
        using var client = new HttpClient();
        
        var payload = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = accessCode,
            ["redirect_uri"] = "com.scee.psxandroid.scecompcall://redirect",
            ["token_format"] = "jwt"  // This was missing - required for JWT tokens
        };

        var formContent = new FormUrlEncodedContent(payload);
        
        // Set headers to match the working PowerShell implementation
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        // Add the Authorization header that was missing - this is the Base64 encoded client credentials
        client.DefaultRequestHeaders.Add("Authorization", "Basic MDk1MTUxNTktNzIzNy00MzcwLTliNDAtMzgwNmU2N2MwODkxOnVjUGprYTV0bnRCMktxc1A=");

        var response = await client.PostAsync("https://ca.account.sony.com/api/authz/v3/oauth/token", formContent);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Token exchange failed with status: {response.StatusCode}, Response: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseContent);
        
        if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
        {
            var token = tokenElement.GetString();
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Access token was null or empty");
            }
            return token;
        }
        
        throw new InvalidOperationException($"Failed to extract access token from response: {responseContent}");
    }
    
    private async Task<T> SendRequestAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new PSNApiException($"API request failed with status code {response.StatusCode}.", response.StatusCode, errorContent);
        }

        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
        {
            return default(T)!;
        }

        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<T>(content, _jsonOptions)!;
    }

    /// <summary>
    /// Gets the user's trophy titles (games with trophies)
    /// </summary>
    /// <param name="_accessToken">PlayStation access token</param>
    /// <param name="accountId">PlayStation account ID</param>
    /// <param name="limit">Number of titles to retrieve (default 200)</param>
    /// <param name="offset">Offset for pagination (default 0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User trophy titles response</returns>
    public async Task<UserTrophyTitlesResponse> GetUserTrophyTitlesAsync(string accountId, int limit = 200, int offset = 0, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/users/{accountId}/trophyTitles?limit={limit}&offset={offset}");
        return await SendRequestAsync<UserTrophyTitlesResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Gets trophies for a specific title
    /// </summary>
    /// <param name="_accessToken">PlayStation access token</param>
    /// <param name="npCommunicationId">PlayStation communication ID for the title</param>
    /// <param name="platform">Platform (PS3, PS4, PS5, PSVITA)</param>
    /// <param name="trophyGroupId">Trophy group ID (default "all")</param>
    /// <param name="acceptLanguage">Accept-Language header for localization</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Title trophies response</returns>
    public async Task<TitleTrophiesResponse> GetTitleTrophiesAsync(string npCommunicationId, string platform, string trophyGroupId = "all", string? acceptLanguage = null, CancellationToken cancellationToken = default)
    {
        string npServiceName = platform.ToUpper() switch
        {
            "PS3" => "trophy",
            "PS4" => "trophy",
            "PS5" => "trophy2",
            "PSVITA" => "trophy",
            _ => "trophy"
        };

        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/npCommunicationIds/{npCommunicationId}/trophyGroups/{trophyGroupId}/trophies?npServiceName={npServiceName}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            request.Headers.AcceptLanguage.Clear();
            request.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
        }
        
        return await SendRequestAsync<TitleTrophiesResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Gets earned trophies for a user and specific title
    /// </summary>
    /// <param name="_accessToken">PlayStation access token</param>
    /// <param name="npCommunicationId">PlayStation communication ID for the title</param>
    /// <param name="platform">Platform (PS3, PS4, PS5, PSVITA)</param>
    /// <param name="accountId">PlayStation account ID</param>
    /// <param name="trophyGroupId">Trophy group ID (default "all")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User earned trophies response</returns>
    public async Task<UserEarnedTrophiesResponse> GetUserEarnedTrophiesAsync(string npCommunicationId, string platform, string accountId, string trophyGroupId = "all", CancellationToken cancellationToken = default)
    {
        string npServiceName = platform.ToUpper() switch
        {
            "PS3" => "trophy",
            "PS4" => "trophy",
            "PS5" => "trophy2",
            "PSVITA" => "trophy",
            _ => "trophy"
        };

        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/users/{accountId}/npCommunicationIds/{npCommunicationId}/trophyGroups/{trophyGroupId}/trophies?npServiceName={npServiceName}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return await SendRequestAsync<UserEarnedTrophiesResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Gets trophy summary for a user
    /// </summary>
    /// <param name="_accessToken">PlayStation access token</param>
    /// <param name="accountId">PlayStation account ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Trophy summary response</returns>
    public async Task<TrophySummaryResponse> GetUserTrophySummaryAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/users/{accountId}/trophySummary");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return await SendRequestAsync<TrophySummaryResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Gets trophy groups for a specific title
    /// </summary>
    /// <param name="_accessToken">PlayStation access token</param>
    /// <param name="npCommunicationId">PlayStation communication ID for the title</param>
    /// <param name="platform">Platform (PS3, PS4, PS5, PSVITA)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Title trophy groups response</returns>
    public async Task<TitleTrophyGroupsResponse> GetTitleTrophyGroupsAsync( string npCommunicationId, string platform, CancellationToken cancellationToken = default)
    {
        string npServiceName = platform.ToUpper() switch
        {
            "PS3" => "trophy",
            "PS4" => "trophy",
            "PS5" => "trophy2",
            "PSVITA" => "trophy",
            _ => "trophy"
        };

        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/npCommunicationIds/{npCommunicationId}/trophyGroups?npServiceName={npServiceName}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return await SendRequestAsync<TitleTrophyGroupsResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Gets earned trophy groups for a user and specific title
    /// </summary>
    /// <param name="_accessToken">PlayStation access token</param>
    /// <param name="npCommunicationId">PlayStation communication ID for the title</param>
    /// <param name="platform">Platform (PS3, PS4, PS5, PSVITA)</param>
    /// <param name="accountId">PlayStation account ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User earned trophy groups response</returns>
    public async Task<UserEarnedTrophyGroupsResponse> GetUserEarnedTrophyGroupsAsync( string npCommunicationId, string platform, string accountId, CancellationToken cancellationToken = default)
    {
        string npServiceName = platform.ToUpper() switch
        {
            "PS3" => "trophy",
            "PS4" => "trophy",
            "PS5" => "trophy2",
            "PSVITA" => "trophy",
            _ => "trophy"
        };

        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/users/{accountId}/npCommunicationIds/{npCommunicationId}/trophyGroups?npServiceName={npServiceName}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return await SendRequestAsync<UserEarnedTrophyGroupsResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Gets trophy summary for a specific title
    /// </summary>
    /// <param name="_accessToken">PlayStation access token</param>
    /// <param name="accountId">PlayStation account ID</param>
    /// <param name="npCommunicationId">PlayStation communication ID for the title</param>
    /// <param name="platform">Platform (PS3, PS4, PS5, PSVITA)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User title trophy summary response</returns>
    public async Task<UserTitlesTrophySummaryResponse> GetUserTitlesTrophySummaryAsync(string accountId, string npCommunicationId, string platform, CancellationToken cancellationToken = default)
    {
        string npServiceName = platform.ToUpper() switch
        {
            "PS3" => "trophy",
            "PS4" => "trophy",
            "PS5" => "trophy2",
            "PSVITA" => "trophy",
            _ => "trophy"
        };

        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/users/{accountId}/npCommunicationIds/{npCommunicationId}/trophySummary?npServiceName={npServiceName}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return await SendRequestAsync<UserTitlesTrophySummaryResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Gets trophies with game help for a specific title
    /// </summary>
    /// <param name="_accessToken">PlayStation access token</param>
    /// <param name="npCommunicationId">PlayStation communication ID for the title</param>
    /// <param name="platform">Platform (PS3, PS4, PS5, PSVITA)</param>
    /// <param name="trophyGroupId">Trophy group ID (default "all")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Trophies with game help response</returns>
    public async Task<TrophiesWithGameHelpResponse> GetTrophiesWithGameHelpAsync( string npCommunicationId, string platform, string trophyGroupId = "all", CancellationToken cancellationToken = default)
    {
        string npServiceName = platform.ToUpper() switch
        {
            "PS3" => "trophy",
            "PS4" => "trophy",
            "PS5" => "trophy2",
            "PSVITA" => "trophy",
            _ => "trophy"
        };

        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/npCommunicationIds/{npCommunicationId}/trophyGroups/{trophyGroupId}/trophies?npServiceName={npServiceName}&includeGameHelp=true");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return await SendRequestAsync<TrophiesWithGameHelpResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Gets game help for specific trophies
    /// </summary>
    /// <param name="_accessToken">PlayStation access token</param>
    /// <param name="npCommunicationId">PlayStation communication ID for the title</param>
    /// <param name="platform">Platform (PS3, PS4, PS5, PSVITA)</param>
    /// <param name="trophyIds">List of trophy IDs to get help for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Game help for trophies response</returns>
    public async Task<GameHelpForTrophiesResponse> GetGameHelpForTrophiesAsync( string npCommunicationId, string platform, List<int> trophyIds, CancellationToken cancellationToken = default)
    {
        string npServiceName = platform.ToUpper() switch
        {
            "PS3" => "trophy",
            "PS4" => "trophy",
            "PS5" => "trophy2",
            "PSVITA" => "trophy",
            _ => "trophy"
        };

        var trophyIdsParam = string.Join(",", trophyIds);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/npCommunicationIds/{npCommunicationId}/gameHelp?npServiceName={npServiceName}&trophyIds={trophyIdsParam}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return await SendRequestAsync<GameHelpForTrophiesResponse>(request, cancellationToken);
    }
}

