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

    private bool isSetup = false;
    private int loadedPages = 0;
    private string? cachedNameFilter;

    private readonly IWallpaperProperty[] defaultProps;
    private List<IWallpaperProperty> customProps;

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
        inp_NameSearch.KeyUp += (_, __) => UpdateFilter();

    }

    public async void LoadPage()
    {
        if (isSetup)
            return;

        isSetup = true;

        currentlySelectedWallpaper = null;

        await MainWindow.AsyncLoad(WorkshopManager.RefreshLocalEntries);
        DrawWallpapers(false);
    }

    private void SetupBasicOptions()
    {
        btn_SidePanel_Set.RegisterClick(SetWallpaper);
        btn_SidePanel_Browse.RegisterClick(BrowseToFolder);

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
        if (id == currentlySelectedWallpaper)
        {
            currentlySelectedWallpaper = null;
            return;
        }

        currentlySelectedWallpaper = id;

        scroll_SidePanel.ScrollToHome();
        cont_SidePanel_Tags.Children.Clear();
        img_SidePanel_Icon.Background = null;
        lbl_SidePanel_Title.Content = "";

        if (!WorkshopManager.TryGetWallpaperEntry(id, out WorkshopEntry? entry))
            return;

        await entry!.Decode();

        lbl_SidePanel_Title.Content = entry.title;
        DrawTags(entry.tags);

        Dictionary<string, string?> savedSettings = (await ConfigManager.GetWallpaperSettings(id)).ToDictionary(x => x.settingKey, x => x.settingValue);

        DrawDefaultProperties(ref savedSettings);
        DrawWallpaperProperties(entry?.properties?.OrderBy(x => x.order), ref savedSettings);

        img_SidePanel_Icon.Background = await ImageFetcher.GetIcon(entry!);
    }

    private void DrawTags(string[]? tags)
    {
        if (tags == null)
            return;

        foreach (string tag in tags)
        {
            HomePage_Tag tagUI = new HomePage_Tag();
            tagUI.tagName.Content = tag;

            cont_SidePanel_Tags.Children.Add(tagUI);
        }
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

    private void BrowseToFolder()
    {
        if (currentlySelectedWallpaper == null || !WorkshopManager.TryGetWallpaperEntry(currentlySelectedWallpaper.Value, out WorkshopEntry? entry))
            return;

        new Process() { StartInfo = new ProcessStartInfo { FileName = "xdg-open", Arguments = entry!.path, UseShellExecute = false } }.Start();
    }

    private async Task SetWallpaper()
    {
        if (currentlySelectedWallpaper == null || !WorkshopManager.TryGetWallpaperEntry(currentlySelectedWallpaper.Value, out WorkshopEntry? entry))
            return;

        WallpaperSetter.WallpaperOptions options = new WallpaperSetter.WallpaperOptions();
        options.scalingOption = prop_Scaling.SelectedIndex - 1 >= 0 ? (WallpaperSetter.ScalingOptions)(prop_Scaling.SelectedIndex - 1) : null;
        options.clampOptions = (WallpaperSetter.ClampOptions)prop_Clamp.SelectedIndex;

        options.contrast = prop_Contrast.Value;
        options.saturation = prop_Saturation.Value;
        options.borderColour = prop_BGColour.StringColour;

        options.screens = WallpaperSetter.WorkOutScreenOffsets((float)prop_OffsetX.Value, (float)prop_OffsetY.Value);
        options.customProperties = customProps?.Select(x => x.CreateArgument()).Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToArray();

        await WallpaperSetter.SetWallpaper(entry!.path, options);
        await SaveWallpaperSettings(currentlySelectedWallpaper.Value);
    }

    private async Task SaveWallpaperSettings(long id)
    {
        List<dbo_WallpaperSettings> props = defaultProps!.Select(x => x.Save(id)).Where(x => x != null).ToList()!;
        props.AddRange(customProps.Select(x => x.Save(id)).Where(x => x != null)!);

        await ConfigManager.SetWallpaperSavedSettings(id, props.ToArray());
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