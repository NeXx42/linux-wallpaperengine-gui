using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Text.RegularExpressions;
using CSharpSqliteORM;
using Logic.Data;
using Logic.Database;
using Logic.db;
using Logic.Enums;

namespace Logic;

public enum DefaultProps
{
    DefaultProp_Clamp,
    DefaultProp_Scaling,

    DefaultProp_OffsetX,
    DefaultProp_OffsetY,

    DefaultProp_BGColour,
    DefaultProp_Contrast,
    DefaultProp_Saturation,

    DefaultProp_Mute,
    DefaultProp_AudioFeedback,
    DefaultProp_AutoMute,

    DefaultProp_MouseInteraction,
    DefaultProp_Parallax,
    DefaultProp_PauseOnFullscreen,

    DefaultSetting_LastUsedDate
}

public static class WallpaperEngine
{
    private static long? activeWallpaper;

    public static Action? OnEngineStatusChange;
    public static Action<long?>? OnActiveWallpaperChange;

    public static long? getActiveWallpaper => activeWallpaper;

    public static async Task Init(bool fullLoad)
    {
        if (fullLoad)
        {
            dbo_Config? config = await ConfigManager.GetConfigValue(ConfigManager.ConfigKeys.LastSetWallpaper);

            if (config?.value != null && long.TryParse(config?.value, out long id))
                activeWallpaper = id;
        }
    }

    public static async Task InstallEngineLocally(string fork, string path, IProgress<int> progress, IProgress<int> taskProgress, Action<string> console)
    {
        const string folderName = "linux-wallpaperengine";

        try
        {
            progress.Report(0);
            await RunCommand(path, "git", "clone", "--recursive", fork, folderName, "--progress");
            progress.Report(1);
            taskProgress.Report(0);

            await RunCommand(Path.Combine(path, folderName), "mkdir", "build");
            await RunCommand(Path.Combine(path, folderName, "build"), "cmake", "-DCMAKE_BUILD_TYPE='Release'", "..");

            progress.Report(2);
            taskProgress.Report(0);

            await RunCommand(Path.Combine(path, folderName, "build"), "make");
        }
        catch
        {
            return;
        }

        await DatabaseManager.db!.AddOrUpdate(new dbo_Config()
        {
            key = ConfigManager.ConfigKeys.ExecutableLocation.ToString(),
            value = Path.Combine(path, folderName, "build", "output")
        }, SQLFilter.Equal(nameof(dbo_Config.key), ConfigManager.ConfigKeys.ExecutableLocation.ToString()));

        await DatabaseManager.db!.AddOrUpdate(new dbo_Config()
        {
            key = ConfigManager.ConfigKeys.ExecutableType.ToString(),
            value = ((int)EngineType.Directory).ToString()
        }, SQLFilter.Equal(nameof(dbo_Config.key), ConfigManager.ConfigKeys.ExecutableType.ToString()));

        OnEngineStatusChange?.Invoke();

        async Task RunCommand(string workingDir, string command, params string[] args)
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = command,
                WorkingDirectory = workingDir
            };

            foreach (string arg in args)
                info.ArgumentList.Add(arg);

            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            Process p = new Process();

            p.OutputDataReceived += UpdateProgress;
            p.ErrorDataReceived += UpdateProgress;

            p.StartInfo = info;
            p.Start();

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            await p.WaitForExitAsync();
        }

        void UpdateProgress(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;

            console?.Invoke(e.Data);
            var match = Regex.Match(e.Data, @"\d+%");

            if (match.Success)
                taskProgress.Report(int.Parse(match.Value.Replace("%", "")));
        }
    }

    public static async Task<string> TryFindExecutableLocation()
    {
        dbo_Config? engineType = await ConfigManager.GetConfigValue(ConfigManager.ConfigKeys.ExecutableType);
        dbo_Config? enginePath = await ConfigManager.GetConfigValue(ConfigManager.ConfigKeys.ExecutableLocation);

        if (engineType == null || enginePath == null)
        {
            throw new Exception("Invalid install of the engine. Either set the location or command");
        }

        const string exeDir = "linux-wallpaperengine";

        if (engineType?.value == ((int)EngineType.Directory).ToString())
        {
            string dir = Path.Combine(enginePath.value!, exeDir);

            if (!File.Exists(dir))
                throw new Exception($"Could not find install at the path {dir}");

            return dir;
        }

        return enginePath.value ?? exeDir;
    }

    public static WallpaperOptions.ScreenSettings[] WorkOutScreenOffsets(float offsetX, float offsetY)
    {
        ConfigManager.Screen[] screens = ConfigManager.GetScreensOrdered();
        WallpaperOptions.ScreenSettings[] res = new WallpaperOptions.ScreenSettings[screens.Length];

        float midWay = (screens.Length - 1) * .5f;
        float xDivision = 1f / screens.Length;

        for (int i = 0; i < screens.Length; i++)
        {
            res[i] = new WallpaperOptions.ScreenSettings()
            {
                screenName = screens[i].screenName,
                offsetX = (xDivision * (i - midWay)) + offsetX,
                offsetY = offsetY
            };
        }

        return res;
    }

    public static async Task SetWallpaper(long? id)
    {
        string command = await TryFindExecutableLocation();

        if (!id.HasValue)
        {
            string? lastSet = (await ConfigManager.GetConfigValue(ConfigManager.ConfigKeys.LastSetWallpaper))?.value;
            lastSet.TryParseLong(out id);
        }

        if (!id.HasValue)
        {
            throw new Exception($"Couldn't find wallpaper - {id}");
        }

        dbo_WallpaperSettings[] savedValues = await ConfigManager.GetWallpaperSettings(id.Value);
        WallpaperOptions options = new WallpaperOptions(savedValues);

        await KillExistingRuns(command);

        LinkedList<string> args = options.CreateArgList();
        args.AddLast(id.ToString()!);

        ProcessStartInfo info = new ProcessStartInfo()
        {
            FileName = command,

            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (ConfigManager.IsFlatpak)
        {
            string xdgSession = info.EnvironmentVariables["XDG_SESSION_TYPE"] ?? "wayland";
            LoggingManager.LogMessage($"Using XDG_SESSION_TYPE={xdgSession}");

            // cannot for get this to work...
            info.FileName = "flatpak-spawn";
            info.ArgumentList.Add("--host");
            info.ArgumentList.Add($"--setenv=XDG_SESSION_TYPE={xdgSession}");
            info.ArgumentList.Add(command);

            foreach (string arg in args)
                info.ArgumentList.Add(arg);
        }
        else
        {
            foreach (string arg in args)
                info.ArgumentList.Add(arg);
        }

        try
        {
            Process p = new Process()
            {
                StartInfo = info,
                EnableRaisingEvents = true
            };

            LoggingManager.LogMessage($"Starting engine with following parameters - {string.Join(" ", args)}");

            p.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    LoggingManager.LogError(e.Data);
            };

            p.Exited += (sender, e) =>
            {
                LoggingManager.LogMessage($"Engine exited with code - {((Process?)sender)?.ExitCode}");

                if (!ConfigManager.IsFlatpak)
                    ((Process?)sender)?.Dispose();
            };

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
        }
        catch (Exception e) { LoggingManager.OnException(e); }

        // this isnt initialized when running as a startup
        if (WorkshopManager.TryGetWallpaperEntry(id, out WorkshopEntry wallpaperObj))
        {
            activeWallpaper = id;
            OnActiveWallpaperChange?.Invoke(id);

            dbo_Config? saveScriptLocation = await ConfigManager.GetConfigValue(ConfigManager.ConfigKeys.SaveStartupScriptLocation);

            if (saveScriptLocation != null)
            {
                await SaveCommandToFile(command, args, saveScriptLocation.value);
            }

            await ConfigManager.SetConfigValue(ConfigManager.ConfigKeys.LastSetWallpaper, id.ToString());
            await DatabaseManager.db!.AddOrUpdate(new dbo_WallpaperSettings()
            {
                wallpaperId = id.Value,
                settingKey = DefaultProps.DefaultSetting_LastUsedDate.ToString(),
                settingValue = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
            }, SQLFilter.Equal(nameof(dbo_WallpaperSettings.wallpaperId), id).Equal(nameof(dbo_WallpaperSettings.settingKey), DefaultProps.DefaultSetting_LastUsedDate.ToString()), nameof(dbo_WallpaperSettings.settingValue));

            wallpaperObj.lastUsed = DateTime.UtcNow;
        }
    }

    private static async Task SaveCommandToFile(string command, LinkedList<string> arguments, string? path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        if (File.Exists(path))
            File.Delete(path);

        string? selfContained = Environment.GetEnvironmentVariable("APPIMAGE");

        if (!string.IsNullOrEmpty(selfContained))
        {
            using (var writer = new StreamWriter(path))
            {
                await writer.WriteLineAsync("#!/bin/bash");
                await writer.WriteLineAsync($"{selfContained} --startup");
            }

            GivePermission(path);
            return;
        }

        using (var writer = new StreamWriter(path))
        {
            await writer.WriteLineAsync("#!/bin/bash");
            await writer.WriteAsync(command);

            foreach (string arg in arguments)
            {
                await writer.WriteAsync(" ");
                await writer.WriteAsync(arg);
            }
        }

        GivePermission(path);

        void GivePermission(string p)
        {
#pragma warning disable CA1416 // Validate platform compatibility
            File.SetUnixFileMode(p,
                UnixFileMode.UserRead |
                UnixFileMode.UserWrite |
                UnixFileMode.UserExecute |
                UnixFileMode.GroupRead |
                UnixFileMode.GroupWrite |
                UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead |
                UnixFileMode.OtherWrite |
                UnixFileMode.OtherExecute
            );
#pragma warning restore CA1416 // Validate platform compatibility
        }
    }

    private static async Task KillExistingRuns(string exeName)
    {
        if (ConfigManager.IsFlatpak)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("flatpak-spawn");
                psi.ArgumentList.Add("--host");
                psi.ArgumentList.Add("pkill");
                psi.ArgumentList.Add("-f");
                psi.ArgumentList.Add(exeName);

                Process p = Process.Start(psi)!;
                await p.WaitForExitAsync();
                p.Dispose();
            }
            catch (Exception e) { LoggingManager.OnException(e); }
        }
        else
        {
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    var processExe = process.MainModule?.FileName;
                    if (processExe != null && Path.GetFullPath(processExe) == exeName)
                    {
                        process.Kill();
                        await process.WaitForExitAsync();
                        process.Dispose();
                    }
                }
                catch (Exception e) { LoggingManager.OnException(e); }
            }
        }
    }

    public enum ScalingOptions
    {
        stretch,
        fit,
        fill,
    }

    public enum ClampOptions
    {
        clamp,
        border,
        repeat
    }

    public class WallpaperOptions
    {
        private ClampOptions? clampOptions;
        private ScalingOptions? scalingOption;
        private ScreenSettings[] screens;

        private bool? mute;
        private bool? noAudioProcessing;
        private bool? noAutoMute;

        private bool? noFullscreenPause;
        private bool? disableMouse;
        private bool? disableParallax;

        private double? saturation;
        private double? contrast;
        private string? borderColour;

        private List<string>? customProperties = new List<string>();


        public struct ScreenSettings
        {
            public required string screenName;
            public float? offsetX;
            public float? offsetY;
        }

        public WallpaperOptions(dbo_WallpaperSettings[] overrideOptions)
        {
            float offsetX = 0;
            float offsetY = 0;

            foreach (dbo_WallpaperSettings setting in overrideOptions)
            {
                if (Enum.TryParse(setting.settingKey, out DefaultProps prop))
                {
                    switch (prop)
                    {
                        case DefaultProps.DefaultProp_Clamp: clampOptions = (ClampOptions)int.Parse(setting.settingValue!); break;
                        case DefaultProps.DefaultProp_Scaling: scalingOption = (ScalingOptions)int.Parse(setting.settingValue!); break;
                        case DefaultProps.DefaultProp_OffsetX: float.TryParse(setting.settingValue, out offsetX); break;
                        case DefaultProps.DefaultProp_OffsetY: float.TryParse(setting.settingValue, out offsetY); break;


                        case DefaultProps.DefaultProp_BGColour: borderColour = setting.settingValue; break;

                        case DefaultProps.DefaultProp_Contrast: setting.settingValue.TryParseDouble(out contrast); break;
                        case DefaultProps.DefaultProp_Saturation: setting.settingValue.TryParseDouble(out saturation); break;

                        case DefaultProps.DefaultProp_Mute: mute = setting.settingValue == "1"; break;
                        case DefaultProps.DefaultProp_AudioFeedback: noAudioProcessing = setting.settingValue == "0"; break;
                        case DefaultProps.DefaultProp_AutoMute: noAutoMute = setting.settingValue == "0"; break;

                        case DefaultProps.DefaultProp_PauseOnFullscreen: noFullscreenPause = setting.settingValue == "0"; break;
                        case DefaultProps.DefaultProp_MouseInteraction: disableMouse = setting.settingValue == "0"; break;
                        case DefaultProps.DefaultProp_Parallax: disableParallax = setting.settingValue == "0"; break;
                    }

                    continue;
                }

                customProperties.Add($"{setting.settingKey}={setting.settingValue}");
            }

            screens = WorkOutScreenOffsets(-offsetX, -offsetY);
        }


        public LinkedList<string> CreateArgList(params string[] injectedArgs)
        {
            LinkedList<string> args = new LinkedList<string>();

            foreach (string arg in injectedArgs)
                args.AddLast(arg);

            TryToAddProp(scalingOption, "--scaling", a => a.ToString()!);
            TryToAddProp(clampOptions ?? ClampOptions.clamp, "--clamp", a => a.ToString()!);

            foreach (ScreenSettings screen in screens)
            {
                TryToAddProp(screen.screenName, "--screen-root", Serialize_String);
                TryToAddProp(screen.offsetX, "--offset-x", Serialize_Float);
                TryToAddProp(screen.offsetY, "--offset-y", Serialize_Float);
            }

            TryToAddProp(contrast, "--contrast", a => (.5f + (contrast / 60) * 1.5f).ToString()!);
            TryToAddProp(saturation, "--saturation", a => (saturation / 60f * 1.4f).ToString()!);
            TryToAddProp(borderColour, "--border-colour", Serialize_String);

            TryToAddSetting(mute ?? false, "--silent");
            TryToAddSetting(noAudioProcessing ?? false, "--no-audio-processing");
            TryToAddSetting(noAutoMute ?? false, "--noautomute");

            TryToAddSetting(noFullscreenPause ?? false, "--no-fullscreen-pause");
            TryToAddSetting(disableMouse ?? false, "--disable-mouse");
            TryToAddSetting(disableParallax ?? false, "--disable-parallax");

            if (customProperties != null)
            {
                foreach (string arg in customProperties)
                    TryToAddProp(arg, "--set-property", a => a);
            }

            args.AddLast("--bg");
            return args;

            void TryToAddProp<T>(T? value, string key, Func<T, string> getValue)
            {
                if (value == null)
                    return;

                args.AddLast(key);
                args.AddLast(getValue(value!));
            }

            void TryToAddSetting(bool val, string key)
            {
                if (!val)
                    return;

                args.AddLast(key);
            }
        }

        private string Serialize_String(string? s) => s!;
        private string Serialize_Bool(bool? b) => b!.Value ? "1" : "0";
        private string Serialize_Int(int? i) => i!.Value.ToString();
        private string Serialize_Float(float? i) => i!.Value.ToString();
    }

}
