using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using AvaloniaUI.Utils;
using Logic;
using Logic.Data;

namespace AvaloniaUI.Pages.SteamWorkshop;

public partial class SteamWorkshop_Wallpaper : UserControl
{
    private static Thickness? unselectedThickness;
    private static Thickness? selectedThickness;
    private static ImmutableSolidColorBrush? selectedBrush;

    private int index;
    private SteamWorkshopPage? master;

    public SteamWorkshop_Wallpaper()
    {
        InitializeComponent();

        img_Icon.PointerPressed += (_, __) => HandleSelection();
    }

    public void Setup(int index)
    {
        this.index = index;
    }

    public async void StartDraw(SteamWorkshopEntry entry, SteamWorkshopPage master)
    {
        this.master = master;

        lbl_Title.Content = entry.name;
        img_Icon.Background = null;

        //await entry.DecodeBasic();
        //lbl_Title.Content = entry.title;

        ImageBrush? brush = await ImageFetcher.GetIcon(entry);
        img_Icon.Background = brush;
    }

    private void HandleSelection()
    {
        master!.ViewWallpaper(index);
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