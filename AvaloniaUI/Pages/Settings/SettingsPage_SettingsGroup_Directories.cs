using System.Threading.Tasks;
using Avalonia.Controls;

namespace AvaloniaUI.Pages.Settings;

public class SettingsPage_SettingsGroup_Directories : ISettingsPage
{
    private SettingsPage_SettingsGroupContainer? ui;

    public UserControl Setup()
    {
        ui = new SettingsPage_SettingsGroupContainer();
        return ui;
    }

    public Task OnOpen()
    {
        ui!.IsVisible = true;
        return Task.CompletedTask;
    }

    public void Close()
    {
        ui!.IsVisible = false;
    }
}
