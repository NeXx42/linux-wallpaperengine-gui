using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaUI.Pages._HomePage;
using AvaloniaUI.Utils;
using Logic;
using Logic.Data;

namespace AvaloniaUI.Pages;

public partial class HomePage : UserControl
{
    public const string DEFAULT_SCALING_NAME = "Default";

    public const int ENTRY_SIZE = 150;
    public const int ENTRIES_PER_PAGE = 50;

    private int loadedPages = 0;

    private long? currentlySelectedWallpaper
    {
        set
        {
            m_currentlySelectedWallpaper = value;
            grid_SidePanel.IsVisible = m_currentlySelectedWallpaper.HasValue;
        }
        get => m_currentlySelectedWallpaper;
    }
    private long? m_currentlySelectedWallpaper;

    public HomePage()
    {
        InitializeComponent();
        SetupBasicOptions();

        if (!Design.IsDesignMode)
        {
            WorkshopManager.RefreshLocalEntries();
            DrawWallpapers();
        }
    }

    private void SetupBasicOptions()
    {
        string[] options = [DEFAULT_SCALING_NAME, .. System.Enum.GetNames(typeof(WallpaperSetter.ScalingOptions))];
        inp_SidePanel_Scaling.SelectedIndex = 0;
        inp_SidePanel_Scaling.ItemsSource = options;

        options = System.Enum.GetNames(typeof(WallpaperSetter.ClampOptions));
        inp_SidePanel_Clamp.SelectedIndex = 0;
        inp_SidePanel_Clamp.ItemsSource = options;

        btn_SidePanel_Set.RegisterClick(SetWallpaper);

        inp_SidePanel_OffsetY.Minimum = -1;
        inp_SidePanel_OffsetY.Maximum = 1;

        inp_SidePanel_OffsetX.Minimum = -1;
        inp_SidePanel_OffsetX.Maximum = 1;

        btn_LoadMore.RegisterClick(LoadExtraEntries);
    }

    private async void DrawWallpapers()
    {
        WorkshopEntry[] wallpapers = WorkshopManager.GetCachedWallpaperEntries(loadedPages * ENTRIES_PER_PAGE, ENTRIES_PER_PAGE);

        foreach (WorkshopEntry wallpaper in wallpapers)
        {
            HomePage_Wallpaper wallpaperEntry = new HomePage_Wallpaper();
            wallpaperEntry.StartDraw(wallpaper, this);
            wallpaperEntry.Height = ENTRY_SIZE;
            wallpaperEntry.Width = ENTRY_SIZE;

            grid_Content_Container.Children.Add(wallpaperEntry);
        }
    }

    public async void SelectWallpaper(long id)
    {
        currentlySelectedWallpaper = id;

        img_SidePanel_Icon.Background = null;
        lbl_SidePanel_Title.Content = "";

        if (!WorkshopManager.TryGetWallpaperEntry(id, out WorkshopEntry? entry))
            return;

        await entry!.Decode();
        lbl_SidePanel_Title.Content = entry.title;

        ImageBrush? brush = await ImageFetcher.GetIcon(id);
        img_SidePanel_Icon.Background = brush;
    }

    private void SetWallpaper()
    {
        if (currentlySelectedWallpaper == null || !WorkshopManager.TryGetWallpaperEntry(currentlySelectedWallpaper.Value, out WorkshopEntry? entry))
            return;

        WallpaperSetter.WallpaperOptions options = new WallpaperSetter.WallpaperOptions();
        options.scalingOption = inp_SidePanel_Scaling.SelectedIndex - 1 >= 0 ? (WallpaperSetter.ScalingOptions)(inp_SidePanel_Scaling.SelectedIndex - 1) : null;
        options.clampOptions = (WallpaperSetter.ClampOptions)inp_SidePanel_Clamp.SelectedIndex;

        options.screens = WallpaperSetter.WorkOutScreenOffsets((float)inp_SidePanel_OffsetX.Value, (float)inp_SidePanel_OffsetY.Value);
        WallpaperSetter.SetWallpaper(entry!.path, options);
    }

    private void LoadExtraEntries()
    {
        loadedPages++;
        DrawWallpapers();

        int maxPages = (int)Math.Ceiling(WorkshopManager.GetWallpaperCount() / (float)ENTRIES_PER_PAGE);

        if (loadedPages >= maxPages - 1)
        {
            btn_LoadMore.IsVisible = false;
        }
    }
}