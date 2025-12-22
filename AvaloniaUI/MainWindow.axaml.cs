using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Logic;

namespace AvaloniaUI;

public partial class MainWindow : Window
{
    public static MainWindow? instance { private set; get; }

    private ImmutableBlurEffect? blurEffect;

    public MainWindow()
    {
        instance = this;

        InitializeComponent();
        RegisterScreens();

        blurEffect = new ImmutableBlurEffect(5);

        btn_Settings.Click += (_, __) => ToggleSettings(true);

        cont_SettingsContainer.PointerPressed += (_, __) => ToggleSettings(false);
        Page_Settings.PointerPressed += (_, e) => e.Handled = true;

        ToggleSettings(false);
    }

    private async void RegisterScreens()
    {
        List<ConfigManager.Screen> unpackedScreens = new List<ConfigManager.Screen>();

        foreach (var screen in this.Screens.All)
        {
            unpackedScreens.Add(new ConfigManager.Screen()
            {
                screenName = screen.DisplayName!
            });
        }

        await ConfigManager.RegisterDisplays(unpackedScreens.ToArray());
    }

    public void ToggleSettings(bool to)
    {
        Pages.Effect = to ? blurEffect : null;
        cont_SettingsContainer.IsVisible = to;

        if (to)
            Page_Settings.OnOpen();
    }

}