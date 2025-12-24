using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaUI.Pages.SteamWorkshop;
using AvaloniaUI.Utils;
using Logic;
using Logic.Data;

namespace AvaloniaUI.Pages;

public partial class SteamWorkshopPage : UserControl
{
    public const int ENTRY_SIZE = 150;
    public const int ENTRIES_PER_PAGE = 50;

    private bool isSetup = false;
    private int currentPage = 0;

    private SteamWorkshop_Wallpaper[] generatedUI;
    private SteamWorkshopEntry[] workshopWallpaper;

    private int? currentlySelectedWallpaper
    {
        set
        {
            if (m_currentlySelectedWallpaper.HasValue)
            {
                generatedUI[m_currentlySelectedWallpaper.Value].ToggleSelection(false);
            }

            m_currentlySelectedWallpaper = value;
            grid_SidePanel.IsVisible = m_currentlySelectedWallpaper.HasValue;

            if (m_currentlySelectedWallpaper.HasValue)
            {
                generatedUI[m_currentlySelectedWallpaper.Value].ToggleSelection(true);
            }
        }
        get => m_currentlySelectedWallpaper;
    }
    private int? m_currentlySelectedWallpaper;


    public SteamWorkshopPage()
    {
        InitializeComponent();
        generatedUI = new SteamWorkshop_Wallpaper[ENTRIES_PER_PAGE];

        for (int i = 0; i < ENTRIES_PER_PAGE; i++)
        {
            var ui = new SteamWorkshop_Wallpaper();
            ui.Width = ENTRY_SIZE;
            ui.Height = ENTRY_SIZE;

            ui.IsVisible = false;
            ui.Setup(i);

            generatedUI[i] = ui;
            grid_Content_Container.Children.Add(ui);
        }

        btn_Prev.RegisterClick(PrevPage);
        btn_Next.RegisterClick(NextPage);

        btn_SidePanel_Download.RegisterClick(DownloadWallpaper);
    }

    public async void LoadPage()
    {
        if (isSetup)
            return;

        isSetup = true;

        currentlySelectedWallpaper = null;
        await NextPage();
    }

    private async Task PrevPage()
    {
        currentPage--;
        currentlySelectedWallpaper = null;

        await UpdatePage();
    }

    private async Task NextPage()
    {
        currentPage++;
        currentlySelectedWallpaper = null;

        await UpdatePage();
    }

    private async Task UpdatePage()
    {
        lbl_PageNumber.Content = currentPage.ToString();

        (workshopWallpaper, Exception? excep) = await MainWindow.AsyncLoad_WithReturn(async () => await SteamWorkshopManager.FetchItems(new SteamWorkshopManager.WorkshopFilter()
        {
            page = currentPage
        }));

        for (int i = 0; i < generatedUI.Length; i++)
        {
            if (i < workshopWallpaper?.Length)
            {
                generatedUI[i].IsVisible = true;
                generatedUI[i].StartDraw(workshopWallpaper[i], this);
            }
            else
            {
                generatedUI[i].IsVisible = false;
            }
        }

        if (excep != null)
        {
            lbl_Content_NetworkResponse.IsVisible = true;
            lbl_Content_NetworkResponse.Content = excep.Message;
        }
        else
        {
            lbl_Content_NetworkResponse.IsVisible = false;
        }
    }

    public async void ViewWallpaper(int id)
    {
        if (id == currentlySelectedWallpaper)
        {
            currentlySelectedWallpaper = null;
            return;
        }

        currentlySelectedWallpaper = id;

        scroll_SidePanel.ScrollToHome();
        cont_SidePanel_Tags.Children.Clear();
        img_SidePanel_Icon.Background = null;

        SteamWorkshopEntry entry = workshopWallpaper[id];
        lbl_SidePanel_Title.Content = entry.name;

        img_SidePanel_Icon.Background = await ImageFetcher.GetIcon(entry);
    }

    private async Task DownloadWallpaper()
    {
        if (!currentlySelectedWallpaper.HasValue)
            return;


    }
}