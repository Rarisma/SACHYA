namespace Sachya;

/// <summary>
/// Common representation of a game with achievement data across all platforms.
/// </summary>
public record PlatformGameInfo
{
    public required string GameId { get; init; }
    public required string Name { get; init; }
    public string? ImageUrl { get; init; }
    public string? Platform { get; init; }
    public int EarnedAchievements { get; init; }
    public int TotalAchievements { get; init; }
    public DateTime? LastPlayed { get; init; }
}

/// <summary>
/// Common representation of a single achievement across all platforms.
/// </summary>
public record PlatformAchievement
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
    public bool IsEarned { get; init; }
    public bool IsHidden { get; init; }
    public DateTime? EarnedAt { get; init; }
    public double? RarityPercent { get; init; }
}

/// <summary>
/// Common interface for platform achievement clients.
/// Each platform client can implement this to provide a unified way to
/// query games and achievements across Steam, PlayStation, Xbox, and RetroAchievements.
/// </summary>
public interface IPlatformClient
{
    /// <summary>
    /// Gets the list of games with achievement progress for a user.
    /// </summary>
    /// <param name="userId">Platform-specific user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<PlatformGameInfo>> GetGamesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets achievements for a specific game.
    /// </summary>
    /// <param name="userId">Platform-specific user identifier.</param>
    /// <param name="gameId">Platform-specific game identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<PlatformAchievement>> GetAchievementsAsync(string userId, string gameId, CancellationToken cancellationToken = default);
}
