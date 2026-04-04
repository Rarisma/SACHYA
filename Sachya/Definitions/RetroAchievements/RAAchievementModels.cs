using System.Text.Json.Serialization;
using Sachya.Misc;

namespace Sachya.Definitions.RetroAchievements;

    // Achievement Models
    public record AchievementCountResponse
    {
        public int GameID { get; init; }
        public List<int> AchievementIDs { get; init; } = new List<int>();
    }

    public record AchievementCoreInfo
    {
        public int ID { get; init; }
        public int? NumAwarded { get; init; }
        public int? NumAwardedHardcore { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public int Points { get; init; }
        public int TrueRatio { get; init; }
        public string? Type { get; init; }
        public string? Author { get; init; }
        public string? AuthorULID { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime DateCreated { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime DateModified { get; init; }
         public string? BadgeName { get; init; }
         public int DisplayOrder { get; init; }
         public string? MemAddr { get; init; }
    }

     public record AchievementInfo : AchievementCoreInfo
    {
        // Inherits all props from AchievementCoreInfo
    }


    public record AchievementOfTheWeekResponse
    {
        public AchievementInfo? Achievement { get; init; }
        public ConsoleIDName? Console { get; init; }
        public ForumTopicInfo? ForumTopic { get; init; }
        public GameIDTitle? Game { get; init; }
        public DateTimeOffset StartAt { get; init; } // Assuming Z means UTC
        public int TotalPlayers { get; init; }
        public List<AotwUnlock> Unlocks { get; init; } = new List<AotwUnlock>();
        public int UnlocksCount { get; init; }
        public int UnlocksHardcoreCount { get; init; }
    }

    public record AotwUnlock
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public int RAPoints { get; init; }
        public int RASoftcorePoints { get; init; }
        public DateTimeOffset DateAwarded { get; init; } // Assuming Z means UTC
        public int HardcoreMode { get; init; } // 0 or 1
        [JsonIgnore]
        public bool IsHardcore => HardcoreMode == 1;
    }

    public record ConsoleIDName {
        public int ID { get; init; }
        public string? Title { get; init; }
    }
     public record ForumTopicInfo {
        public int ID { get; init; }
    }
    public record GameIDTitle {
        public int ID { get; init; }
        public string? Title { get; init; }
    }

    public record AchievementUnlocksResponse
    {
        public AchievementInfo? Achievement { get; init; }
        public ConsoleIDName? Console { get; init; }
        public GameIDTitle? Game { get; init; }
        public int UnlocksCount { get; init; }
        public int UnlocksHardcoreCount { get; init; }
        public int TotalPlayers { get; init; }
        public List<AchievementUnlockEntry> Unlocks { get; init; } = new List<AchievementUnlockEntry>();
    }

    public record AchievementUnlockEntry
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public int RAPoints { get; init; }
        public int RASoftcorePoints { get; init; }
        public DateTimeOffset DateAwarded { get; init; } // Assuming Z means UTC
        public int HardcoreMode { get; init; } // 0 or 1
        [JsonIgnore]
        public bool IsHardcore => HardcoreMode == 1;
    }


    // Claim Models
    public record Claim
    {
        public int ID { get; init; }
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public int GameID { get; init; }
        public string? GameTitle { get; init; }
        public string? GameIcon { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public int ClaimType { get; init; } // Enum? 0=Primary, 1=Collaboration
        public int SetType { get; init; } // Enum? 0=New, 1=Revision, 2=Rescore
        public int Status { get; init; } // Enum? 0=Active, 1=Completed, 2=Dropped
        public int Extension { get; init; } // Count of extensions
        public int Special { get; init; } // 0 or 1, meaning?
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime Created { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime DoneTime { get; init; } // Expiry or completion time
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime Updated { get; init; }
        public int UserIsJrDev { get; init; } // 0 or 1
        [JsonIgnore]
        public bool IsUserJrDev => UserIsJrDev == 1;
        public int MinutesLeft { get; init; } // Can be negative if expired/done
    }

    // Comment Models
    public record Comment
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime Submitted { get; init; }
        public string? CommentText { get; init; }
    }


    // Console/System Models
    public record ConsoleInfo
    {
        public int ID { get; init; }
        public string? Name { get; init; }
        public string? IconURL { get; init; }
        public bool Active { get; init; }
        public bool IsGameSystem { get; init; }
    }
