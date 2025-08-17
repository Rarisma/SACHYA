using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sachya.Clients;

public class XboxAuthService
{
    private readonly HttpClient _httpClient;
    private string? _userHash;
    private string? _xstsToken;
    private string? _xuid;
    private DateTime _tokenExpiry;
    
    // Xbox Live endpoints
    private const string UserAuthUrl = "https://user.auth.xboxlive.com/user/authenticate";
    private const string XstsAuthUrl = "https://xsts.auth.xboxlive.com/xsts/authorize";
    
    public string? Xuid => _xuid;
    public string? UserHash => _userHash;
    public string? XstsToken => _xstsToken;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_xstsToken) && DateTime.UtcNow < _tokenExpiry;

    public XboxAuthService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <summary>
    /// Authenticates with Xbox Live using a Microsoft OAuth access token
    /// </summary>
    /// <param name="microsoftAccessToken">The Microsoft OAuth access token obtained with Xbox Live scopes</param>
    /// <returns>True if authentication was successful</returns>
    public async Task<bool> AuthenticateAsync(string microsoftAccessToken)
    {
        try
        {
            // Step 1: Exchange MS token for Xbox User Token (XUT)
            var userToken = await GetXboxUserTokenAsync(microsoftAccessToken);
            if (string.IsNullOrEmpty(userToken.Token) || string.IsNullOrEmpty(userToken.UserHash))
            {
                throw new InvalidOperationException("Failed to obtain Xbox User Token");
            }
            
            _userHash = userToken.UserHash;
            
            // Step 2: Exchange User Token for XSTS token
            var xstsResponse = await GetXSTSTokenAsync(userToken.Token);
            if (string.IsNullOrEmpty(xstsResponse.Token) || string.IsNullOrEmpty(xstsResponse.Xuid))
            {
                throw new InvalidOperationException("Failed to obtain XSTS token");
            }
            
            _xstsToken = xstsResponse.Token;
            _xuid = xstsResponse.Xuid;
            
            // Tokens typically last 1 hour, refresh at 55 minutes to be safe
            _tokenExpiry = DateTime.UtcNow.AddMinutes(55);
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Xbox authentication failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Prepares an HTTP request with Xbox Live authentication headers
    /// </summary>
    public HttpRequestMessage PrepareAuthenticatedRequest(HttpMethod method, string url)
    {
        if (!IsAuthenticated)
        {
            throw new InvalidOperationException("Not authenticated with Xbox Live");
        }

        var request = new HttpRequestMessage(method, url);
        request.Headers.Clear();
        request.Headers.Add("Authorization", $"XBL3.0 x={_userHash};{_xstsToken}");
        request.Headers.Add("x-xbl-contract-version", "2");
        request.Headers.Add("Accept-Language", "en-US");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        return request;
    }

    private async Task<XboxUserTokenResponse> GetXboxUserTokenAsync(string msAccessToken)
    {
        var requestBody = new XboxUserTokenRequest
        {
            Properties = new XboxUserTokenProperties
            {
                AuthMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = $"d={msAccessToken}" // Important: Must prefix with "d="
            },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync(UserAuthUrl, content);
        var responseJson = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to get Xbox User Token: {response.StatusCode} - {responseJson}");
        }

        var tokenResponse = JsonSerializer.Deserialize<XboxUserTokenResponse>(responseJson);
        if (tokenResponse == null)
        {
            throw new InvalidOperationException("Failed to parse Xbox User Token response");
        }

        // Extract user hash from DisplayClaims
        if (tokenResponse.DisplayClaims?.Xui != null && tokenResponse.DisplayClaims.Xui.Count > 0)
        {
            tokenResponse.UserHash = tokenResponse.DisplayClaims.Xui[0].Uhs;
        }

        return tokenResponse;
    }

    private async Task<XstsTokenResponse> GetXSTSTokenAsync(string userToken)
    {
        var requestBody = new XstsTokenRequest
        {
            Properties = new XstsTokenProperties
            {
                SandboxId = "RETAIL",
                UserTokens = new[] { userToken }
            },
            RelyingParty = "http://xboxlive.com",
            TokenType = "JWT"
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync(XstsAuthUrl, content);
        var responseJson = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            // Parse error response for specific Xbox Live errors
            try
            {
                var errorResponse = JsonSerializer.Deserialize<XboxErrorResponse>(responseJson);
                if (errorResponse != null)
                {
                    var errorMessage = errorResponse.XErr switch
                    {
                        2148916233 => "Account doesn't have Xbox Live subscription",
                        2148916235 => "Account is banned from Xbox Live",
                        2148916238 => "Adult verification needed for child account",
                        _ => $"Xbox Live error {errorResponse.XErr}: {errorResponse.Message}"
                    };
                    throw new InvalidOperationException(errorMessage);
                }
            }
            catch (JsonException)
            {
                // If error parsing fails, throw generic error
            }
            
            throw new HttpRequestException($"Failed to get XSTS token: {response.StatusCode} - {responseJson}");
        }

        var tokenResponse = JsonSerializer.Deserialize<XstsTokenResponse>(responseJson);
        if (tokenResponse == null)
        {
            throw new InvalidOperationException("Failed to parse XSTS token response");
        }

        // Extract XUID from DisplayClaims
        if (tokenResponse.DisplayClaims?.Xui != null && tokenResponse.DisplayClaims.Xui.Count > 0)
        {
            tokenResponse.Xuid = tokenResponse.DisplayClaims.Xui[0].Xid;
        }

        return tokenResponse;
    }

    // Request/Response models for Xbox authentication
    private class XboxUserTokenRequest
    {
        [JsonPropertyName("Properties")]
        public XboxUserTokenProperties Properties { get; set; } = null!;
        
        [JsonPropertyName("RelyingParty")]
        public string RelyingParty { get; set; } = null!;
        
        [JsonPropertyName("TokenType")]
        public string TokenType { get; set; } = null!;
    }

    private class XboxUserTokenProperties
    {
        [JsonPropertyName("AuthMethod")]
        public string AuthMethod { get; set; } = null!;
        
        [JsonPropertyName("SiteName")]
        public string SiteName { get; set; } = null!;
        
        [JsonPropertyName("RpsTicket")]
        public string RpsTicket { get; set; } = null!;
    }

    private class XboxUserTokenResponse
    {
        [JsonPropertyName("Token")]
        public string Token { get; set; } = null!;
        
        [JsonPropertyName("DisplayClaims")]
        public XboxDisplayClaims? DisplayClaims { get; set; }
        
        // Extracted from DisplayClaims for convenience
        public string? UserHash { get; set; }
    }

    private class XstsTokenRequest
    {
        [JsonPropertyName("Properties")]
        public XstsTokenProperties Properties { get; set; } = null!;
        
        [JsonPropertyName("RelyingParty")]
        public string RelyingParty { get; set; } = null!;
        
        [JsonPropertyName("TokenType")]
        public string TokenType { get; set; } = null!;
    }

    private class XstsTokenProperties
    {
        [JsonPropertyName("SandboxId")]
        public string SandboxId { get; set; } = null!;
        
        [JsonPropertyName("UserTokens")]
        public string[] UserTokens { get; set; } = null!;
    }

    private class XstsTokenResponse
    {
        [JsonPropertyName("Token")]
        public string Token { get; set; } = null!;
        
        [JsonPropertyName("DisplayClaims")]
        public XboxDisplayClaims? DisplayClaims { get; set; }
        
        // Extracted from DisplayClaims for convenience
        public string? Xuid { get; set; }
    }

    private class XboxDisplayClaims
    {
        [JsonPropertyName("xui")]
        public List<XboxUserInfo> Xui { get; set; } = null!;
    }

    private class XboxUserInfo
    {
        [JsonPropertyName("gtg")]
        public string? Gtg { get; set; } // Gamertag
        
        [JsonPropertyName("xid")]
        public string? Xid { get; set; } // XUID
        
        [JsonPropertyName("uhs")]
        public string? Uhs { get; set; } // User Hash
    }

    private class XboxErrorResponse
    {
        [JsonPropertyName("XErr")]
        public long XErr { get; set; }
        
        [JsonPropertyName("Message")]
        public string? Message { get; set; }
        
        [JsonPropertyName("Redirect")]
        public string? Redirect { get; set; }
    }
}