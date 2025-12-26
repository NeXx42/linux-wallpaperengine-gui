using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaUI.Pages.Common;
using AvaloniaUI.Utils;
using Logic;
using Logic.Data;

namespace AvaloniaUI.Pages;

public partial class SteamWorkshopPage : UserControl
{
    private bool isSetup = false;

    private Common_ItemFormatter_Paginated itemFormatter;

    public SteamWorkshopPage()
    {
        InitializeComponent();

        itemFormatter = new Common_ItemFormatter_Paginated();
        itemFormatter.Setup(ItemViewer, ViewWallpaper, FetchEntries);

        ItemViewer.Setup(itemFormatter, null);
        ItemViewer.RegisterAction("Download", DownloadWallpaper);
    }

    public async void LoadPage()
    {
        if (isSetup)
            return;

        isSetup = true;
        await itemFormatter.Reset();
    }

    private async Task<DataFetchResponse> FetchEntries(DataFetchRequest req)
    {
        return await SteamWorkshopManager.FetchItems(req);
    }

    private async Task ViewWallpaper(long id)
    {
        SteamWorkshopEntry entry = (SteamWorkshopEntry)itemFormatter.GetItemByID(id)!;
        ItemViewer.lbl_SidePanel_Title.Content = entry.name;

        ItemViewer.img_SidePanel_Icon.Background = await ImageFetcher.GetIcon(entry);
    }

    private async Task DownloadWallpaper()
    {
        if (!itemFormatter.currentlySelectedWallpaper.HasValue)
            return;


    }
}