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
