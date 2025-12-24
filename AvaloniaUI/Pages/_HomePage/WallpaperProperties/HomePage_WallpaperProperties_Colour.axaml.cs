using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Logic.Data;
using Logic.Database;

namespace AvaloniaUI.Pages._HomePage.WallpaperProperties;

public partial class HomePage_WallpaperProperties_Colour : UserControl, IWallpaperProperty
{
    private string? key;
    private bool isDirty = false;

    private Color? defaultColour;
    private Color? currentColour;

    public string? StringColour => currentColour != null ? GetColourAsString(currentColour.Value) : string.Empty;
    public string? GetColourAsString(Color c) => $"{c.R / 255f},{c.G / 255f},{c.B / 255f}";

    public HomePage_WallpaperProperties_Colour()
    {
        InitializeComponent();
    }

    public IWallpaperProperty Init(WorkshopEntry.Properties prop)
    {
        Color? parsedColour = null;

        if (prop.value!.StartsWith("#"))
        {
            if (Color.TryParse(prop.value, out Color _c))
                parsedColour = _c;
        }
        else
        {
            string[] channels = prop.value!.Split(" ");

            if (channels.Length == 3)
            {
                byte[] comps = channels.Select(x => (byte)Math.Round(double.Parse(x) * 255)).ToArray();
                parsedColour = Color.FromRgb(comps[0], comps[1], comps[2]);
            }
        }

        return Init(prop.propertyName!, prop.text!, parsedColour ?? Color.FromRgb(0, 0, 0));
    }

    public IWallpaperProperty Init(string settingName, string title, Color defaultColour)
    {
        key = settingName;
        lbl.Content = title;

        this.defaultColour = defaultColour;

        inp.KeyUp += (_, __) => UpdateColour();
        inp.Text = ColorToHex(defaultColour);

        UpdateColour();
        isDirty = false;

        return this;
    }

    public void Load(ref Dictionary<string, string?> options)
    {
        if (!options.TryGetValue(key!, out string? res) || string.IsNullOrEmpty(res))
        {
            inp.Text = ColorToHex(defaultColour ?? Color.FromRgb(0, 0, 0));
            isDirty = false;
            return;
        }

        isDirty = true;
        inp.Text = res;

        UpdateColour();
    }

    public dbo_WallpaperSettings? Save(long id)
    {
        if (currentColour == null || !isDirty)
            return null;

        return new dbo_WallpaperSettings()
        {
            wallpaperId = id,
            settingKey = key!,
            settingValue = ColorToHex(currentColour.Value)
        };
    }

    // avalonia puts alpha first?
    private static string ColorToHex(Color colour)
    {
        return $"{colour.R:X2}{colour.G:X2}{colour.B:X2}";
    }

    private void UpdateColour()
    {
        string hexCol = $"#{inp.Text?.Replace("#", string.Empty) ?? string.Empty}";

        if (Color.TryParse(hexCol, out Color c))
        {
            currentColour = c;
            inp.Background = new ImmutableSolidColorBrush(currentColour.Value);

            isDirty = true;
        }
    }

    public string? CreateArgument()
    {
        if (!isDirty || currentColour == null)
            return null;

        return $"{key!}={GetColourAsString(currentColour.Value)}";
    }
}