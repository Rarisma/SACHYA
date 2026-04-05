using System.Text.Json.Serialization;

namespace Sachya.Definitions.Xbox;

public class XboxProfileResponse
{
    [JsonPropertyName("profileUsers")]
    public List<XboxProfileUser> ProfileUsers { get; set; } = new();
}

public class XboxProfileUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("settings")]
    public List<XboxProfileSetting> Settings { get; set; } = new();
}

public class XboxProfileSetting
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class XboxTitleHistoryResponse
{
    [JsonPropertyName("titles")]
    public List<XboxTitle> Titles { get; set; } = new();
}

public class XboxTitle
{
    [JsonPropertyName("titleId")]
    public string TitleId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayImage")]
    public string? DisplayImage { get; set; }

    [JsonPropertyName("modernTitleId")]
    public string? ModernTitleId { get; set; }

    [JsonPropertyName("achievement")]
    public XboxTitleAchievementInfo? Achievement { get; set; }

    [JsonPropertyName("titleHistory")]
    public XboxTitleHistoryInfo? TitleHistory { get; set; }

    [JsonPropertyName("stats")]
    public XboxTitleStats? Stats { get; set; }
}

public class XboxTitleAchievementInfo
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
}

public class XboxTitleHistoryInfo
{
    [JsonPropertyName("lastTimePlayed")]
    public DateTime? LastTimePlayed { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; }
}

public class XboxTitleStats
{
    [JsonPropertyName("sourceVersion")]
    public int SourceVersion { get; set; }
}

public class XboxAchievementsResponse
{
    [JsonPropertyName("achievements")]
    public List<XboxAchievement> Achievements { get; set; } = new();

    [JsonPropertyName("pagingInfo")]
    public XboxPagingInfo? PagingInfo { get; set; }
}

public class XboxAchievement
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("serviceConfigId")]
    public string? ServiceConfigId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("lockedDescription")]
    public string? LockedDescription { get; set; }

    [JsonPropertyName("unlockedDescription")]
    public string? UnlockedDescription { get; set; }

    [JsonPropertyName("imageUnlocked")]
    public XboxMediaAsset? ImageUnlocked { get; set; }

    [JsonPropertyName("imageLocked")]
    public XboxMediaAsset? ImageLocked { get; set; }

    [JsonPropertyName("mediaAssets")]
    public List<XboxMediaAsset>? MediaAssets { get; set; }

    [JsonPropertyName("isSecret")]
    public bool IsSecret { get; set; }

    [JsonPropertyName("rewards")]
    public List<XboxReward>? Rewards { get; set; }

    [JsonPropertyName("rarity")]
    public XboxRarity? Rarity { get; set; }

    [JsonPropertyName("progressState")]
    public string? ProgressState { get; set; }

    [JsonPropertyName("progression")]
    public XboxProgression? Progression { get; set; }
}

public class XboxMediaAsset
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}

public class XboxReward
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class XboxRarity
{
    [JsonPropertyName("currentCategory")]
    public string? CurrentCategory { get; set; }

    [JsonPropertyName("currentPercentage")]
    public double CurrentPercentage { get; set; }
}

public class XboxProgression
{
    [JsonPropertyName("requirements")]
    public List<XboxRequirement>? Requirements { get; set; }

    [JsonPropertyName("timeUnlocked")]
    public DateTime? TimeUnlocked { get; set; }
}

public class XboxRequirement
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("current")]
    public string? Current { get; set; }

    [JsonPropertyName("target")]
    public string? Target { get; set; }
}

public class XboxPagingInfo
{
    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }

    [JsonPropertyName("totalRecords")]
    public int TotalRecords { get; set; }
}

/// <summary>
/// Xbox 360 v1 API achievement format — different field names and types from v2
/// </summary>
public class Xbox360AchievementsResponse
{
    [JsonPropertyName("achievements")]
    public List<Xbox360Achievement> Achievements { get; set; } = new();
}

public class Xbox360Achievement
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("titleId")]
    public int TitleId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("sequence")]
    public int Sequence { get; set; }

    [JsonPropertyName("flags")]
    public int Flags { get; set; }

    [JsonPropertyName("unlockedOnline")]
    public bool UnlockedOnline { get; set; }

    [JsonPropertyName("unlocked")]
    public bool Unlocked { get; set; }

    [JsonPropertyName("isSecret")]
    public bool IsSecret { get; set; }

    [JsonPropertyName("platform")]
    public int Platform { get; set; }

    [JsonPropertyName("gamerscore")]
    public int Gamerscore { get; set; }

    [JsonPropertyName("imageId")]
    public int ImageId { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("lockedDescription")]
    public string? LockedDescription { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("timeUnlocked")]
    public DateTime? TimeUnlocked { get; set; }
}

public class XboxUserStatsResponse
{
    [JsonPropertyName("groups")]
    public List<XboxStatGroup> Groups { get; set; } = new();
}

public class XboxStatGroup
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("titleId")]
    public string? TitleId { get; set; }

    [JsonPropertyName("statlistscollection")]
    public List<XboxStatCollection>? StatListsCollection { get; set; }
}

public class XboxStatCollection
{
    [JsonPropertyName("stats")]
    public List<XboxStat> Stats { get; set; } = new();
}

public class XboxStat
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
