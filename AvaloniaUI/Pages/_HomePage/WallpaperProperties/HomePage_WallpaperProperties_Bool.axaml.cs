using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Logic.Data;
using Logic.Database;

namespace AvaloniaUI.Pages._HomePage.WallpaperProperties;

public partial class HomePage_WallpaperProperties_Bool : UserControl, IWallpaperProperty
{
    private string? key;
    private bool? defaultVal;

    private bool isDirty = false;

    public HomePage_WallpaperProperties_Bool()
    {
        InitializeComponent();
    }

    public IWallpaperProperty Init(WorkshopEntry.Properties prop)
    {
        key = prop.propertyName;

        lbl.Content = prop.text;
        inp.IsChecked = prop.value == "1";

        defaultVal = inp.IsChecked;

        isDirty = false;
        inp.IsCheckedChanged += (_, __) => isDirty = true;

        return this;
    }

    public void Load(ref Dictionary<string, string?> options)
    {
        if (!options.TryGetValue(key!, out string? res) || string.IsNullOrEmpty(res))
        {
            inp.IsChecked = defaultVal;
            isDirty = false;

            return;
        }

        isDirty = true;
        inp.IsChecked = res == "1";
    }

    public dbo_WallpaperSettings? Save(long id)
    {
        if (!isDirty)
            return null;

        return new dbo_WallpaperSettings()
        {
            wallpaperId = id,
            settingKey = key!,
            settingValue = (inp.IsChecked ?? false) ? "1" : "0"
        };
    }

    public string? CreateArgument()
    {
        if (!isDirty)
            return null;

        return $"{key!}={((inp.IsChecked ?? false) ? "1" : "0")}";
    }
}