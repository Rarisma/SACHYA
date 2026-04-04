using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Sachya.Definitions.RetroAchievements;

namespace Sachya.Clients;

/// <summary>
/// Client for interacting with the RetroAchievements Web and Connect APIs.
/// </summary>
public partial class RetroAchievements : IDisposable
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
            queryParams["y"] = _webApiKey;
        }

        var url = BuildUrl(baseUrl, endpoint, queryParams);
        bool isConnectApi = baseUrl == ConnectApiBaseUrl;

        var response = await HttpRetryHandler.SendWithRetryAsync(
            _httpClient,
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (isConnectApi)
                {
                    if (string.IsNullOrWhiteSpace(ConnectApiUserAgent))
                        throw new InvalidOperationException($"{nameof(ConnectApiUserAgent)} must be set for Connect API calls.");
                    request.Headers.UserAgent.ParseAdd(ConnectApiUserAgent);
                }
                return request;
            },
            cancellationToken: cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            if ((typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>)) ||
                (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Dictionary<,>)))
            {
                return JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions) ?? Activator.CreateInstance<T>();
            }

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
