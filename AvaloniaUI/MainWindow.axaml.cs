using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
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

        btn_Settings.RegisterClick(() => ToggleSettings(true));

        cont_SettingsContainer.PointerPressed += (_, __) => ToggleSettings(false);
        Page_Settings.PointerPressed += (_, e) => e.Handled = true;

        ToggleSettings(false);

        btn_Library.RegisterClick(() => OpenMenu(nameof(btn_Library)));
        btn_Workshop.RegisterClick(() => OpenMenu(nameof(btn_Workshop)));

        OpenMenu(nameof(btn_Library));
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

    private void OpenMenu(string to)
    {
        switch (to)
        {
            case nameof(btn_Library):
                Page_HomePage.IsVisible = true;
                Page_WorkshopPage.IsVisible = false;

                Page_HomePage.LoadPage();
                break;

            case nameof(btn_Workshop):
                Page_HomePage.IsVisible = false;
                Page_WorkshopPage.IsVisible = true;

                Page_WorkshopPage.LoadPage();
                break;
        }
    }

    public static async Task AsyncLoad(Func<Task> task)
    {
        instance!.Pages.Effect = instance.blurEffect;
        Dispatcher.UIThread.Post(() => { });

        await task();
        instance.Pages.Effect = null;
    }

    public static async Task<T> AsyncLoad_WithReturn<T>(Func<Task<T>> task)
    {
        instance!.Pages.Effect = instance.blurEffect;
        Dispatcher.UIThread.Post(() => { });

        T res = await task();
        instance.Pages.Effect = null;

        return res;
    }
}