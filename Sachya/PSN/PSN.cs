using System.Net;
using System.Text.Json.Serialization;

namespace Sachya.PSN
{
    public record EarnedTrophiesSummary
    {
        [JsonPropertyName("bronze")]
        public int Bronze { get; init; }
        [JsonPropertyName("silver")]
        public int Silver { get; init; }
        [JsonPropertyName("gold")]
        public int Gold { get; init; }
        [JsonPropertyName("platinum")]
        public int Platinum { get; init; }
    }

    public record DefinedTrophiesSummary
    {
        [JsonPropertyName("bronze")]
        public int Bronze { get; init; }
        [JsonPropertyName("silver")]
        public int Silver { get; init; }
        [JsonPropertyName("gold")]
        public int Gold { get; init; }
        [JsonPropertyName("platinum")]
        public int Platinum { get; init; }
    }

    public record RarestTrophy
    {
        [JsonPropertyName("trophyId")]
        public int TrophyId { get; init; }
        [JsonPropertyName("trophyHidden")]
        public bool TrophyHidden { get; init; }
        [JsonPropertyName("trophyType")]
        public string TrophyType { get; init; } = string.Empty;
        [JsonPropertyName("trophyName")]
        public string? TrophyName { get; init; } // Only in Title Summary
        [JsonPropertyName("trophyDetail")]
        public string? TrophyDetail { get; init; } // Only in Title Summary
        [JsonPropertyName("trophyIconUrl")]
        public string? TrophyIconUrl { get; init; } // Only in Title Summary
        [JsonPropertyName("trophyRare")]
        public int TrophyRare { get; init; }
        [JsonPropertyName("trophyEarnedRate")]
        public string TrophyEarnedRate { get; init; } = string.Empty; // Represented as string in JSON
        [JsonPropertyName("earned")]
        public bool Earned { get; init; }
        [JsonPropertyName("earnedDateTime")]
        public DateTimeOffset EarnedDateTime { get; init; }
    }
    public record TrophyTitle
    {
        [JsonPropertyName("npServiceName")]
        public string NpServiceName { get; init; } = string.Empty;
        [JsonPropertyName("npCommunicationId")]
        public string NpCommunicationId { get; init; } = string.Empty;
        [JsonPropertyName("trophySetVersion")]
        public string TrophySetVersion { get; init; } = string.Empty;
        [JsonPropertyName("trophyTitleName")]
        public string TrophyTitleName { get; init; } = string.Empty;
        [JsonPropertyName("trophyTitleDetail")]
        public string? TrophyTitleDetail { get; init; } // PS3/4/Vita only
        [JsonPropertyName("trophyTitleIconUrl")]
        public string TrophyTitleIconUrl { get; init; } = string.Empty;
        [JsonPropertyName("trophyTitlePlatform")]
        public string TrophyTitlePlatform { get; init; } = string.Empty;
        [JsonPropertyName("hasTrophyGroups")]
        public bool HasTrophyGroups { get; init; }
        [JsonPropertyName("definedTrophies")]
        public DefinedTrophiesSummary DefinedTrophies { get; init; } = null!;
        [JsonPropertyName("progress")]
        public int Progress { get; init; }
        [JsonPropertyName("earnedTrophies")]
        public EarnedTrophiesSummary EarnedTrophies { get; init; } = null!;
        [JsonPropertyName("hiddenFlag")]
        public bool HiddenFlag { get; init; }
        [JsonPropertyName("lastUpdatedDateTime")]
        public DateTimeOffset LastUpdatedDateTime { get; init; }
    }

    public record UserTrophyTitlesResponse
    {
        [JsonPropertyName("trophyTitles")]
        public List<TrophyTitle> TrophyTitles { get; init; } = new();
        [JsonPropertyName("totalItemCount")]
        public int TotalItemCount { get; init; }
        [JsonPropertyName("nextOffset")]
        public int? NextOffset { get; init; }
        [JsonPropertyName("previousOffset")]
        public int? PreviousOffset { get; init; }
    }

    public record Trophy
    {
        [JsonPropertyName("trophyId")]
        public int TrophyId { get; init; }
        [JsonPropertyName("trophyHidden")]
        public bool TrophyHidden { get; init; }
        [JsonPropertyName("trophyType")]
        public string TrophyType { get; init; } = string.Empty;
        [JsonPropertyName("trophyName")]
        public string TrophyName { get; init; } = string.Empty;
        [JsonPropertyName("trophyDetail")]
        public string TrophyDetail { get; init; } = string.Empty;
        [JsonPropertyName("trophyIconUrl")]
        public string TrophyIconUrl { get; init; } = string.Empty;
        [JsonPropertyName("trophyGroupId")]
        public string TrophyGroupId { get; init; } = string.Empty;
        [JsonPropertyName("trophyProgressTargetValue")]
        public string? TrophyProgressTargetValue { get; init; } // PS5 Only, String in JSON
        [JsonPropertyName("trophyRewardName")]
        public string? TrophyRewardName { get; init; } // PS5 Only
        [JsonPropertyName("trophyRewardImageUrl")]
        public string? TrophyRewardImageUrl { get; init; } // PS5 Only
    }

    public record TitleTrophiesResponse
    {
        [JsonPropertyName("trophySetVersion")]
        public string TrophySetVersion { get; init; } = string.Empty;
        [JsonPropertyName("hasTrophyGroups")]
        public bool HasTrophyGroups { get; init; }
        [JsonPropertyName("trophies")]
        public List<Trophy> Trophies { get; init; } = new();
        [JsonPropertyName("totalItemCount")]
        public int TotalItemCount { get; init; }
         [JsonPropertyName("nextOffset")]
        public int? NextOffset { get; init; }
        [JsonPropertyName("previousOffset")]
        public int? PreviousOffset { get; init; }
    }

    public record EarnedTrophy
    {
        [JsonPropertyName("trophyId")]
        public int TrophyId { get; init; }
        [JsonPropertyName("trophyHidden")]
        public bool TrophyHidden { get; init; }
        [JsonPropertyName("earned")]
        public bool Earned { get; init; }
        [JsonPropertyName("progress")]
        public string? Progress { get; init; } // PS5 Only, String in JSON
        [JsonPropertyName("progressRate")]
        public int? ProgressRate { get; init; } // PS5 Only
        [JsonPropertyName("progressedDateTime")]
        public DateTimeOffset? ProgressedDateTime { get; init; } // PS5 Only
        [JsonPropertyName("earnedDateTime")]
        public DateTime? EarnedDateTime { get; init; } // If earned is true
        [JsonPropertyName("trophyType")]
        public string TrophyType { get; init; } = string.Empty;
        [JsonPropertyName("trophyRare")]
        public int TrophyRare { get; init; }
        [JsonPropertyName("trophyEarnedRate")]
        public string TrophyEarnedRate { get; init; } = string.Empty; // String in JSON
    }

    public record UserEarnedTrophiesResponse
    {
        [JsonPropertyName("trophySetVersion")]
        public string TrophySetVersion { get; init; } = string.Empty;
        [JsonPropertyName("hasTrophyGroups")]
        public bool HasTrophyGroups { get; init; }
        [JsonPropertyName("lastUpdatedDateTime")]
        public DateTimeOffset LastUpdatedDateTime { get; init; }
        [JsonPropertyName("trophies")]
        public List<EarnedTrophy> Trophies { get; init; } = new();
        [JsonPropertyName("rarestTrophies")]
        public List<RarestTrophy> RarestTrophies { get; init; } = new();
        [JsonPropertyName("totalItemCount")]
        public int TotalItemCount { get; init; }
         [JsonPropertyName("nextOffset")]
        public int? NextOffset { get; init; }
        [JsonPropertyName("previousOffset")]
        public int? PreviousOffset { get; init; }
    }

    public record TrophySummaryResponse
    {
        [JsonPropertyName("accountId")]
        public string AccountId { get; init; } = string.Empty;
        [JsonPropertyName("trophyLevel")]
        public int TrophyLevel { get; init; }
        [JsonPropertyName("trophyPoint")]
        public int TrophyPoint { get; init; }
        [JsonPropertyName("trophyLevelBasePoint")]
        public int TrophyLevelBasePoint { get; init; }
        [JsonPropertyName("trophyLevelNextPoint")]
        public int TrophyLevelNextPoint { get; init; }
        [JsonPropertyName("progress")]
        public int Progress { get; init; }
        [JsonPropertyName("tier")]
        public int Tier { get; init; }
        [JsonPropertyName("earnedTrophies")]
        public EarnedTrophiesSummary EarnedTrophies { get; init; } = null!;
    }

    public record TrophyGroupDefinition
    {
        [JsonPropertyName("trophyGroupId")]
        public string TrophyGroupId { get; init; } = string.Empty;
        [JsonPropertyName("trophyGroupName")]
        public string TrophyGroupName { get; init; } = string.Empty;
        [JsonPropertyName("trophyGroupDetail")]
        public string? TrophyGroupDetail { get; init; } // PS3/4/Vita only
        [JsonPropertyName("trophyGroupIconUrl")]
        public string TrophyGroupIconUrl { get; init; } = string.Empty;
        [JsonPropertyName("definedTrophies")]
        public DefinedTrophiesSummary DefinedTrophies { get; init; } = null!;
    }

    public record TitleTrophyGroupsResponse
    {
        [JsonPropertyName("trophySetVersion")]
        public string TrophySetVersion { get; init; } = string.Empty;
        [JsonPropertyName("trophyGroups")]
        public List<TrophyGroup> TrophyGroups { get; init; } = new();
        [JsonPropertyName("totalItemCount")]
        public int TotalItemCount { get; init; }
    }

     public record EarnedTrophyGroup
    {
        [JsonPropertyName("trophyGroupId")]
        public string TrophyGroupId { get; init; } = string.Empty;
        [JsonPropertyName("progress")]
        public int Progress { get; init; }
        [JsonPropertyName("earnedTrophies")]
        public EarnedTrophiesSummary EarnedTrophies { get; init; } = null!;
        [JsonPropertyName("lastUpdatedDateTime")]
        public DateTimeOffset LastUpdatedDateTime { get; init; }
    }

    public record UserEarnedTrophyGroupsResponse
    {
        [JsonPropertyName("trophySetVersion")]
        public string TrophySetVersion { get; init; } = string.Empty;
        [JsonPropertyName("lastUpdatedDateTime")]
        public DateTimeOffset LastUpdatedDateTime { get; init; }
        [JsonPropertyName("trophyGroups")]
        public List<EarnedTrophyGroup> TrophyGroups { get; init; } = new();
        [JsonPropertyName("totalItemCount")]
        public int TotalItemCount { get; init; }
    }

    public record TitleTrophyTitleSummary
    {
        [JsonPropertyName("npServiceName")]
        public string NpServiceName { get; init; } = string.Empty;
        [JsonPropertyName("npCommunicationId")]
        public string NpCommunicationId { get; init; } = string.Empty;
        [JsonPropertyName("trophyTitleName")]
        public string TrophyTitleName { get; init; } = string.Empty;
        [JsonPropertyName("trophyTitleDetail")]
        public string? TrophyTitleDetail { get; init; } // PS3/4/Vita only
        [JsonPropertyName("trophyTitleIconUrl")]
        public string TrophyTitleIconUrl { get; init; } = string.Empty;
        [JsonPropertyName("hasTrophyGroups")]
        public bool HasTrophyGroups { get; init; }
        [JsonPropertyName("rarestTrophies")]
        public List<RarestTrophy> RarestTrophies { get; init; } = new();
        [JsonPropertyName("progress")]
        public int Progress { get; init; }
        [JsonPropertyName("earnedTrophies")]
        public EarnedTrophiesSummary EarnedTrophies { get; init; } = null!;
        [JsonPropertyName("definedTrophies")]
        public DefinedTrophiesSummary DefinedTrophies { get; init; } = null!;
        [JsonPropertyName("notEarnedTrophyIds")]
        public List<int>? NotEarnedTrophyIds { get; init; } // Only if requested
        [JsonPropertyName("lastUpdatedDateTime")]
        public DateTimeOffset LastUpdatedDateTime { get; init; }
    }

     public record TitleSummary
    {
        [JsonPropertyName("npTitleId")]
        public string NpTitleId { get; init; } = string.Empty;
        [JsonPropertyName("trophyTitles")]
        public List<TitleTrophyTitleSummary> TrophyTitles { get; init; } = new();
    }

    public record UserTitlesTrophySummaryResponse
    {
        [JsonPropertyName("titles")]
        public List<TitleSummary> Titles { get; init; } = new();
    }

    // Request structure for metGetTips
    public record GameHelpRequestTrophy
    {
        [JsonPropertyName("trophyId")]
        public string TrophyId { get; init; } = string.Empty;
        [JsonPropertyName("udsObjectId")]
        public string UdsObjectId { get; init; } = string.Empty;
        [JsonPropertyName("helpType")]
        public string HelpType { get; init; } = string.Empty;
    }

    // --- Hint Availability Response ---
    public record TrophyInfoWithHintAvailable
    {
        [JsonPropertyName("__typename")]
        public string Typename { get; init; } = string.Empty;
        [JsonPropertyName("helpType")]
        public string HelpType { get; init; } = string.Empty;
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;
        [JsonPropertyName("trophyId")]
        public string TrophyId { get; init; } = string.Empty;
        [JsonPropertyName("udsObjectId")]
        public string UdsObjectId { get; init; } = string.Empty;
    }

    public record HintAvailability
    {
        [JsonPropertyName("__typename")]
        public string Typename { get; init; } = string.Empty;
        [JsonPropertyName("trophies")]
        public List<TrophyInfoWithHintAvailable> Trophies { get; init; } = new();
    }

    public record HintAvailabilityRetrieve
    {
        [JsonPropertyName("hintAvailabilityRetrieve")]
        public HintAvailability HintAvailability { get; init; } = null!;
    }

    public record GameHelpAvailabilityResponse
    {
        [JsonPropertyName("data")]
        public HintAvailabilityRetrieve Data { get; init; } = null!;
    }


    // --- Tips Response ---
    public record TipContent
    {
        [JsonPropertyName("__typename")]
        public string Typename { get; init; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;
        [JsonPropertyName("displayName")]
        public string DisplayName { get; init; } = string.Empty;
        [JsonPropertyName("mediaId")]
        public string? MediaId { get; init; }
        [JsonPropertyName("mediaType")]
        public string? MediaType { get; init; } // e.g., VIDEO
        [JsonPropertyName("mediaUrl")]
        public string? MediaUrl { get; init; }
        [JsonPropertyName("tipId")]
        public string TipId { get; init; } = string.Empty;
    }

    public record TipGroup
    {
        [JsonPropertyName("__typename")]
        public string Typename { get; init; } = string.Empty;
        [JsonPropertyName("groupId")]
        public string? GroupId { get; init; } // Often null
        [JsonPropertyName("groupName")]
        public string? GroupName { get; init; } // Often null
        [JsonPropertyName("tipContents")]
        public List<TipContent> TipContents { get; init; } = new();
    }

    public record TrophyTip
    {
        [JsonPropertyName("__typename")]
        public string Typename { get; init; } = string.Empty;
        [JsonPropertyName("groups")]
        public List<TipGroup> Groups { get; init; } = new();
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;
        [JsonPropertyName("totalGroupCount")]
        public int TotalGroupCount { get; init; }
        [JsonPropertyName("trophyId")]
        public string TrophyId { get; init; } = string.Empty;
    }

    public record Tips
    {
        [JsonPropertyName("__typename")]
        public string Typename { get; init; } = string.Empty;
        [JsonPropertyName("hasAccess")]
        public bool HasAccess { get; init; } // Requires PS+ according to docs (though maybe changed)
        [JsonPropertyName("trophies")]
        public List<TrophyTip> Trophies { get; init; } = new();
    }

    public record TipsRetrieve
    {
        [JsonPropertyName("tipsRetrieve")]
        public Tips TipsRetrieved { get; init; } = null!;
    }

     public record GameHelpTipsResponse
    {
        [JsonPropertyName("data")]
        public TipsRetrieve Data { get; init; } = null!;
    }
    
    public class PlaystationApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string? ResponseContent { get; }

        public PlaystationApiException(string message, HttpStatusCode statusCode, string? responseContent = null)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }

        public PlaystationApiException(string message, HttpStatusCode statusCode, Exception innerException, string? responseContent = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }
    }

    public record TrophyGroup
    {
        [JsonPropertyName("trophyGroupId")]
        public string TrophyGroupId { get; init; } = string.Empty;
        [JsonPropertyName("trophyGroupName")]
        public string TrophyGroupName { get; init; } = string.Empty;
        [JsonPropertyName("trophyGroupDetail")]
        public string? TrophyGroupDetail { get; init; }
        [JsonPropertyName("trophyGroupIconUrl")]
        public string TrophyGroupIconUrl { get; init; } = string.Empty;
        [JsonPropertyName("definedTrophies")]
        public DefinedTrophiesSummary DefinedTrophies { get; init; } = null!;
    }

    public record TrophiesWithGameHelpResponse
    {
        [JsonPropertyName("trophySetVersion")]
        public string TrophySetVersion { get; init; } = string.Empty;
        [JsonPropertyName("hasTrophyGroups")]
        public bool HasTrophyGroups { get; init; }
        [JsonPropertyName("trophies")]
        public List<TrophyWithGameHelp> Trophies { get; init; } = new();
        [JsonPropertyName("totalItemCount")]
        public int TotalItemCount { get; init; }
        [JsonPropertyName("nextOffset")]
        public int? NextOffset { get; init; }
        [JsonPropertyName("previousOffset")]
        public int? PreviousOffset { get; init; }
    }

    public record GameHelpForTrophiesResponse
    {
        [JsonPropertyName("gameHelp")]
        public List<GameHelp> GameHelp { get; init; } = new();
        [JsonPropertyName("totalItemCount")]
        public int TotalItemCount { get; init; }
    }

    public record TrophyWithGameHelp
    {
        [JsonPropertyName("trophyId")]
        public int TrophyId { get; init; }
        [JsonPropertyName("trophyHidden")]
        public bool TrophyHidden { get; init; }
        [JsonPropertyName("trophyType")]
        public string TrophyType { get; init; } = string.Empty;
        [JsonPropertyName("trophyName")]
        public string TrophyName { get; init; } = string.Empty;
        [JsonPropertyName("trophyDetail")]
        public string TrophyDetail { get; init; } = string.Empty;
        [JsonPropertyName("trophyIconUrl")]
        public string TrophyIconUrl { get; init; } = string.Empty;
        [JsonPropertyName("trophyGroupId")]
        public string TrophyGroupId { get; init; } = string.Empty;
        [JsonPropertyName("gameHelp")]
        public GameHelp? GameHelp { get; init; }
    }

    public record GameHelp
    {
        [JsonPropertyName("trophyId")]
        public int TrophyId { get; init; }
        [JsonPropertyName("gameHelpImages")]
        public List<GameHelpImage> GameHelpImages { get; init; } = new();
        [JsonPropertyName("gameHelpVideos")]
        public List<GameHelpVideo> GameHelpVideos { get; init; } = new();
    }

    public record GameHelpImage
    {
        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; init; } = string.Empty;
        [JsonPropertyName("imageCaption")]
        public string? ImageCaption { get; init; }
    }

    public record GameHelpVideo
    {
        [JsonPropertyName("videoUrl")]
        public string VideoUrl { get; init; } = string.Empty;
        [JsonPropertyName("videoCaption")]
        public string? VideoCaption { get; init; }
    }
}
