using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using HtmlAgilityPack;
using Logic.Data;
using Logic.Enums;

namespace Logic;

public static class SteamWorkshopManager
{
    public static HttpClient httpClient
    {
        get
        {
            if (m_Client == null)
                m_Client = new HttpClient();

            return m_Client;
        }
    }
    private static HttpClient? m_Client;
    private static CancellationTokenSource? activeQuery;

    public readonly static string[] tags = [
         "Abstract",
        "Animal",
        "Anime",
        "Cartoon",
        "CGI",
        "Cyberpunk",
        "Fantasy",
        "Game",
        "Girls",
        "Guys",
        "Landscape",
        "Medieval",
        "Memes",
        "MMD",
        "Music",
        "Nature",
        "Pixel art",
        "Relaxing",
        "Retro",
        "Sci-Fi",
        "Sports",
        "Technology",
        "Television",
        "Vehicle",
        "Unspecified"
    ];

    public readonly static string[] resolutions = [
        "Standard Definition",
        "1280 x 720",
        "1366 x 768",
        "1920 x 1080",
        "2560 x 1440",
        "3840 x 2160",
        "Ultrawide Standard Definition",
        "Ultrawide 2560 x 1080",
        "Ultrawide 3440 x 1440",
        "Dual Standard Definition",
        "Dual 3840 x 1080",
        "Dual 5120 x 1440",
        "Dual 7680 x 2160",
        "Triple Standard Definition",
        "Triple 4096 x 768",
        "Triple 5760 x 1080",
        "Triple 7680 x 1440",
        "Triple 11520 x 2160",
        "Portrait Standard Definition",
        "Portrait 720 x 1280",
        "Portrait 1080 x 1920",
        "Portrait 1440 x 2560",
        "Portrait 2160 x 3840",
        "Other resolution",
        "Dynamic resolution"
    ];

    public static async Task<DataFetchResponse> FetchItems(DataFetchRequest filter, bool recacheFilters)
    {
        try
        {
            await (activeQuery?.CancelAsync() ?? Task.CompletedTask);
            activeQuery = new CancellationTokenSource();

            string url = BuildURLFromFilter(filter);
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("x-valve-action-type", "c01qDV6jYXQx5fEYHzoJXsB8WNMVfAkX02y3S0Gq5kI:Browse");
            req.Headers.Add("x-valve-request-type", "routeAction");

            string browseSort = "";

            switch ((SteamWallpaperOrdering)filter.orderId)
            {
                default: browseSort = "trend"; break;
                case SteamWallpaperOrdering.New: browseSort = "mostrecent"; break;
                case SteamWallpaperOrdering.LastUpdated: browseSort = "lastupdated"; break;
                case SteamWallpaperOrdering.MostSubscribed: browseSort = "totaluniquesubscribers"; break;
            }

            SteamPageRequest[] reqBody = [new SteamPageRequest(){
                admin_view = false,
                appid = ConfigManager.WALLPAPER_ENGINE_ID,
                browse_sort =browseSort,
                childpublishedfileid = "",
                num_per_page = filter.take,
                page = filter.skip,
                required_apps_preset = 0,
                search_text = filter.textFilter ?? "",
                search_text_target = 0,
                section = "readytouseitems",
                trend_days = 7,
                required_tags = [..(string.IsNullOrEmpty(filter.resolutionFilter) ? (string[])[] : [filter.resolutionFilter]), ..(filter.tags ?? [])]
            }];

            string json = JsonSerializer.Serialize(reqBody);
            req.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var res = await httpClient.SendAsync(req, activeQuery.Token);

            res.EnsureSuccessStatusCode();

            string responseJson = await res.Content.ReadAsStringAsync();
            SteamPageResponse response = JsonSerializer.Deserialize<SteamPageResponse>(responseJson);

            return new DataFetchResponse(response.results.Select(r => new SteamWorkshopEntry()
            {
                id = long.Parse(r.publishedfileid),
                name = r.title,
                imgUrl = r.preview_url,
                tags = r.tags.Select(t => t.display_name).ToArray()
            }).ToArray());
        }
        catch (TaskCanceledException)
        {
            return new DataFetchResponse();
        }
        catch (Exception e)
        {
            return new DataFetchResponse(e);
        }
    }

    private struct SteamPageRequest
    {
        public bool admin_view { get; set; }
        public int appid { get; set; }
        public string browse_sort { get; set; }
        public string childpublishedfileid { get; set; }
        public int num_per_page { get; set; }
        public int page { get; set; }
        public int required_apps_preset { get; set; }
        public string search_text { get; set; }
        public int search_text_target { get; set; }
        public string section { get; set; }
        public int trend_days { get; set; }
        public string[]? required_tags { get; set; }
    }

    private struct SteamPageResponse
    {
        public int eresult { get; set; }
        public int page { get; set; }
        public int total_pages { get; set; }
        public int total_count { get; set; }
        public string next_cursor { get; set; }
        public SteamPageResponse_Entry[] results { get; set; }
    }

    public struct SteamPageResponse_Entry
    {
        public string publishedfileid { get; set; }
        public string creator { get; set; }
        public long consumer_appid { get; set; }
        public int file_type { get; set; }
        public string preview_url { get; set; }
        public string title { get; set; }
        public string short_description { get; set; }
        public bool workshop_accepted { get; set; }
        public long flags { get; set; }
        public long views { get; set; }
        public int time_created { get; set; }
        public int time_updated { get; set; }
        public string file_size { get; set; }
        public SteamPageResponse_Entry_Tag[] tags { get; set; }
        public int subscriptions { get; set; }
        public int lifetime_subscriptions { get; set; }
        public int favorited { get; set; }
        public int lifetime_favorited { get; set; }
        public string lifetime_playtime_sessions { get; set; }
        public int star_rating { get; set; }
        public int total_votes { get; set; }
    }

    public struct SteamPageResponse_Entry_Tag
    {
        public string tag { get; set; }
        public string display_name { get; set; }
    }


    /*
    public static async Task<DataFetchResponse> FetchItems(DataFetchRequest filter, bool recacheFilters)
    {
        await (activeQuery?.CancelAsync() ?? Task.CompletedTask);
        activeQuery = new CancellationTokenSource();

        try
        {
            string url = BuildURLFromFilter(filter);
            HtmlDocument doc = await LoadDocument(url, activeQuery.Token);

            if (recacheFilters)
            {
                await RecacheFilters(doc);
            }

            return new DataFetchResponse()
            {
                entries = await ScrapeAllWorkshopElements(doc, activeQuery.Token)
            };
        }
        catch (TaskCanceledException)
        {
            return new DataFetchResponse();
        }
        catch (Exception e)
        {
            return new DataFetchResponse(e);
        }
    }
*/
    private static async Task<SteamWorkshopEntry[]> ScrapeAllWorkshopElements(HtmlDocument doc, CancellationToken token)
    {
        HtmlNodeCollection entries = doc.DocumentNode.SelectNodes("//div[@class='workshopItem']");
        ConcurrentBag<SteamWorkshopEntry> results = new ConcurrentBag<SteamWorkshopEntry>();

        await Parallel.ForEachAsync(entries, token, async (i, token) =>
        {
            SteamWorkshopEntry? res = await ScrapeWorkshopElements(i, token);

            if (res != null)
                results.Add(res);
        });

        return results.ToArray();
    }

    private static Task<SteamWorkshopEntry?> ScrapeWorkshopElements(HtmlNode item, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return Task.FromResult<SteamWorkshopEntry?>(null);

        string id = item.SelectSingleNode(".//a[contains(@class, 'ugc')]")?.GetDataAttribute("publishedfileid")?.Value ?? "";
        string name = item.SelectSingleNode(".//div[contains(@class, 'workshopItemTitle')]")?.InnerHtml ?? "";
        string imgUrl = item.SelectSingleNode(".//img[contains(@class, 'workshopItemPreviewImage')]")?.GetAttributeValue("src", "") ?? "";

        if (long.TryParse(id, out long _id))
        {
            return Task.FromResult<SteamWorkshopEntry?>(new SteamWorkshopEntry()
            {
                id = _id,
                name = name,
                imgUrl = imgUrl,
            });
        }

        return Task.FromResult<SteamWorkshopEntry?>(null);
    }

    private static async Task<HtmlDocument> LoadDocument(string url, CancellationToken token)
    {
        string html = await httpClient.GetStringAsync(url, token);

        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(html);

        return doc;
    }

    /*
        private static async Task RecacheFilters(HtmlDocument doc)
        {
            HtmlNodeCollection allTags = doc.DocumentNode.SelectNodes("//input[@class='inputTagsFilter']");
            tags = allTags.Select(x => x?.GetAttributeValue("value", "")!).Order().ToArray();

            var node = doc.DocumentNode.SelectSingleNode("//div[@class='tag_category_desc' and normalize-space(text())='Resolution']");
            resolutions = node.NextSibling.NextSibling.ChildNodes.Select(x => x?.GetAttributeValue("value", "")!).Where(x => !string.IsNullOrEmpty(x) && x != "-1").Order().ToArray();
        }
    */

    private static string BuildURLFromFilter(DataFetchRequest filter)
    {
        StringBuilder sb = new StringBuilder($"https://steamcommunity.com/workshop/browse/?appid={ConfigManager.WALLPAPER_ENGINE_ID}");


        if (!string.IsNullOrEmpty(filter.resolutionFilter))
        {
            sb.Append($"&requiredtags[]={filter.resolutionFilter.Replace(" ", "+")}");
        }

        if (filter.tags != null)
            foreach (string str in filter.tags)
            {
                sb.Append($"&requiredtags[]={str}");
            }

        sb.Append("&browsesort=");
        switch ((SteamWallpaperOrdering)filter.orderId)
        {
            case SteamWallpaperOrdering.Popular: sb.Append("trend&days=7"); break;
            case SteamWallpaperOrdering.New: sb.Append("mostrecent"); break;
            case SteamWallpaperOrdering.LastUpdated: sb.Append("lastupdated"); break;
            case SteamWallpaperOrdering.MostSubscribed: sb.Append("totaluniquesubscribers"); break;
        }

        sb.Append($"&p={filter.skip}");
        sb.Append("&rss=1");

        if (!string.IsNullOrEmpty(filter.textFilter))
        {
            sb.Append($"&searchtext={filter.textFilter}");
        }

        return sb.ToString();
    }
}
