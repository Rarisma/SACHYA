public class BasicGame
{
    public string TitleId { get; }
    public string Name { get; }
    public string Developer { get; }

    public string? Platform { get; }
    public string? IconUrl { get; }

    public BasicGame(string titleId, string name, string iconUrl)
    {
        TitleId = titleId;
        Name = name;
        IconUrl = iconUrl;
    }
}