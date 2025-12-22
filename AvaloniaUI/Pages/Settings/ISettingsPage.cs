using System.Threading.Tasks;

namespace AvaloniaUI.Pages.Settings;

public interface ISettingsPage
{
    public Task OnOpen();
    public void Close();
}
