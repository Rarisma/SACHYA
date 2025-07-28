using System.Text.Json.Serialization;
using Sachya.Misc;

namespace Sachya.Definitions.Steam;

/// <summary>
/// Response from the Steam ResolveVanityURL API
/// </summary>
public class VanityUrlResponse
{
    [JsonPropertyName("response")]
    public VanityUrlResult? response { get; set; }

    public class VanityUrlResult
    {
        [JsonPropertyName("success")]
        public int success { get; set; }

        [JsonPropertyName("steamid")]
        public string? steamid { get; set; }

        [JsonPropertyName("message")]
        public string? message { get; set; }
    }
}
// GetNewsForApp
public class NewsForAppResult
{
    public AppNews appnews { get; set; }
}

public class AppNews
{
    public int appid { get; set; }
    public int count { get; set; }
    public List<NewsItem> newsitems { get; set; }
}

public class NewsItem
{
    public string gid { get; set; }
    public string title { get; set; }
    public string url { get; set; }
    public bool is_external_url { get; set; }
    public string author { get; set; }
    public string contents { get; set; }
    public string feedlabel { get; set; }
    public long date { get; set; }
    public string feedname { get; set; }
    public int feed_type { get; set; }
    public int appid { get; set; }
    public List<string> tags { get; set; }
}
// GetGlobalAchievementPercentagesForApp
public class GlobalAchievementPercentagesResult
{
    public AchievementPercentages achievementpercentages { get; set; }
}


// GetPlayerSummaries
public class PlayerSummariesResult
{
    public PlayerSummariesResponse response { get; set; }
}

public class PlayerSummariesResponse
{
    public List<Player> players { get; set; }
}

public class Player
{
    public string steamid { get; set; }
    public string personaname { get; set; }
    public string profileurl { get; set; }
    public string avatar { get; set; }
    public string avatarmedium { get; set; }
    public string avatarfull { get; set; }
    public int personastate { get; set; }
    public int communityvisibilitystate { get; set; }
    public int profilestate { get; set; }
    public long lastlogoff { get; set; }
    public int commentpermission { get; set; }
    // Private fields (if available)
    public string realname { get; set; }
    public string primaryclanid { get; set; }
    public long timecreated { get; set; }
    public int gameid { get; set; }
    public string gameserverip { get; set; }
    public string gameextrainfo { get; set; }
    public int cityid { get; set; }
    public string loccountrycode { get; set; }
    public string locstatecode { get; set; }
    public int loccityid { get; set; }
}

// GetFriendList
public class FriendListResult
{
    public FriendListResponse friendslist { get; set; }
}

public class FriendListResponse
{
    public List<Friend> friends { get; set; }
}

public class Friend
{
    public string steamid { get; set; }
    public string relationship { get; set; }
    public long friend_since { get; set; }
}

// GetPlayerAchievements / GetUserStatsForGame
public class PlayerAchievementsResult
{
    public PlayerStats playerstats { get; set; }
}

public class PlayerStats
{
    public string steamID { get; set; }
    public string gameName { get; set; }
    public bool success { get; set; }
    public List<PlayerAchievement> achievements { get; set; }
}

public class PlayerAchievement
{
    public string apiname { get; set; }
    public int achieved { get; set; }
    public long unlocktime { get; set; }
    public string name { get; set; }
    public string description { get; set; }
}

// GetOwnedGames
public class OwnedGamesResult
{
    public OwnedGamesResponse response { get; set; }
}

public class OwnedGamesResponse
{
    public int game_count { get; set; }
    public List<Game> games { get; set; }
}

public class Game
{
    public int appid { get; set; }
    public string name { get; set; }
    public int playtime_forever { get; set; }
    public int playtime_2weeks { get; set; }
    public string img_icon_url { get; set; }
    public string img_logo_url { get; set; }
    public bool has_community_visible_stats { get; set; }
}

// GetRecentlyPlayedGames
public class RecentlyPlayedGamesResult
{
    public RecentlyPlayedGamesResponse response { get; set; }
}

public class RecentlyPlayedGamesResponse
{
    public int total_count { get; set; }
    public List<Game> games { get; set; }
}
public class GameSchemaResult
{
    public GameSchema game { get; set; }
}

public class GameSchema
{
    public AvailableGameStats availableGameStats { get; set; }
}

public class AvailableGameStats
{
    public List<AchievementDefinition> achievements { get; set; }
}

public class AchievementDefinition
{
    public string name { get; set; }
    public string displayName { get; set; }
    public string description { get; set; }
    public string icon { get; set; }
    
    public int hidden { get; set; }
    public string icongray { get; set; }
}

public class AchievementPercentagesResult
{
    public AchievementPercentages achievementpercentages { get; set; }
}

public class AchievementPercentages
{
    public List<GlobalAchievement> achievements { get; set; }
}

public class GlobalAchievement
{
    public string name { get; set; }

    [JsonConverter(typeof(StringToFloatConverter))]
    public float percent { get; set; }
}
