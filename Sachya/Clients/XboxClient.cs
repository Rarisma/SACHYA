using Sachya.Definitions.Xbox;

namespace Sachya.Clients;

/// <summary>
/// Main Xbox Live API client that handles authentication and API calls
/// This replaces the OpenXblApiClient with direct Xbox Live API access
/// </summary>
public class XboxApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly XboxAuthService _authService;
    private readonly XboxLiveApiClient _apiClient;
    private bool _isAuthenticated;
    private Func<Task<string?>>? _tokenRefreshCallback;

    /// <summary>
    /// Creates a new Xbox API client with Microsoft OAuth token
    /// </summary>
    /// <param name="microsoftAccessToken">OAuth token obtained from Microsoft login with Xbox Live scopes</param>
    /// <param name="httpClient">Optional HttpClient instance</param>
    public XboxApiClient(string microsoftAccessToken, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _authService = new XboxAuthService(_httpClient);
        _apiClient = new XboxLiveApiClient(_authService, _httpClient);
        
        // Store token for authentication
        MicrosoftAccessToken = microsoftAccessToken;
    }

    public string MicrosoftAccessToken { get; private set; }
    public string? Xuid => _authService.Xuid;
    public bool IsAuthenticated => _isAuthenticated && _authService.IsAuthenticated;
    
    /// <summary>
    /// Sets a callback function to refresh the Microsoft access token when it expires
    /// </summary>
    public void SetTokenRefreshCallback(Func<Task<string?>> refreshCallback)
    {
        _tokenRefreshCallback = refreshCallback;
    }

    /// <summary>
    /// Authenticates with Xbox Live services
    /// Must be called before making any API calls
    /// </summary>
    public async Task<bool> AuthenticateAsync()
    {
        _isAuthenticated = await _authService.AuthenticateAsync(MicrosoftAccessToken);
        return _isAuthenticated;
    }

    /// <summary>
    /// Ensures the client is authenticated, authenticating if necessary
    /// </summary>
    private async Task EnsureAuthenticatedAsync()
    {
        // Check if we need to refresh the token
        if (!IsAuthenticated)
        {
            // First try to refresh the Microsoft token if we have a callback
            if (_tokenRefreshCallback != null)
            {
                var newToken = await _tokenRefreshCallback();
                if (!string.IsNullOrEmpty(newToken))
                {
                    MicrosoftAccessToken = newToken;
                    _isAuthenticated = await _authService.AuthenticateAsync(newToken);
                    
                    if (_isAuthenticated)
                    {
                        return;
                    }
                }
            }
            
            // Fall back to using the existing token
            await AuthenticateAsync();
        }
        
        if (!IsAuthenticated)
        {
            throw new InvalidOperationException("Failed to authenticate with Xbox Live");
        }
    }

    #region Profile Endpoints

    /// <summary>
    /// Gets the authenticated user's profile
    /// </summary>
    public async Task<ProfileResponse> GetProfileAsync()
    {
        await EnsureAuthenticatedAsync();
        
        var xboxProfile = await _apiClient.GetProfileAsync();
        
        // Convert to existing ProfileResponse format for compatibility
        return new ProfileResponse
        {
            ProfileUsers = xboxProfile.ProfileUsers.Select(u => new ProfileUser
            {
                Id = u.Id,
                Settings = u.Settings.Select(s => new ProfileSetting
                {
                    Id = s.Id,
                    Value = s.Value
                }).ToList()
            }).ToList()
        };
    }

    /// <summary>
    /// Gets a specific user's profile by XUID
    /// </summary>
    public async Task<ProfileResponse> GetProfileAsync(string xuid)
    {
        // For now, only support getting own profile
        // The real Xbox API requires different endpoints for other users
        if (xuid != _authService.Xuid)
        {
            throw new NotImplementedException("Getting other users' profiles is not yet implemented");
        }
        
        return await GetProfileAsync();
    }

    #endregion

    #region Title/Game Endpoints

    /// <summary>
    /// Gets the user's title history (game library)
    /// </summary>
    public async Task<TitleHistoryResponse> GetTitleHistoryAsync()
    {
        await EnsureAuthenticatedAsync();
        return await GetTitleHistoryAsync(_authService.Xuid!);
    }

    /// <summary>
    /// Gets a specific user's title history
    /// </summary>
    public async Task<TitleHistoryResponse> GetTitleHistoryAsync(string xuid)
    {
        await EnsureAuthenticatedAsync();
        
        // For now, only support getting own title history
        if (xuid != _authService.Xuid)
        {
            throw new NotImplementedException("Getting other users' title history is not yet implemented");
        }
        
        var xboxHistory = await _apiClient.GetTitleHistoryAsync();
        
        // Convert to existing TitleHistoryResponse format
        return new TitleHistoryResponse
        {
            Titles = xboxHistory.Titles.Select(t => new TitleHistory
            {
                TitleId = t.TitleId,
                Name = t.Name,
                DisplayImage = t.DisplayImage,
                Achievement = t.Achievement != null ? new TitleHistoryAchievement
                {
                    CurrentAchievements = t.Achievement.CurrentAchievements,
                    TotalAchievements = t.Achievement.TotalAchievements,
                    CurrentGamerscore = t.Achievement.CurrentGamerscore,
                    TotalGamerscore = t.Achievement.TotalGamerscore,
                    ProgressPercentage = t.Achievement.ProgressPercentage,
                    SourceVersion = 0
                } : null,
                Details = t.TitleHistory != null ? new TitleHistoryInfo
                {
                    LastTimePlayed = t.TitleHistory.LastTimePlayed ?? DateTime.MinValue,
                    Visible = t.TitleHistory.Visible,
                    CanHide = false
                } : null,
                Stats = t.Stats != null ? new StatsInfo
                {
                    SourceVersion = t.Stats.SourceVersion
                } : null
            }).ToList()
        };
    }

    #endregion

    #region Achievement Endpoints

    /// <summary>
    /// Gets achievements for the authenticated user
    /// </summary>
    public async Task<AchievementTitlesResponse> GetAchievementsAsync()
    {
        await EnsureAuthenticatedAsync();
        
        // Get title history which includes achievement summaries
        var titleHistory = await GetTitleHistoryAsync();
        
        // Convert to AchievementTitlesResponse format
        return new AchievementTitlesResponse
        {
            Titles = titleHistory.Titles.Select(t => new Title
            {
                TitleId = t.TitleId,
                Name = t.Name,
                DisplayImage = t.DisplayImage,
                Achievement = t.Achievement != null ? new AchievementInfo
                {
                    CurrentAchievements = t.Achievement.CurrentAchievements,
                    TotalAchievements = t.Achievement.TotalAchievements,
                    CurrentGamerscore = t.Achievement.CurrentGamerscore,
                    TotalGamerscore = t.Achievement.TotalGamerscore,
                    ProgressPercentage = (int)t.Achievement.ProgressPercentage,
                    SourceVersion = t.Achievement.SourceVersion
                } : null,
                TitleHistory = t.Details
            }).ToList()
        };
    }

    /// <summary>
    /// Gets achievements for a specific user
    /// </summary>
    public async Task<AchievementTitlesResponse> GetPlayerAchievementsAsync(string xuid)
    {
        if (xuid != _authService.Xuid)
        {
            throw new NotImplementedException("Getting other users' achievements is not yet implemented");
        }
        
        return await GetAchievementsAsync();
    }

    /// <summary>
    /// Gets achievements for a specific user and title
    /// </summary>
    public async Task<AchievementTitlesResponse> GetPlayerAchievementsAsync(string xuid, string titleId)
    {
        await EnsureAuthenticatedAsync();
        
        if (xuid != _authService.Xuid)
        {
            throw new NotImplementedException("Getting other users' achievements is not yet implemented");
        }
        
        // Get title history to find the specific title
        var titleHistory = await GetTitleHistoryAsync();
        var title = titleHistory.Titles.FirstOrDefault(t => t.TitleId == titleId);
        
        if (title == null)
        {
            return new AchievementTitlesResponse { Titles = new List<Title>() };
        }
        
        // Return just this title
        return new AchievementTitlesResponse
        {
            Titles = new List<Title>
            {
                new Title
                {
                    TitleId = title.TitleId,
                    Name = title.Name,
                    DisplayImage = title.DisplayImage,
                    Achievement = title.Achievement != null ? new AchievementInfo
                    {
                        CurrentAchievements = title.Achievement.CurrentAchievements,
                        TotalAchievements = title.Achievement.TotalAchievements,
                        CurrentGamerscore = title.Achievement.CurrentGamerscore,
                        TotalGamerscore = title.Achievement.TotalGamerscore,
                        ProgressPercentage = (int)title.Achievement.ProgressPercentage,
                        SourceVersion = title.Achievement.SourceVersion
                    } : null,
                    TitleHistory = title.Details
                }
            }
        };
    }

    /// <summary>
    /// Gets player achievements for a specific title with another endpoint
    /// </summary>
    public async Task<AchievementTitlesResponse> GetPlayerTitleAchievementsAsync(string xuid, string titleId)
    {
        // This is essentially the same as GetPlayerAchievementsAsync(xuid, titleId)
        return await GetPlayerAchievementsAsync(xuid, titleId);
    }

    /// <summary>
    /// Gets Xbox 360 achievements for a specific user and title
    /// </summary>
    public async Task<AchievementTitlesResponse> GetXbox360AchievementsAsync(string xuid, string titleId)
    {
        // The real Xbox API handles both Xbox 360 and modern titles the same way
        return await GetPlayerAchievementsAsync(xuid, titleId);
    }

    /// <summary>
    /// Gets detailed achievements for a specific title
    /// </summary>
    public async Task<TitleAchievementResponse> GetTitleAchievementsAsync(string titleId)
    {
        await EnsureAuthenticatedAsync();
        
        var achievements = await _apiClient.GetAchievementsAsync(titleId);
        
        // Convert to TitleAchievementResponse format
        return new TitleAchievementResponse
        {
            Achievements = achievements.Achievements.Select(a => new TitleAchievement
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.IsSecret && a.ProgressState != "Achieved" 
                    ? a.LockedDescription ?? "Secret achievement" 
                    : a.Description ?? string.Empty,
                Icon = a.ImageLocked?.Url ?? a.ImageUnlocked?.Url ?? string.Empty,
                Gamerscore = a.Rewards?.FirstOrDefault(r => r.Type == "Gamerscore")?.Value ?? "0",
                DisplayBeforeEarned = (!a.IsSecret).ToString()
            }).ToList(),
            PagingInfo = achievements.PagingInfo != null ? new PagingInfo
            {
                ContinuationToken = achievements.PagingInfo.ContinuationToken,
                TotalRecords = achievements.PagingInfo.TotalRecords
            } : null
        };
    }

    /// <summary>
    /// Gets detailed achievements for multiple titles
    /// </summary>
    public async Task<AchievementResponse> GetMultipleTitleAchievementsAsync(string titleIds)
    {
        await EnsureAuthenticatedAsync();
        
        // For now, only support single title ID
        var titleId = titleIds.Split(',')[0].Trim();
        var achievements = await _apiClient.GetAchievementsAsync(titleId);
        
        // Convert to AchievementResponse format with full Achievement objects
        return new AchievementResponse
        {
            Achievements = achievements.Achievements.Select(a => new Achievement
            {
                Id = a.Id,
                ServiceConfigId = a.ServiceConfigId ?? string.Empty,
                Name = a.Name,
                TitleId = titleId,
                Description = a.Description,
                LockedDescription = a.LockedDescription,
                UnlockedDescription = a.UnlockedDescription,
                Icon = a.ImageLocked?.Url ?? a.ImageUnlocked?.Url ?? string.Empty,
                IsSecret = a.IsSecret,
                Gamerscore = a.Rewards?.FirstOrDefault(r => r.Type == "Gamerscore")?.Value ?? "0",
                IsRevoked = false,
                Rarity = a.Rarity?.CurrentCategory,
                Progressions = a.Progression != null ? new List<Progression>
                {
                    new Progression
                    {
                        TimeUnlocked = a.Progression.TimeUnlocked ?? DateTime.MinValue,
                        Requirements = a.Progression.Requirements?.Select(r => new Requirement
                        {
                            Id = r.Id ?? string.Empty,
                            Current = r.Current ?? "0",
                            Target = r.Target ?? "0"
                        }).ToList() ?? new List<Requirement>()
                    }
                } : new List<Progression>()
            }).ToList(),
            PagingInfo = achievements.PagingInfo != null ? new PagingInfo
            {
                ContinuationToken = achievements.PagingInfo.ContinuationToken,
                TotalRecords = achievements.PagingInfo.TotalRecords
            } : null
        };
    }

    #endregion

    #region Stub Methods for Compatibility

    // These methods exist for API compatibility but are not implemented with the real Xbox API
    
    public Task<SearchResponse> SearchPlayerAsync(string gamertag)
    {
        throw new NotImplementedException("Player search is not available with direct Xbox API");
    }

    public Task<AlertsResponse> GetAlertsAsync()
    {
        throw new NotImplementedException("Alerts are not available with direct Xbox API");
    }

    public Task<GenerateGamertagResponse> GenerateGamertagAsync(GenerateGamertagRequest request)
    {
        throw new NotImplementedException("Gamertag generation is not available with direct Xbox API");
    }

    public Task<PresenceResponse> GetFriendsPresenceAsync()
    {
        throw new NotImplementedException("Friends presence is not available with direct Xbox API");
    }

    public Task<PresenceResponse> GetPresenceAsync(string xuid)
    {
        throw new NotImplementedException("Presence is not available with direct Xbox API");
    }

    public Task<AchievementStatsResponse> GetAchievementStatsAsync(string titleId)
    {
        throw new NotImplementedException("Achievement stats are not available with direct Xbox API");
    }

    public Task<TitleAchievementResponse> GetTitleAchievementsAsync(string titleId, string continuationToken)
    {
        throw new NotImplementedException("Achievement pagination is not yet implemented");
    }

    public Task<PlayerSummaryResponse> GetPlayerSummaryAsync()
    {
        throw new NotImplementedException("Player summary is not available with direct Xbox API");
    }

    public Task<PlayerSummaryResponse> GetPlayerSummaryAsync(string xuid)
    {
        throw new NotImplementedException("Player summary is not available with direct Xbox API");
    }

    #endregion

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}