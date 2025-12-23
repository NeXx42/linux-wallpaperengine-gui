using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaUI.Pages._HomePage;
using AvaloniaUI.Pages._HomePage.WallpaperProperties;
using AvaloniaUI.Utils;
using Logic;
using Logic.Data;
using Logic.Database;

namespace AvaloniaUI.Pages;

public partial class HomePage : UserControl
{
    public const string DEFAULT_SCALING_NAME = "Default";

    public const int ENTRY_SIZE = 150;
    public const int ENTRIES_PER_PAGE = 50;

    private int loadedPages = 0;
    private string? cachedNameFilter;

    private Dictionary<long, HomePage_Wallpaper> cachedWallpaperUI = new Dictionary<long, HomePage_Wallpaper>();

    private long? currentlySelectedWallpaper
    {
        set
        {
            if (m_currentlySelectedWallpaper.HasValue && cachedWallpaperUI.TryGetValue(m_currentlySelectedWallpaper.Value, out HomePage_Wallpaper? ui))
            {
                ui.ToggleSelection(false);
            }

            m_currentlySelectedWallpaper = value;
            grid_SidePanel.IsVisible = m_currentlySelectedWallpaper.HasValue;

            if (m_currentlySelectedWallpaper.HasValue && cachedWallpaperUI.TryGetValue(m_currentlySelectedWallpaper.Value, out ui))
            {
                ui.ToggleSelection(true);
            }
        }
        get => m_currentlySelectedWallpaper;
    }
    private long? m_currentlySelectedWallpaper;

    public HomePage()
    {
        InitializeComponent();
        SetupBasicOptions();

        inp_NameSearch.KeyUp += (_, __) => UpdateFilter();

        if (!Design.IsDesignMode)
        {
            DrawWallpapers(false);
        }
    }

    private void SetupBasicOptions()
    {
        string[] options = [DEFAULT_SCALING_NAME, .. System.Enum.GetNames(typeof(WallpaperSetter.ScalingOptions))];
        inp_SidePanel_Scaling.SelectedIndex = 0;
        inp_SidePanel_Scaling.ItemsSource = options;

        options = System.Enum.GetNames(typeof(WallpaperSetter.ClampOptions));
        inp_SidePanel_Clamp.SelectedIndex = 0;
        inp_SidePanel_Clamp.ItemsSource = options;

        btn_SidePanel_Set.RegisterClick(SetWallpaper);

        inp_SidePanel_OffsetY.Minimum = -1;
        inp_SidePanel_OffsetY.Maximum = 1;

        inp_SidePanel_OffsetX.Minimum = -1;
        inp_SidePanel_OffsetX.Maximum = 1;

        inp_SidePanel_Colours_Contrast.Maximum = 5;
        inp_SidePanel_Colours_Contrast.Value = 1;
        inp_SidePanel_Colours_Contrast.Minimum = -4;

        inp_SidePanel_Colours_Saturation.Maximum = 5;
        inp_SidePanel_Colours_Saturation.Value = 1;
        inp_SidePanel_Colours_Saturation.Minimum = -4;

        btn_LoadMore.RegisterClick(LoadExtraEntries);
    }

    private async void DrawWallpapers(bool additive)
    {
        WorkshopEntry[] wallpapers = WorkshopManager.GetCachedWallpaperEntries(inp_NameSearch.Text, null, loadedPages * ENTRIES_PER_PAGE, ENTRIES_PER_PAGE);

        if (!additive)
            grid_Content_Container.Children.Clear();

        for (int i = 0; i < wallpapers.Length; i++)
        {
            HomePage_Wallpaper ui = GetWallpaperUI(wallpapers[i]);

            ui.StartDraw(wallpapers[i], this);
            grid_Content_Container.Children.Add(ui);
        }

        int maxPages = (int)Math.Ceiling(WorkshopManager.GetWallpaperCount() / (float)ENTRIES_PER_PAGE);
        btn_LoadMore.IsVisible = loadedPages < maxPages - 1;

        HomePage_Wallpaper GetWallpaperUI(WorkshopEntry entry)
        {
            if (cachedWallpaperUI.TryGetValue(entry.id, out HomePage_Wallpaper? cached))
                return cached!;

            HomePage_Wallpaper wallpaperEntry = new HomePage_Wallpaper();
            wallpaperEntry.Height = ENTRY_SIZE;
            wallpaperEntry.Width = ENTRY_SIZE;

            cachedWallpaperUI.Add(entry.id, wallpaperEntry);
            return wallpaperEntry;
        }
    }

    public async void SelectWallpaper(long id)
    {
        currentlySelectedWallpaper = id;

        img_SidePanel_Icon.Background = null;
        lbl_SidePanel_Title.Content = "";

        if (!WorkshopManager.TryGetWallpaperEntry(id, out WorkshopEntry? entry))
            return;

        await entry!.Decode();
        lbl_SidePanel_Title.Content = entry.title;

        Dictionary<string, string?> savedSettings = (await ConfigManager.GetWallpaperSettings(id)).ToDictionary(x => x.settingKey, x => x.settingValue);

        DrawDefaultProperties(ref savedSettings);
        DrawWallpaperProperties(entry.properties, ref savedSettings);

        ImageBrush? brush = await ImageFetcher.GetIcon(id);
        img_SidePanel_Icon.Background = brush;
    }

    private void DrawDefaultProperties(ref Dictionary<string, string?> options)
    {
        LoadValue(nameof(inp_SidePanel_OffsetX), ref inp_SidePanel_OffsetX, ref options, 0);
        LoadValue(nameof(inp_SidePanel_OffsetY), ref inp_SidePanel_OffsetY, ref options, 0);

        LoadValue(nameof(inp_SidePanel_Clamp), ref inp_SidePanel_Clamp, ref options, 0);
        LoadValue(nameof(inp_SidePanel_Scaling), ref inp_SidePanel_Scaling, ref options, 0);

        LoadValue(nameof(inp_SidePanel_Colours_Contrast), ref inp_SidePanel_Colours_Contrast, ref options, 1);
        LoadValue(nameof(inp_SidePanel_Colours_Saturation), ref inp_SidePanel_Colours_Saturation, ref options, 1);
    }

    private void DrawWallpaperProperties(WorkshopEntry.Properties[]? props, ref Dictionary<string, string?> options)
    {
        cont_SidePanel_CustomProperties.Children.Clear();

        if (props == null)
            return;

        IWallpaperProperty? ui;

        foreach (WorkshopEntry.Properties prop in props)
        {
            ui = GetWallpaperPropertyUI(prop.type ?? WorkshopEntry.PropertyType.INVALID);

            if (ui == null)
                continue;

            ui.Draw(prop);
            cont_SidePanel_CustomProperties.Children.Add((ui as UserControl)!);
        }
    }

    private IWallpaperProperty? GetWallpaperPropertyUI(WorkshopEntry.PropertyType type)
    {
        switch (type)
        {
            case WorkshopEntry.PropertyType.colour: return new HomePage_WallpaperProperties_Colour();
                //case WorkshopEntry.PropertyType.boolean: return new HomePage_WallpaperProperties_Bool();
                //case WorkshopEntry.PropertyType.combo: return new HomePage_WallpaperProperties_Combo();
                //case WorkshopEntry.PropertyType.text_input: return new HomePage_WallpaperProperties_TextInput();
                //case WorkshopEntry.PropertyType.scene_texture: return new HomePage_WallpaperProperties_SceneTexture();
        }

        return null;
    }

    private async Task SetWallpaper()
    {
        if (currentlySelectedWallpaper == null || !WorkshopManager.TryGetWallpaperEntry(currentlySelectedWallpaper.Value, out WorkshopEntry? entry))
            return;

        WallpaperSetter.WallpaperOptions options = new WallpaperSetter.WallpaperOptions();
        options.scalingOption = inp_SidePanel_Scaling.SelectedIndex - 1 >= 0 ? (WallpaperSetter.ScalingOptions)(inp_SidePanel_Scaling.SelectedIndex - 1) : null;
        options.clampOptions = (WallpaperSetter.ClampOptions)inp_SidePanel_Clamp.SelectedIndex;

        options.contrast = inp_SidePanel_Colours_Contrast.Value;
        options.saturation = inp_SidePanel_Colours_Saturation.Value;

        options.screens = WallpaperSetter.WorkOutScreenOffsets((float)inp_SidePanel_OffsetX.Value, (float)inp_SidePanel_OffsetY.Value);

        await WallpaperSetter.SetWallpaper(entry!.path, options);
        await SaveWallpaperSettings(currentlySelectedWallpaper.Value);
    }

    private async Task SaveWallpaperSettings(long id)
    {
        dbo_WallpaperSettings[] settings = [
            SaveValue(nameof(inp_SidePanel_OffsetX), inp_SidePanel_OffsetX, id),
            SaveValue(nameof(inp_SidePanel_OffsetY), inp_SidePanel_OffsetY, id),

            SaveValue(nameof(inp_SidePanel_Clamp), inp_SidePanel_Clamp, id),
            SaveValue(nameof(inp_SidePanel_Scaling), inp_SidePanel_Scaling, id),

            SaveValue(nameof(inp_SidePanel_Colours_Contrast), inp_SidePanel_Colours_Contrast, id),
            SaveValue(nameof(inp_SidePanel_Colours_Saturation), inp_SidePanel_Colours_Saturation, id),
        ];

        await ConfigManager.SetWallpaperSavedSettings(id, settings);
    }

    private dbo_WallpaperSettings SaveValue(string settingName, Slider value, long id)
        => new dbo_WallpaperSettings() { settingKey = settingName, settingValue = value.Value.ToString(), wallpaperId = id };

    private void LoadValue(string settingName, ref Slider slider, ref Dictionary<string, string?> vals, double defaultVal)
    {
        if (vals.TryGetValue(settingName!, out string? res) && !string.IsNullOrEmpty(res))
            slider.Value = double.Parse(res);
        else
            slider.Value = defaultVal;
    }

    private dbo_WallpaperSettings SaveValue(string settingName, ComboBox value, long id)
        => new dbo_WallpaperSettings() { settingKey = settingName, settingValue = value.SelectedIndex.ToString(), wallpaperId = id };

    private void LoadValue(string settingName, ref ComboBox slider, ref Dictionary<string, string?> vals, int defaultVal)
    {
        if (vals.TryGetValue(settingName!, out string? res) && !string.IsNullOrEmpty(res))
            slider.SelectedIndex = int.Parse(res);
        else
            slider.SelectedIndex = defaultVal;
    }


    private void LoadExtraEntries()
    {
        loadedPages++;
        DrawWallpapers(true);
    }

    private void UpdateFilter()
    {
        if (cachedNameFilter == inp_NameSearch.Text)
        {
            return;
        }

        cachedNameFilter = inp_NameSearch.Text;

        loadedPages = 0;
        DrawWallpapers(false);
    }
}