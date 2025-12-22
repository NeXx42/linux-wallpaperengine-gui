using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaUI.Common;
using AvaloniaUI.Pages.Settings;
using Logic;

namespace AvaloniaUI.Pages;

public partial class SettingsPage : UserControl
{
    private int? currentPage;
    private (Common_Button btn, ISettingsPage page)[] pages;

    public SettingsPage()
    {
        InitializeComponent();

        pages = [
            (btn_General, new SettingsPage_SettingsGroup_General()),
            (btn_Displays, new SettingsPage_SettingsGroup_Display()),
            (btn_Directories, new SettingsPage_SettingsGroup_Directories()),
        ];

        for (int i = 0; i < pages.Length; i++)
        {
            int temp = i;

            cont_settingsGroups.Children.Add(pages[i].page.Setup());
            pages[i].btn.RegisterClick(() => OpenPage(temp));
        }
    }

    public async void OnOpen()
    {
        await OpenPage(currentPage ?? 0);
    }

    private async Task OpenPage(int page)
    {
        currentPage = page;

        for (int i = 0; i < pages.Length; i++)
            pages[i].page.Close();

        // do this after the fact so that if it loads later pages are already hidden
        await pages[currentPage.Value].page.OnOpen();
    }
}