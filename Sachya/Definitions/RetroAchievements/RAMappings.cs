using System.Text.Json.Serialization;
using Sachya.Misc;

namespace Sachya.Definitions.RetroAchievements;

   // --- Enums ---
    public enum ClaimKind
    {
        Completed = 1,
        Dropped = 2,
        Expired = 3
    }

    public enum GameRankType
    {
        HighScores = 0, // Top points earners (non-master?) or first masterers? Doc unclear. Check behavior. Based on Node lib, seems like high scores non-master.
        LatestMasters = 1
    }
     public enum GameAwardKind
    {
        [JsonPropertyName("beaten-softcore")] Beaten_Softcore,
        [JsonPropertyName("beaten-hardcore")] Beaten_Hardcore,
        [JsonPropertyName("completed")] Completed,
        [JsonPropertyName("mastered")] Mastered
    }

    // --- Response Models ---
    // Note: Using [JsonPropertyName] to map JSON fields to C# property names.

    public record PaginatedResponse<T>
    {
        public int Count { get; init; }
        public int Total { get; init; }
        public List<T> Results { get; init; } = new List<T>();
    }


    // Connect API Models
    public record ConnectLoginResponse
    {
        public bool Success { get; init; }
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? Token { get; init; }
        public int Score { get; init; }
        public int SoftcoreScore { get; init; }
        public int Messages { get; init; }
        public int Permissions { get; init; } // Consider enum if values are known
        public string? AccountType { get; init; } // Consider enum
    }

    public record HardcoreUnlockInfo
    {
        public int ID { get; init; }
        public long When { get; init; } // Unix timestamp
        [JsonIgnore]
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTimeOffset WhenDateTime => DateTimeOffset.FromUnixTimeSeconds(When);
    }

    public record StartSessionResponse
    {
        public bool Success { get; init; }
        public List<HardcoreUnlockInfo> HardcoreUnlocks { get; init; } = new List<HardcoreUnlockInfo>();
        public long ServerNow { get; init; } // Unix timestamp
        [JsonIgnore]
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTimeOffset ServerNowDateTime => DateTimeOffset.FromUnixTimeSeconds(ServerNow);
    }

    public record PingResponse
    {
        public bool Success { get; init; }
    }

    public record AwardAchievementResponse
    {
        public bool Success { get; init; }
        public int AchievementsRemaining { get; init; }
        public int Score { get; init; }
        public int SoftcoreScore { get; init; }
        public int AchievementID { get; init; }
    }
     public record AwardAchievementsResponse
    {
        public bool Success { get; init; }
        public int Score { get; init; }
        public int SoftcoreScore { get; init; }
        public List<int> ExistingIDs { get; init; } = new List<int>();
        public List<int> SuccessfulIDs { get; init; } = new List<int>();
    }

    // Ticket Models
    public record AchievementTicketStats
    {
        public int AchievementID { get; init; }
        public string? AchievementTitle { get; init; }
        public string? AchievementDescription { get; init; }
        public string? AchievementType { get; init; }
        public string? URL { get; init; }
        public int OpenTickets { get; init; }
    }

    public record DeveloperTicketStats
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public int Open { get; init; }
        public int Closed { get; init; }
        public int Resolved { get; init; }
        public int Total { get; init; }
        public string? URL { get; init; }
    }

    public record GameTicketStats // Define 'Tickets' array type if 'd=1' is used and structure is known
    {
        public int GameID { get; init; }
        public string? GameTitle { get; init; }
        public string? ConsoleName { get; init; }
        public int OpenTickets { get; init; }
        public string? URL { get; init; }
        // public List<TicketData>? Tickets { get; init; } // If d=1 is used
    }

     public record TicketData
    {
        public int ID { get; init; }
        public int AchievementID { get; init; }
        public string? AchievementTitle { get; init; }
        [JsonPropertyName("AchievementDesc")]
        public string? AchievementDescription { get; init; }
        public string? AchievementType { get; init; } // Often null in examples
        public int Points { get; init; }
        public string? BadgeName { get; init; }
        public string? AchievementAuthor { get; init; }
        public string? AchievementAuthorULID { get; init; }
        public int GameID { get; init; }
        public string? ConsoleName { get; init; }
        public string? GameTitle { get; init; }
        public string? GameIcon { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime ReportedAt { get; init; }
        public int ReportType { get; init; }
        public bool? Hardcore { get; init; } // Nullable bool (or int if 0/1)
        public string? ReportNotes { get; init; }
        public string? ReportedBy { get; init; }
        public string? ReportedByULID { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime? ResolvedAt { get; init; }
        public string? ResolvedBy { get; init; }
        public string? ResolvedByULID { get; init; }
        public int ReportState { get; init; }
        public string? ReportStateDescription { get; init; }
        public string? ReportTypeDescription { get; init; }
        public string? URL { get; init; }
    }

    public record MostRecentTicketsResponse
    {
        public List<TicketData> RecentTickets { get; init; } = new List<TicketData>();
        public int OpenTickets { get; init; }
        public string? URL { get; init; }
    }

    public record MostTicketedGameInfo
    {
        public int GameID { get; init; }
        public string? GameTitle { get; init; }
        public string? GameIcon { get; init; }
        public string? Console { get; init; } // Different name than ConsoleName
        public int OpenTickets { get; init; }
    }

    public record MostTicketedGamesResponse
    {
        public List<MostTicketedGameInfo> MostReportedGames { get; init; } = new List<MostTicketedGameInfo>();
        public string? URL { get; init; }
    }


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


    // User Models

    public record UserRecentAchievement
    {
        public DateTime Date { get; init; }
        public int HardcoreMode { get; init; } // 0 or 1
        [JsonIgnore]
        public bool IsHardcore => HardcoreMode == 1;
        public int AchievementID { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public string? BadgeName { get; init; }
        public int Points { get; init; }
        public int TrueRatio { get; init; }
        public string? Type { get; init; }
        public string? Author { get; init; }
        public string? AuthorULID { get; init; }
        public string? GameTitle { get; init; }
        public string? GameIcon { get; init; }
        public int GameID { get; init; }
        public string? ConsoleName { get; init; }
        public int? CumulScore { get; init; } // Seems to be sum of points earned *in this batch*? Use with caution.
        public string? BadgeURL { get; init; }
        public string? GameURL { get; init; }
    }

    public record UserProfile
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public string? UserPic { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]

        public DateTime MemberSince { get; init; }
        public string? RichPresenceMsg { get; init; }
        public int? LastGameID { get; init; }
        public int ContribCount { get; init; } // Deprecated? Meaning?
        public int ContribYield { get; init; } // Deprecated? Meaning?
        public int TotalPoints { get; init; }
        public int TotalSoftcorePoints { get; init; }
        public int TotalTruePoints { get; init; }
        public int Permissions { get; init; } // 1=Normal, 2=JrDev, 3=Dev, 4=?, 5=Admin? Enum?
        public int Untracked { get; init; } // 0 or 1
        [JsonIgnore]
        public bool IsUntracked => Untracked == 1;
        public int ID { get; init; }
        public bool UserWallActive { get; init; }
        public string? Motto { get; init; }
    }

    public record TopTenUser
    {
        [JsonPropertyName("1")]
        public string? Username { get; init; }
        [JsonPropertyName("2")]
        public int TotalPoints { get; init; }
        [JsonPropertyName("3")]
        public int TotalRatioPoints { get; init; } // RetroPoints (white points)
        [JsonPropertyName("4")]
        public string? ULID { get; init; }
    }

     public record UserAward
    {
        public DateTimeOffset AwardedAt { get; init; }
        public string? AwardType { get; init; } // Enum? "Mastery/Completion", "Game Beaten", etc.
        public int AwardData { get; init; } // Game ID for Mastery/Beaten, Amount for Yield?
        public int AwardDataExtra { get; init; } // 1 for hardcore beaten?
        public int DisplayOrder { get; init; } // Or string?
        public string? Title { get; init; } // Game Title for game awards
        public int? ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public int? Flags { get; init; }
        public string? ImageIcon { get; init; } // Game icon for game awards
    }

    public record UserAwardsResponse
    {
        public int TotalAwardsCount { get; init; }
        public int HiddenAwardsCount { get; init; }
        public int MasteryAwardsCount { get; init; }
        public int CompletionAwardsCount { get; init; } // Usually 0?
        public int BeatenHardcoreAwardsCount { get; init; }
        public int BeatenSoftcoreAwardsCount { get; init; }
        public int EventAwardsCount { get; init; }
        public int SiteAwardsCount { get; init; }
        public List<UserAward> VisibleUserAwards { get; init; } = new List<UserAward>();
    }

    public record UserCompletedGame
    {
        public int GameID { get; init; }
        public string? Title { get; init; }
        public string? ImageIcon { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public int MaxPossible { get; init; }
        public int NumAwarded { get; init; }
        public string? PctWon { get; init; } // Changed from decimal to string
        [JsonIgnore]
        public decimal PctWonDecimal => decimal.TryParse(PctWon, out var result) ? result : 0m;
        public string? HardcoreMode { get; init; }
        [JsonIgnore]
        public bool IsHardcore => HardcoreMode == "1";
    }

    public record UserCompletionProgressGame
    {
        public int GameID { get; init; }
        public string? Title { get; init; }
        public string? ImageIcon { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public int MaxPossible { get; init; } // Achievement count
        public int NumAwarded { get; init; } // Softcore achievements awarded
        public int NumAwardedHardcore { get; init; }
        public DateTimeOffset? MostRecentAwardedDate { get; init; } // Seems to be last time *any* cheevo was earned?
        public string? HighestAwardKind { get; init; } // "mastered", "completed", "beaten-hardcore", "beaten-softcore" etc. Enum?
        public DateTimeOffset? HighestAwardDate { get; init; }
    }


    public record AchievementUserProgress : AchievementCoreInfo
    {
        // Inherits core achievement properties
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime? DateEarned { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime? DateEarnedHardcore { get; init; }
    }

    public record GameInfoExtendedUserProgress : GameInfoExtended
    {
        // Inherits GameInfoExtended properties
        // Note: Achievements dictionary now contains AchievementUserProgress
        public new Dictionary<string, AchievementUserProgress> Achievements { get; init; } = new Dictionary<string, AchievementUserProgress>();

        public int NumAwardedToUser { get; init; } // Softcore
        public int NumAwardedToUserHardcore { get; init; }
        public string? UserCompletion { get; init; } // e.g., "100.00%"
        public string? UserCompletionHardcore { get; init; } // e.g., "100.00%"
         public string? HighestAwardKind { get; init; } // "mastered", etc.
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime HighestAwardDate { get; init; }
    }

    public record UserGameRank
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public int UserRank { get; init; }
        public int TotalScore { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime LastAward { get; init; }
    }

     public record UserGameLeaderboardEntry
    {
        public int ID { get; init; }
        public bool RankAsc { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public string? Format { get; init; }
        public LeaderboardEntry? UserEntry { get; init; } // User's entry, Rank/DateSubmitted might be null if no entry
    }


    public record UserPoints
    {
        public int Points { get; init; } // Hardcore
        public int SoftcorePoints { get; init; }
    }

    public record UserGameProgress
    {
        public int NumPossibleAchievements { get; init; }
        public int PossibleScore { get; init; }
        public int NumAchieved { get; init; } // Softcore
        public int ScoreAchieved { get; init; } // Softcore
        public int NumAchievedHardcore { get; init; }
        public int ScoreAchievedHardcore { get; init; }
    }

    public record UserRecentlyPlayedGame : UserGameProgress
    {
         // Inherits UserGameProgress properties
        public int GameID { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public string? Title { get; init; }
        public string? ImageIcon { get; init; }
        public string? ImageTitle { get; init; } // Included in response, but maybe not needed here?
        public string? ImageIngame { get; init; }
        public string? ImageBoxArt { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime LastPlayed { get; init; }
        public int AchievementsTotal { get; init; } // Same as NumPossibleAchievements? Check consistency.

    }

    public record SetRequestGameInfo
    {
        public int GameID { get; init; }
        public string? Title { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public string? ImageIcon { get; init; }
    }

     public record UserSetRequestsResponse
    {
        public List<SetRequestGameInfo> RequestedSets { get; init; } = new List<SetRequestGameInfo>();
        public int TotalRequests { get; init; }
        public int PointsForNext { get; init; }
    }


    public record UserLastActivity
    {
        public int ID { get; init; }
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime? timestamp { get; init; } // Nullable? Format?
        [JsonConverter(typeof(RaDateTimeConverter))]
        public DateTime? lastupdate { get; init; } // Nullable? Format?
        public int? activitytype { get; init; } // Nullable? Enum?
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? data { get; init; } // Meaning?
        public string? data2 { get; init; } // Meaning?
    }

    public record UserSummaryAchievement : AchievementCoreInfo
    {
         // Inherits core achievement properties
         public int GameID { get; init; }
         public string? GameTitle { get; init; }
         [JsonPropertyName("IsAwarded")] // "1" or null/missing? Or "0"? Need to test. Let's assume string.
         public string? IsAwardedString { get; init; }
         [JsonIgnore]
         public bool IsAwarded => IsAwardedString == "1";
         public DateTime? DateAwarded { get; init; }
         public int? HardcoreAchieved { get; init; } // 0 or 1 or null?
          [JsonIgnore]
         public bool? IsHardcoreAchieved => HardcoreAchieved.HasValue ? HardcoreAchieved == 1 : null;
    }


    public record UserSummaryRecentlyPlayed
    {
        public int GameID { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public string? Title { get; init; }
        public string? ImageIcon { get; init; }
        public string? ImageTitle { get; init; }
        public string? ImageIngame { get; init; }
        public string? ImageBoxArt { get; init; }
        public DateTime LastPlayed { get; init; }
        public int AchievementsTotal { get; init; } // NumPossibleAchievements?
    }

    public record UserSummaryLastGame : GameInfo
    {
        // Inherits GameInfo
        public int IsFinal { get; init; } // 0 or 1? bool?
    }

    public record UserSummary : UserProfile
    {
        // Inherits UserProfile properties
        public UserLastActivity? LastActivity { get; init; }
        public int Rank { get; init; }
        public int RecentlyPlayedCount { get; init; }
        public List<UserSummaryRecentlyPlayed> RecentlyPlayed { get; init; } = new List<UserSummaryRecentlyPlayed>();
        // Key is GameID as string, Value is UserGameProgress
        public Dictionary<string, UserGameProgress> Awarded { get; init; } = new Dictionary<string, UserGameProgress>();
        // Outer key is GameID as string, Inner key is AchievementID as string
        public Dictionary<string, Dictionary<string, UserSummaryAchievement>> RecentAchievements { get; init; } = new Dictionary<string, Dictionary<string, UserSummaryAchievement>>();
        public UserSummaryLastGame? LastGame { get; init; }
        public int TotalRanked { get; init; } // Total ranked users on site?
        public string? Status { get; init; } // "Offline", "Online", etc? Enum?
    }

     public record WantToPlayGame
    {
        public int ID { get; init; }
        public string? Title { get; init; }
        public string? ImageIcon { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
        public int PointsTotal { get; init; }
        public int AchievementsPublished { get; init; }
    }

    public record FollowingUser
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public int Points { get; init; } // Hardcore
        public int PointsSoftcore { get; init; }
        public bool AmIFollowing { get; init; } // Always true for this endpoint? Verify.
    }

     public record FollowedUser
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public int Points { get; init; } // Hardcore
        public int PointsSoftcore { get; init; }
        public bool IsFollowingMe { get; init; }
    }

     public record RecentGameAward
    {
        [JsonPropertyName("User")]
        public string? Username { get; init; }
        public string? ULID { get; init; }
        public string? AwardKind { get; init; } // "mastered", etc. Enum?
        public DateTimeOffset AwardDate { get; init; }
        public int GameID { get; init; }
        public string? GameTitle { get; init; }
        public int ConsoleID { get; init; }
        public string? ConsoleName { get; init; }
    }

    public record RecentGameAwardsResponse : PaginatedResponse<RecentGameAward> { }
