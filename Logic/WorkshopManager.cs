using System.Collections.Concurrent;
using Logic.Data;

namespace Logic;

public static class WorkshopManager
{
    private static int? filterEntriesCount;
    private static Dictionary<long, WorkshopEntry> cachedEntries = new Dictionary<long, WorkshopEntry>();

    public static int GetWallpaperCount() => filterEntriesCount ?? 0;

    public static async Task RefreshLocalEntries()
    {
        foreach (string dir in ConfigManager.localWorkshopLocations!)
        {
            string[] entries = Directory.GetDirectories(dir);
            foreach (string wallpaper in entries)
            {
                InvestigateWallpaper(wallpaper);
            }
        }

        await Parallel.ForEachAsync(cachedEntries, async (KeyValuePair<long, WorkshopEntry> res, CancellationToken token) => await res.Value.DecodeBasic());

        void InvestigateWallpaper(string path)
        {
            if (!long.TryParse(Path.GetFileName(path), out long wallpaperId) || cachedEntries.ContainsKey(wallpaperId))
                return;

            cachedEntries.Add(wallpaperId, new WorkshopEntry(wallpaperId, path));
        }
    }

    public static WorkshopEntry[] GetCachedWallpaperEntries(string? nameSearch, HashSet<string>? tags, int skip, int take)
    {
        IEnumerable<WorkshopEntry> entries = cachedEntries.Values;

        if (!string.IsNullOrEmpty(nameSearch))
        {
            entries = entries.Where(x => x.title?.Contains(nameSearch, StringComparison.InvariantCultureIgnoreCase) ?? false);
        }

        filterEntriesCount = entries.Count();
        return entries.Skip(skip).Take(take).ToArray();
    }

    public static bool TryGetWallpaperEntry(long id, out WorkshopEntry? entry)
        => cachedEntries.TryGetValue(id, out entry);
}
