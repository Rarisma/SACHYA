using System.Text;
using System.Text.Json;
using Sachya.Definitions.Xbox;

namespace Sachya.Clients;
public class OpenXblApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://xbl.io";

    public OpenXblApiClient(string apiKey, HttpClient httpClient = null)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("x-authorization", _apiKey);
    }

    // Account endpoints
    public async Task<ProfileResponse> GetProfileAsync()
    {
        return await GetAsync<ProfileResponse>("/api/v2/account");
    }

    public async Task<ProfileResponse> GetProfileAsync(string xuid)
    {
        return await GetAsync<ProfileResponse>($"/api/v2/account/{xuid}");
    }

    public async Task<SearchResponse> SearchPlayerAsync(string gamertag)
    {
        return await GetAsync<SearchResponse>($"/api/v2/search/{gamertag}");
    }

    public async Task<AlertsResponse> GetAlertsAsync()
    {
        return await GetAsync<AlertsResponse>("/api/v2/alerts");
    }

    public async Task<GenerateGamertagResponse> GenerateGamertagAsync(GenerateGamertagRequest request)
    {
        return await PostAsync<GenerateGamertagResponse>("/api/v2/generate/gamertag", request);
    }

    // Presence endpoints
    public async Task<PresenceResponse> GetFriendsPresenceAsync()
    {
        return await GetAsync<PresenceResponse>("/api/v2/presence");
    }

    public async Task<PresenceResponse> GetPresenceAsync(string xuid)
    {
        return await GetAsync<PresenceResponse>($"/api/v2/{xuid}/presence");
    }

    // Achievement endpoints
    public async Task<AchievementTitlesResponse> GetAchievementsAsync()
    {
        return await GetAsync<AchievementTitlesResponse>("/api/v2/achievements");
    }

    public async Task<AchievementTitlesResponse> GetPlayerAchievementsAsync(string xuid)
    {
        return await GetAsync<AchievementTitlesResponse>($"/api/v2/achievements/player/{xuid}");
    }

    public async Task<AchievementTitlesResponse> GetPlayerAchievementsAsync(string xuid, string titleId)
    {
        return await GetAsync<AchievementTitlesResponse>($"/api/v2/achievements/player/{xuid}/{titleId}");
    }

    public async Task<AchievementTitlesResponse> GetPlayerTitleAchievementsAsync(string xuid, string titleId)
    {
        return await GetAsync<AchievementTitlesResponse>($"/api/v2/achievements/player/{xuid}/title/{titleId}");
    }

    public async Task<AchievementTitlesResponse> GetXbox360AchievementsAsync(string xuid, string titleId)
    {
        return await GetAsync<AchievementTitlesResponse>($"/api/v2/achievements/x360/{xuid}/title/{titleId}");
    }

    public async Task<AchievementStatsResponse> GetAchievementStatsAsync(string titleId)
    {
        return await GetAsync<AchievementStatsResponse>($"/api/v2/achievements/stats/{titleId}");
    }

    public async Task<TitleAchievementResponse> GetTitleAchievementsAsync(string titleId)
    {
        return await GetAsync<TitleAchievementResponse>($"/api/v2/achievements/title/{titleId}");
    }

    public async Task<TitleAchievementResponse> GetTitleAchievementsAsync(string titleId, string continuationToken)
    {
        return await GetAsync<TitleAchievementResponse>($"/api/v2/achievements/title/{titleId}/{continuationToken}");
    }

    public async Task<AchievementResponse> GetMultipleTitleAchievementsAsync(string titleIds)
    {
        return await GetAsync<AchievementResponse>($"/api/v2/achievements/{titleIds}");
    }

    // Activity endpoints
    public async Task<ActivityFeedResponse> GetActivityFeedAsync()
    {
        return await GetAsync<ActivityFeedResponse>("/api/v2/activity/feed");
    }

    public async Task<ActivityPostResponse> PostToActivityFeedAsync(ActivityPostRequest request)
    {
        return await PostAsync<ActivityPostResponse>("/api/v2/activity/feed", request);
    }

    public async Task<ActivityHistoryResponse> GetActivityHistoryAsync()
    {
        return await GetAsync<ActivityHistoryResponse>("/api/v2/activity/history");
    }

    public async Task<ShareResponse> CreateShareableLinkAsync(ShareRequest request)
    {
        return await PostAsync<ShareResponse>("/api/v2/activity/share", request);
    }

    // Club endpoints
    public async Task<ClubRecommendationsResponse> GetClubRecommendationsAsync()
    {
        return await PostAsync<ClubRecommendationsResponse>("/api/v2/clubs/recommendations", new object());
    }

    public async Task<ClubDetailsResponse> GetClubDetailsAsync(string clubId)
    {
        return await GetAsync<ClubDetailsResponse>($"/api/v2/clubs/{clubId}");
    }

    public async Task<ClubInviteResponse> InviteToClubAsync(string clubId, string xuid)
    {
        return await PostAsync<ClubInviteResponse>($"/api/v2/clubs/{clubId}/invite/{xuid}", new object());
    }

    public async Task<OwnedClubsResponse> GetOwnedClubsAsync()
    {
        return await GetAsync<OwnedClubsResponse>("/api/v2/clubs/owned");
    }

    public async Task<CreateClubResponse> CreateClubAsync(CreateClubRequest request)
    {
        return await PostAsync<CreateClubResponse>("/api/v2/clubs/create", request);
    }

    public async Task<FindClubsResponse> FindClubsAsync(string query)
    {
        return await GetAsync<FindClubsResponse>($"/api/v2/clubs/find?q={Uri.EscapeDataString(query)}");
    }

    public async Task<ReserveClubResponse> ReserveClubNameAsync(ReserveClubRequest request)
    {
        return await PostAsync<ReserveClubResponse>("/api/v2/clubs/reserve", request);
    }

    public async Task<DeleteClubResponse> DeleteClubAsync(string clubId)
    {
        return await GetAsync<DeleteClubResponse>($"/api/v2/clubs/delete/{clubId}");
    }

    // Conversation endpoints
    public async Task<ConversationsResponse> GetConversationsAsync()
    {
        return await GetAsync<ConversationsResponse>("/api/v2/conversations");
    }

    public async Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request)
    {
        return await PostAsync<SendMessageResponse>("/api/v2/conversations", request);
    }

    public async Task<ConversationRequestsResponse> GetConversationRequestsAsync()
    {
        return await GetAsync<ConversationRequestsResponse>("/api/v2/conversations/requests");
    }

    // DVR endpoints
    public async Task<ScreenshotsResponse> GetScreenshotsAsync()
    {
        return await GetAsync<ScreenshotsResponse>("/api/v2/dvr/screenshots");
    }

    public async Task<GameClipsResponse> GetGameClipsAsync()
    {
        return await GetAsync<GameClipsResponse>("/api/v2/dvr/gameclips");
    }

    public async Task<DeleteGameClipResponse> DeleteGameClipAsync(string gameClipId)
    {
        return await GetAsync<DeleteGameClipResponse>($"/api/v2/dvr/gameclips/delete/{gameClipId}");
    }

    public async Task<SetPrivacyResponse> SetDvrPrivacyAsync(SetPrivacyRequest request)
    {
        return await PostAsync<SetPrivacyResponse>("/api/v2/dvr/privacy", request);
    }

    // Friends endpoints
    public async Task<FriendsResponse> GetFriendsAsync()
    {
        return await GetAsync<FriendsResponse>("/api/v2/friends");
    }

    public async Task<SearchFriendsResponse> SearchFriendsAsync(string gamertag)
    {
        return await GetAsync<SearchFriendsResponse>($"/api/v2/friends/search/{gamertag}");
    }

    public async Task<AddFriendsResponse> AddFriendsAsync(AddFriendsRequest request)
    {
        return await PostAsync<AddFriendsResponse>("/api/v2/friends/add", request);
    }

    public async Task<RemoveFriendResponse> RemoveFriendAsync(RemoveFriendRequest request)
    {
        return await PostAsync<RemoveFriendResponse>("/api/v2/friends/remove", request);
    }

    public async Task<RecentPlayersResponse> GetRecentPlayersAsync()
    {
        return await GetAsync<RecentPlayersResponse>("/api/v2/recent-players");
    }

    public async Task<FavoritesResponse> AddFavoritesAsync(FavoritesRequest request)
    {
        return await PostAsync<FavoritesResponse>("/api/v2/friends/favorite", request);
    }

    public async Task<FavoritesResponse> ManageFavoritesAsync(string method, FavoritesRequest request)
    {
        return await PostAsync<FavoritesResponse>($"/api/v2/friends/favorite/{method}", request);
    }

    // Game Pass endpoints
    public async Task<GamePassResponse> GetAllGamePassGamesAsync()
    {
        return await GetAsync<GamePassResponse>("/api/v2/gamepass/all");
    }

    public async Task<GamePassResponse> GetPcGamePassGamesAsync()
    {
        return await GetAsync<GamePassResponse>("/api/v2/gamepass/pc");
    }

    public async Task<GamePassResponse> GetEaPlayGamesAsync()
    {
        return await GetAsync<GamePassResponse>("/api/v2/gamepass/ea-play");
    }

    public async Task<GamePassResponse> GetNoControllerGamesAsync()
    {
        return await GetAsync<GamePassResponse>("/api/v2/gamepass/no-controller");
    }

    // Group endpoints
    public async Task<GroupConversationsResponse> GetGroupConversationsAsync()
    {
        return await GetAsync<GroupConversationsResponse>("/api/v2/group");
    }

    public async Task<CreateGroupResponse> CreateGroupAsync(CreateGroupRequest request)
    {
        return await PostAsync<CreateGroupResponse>("/api/v2/group/create", request);
    }

    public async Task<SendGroupMessageResponse> SendGroupMessageAsync(SendGroupMessageRequest request)
    {
        return await PostAsync<SendGroupMessageResponse>("/api/v2/group/send", request);
    }

    public async Task<GroupSummaryResponse> GetGroupSummaryAsync(string groupId)
    {
        return await GetAsync<GroupSummaryResponse>($"/api/v2/group/summary/{groupId}");
    }

    public async Task<GroupMessagesResponse> GetGroupMessagesAsync(string groupId)
    {
        return await GetAsync<GroupMessagesResponse>($"/api/v2/group/messages/{groupId}");
    }

    public async Task<InviteVoiceChatResponse> InviteToVoiceChatAsync(InviteVoiceChatRequest request)
    {
        return await PostAsync<InviteVoiceChatResponse>("/api/v2/group/invite/voice", request);
    }

    public async Task<InviteGroupResponse> InviteToGroupAsync(InviteGroupRequest request)
    {
        return await PostAsync<InviteGroupResponse>("/api/v2/group/invite", request);
    }

    public async Task<KickFromGroupResponse> KickFromGroupAsync(KickFromGroupRequest request)
    {
        return await PostAsync<KickFromGroupResponse>("/api/v2/group/kick", request);
    }

    public async Task<LeaveGroupResponse> LeaveGroupAsync(LeaveGroupRequest request)
    {
        return await PostAsync<LeaveGroupResponse>("/api/v2/group/leave", request);
    }

    // Marketplace endpoints
    public async Task<MarketplaceResponse> GetNewGamesAsync()
    {
        return await GetAsync<MarketplaceResponse>("/api/v2/marketplace/new");
    }

    public async Task<MarketplaceResponse> GetTopPaidGamesAsync()
    {
        return await GetAsync<MarketplaceResponse>("/api/v2/marketplace/top-paid");
    }

    public async Task<MarketplaceResponse> GetBestRatedGamesAsync()
    {
        return await GetAsync<MarketplaceResponse>("/api/v2/marketplace/best-rated");
    }

    public async Task<MarketplaceResponse> GetComingSoonGamesAsync()
    {
        return await GetAsync<MarketplaceResponse>("/api/v2/marketplace/coming-soon");
    }

    public async Task<MarketplaceResponse> GetDealsAsync()
    {
        return await GetAsync<MarketplaceResponse>("/api/v2/marketplace/deals");
    }

    public async Task<MarketplaceResponse> GetTopFreeGamesAsync()
    {
        return await GetAsync<MarketplaceResponse>("/api/v2/marketplace/top-free");
    }

    public async Task<MarketplaceResponse> GetMostPlayedGamesAsync()
    {
        return await GetAsync<MarketplaceResponse>("/api/v2/marketplace/most-played");
    }

    public async Task<GameDetailsResponse> GetGameDetailsAsync(GameDetailsRequest request)
    {
        return await PostAsync<GameDetailsResponse>("/api/v2/marketplace/details", request);
    }

    // Player endpoints
    public async Task<PlayerSummaryResponse> GetPlayerSummaryAsync()
    {
        return await GetAsync<PlayerSummaryResponse>("/api/v2/player/summary");
    }

    public async Task<PlayerSummaryResponse> GetPlayerSummaryAsync(string xuid)
    {
        return await GetAsync<PlayerSummaryResponse>($"/api/v2/player/summary/{xuid}");
    }

    public async Task<PlayerStatsResponse> GetPlayerStatsAsync(PlayerStatsRequest request)
    {
        return await PostAsync<PlayerStatsResponse>("/api/v2/player/stats", request);
    }

    public async Task<TitleHistoryResponse> GetTitleHistoryAsync()
    {
        return await GetAsync<TitleHistoryResponse>("/api/v2/player/titleHistory");
    }

    public async Task<TitleHistoryResponse> GetTitleHistoryAsync(string xuid)
    {
        return await GetAsync<TitleHistoryResponse>($"/api/v2/player/titleHistory/{xuid}");
    }

    // Session endpoints
    public async Task<SessionResponse> GetSessionsAsync()
    {
        return await GetAsync<SessionResponse>("/api/v2/session");
    }

    public async Task<SessionInviteResponse> InviteToSessionAsync(string sessionId, SessionInviteRequest request)
    {
        return await PostAsync<SessionInviteResponse>($"/api/v2/session/invite/{sessionId}", request);
    }

    public async Task<CreateSessionResponse> CreateSessionAsync()
    {
        return await GetAsync<CreateSessionResponse>("/api/v2/session/create");
    }

    public async Task<SessionConfigResponse> GetSessionConfigAsync()
    {
        return await GetAsync<SessionConfigResponse>("/api/v2/session/config");
    }

    // Helper methods
    private async Task<T> GetAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private async Task<T> PostAsync<T>(string endpoint, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
