using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Sachya.Definitions.RetroAchievements;

namespace Sachya.Clients;

public partial class RetroAchievements
{
    // --- Connect API Methods (from Standalones Guide) ---

    /// <summary>
    /// Retrieves the Connect API token using integration account credentials.
    /// Treat the returned token like a password.
    /// </summary>
    /// <param name="integrationUsername">The username of the dedicated integration account.</param>
    /// <param name="integrationPassword">The password of the dedicated integration account.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login information including the Connect API token.</returns>
    public async Task<ConnectLoginResponse> GetConnectApiTokenAsync(string integrationUsername, string integrationPassword, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"u", integrationUsername},
            {"p", integrationPassword},
            {"r", "login2"}
        };
        // This is a GET request according to the documentation
        return await GetApiAsync<ConnectLoginResponse>(ConnectApiBaseUrl, string.Empty, queryParams, cancellationToken);
    }

    /// <summary>
    /// Starts a game session for a player.
    /// </summary>
    /// <param name="integrationUsername">The username of the dedicated integration account.</param>
    /// <param name="connectToken">The Connect API token obtained via GetConnectApiTokenAsync.</param>
    /// <param name="gameId">The game's primary RetroAchievements ID.</param>
    /// <param name="playerUsername">The player's RetroAchievements username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session start information.</returns>
    public async Task<StartSessionResponse> StartPlayerSessionAsync(string integrationUsername, string connectToken, int gameId, string playerUsername, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"u", integrationUsername},
            {"t", connectToken},
            {"r", "startsession"},
            {"g", gameId.ToString()},
            {"k", playerUsername}
        };
        // Documentation shows this as POST
        return await PostConnectApiAsync<StartSessionResponse>(queryParams, null, cancellationToken);
    }

    /// <summary>
    /// Sends a heartbeat ping for an active player session, optionally updating Rich Presence.
    /// </summary>
    /// <param name="integrationUsername">The username of the dedicated integration account.</param>
    /// <param name="connectToken">The Connect API token obtained via GetConnectApiTokenAsync.</param>
    /// <param name="gameId">The game's primary RetroAchievements ID.</param>
    /// <param name="playerUsername">The player's RetroAchievements username.</param>
    /// <param name="richPresence">Optional Rich Presence string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ping response.</returns>
    public async Task<PingResponse> SendHeartbeatPingAsync(string integrationUsername, string connectToken, int gameId, string playerUsername, string? richPresence = null, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>
        {
            {"u", integrationUsername},
            {"t", connectToken},
            {"r", "ping"},
            {"g", gameId.ToString()},
            {"k", playerUsername}
        };

        HttpContent? content = null;
        if (!string.IsNullOrEmpty(richPresence))
        {
            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new StringContent(richPresence), "m");
            content = multipartContent;
        }

        // Documentation shows this as POST
        return await PostConnectApiAsync<PingResponse>(queryParams, content, cancellationToken);
    }

    /// <summary>
    /// Awards a single achievement to a player.
    /// </summary>
    /// <param name="integrationUsername">The username of the dedicated integration account.</param>
    /// <param name="connectToken">The Connect API token obtained via GetConnectApiTokenAsync.</param>
    /// <param name="playerUsername">The player's RetroAchievements username.</param>
    /// <param name="achievementId">The ID of the achievement to award.</param>
    /// <param name="isHardcore">True for hardcore mode, false for softcore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Award response.</returns>
    public async Task<AwardAchievementResponse> AwardSingleAchievementAsync(string integrationUsername, string connectToken, string playerUsername, int achievementId, bool isHardcore, CancellationToken cancellationToken = default)
    {
        string hardcoreFlag = isHardcore ? "1" : "0";
        string verificationHashInput = $"{achievementId}{playerUsername}{hardcoreFlag}{achievementId}";
        string verificationHash = CalculateMd5Hash(verificationHashInput);

        var queryParams = new Dictionary<string, string?>
        {
            {"u", integrationUsername},
            {"t", connectToken},
            {"r", "awardachievement"},
            {"k", playerUsername},
            {"a", achievementId.ToString()},
            {"v", verificationHash},
            {"h", hardcoreFlag}
        };
        // Documentation shows this as POST
        return await PostConnectApiAsync<AwardAchievementResponse>(queryParams, null, cancellationToken);
    }

    /// <summary>
    /// Awards multiple achievements to a player simultaneously.
    /// </summary>
    /// <param name="integrationUsername">The username of the dedicated integration account.</param>
    /// <param name="connectToken">The Connect API token obtained via GetConnectApiTokenAsync.</param>
    /// <param name="playerUsername">The player's RetroAchievements username.</param>
    /// <param name="achievementIds">A list of achievement IDs to award.</param>
    /// <param name="isHardcore">True for hardcore mode, false for softcore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Award response detailing which achievements were newly awarded.</returns>
    public async Task<AwardAchievementsResponse> AwardMultipleAchievementsAsync(string integrationUsername, string connectToken, string playerUsername, IEnumerable<int> achievementIds, bool isHardcore, CancellationToken cancellationToken = default)
    {
        string achievementIdList = string.Join(",", achievementIds);
        string hardcoreFlag = isHardcore ? "1" : "0";
        string verificationHashInput = $"{achievementIdList}{playerUsername}{hardcoreFlag}";
        string verificationHash = CalculateMd5Hash(verificationHashInput);

        var queryParams = new Dictionary<string, string?>
        {
            {"u", integrationUsername},
            {"t", connectToken},
            {"r", "awardachievements"},
            {"k", playerUsername}
            // Note: a, h, v are sent in the body
        };

        var multipartContent = new MultipartFormDataContent
        {
            { new StringContent(achievementIdList), "a" },
            { new StringContent(hardcoreFlag), "h" },
            { new StringContent(verificationHash), "v" }
        };

        return await PostConnectApiAsync<AwardAchievementsResponse>(queryParams, multipartContent, cancellationToken);
    }
}
