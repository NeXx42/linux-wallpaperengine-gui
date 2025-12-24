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
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}