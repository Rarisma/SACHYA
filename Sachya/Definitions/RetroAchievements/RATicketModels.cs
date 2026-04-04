using System.Text.Json.Serialization;
using Sachya.Misc;

namespace Sachya.Definitions.RetroAchievements;

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
