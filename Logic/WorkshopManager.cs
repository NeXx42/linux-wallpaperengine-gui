using System.Collections.Concurrent;
using Logic.Data;

namespace Logic;

public static class WorkshopManager
{
    private static int? filterEntriesCount;
    private static ConcurrentDictionary<long, WorkshopEntry> cachedEntries = new ConcurrentDictionary<long, WorkshopEntry>();

    public static int GetWallpaperCount() => filterEntriesCount ?? 0;

    public static async Task RefreshLocalEntries()
    {
        List<DirectoryInfo> folders = new List<DirectoryInfo>();

        foreach (string dir in ConfigManager.localWorkshopLocations!)
        {
            string[] entries = Directory.GetDirectories(dir);
            foreach (string wallpaper in entries)
            {
                folders.Add(new DirectoryInfo(wallpaper));
            }
        }

        folders = folders.OrderByDescending(x => x.CreationTimeUtc).ToList();

        await Parallel.ForEachAsync(folders, async (DirectoryInfo dir, CancellationToken token) =>
        {
            if (!long.TryParse(Path.GetFileName(dir.FullName), out long wallpaperId) || cachedEntries.ContainsKey(wallpaperId))
                return;

            WorkshopEntry entry = new WorkshopEntry(wallpaperId, dir.FullName, dir.CreationTimeUtc);
            await entry.DecodeBasic();

            cachedEntries.TryAdd(wallpaperId, entry);
        });
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
