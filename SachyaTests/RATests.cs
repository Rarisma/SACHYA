using dotenv.net;
using Sachya;
namespace Tests
{
    [TestFixture] // Marks the class as containing NUnit tests
    [NonParallelizable] //Prevent ratelimits
    public class RetroAchievementsTests : IDisposable
    {
        private static string ApiKey = "";
        private const string TestUsername = "Scott"; // A known, stable admin account often used in examples
        private const string TestUsernameWithData = "MaxMilyin"; // A known top user with lots of data
        private const int TestGameIdSonic1 = 1; // Sonic the Hedgehog (Mega Drive)
        private const int TestGameIdDragster = 14402; // Dragster (Atari 2600)
        private const int TestAchievementIdSonic1Easy = 9; // "That Was Easy" from Sonic 1
        private const int TestLeaderboardIdSonic1Score = 104370; // "South Island Conqueror" from Sonic 1
        private const string WebApiBase = "https://retroachievements.org/API/"; // For logging comparison

        private RetroAchievements _apiClient = null!; // Initialized in OneTimeSetUp
        private HttpClient _httpClient = null!; // Initialized in OneTimeSetUp

        // Skip reason and flag
        private const string SkipReason = $"Missing API key";
        private static bool SkipTests => ApiKey.StartsWith("---");

        [OneTimeSetUp] // NUnit equivalent of setting up once for the entire fixture
        public void FixtureSetup()
        {
            DotEnv.Load();
            var envVars = DotEnv.Read();
            ApiKey = envVars["RetroAPI"];
            
            if (SkipTests)
            {
                Assert.Ignore(SkipReason); // Use Assert.Ignore to skip all tests in the fixture
            }

            _httpClient = new HttpClient(); // Use a real HttpClient
            _apiClient = new RetroAchievements(TestUsername, ApiKey, "IntegrationTestClient/1.0 (NUnit)", _httpClient);
            TestContext.Progress.WriteLine("Initialized HttpClient and ApiClient for integration tests.");
        }

        private void LogUrl(string url)
        {
            // Obfuscate API key before logging
            var obfuscatedUrl = url.Replace(ApiKey, "[API_KEY_HIDDEN]");
            TestContext.Progress.WriteLine($"Hitting URL: {obfuscatedUrl}");
        }

        // --- User Endpoint Tests ---

        [Test]
        public async Task GetUserProfileAsync()
        {
            // Arrange
            var usernameToTest = TestUsernameWithData; // User known to exist and have data

            // Act
            TestContext.Progress.WriteLine($"Testing GetUserProfileAsync for user: {usernameToTest}");
            var result = await _apiClient.GetUserProfileAsync(usernameToTest);
            LogUrl($"{WebApiBase}API_GetUserProfile.php?u={usernameToTest}&y=..."); // Example logged URL structure

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(usernameToTest, result.Username, "Username mismatch (ignore case)");
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.UserPic), "UserPic should not be empty");
            Assert.IsTrue(result.TotalPoints > 0 || result.TotalSoftcorePoints > 0, "User should have some points");
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ULID), "ULID should not be empty");
            Assert.IsTrue(result.MemberSince < DateTime.UtcNow, "MemberSince should be in the past");
            TestContext.Progress.WriteLine($"... OK: Received profile for {result.Username} (ID: {result.ID}, Points: {result.TotalPoints})");
        }

        [Test]
        public async Task GetUserPointsAsync()
        {
            // Arrange
            var usernameToTest = TestUsernameWithData;

            // Act
            TestContext.Progress.WriteLine($"Testing GetUserPointsAsync for user: {usernameToTest}");
            var result = await _apiClient.GetUserPointsAsync(usernameToTest);
            LogUrl($"{WebApiBase}API_GetUserPoints.php?u={usernameToTest}&y=...");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Points >= 0, "Hardcore points should be >= 0");
            Assert.IsTrue(result.SoftcorePoints >= 0, "Softcore points should be >= 0");
            TestContext.Progress.WriteLine($"... OK: Received points for {usernameToTest} (HC: {result.Points}, SC: {result.SoftcorePoints})");
        }

        [Test]
        public async Task GetUserRecentlyPlayedGamesAsync()
        {
            // Arrange
            var usernameToTest = TestUsernameWithData;
            int expectedCount = 5;

            // Act
            TestContext.Progress.WriteLine($"Testing GetUserRecentlyPlayedGamesAsync for user: {usernameToTest}");
            var results = await _apiClient.GetUserRecentlyPlayedGamesAsync(usernameToTest, count: expectedCount);
            LogUrl($"{WebApiBase}API_GetUserRecentlyPlayedGames.php?u={usernameToTest}&c={expectedCount}&o=0&y=...");

            // Assert
            Assert.IsNotNull(results);
            Assert.IsNotEmpty(results, "This user should have played games recently");
            Assert.IsTrue(results.Count <= expectedCount, $"API should return at most {expectedCount} games");
            var firstGame = results.First();
            Assert.IsTrue(firstGame.GameID > 0, "First game ID should be > 0");
            Assert.IsFalse(string.IsNullOrWhiteSpace(firstGame.Title), "First game Title should not be empty");
            Assert.IsFalse(string.IsNullOrWhiteSpace(firstGame.ConsoleName), "First game ConsoleName should not be empty");
            Assert.IsTrue(firstGame.LastPlayed < DateTime.UtcNow, "First game LastPlayed should be in the past");
            TestContext.Progress.WriteLine($"... OK: Received {results.Count} recently played games for {usernameToTest}. First: {firstGame.Title}");
        }

        // --- Game Endpoint Tests ---

        [Test]
        public async Task GetGameAsync()
        {
            // Arrange
            int gameId = TestGameIdSonic1;

            // Act
            TestContext.Progress.WriteLine($"Testing GetGameAsync for game ID: {gameId}");
            var result = await _apiClient.GetGameAsync(gameId);
            LogUrl($"{WebApiBase}API_GetGame.php?i={gameId}&y=...");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Sonic the Hedgehog", result.Title, "Game Title mismatch");
            Assert.AreEqual("Genesis/Mega Drive", result.ConsoleName, "Console Name mismatch");
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ImageIcon), "ImageIcon should not be empty");
            TestContext.Progress.WriteLine($"... OK: Received game info for {result.Title} ({result.ConsoleName})");
        }

        [Test]
        public async Task GetGameExtendedAsyncWithAchievements()
        {
            // Arrange
            int gameId = TestGameIdDragster;

            // Act
            TestContext.Progress.WriteLine($"Testing GetGameExtendedAsync for game ID: {gameId}");
            var result = await _apiClient.GetGameExtendedAsync(gameId);
            LogUrl($"{WebApiBase}API_GetGameExtended.php?i={gameId}&f=3&y=...");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(gameId, result.ID, "Game ID mismatch");
            Assert.AreEqual("Dragster", result.Title,"Game Title mismatch");
            Assert.IsNotNull(result.Achievements, "Achievements dictionary should not be null");
            Assert.IsNotEmpty(result.Achievements, "Dragster should have achievements");
            Assert.IsTrue(result.NumAchievements > 0, "NumAchievements should be > 0");
            Assert.IsTrue(result.NumDistinctPlayersCasual >= 0, "NumDistinctPlayersCasual should be >= 0");

            var firstAch = result.Achievements.First().Value;
            Assert.IsTrue(firstAch.ID > 0, "First achievement ID should be > 0");
            Assert.IsFalse(string.IsNullOrWhiteSpace(firstAch.Title), "First achievement Title should not be empty");
            TestContext.Progress.WriteLine($"... OK: Received extended info for {result.Title}. Found {result.Achievements.Count} achievements. First: {firstAch.Title}");
        }

        // --- Console/System Endpoint Tests ---

        [Test]
        public async Task GetConsoleIdsAsync()
        {
            // Act
            TestContext.Progress.WriteLine($"Testing GetConsoleIdsAsync");
            var results = await _apiClient.GetConsoleIdsAsync();
            LogUrl($"{WebApiBase}API_GetConsoleIDs.php?y=...");

            // Assert
            Assert.IsNotNull(results);
            Assert.IsNotEmpty(results);
            // Check for known consoles using LINQ Any
            Assert.IsTrue(results.Any(c => c.ID == 1 && c.Name.Contains("Genesis/Mega Drive")), "Mega Drive (ID 1) not found");
            Assert.IsTrue(results.Any(c => c.ID == 25 && c.Name.Contains("Atari 2600")), "Atari 2600 (ID 25) not found");
            TestContext.Progress.WriteLine($"... OK: Received {results.Count} console entries.");
        }

        // --- Feed Endpoint Tests ---

        [Test]
        public async Task GetTopTenUsersAsync()
        {
            // Act
            TestContext.Progress.WriteLine($"Testing GetTopTenUsersAsync");
            var results = await _apiClient.GetTopTenUsersAsync();
            LogUrl($"{WebApiBase}API_GetTopTenUsers.php?y=...");

            // Assert
            Assert.IsNotNull(results);
            Assert.IsNotEmpty(results);
            // Use Assert.That for collection constraints
            Assert.That(results.Count, Is.GreaterThanOrEqualTo(9).And.LessThanOrEqualTo(11), "Should return around 10 users");
            var firstUser = results.First();
            Assert.IsFalse(string.IsNullOrWhiteSpace(firstUser.Username), "First user Username should not be empty");
            Assert.IsFalse(string.IsNullOrWhiteSpace(firstUser.ULID), "First user ULID should not be empty");
            Assert.IsTrue(firstUser.TotalPoints > 1000, "Top user should have significant points");
            TestContext.Progress.WriteLine($"... OK: Received {results.Count} top users. First: {firstUser.Username} ({firstUser.TotalPoints} pts)");
        }

        // --- Achievement Endpoint Tests ---

        [Test]
        public async Task GetAchievementUnlocksAsync()
        {
            // Arrange
            int achievementId = TestAchievementIdSonic1Easy; // Known achievement with many unlocks
            int expectedCount = 10;

            // Act
            TestContext.Progress.WriteLine($"Testing GetAchievementUnlocksAsync for achievement ID: {achievementId}");
            var result = await _apiClient.GetAchievementUnlocksAsync(achievementId, count: expectedCount);
            LogUrl($"{WebApiBase}API_GetAchievementUnlocks.php?a={achievementId}&c={expectedCount}&o=0&y=...");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Achievement, "Achievement info should not be null");
            Assert.AreEqual(achievementId, result.Achievement.ID, "Achievement ID mismatch");
            Assert.AreEqual("That Was Easy", result.Achievement.Title, "Achievement Title mismatch");
            Assert.IsTrue(result.UnlocksCount > 1000, "This achievement should have many unlocks");
            Assert.IsNotNull(result.Unlocks, "Unlocks list should not be null");
            Assert.IsNotEmpty(result.Unlocks, "Unlocks list should not be empty");
            Assert.IsTrue(result.Unlocks.Count <= expectedCount, $"Should return at most {expectedCount} unlocks");
            var firstUnlock = result.Unlocks.First();
            Assert.IsFalse(string.IsNullOrWhiteSpace(firstUnlock.Username), "First unlock Username should not be empty");
            TestContext.Progress.WriteLine($"... OK: Received {result.Unlocks.Count} unlocks for '{result.Achievement.Title}'. Total: {result.UnlocksCount}");
        }

        // --- User + Game Endpoint Tests ---

        [Test]
        public async Task GetGameInfoAndUserProgressAsync()
        {
            // Arrange
            int gameId = TestGameIdSonic1;
            var usernameToTest = TestUsernameWithData; // User known to have played Sonic 1

            // Act
            TestContext.Progress.WriteLine($"Testing GetGameInfoAndUserProgressAsync for game {gameId} and user {usernameToTest}");
            var result = await _apiClient.GetGameInfoAndUserProgressAsync(usernameToTest, gameId);
            LogUrl($"{WebApiBase}API_GetGameInfoAndUserProgress.php?u={usernameToTest}&g={gameId}&y=...");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(gameId, result.ID, "Game ID mismatch");
            Assert.AreEqual("Sonic the Hedgehog", result.Title, "Game Title mismatch");
            Assert.IsTrue(result.NumAwardedToUser > 0 || result.NumAwardedToUserHardcore > 0, "This user likely has unlocks");
            Assert.IsNotNull(result.Achievements, "Achievements dictionary should not be null");
            Assert.IsNotEmpty(result.Achievements, "Game should have achievements");

            // Check if progress data is embedded in achievements
            var firstAchKvp = result.Achievements.First();
            Assert.IsTrue(int.Parse(firstAchKvp.Key) > 0, "Achievement key (ID string) should parse to > 0");
            Assert.IsNotNull(firstAchKvp.Value, "First achievement value should not be null");
            // Check if DateEarned or DateEarnedHardcore has a value for at least one achievement (heuristic)
            Assert.IsTrue(result.Achievements.Values.Any(ach => ach.DateEarned.HasValue || ach.DateEarnedHardcore.HasValue), "At least one achievement should show earned date");
            TestContext.Progress.WriteLine($"... OK: Received progress for {usernameToTest} on {result.Title}. Earned HC: {result.NumAwardedToUserHardcore}/{result.NumAchievements}");
        }

        // --- Leaderboard Endpoint Test ---
        [Test]
        public async Task GetLeaderboardEntriesAsync()
        {
            // Arrange
            int leaderboardId = TestLeaderboardIdSonic1Score;
            int expectedCount = 5;

            // Act
            TestContext.Progress.WriteLine($"Testing GetLeaderboardEntriesAsync for leaderboard ID: {leaderboardId}");
            var result = await _apiClient.GetLeaderboardEntriesAsync(leaderboardId, count: expectedCount);
            LogUrl($"{WebApiBase}API_GetLeaderboardEntries.php?i={leaderboardId}&c={expectedCount}&o=0&y=...");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Total > 0, "This leaderboard should have entries");
            Assert.IsNotNull(result.Results, "Results list should not be null");
            Assert.IsNotEmpty(result.Results, "Results list should not be empty");
            Assert.IsTrue(result.Results.Count <= expectedCount, $"Should return at most {expectedCount} entries");

            var firstEntry = result.Results.First();
            Assert.AreEqual(1, firstEntry.Rank, "Top entry Rank should be 1");
            Assert.IsFalse(string.IsNullOrWhiteSpace(firstEntry.Username), "Rank 1 Username should not be empty");
            Assert.IsTrue(firstEntry.Score > 0, "Rank 1 Score should be > 0");
            Assert.IsNotNull(firstEntry.DateSubmitted, "Rank 1 DateSubmitted should not be null");

            TestContext.Progress.WriteLine($"... OK: Received {result.Results.Count} entries for leaderboard {leaderboardId}. Rank 1: {firstEntry.Username} ({firstEntry.FormattedScore})");
        }

        [Test]
        public async Task GetGameRankAndScoreAsync()
        {
            // Arrange
            int gameId = 24541; // GP World (SG-1000), likely few players/masters

            // Act
            TestContext.Progress.WriteLine($"Testing GetGameRankAndScoreAsync (HighScores) for game ID: {gameId}");
            var results = await _apiClient.GetGameRankAndScoreAsync(gameId, GameRankType.HighScores); // Test high scores first
            LogUrl($"{WebApiBase}API_GetGameRankAndScore.php?g={gameId}&t=0&y=...");

            // Assert
            Assert.IsNotNull(results, "HighScores result list should not be null (can be empty)");
            if (results.Any())
            {
                var firstRank = results.First();
                Assert.IsFalse(string.IsNullOrWhiteSpace(firstRank.Username), "Top scorer Username should not be empty");
                TestContext.Progress.WriteLine($"... OK: Received {results.Count} high score entries for {gameId}. Top score by: {firstRank.Username}");
            }
            else
            {
                TestContext.Progress.WriteLine($"... OK: Received 0 high score entries for {gameId} (as expected or possible).");
            }

            // Also test latest masters (might be empty)
            TestContext.Progress.WriteLine($"Testing GetGameRankAndScoreAsync (LatestMasters) for game ID: {gameId}");
            results = await _apiClient.GetGameRankAndScoreAsync(gameId, GameRankType.LatestMasters);
            LogUrl($"{WebApiBase}API_GetGameRankAndScore.php?g={gameId}&t=1&y=...");
            Assert.IsNotNull(results, "LatestMasters result list should not be null (can be empty)");
            if (results.Any())
            {
                var firstMaster = results.First();
                Assert.IsFalse(string.IsNullOrWhiteSpace(firstMaster.Username), "Most recent master Username should not be empty");
                TestContext.Progress.WriteLine($"... OK: Received {results.Count} latest master entries for {gameId}. Most recent: {firstMaster.Username}");
            }
            else
            {
                TestContext.Progress.WriteLine($"... OK: Received 0 latest master entries for {gameId} (as expected or possible).");
            }
        }

        [SetUp]
        public async Task BeforeEach()
        {
            //delay every test 1s to prevent rate limits
            await Task.Delay(1000);
        }

        [OneTimeTearDown] // NUnit equivalent of cleaning up once after all tests
        public void FixtureTearDown()
        {
            _httpClient?.Dispose();
            TestContext.Progress.WriteLine("Disposed HttpClient.");
        }

        // IDisposable implementation (optional if using OneTimeTearDown, but good practice)
        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
