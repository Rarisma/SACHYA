using Sachya.Definitions.RetroAchievements;

namespace Sachya.Clients;

public partial class RetroAchievements : IPlatformClient
{
    public async Task<IReadOnlyList<PlatformGameInfo>> GetGamesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var result = await GetUserCompletionProgressAsync(userId, count: 500, cancellationToken: cancellationToken);

        return result.Results.Select(g => new PlatformGameInfo
        {
            GameId = g.GameID.ToString(),
            Name = g.Title ?? $"Game {g.GameID}",
            ImageUrl = !string.IsNullOrEmpty(g.ImageIcon)
                ? $"https://media.retroachievements.org{g.ImageIcon}"
                : null,
            Platform = g.ConsoleName ?? "RetroAchievements",
            EarnedAchievements = g.NumAwardedHardcore,
            TotalAchievements = g.MaxPossible,
            LastPlayed = g.MostRecentAwardedDate?.UtcDateTime,
        }).ToList();
    }

    public async Task<IReadOnlyList<PlatformAchievement>> GetAchievementsAsync(string userId, string gameId, CancellationToken cancellationToken = default)
    {
        var result = await GetGameInfoAndUserProgressAsync(userId, int.Parse(gameId), cancellationToken: cancellationToken);

        return result.Achievements.Values.Select(a => new PlatformAchievement
        {
            Id = a.ID.ToString(),
            Name = a.Title ?? $"Achievement {a.ID}",
            Description = a.Description,
            IconUrl = null, // RA badge URLs require separate lookup
            IsEarned = a.DateEarnedHardcore != null,
            IsHidden = false,
            EarnedAt = a.DateEarnedHardcore ?? a.DateEarned,
        }).ToList();
    }
}
