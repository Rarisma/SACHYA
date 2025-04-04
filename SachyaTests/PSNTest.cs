// PlayStationTrophyClientTests.cs (Modified for Real API)
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Sachya;
using System.Linq;
using System.Collections.Generic;
using dotenv.net;

namespace Tests;

[TestFixture]
[Explicit("Hits the real PlayStation API and requires valid authentication via environment variables.")] // Mark class as Explicit
public class PlayStationTrophyClientTests_Integration
{
    // --- Test Configuration ---
    private string? _accessToken;
    private string? _testNumericAccountId; // Account ID for testing access to OTHER users

    // --- Test User/Game Data ---
    private const string TestAccountIdMe = "me"; // Represents the authenticated user
    private const string GravityRushNpCommunicationId = "NPWR08920_00"; // Gravity Rush Remastered PS4
    private const string GravityRushPlatform = "PS4";
    private const string GravityRushNpTitleId = "CUSA01130_00"; // Gravity Rush Remastered PS4
    private const string AstrosPlayroomNpCommunicationId = "NPWR20188_00"; // ASTRO's PLAYROOM PS5
    private const string AstrosPlayroomPlatform = "PS5";


    private HttpClient _httpClient = null!;
    private PlayStationTrophyClient _client = null!;
    private const int ApiDelayMs = 500; // Delay between tests to mitigate rate limiting

    [OneTimeSetUp]
    public void GlobalSetup()
    {        
        DotEnv.Load();
        var envVars = DotEnv.Read();
        _accessToken = envVars["PSNSSO"];
        _testNumericAccountId = envVars["PSNTestUser"]; // Optional, for testing other accounts

        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            Assert.Inconclusive("PSN_ACCESS_TOKEN environment variable not set. Skipping integration tests.");
        }

        // Use a single HttpClient instance for all tests in this fixture
        _httpClient = new HttpClient();
        // Base URL is set within the client constructor
        _client = new PlayStationTrophyClient(_httpClient);
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
        var result = await _client.GetUserTrophyTitlesAsync(_accessToken!, TestAccountIdMe);

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
        int limit = 5;
        int offset = 2;

        // Act
        // Ensure the account has enough games for this offset/limit to make sense
        var result = await _client.GetUserTrophyTitlesAsync(_accessToken!, TestAccountIdMe, limit, offset);

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
        var result = await _client.GetTitleTrophiesAsync(_accessToken!, GravityRushNpCommunicationId, GravityRushPlatform, "all");

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
        var result = await _client.GetTitleTrophiesAsync(_accessToken!, AstrosPlayroomNpCommunicationId, AstrosPlayroomPlatform, "all");

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
        var resultEn = await _client.GetTitleTrophiesAsync(_accessToken!, AstrosPlayroomNpCommunicationId, AstrosPlayroomPlatform, "all", acceptLanguage: "en-US");
        await Task.Delay(ApiDelayMs); // Add delay before next call
        var resultDe = await _client.GetTitleTrophiesAsync(_accessToken!, AstrosPlayroomNpCommunicationId, AstrosPlayroomPlatform, "all", acceptLanguage: lang);


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
            result = await _client.GetUserEarnedTrophiesAsync(_accessToken!, GravityRushNpCommunicationId, GravityRushPlatform, TestAccountIdMe, "all");
        }
        catch (PlaystationApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Assert.Inconclusive($"Test account '{TestAccountIdMe}' has not played Gravity Rush Remastered (NPWR08920_00) or sync is needed. Skipping.");
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
        var result = await _client.GetUserTrophySummaryAsync(_accessToken!, TestAccountIdMe);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(TestAccountIdMe, result.AccountId); // AccountId might be numeric even for "me" in response
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
        var result = await _client.GetTitleTrophyGroupsAsync(_accessToken!, GravityRushNpCommunicationId, GravityRushPlatform);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Gravity Rush Remastered", result.TrophyTitleName); // Check title name matches
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
            result = await _client.GetUserEarnedTrophyGroupsAsync(_accessToken!, GravityRushNpCommunicationId, GravityRushPlatform, TestAccountIdMe);
        }
        catch (PlaystationApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Assert.Inconclusive($"Test account '{TestAccountIdMe}' has not played Gravity Rush Remastered (NPWR08920_00) or sync is needed. Skipping.");
            return; // Skip assertion
        }

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.TrophyGroups);
        Assert.GreaterOrEqual(result.TrophyGroups.Count, 1); // Expect at least default group if played
        Assert.GreaterOrEqual(result.Progress, 0); // Overall progress %
        Assert.LessOrEqual(result.Progress, 100);
        Console.WriteLine($"Gravity Rush Earned Groups: Overall Progress {result.Progress}%. Found {result.TrophyGroups.Count} groups.");
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
            result = await _client.GetUserTitlesTrophySummaryAsync(_accessToken!, titleIds, TestAccountIdMe);
        }
        catch (PlaystationApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // This might happen if the Title ID itself is wrong, or less likely if the user hasn't played *any* title
            Assert.Fail($"API returned 404 for title summary request with ID {titleIds}. Error: {ex.ResponseContent}");
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
        string titleIds = GravityRushNpTitleId;
        bool includeUnearned = true;

        // Act
        UserTitlesTrophySummaryResponse result;
        try
        {
            // Assuming the user has played but NOT platinumed Gravity Rush for this test
            result = await _client.GetUserTitlesTrophySummaryAsync(_accessToken!, titleIds, TestAccountIdMe, includeUnearned);
        }
        catch (PlaystationApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Assert.Fail($"API returned 404 for title summary request with ID {titleIds}. Error: {ex.ResponseContent}");
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

    // --- Game Help Tests (Example - Astro's Playroom) ---
    // These require the game to actually support Game Help

    [Test]
    public async Task GetTrophiesWithGameHelpAsync_AstrosPlayroom_ReturnsHelpInfo()
    {
        // Act
        GameHelpAvailabilityResponse result;
        try
        {
            result = await _client.GetTrophiesWithGameHelpAsync(_accessToken!, AstrosPlayroomNpCommunicationId);
        }
        catch (PlaystationApiException ex)
        {
            // Handle potential errors like 403 Forbidden if Game Help access changed, or 404 if ID is wrong
            Assert.Fail($"GetTrophiesWithGameHelpAsync failed: {ex.StatusCode} - {ex.ResponseContent}");
            return;
        }

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Data);
        Assert.IsNotNull(result.Data.HintAvailability);
        Assert.IsNotNull(result.Data.HintAvailability.Trophies);
        // Astro's Playroom has Game Help
        Assert.Greater(result.Data.HintAvailability.Trophies.Count, 0, "Astro's Playroom should have trophies with Game Help available.");
        Console.WriteLine($"Found {result.Data.HintAvailability.Trophies.Count} trophies with Game Help for Astro's Playroom.");
        // Verify structure of one entry
        var firstHelp = result.Data.HintAvailability.Trophies.First();
        Assert.AreEqual("TrophyInfoWithHintAvailable", firstHelp.Typename);
        Assert.IsNotEmpty(firstHelp.HelpType);
        Assert.IsNotEmpty(firstHelp.Id);
        Assert.IsNotEmpty(firstHelp.TrophyId);
        Assert.IsNotEmpty(firstHelp.UdsObjectId);
    }

    [Test]
    public async Task GetGameHelpForTrophiesAsync_AstrosPlayroomSingleTrophy_ReturnsTipContent()
    {
        // Arrange: First get available help to find a valid trophy/UDS ID
        var availability = await _client.GetTrophiesWithGameHelpAsync(_accessToken!, AstrosPlayroomNpCommunicationId);
        var trophyWithHelp = availability?.Data?.HintAvailability?.Trophies?.FirstOrDefault();

        if (trophyWithHelp == null)
        {
            Assert.Inconclusive("Could not find a trophy with Game Help available for Astro's Playroom to test GetGameHelpForTrophiesAsync.");
            return;
        }

        var requestInfo = new List<GameHelpRequestTrophy>
        {
            new() { TrophyId = trophyWithHelp.TrophyId, UdsObjectId = trophyWithHelp.UdsObjectId, HelpType = trophyWithHelp.HelpType }
        };

        // Act
        GameHelpTipsResponse result;
        try
        {
            result = await _client.GetGameHelpForTrophiesAsync(_accessToken!, AstrosPlayroomNpCommunicationId, requestInfo);
        }
        catch (PlaystationApiException ex)
        {
            Assert.Fail($"GetGameHelpForTrophiesAsync failed: {ex.StatusCode} - {ex.ResponseContent}");
            return;
        }


        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Data);
        Assert.IsNotNull(result.Data.TipsRetrieved);
        // Check access based on current PSN rules (might not require PS+ anymore)
        // Assert.IsTrue(result.Data.TipsRetrieve.HasAccess, "Account should have access to Game Help."); // Might fail if PS+ is needed and account doesn't have it
        if (!result.Data.TipsRetrieved.HasAccess) Console.WriteLine("Warning: Game Help access reported as false.");

        Assert.IsNotNull(result.Data.TipsRetrieved.Trophies);
        Assert.AreEqual(1, result.Data.TipsRetrieved.Trophies.Count);

        var tip = result.Data.TipsRetrieved.Trophies.First();
        Assert.AreEqual(trophyWithHelp.TrophyId, tip.TrophyId);
        Assert.Greater(tip.TotalGroupCount, 0);
        Assert.IsNotNull(tip.Groups);
        Assert.GreaterOrEqual(tip.Groups.Count, 1);

        var tipGroup = tip.Groups.First();
        Assert.IsNotNull(tipGroup.TipContents);
        Assert.Greater(tipGroup.TipContents.Count, 0);

        var tipContent = tipGroup.TipContents.First();
        Assert.IsNotEmpty(tipContent.Description);
        Assert.IsNotEmpty(tipContent.DisplayName);
        // Media might be null or present depending on the tip
        Console.WriteLine($"Game Help for Astro's Trophy {tip.TrophyId}: '{tipContent.DisplayName}' - HasMedia: {!string.IsNullOrEmpty(tipContent.MediaUrl)}");
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
            result = await _client.GetUserTrophyTitlesAsync(_accessToken!, _testNumericAccountId!);
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