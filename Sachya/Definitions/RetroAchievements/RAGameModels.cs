using System.Text.Json.Serialization;
using Sachya.Misc;

namespace Sachya.Definitions.RetroAchievements;

    // Game Models
    public record GameInfoBasic
    {
        public string? Title { get; init; }
        public int ID { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public string? ImageIcon { get; init; }
        public int NumAchievements { get; init; }
        public int NumLeaderboards { get; init; }
        public int Points { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime DateModified { get; init; }
        public int? ForumTopicID { get; init; } // Nullable?
        public List<string>? Hashes { get; init; } // Only present if h=1
    }

     public record GameInfo : GameIDTitle
    {
        // GameTitle is redundant if Title exists
        // public string? GameTitle { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        // Console seems redundant if ConsoleName exists
        // public string? Console { get; init; }
        public int? ForumTopicID { get; init; }
        public int? Flags { get; init; } // Usually 0 or null, meaning?
        [JsonPropertyName("GameIcon")] // Use GameIcon or ImageIcon? Doc shows both, sometimes redundant
        public string? ImageIcon { get; init; }
        // public string? ImageIcon { get; init; }
        public string? ImageTitle { get; init; }
        public string? ImageIngame { get; init; }
        public string? ImageBoxArt { get; init; }
        public string? Publisher { get; init; }
        public string? Developer { get; init; }
        public string? Genre { get; init; }
        public string? Released { get; init; } // Can be YYYY, YYYY-MM, YYYY-MM-DD HH:MM:SS. Parse carefully.
        public string? ReleasedAtGranularity {get; init; } // year, month, day
    }

    public record GameInfoExtended : GameInfo
    {
        // Inherits base GameInfo properties
        public bool IsFinal { get; init; } // Deprecated, always false
        public string? RichPresencePatch { get; init; }
        public string? GuideURL { get; init; }
        public DateTimeOffset Updated { get; init; } // More precise than DateModified
        public int? ParentGameID { get; init; }
        public int NumDistinctPlayers { get; init; } // Might be same as NumDistinctPlayersCasual?
        public int NumAchievements { get; init; }
        public Dictionary<string, AchievementCoreInfo> Achievements { get; init; } = new Dictionary<string, AchievementCoreInfo>(); // Key is Achievement ID as string
        public List<Claim> Claims { get; init; } = new List<Claim>(); // Usually empty in examples
        public int NumDistinctPlayersCasual { get; init; }
        public int NumDistinctPlayersHardcore { get; init; }

    }

    public record GameHashEntry
    {
        public string? MD5 { get; init; }
        public string? Name { get; init; }
        public List<string> Labels { get; init; } = new List<string>();
        public string? PatchUrl { get; init; }
    }

     public record GameHashesResponse
    {
        public List<GameHashEntry> Results { get; init; } = new List<GameHashEntry>();
    }

    public record GameRank
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public int? NumAchievements { get; init; } // Only in LatestMasters?
        public int TotalScore { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime LastAward { get; init; }
        public int? Rank { get; init; } // Only in high-scores non-master?
    }

     public record GameLeaderboardInfo
    {
        public int ID { get; init; }
        public bool RankAsc { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public string? Format { get; init; } // VALUE, SCORE, TIME, MILLISECS etc. Enum?
        public LeaderboardEntry? TopEntry { get; init; }
    }

    public record GameLeaderboardsResponse : PaginatedResponse<GameLeaderboardInfo> { }


    // Leaderboard Models
     public record LeaderboardEntry
    {
        public int? Rank { get; init; } // Null if requesting UserGameLeaderboards and user has no entry
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public long Score { get; init; } // Use long for SCORE/VALUE, maybe specific types based on Format?
        public string? FormattedScore { get; init; }
        public DateTimeOffset? DateSubmitted { get; init; } // Null if requesting UserGameLeaderboards and user has no entry
        public DateTimeOffset? DateUpdated { get; init; } // Only in UserGameLeaderboards?
    }

    public record LeaderboardEntriesResponse : PaginatedResponse<LeaderboardEntry> { }
