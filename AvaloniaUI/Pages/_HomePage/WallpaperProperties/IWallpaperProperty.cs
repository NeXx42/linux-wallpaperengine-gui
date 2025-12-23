using System.Collections.Generic;
using Logic.Data;
using Logic.Database;

namespace AvaloniaUI.Pages._HomePage.WallpaperProperties;

public interface IWallpaperProperty
{
    public IWallpaperProperty Init(WorkshopEntry.Properties prop);

    public dbo_WallpaperSettings? Save(long id);
    public void Load(ref Dictionary<string, string?> options);

    public string? CreateArgument();
}
