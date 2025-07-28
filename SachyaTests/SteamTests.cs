using dotenv.net;
using Sachya;
using Sachya.Clients;
using Sachya.Definitions.Steam;

namespace Tests;

[TestFixture, Category("Integration")]
public class SteamTests
{
    private SteamWebApiClient _client;
    private string _apiKey;
    private readonly string _publicSteamId = "76561198411207982";

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        DotEnv.Load();
        var envVars = DotEnv.Read();
        _apiKey = envVars["SteamAPI"];
    }

    [SetUp]
    public void Setup()
    {
        // Instantiate the client. Some endpoints work without an API key.
        _client = new SteamWebApiClient(_apiKey);
    }

    [Test]
    public async Task GetNewsForAppAsync()
    {
        NewsForAppResult news = await _client.GetNewsForAppAsync(1772830);
        Assert.NotNull(news, "News response is null");
        Assert.AreEqual(1772830, news.appnews.appid, "App ID does not match");
        Assert.NotNull(news.appnews.newsitems, "News items collection is null");
        Assert.IsNotEmpty(news.appnews.newsitems, "No news items returned");
    }


    [Test]
    public async Task GetGlobalAchievementPercentagesForAppAsync()
    {
        var achievements = await _client.GetGlobalAchievementPercentagesForAppAsync(1772830);
        Assert.NotNull(achievements, "Achievements response is null");
    }

    [Test]
    public async Task GetPlayerSummariesAsync()
    {
        var players = await _client.GetPlayerSummariesAsync(_publicSteamId);
        Assert.NotNull(players, "Player summaries response is null");
        Assert.IsNotEmpty(players.response.players, "No players returned");
        // Verify that the returned player has the expected SteamID.
        Assert.That(players.response.players[0].steamid, Is.EqualTo(_publicSteamId), "Returned SteamID does not match");
    }

    [Test]
    public async Task GetFriendListAsync()
    {
        // Note: This profile’s friend list may be private. We simply check the call succeeds.
        var friends = await _client.GetFriendListAsync(_publicSteamId);
        Assert.NotNull(friends, "Friend list response is null");
        // If friends are returned, check that at least one has a non-null steamid.
        if (friends.friendslist.friends.Count > 0)
        {
            Assert.NotNull(friends.friendslist.friends[0].steamid, "Friend steamid is null");
        }
    }

    [Test]
    public async Task GetPlayerAchievementsAsync()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Ignore("STEAM_API_KEY not provided; skipping player achievements test.");
        }
        PlayerAchievementsResult stats = await _client.GetPlayerAchievementsAsync(_publicSteamId, 243470);
        Assert.NotNull(stats, "Player achievements response is null");
        // Depending on profile settings, 'success' might be false if data isn’t available.
        Assert.IsTrue(stats.playerstats.success, "Achievement retrieval not successful");
        Assert.NotNull(stats.playerstats.achievements, "Achievements list is null");
    }

    [Test]
    public async Task GetUserStatsForGameAsync()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Ignore("STEAM_API_KEY not provided; skipping user stats test.");
        }
        PlayerAchievementsResult stats = await _client.GetUserStatsForGameAsync(_publicSteamId, 243470);
        Assert.NotNull(stats, "User stats response is null");
    }

    [Test]
    public async Task GetOwnedGamesAsync()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Ignore("STEAM_API_KEY not provided; skipping owned games test.");
        }
        OwnedGamesResult ownedGames = await _client.GetOwnedGamesAsync(_publicSteamId, includeAppInfo: true);
        Assert.NotNull(ownedGames, "Owned games response is null");
        Assert.GreaterOrEqual(ownedGames.response.game_count, 0, "Game count should be >= 0");
        // If there are games, verify that game details are provided.
        if (ownedGames.response != null && ownedGames.response.game_count > 0)
        {
            Assert.IsTrue(ownedGames.response.games[0].appid > 0, "Invalid appid in game entry");
        }
    }

    [SetUp]
    public async Task BeforeEach()
    {
        //delay every test 1s to prevent rate limits
        await Task.Delay(1000);
    }

    
    [Test]
    public async Task GetRecentlyPlayedGames()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Ignore("STEAM_API_KEY not provided; skipping recently played games test.");
        }
        RecentlyPlayedGamesResult recentGames = await _client.GetRecentlyPlayedGamesAsync(_publicSteamId, count: 1);
        Assert.NotNull(recentGames, "Recently played games response is null");
        Assert.GreaterOrEqual(recentGames.response.total_count, 0, "Total count should be >= 0");
        // Even if no recent games exist, the API should return a valid structure.
    }
}
