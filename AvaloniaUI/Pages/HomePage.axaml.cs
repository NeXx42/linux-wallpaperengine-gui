using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaUI.Pages._HomePage;
using AvaloniaUI.Pages._HomePage.WallpaperProperties;
using AvaloniaUI.Pages.Common;
using AvaloniaUI.Utils;
using Logic;
using Logic.Data;
using Logic.Database;

namespace AvaloniaUI.Pages;

public partial class HomePage : UserControl
{
    public const int PROPERTY_DEFAULT_HEIGHT = 30;
    public const int PROPERTY_DEFAULT_FONT_SIZE = 12;

    public const int ENTRY_SIZE = 150;

    private bool isSetup = false;

    private Common_ItemFormatter_EndlessGrid formatter;
    private HomePage_SidePanel sidePanel;

    public HomePage()
    {
        InitializeComponent();

        sidePanel = new HomePage_SidePanel();

        formatter = new Common_ItemFormatter_EndlessGrid();
        formatter.Setup(ItemViewer, OnSelectWallpaper, FetchEntries);

        ItemViewer.Setup(formatter, sidePanel);
        ItemViewer.RegisterAction("Browse To", BrowseToFolder);
        ItemViewer.RegisterAction("Set Wallpaper", SetWallpaper);
    }

    public async void LoadPage(bool force = false)
    {
        if (isSetup && !force)
            return;

        isSetup = true;
        await formatter.Reset();
    }

    private async Task OnSelectWallpaper(long id)
    {
        if (!WorkshopManager.TryGetWallpaperEntry(id, out WorkshopEntry? entry))
            return;

        await entry!.Decode();

        ItemViewer.lbl_SidePanel_Title.Content = entry.title;
        ItemViewer.DrawTags(entry.tags);

        await sidePanel.OnSelectWallpaper(entry);
        ItemViewer.img_SidePanel_Icon.Background = await ImageFetcher.GetIcon(entry!);
    }

    private void BrowseToFolder()
    {
        if (formatter.currentlySelectedWallpaper == null || !WorkshopManager.TryGetWallpaperEntry(formatter.currentlySelectedWallpaper.Value, out WorkshopEntry? entry))
            return;

        new Process() { StartInfo = new ProcessStartInfo { FileName = "xdg-open", Arguments = entry!.path, UseShellExecute = false } }.Start();
    }

    private async Task SetWallpaper()
    {
        if (formatter.currentlySelectedWallpaper == null || !WorkshopManager.TryGetWallpaperEntry(formatter.currentlySelectedWallpaper.Value, out WorkshopEntry? entry))
            return;

        WallpaperSetter.WallpaperOptions options = sidePanel.GetWallpaperOptions();

        await WallpaperSetter.SetWallpaper(entry!.path, options);
        await sidePanel.SaveWallpaperOptions(formatter.currentlySelectedWallpaper.Value);
    }

    private Task<DataFetchResponse> FetchEntries(DataFetchRequest req)
    {
        return Task.FromResult(new DataFetchResponse()
        {
            entries = WorkshopManager.GetCachedWallpaperEntries(req.textFilter, null, req.skip, req.take)
        });

    }
}