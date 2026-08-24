using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Logic.Data;
using Logic.Database;

namespace AvaloniaUI.Pages._HomePage.WallpaperProperties;

public partial class HomePage_WallpaperProperties_SceneTexture : UserControl, IWallpaperProperty
{
    private string? key;

    public HomePage_WallpaperProperties_SceneTexture()
    {
        InitializeComponent();

        this.Height = HomePage.PROPERTY_DEFAULT_HEIGHT;
        this.lbl.FontSize = HomePage.PROPERTY_DEFAULT_FONT_SIZE;
    }

    public string? CreateArgument()
    {
        throw new System.NotImplementedException();
    }

    public IWallpaperProperty Init(WorkshopEntry.Properties prop)
    {
        key = prop.propertyName;
        lbl.Content = prop.text;

        return this;
    }

    public void Load(ref Dictionary<string, string?> options)
    {
        if (!options.TryGetValue(key!, out string? res) || string.IsNullOrEmpty(res))
            return;
    }

    public dbo_WallpaperSettings? Save(long id)
    {
        // dont know what to do with this yet
        return null;
    }
}