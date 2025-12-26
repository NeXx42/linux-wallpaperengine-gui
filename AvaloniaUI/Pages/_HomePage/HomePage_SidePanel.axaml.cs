using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaUI.Pages._HomePage.WallpaperProperties;
using Logic;
using Logic.Data;
using Logic.Database;

namespace AvaloniaUI.Pages._HomePage;

public partial class HomePage_SidePanel : UserControl
{
    public const string DEFAULT_SCALING_NAME = "Default";

    private readonly IWallpaperProperty[] defaultProps;
    private List<IWallpaperProperty> customProps;

    public HomePage_SidePanel()
    {
        InitializeComponent();

        defaultProps = [
            prop_Clamp.Init(nameof(prop_Clamp), "Clamp", Enum.GetNames(typeof(WallpaperSetter.ClampOptions)), 0),
            prop_Scaling.Init(nameof(prop_Scaling), "Scaling", [DEFAULT_SCALING_NAME, .. Enum.GetNames(typeof(WallpaperSetter.ScalingOptions))], 0),

            prop_OffsetX.Init(nameof(prop_OffsetX), "Offset X", -1, 1, 0),
            prop_OffsetY.Init(nameof(prop_OffsetY), "Offset Y", -1, 1, 0),

            prop_BGColour.Init(nameof(prop_BGColour), "Border Colour", Color.FromRgb(0, 0, 0)),
            prop_Contrast.Init(nameof(prop_Contrast), "Contrast", -4, 5, 1),
            prop_Saturation.Init(nameof(prop_Saturation), "Saturation", -4, 5, 1),
        ];

        customProps = new List<IWallpaperProperty>();
    }

    public async Task OnSelectWallpaper(WorkshopEntry entry)
    {
        Dictionary<string, string?> savedSettings = (await ConfigManager.GetWallpaperSettings(entry.id)).ToDictionary(x => x.settingKey, x => x.settingValue);

        DrawDefaultProperties(ref savedSettings);
        DrawWallpaperProperties(entry.properties?.OrderBy(x => x.order), ref savedSettings);
    }

    private void DrawDefaultProperties(ref Dictionary<string, string?> options)
    {
        prop_Clamp.Load(ref options);
        prop_Scaling.Load(ref options);

        prop_OffsetX.Load(ref options);
        prop_OffsetY.Load(ref options);

        prop_Contrast.Load(ref options);
        prop_Saturation.Load(ref options);
    }

    private void DrawWallpaperProperties(IEnumerable<WorkshopEntry.Properties>? props, ref Dictionary<string, string?> options)
    {
        customProps.Clear();

        if (!(props?.Count() > 0))
        {
            cont_SidePanel_CustomPropertiesGroup.IsVisible = false;
            return;
        }

        cont_SidePanel_CustomPropertiesGroup.IsVisible = true;
        cont_SidePanel_CustomProperties.Children.Clear();

        if (props == null)
            return;

        IWallpaperProperty? ui;

        foreach (WorkshopEntry.Properties prop in props)
        {
            if (prop.type == WorkshopEntry.PropertyType.label)
            {
                TextBlock l = new TextBlock();
                l.TextWrapping = TextWrapping.Wrap;
                l.Text = Regex.Replace(prop.text?.Replace("<br>", "\n").Replace("<br/>", "\n") ?? string.Empty, "<.*?>", string.Empty);

                cont_SidePanel_CustomProperties.Children.Add(l);
                continue;
            }

            ui = GetWallpaperPropertyUI(prop.type ?? WorkshopEntry.PropertyType.INVALID);

            if (ui == null)
                continue;


            ui.Init(prop);
            ui.Load(ref options);

            customProps.Add(ui);
            cont_SidePanel_CustomProperties.Children.Add((ui as UserControl)!);
        }
    }

    private IWallpaperProperty? GetWallpaperPropertyUI(WorkshopEntry.PropertyType type)
    {
        switch (type)
        {
            case WorkshopEntry.PropertyType.colour: return new HomePage_WallpaperProperties_Colour();
            case WorkshopEntry.PropertyType.boolean: return new HomePage_WallpaperProperties_Bool();
            case WorkshopEntry.PropertyType.combo: return new HomePage_WallpaperProperties_Combo();
            case WorkshopEntry.PropertyType.text_input: return new HomePage_WallpaperProperties_TextInput();
            case WorkshopEntry.PropertyType.slider: return new HomePage_WallpaperProperties_Slider();

                // case WorkshopEntry.PropertyType.scene_texture: return new HomePage_WallpaperProperties_SceneTexture(); // i dont even know what this is
        }

        return null;
    }

    public WallpaperSetter.WallpaperOptions GetWallpaperOptions()
    {
        WallpaperSetter.WallpaperOptions options = new WallpaperSetter.WallpaperOptions();
        options.scalingOption = prop_Scaling.SelectedIndex - 1 >= 0 ? (WallpaperSetter.ScalingOptions)(prop_Scaling.SelectedIndex - 1) : null;
        options.clampOptions = (WallpaperSetter.ClampOptions)prop_Clamp.SelectedIndex;

        options.contrast = prop_Contrast.Value;
        options.saturation = prop_Saturation.Value;
        options.borderColour = prop_BGColour.StringColour;

        options.screens = WallpaperSetter.WorkOutScreenOffsets((float)prop_OffsetX.Value, (float)prop_OffsetY.Value);
        options.customProperties = customProps?.Select(x => x.CreateArgument()).Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToArray();

        return options;
    }

    public async Task SaveWallpaperOptions(long id)
    {
        List<dbo_WallpaperSettings> props = defaultProps!.Select(x => x.Save(id)).Where(x => x != null).ToList()!;
        props.AddRange(customProps.Select(x => x.Save(id)).Where(x => x != null)!);

        await ConfigManager.SetWallpaperSavedSettings(id, props.ToArray());
    }
}