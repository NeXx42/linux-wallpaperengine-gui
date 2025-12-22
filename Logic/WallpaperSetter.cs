using System.Diagnostics;
using System.IO.IsolatedStorage;
using CSharpSqliteORM;
using Logic.db;

namespace Logic;

public static class WallpaperSetter
{
    private static string? cachedExecutableLocation;

    public static async Task TryFindExecutableLocation()
    {
        dbo_Config? overridePath = await ConfigManager.GetConfigValue(ConfigManager.ConfigKeys.ExecutableLocation);

        if (overridePath != null)
        {
            cachedExecutableLocation = overridePath.value;
            return;
        }

        const string exeDir = "Engine/linux-wallpaperengine";
        string localPath = Path.Combine(AppContext.BaseDirectory, exeDir);

        if (File.Exists(localPath))
        {
            cachedExecutableLocation = localPath;
        }
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

    public static async Task SetWallpaper(string path, WallpaperOptions options)
    {
        if (string.IsNullOrEmpty(cachedExecutableLocation))
        {
            throw new Exception("No executable linked");
        }

        KillExistingRuns(cachedExecutableLocation);

        ProcessStartInfo info = options.CreateArgList();
        info.FileName = cachedExecutableLocation;
        info.ArgumentList.Add(path);

        info.UseShellExecute = false;
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        info.CreateNoWindow = true;

        Process p = new Process();
        p.StartInfo = info;
        p.Start();

        dbo_Config? saveScriptLocation = await ConfigManager.GetConfigValue(ConfigManager.ConfigKeys.SaveStartupScriptLocation);

        if (saveScriptLocation != null)
        {
            await SaveCommandToFile(info, saveScriptLocation.value);
        }
    }

    private static async Task SaveCommandToFile(ProcessStartInfo arguments, string? path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        if (File.Exists(path))
            File.Delete(path);

        ProcessStartInfo info = new ProcessStartInfo();
        info.FileName = "/bin/echo";

        info.ArgumentList.Add(cachedExecutableLocation!);

        foreach (var arg in arguments.ArgumentList)
            info.ArgumentList.Add(arg);

        info.RedirectStandardOutput = true;

        using (Process p = Process.Start(info)!)
        {
            using (var writer = new StreamWriter(path))
            {
                await writer.WriteLineAsync("#!/bin/bash");
                await writer.WriteLineAsync(await p.StandardOutput.ReadToEndAsync());
            }

            p.WaitForExit();
        }
    }

    private static void KillExistingRuns(string exeName)
    {
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var processExe = process.MainModule?.FileName;

                if (processExe != null && Path.GetFullPath(processExe) == exeName)
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
            catch { }
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

    public struct WallpaperOptions
    {
        public ClampOptions? clampOptions;
        public ScalingOptions? scalingOption;
        public ScreenSettings[] screens;

        public struct ScreenSettings
        {
            public required string screenName;
            public float? offsetX;
            public float? offsetY;
        }


        public ProcessStartInfo CreateArgList(params string[] injectedArgs)
        {
            ProcessStartInfo info = new ProcessStartInfo();

            foreach (string arg in injectedArgs)
                info.ArgumentList.Add(arg);

            if (scalingOption.HasValue)
            {
                info.ArgumentList.Add("--scaling");
                info.ArgumentList.Add(scalingOption.ToString()!);
            }

            info.ArgumentList.Add("--clamp");
            info.ArgumentList.Add((clampOptions ?? ClampOptions.clamp).ToString());

            foreach (ScreenSettings screen in screens)
            {
                info.ArgumentList.Add("--screen-root");
                info.ArgumentList.Add(screen.screenName);

                if (screen.offsetX.HasValue)
                {
                    info.ArgumentList.Add("--offset-x");
                    info.ArgumentList.Add(screen.offsetX.ToString()!);
                }

                if (screen.offsetY.HasValue)
                {
                    info.ArgumentList.Add("--offset-y");
                    info.ArgumentList.Add(screen.offsetY.ToString()!);
                }
            }

            info.ArgumentList.Add("--bg");
            return info;
        }
    }

}
