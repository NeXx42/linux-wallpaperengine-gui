using System.Text;
using HtmlAgilityPack;
using Logic.Data;

namespace Logic;

public static class SteamWorkshopManager
{
    public static async Task<(SteamWorkshopEntry[]?, Exception? e)> FetchItems(WorkshopFilter filter)
    {
        List<SteamWorkshopEntry> items = new List<SteamWorkshopEntry>();

        try
        {
            using (HttpClient client = new HttpClient())
            {
                string html = await client.GetStringAsync(filter.ConstructURL());

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

                return (items.ToArray(), null);
            }
        }
        catch (Exception e)
        {
            return (null, e);
        }
    }


    public struct WorkshopFilter
    {
        public int page;

        public string ConstructURL()
        {
            StringBuilder sb = new StringBuilder($"https://steamcommunity.com/workshop/browse/?appid={ConfigManager.WALLPAPER_ENGINE_ID}&");

            sb.Append($"&p={page}");
            return sb.ToString();
        }
    }
}
