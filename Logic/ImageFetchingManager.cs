using System.Collections.Concurrent;
using Logic.Data;

namespace Logic;

public static class ImageFetchingManager
{
    public const int ICON_FETCH_PER_ITERATION = 10;
    public const int ICON_CACHE_LIMIT = 200;

    public delegate Task IconFetchCallback(long entryId, object? result);

    private static Func<string, Task<object?>>? iconFetchTask;
    private static Func<string, Task<(byte[]?, object?)>>? webIconFetch;

    private static ConcurrentBag<long> persistentCache = new ConcurrentBag<long>();

    private static ConcurrentDictionary<long, object?> iconCache = new ConcurrentDictionary<long, object?>();
    private static ConcurrentDictionary<long, IconFetchRequest> iconFetchQueue = new ConcurrentDictionary<long, IconFetchRequest>();

    private static Thread? iconThread;

    private static string getPersistentCacheRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", ConfigManager.APPLICATION_NAME, ".icons");


    public static async Task Init(Func<string, Task<object?>> iconFetchTask, Func<string, Task<(byte[]?, object?)>> webIconFetch)
    {
        if (Directory.Exists(getPersistentCacheRoot))
        {
            string[] cachedItems = Directory.GetFiles(getPersistentCacheRoot);

            Parallel.ForEach(cachedItems, (string file) =>
            {
                if (long.TryParse(Path.GetFileName(file), out long _id))
                    persistentCache.Add(_id);
            });
        }
        else
        {
            Directory.CreateDirectory(getPersistentCacheRoot);
        }

        ImageFetchingManager.webIconFetch = webIconFetch;
        ImageFetchingManager.iconFetchTask = iconFetchTask;

        iconThread = new Thread(HandleIconThread);
        iconThread.Start();
    }


    public static void QueueFetchWallpaperIcon(IWorkshopEntry entry, IconFetchCallback onFetch)
    {
        if (iconFetchQueue.TryGetValue(entry.getId, out IconFetchRequest? fetchRequest))
        {
            fetchRequest!.callback += onFetch;
        }
        else
        {
            string? path = entry.getIconPath;

            if (string.IsNullOrEmpty(path))
                return;

            if (path.StartsWith("https://") && persistentCache.Contains(entry.getId))
            {
                path = Path.Combine(getPersistentCacheRoot, entry.getId.ToString());
            }

            iconFetchQueue[entry.getId] = new IconFetchRequest()
            {
                path = path,
                callback = onFetch
            };
        }
    }

    private static async void HandleIconThread()
    {
        IEnumerable<long> toComplete = new List<long>();

        while (true)
        {
            await Task.Delay(10);

            // lazy....
            if (iconCache.Count >= ICON_CACHE_LIMIT)
            {
                iconCache.Clear();
                continue;
            }

            if (iconFetchQueue.Count == 0)
                continue;

            toComplete = iconFetchQueue.Keys.Take(ICON_FETCH_PER_ITERATION);

            await Parallel.ForEachAsync(toComplete, async (long fetch, CancellationToken token) =>
            {
                if (!iconFetchQueue.TryGetValue(fetch, out IconFetchRequest? req) || req == null)
                    return;

                if (req.path.StartsWith("https://"))
                {
                    (byte[]? img, object? brush) = await webIconFetch!(req.path);
                    req.result = brush;

                    await CacheIntercept(fetch, img);
                }
                else
                {
                    req.result = await iconFetchTask!(req.path);
                }

                iconCache.TryAdd(fetch, req.result);
            });

            foreach (long complete in toComplete)
            {
                iconFetchQueue.TryRemove(complete, out IconFetchRequest? req);
                req?.callback?.Invoke(complete, req!.result);
            }
        }
    }

    private static async Task CacheIntercept(long entryId, byte[]? img)
    {
        if (img == null)
            return;

        persistentCache.Add(entryId);
        await File.WriteAllBytesAsync(Path.Combine(getPersistentCacheRoot, entryId.ToString()), img);
    }

    private class IconFetchRequest
    {
        public required string path;
        public required IconFetchCallback callback;

        public object? result;
    }
}
