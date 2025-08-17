using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Sachya.PSN;
using dotenv.net;

namespace Tests;

[TestFixture]
[Explicit("Requires valid PSN credentials via environment variables.")]
public class PSNAuthenticationTests
{
    private string? _psnUsername;
    private string? _psnPassword;
    
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        DotEnv.Load();
        var envVars = DotEnv.Read();
        
        _psnUsername = envVars.ContainsKey("PSNUsername") ? envVars["PSNUsername"] : Environment.GetEnvironmentVariable("PSNUsername");
        _psnPassword = envVars.ContainsKey("PSNPassword") ? envVars["PSNPassword"] : Environment.GetEnvironmentVariable("PSNPassword");
        
        if (string.IsNullOrEmpty(_psnUsername) || string.IsNullOrEmpty(_psnPassword))
        {
            Assert.Ignore("PSNUsername and/or PSNPassword not found in .env file or environment variables. Skipping PSN authentication tests.");
        }
    }
    
    [Test]
    public async Task GetNpssoFromCredentials_ValidCredentials_ReturnsNpssoToken()
    {
        // Act
        var npssoToken = await PSNClient.GetNpssoFromCredentials(_psnUsername!, _psnPassword!);
        
        // Assert
        Assert.IsNotNull(npssoToken);
        Assert.IsNotEmpty(npssoToken);
        Console.WriteLine($"Successfully obtained NPSSO token (length: {npssoToken.Length})");
    }
    
    [Test]
    public async Task GetNpssoFromCredentials_InvalidCredentials_ThrowsException()
    {
        // Arrange
        var invalidUsername = "invalid@example.com";
        var invalidPassword = "wrongpassword";
        
        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await PSNClient.GetNpssoFromCredentials(invalidUsername, invalidPassword);
        });
    }
    
    [Test]
    public async Task CreateFromCredentials_ValidCredentials_CreatesClientSuccessfully()
    {
        // Act
        var client = await PSNClient.CreateFromNpsso(
            await PSNClient.GetNpssoFromCredentials(_psnUsername!, _psnPassword!)
        );
        
        // Assert
        Assert.IsNotNull(client);
        
        // Test that the client works by making a simple API call
        var trophySummary = await client.GetUserTrophySummaryAsync("me");
        Assert.IsNotNull(trophySummary);
        Assert.GreaterOrEqual(trophySummary.TrophyLevel, 0);
        Console.WriteLine($"Successfully authenticated. Trophy Level: {trophySummary.TrophyLevel}");
    }
    
    [Test]
    public async Task FullAuthenticationFlow_ValidCredentials_CanAccessTrophyData()
    {
        // Step 1: Get NPSSO token from credentials
        var npssoToken = await PSNClient.GetNpssoFromCredentials(_psnUsername!, _psnPassword!);
        Assert.IsNotNull(npssoToken);
        
        // Step 2: Create client from NPSSO token
        var client = await PSNClient.CreateFromNpsso(npssoToken);
        Assert.IsNotNull(client);
        
        // Step 3: Get user's trophy titles
        var trophyTitles = await client.GetUserTrophyTitlesAsync("me", limit: 10);
        Assert.IsNotNull(trophyTitles);
        Assert.IsNotNull(trophyTitles.TrophyTitles);
        
        Console.WriteLine($"Found {trophyTitles.TotalItemCount} trophy titles");
        if (trophyTitles.TrophyTitles.Count > 0)
        {
            var firstTitle = trophyTitles.TrophyTitles[0];
            Console.WriteLine($"First title: {firstTitle.TrophyTitleName} (Platform: {firstTitle.TrophyTitlePlatform})");
        }
    }
}