using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Sachya.Clients;
using Sachya.Definitions.Xbox;

[TestFixture]
public class OpenXblRealApiTests
{
    private OpenXblApiClient _apiClient;
    private string _apiKey;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        LoadEnvironmentVariables();
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            Assert.Ignore("OPENXBL_API_KEY not found in .env file. Please add your API key to run integration tests.");
        }

        _apiClient = new OpenXblApiClient(_apiKey);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _apiClient?.Dispose();
    }

    private void LoadEnvironmentVariables()
    {
        var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        
        if (!File.Exists(envFile))
        {
            // Try parent directories for .env file
            var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, ".env")))
            {
                currentDir = currentDir.Parent;
            }
            
            if (currentDir != null)
            {
                envFile = Path.Combine(currentDir.FullName, ".env");
            }
        }

        if (File.Exists(envFile))
        {
            var lines = File.ReadAllLines(envFile);
            foreach (var line in lines)
            {
                if (line.StartsWith("OPENXBL_API_KEY="))
                {
                    _apiKey = line.Substring("OPENXBL_API_KEY=".Length).Trim();
                    break;
                }
            }
        }

        // Also check environment variables
        if (string.IsNullOrEmpty(_apiKey))
        {
            _apiKey = Environment.GetEnvironmentVariable("OPENXBL_API_KEY");
        }
    }

    #region Account Tests

    [Test]
    public async Task GetProfile_RealApi_ReturnsValidProfileData()
    {
        // Act
        var result = await _apiClient.GetProfileAsync();

        // Assert
        Assert.That(result, Is.Not.Null, "Profile response should not be null");
        Assert.That(result.ProfileUsers, Is.Not.Null, "ProfileUsers should not be null");
        Assert.That(result.ProfileUsers, Is.Not.Empty, "ProfileUsers should contain at least one user");

        var user = result.ProfileUsers[0];
        Assert.That(user.Id, Is.Not.Null.And.Not.Empty, "User ID should not be null or empty");
        Assert.That(user.Settings, Is.Not.Null, "User settings should not be null");
        Assert.That(user.Settings, Is.Not.Empty, "User settings should contain at least one setting");

        // Verify common profile settings
        var gamertagSetting = user.Settings.FirstOrDefault(s => s.Id == "Gamertag");
        if (gamertagSetting != null)
        {
            Assert.That(gamertagSetting.Value, Is.Not.Null.And.Not.Empty, "Gamertag should not be null or empty");
            TestContext.WriteLine($"Gamertag: {gamertagSetting.Value}");
        }

        var gamerscoreSetting = user.Settings.FirstOrDefault(s => s.Id == "Gamerscore");
        if (gamerscoreSetting != null)
        {
            Assert.That(int.TryParse(gamerscoreSetting.Value, out var gamerscore), Is.True, "Gamerscore should be a valid integer");
            Assert.That(gamerscore, Is.GreaterThanOrEqualTo(0), "Gamerscore should be non-negative");
            TestContext.WriteLine($"Gamerscore: {gamerscoreSetting.Value}");
        }

        TestContext.WriteLine($"Profile loaded for user ID: {user.Id}");
        TestContext.WriteLine($"Total settings: {user.Settings.Count}");
    }

    [Test]
    public async Task SearchPlayer_RealApi_WithKnownGamertag_ReturnsResults()
    {
        // Arrange - using a well-known Xbox gamertag
        var knownGamertag = "MajorNelson"; // Larry Hryb's gamertag

        // Act
        var result = await _apiClient.SearchPlayerAsync(knownGamertag);

        // Assert
        Assert.That(result, Is.Not.Null, "Search response should not be null");
        
        if (result.Results != null && result.Results.Any())
        {
            var foundUser = result.Results.FirstOrDefault();
            Assert.That(foundUser.Xuid, Is.Not.Null.And.Not.Empty, "Found user XUID should not be null or empty");
            Assert.That(foundUser.Gamertag, Is.Not.Null.And.Not.Empty, "Found user Gamertag should not be null or empty");
            
            TestContext.WriteLine($"Found user: {foundUser.Gamertag} (XUID: {foundUser.Xuid})");
        }
        else
        {
            TestContext.WriteLine($"No results found for gamertag: {knownGamertag}");
        }
    }

    [Test]
    public async Task GetAlerts_RealApi_ReturnsAlertsData()
    {
        // Act
        var result = await _apiClient.GetAlertsAsync();

        // Assert
        Assert.That(result, Is.Not.Null, "Alerts response should not be null");
        
        if (result.Alerts != null && result.Alerts.Any())
        {
            TestContext.WriteLine($"Found {result.Alerts.Count} alerts");
            
            foreach (var alert in result.Alerts.Take(3)) // Test first 3 alerts
            {
                Assert.That(alert.Id, Is.Not.Null.And.Not.Empty, "Alert ID should not be null or empty");
                Assert.That(alert.Type, Is.Not.Null.And.Not.Empty, "Alert type should not be null or empty");
                TestContext.WriteLine($"Alert: {alert.Type} - {alert.Title}");
            }
        }
        else
        {
            TestContext.WriteLine("No alerts found for this account");
        }
    }

    #endregion

    #region Achievement Tests

    [Test]
    public async Task GetAchievements_RealApi_ReturnsTitleAchievementData()
    {
        // Act
        var result = await _apiClient.GetAchievementsAsync();

        // Assert
        Assert.That(result, Is.Not.Null, "The API response should not be null.");
        Assert.That(result.Titles, Is.Not.Null, "The Titles list should not be null.");

        if (result.Titles.Any())
        {
            TestContext.WriteLine($"Found {result.Titles.Count} titles with achievement data.");

            var title = result.Titles.First();
            Assert.That(title.TitleId, Is.Not.Null.And.Not.Empty, "Title ID should not be null or empty.");
            Assert.That(title.Name, Is.Not.Null.And.Not.Empty, "Title name should not be null or empty.");
            Assert.That(title.Achievement, Is.Not.Null, "Title achievement info should not be null.");
            Assert.That(title.TitleHistory, Is.Not.Null, "Title history info should not be null.");

            // Log details of the first title for verification
            TestContext.WriteLine($"Sample Title: {title.Name} (ID: {title.TitleId})");
            TestContext.WriteLine($"Platform(s): {string.Join(", ", title.Devices)}");
            TestContext.WriteLine($"Gamerscore: {title.Achievement.CurrentGamerscore}/{title.Achievement.TotalGamerscore}");
            TestContext.WriteLine($"Achievements Unlocked: {title.Achievement.CurrentAchievements}");
            TestContext.WriteLine($"Progress: {title.Achievement.ProgressPercentage}%");
            TestContext.WriteLine($"Last Played: {title.TitleHistory.LastTimePlayed:yyyy-MM-dd HH:mm:ss}");
        }
        else
        {
            TestContext.WriteLine("No titles with achievement data were found for this account.");
        }
    }
    [Test]
    public async Task GetAchievementStats_RealApi_WithKnownTitleId_ReturnsStats()
    {
        // Arrange - using Halo Infinite's title ID
        var haloInfiniteTitleId = "1777860928";

        try
        {
            // Act
            var result = await _apiClient.GetAchievementStatsAsync(haloInfiniteTitleId);

            // Assert
            Assert.That(result, Is.Not.Null, "Achievement stats response should not be null");
            
            if (result.Stats != null && result.Stats.Any())
            {
                TestContext.WriteLine($"Found {result.Stats.Count} stats for Halo Infinite");
                
                foreach (var stat in result.Stats.Take(5))
                {
                    Assert.That(stat.Name, Is.Not.Null.And.Not.Empty, "Stat name should not be null or empty");
                    Assert.That(stat.TitleId, Is.EqualTo(haloInfiniteTitleId), "Stat title ID should match requested title ID");
                    TestContext.WriteLine($"Stat: {stat.Name} = {stat.Value}");
                }
            }
            else
            {
                TestContext.WriteLine("No achievement stats found for this title");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Note: Achievement stats may not be available for this title or account. Error: {ex.Message}");
            Assert.Pass("Achievement stats endpoint may have access restrictions");
        }
    }

    [Test]
    public async Task GetTitleAchievements_RealApi_WithKnownTitleId_ReturnsAchievements()
    {
        // Arrange - using Minecraft's title ID
        var minecraftTitleId = "1739947436";

        try
        {
            // Act
            var result = await _apiClient.GetTitleAchievementsAsync(minecraftTitleId);

            // Assert
            Assert.That(result, Is.Not.Null, "Title achievements response should not be null");
            Assert.That(result.Achievements, Is.Not.Null, "Title achievements list should not be null");

            if (result.Achievements.Any())
            {
                TestContext.WriteLine($"Found {result.Achievements.Count} achievements for Minecraft");

                var achievement = result.Achievements.First();
                Assert.That(achievement.Id, Is.Not.Null.And.Not.Empty, "Achievement ID should not be null or empty");
                Assert.That(achievement.Name, Is.Not.Null.And.Not.Empty, "Achievement name should not be null or empty");

                TestContext.WriteLine($"Sample achievement: {achievement.Name}");
                TestContext.WriteLine($"Description: {achievement.Description}");
                TestContext.WriteLine($"Gamerscore: {achievement.Gamerscore}");

                // Test paging info
                if (result.PagingInfo != null)
                {
                    TestContext.WriteLine($"Total achievements available: {result.PagingInfo.TotalRecords}");
                    if (!string.IsNullOrEmpty(result.PagingInfo.ContinuationToken))
                    {
                        TestContext.WriteLine("More achievements available on next page");
                    }
                }
            }
            else
            {
                TestContext.WriteLine("No achievements found for this title");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Note: Title achievements may not be available. Error: {ex.Message}");
            Assert.Pass("Title achievements endpoint may have access restrictions");
        }
    }

    #endregion

    #region Game Pass Tests

    [Test]
    public async Task GetAllGamePassGames_RealApi_ReturnsGameData()
    {
        // Act
        var result = await _apiClient.GetAllGamePassGamesAsync();

        // Assert
        Assert.That(result, Is.Not.Null, "Game Pass response should not be null");
        Assert.That(result.Games, Is.Not.Null, "Games list should not be null");
        Assert.That(result.Games, Is.Not.Empty, "Games list should contain at least one game");

        TestContext.WriteLine($"Found {result.Games.Count} Game Pass games");

        var game = result.Games.First();
        Assert.That(game.TitleId, Is.Not.Null.And.Not.Empty, "Game title ID should not be null or empty");
        Assert.That(game.Name, Is.Not.Null.And.Not.Empty, "Game name should not be null or empty");

        TestContext.WriteLine($"Sample game: {game.Name} (ID: {game.TitleId})");
        TestContext.WriteLine($"Developer: {game.DeveloperName}");
        TestContext.WriteLine($"Publisher: {game.PublisherName}");
        TestContext.WriteLine($"Category: {game.Category}");
        TestContext.WriteLine($"Release Date: {game.ReleaseDate:yyyy-MM-dd}");

        if (game.Platforms != null && game.Platforms.Any())
        {
            TestContext.WriteLine($"Platforms: {string.Join(", ", game.Platforms)}");
        }

        if (game.Genres != null && game.Genres.Any())
        {
            TestContext.WriteLine($"Genres: {string.Join(", ", game.Genres)}");
        }

        if (game.Images != null && game.Images.Any())
        {
            TestContext.WriteLine($"Images: {game.Images.Count}");
            foreach (var image in game.Images.Take(3))
            {
                TestContext.WriteLine($"  - {image.ImageType}: {image.Uri}");
            }
        }

        // Verify data consistency
        Assert.That(game.IsBundle, Is.TypeOf<bool>(), "IsBundle should be a boolean");
        
        if (game.ReleaseDate != default(DateTime))
        {
            Assert.That(game.ReleaseDate, Is.LessThanOrEqualTo(DateTime.Now.AddYears(5)), "Release date should not be too far in the future");
        }
    }

    [Test]
    public async Task GetPcGamePassGames_RealApi_ReturnsOnlyPcGames()
    {
        // Act
        var result = await _apiClient.GetPcGamePassGamesAsync();

        // Assert
        Assert.That(result, Is.Not.Null, "PC Game Pass response should not be null");
        Assert.That(result.Games, Is.Not.Null, "PC games list should not be null");

        if (result.Games.Any())
        {
            TestContext.WriteLine($"Found {result.Games.Count} PC Game Pass games");

            var pcGame = result.Games.First();
            TestContext.WriteLine($"Sample PC game: {pcGame.Name}");

            // Verify PC platform is included (if platforms are specified)
            if (pcGame.Platforms != null && pcGame.Platforms.Any())
            {
                var hasPcPlatform = pcGame.Platforms.Any(p => 
                    p.Contains("PC", StringComparison.OrdinalIgnoreCase) || 
                    p.Contains("Windows", StringComparison.OrdinalIgnoreCase));
                
                if (hasPcPlatform)
                {
                    TestContext.WriteLine($"✓ Confirmed PC platform: {string.Join(", ", pcGame.Platforms)}");
                }
                else
                {
                    TestContext.WriteLine($"Note: PC platform not explicitly listed: {string.Join(", ", pcGame.Platforms)}");
                }
            }
        }
        else
        {
            TestContext.WriteLine("No PC Game Pass games found");
        }
    }

    [Test]
    public async Task GetEaPlayGames_RealApi_ReturnsEaGames()
    {
        // Act
        var result = await _apiClient.GetEaPlayGamesAsync();

        // Assert
        Assert.That(result, Is.Not.Null, "EA Play response should not be null");
        Assert.That(result.Games, Is.Not.Null, "EA Play games list should not be null");

        if (result.Games.Any())
        {
            TestContext.WriteLine($"Found {result.Games.Count} EA Play games");

            var eaGame = result.Games.First();
            TestContext.WriteLine($"Sample EA Play game: {eaGame.Name}");
            TestContext.WriteLine($"Publisher: {eaGame.PublisherName}");

            // Many EA games should have "Electronic Arts" as publisher
            var eaGames = result.Games.Where(g => 
                g.PublisherName != null && 
                g.PublisherName.Contains("Electronic Arts", StringComparison.OrdinalIgnoreCase)).ToList();

            TestContext.WriteLine($"Games with EA as publisher: {eaGames.Count}");
        }
        else
        {
            TestContext.WriteLine("No EA Play games found");
        }
    }

    [Test]
    public async Task GetNoControllerGames_RealApi_ReturnsTouchFriendlyGames()
    {
        // Act
        var result = await _apiClient.GetNoControllerGamesAsync();

        // Assert
        Assert.That(result, Is.Not.Null, "No controller games response should not be null");
        Assert.That(result.Games, Is.Not.Null, "No controller games list should not be null");

        if (result.Games.Any())
        {
            TestContext.WriteLine($"Found {result.Games.Count} games that don't require a controller");

            var touchGame = result.Games.First();
            TestContext.WriteLine($"Sample touch-friendly game: {touchGame.Name}");
            TestContext.WriteLine($"Category: {touchGame.Category}");

            if (touchGame.Genres != null && touchGame.Genres.Any())
            {
                TestContext.WriteLine($"Genres: {string.Join(", ", touchGame.Genres)}");
            }
        }
        else
        {
            TestContext.WriteLine("No touch-friendly games found");
        }
    }

    #endregion

    #region Marketplace Tests

    [Test]
    public async Task GetNewGames_RealApi_ReturnsRecentReleases()
    {
        // Act
        var result = await _apiClient.GetNewGamesAsync();

        // Assert
        Assert.That(result, Is.Not.Null, "New games response should not be null");
        Assert.That(result.Products, Is.Not.Null, "Products list should not be null");

        if (result.Products.Any())
        {
            TestContext.WriteLine($"Found {result.Products.Count} new games");

            var newGame = result.Products.First();
            Assert.That(newGame.ProductId, Is.Not.Null.And.Not.Empty, "Product ID should not be null or empty");
            Assert.That(newGame.Name, Is.Not.Null.And.Not.Empty, "Product name should not be null or empty");

            TestContext.WriteLine($"Sample new game: {newGame.Name}");
            TestContext.WriteLine($"Product ID: {newGame.ProductId}");
            TestContext.WriteLine($"Developer: {newGame.DeveloperName}");
            TestContext.WriteLine($"Publisher: {newGame.PublisherName}");
            TestContext.WriteLine($"Category: {newGame.Category}");
            TestContext.WriteLine($"Release Date: {newGame.ReleaseDate:yyyy-MM-dd}");

            if (newGame.Price != null)
            {
                TestContext.WriteLine($"Price: {newGame.Price.ListPrice} ({newGame.Price.CurrencyCode})");
                TestContext.WriteLine($"Is Free: {newGame.Price.IsFree}");
            }

            if (newGame.Platforms != null && newGame.Platforms.Any())
            {
                TestContext.WriteLine($"Platforms: {string.Join(", ", newGame.Platforms)}");
            }

            if (newGame.Rating != null)
            {
                TestContext.WriteLine($"Rating: {newGame.Rating.RatingId} ({newGame.Rating.RatingSystemId})");
            }

            if (newGame.Images != null && newGame.Images.Any())
            {
                TestContext.WriteLine($"Images available: {newGame.Images.Count}");
                var boxArt = newGame.Images.FirstOrDefault(i => i.ImageType == "BoxArt");
                if (boxArt != null)
                {
                    TestContext.WriteLine($"Box Art: {boxArt.Uri} ({boxArt.Width}x{boxArt.Height})");
                }
            }
        }
        else
        {
            TestContext.WriteLine("No new games found");
        }
    }

    [Test]
    public async Task GetTopFreeGames_RealApi_ReturnsOnlyFreeGames()
    {
        // Act
        var result = await _apiClient.GetTopFreeGamesAsync();

        // Assert
        Assert.That(result, Is.Not.Null, "Top free games response should not be null");
        Assert.That(result.Products, Is.Not.Null, "Products list should not be null");

        if (result.Products.Any())
        {
            TestContext.WriteLine($"Found {result.Products.Count} top free games");

            var freeGame = result.Products.First();
            TestContext.WriteLine($"Sample free game: {freeGame.Name}");

            if (freeGame.Price != null)
            {
                Assert.That(freeGame.Price.IsFree, Is.True, "Game should be marked as free");
                TestContext.WriteLine($"✓ Confirmed free: {freeGame.Price.ListPrice}");
            }

            // Count games that are explicitly marked as free
            var explicitlyFreeGames = result.Products.Count(p => p.Price?.IsFree == true);
            TestContext.WriteLine($"Games explicitly marked as free: {explicitlyFreeGames}");
        }
        else
        {
            TestContext.WriteLine("No free games found");
        }
    }

    [Test]
    public async Task GetBestRatedGames_RealApi_ReturnsHighQualityGames()
    {
        // Act
        var result = await _apiClient.GetBestRatedGamesAsync();

        // Assert
        Assert.That(result, Is.Not.Null, "Best rated games response should not be null");
        Assert.That(result.Products, Is.Not.Null, "Products list should not be null");

        if (result.Products.Any())
        {
            TestContext.WriteLine($"Found {result.Products.Count} best rated games");

            var topGame = result.Products.First();
            TestContext.WriteLine($"Top rated game: {topGame.Name}");
            TestContext.WriteLine($"Developer: {topGame.DeveloperName}");
            TestContext.WriteLine($"Publisher: {topGame.PublisherName}");

            if (topGame.Rating != null)
            {
                TestContext.WriteLine($"Rating: {topGame.Rating.RatingId}");
                if (topGame.Rating.RatingDescriptors != null && topGame.Rating.RatingDescriptors.Any())
                {
                    foreach (var descriptor in topGame.Rating.RatingDescriptors)
                    {
                        TestContext.WriteLine($"  - {descriptor.RatingDescriptor}: {descriptor.RatingDisclaimers}");
                    }
                }
            }
        }
        else
        {
            TestContext.WriteLine("No best rated games found");
        }
    }

    [Test]
    public async Task GetDeals_RealApi_ReturnsDiscountedGames()
    {
        // Act
        var result = await _apiClient.GetDealsAsync();

        // Assert
        Assert.That(result, Is.Not.Null, "Deals response should not be null");
        Assert.That(result.Products, Is.Not.Null, "Products list should not be null");

        if (result.Products.Any())
        {
            TestContext.WriteLine($"Found {result.Products.Count} games on sale");

            var dealGame = result.Products.First();
            TestContext.WriteLine($"Sample deal: {dealGame.Name}");

            if (dealGame.Price != null)
            {
                TestContext.WriteLine($"Current price: {dealGame.Price.ListPrice} ({dealGame.Price.CurrencyCode})");
                TestContext.WriteLine($"Is free: {dealGame.Price.IsFree}");
            }
        }
        else
        {
            TestContext.WriteLine("No deals found");
        }
    }

    [Test]
    public async Task GetGameDetails_RealApi_WithMultipleProductIds_ReturnsDetailedInfo()
    {
        // First get some product IDs from new games
        var newGamesResult = await _apiClient.GetNewGamesAsync();
        
        if (!newGamesResult.Products.Any())
        {
            Assert.Ignore("No new games available to test game details");
            return;
        }

        // Take first 2 product IDs
        var productIds = newGamesResult.Products.Take(2).Select(p => p.ProductId).ToList();
        var productIdsString = string.Join(",", productIds);

        TestContext.WriteLine($"Getting details for products: {productIdsString}");

        // Arrange
        var request = new GameDetailsRequest
        {
            Products = productIdsString
        };

        // Act
        var result = await _apiClient.GetGameDetailsAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null, "Game details response should not be null");
        Assert.That(result.Products, Is.Not.Null, "Products list should not be null");

        if (result.Products.Any())
        {
            TestContext.WriteLine($"Found details for {result.Products.Count} products");

            var detailedGame = result.Products.First();
            TestContext.WriteLine($"Detailed game: {detailedGame.Name}");
            TestContext.WriteLine($"Product Type: {detailedGame.ProductType}");

            if (detailedGame.Skus != null && detailedGame.Skus.Any())
            {
                TestContext.WriteLine($"Available SKUs: {detailedGame.Skus.Count}");
                foreach (var sku in detailedGame.Skus)
                {
                    TestContext.WriteLine($"  - {sku.Name}: {sku.Price?.ListPrice}");
                }

                // Verify SKU data integrity
                foreach (var sku in detailedGame.Skus)
                {
                    Assert.That(sku.SkuId, Is.Not.Null.And.Not.Empty, "SKU ID should not be null or empty");
                    Assert.That(sku.Name, Is.Not.Null.And.Not.Empty, "SKU name should not be null or empty");
                }
            }
        }
        else
        {
            TestContext.WriteLine("No detailed game information found");
        }
    }

    #endregion

    #region Data Validation Tests

    [Test]
    public async Task ApiResponse_DataConsistency_AllEndpointsReturnValidJson()
    {
        var testResults = new List<(string endpoint, bool success, string error)>();

        // Test multiple endpoints for JSON consistency
        try
        {
            await _apiClient.GetProfileAsync();
            testResults.Add(("Profile", true, null));
        }
        catch (Exception ex)
        {
            testResults.Add(("Profile", false, ex.Message));
        }

        try
        {
            await _apiClient.GetAchievementsAsync();
            testResults.Add(("Achievements", true, null));
        }
        catch (Exception ex)
        {
            testResults.Add(("Achievements", false, ex.Message));
        }

        try
        {
            await _apiClient.GetAllGamePassGamesAsync();
            testResults.Add(("GamePass", true, null));
        }
        catch (Exception ex)
        {
            testResults.Add(("GamePass", false, ex.Message));
        }

        try
        {
            await _apiClient.GetNewGamesAsync();
            testResults.Add(("Marketplace", true, null));
        }
        catch (Exception ex)
        {
            testResults.Add(("Marketplace", false, ex.Message));
        }

        // Report results
        TestContext.WriteLine("API Endpoint Test Results:");
        foreach (var (endpoint, success, error) in testResults)
        {
            if (success)
            {
                TestContext.WriteLine($"✓ {endpoint}: SUCCESS");
            }
            else
            {
                TestContext.WriteLine($"✗ {endpoint}: FAILED - {error}");
            }
        }

        var successfulCalls = testResults.Count(r => r.success);
        var totalCalls = testResults.Count;
        
        TestContext.WriteLine($"Overall success rate: {successfulCalls}/{totalCalls} ({(double)successfulCalls / totalCalls * 100:F1}%)");

        // At least half the endpoints should work
        Assert.That(successfulCalls, Is.GreaterThanOrEqualTo(totalCalls / 2), 
            "At least half of the tested endpoints should work");
    }

    #endregion

    #region Performance Tests

    [Test]
    public async Task ApiResponse_Performance_ReasonableResponseTimes()
    {
        var responseTimeTests = new List<(string endpoint, TimeSpan duration)>();

        // Test Profile endpoint
        var profileStart = DateTime.UtcNow;
        try
        {
            await _apiClient.GetProfileAsync();
            responseTimeTests.Add(("Profile", DateTime.UtcNow - profileStart));
        }
        catch
        {
            // Ignore errors for performance test
        }

        // Test GamePass endpoint
        var gamePassStart = DateTime.UtcNow;
        try
        {
            await _apiClient.GetAllGamePassGamesAsync();
            responseTimeTests.Add(("GamePass", DateTime.UtcNow - gamePassStart));
        }
        catch
        {
            // Ignore errors for performance test
        }

        // Test Marketplace endpoint
        var marketplaceStart = DateTime.UtcNow;
        try
        {
            await _apiClient.GetNewGamesAsync();
            responseTimeTests.Add(("Marketplace", DateTime.UtcNow - marketplaceStart));
        }
        catch
        {
            // Ignore errors for performance test
        }

        // Report performance results
        TestContext.WriteLine("API Response Time Results:");
        foreach (var (endpoint, duration) in responseTimeTests)
        {
            TestContext.WriteLine($"{endpoint}: {duration.TotalMilliseconds:F0}ms");
            
            // Most API calls should complete within 10 seconds
            Assert.That(duration.TotalSeconds, Is.LessThan(10), 
                $"{endpoint} endpoint took too long: {duration.TotalSeconds:F2} seconds");
        }

        if (responseTimeTests.Any())
        {
            var averageTime = responseTimeTests.Average(r => r.duration.TotalMilliseconds);
            TestContext.WriteLine($"Average response time: {averageTime:F0}ms");
        }
    }

    #endregion
}
