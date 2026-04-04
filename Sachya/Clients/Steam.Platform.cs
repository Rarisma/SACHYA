using Sachya.Definitions.Steam;

namespace Sachya.Clients;

public partial class SteamWebApiClient : IPlatformClient
{
    public async Task<IReadOnlyList<PlatformGameInfo>> GetGamesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var result = await GetOwnedGamesAsync(userId, includeAppInfo: true);
        if (result?.response?.games == null)
            return Array.Empty<PlatformGameInfo>();

        return result.response.games.Select(g => new PlatformGameInfo
        {
            GameId = g.appid.ToString(),
            Name = g.name ?? $"App {g.appid}",
            ImageUrl = !string.IsNullOrEmpty(g.img_icon_url)
                ? $"https://media.steampowered.com/steamcommunity/public/images/apps/{g.appid}/{g.img_icon_url}.jpg"
                : null,
            Platform = "Steam",
            LastPlayed = null // Steam owned games don't include last played in this response
        }).ToList();
    }

    public async Task<IReadOnlyList<PlatformAchievement>> GetAchievementsAsync(string userId, string gameId, CancellationToken cancellationToken = default)
    {
        var result = await GetPlayerAchievementsAsync(userId, int.Parse(gameId));
        if (result?.playerstats?.achievements == null)
            return Array.Empty<PlatformAchievement>();

        return result.playerstats.achievements.Select(a => new PlatformAchievement
        {
            Id = a.apiname,
            Name = a.name ?? a.apiname,
            Description = a.description,
            IsEarned = a.achieved == 1,
            EarnedAt = a.unlocktime > 0
                ? DateTimeOffset.FromUnixTimeSeconds(a.unlocktime).UtcDateTime
                : null,
        }).ToList();
    }
}
