using System.Text;
using HtmlAgilityPack;
using Logic.Data;

namespace Logic;

public static class SteamWorkshopManager
{
    public static async Task<DataFetchResponse> FetchItems(DataFetchRequest filter)
    {
        List<SteamWorkshopEntry> items = new List<SteamWorkshopEntry>();

        try
        {
            using (HttpClient client = new HttpClient())
            {
                string html = await client.GetStringAsync(BuildURLFromFilter(filter));

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                var entries = doc.DocumentNode.SelectNodes("//div[@class='workshopItem']");

                foreach (var item in entries)
                {
                    try
                    {
                        string id = item.SelectSingleNode(".//a[@class='ugc']").GetDataAttribute("publishedfileid").Value;
                        string name = item.SelectSingleNode(".//div[contains(@class, 'workshopItemTitle')]")?.InnerHtml ?? "";
                        string imgUrl = item.SelectSingleNode(".//img[contains(@class, 'workshopItemPreviewImage')]")?.GetAttributeValue("src", "") ?? "";

                        if (long.TryParse(id, out long _id))
                        {
                            items.Add(new SteamWorkshopEntry()
                            {
                                id = _id,
                                name = name,
                                imgUrl = imgUrl,
                            });
                        }
                    }
                    catch
                    {

                    }
                }

                return new DataFetchResponse(items.ToArray());
            }
        }
        catch (Exception e)
        {
            return new DataFetchResponse(e);
        }
    }

    private static string BuildURLFromFilter(DataFetchRequest filter)
    {
        StringBuilder sb = new StringBuilder($"https://steamcommunity.com/workshop/browse/?appid={ConfigManager.WALLPAPER_ENGINE_ID}&");

        if (!string.IsNullOrEmpty(filter.textFilter))
        {
            sb.Append($"&searchtext={filter.textFilter}");
        }

        sb.Append($"&p={filter.skip}");
        return sb.ToString();
    }
}
