using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaloniaUI.Pages.Settings.General;

public partial class SettingsPage_General : UserControl, ISettingsPage
{
    public SettingsPage_General()
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