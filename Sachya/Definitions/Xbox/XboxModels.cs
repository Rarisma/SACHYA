using System.Text.Json.Serialization;

namespace Sachya.Definitions.Xbox;
// Base response models
    // The root response object
    public class TitleHistoryResponse
    {
        [JsonPropertyName("titles")]
        public List<TitleHistory> Titles { get; set; }
    }

    // Represents a single game title in the list
    public class TitleHistory
    {
        [JsonPropertyName("titleId")]
        public string TitleId { get; set; }
        
        [JsonPropertyName("pfn")]
        public string Pfn { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("displayImage")]
        public string DisplayImage { get; set; }

        [JsonPropertyName("mediaItemType")]
        public string MediaItemType { get; set; }
        
        [JsonPropertyName("devices")]
        public List<string> Devices { get; set; }

        [JsonPropertyName("achievement")]
        public TitleHistoryAchievement Achievement { get; set; }

        [JsonPropertyName("gamePass")]
        public GamePassInfo GamePass { get; set; }

        [JsonPropertyName("stats")]
        public StatsInfo Stats { get; set; }

        [JsonPropertyName("titleHistory")]
        public TitleHistoryInfo Details { get; set; } // Renamed to avoid conflict
    }

    // Corrected Achievement Info
    public class TitleHistoryAchievement
    {
        [JsonPropertyName("currentAchievements")]
        public int CurrentAchievements { get; set; }

        [JsonPropertyName("totalAchievements")]
        public int TotalAchievements { get; set; }

        [JsonPropertyName("currentGamerscore")]
        public int CurrentGamerscore { get; set; }

        [JsonPropertyName("totalGamerscore")]
        public int TotalGamerscore { get; set; }

        [JsonPropertyName("progressPercentage")]
        public double ProgressPercentage { get; set; }

        [JsonPropertyName("sourceVersion")]
        public int SourceVersion { get; set; } // FIX: Changed from string to int
    }

    // New class for "stats" object
    public class StatsInfo
    {
        [JsonPropertyName("sourceVersion")]
        public int SourceVersion { get; set; }
    }

    // New class for "gamePass" object
    public class GamePassInfo
    {
        [JsonPropertyName("isGamePass")]
        public bool IsGamePass { get; set; }
    }
    
    // Unchanged from before
    public class TitleHistoryInfo
    {
        [JsonPropertyName("lastTimePlayed")]
        public DateTime LastTimePlayed { get; set; }

        [JsonPropertyName("visible")]
        public bool Visible { get; set; }

        [JsonPropertyName("canHide")]
        public bool CanHide { get; set; }
    }
    // This is the root object for the response
    public class AchievementTitlesResponse
    {
        [JsonPropertyName("titles")]
        public List<Title> Titles { get; set; }
    }

    public class Title
    {
        [JsonPropertyName("titleId")]
        public string TitleId { get; set; }

        [JsonPropertyName("pfn")]
        public string Pfn { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("devices")]
        public List<string> Devices { get; set; }

        [JsonPropertyName("displayImage")]
        public string DisplayImage { get; set; }

        [JsonPropertyName("mediaItemType")]
        public string MediaItemType { get; set; }

        [JsonPropertyName("modernTitleId")]
        public string ModernTitleId { get; set; }

        [JsonPropertyName("isBundle")]
        public bool IsBundle { get; set; }

        [JsonPropertyName("achievement")]
        public AchievementInfo Achievement { get; set; }

        [JsonPropertyName("images")]
        public List<ImageInfo> Images { get; set; }

        [JsonPropertyName("titleHistory")]
        public TitleHistoryInfo TitleHistory { get; set; }

        [JsonPropertyName("xboxLiveTier")]
        public string XboxLiveTier { get; set; }

        [JsonPropertyName("isStreamable")]
        public bool? IsStreamable { get; set; }
    }

    public class AchievementInfo
    {
        [JsonPropertyName("currentAchievements")]
        public int CurrentAchievements { get; set; }

        [JsonPropertyName("totalAchievements")]
        public int TotalAchievements { get; set; }

        [JsonPropertyName("currentGamerscore")]
        public int CurrentGamerscore { get; set; }

        [JsonPropertyName("totalGamerscore")]
        public int TotalGamerscore { get; set; }

        [JsonPropertyName("progressPercentage")]
        public int ProgressPercentage { get; set; }

        [JsonPropertyName("sourceVersion")]
        public int SourceVersion { get; set; }
    }

    public class ImageInfo
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("caption")]
        public string Caption { get; set; }
    }

public class BaseResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
}

// Profile models
public class ProfileResponse
{
    public List<ProfileUser> ProfileUsers { get; set; }
}

public class ProfileUser
{
    public string Id { get; set; }
    public string HostId { get; set; }
    public List<ProfileSetting> Settings { get; set; }
    public bool IsSponsoredUser { get; set; }
}

public class ProfileSetting
{
    public string Id { get; set; }
    public string Value { get; set; }
}

// Search models
public class SearchResponse
{
    public List<SearchResult> Results { get; set; }
}

public class SearchResult
{
    public string Xuid { get; set; }
    public string Gamertag { get; set; }
    public string DisplayPictureRaw { get; set; }
}

// Alerts models
public class AlertsResponse
{
    public List<Alert> Alerts { get; set; }
}

public class Alert
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Gamertag generation models
public class GenerateGamertagRequest
{
    public int Algorithm { get; set; }
    public int Count { get; set; }
    public string Seed { get; set; }
    public string Locale { get; set; }
}

public class GenerateGamertagResponse
{
    public List<string> Gamertags { get; set; }
}

// Presence models
public class PresenceResponse
{
    public List<PresenceRecord> PresenceRecords { get; set; }
}

public class PresenceRecord
{
    public string Xuid { get; set; }
    public string State { get; set; }
    public List<DeviceRecord> Devices { get; set; }
}

public class DeviceRecord
{
    public string Type { get; set; }
    public List<TitleRecord> Titles { get; set; }
}

public class TitleRecord
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string State { get; set; }
    public string Placement { get; set; }
    public DateTime LastModified { get; set; }
}

// Achievement models
public class AchievementResponse
{
    public List<Achievement> Achievements { get; set; }
    public PagingInfo PagingInfo { get; set; }
}

public class Achievement
{
    public string Id { get; set; }
    public string ServiceConfigId { get; set; }
    public string Name { get; set; }
    public string TitleId { get; set; }
    public string Description { get; set; }
    public string LockedDescription { get; set; }
    public string Icon { get; set; }
    public bool IsSecret { get; set; }
    public string UnlockedDescription { get; set; }
    public string Gamerscore { get; set; }
    public bool IsRevoked { get; set; }
    public List<TimeWindow> TimeWindow { get; set; }
    public List<Reward> Rewards { get; set; }
    public string EstimatedTime { get; set; }
    public string Deeplink { get; set; }
    public bool IsRetail { get; set; }
    public string Rarity { get; set; }
    public List<Progression> Progressions { get; set; }
}

public class TimeWindow
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class Reward
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Value { get; set; }
    public string Type { get; set; }
    public object ValueType { get; set; }
}

public class Progression
{
    public List<Requirement> Requirements { get; set; }
    public DateTime TimeUnlocked { get; set; }
}

public class Requirement
{
    public string Id { get; set; }
    public string Current { get; set; }
    public string Target { get; set; }
}

public class PagingInfo
{
    public string ContinuationToken { get; set; }
    public int TotalRecords { get; set; }
}

public class AchievementStatsResponse
{
    public List<AchievementStat> Stats { get; set; }
}

public class AchievementStat
{
    public string Name { get; set; }
    public string TitleId { get; set; }
    public object Value { get; set; }
}

public class TitleAchievementResponse
{
    public List<TitleAchievement> Achievements { get; set; }
    public PagingInfo PagingInfo { get; set; }
}

public class TitleAchievement
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public string Gamerscore { get; set; }
    public string DisplayBeforeEarned { get; set; }
}

// Activity models
public class ActivityFeedResponse
{
    public List<ActivityItem> ActivityItems { get; set; }
    public PagingInfo PagingInfo { get; set; }
}

public class ActivityItem
{
    public string ActivityId { get; set; }
    public string SessionId { get; set; }
    public string ContentType { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public bool HasUgc { get; set; }
    public string TitleId { get; set; }
    public string Platform { get; set; }
    public ActivityUser User { get; set; }
}

public class ActivityUser
{
    public string FollowersCount { get; set; }
    public string FollowingCount { get; set; }
    public bool HasGamercardAccess { get; set; }
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string RealName { get; set; }
    public string DisplayPicRaw { get; set; }
    public bool ShowUserAsAvatar { get; set; }
    public string Gamertag { get; set; }
    public string Gamerscore { get; set; }
    public string XboxOneRep { get; set; }
    public string PresenceState { get; set; }
    public string PresenceText { get; set; }
    public List<PresenceDevice> PresenceDevices { get; set; }
    public bool IsBroadcasting { get; set; }
    public bool IsCloaked { get; set; }
    public bool IsFollowedByCaller { get; set; }
    public bool IsFollowingCaller { get; set; }
    public bool IsIdentityShared { get; set; }
    public bool AddedDateTimeUtc { get; set; }
    public string DisplayOrderHints { get; set; }
    public string LegacyScreenshots { get; set; }
    public string Suggestion { get; set; }
    public string Recommendation { get; set; }
    public string Search { get; set; }
    public string TitleHistory { get; set; }
    public string MultiplayerSummary { get; set; }
    public string RecentPlayer { get; set; }
    public string Follower { get; set; }
    public string PreferredColor { get; set; }
    public string PresenceDetails { get; set; }
    public string TitlePresence { get; set; }
    public string TitleSummaries { get; set; }
    public string PresenceTitleIds { get; set; }
    public string Detail { get; set; }
}

public class PresenceDevice
{
    public string Type { get; set; }
    public List<PresenceTitle> Titles { get; set; }
}

public class PresenceTitle
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Placement { get; set; }
    public string State { get; set; }
    public DateTime LastModified { get; set; }
}

public class ActivityPostRequest
{
    public string Message { get; set; }
}

public class ActivityPostResponse : BaseResponse
{
    public string ActivityId { get; set; }
}

public class ActivityHistoryResponse
{
    public List<ActivityItem> ActivityItems { get; set; }
    public PagingInfo PagingInfo { get; set; }
}

public class ShareRequest
{
    public string Locator { get; set; }
}

public class ShareResponse : BaseResponse
{
    public string ShareUrl { get; set; }
    public string ShortUrl { get; set; }
}

// Club models
public class ClubRecommendationsResponse
{
    public List<Club> Clubs { get; set; }
}

public class Club
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ClubType Type { get; set; }
    public string Tags { get; set; }
    public ClubOwner Owner { get; set; }
    public ClubProfile Profile { get; set; }
    public int FollowerCount { get; set; }
    public int MembershipCount { get; set; }
    public ClubRoster Roster { get; set; }
    public ClubTargetRoleInfos TargetRoleInfos { get; set; }
    public ClubRecommendationInfo RecommendationInfo { get; set; }
    public DateTime CreationDateUtc { get; set; }
    public ClubState State { get; set; }
    public ClubSuspendedInfo SuspendedInfo { get; set; }
    public List<ClubDisplayClaim> DisplayClaims { get; set; }
    public ClubClubPresenceInfo ClubPresenceInfo { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}

public enum ClubType
{
    Open = 1,
    Private = 2,
    Hidden = 3
}

public class ClubOwner
{
    public string Xuid { get; set; }
}

public class ClubProfile
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> Tags { get; set; }
    public ClubSettings Settings { get; set; }
}

public class ClubSettings
{
    public bool IsSearchable { get; set; }
    public bool IsRecommendable { get; set; }
    public bool RequestToJoinEnabled { get; set; }
    public bool OpenJoinEnabled { get; set; }
    public bool LeaveEnabled { get; set; }
    public bool TransferOwnershipEnabled { get; set; }
    public string DisplayImageUrl { get; set; }
    public string BackgroundImageUrl { get; set; }
    public ClubPreferredLocale PreferredLocale { get; set; }
    public List<string> AssociatedTitles { get; set; }
    public string PrimaryColor { get; set; }
    public string SecondaryColor { get; set; }
    public string TertiaryColor { get; set; }
    public string PrimaryFontColor { get; set; }
    public string SecondaryFontColor { get; set; }
    public string TertiaryFontColor { get; set; }
}

public class ClubPreferredLocale
{
    public string Locale { get; set; }
}

public class ClubRoster
{
    public List<ClubMember> Members { get; set; }
}

public class ClubMember
{
    public string Xuid { get; set; }
    public List<string> Roles { get; set; }
    public DateTime JoinTime { get; set; }
    public DateTime LastSeenTime { get; set; }
}

public class ClubTargetRoleInfos
{
    public List<ClubTargetRoleInfo> TargetRoles { get; set; }
}

public class ClubTargetRoleInfo
{
    public string Role { get; set; }
    public int CurrentCount { get; set; }
    public int TargetCount { get; set; }
}

public class ClubRecommendationInfo
{
    public List<string> Reasons { get; set; }
}

public class ClubState
{
    public string StateReason { get; set; }
}

public class ClubSuspendedInfo
{
    public DateTime SuspendedUtc { get; set; }
    public string Reason { get; set; }
}

public class ClubDisplayClaim
{
    public string ClaimType { get; set; }
    public string ClaimValue { get; set; }
}

public class ClubClubPresenceInfo
{
    public DateTime LastSeenTimestamp { get; set; }
}

public class ClubDetailsResponse
{
    public Club Club { get; set; }
}

public class ClubInviteResponse : BaseResponse
{
}

public class OwnedClubsResponse
{
    public List<Club> Clubs { get; set; }
}

public class CreateClubRequest
{
    public string Name { get; set; }
    public ClubType Type { get; set; }
}

public class CreateClubResponse : BaseResponse
{
    public string ClubId { get; set; }
}

public class FindClubsResponse
{
    public List<Club> Clubs { get; set; }
}

public class ReserveClubRequest
{
    public string Name { get; set; }
}

public class ReserveClubResponse : BaseResponse
{
    public string ReservationToken { get; set; }
}

public class DeleteClubResponse : BaseResponse
{
}

// Conversation models
public class ConversationsResponse
{
    public List<Conversation> Conversations { get; set; }
}

public class Conversation
{
    public string ConversationId { get; set; }
    public List<ConversationUser> Users { get; set; }
    public ConversationMessage LastMessage { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class ConversationUser
{
    public string Xuid { get; set; }
    public string Gamertag { get; set; }
    public string DisplayPictureRaw { get; set; }
}

public class ConversationMessage
{
    public string Id { get; set; }
    public string Text { get; set; }
    public string Type { get; set; }
    public DateTime Sent { get; set; }
    public string Sender { get; set; }
}

public class SendMessageRequest
{
    public string Message { get; set; }
    public string Xuid { get; set; }
}

public class SendMessageResponse : BaseResponse
{
    public string MessageId { get; set; }
}

public class ConversationRequestsResponse
{
    public List<ConversationRequest> Requests { get; set; }
}

public class ConversationRequest
{
    public string RequestId { get; set; }
    public ConversationUser From { get; set; }
    public DateTime CreatedAt { get; set; }
}

// DVR models
public class ScreenshotsResponse
{
    public List<Screenshot> Screenshots { get; set; }
    public PagingInfo PagingInfo { get; set; }
}

public class Screenshot
{
    public string ScreenshotId { get; set; }
    public string ResolutionHeight { get; set; }
    public string ResolutionWidth { get; set; }
    public string State { get; set; }
    public DateTime DatePublished { get; set; }
    public DateTime DateTaken { get; set; }
    public DateTime LastModified { get; set; }
    public List<ScreenshotUri> ScreenshotUris { get; set; }
    public string ScId { get; set; }
    public string TitleId { get; set; }
    public int Rating { get; set; }
    public int RatingCount { get; set; }
    public List<ScreenshotView> Views { get; set; }
    public string TitleData { get; set; }
    public bool SystemProperties { get; set; }
    public bool Shared { get; set; }
    public List<string> CommentIds { get; set; }
    public int CommentCount { get; set; }
    public List<string> LikeIds { get; set; }
    public int LikeCount { get; set; }
    public string ContentImageUri { get; set; }
    public string ThumbnailImageUri { get; set; }
    public string AuthorType { get; set; }
    public ScreenshotTitleInfo TitleInfo { get; set; }
    public ScreenshotOwner Owner { get; set; }
}

public class ScreenshotUri
{
    public string Uri { get; set; }
    public string FileSize { get; set; }
    public string UriType { get; set; }
    public DateTime Expiration { get; set; }
}

public class ScreenshotView
{
    public string ViewId { get; set; }
    public int ViewCount { get; set; }
}

public class ScreenshotTitleInfo
{
    public string TitleId { get; set; }
    public string Name { get; set; }
}

public class ScreenshotOwner
{
    public string Xuid { get; set; }
}

public class GameClipsResponse
{
    public List<GameClip> GameClips { get; set; }
    public PagingInfo PagingInfo { get; set; }
}

public class GameClip
{
    public string GameClipId { get; set; }
    public string State { get; set; }
    public DateTime DatePublished { get; set; }
    public DateTime DateRecorded { get; set; }
    public DateTime LastModified { get; set; }
    public List<GameClipUri> GameClipUris { get; set; }
    public string ScId { get; set; }
    public string TitleId { get; set; }
    public int Rating { get; set; }
    public int RatingCount { get; set; }
    public List<GameClipView> Views { get; set; }
    public string TitleData { get; set; }
    public bool SystemProperties { get; set; }
    public bool Shared { get; set; }
    public List<string> CommentIds { get; set; }
    public int CommentCount { get; set; }
    public List<string> LikeIds { get; set; }
    public int LikeCount { get; set; }
    public string ContentImageUri { get; set; }
    public string ThumbnailImageUri { get; set; }
    public string AuthorType { get; set; }
    public GameClipTitleInfo TitleInfo { get; set; }
    public GameClipOwner Owner { get; set; }
    public string DurationInSeconds { get; set; }
    public string ClipName { get; set; }
}

public class GameClipUri
{
    public string Uri { get; set; }
    public string FileSize { get; set; }
    public string UriType { get; set; }
    public DateTime Expiration { get; set; }
}

public class GameClipView
{
    public string ViewId { get; set; }
    public int ViewCount { get; set; }
}

public class GameClipTitleInfo
{
    public string TitleId { get; set; }
    public string Name { get; set; }
}

public class GameClipOwner
{
    public string Xuid { get; set; }
}

public class DeleteGameClipResponse : BaseResponse
{
}

public class SetPrivacyRequest
{
    public PrivacySetting Value { get; set; }
}

public enum PrivacySetting
{
    Everyone,
    PeopleOnMyList,
    Blocked
}

public class SetPrivacyResponse : BaseResponse
{
}

// Friends models
public class FriendsResponse
{
    public List<Friend> People { get; set; }
}

public class Friend
{
    public string Xuid { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsFollowingCaller { get; set; }
    public bool IsFollowedByCaller { get; set; }
    public bool IsIdentityShared { get; set; }
    public DateTime AddedDateTimeUtc { get; set; }
    public string DisplayName { get; set; }
    public string RealName { get; set; }
    public string DisplayPicRaw { get; set; }
    public bool ShowUserAsAvatar { get; set; }
    public string Gamertag { get; set; }
    public string Gamerscore { get; set; }
    public string XboxOneRep { get; set; }
    public string PresenceState { get; set; }
    public string PresenceText { get; set; }
    public List<PresenceDevice> PresenceDevices { get; set; }
    public bool IsBroadcasting { get; set; }
    public bool IsCloaked { get; set; }
    public string PreferredColor { get; set; }
}

public class SearchFriendsResponse
{
    public List<Friend> Results { get; set; }
}

public class AddFriendsRequest
{
    public string Xuids { get; set; }
}

public class AddFriendsResponse : BaseResponse
{
}

public class RemoveFriendRequest
{
    public string Xuid { get; set; }
}

public class RemoveFriendResponse : BaseResponse
{
}

public class RecentPlayersResponse
{
    public List<RecentPlayer> RecentPlayers { get; set; }
}

public class RecentPlayer
{
    public string Xuid { get; set; }
    public List<RecentPlayerEncounter> Encounters { get; set; }
}

public class RecentPlayerEncounter
{
    public string EncounterId { get; set; }
    public DateTime EncounterDate { get; set; }
    public List<RecentPlayerTitle> Titles { get; set; }
}

public class RecentPlayerTitle
{
    public string TitleId { get; set; }
    public string TitleName { get; set; }
}

public class FavoritesRequest
{
    public List<string> Xuids { get; set; }
}

public class FavoritesResponse : BaseResponse
{
}

// Game Pass models
public class GamePassResponse
{
    public List<GamePassGame> Games { get; set; }
}

public class GamePassGame
{
    public string TitleId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ProductId { get; set; }
    public string SandboxId { get; set; }
    public DateTime ReleaseDate { get; set; }
    public List<string> Genres { get; set; }
    public List<string> Platforms { get; set; }
    public List<GamePassImage> Images { get; set; }
    public string DeveloperName { get; set; }
    public string PublisherName { get; set; }
    public string Category { get; set; }
    public bool IsBundle { get; set; }
}

public class GamePassImage
{
    public string ImageType { get; set; }
    public string Uri { get; set; }
}

// Group conversation models
public class GroupConversationsResponse
{
    public List<GroupConversation> Conversations { get; set; }
}

public class GroupConversation
{
    public string ConversationId { get; set; }
    public List<GroupParticipant> Participants { get; set; }
    public GroupMessage LastMessage { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class GroupParticipant
{
    public string Xuid { get; set; }
    public string Gamertag { get; set; }
    public string DisplayPictureRaw { get; set; }
}

public class GroupMessage
{
    public string Id { get; set; }
    public string Text { get; set; }
    public string Type { get; set; }
    public DateTime Sent { get; set; }
    public string Sender { get; set; }
}

public class CreateGroupRequest
{
    public List<long> Participants { get; set; }
}

public class CreateGroupResponse : BaseResponse
{
    public string GroupId { get; set; }
}

public class SendGroupMessageRequest
{
    public string Message { get; set; }
    public string GroupId { get; set; }
}

public class SendGroupMessageResponse : BaseResponse
{
    public string MessageId { get; set; }
}

public class GroupSummaryResponse
{
    public GroupConversation Group { get; set; }
}

public class GroupMessagesResponse
{
    public List<GroupMessage> Messages { get; set; }
    public PagingInfo PagingInfo { get; set; }
}

public class InviteVoiceChatRequest
{
    public string GroupId { get; set; }
}

public class InviteVoiceChatResponse : BaseResponse
{
}

public class InviteGroupRequest
{
    public string GroupId { get; set; }
    public List<long> Participants { get; set; }
}

public class InviteGroupResponse : BaseResponse
{
}

public class KickFromGroupRequest
{
    public string GroupId { get; set; }
    public string Xuid { get; set; }
}

public class KickFromGroupResponse : BaseResponse
{
}

public class LeaveGroupRequest
{
    public string GroupId { get; set; }
}

public class LeaveGroupResponse : BaseResponse
{
}

// Marketplace models
public class MarketplaceResponse
{
    public List<MarketplaceProduct> Products { get; set; }
}

public class MarketplaceProduct
{
    public string ProductId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string PublisherName { get; set; }
    public string DeveloperName { get; set; }
    public string Category { get; set; }
    public List<string> Platforms { get; set; }
    public List<MarketplaceImage> Images { get; set; }
    public MarketplacePrice Price { get; set; }
    public DateTime ReleaseDate { get; set; }
    public MarketplaceRating Rating { get; set; }
}

public class MarketplaceImage
{
    public string ImageType { get; set; }
    public string Uri { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class MarketplacePrice
{
    public string ListPrice { get; set; }
    public string CurrencyCode { get; set; }
    public bool IsFree { get; set; }
}

public class MarketplaceRating
{
    public string RatingId { get; set; }
    public string RatingSystemId { get; set; }
    public List<MarketplaceRatingDescriptor> RatingDescriptors { get; set; }
}

public class MarketplaceRatingDescriptor
{
    public string RatingDescriptor { get; set; }
    public string RatingDisclaimers { get; set; }
}

public class GameDetailsRequest
{
    public string Products { get; set; }
}

public class GameDetailsResponse
{
    public List<GameDetail> Products { get; set; }
}

public class GameDetail
{
    public string ProductId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ProductType { get; set; }
    public string PublisherName { get; set; }
    public string DeveloperName { get; set; }
    public string Category { get; set; }
    public List<string> Platforms { get; set; }
    public List<MarketplaceImage> Images { get; set; }
    public MarketplacePrice Price { get; set; }
    public DateTime ReleaseDate { get; set; }
    public MarketplaceRating Rating { get; set; }
    public List<GameDetailSku> Skus { get; set; }
}

public class GameDetailSku
{
    public string SkuId { get; set; }
    public string Name { get; set; }
    public MarketplacePrice Price { get; set; }
}

// Player models
public class PlayerSummaryResponse
{
    public List<PlayerSummary> People { get; set; }
}

public class PlayerSummary
{
    public string Xuid { get; set; }
    public string Gamertag { get; set; }
    public string DisplayName { get; set; }
    public string RealName { get; set; }
    public string DisplayPicRaw { get; set; }
    public string Gamerscore { get; set; }
    public string AccountTier { get; set; }
    public string XboxOneRep { get; set; }
    public string PreferredColor { get; set; }
    public string Bio { get; set; }
    public string Location { get; set; }
    public DateTime TenureLevel { get; set; }
    public int YearsOnXboxLive { get; set; }
    public List<PlayerTitleSummary> TitleSummaries { get; set; }
}

public class PlayerTitleSummary
{
    public string TitleId { get; set; }
    public string Name { get; set; }
    public int UnlockedAchievementCount { get; set; }
    public int TotalAchievements { get; set; }
    public int CurrentGamerscore { get; set; }
    public int MaxGamerscore { get; set; }
    public DateTime LastPlayed { get; set; }
}

public class PlayerStatsRequest
{
    public List<string> Xuids { get; set; }
    public List<StatGroup> Groups { get; set; }
    public List<StatDefinition> Stats { get; set; }
}

public class StatGroup
{
    public string Name { get; set; }
    public string TitleId { get; set; }
}

public class StatDefinition
{
    public string Name { get; set; }
    public string TitleId { get; set; }
}

public class PlayerStatsResponse
{
    public List<PlayerStatResult> Results { get; set; }
}

public class PlayerStatResult
{
    public string Xuid { get; set; }
    public List<StatValue> Groups { get; set; }
    public List<StatValue> Stats { get; set; }
}

public class StatValue
{
    public string Name { get; set; }
    public string TitleId { get; set; }
    public object Value { get; set; }
}

public class TitleHistoryStats
{
    public object MinutesPlayed { get; set; }
    public object SessionCount { get; set; }
}


// Session models
public class SessionResponse
{
    public List<Session> Sessions { get; set; }
}

public class Session
{
    public string SessionId { get; set; }
    public string SessionName { get; set; }
    public DateTime StartTime { get; set; }
    public List<SessionParticipant> Participants { get; set; }
    public SessionProperties Properties { get; set; }
}

public class SessionParticipant
{
    public string Xuid { get; set; }
    public string Gamertag { get; set; }
    public string Role { get; set; }
    public DateTime JoinTime { get; set; }
}

public class SessionProperties
{
    public string Visibility { get; set; }
    public string JoinRestriction { get; set; }
    public int MaxMembersCount { get; set; }
    public int MembersCount { get; set; }
}

public class SessionInviteRequest
{
    public string Xuid { get; set; }
    public string SessionName { get; set; }
}

public class SessionInviteResponse : BaseResponse
{
}

public class CreateSessionResponse : BaseResponse
{
    public string SessionId { get; set; }
    public string SessionName { get; set; }
}

public class SessionConfigResponse
{
    public SessionConfig Config { get; set; }
}

public class SessionConfig
{
    public List<SessionCapability> Capabilities { get; set; }
    public SessionLimits Limits { get; set; }
}

public class SessionCapability
{
    public string Name { get; set; }
    public bool Supported { get; set; }
}

public class SessionLimits
{
    public int MaxSessionCount { get; set; }
    public int MaxParticipantsPerSession { get; set; }
}
