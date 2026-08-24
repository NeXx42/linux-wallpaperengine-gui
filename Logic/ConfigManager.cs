using CSharpSqliteORM;
using Logic.Database;
using Logic.db;

namespace Logic;

public static class ConfigManager
{
    public static readonly bool IsFlatpak = File.Exists("/.flatpak-info");

    public const string APPLICATION_NAME = "LinuxWallpaperEngineGUI";
    public const int WALLPAPER_ENGINE_ID = 431960;

    public enum ConfigKeys
    {
        ExecutableLocation,
        WorkshopLocations,

        SaveStartupScriptLocation,

        SteamUsername,
        LastSetWallpaper,
        ExecutableType,
    }

    public static string[]? localWorkshopLocations { private set; get; }

    private static Screen[]? screens;

    public static async Task Init(bool fullLoad = true)
    {
        string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{APPLICATION_NAME}.db");

        await DatabaseManager.Init(dbPath);
        await WallpaperEngine.Init(fullLoad);
        await LoggingManager.Init(e => { });

        if (fullLoad)
        {
            await LoadWorkshopLocations();
        }
    }

    public static async void LoadStartupVersion(long? specificWallpaper)
    {
        await Init(false);

        dbo_ScreenSettings[] savedScreens = await DatabaseManager.db!.GetItems<dbo_ScreenSettings>();
        screens = savedScreens.Select(x => new Screen()
        {
            screenName = x.screenName,
            priority = x.screenOrder ?? 0
        }).ToArray();

        await WallpaperEngine.SetWallpaper(specificWallpaper);
        Console.WriteLine("Set wallpaper");

        Environment.Exit(0);
    }

    private static async Task LoadWorkshopLocations()
    {
        dbo_Config[] entries = await GetConfigValues(ConfigKeys.WorkshopLocations);

        if (entries.Length == 0)
        {
            localWorkshopLocations = [Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".local/share/Steam/steamapps/workshop/content/{WALLPAPER_ENGINE_ID}")];
        }
        else
        {
            localWorkshopLocations = entries.Select(x => x.value!).ToArray();
        }
    }

    public static async Task RegisterDisplays(Screen[] screens)
    {
        dbo_ScreenSettings[] existingConfigs = await DatabaseManager.db!.GetItems<dbo_ScreenSettings>();

        for (int i = 0; i < screens.Length; i++)
        {
            dbo_ScreenSettings? savedSetting = existingConfigs.FirstOrDefault(x => x.screenName.Equals(screens[i].screenName));

            if (savedSetting == null)
                continue;

            screens[i].priority = savedSetting.screenOrder ?? 0;
        }

        ConfigManager.screens = screens;

    }

    public static Screen[] GetScreensOrdered() => screens!.OrderBy(x => x.priority).ThenBy(x => x.screenName).ToArray();

    public static async Task UpdateDisplayOrder(string screenName, int to)
    {
        for (int i = 0; i < screens!.Length; i++)
        {
            if (screens[i].screenName == screenName)
            {
                screens[i].priority = to;

                dbo_ScreenSettings newSetting = new dbo_ScreenSettings()
                {
                    screenName = screenName,
                    screenOrder = to
                };

                await DatabaseManager.db!.AddOrUpdate(newSetting, SQLFilter.Equal(nameof(newSetting.screenName), screenName), nameof(newSetting.screenOrder));
                return;
            }
        }
    }



    public static async Task<dbo_Config?> GetConfigValue(ConfigKeys key) => (await GetConfigValues(key)).FirstOrDefault();
    public static async Task<dbo_Config[]> GetConfigValues(ConfigKeys key) => await DatabaseManager.db!.GetItems<dbo_Config>(SQLFilter.Equal(nameof(dbo_Config.key), key.ToString()));

    public static async Task SetConfigValue(ConfigKeys key, string? to, bool deleteIfNull = true)
    {
        if (deleteIfNull && string.IsNullOrEmpty(to))
        {
            await DatabaseManager.db!.Delete<dbo_Config>(SQLFilter.Equal(nameof(dbo_Config.key), key.ToString()));
            return;
        }

        dbo_Config change = new dbo_Config()
        {
            key = key.ToString(),
            value = to
        };

        await DatabaseManager.db!.AddOrUpdate(change, SQLFilter.Equal(nameof(dbo_Config.key), change.key), nameof(change.value));
    }

    public static async Task<dbo_WallpaperSettings[]> GetWallpaperSettings(long id)
        => await DatabaseManager.db!.GetItems<dbo_WallpaperSettings>(SQLFilter.Equal(nameof(dbo_WallpaperSettings.wallpaperId), id));

    public static async Task SetWallpaperSavedSettings(long id, dbo_WallpaperSettings[] options)
    {
        Func<dbo_WallpaperSettings, SQLFilter.InternalSQLFilter> matcher = (x) => SQLFilter.Equal(nameof(dbo_WallpaperSettings.wallpaperId), id).Equal(nameof(dbo_WallpaperSettings.settingKey), x.settingKey);
        await DatabaseManager.db!.AddOrUpdate(options, matcher, nameof(dbo_WallpaperSettings.settingValue));
    }


    public struct Screen
    {
        public string screenName;
        public int priority;
    }
}
