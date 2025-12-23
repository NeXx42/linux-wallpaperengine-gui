using CSharpSqliteORM;
using Logic.Database;
using Logic.db;

namespace Logic;

public static class ConfigManager
{
    public enum ConfigKeys
    {
        ExecutableLocation,
        WorkshopLocations,

        SaveStartupScriptLocation
    }

    public static string[]? localWorkshopLocations { private set; get; }

    private static Screen[]? screens;

    public static async Task Init()
    {
        string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LinuxWallpaperEngineGUI.db");
        await Database_Manager.Init(dbPath);

        await LoadWorkshopLocations();
        await WallpaperSetter.TryFindExecutableLocation();
    }

    private static async Task LoadWorkshopLocations()
    {
        dbo_Config[] entries = await GetConfigValues(ConfigKeys.WorkshopLocations);

        if (entries.Length == 0)
        {
            localWorkshopLocations = [Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/Steam/steamapps/workshop/content/431960")];
        }
        else
        {
            localWorkshopLocations = entries.Select(x => x.value!).ToArray();
        }
    }

    public static async Task RegisterDisplays(Screen[] screens)
    {
        dbo_ScreenSettings[] existingConfigs = await Database_Manager.GetItems<dbo_ScreenSettings>();

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

                await Database_Manager.AddOrUpdate(newSetting, SQLFilter.Equal(nameof(newSetting.screenName), screenName), nameof(newSetting.screenOrder));
                return;
            }
        }
    }



    public static async Task<dbo_Config?> GetConfigValue(ConfigKeys key) => (await GetConfigValues(key)).FirstOrDefault();
    public static async Task<dbo_Config[]> GetConfigValues(ConfigKeys key) => await Database_Manager.GetItems<dbo_Config>(SQLFilter.Equal(nameof(dbo_Config.key), key.ToString()));

    public static async Task SetConfigValue(ConfigKeys key, string? to, bool deleteIfNull = true)
    {
        if (deleteIfNull && string.IsNullOrEmpty(to))
        {
            await Database_Manager.Delete<dbo_Config>(SQLFilter.Equal(nameof(dbo_Config.key), key.ToString()));
            return;
        }

        dbo_Config change = new dbo_Config()
        {
            key = key.ToString(),
            value = to
        };

        await Database_Manager.AddOrUpdate(change, SQLFilter.Equal(nameof(dbo_Config.key), change.key), nameof(change.value));
    }

    public static async Task<dbo_WallpaperSettings[]> GetWallpaperSettings(long id)
        => await Database_Manager.GetItems<dbo_WallpaperSettings>(SQLFilter.Equal(nameof(dbo_WallpaperSettings.wallpaperId), id));

    public static async Task SetWallpaperSavedSettings(long id, dbo_WallpaperSettings[] options)
    {
        Func<dbo_WallpaperSettings, SQLFilter.InternalSQLFilter> matcher = (x) => SQLFilter.Equal(nameof(dbo_WallpaperSettings.wallpaperId), id).Equal(nameof(dbo_WallpaperSettings.settingKey), x.settingKey);
        await Database_Manager.AddOrUpdate(options, matcher, nameof(dbo_WallpaperSettings.settingValue));
    }


    public struct Screen
    {
        public string screenName;
        public int priority;
    }
}
