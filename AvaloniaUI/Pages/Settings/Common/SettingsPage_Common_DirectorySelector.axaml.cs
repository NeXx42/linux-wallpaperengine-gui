using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Logic;
using Logic.db;

namespace AvaloniaUI.Pages.Settings.Common;

public partial class SettingsPage_Common_DirectorySelector : UserControl
{
    private ConfigManager.ConfigKeys? configKey;
    private Func<string?, Task>? callback;

    private string? selectedDir
    {
        set
        {
            m_selectedDir = value;
            btn_Select.Label = value ?? "Select Location";
        }
        get => m_selectedDir;
    }
    private string? m_selectedDir;

    public SettingsPage_Common_DirectorySelector()
    {
        InitializeComponent();
    }

    public UserControl Init(string lbl, ConfigManager.ConfigKeys key, Func<string?, string?>? process = null)
    {
        configKey = key;
        return Init(lbl, (p) => SaveToConfig(p, process));
    }

    public UserControl Init(string lbl, Func<string?, Task> onUpdate)
    {
        this.callback = onUpdate;
        lbl_Loc.Content = lbl;

        btn_Reset.RegisterClick(() => OnManuallySelectedOption(null));
        btn_Select.RegisterClick(OpenFolderPicker);

        return this;
    }

    public async Task LoadFromConfig()
    {
        if (configKey == null)
            return;

        dbo_Config? key = await ConfigManager.GetConfigValue(configKey.Value);

        if (key != null)
        {
            selectedDir = key.value;
        }
    }

    private async Task SaveToConfig(string? selectedVal, Func<string?, string?>? process)
    {
        if (configKey == null)
            return;

        selectedDir = process != null ? process(selectedVal) : selectedVal;
        await ConfigManager.SetConfigValue(configKey.Value, selectedDir);
    }

    private async Task OnManuallySelectedOption(string? opt)
    {
        selectedDir = opt;

        if (callback != null)
            await callback(selectedDir);
    }

    private async Task OpenFolderPicker()
    {
        var result = (await MainWindow.instance!.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            AllowMultiple = false,
            Title = "Start up script storage location"
        })).FirstOrDefault();

        if (result != null)
        {
            await OnManuallySelectedOption(result.Path.AbsolutePath);
        }
    }
}