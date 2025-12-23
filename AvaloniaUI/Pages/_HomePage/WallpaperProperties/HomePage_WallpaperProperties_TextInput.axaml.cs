using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Logic.Data;
using Logic.Database;

namespace AvaloniaUI.Pages._HomePage.WallpaperProperties;

public partial class HomePage_WallpaperProperties_TextInput : UserControl, IWallpaperProperty
{
    private string? key;
    private bool isDirty = false;

    public HomePage_WallpaperProperties_TextInput()
    {
        InitializeComponent();
    }

    public IWallpaperProperty Init(WorkshopEntry.Properties prop)
    {
        key = prop.propertyName;
        lbl.Content = prop.text;

        inp.KeyUp += (_, __) => isDirty = true;

        return this;
    }

    public void Load(ref Dictionary<string, string?> options)
    {
        if (!options.TryGetValue(key!, out string? res) || string.IsNullOrEmpty(res))
        {
            isDirty = false;
            return;
        }

        isDirty = true;
        inp.Text = res;
    }

    public dbo_WallpaperSettings? Save(long id)
    {
        if (!isDirty)
            return null;

        return new dbo_WallpaperSettings()
        {
            wallpaperId = id,
            settingKey = key!,
            settingValue = inp.Text
        };
    }

    public string? CreateArgument()
    {
        if (!isDirty)
            return null;

        return $"{key!}={inp.Text}";
    }
}