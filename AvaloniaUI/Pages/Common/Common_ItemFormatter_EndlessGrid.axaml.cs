using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Logic;
using Logic.Data;

namespace AvaloniaUI.Pages.Common;

public partial class Common_ItemFormatter_EndlessGrid : UserControl, IItemFormatterBase
{
    public const int ENTRIES_PER_PAGE = 50;

    private int loadedPages = 0;
    private string? cachedNameFilter;

    private Common_ItemViewer? viewer;

    private Func<long, Task>? sidePanelHandler;
    private Func<DataFetchRequest, Task<DataFetchResponse>>? dataFetcher;

    private Dictionary<long, Common_Wallpaper> cachedWallpaperUI = new Dictionary<long, Common_Wallpaper>();

    public long? currentlySelectedWallpaper
    {
        set
        {
            if (m_currentlySelectedWallpaper.HasValue && cachedWallpaperUI.TryGetValue(m_currentlySelectedWallpaper.Value, out Common_Wallpaper? ui))
            {
                ui.ToggleSelection(false);
            }

            m_currentlySelectedWallpaper = value;
            viewer!.grid_SidePanel.IsVisible = m_currentlySelectedWallpaper.HasValue;

            if (m_currentlySelectedWallpaper.HasValue && cachedWallpaperUI.TryGetValue(m_currentlySelectedWallpaper.Value, out ui))
            {
                ui.ToggleSelection(true);
            }
        }
        get => m_currentlySelectedWallpaper;
    }
    private long? m_currentlySelectedWallpaper;


    public Common_ItemFormatter_EndlessGrid()
    {
        InitializeComponent();
        btn_LoadMore.RegisterClick(LoadExtraEntries);
        //viewer.btn_Refresh.RegisterClick(() => LoadPage(true));
    }

    public void Setup(Common_ItemViewer viewer, Func<long, Task> sidePanelHandler, Func<DataFetchRequest, Task<DataFetchResponse>> dataFetcher)
    {
        this.viewer = viewer;
        this.sidePanelHandler = sidePanelHandler;
        this.dataFetcher = dataFetcher;

        viewer.inp_NameSearch.KeyUp += async (_, __) => await UpdateFilter();
    }

    public async Task Reset()
    {
        loadedPages = 0;
        currentlySelectedWallpaper = null;

        await MainWindow.AsyncLoad(WorkshopManager.RefreshLocalEntries);
        await Draw(false);
    }

    public async Task Draw(bool additive)
    {
        DataFetchResponse res = await dataFetcher!(new DataFetchRequest()
        {
            skip = loadedPages * ENTRIES_PER_PAGE,
            take = ENTRIES_PER_PAGE,

            textFilter = viewer!.inp_NameSearch.Text
        });

        if (!additive)
            grid_Content_Container.Children.Clear();

        for (int i = 0; i < res.entries!.Length; i++)
        {
            Common_Wallpaper ui = GetWallpaperUI(res.entries![i]);

            ui.StartDraw(res.entries![i], this);
            grid_Content_Container.Children.Add(ui);
        }

        int maxPages = (int)Math.Ceiling(WorkshopManager.GetWallpaperCount() / (float)ENTRIES_PER_PAGE);
        btn_LoadMore.IsVisible = loadedPages < maxPages - 1;

        Common_Wallpaper GetWallpaperUI(IWorkshopEntry entry)
        {
            if (cachedWallpaperUI.TryGetValue(entry.getId, out Common_Wallpaper? cached))
                return cached!;

            Common_Wallpaper wallpaperEntry = new Common_Wallpaper();
            wallpaperEntry.Height = HomePage.ENTRY_SIZE;
            wallpaperEntry.Width = HomePage.ENTRY_SIZE;

            cachedWallpaperUI.Add(entry.getId, wallpaperEntry);
            return wallpaperEntry;
        }
    }



    public async void SelectWallpaper(long id)
    {
        if (id == currentlySelectedWallpaper)
            return;

        currentlySelectedWallpaper = id;
        viewer!.OpenSidePanel();

        await sidePanelHandler!(id);
    }



    private async Task LoadExtraEntries()
    {
        loadedPages++;
        await Draw(true);
    }

    private async Task UpdateFilter()
    {
        if (cachedNameFilter == viewer!.inp_NameSearch.Text)
        {
            return;
        }

        cachedNameFilter = viewer.inp_NameSearch.Text;

        loadedPages = 0;
        await Draw(false);
    }
}