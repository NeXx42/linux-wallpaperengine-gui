using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaUI.Utils;
using Logic;

namespace AvaloniaUI;

public partial class App : Application
{
    public override void Initialize()
    {
        SetupDependencies();
        AvaloniaXamlLoader.Load(this);
    }

    private async void SetupDependencies()
    {
        if (!Design.IsDesignMode)
        {
            await ConfigManager.Init();
            await ImageFetchingManager.Init(ImageFetcher.HandleBrushCreation, ImageFetcher.HandleWebBrushCreation);
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.Args?.FirstOrDefault() == "--startup")
            {
                long? specificWallpaper = null;

                if (desktop.Args.Length == 2 && long.TryParse(desktop.Args[1], out long id))
                    specificWallpaper = id;

                ConfigManager.LoadStartupVersion(specificWallpaper);
                return;
            }

            desktop.ShutdownRequested += (sender, e) => LoggingManager.StopLogThread();
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}