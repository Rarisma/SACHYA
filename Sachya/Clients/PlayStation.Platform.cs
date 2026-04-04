namespace Sachya.PSN;

public partial class PSNClient : IPlatformClient
{
    public async Task<IReadOnlyList<PlatformGameInfo>> GetGamesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var result = await GetUserTrophyTitlesAsync(userId, cancellationToken: cancellationToken);

        return result.TrophyTitles.Select(t =>
        {
            var earned = t.EarnedTrophies;
            var defined = t.DefinedTrophies;
            return new PlatformGameInfo
            {
                GameId = $"{t.NpCommunicationId}|{t.TrophyTitlePlatform}",
                Name = t.TrophyTitleName,
                ImageUrl = t.TrophyTitleIconUrl,
                Platform = t.TrophyTitlePlatform,
                EarnedAchievements = earned.Bronze + earned.Silver + earned.Gold + earned.Platinum,
                TotalAchievements = defined.Bronze + defined.Silver + defined.Gold + defined.Platinum,
                LastPlayed = t.LastUpdatedDateTime.UtcDateTime,
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<PlatformAchievement>> GetAchievementsAsync(string userId, string gameId, CancellationToken cancellationToken = default)
    {
        var parts = gameId.Split('|', 2);
        var npCommunicationId = parts[0];
        var platform = parts.Length > 1 ? parts[1] : "PS4";

        // Fetch definitions and earned status in parallel
        var definitionsTask = GetTitleTrophiesAsync(npCommunicationId, platform, cancellationToken: cancellationToken);
        var earnedTask = GetUserEarnedTrophiesAsync(npCommunicationId, platform, userId, cancellationToken: cancellationToken);

        await Task.WhenAll(definitionsTask, earnedTask);

        var definitions = definitionsTask.Result;
        var earned = earnedTask.Result;

        var earnedLookup = earned.Trophies.ToDictionary(e => e.TrophyId);

        return definitions.Trophies.Select(t =>
        {
            earnedLookup.TryGetValue(t.TrophyId, out var earnedInfo);
            return new PlatformAchievement
            {
                Id = t.TrophyId.ToString(),
                Name = t.TrophyName,
                Description = t.TrophyDetail,
                IconUrl = t.TrophyIconUrl,
                IsEarned = earnedInfo?.Earned ?? false,
                IsHidden = t.TrophyHidden,
                EarnedAt = earnedInfo?.EarnedDateTime,
                RarityPercent = double.TryParse(earnedInfo?.TrophyEarnedRate, out var rate) ? rate : null,
            };
        }).ToList();
    }
}
