using System;
using System.Threading.Tasks;
using Logic.Data;

namespace AvaloniaUI.Pages.Common;

public interface IItemFormatterBase
{
    public long? currentlySelectedWallpaper { get; set; }

    public void Setup(Common_ItemViewer viewer, Func<long, Task> sidePanelHandler, Func<DataFetchRequest, Task<DataFetchResponse>> dataFetcher);
    public Task Reset();

    public Task Draw(bool additive);
    public void SelectWallpaper(long id);
}