using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using AvaloniaUI.Common;
using Logic;
using Logic.Data;

namespace AvaloniaUI.Pages.Common;

public partial class Common_ItemFormatter_Paginated : UserControl, IItemFormatterBase
{
    public const int ENTRY_SIZE = 150;
    public const int ENTRIES_PER_PAGE = 50;

    private Common_ItemViewer? viewer;

    private Func<long, Task>? sidePanelHandler;
    private Func<DataFetchRequest, Task<DataFetchResponse>>? dataFetcher;

    private Common_Wallpaper[] generatedUI;
    private IWorkshopEntry[]? entries;

    private int currentPage = 0;

    public long? currentlySelectedWallpaper
    {
        set
        {
            m_currentlySelectedWallpaper = value;
            viewer!.grid_SidePanel.IsVisible = m_currentlySelectedWallpaper.HasValue;

            for (int i = 0; i < generatedUI.Length; i++)
            {
                bool isSelected = i < entries?.Length && entries[i].getId == currentlySelectedWallpaper;
                generatedUI[i].ToggleSelection(isSelected);
            }
        }
        get => m_currentlySelectedWallpaper;
    }
    private long? m_currentlySelectedWallpaper;

    public IWorkshopEntry? GetItemByID(long id) => entries?.FirstOrDefault(x => x.getId == id);

    public Common_ItemFormatter_Paginated()
    {
        InitializeComponent();
        generatedUI = new Common_Wallpaper[ENTRIES_PER_PAGE];

        for (int i = 0; i < ENTRIES_PER_PAGE; i++)
        {
            var ui = new Common_Wallpaper();
            ui.Width = ENTRY_SIZE;
            ui.Height = ENTRY_SIZE;

            ui.IsVisible = false;

            generatedUI[i] = ui;
            grid_Content_Container.Children.Add(ui);
        }

        btn_Page_1.RegisterClick(() => ChangePageNumber(-2));
        btn_Page_2.RegisterClick(() => ChangePageNumber(-1));
        btn_Page_3.RegisterClick(() => ChangePageNumber(1));
        btn_Page_4.RegisterClick(() => ChangePageNumber(2));

    }

    public void Setup(Common_ItemViewer viewer, Func<long, Task> sidePanelHandler, Func<DataFetchRequest, Task<DataFetchResponse>> dataFetcher)
    {
        this.viewer = viewer;
        viewer.inp_NameSearch.KeyUp += UpdateFilter;

        this.sidePanelHandler = sidePanelHandler;
        this.dataFetcher = dataFetcher;
    }

    public async Task Draw(bool additive)
    {
        DataFetchResponse res = await dataFetcher!(new DataFetchRequest()
        {
            skip = currentPage,
            take = ENTRIES_PER_PAGE,

            textFilter = viewer!.inp_NameSearch.Text
        });

        entries = res.entries;

        if (entries != null)
        {

            for (int i = 0; i < generatedUI.Length; i++)
            {
                if (i < entries?.Length)
                {
                    generatedUI[i].IsVisible = true;
                    generatedUI[i].StartDraw(entries[i], this);
                }
                else
                {
                    generatedUI[i].IsVisible = false;
                }
            }
        }

        if (res.exception != null)
        {
            lbl_Content_NetworkResponse.IsVisible = true;
            lbl_Content_NetworkResponse.Content = res.exception.Message;
        }
        else
        {
            lbl_Content_NetworkResponse.IsVisible = false;
        }
    }

    public async Task Reset()
    {
        currentPage = 1;
        currentlySelectedWallpaper = null;

        await ChangePageNumber(0);
    }

    public async void SelectWallpaper(long id)
    {
        if (id == currentlySelectedWallpaper)
            return;

        currentlySelectedWallpaper = id;
        await sidePanelHandler!(id);
    }

    private async Task ChangePageNumber(int delta)
    {
        currentPage = Math.Max(0, currentPage + delta);
        currentlySelectedWallpaper = null;

        //btn_Page_1.IsVisible = currentPage > 2;
        //btn_Page_2.IsVisible = currentPage > 1;

        //btn_Page_3.IsVisible = currentPage >= 1; // some how need a way to find out what the end is
        //btn_Page_4.IsVisible = currentPage >= 1; 

        btn_Page_1.Label = (currentPage - 2).ToString();
        btn_Page_2.Label = (currentPage - 1).ToString();
        btn_Page_3.Label = (currentPage + 1).ToString();
        btn_Page_4.Label = (currentPage + 2).ToString();

        inp_CurPage.Text = currentPage.ToString();

        await Draw(false);
    }

    private async void UpdateFilter(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        await ChangePageNumber(0);
    }
}