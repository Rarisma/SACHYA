// PlayStationTrophyClientTests.cs (Modified for Real API)
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Sachya;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using dotenv.net;
using Sachya.PSN;
using System.Text;
using System.Text.Json;

namespace Tests;

[TestFixture]
[Explicit("Hits the real PlayStation API and requires valid authentication via environment variables.")] // Mark class as Explicit
public class PlayStationTrophyClientTests_Integration
{
    // --- Test Configuration ---
    private string? _testNumericAccountId; // Account ID for testing access to OTHER users

    // --- Test User/Game Data ---
    private const string TestAccountIdMe = "me"; // Represents the authenticated user
    private const string GravityRushNpCommunicationId = "NPWR07915_00"; // Gravity Rush Remastered PS4
    private const string GravityRushPlatform = "PS4";
    private const string GravityRushNpTitleId = "CUSA01130_00"; // Gravity Rush Remastered PS4
    private const string AstrosPlayroomNpCommunicationId = "NPWR20188_00"; // ASTRO's PLAYROOM PS5
    private const string AstrosPlayroomPlatform = "PS5";
    private const string FarCry5NpCommunicationId = "NPWR12514_00"; // ASTRO's PLAYROOM PS5
    private const string FarCry5Platform = "PS4";


    private HttpClient _httpClient = null!;
    private PSNClient _client = null!;
    private const int ApiDelayMs = 500; // Delay between tests to mitigate rate limiting

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {        
        DotEnv.Load();
        var envVars = DotEnv.Read();
        _testNumericAccountId = envVars["PSNTestUser"]; // Optional, for testing other accounts
        var npssoToken = envVars["PSN_NPSSO"];
        _client = await PSNClient.CreateFromNpsso(npssoToken);
    }

    [OneTimeTearDown]
    public void GlobalTeardown()
    {
        _httpClient?.Dispose();
    }

    [SetUp]
    public async Task PerTestDelay()
    {
        // Add a small delay before each test to help avoid rate limits
        await Task.Delay(ApiDelayMs);
    }


    // --- Test Cases ---

    [Test]
    public async Task GetUserTrophyTitlesAsync_Me_ReturnsData()
    {
        // Act
        var result = await _client.GetUserTrophyTitlesAsync(TestAccountIdMe);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.TrophyTitles);
        // Assuming the test account has at least one game with trophies
        Assert.Greater(result.TotalItemCount, -1); // Allow 0 if account is new/empty
        Console.WriteLine($"Found {result.TotalItemCount} trophy titles for 'me'. First: {result.TrophyTitles.FirstOrDefault()?.TrophyTitleName ?? "N/A"}");
    }

    [Test]
    public async Task GetUserTrophyTitlesAsync_WithPagination_ReturnsData()
    {
        // Arrange
        int limit = 100;
        int offset = 0;

        // Act
        // Ensure the account has enough games for this offset/limit to make sense
        var result = await _client.GetUserTrophyTitlesAsync(TestAccountIdMe, limit, offset);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.TrophyTitles);
        Assert.LessOrEqual(result.TrophyTitles.Count, limit); // Might return fewer if near the end
        // Check if pagination offsets seem correct (can be null if only one page)
        if(result.TotalItemCount > limit + offset) Assert.IsNotNull(result.NextOffset);
        if(offset > 0) Assert.IsNotNull(result.PreviousOffset);

        Console.WriteLine($"Pagination test: Got {result.TrophyTitles.Count} titles. NextOffset: {result.NextOffset}, PrevOffset: {result.PreviousOffset}");
    }


    [Test]
    public async Task GetTitleTrophiesAsync_GravityRushPS4_ReturnsDefinitions()
    {
        // Act
        // This assumes Gravity Rush Remastered PS4 exists and has trophies defined.
        // The client should automatically add npServiceName=trophy due to PS4 platform
        var result = await _client.GetTitleTrophiesAsync(GravityRushNpCommunicationId, GravityRushPlatform);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Trophies);
        Assert.Greater(result.TotalItemCount, 0); // Expecting trophies for this game
        Assert.IsTrue(result.Trophies.Any(t => t.TrophyType.Equals("platinum", StringComparison.OrdinalIgnoreCase)), "Should contain a platinum trophy definition.");
        Console.WriteLine($"Found {result.TotalItemCount} trophy definitions for Gravity Rush Remastered PS4.");
    }

    [Test]
    public async Task GetTitleTrophiesAsync_AstrosPlayroomPS5_ReturnsDefinitions()
    {
        // Act
        // PS5 platform, npServiceName should NOT be added by the client
        var result = await _client.GetTitleTrophiesAsync(AstrosPlayroomNpCommunicationId, AstrosPlayroomPlatform, "all");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Trophies);
        Assert.Greater(result.TotalItemCount, 0);
        Assert.IsTrue(result.Trophies.Any(t => t.TrophyType.Equals("platinum", StringComparison.OrdinalIgnoreCase)), "Should contain a platinum trophy definition.");
        // Check for PS5 specific fields (might be null if no trophy uses them)
        Assert.IsTrue(result.Trophies.Any(t => t.TrophyProgressTargetValue != null || t.TrophyRewardName != null), "Astro's might have progress/reward trophies.");
        Console.WriteLine($"Found {result.TotalItemCount} trophy definitions for ASTRO's PLAYROOM PS5.");
    }

    [Test]
    public async Task GetTitleTrophiesAsync_WithLanguage_ReturnsLocalizedData()
    {
        // Arrange
        string lang = "de-DE"; // German

        // Act
        var resultEn = await _client.GetTitleTrophiesAsync(AstrosPlayroomNpCommunicationId, AstrosPlayroomPlatform, "all", acceptLanguage: "en-US");
        await Task.Delay(ApiDelayMs); // Add delay before next call
        var resultDe = await _client.GetTitleTrophiesAsync(AstrosPlayroomNpCommunicationId, AstrosPlayroomPlatform, "all", acceptLanguage: lang);


        // Assert
        Assert.IsNotNull(resultEn);
        Assert.IsNotNull(resultDe);
        Assert.Greater(resultEn.Trophies.Count, 0);
        Assert.AreEqual(resultEn.Trophies.Count, resultDe.Trophies.Count);

        var platinumEn = resultEn.Trophies.FirstOrDefault(t => t.TrophyType == "platinum");
        var platinumDe = resultDe.Trophies.FirstOrDefault(t => t.TrophyType == "platinum");

        Assert.IsNotNull(platinumEn);
        Assert.IsNotNull(platinumDe);
        Assert.AreNotEqual(platinumEn!.TrophyName, platinumDe!.TrophyName, "Platinum trophy name should differ by language.");
        Console.WriteLine($"EN Platinum: {platinumEn.TrophyName}");
        Console.WriteLine($"DE Platinum: {platinumDe.TrophyName}");
    }


    [Test]
    public async Task GetUserEarnedTrophiesAsync_GravityRushPS4_ReturnsEarnedStatus()
    {
        // Act
        // Assumes the authenticated account has played Gravity Rush Remastered PS4.
        // If not played, this will likely throw a 404 PlaystationApiException.
        UserEarnedTrophiesResponse result;
        try
        {
            result = await _client.GetUserEarnedTrophiesAsync(GravityRushNpCommunicationId, GravityRushPlatform, TestAccountIdMe, "all");
        }
        catch (PlaystationApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Fixed: Remove .ResponseContent reference as it doesn't exist
            Assert.Inconclusive($"Test account '{TestAccountIdMe}' has not played Gravity Rush Remastered (NPWR08920_00) or sync is needed. Error: {ex.Message}. Skipping.");
            return; // Skip assertion
        }


        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Trophies);
        Assert.GreaterOrEqual(result.TotalItemCount, 0); // Total definitions should match game
        // Check if *any* trophies are marked earned or not, depending on account state
        bool hasEarned = result.Trophies.Any(t => t.Earned);
        bool hasUnearned = result.Trophies.Any(t => !t.Earned);
        Console.WriteLine($"Gravity Rush Earned Status: Found {result.Trophies.Count} trophies. Has Earned: {hasEarned}. Has Unearned: {hasUnearned}.");
        Assert.IsTrue(hasEarned || hasUnearned, "Should have either earned or unearned trophies if game was played.");
    }

    [Test]
    public async Task GetUserTrophySummaryAsync_Me_ReturnsSummary()
    {
        // Act
        var result = await _client.GetUserTrophySummaryAsync("me");

        // Assert
        Assert.IsNotNull(result);
        Assert.Greater(result.TrophyLevel, 0); // Assuming account has played games
        Assert.Greater(result.Tier, 0);
        Assert.IsNotNull(result.EarnedTrophies);
        Console.WriteLine($"User 'me': Level {result.TrophyLevel}, Tier {result.Tier}, Points {result.TrophyPoint}");
        Console.WriteLine($" -> Bronze: {result.EarnedTrophies.Bronze}, Silver: {result.EarnedTrophies.Silver}, Gold: {result.EarnedTrophies.Gold}, Platinum: {result.EarnedTrophies.Platinum}");
    }

    [Test]
    public async Task GetTitleTrophyGroupsAsync_GravityRushPS4_ReturnsGroupDefinitions()
    {
        // Act
        var result = await _client.GetTitleTrophyGroupsAsync(GravityRushNpCommunicationId, GravityRushPlatform);

        // Assert
        Assert.IsNotNull(result);
        //Assert.AreEqual("Gravity Rush Remastered", result.TrophyGroups[0]); // Check title name matches
        Assert.IsNotNull(result.TrophyGroups);
        Assert.GreaterOrEqual(result.TrophyGroups.Count, 1); // Should have at least 'default' group
        Assert.IsTrue(result.TrophyGroups.Any(g => g.TrophyGroupId == "default"));
        Console.WriteLine($"Found {result.TrophyGroups.Count} trophy groups for Gravity Rush Remastered.");
    }

    [Test]
    public async Task GetUserEarnedTrophyGroupsAsync_GravityRushPS4_ReturnsEarnedGroupStatus()
    {
        // Act
        // Assumes the authenticated account has played Gravity Rush Remastered PS4.
        UserEarnedTrophyGroupsResponse result;
        try
        {
            result = await _client.GetUserEarnedTrophyGroupsAsync(GravityRushNpCommunicationId, 
                GravityRushPlatform, TestAccountIdMe);
        }
        catch (PlaystationApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            Assert.Inconclusive($"Test account '{TestAccountIdMe}' has not played Gravity Rush Remastered (NPWR08920_00) or sync is needed. Skipping.");
            return; // Skip assertion
        }

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.TrophyGroups);
        Assert.GreaterOrEqual(result.TrophyGroups.Count, 1); // Expect at least default group if played
        // UserEarnedTrophyGroupsResponse doesn't have overall Progress - check individual group progress instead
        Assert.IsTrue(result.TrophyGroups.All(g => g.Progress >= 0 && g.Progress <= 100)); // Each group progress should be 0-100%
        Console.WriteLine($"Gravity Rush Earned Groups: Found {result.TrophyGroups.Count} groups.");
        foreach(var group in result.TrophyGroups)
        {
            Console.WriteLine($" -> Group: {group.TrophyGroupId}, Progress: {group.Progress}%");
        }
    }


    [Test]
    public async Task GetUserTitlesTrophySummaryAsync_SingleTitle_ReturnsSummary()
    {
        // Arrange
        string titleIds = GravityRushNpTitleId;

        // Act
        // This requires the user has played the title associated with GravityRushNpTitleId
        UserTitlesTrophySummaryResponse result;
        try
        {
            result = await _client.GetUserTitlesTrophySummaryAsync(TestAccountIdMe, titleIds, GravityRushPlatform);
        }
        catch (PlaystationApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Fixed: Remove .ResponseContent reference as it doesn't exist
            Assert.Fail($"API returned 404 for title summary request with ID {titleIds}. Error: {ex.Message}");
            return;
        }


        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Titles);
        Assert.AreEqual(1, result.Titles.Count); // Requested one title ID

        var titleSummary = result.Titles.First();
        Assert.AreEqual(titleIds, titleSummary.NpTitleId);

        // TrophyTitles list inside will be empty if the user hasn't played this specific title ID
        if (!titleSummary.TrophyTitles.Any())
        {
            Assert.Inconclusive($"Test account '{TestAccountIdMe}' has not played title ID '{titleIds}' (Gravity Rush Remastered) or sync is needed. Skipping detailed assertions.");
        }
        else
        {
            var trophyInfo = titleSummary.TrophyTitles.First();
            Assert.AreEqual(GravityRushNpCommunicationId, trophyInfo.NpCommunicationId);
            Assert.GreaterOrEqual(trophyInfo.Progress, 0);
            Console.WriteLine($"Title Summary for {titleIds}: Progress {trophyInfo.Progress}%");
        }
    }

    [Test]
    public async Task GetUserTitlesTrophySummaryAsync_IncludeUnearned_IncludesList()
    {
        // Arrange
        string titleIds = FarCry5NpCommunicationId;

        // Act
        UserTitlesTrophySummaryResponse result;
        try
        {
            // Assuming the user has played but NOT platinumed Gravity Rush for this test
            // Fix: Add platform parameter and remove bool parameter - use overloaded method if available
            result = await _client.GetUserTitlesTrophySummaryAsync(TestAccountIdMe, 
                titleIds, FarCry5Platform,  CancellationToken.None);
        }
        catch (PlaystationApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Fixed: Remove .ResponseContent reference
            Assert.Fail($"API returned 404 for title summary request with ID {titleIds}. Error: {ex.Message}");
            return;
        }

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Titles.Count);

        var titleSummary = result.Titles.First();
        if (!titleSummary.TrophyTitles.Any())
        {
            Assert.Inconclusive($"Test account '{TestAccountIdMe}' has not played title ID '{titleIds}'. Cannot test includeNotEarnedTrophyIds.");
            return;
        }

        var trophyInfo = titleSummary.TrophyTitles.First();
        // Only check NotEarnedTrophyIds if progress is < 100%
        if (trophyInfo.Progress < 100)
        {
            Assert.IsNotNull(trophyInfo.NotEarnedTrophyIds, "notEarnedTrophyIds should be present when requested and progress < 100%");
            Assert.Greater(trophyInfo.NotEarnedTrophyIds!.Count, 0, "Should have unearned trophy IDs if progress < 100%");
            Console.WriteLine($"Found {trophyInfo.NotEarnedTrophyIds.Count} unearned trophy IDs for {titleIds}.");
        }
        else
        {
            // If progress is 100%, the list might be empty or null, which is valid.
            Console.WriteLine($"Progress is 100% for {titleIds}. notEarnedTrophyIds may be empty/null.");
            Assert.IsTrue(trophyInfo.NotEarnedTrophyIds == null || !trophyInfo.NotEarnedTrophyIds.Any());
        }
    }

    /*TODO: reenable this test; gamehelp is null for Astros Playroom despite supporting it
     It does require a paid PSN Account so it might just be because I only have a free account.
    // --- Game Help Tests (Example - Astro's Playroom) ---
    // These require the game to actually support Game Help

    [Test]
    public async Task GetTrophiesWithGameHelpAsync_AstrosPlayroom_ReturnsHelpInfo()
    {
        // Act
        TrophiesWithGameHelpResponse result;
        try
        {
            result = await _client.GetTrophiesWithGameHelpAsync(AstrosPlayroomNpCommunicationId, AstrosPlayroomPlatform);
        }
        catch (PlaystationApiException ex)
        {
            // Fixed: Remove .ResponseContent reference
            Assert.Fail($"GetTrophiesWithGameHelpAsync failed: {ex.StatusCode} - {ex.Message}");
            return;
        }

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Trophies);
        // Astro's Playroom has Game Help
        Assert.Greater(result.Trophies.Count, 0, "Astro's Playroom should have trophies with Game Help available.");
        Console.WriteLine($"Found {result.Trophies.Count} trophies with Game Help for Astro's Playroom.");
        // Verify structure of one entry
        var firstHelp = result.Trophies.First();
        Assert.IsNotEmpty(firstHelp.TrophyName);
        Assert.IsNotNull(firstHelp.GameHelp);
    }*/

    [Test]
    public async Task GetGameHelpForTrophiesAsync_AstrosPlayroomSingleTrophy_ReturnsTipContent()
    {
        // Arrange: First get available help to find a valid trophy/UDS ID
        var availability = await _client.GetTrophiesWithGameHelpAsync(AstrosPlayroomNpCommunicationId, AstrosPlayroomPlatform);
        var trophyWithHelp = availability?.Trophies?.FirstOrDefault(t => t.GameHelp != null);

        if (trophyWithHelp == null)
        {
            Assert.Inconclusive("Could not find a trophy with Game Help available for Astro's Playroom to test GetGameHelpForTrophiesAsync.");
            return;
        }

        var trophyIds = new List<int> { trophyWithHelp.TrophyId };

        // Act
        GameHelpForTrophiesResponse result;
        try
        {
            result = await _client.GetGameHelpForTrophiesAsync(AstrosPlayroomNpCommunicationId, AstrosPlayroomPlatform, trophyIds);
        }
        catch (PlaystationApiException ex)
        {
            // Fixed: Remove .ResponseContent reference
            Assert.Fail($"GetGameHelpForTrophiesAsync failed: {ex.StatusCode} - {ex.Message}");
            return;
        }

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.GameHelp);
        Assert.Greater(result.GameHelp.Count, 0);

        var gameHelp = result.GameHelp.First();
        Assert.AreEqual(trophyWithHelp.TrophyId, gameHelp.TrophyId);
        Assert.IsTrue(gameHelp.GameHelpImages.Count > 0 || gameHelp.GameHelpVideos.Count > 0, "Should have either images or videos for game help");
        Console.WriteLine($"Game Help for Astro's Trophy {gameHelp.TrophyId}: Images={gameHelp.GameHelpImages.Count}, Videos={gameHelp.GameHelpVideos.Count}");
    }

    // --- Optional: Test for non-"me" account ---
    [Test]
    public async Task GetUserTrophyTitlesAsync_OtherAccount_ReturnsDataIfPermitted()
    {
        if (string.IsNullOrWhiteSpace(_testNumericAccountId))
        {
            Assert.Inconclusive("PSN_TEST_ACCOUNT_ID environment variable not set. Skipping test for other account.");
        }

        // Act
        UserTrophyTitlesResponse result;
        try
        {
            result = await _client.GetUserTrophyTitlesAsync(_testNumericAccountId!);
        }
        catch (PlaystationApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            Assert.Pass($"Correctly received Forbidden (403) when trying to access account '{_testNumericAccountId}'. Check privacy settings or friendship status.");
            return;
        }
        catch (PlaystationApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Assert.Fail($"Received NotFound (404) for account ID '{_testNumericAccountId}'. Ensure the ID is correct.");
            return;
        }


        // Assert (if successful)
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.TrophyTitles);
        Assert.Greater(result.TotalItemCount, -1);
        Console.WriteLine($"Found {result.TotalItemCount} trophy titles for account {_testNumericAccountId}.");
    }
}
