using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaloniaUI.Pages.Settings.Directories;

public partial class SettingsPage_Directories : UserControl, ISettingsPage
{
    public SettingsPage_Directories()
    {
        InitializeComponent();
    }

    public void Close()
    {
        this.IsVisible = false;
    }

    public Task OnOpen()
    {
        this.IsVisible = true;
        return Task.CompletedTask;
    }
}