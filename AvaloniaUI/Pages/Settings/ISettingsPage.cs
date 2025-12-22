using System.Threading.Tasks;
using Avalonia.Controls;

namespace AvaloniaUI.Pages.Settings;

public interface ISettingsPage
{
    public UserControl Setup();

    public Task OnOpen();
    public void Close();
}
