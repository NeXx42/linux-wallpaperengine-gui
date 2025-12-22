using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaUI.Pages.Settings.Common;
using Logic;

namespace AvaloniaUI.Pages.Settings;

public class SettingsPage_SettingsGroup_General : ISettingsPage
{
    private SettingsPage_SettingsGroupContainer? ui;

    private SettingsPage_Common_DirectorySelector? startupScriptDir;

    public UserControl Setup()
    {
        ui = new SettingsPage_SettingsGroupContainer();
        ui.lbl_SettingsName.Content = "General Settings";

        startupScriptDir = new SettingsPage_Common_DirectorySelector();
        ui.content.Children.Add(startupScriptDir.Init("Save startup script to", ConfigManager.ConfigKeys.SaveStartupScriptLocation, (p) => string.IsNullOrEmpty(p) ? string.Empty : Path.Combine(p, "startup.sh")));

        return ui;
    }

    public async Task OnOpen()
    {
        ui!.IsVisible = true;

        await startupScriptDir!.LoadFromConfig();
    }

    public void Close()
    {
        ui!.IsVisible = false;
    }
}
