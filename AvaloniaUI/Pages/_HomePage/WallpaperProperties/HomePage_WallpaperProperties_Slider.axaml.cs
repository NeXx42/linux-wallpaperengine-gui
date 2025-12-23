using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Logic.Data;
using Logic.Database;

namespace AvaloniaUI.Pages._HomePage.WallpaperProperties;

public partial class HomePage_WallpaperProperties_Slider : UserControl, IWallpaperProperty
{
    private string? key;

    private double? defaultValue;
    private bool isDirty = false;

    public double Value => inp.Value;

    public HomePage_WallpaperProperties_Slider()
    {
        InitializeComponent();
    }

    public IWallpaperProperty Init(WorkshopEntry.Properties prop)
    {
        return Init(prop.propertyName!, prop.text!, (float)prop.min!, (float)prop.max!, float.Parse(prop.value!));
    }

    public IWallpaperProperty Init(string name, string label, float min, float max, float val)
    {
        key = name;
        lbl.Content = label;

        inp.ValueChanged += (_, __) => OnChangeValue();

        defaultValue = val;
        inp.Minimum = min;
        inp.Maximum = max;
        inp.Value = val;

        isDirty = false;
        return this;
    }

    public void Load(ref Dictionary<string, string?> options)
    {
        if (!options.TryGetValue(key!, out string? res) || string.IsNullOrEmpty(res))
        {
            inp.Value = defaultValue ?? 0;
            isDirty = false;

            return;
        }

        isDirty = true;
        inp.Value = double.Parse(res);
    }

    public dbo_WallpaperSettings? Save(long id)
    {
        if (!isDirty)
            return null;

        return new dbo_WallpaperSettings()
        {
            wallpaperId = id,
            settingKey = key!,
            settingValue = inp.Value.ToString(),
        };
    }

    private void OnChangeValue()
    {
        isDirty = true;
        amount.Text = inp.Value.ToString();
    }

    public string? CreateArgument()
    {
        if (!isDirty)
            return null;

        return $"{key!}={inp.Value}";
    }
}