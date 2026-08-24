using System.Collections.Concurrent;

namespace Logic;

public static class LoggingManager
{
    private static Action<Exception>? uiCallback;

    private static Thread? loggingThread;
    private static CancellationTokenSource loggingToken = new();
    private static ConcurrentQueue<string> messageQueue = new();

    public static async Task Init(Action<Exception> uiCallback)
    {
        LoggingManager.uiCallback = uiCallback;

        loggingThread = new Thread(LoggingThreadFunction)
        {
            IsBackground = true
        };
        loggingThread.Start();

        LogMessage("App startup");

        if (ConfigManager.IsFlatpak)
            LogMessage("Assuming flatpak");
    }

    public static void OnException(Exception e)
    {
        LogError(e);
        Console.WriteLine(e.Message);

        uiCallback?.Invoke(e);
    }

    public static void StopLogThread() => loggingToken.Cancel();

    public static void LogError(string e)
        => FormatLogMessage("ERROR", e);

    public static void LogError(Exception e)
        => FormatLogMessage("ERROR", e.Message);

    public static void LogWarning(string msg)
        => FormatLogMessage("WARNING", msg);

    public static void LogMessage(string msg)
        => FormatLogMessage("MESSAGE", msg);

    private static void FormatLogMessage(string prefix, string msg)
        => messageQueue.Enqueue($"[{DateTime.UtcNow}] {prefix} - {msg}");

    private static void LoggingThreadFunction()
    {
        string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ConfigManager.APPLICATION_NAME);

        try
        {
            Directory.CreateDirectory(logDir);
            string logFile = Path.Combine(logDir, "app.log");

            if (File.Exists(logFile))
                File.Delete(logFile);

            using StreamWriter writer = new StreamWriter(logFile, true) { AutoFlush = true };

            while (!loggingToken.IsCancellationRequested)
            {
                Thread.Sleep(100);

                if (messageQueue.TryDequeue(out string? msg))
                    writer.WriteLine(msg);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Logging thread crashed: {ex}");
        }
    }
}
