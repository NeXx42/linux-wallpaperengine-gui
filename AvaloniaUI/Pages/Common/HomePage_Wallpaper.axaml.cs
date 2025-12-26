using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using AvaloniaUI.Pages.Common;
using AvaloniaUI.Utils;
using Logic.Data;

namespace AvaloniaUI.Pages.Common;

public partial class Common_Wallpaper : UserControl
{
    private static Thickness? unselectedThickness;
    private static Thickness? selectedThickness;
    private static ImmutableSolidColorBrush? selectedBrush;

    private long? representingId;
    private IItemFormatterBase? master;

    public Common_Wallpaper()
    {
        InitializeComponent();

        border.PointerPressed += (_, __) => HandleSelection();
    }

    public async void StartDraw(IWorkshopEntry entry, IItemFormatterBase master)
    {
        this.master = master;
        this.representingId = entry.getId;
        lbl_Title.Content = entry.getTitle;

        img_Icon.Background = null;
        ImageBrush? brush = await ImageFetcher.GetIcon(entry);
        img_Icon.Background = brush;
    }

    private void HandleSelection()
    {
        if (master == null || !representingId.HasValue)
            return;

        master.SelectWallpaper(representingId.Value);
    }

    public void ToggleSelection(bool to)
    {
        unselectedThickness ??= new Thickness(0);
        selectedThickness ??= new Thickness(2);
        selectedBrush ??= new ImmutableSolidColorBrush(Color.FromRgb(0, 255, 0));

        border.BorderThickness = to ? selectedThickness.Value : unselectedThickness.Value;
        border.BorderBrush = to ? selectedBrush : null;
    }
}