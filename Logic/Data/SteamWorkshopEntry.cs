namespace Logic.Data;

public class SteamWorkshopEntry : IWorkshopEntry
{
    public required long id;
    public string? name;
    public string? imgUrl;


    public long getId => id;
    public string? getIconPath => imgUrl;
    public string? getTitle => name;
}
