using Sachya.Definitions.Xbox;

namespace Sachya.Clients;

public partial class XboxApiClient : IPlatformClient
{
    public async Task<IReadOnlyList<PlatformGameInfo>> GetGamesAsync(string userId, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync();

        var history = await _apiClient.GetTitleHistoryAsync();

        return history.Titles.Select(t => new PlatformGameInfo
        {
            GameId = t.TitleId,
            Name = t.Name,
            ImageUrl = t.DisplayImage,
            Platform = "Xbox",
            EarnedAchievements = t.Achievement?.CurrentAchievements ?? 0,
            TotalAchievements = t.Achievement?.TotalAchievements ?? 0,
            LastPlayed = t.TitleHistory?.LastTimePlayed,
        }).ToList();
    }

    public async Task<IReadOnlyList<PlatformAchievement>> GetAchievementsAsync(string userId, string gameId, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync();

        var result = await _apiClient.GetAchievementsAsync(gameId);

        return result.Achievements.Select(a => new PlatformAchievement
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.ProgressState == "Achieved"
                ? a.Description ?? a.UnlockedDescription
                : a.LockedDescription ?? a.Description,
            IconUrl = a.ImageUnlocked?.Url ?? a.ImageLocked?.Url,
            IsEarned = a.ProgressState == "Achieved",
            IsHidden = a.IsSecret,
            EarnedAt = a.Progression?.TimeUnlocked,
            RarityPercent = a.Rarity?.CurrentPercentage,
        }).ToList();
    }
}
