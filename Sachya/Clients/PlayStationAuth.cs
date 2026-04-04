using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Sachya.PSN;

public partial class PSNClient
{
    /// <summary>
    /// Creates a PSNClient from an NPSSO token.
    /// </summary>
    public static async Task<PSNClient> CreateFromNpsso(string npsso, string baseUrl = "https://m.np.playstation.com/api/trophy/v1")
    {
        var accessCode = await ExchangeNpssoForAccessCodeAsync(npsso);
        var accessToken = await ExchangeAccessCodeForTokenAsync(accessCode);

        var client = new PSNClient
        {
            _httpClient = new HttpClient(),
            _baseUrl = baseUrl.TrimEnd('/'),
            _accessToken = accessToken
        };

        client._httpClient.DefaultRequestHeaders.Accept.Clear();
        client._httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>
    /// Gets NPSSO token from username and password.
    /// </summary>
    public static async Task<string> GetNpssoFromCredentials(string username, string password)
    {
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            CookieContainer = new CookieContainer()
        };
        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        var loginUrl = "https://auth.api.sonyentertainmentnetwork.com/2.0/oauth/authorize";
        var loginParams = new Dictionary<string, string>
        {
            ["client_id"] = "71a7beb8-f21a-47d9-a604-2e71bee24fe0",
            ["response_type"] = "code",
            ["scope"] = "psn:sceapp",
            ["redirect_uri"] = "https://auth.api.sonyentertainmentnetwork.com/mobile-success.html"
        };

        var queryString = string.Join("&", loginParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        await client.GetAsync($"{loginUrl}?{queryString}");

        var authPayload = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["authentication_type"] = "password",
            ["username"] = username,
            ["password"] = password,
            ["client_id"] = "71a7beb8-f21a-47d9-a604-2e71bee24fe0"
        });

        var authResponse = await client.PostAsync("https://auth.api.sonyentertainmentnetwork.com/2.0/oauth/signin", authPayload);

        if (!authResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Authentication failed. Please check your credentials. Status: {authResponse.StatusCode}");

        var cookies = handler.CookieContainer.GetCookies(new Uri("https://auth.api.sonyentertainmentnetwork.com"));
        var npssoCookie = cookies["npsso"];

        if (npssoCookie == null)
        {
            cookies = handler.CookieContainer.GetCookies(new Uri("https://ca.account.sony.com"));
            npssoCookie = cookies["npsso"];
        }

        if (npssoCookie == null || string.IsNullOrEmpty(npssoCookie.Value))
            throw new InvalidOperationException("Failed to obtain NPSSO token from authentication response.");

        return npssoCookie.Value;
    }

    private static async Task<string> ExchangeNpssoForAccessCodeAsync(string npsso)
    {
        var parameters = new Dictionary<string, string>
        {
            ["access_type"] = "offline",
            ["client_id"] = "09515159-7237-4370-9b40-3806e67c0891",
            ["redirect_uri"] = "com.scee.psxandroid.scecompcall://redirect",
            ["response_type"] = "code",
            ["scope"] = "psn:mobile.v2.core psn:clientapp"
        };

        var queryString = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        var fullUrl = $"https://ca.account.sony.com/api/authz/v3/oauth/authorize?{queryString}";

        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        client.DefaultRequestHeaders.Add("Cookie", $"npsso={npsso}");

        var response = await client.GetAsync(fullUrl);

        if (response.StatusCode is HttpStatusCode.Found or HttpStatusCode.Moved or HttpStatusCode.Redirect
            || response.Headers.Location != null)
        {
            var location = response.Headers.Location?.ToString();

            if (string.IsNullOrEmpty(location) || !location.Contains("?code="))
            {
                throw new InvalidOperationException(
                    "There was a problem retrieving your PSN access code. Is your NPSSO code valid? " +
                    "To get a new NPSSO code, visit https://ca.account.sony.com/api/v1/ssocookie.");
            }

            var redirectParams = location.Split("redirect")[1];
            if (redirectParams.StartsWith("/"))
                redirectParams = redirectParams.Substring(1);

            var queryParams = System.Web.HttpUtility.ParseQueryString(redirectParams);
            var code = queryParams["code"];

            if (string.IsNullOrEmpty(code))
                throw new InvalidOperationException("Failed to extract access code from redirect location.");

            return code;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Unexpected response from PSN authorization endpoint. Status: {response.StatusCode}, Response: {responseContent}");
    }

    private static async Task<string> ExchangeAccessCodeForTokenAsync(string accessCode)
    {
        using var client = new HttpClient();

        var payload = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = accessCode,
            ["redirect_uri"] = "com.scee.psxandroid.scecompcall://redirect",
            ["token_format"] = "jwt"
        };

        var formContent = new FormUrlEncodedContent(payload);
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
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
                throw new InvalidOperationException("Access token was null or empty");
            return token;
        }

        throw new InvalidOperationException($"Failed to extract access token from response: {responseContent}");
    }
}
