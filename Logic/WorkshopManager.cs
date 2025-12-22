using System.Collections.Concurrent;
using Logic.Data;

namespace Logic;

public static class WorkshopManager
{
    public const int ICON_FETCH_PER_ITTERATION = 10;

    public delegate Task IconFetchCallback(long entryId, object? result);

    private static Func<string, Task<object?>>? iconFetchTask;
    private static ConcurrentDictionary<long, IconFetchRequest> iconFetchQueue = new ConcurrentDictionary<long, IconFetchRequest>();
    private static ConcurrentDictionary<long, object?> iconCache = new ConcurrentDictionary<long, object?>();

    private static Thread? iconThread;

    private static Dictionary<long, WorkshopEntry> cachedEntries = new Dictionary<long, WorkshopEntry>();



    public static async Task Init(Func<string, Task<object?>> iconFetchTask)
    {
        WorkshopManager.iconFetchTask = iconFetchTask;

        iconThread = new Thread(HandleIconThread);
        iconThread.Start();
    }

    public static int GetWallpaperCount() => cachedEntries.Count;

    public static void RefreshLocalEntries()
    {
        foreach (string dir in ConfigManager.localWorkshopLocations)
        {
            string[] entries = Directory.GetDirectories(dir);
            foreach (string wallpaper in entries)
            {
                InvestigateWallpaper(wallpaper);
            }
        }

        void InvestigateWallpaper(string path)
        {
            if (!long.TryParse(Path.GetFileName(path), out long wallpaperId) || cachedEntries.ContainsKey(wallpaperId))
                return;

            cachedEntries.Add(wallpaperId, new WorkshopEntry(wallpaperId, path));
        }
    }

    public static WorkshopEntry[] GetCachedWallpaperEntries(int skip, int take)
    {
        return cachedEntries.Values.Skip(skip).Take(take).ToArray();
    }

    public static bool TryGetWallpaperEntry(long id, out WorkshopEntry? entry)
        => cachedEntries.TryGetValue(id, out entry);



    // icon fetching


    public static void QueueFetchWallpaperIcon(long id, IconFetchCallback onFetch)
    {
        if (!cachedEntries.TryGetValue(id, out WorkshopEntry? entry))
        {
            onFetch?.Invoke(id, null);
            return;
        }

        if (iconFetchQueue.TryGetValue(id, out IconFetchRequest? fetchRequest))
        {
            fetchRequest!.callback += onFetch;
        }
        else
        {
            iconFetchQueue[id] = new IconFetchRequest()
            {
                path = Path.Combine(entry.path, entry.iconPath!),
                callback = onFetch
            };
        }
    }

    private static async void HandleIconThread()
    {
        int itterationLimit;
        List<long> toComplete = new List<long>();

        while (true)
        {
            await Task.Delay(10);

            if (iconFetchQueue.Count == 0)
                continue;

            toComplete.Clear();
            itterationLimit = 0;

            foreach (KeyValuePair<long, IconFetchRequest> fetch in iconFetchQueue)
            {
                if (itterationLimit >= ICON_FETCH_PER_ITTERATION)
                    break;

                fetch.Value.result = await iconFetchTask!(fetch.Value.path);
                iconCache.TryAdd(fetch.Key, fetch.Value.result);

                toComplete.Add(fetch.Key);
                itterationLimit++;
            }

            foreach (long complete in toComplete)
            {
                iconFetchQueue.TryRemove(complete, out IconFetchRequest? req);
                req?.callback?.Invoke(complete, req!.result);
            }
        }
    }

    private class IconFetchRequest
    {
        public required string path;
        public required IconFetchCallback callback;

        public object? result;
    }
}
