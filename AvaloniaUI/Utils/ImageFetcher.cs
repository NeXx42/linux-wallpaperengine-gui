using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Logic;
using Logic.Data;

namespace AvaloniaUI.Utils;

public static class ImageFetcher
{
    public static Task<ImageBrush?> GetIcon(IWorkshopEntry game)
    {
        TaskCompletionSource<ImageBrush?> req = new TaskCompletionSource<ImageBrush?>(TaskCreationOptions.RunContinuationsAsynchronously);
        ImageFetchingManager.QueueFetchWallpaperIcon(game, HandleReturn);

        return req.Task;

        Task HandleReturn(long gameId, object? brush)
        {
            req.SetResult(brush != null ? (ImageBrush)brush : null);
            return Task.CompletedTask;
        }
    }

    public static async Task<(byte[]?, object?)> HandleWebBrushCreation(string path)
    {
        try
        {
            using var http = new HttpClient();
            byte[] bytes = await http.GetByteArrayAsync(path);

            var bitmap = new Bitmap(new MemoryStream(bytes));
            ImageBrush? brush = null;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                brush = new ImageBrush(bitmap)
                {
                    Stretch = Stretch.UniformToFill
                };
            });

            return (bytes, brush);
        }
        catch
        {
            return (null, null);
        }
    }


    public static async Task<object?> HandleBrushCreation(string path)
    {
        if (!File.Exists(path))
            return null;

        var bitmap = new Bitmap(path);
        ImageBrush? brush = null;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            brush = new ImageBrush(bitmap)
            {
                Stretch = Stretch.UniformToFill
            };
        });

        return brush;
    }
}
