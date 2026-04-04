using System.Text.Json.Serialization;
using Sachya.Misc;

namespace Sachya.Definitions.RetroAchievements;

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
