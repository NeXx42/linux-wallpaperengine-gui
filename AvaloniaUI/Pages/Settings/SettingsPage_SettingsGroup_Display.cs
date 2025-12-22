using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaUI.Pages.Settings.Display;

namespace AvaloniaUI.Pages.Settings;

public class SettingsPage_SettingsGroup_Display : ISettingsPage
{
    private SettingsPage_SettingsGroupContainer? ui;

    private SettingsPage_Display_DisplayGroup? displayGroup;

    public UserControl Setup()
    {
        ui = new SettingsPage_SettingsGroupContainer();
        ui.lbl_SettingsName.Content = "Display Settings";

        displayGroup = new SettingsPage_Display_DisplayGroup();
        ui.content.Children.Add(displayGroup);

        return ui;
    }

    public async Task OnOpen()
    {
        ui!.IsVisible = true;

        await displayGroup!.Draw();
    }

    public void Close()
    {
        ui!.IsVisible = false;
    }
}
