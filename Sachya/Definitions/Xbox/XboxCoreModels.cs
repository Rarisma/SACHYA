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
    public double? RarityPercentage { get; set; }
    public string? ProgressState { get; set; }
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
    public string? ProgressState { get; set; }
}

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
