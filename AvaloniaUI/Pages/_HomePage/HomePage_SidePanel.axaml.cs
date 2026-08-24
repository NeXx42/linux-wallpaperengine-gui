using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaUI.Pages._HomePage.WallpaperProperties;
using AvaloniaUI.Pages.Common;
using CSharpSqliteORM;
using Logic;
using Logic.Data;
using Logic.Database;

namespace AvaloniaUI.Pages._HomePage;

public partial class HomePage_SidePanel : UserControl, ISidebarContent
{
    private Common_Sidebar? master;
    private IWorkshopEntry? selected;

    private readonly IWallpaperProperty[] defaultProps;
    private List<IWallpaperProperty> customProps;

    public HomePage_SidePanel()
    {
        InitializeComponent();

        defaultProps = [
            prop_Clamp.Init(DefaultProps.DefaultProp_Clamp.ToString(), "Clamp", Enum.GetNames(typeof(WallpaperEngine.ClampOptions)), 0),
            prop_Scaling.Init(DefaultProps.DefaultProp_Scaling.ToString(), "Scaling", Enum.GetNames(typeof(WallpaperEngine.ScalingOptions)), 0),

            prop_OffsetX.Init(DefaultProps.DefaultProp_OffsetX.ToString(), "Offset X", -1, 1, 0),
            prop_OffsetY.Init(DefaultProps.DefaultProp_OffsetY.ToString(), "Offset Y", -1, 1, 0),

            prop_BGColour.Init(DefaultProps.DefaultProp_BGColour.ToString(), "Border Colour", Color.FromRgb(0, 0, 0)),
            prop_Contrast.Init(DefaultProps.DefaultProp_Contrast.ToString(), "Contrast", 0, 60, 30),
            prop_Saturation.Init(DefaultProps.DefaultProp_Saturation.ToString(), "Saturation", 0, 60, 30),

            prop_Mute.Init(DefaultProps.DefaultProp_Mute.ToString(), "Mute", false),
            prop_AudioFeedback.Init(DefaultProps.DefaultProp_AudioFeedback.ToString(), "Audio feedback", true),
            prop_AudioFeedback.Init(DefaultProps.DefaultProp_AutoMute.ToString(), "Auto mute", false),

            prop_FullscreenPause.Init(DefaultProps.DefaultProp_PauseOnFullscreen.ToString(), "Pause in fullscreen", true),
            prop_MouseInteraction.Init(DefaultProps.DefaultProp_MouseInteraction.ToString(), "Mouse effects", true),
            prop_Parallax.Init(DefaultProps.DefaultProp_Parallax.ToString(), "Parallax", true),
        ];

        customProps = new List<IWallpaperProperty>();
    }

    public async Task OnSelectWallpaper(Common_Sidebar master, IWorkshopEntry? iEntry)
    {
        selected = iEntry;
        this.master = master;

        master.btn_Act.Label = "Apply Wallpaper";
        master.btn_Act.ClearCallback();
        master.btn_Act.RegisterClick(SetWallpaper);

        master.btn_Act2.Label = "Browse";
        master.btn_Act2.ClearCallback();
        master.btn_Act2.IsVisible = true;
        master.btn_Act2.RegisterClick(BrowseToFolder);

        master.btn_Act3.Label = "Reset";
        master.btn_Act3.ClearCallback();
        master.btn_Act3.IsVisible = true;
        master.btn_Act3.RegisterClick(ResetWallpaperOptions);

        if (iEntry is not WorkshopEntry entry)
            return;

        await entry.Decode();

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

        prop_BGColour.Load(ref options);
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

    public async Task SaveWallpaperOptions(long id)
    {
        List<dbo_WallpaperSettings> props = defaultProps!.Select(x => x.Save(id)).Where(x => x != null).ToList()!;
        props.AddRange(customProps.Select(x => x.Save(id)).Where(x => x != null)!);

        await ConfigManager.SetWallpaperSavedSettings(id, props.ToArray());
    }

    private async Task SetWallpaper()
    {
        if (selected == null)
            return;

        await SaveWallpaperOptions(selected.getId);
        await WallpaperEngine.SetWallpaper(selected.getId);
    }

    private Task BrowseToFolder()
    {
        if (!WorkshopManager.TryGetWallpaperEntry(selected?.getId, out WorkshopEntry entry))
            return Task.CompletedTask;

        new Process() { StartInfo = new ProcessStartInfo { FileName = "xdg-open", Arguments = entry.path, UseShellExecute = false } }.Start();
        return Task.CompletedTask;
    }

    private async Task ResetWallpaperOptions()
    {
        if (selected == null)
            return;

        await DatabaseManager.db!.Delete<dbo_WallpaperSettings>(SQLFilter.Equal(nameof(dbo_WallpaperSettings.wallpaperId), selected.getId));
        await (master?.Draw(selected) ?? Task.CompletedTask);
    }
}