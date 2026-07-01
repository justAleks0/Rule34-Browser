using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Rule34GalleryApp.Services;

public static class LocalVideoThumbnailService
{
    private static readonly string CacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Rule34GalleryApp",
        "video-thumbs");

    public static event EventHandler<string>? ThumbnailUpdated;

    public static bool IsLocalVideoPath(string path)
        => File.Exists(path) && PostMedia.ShouldUseShellThumbnail(path);

    public static async Task<BitmapImage?> GetThumbnailAsync(
        string filePath,
        int decodePixelWidth,
        CancellationToken cancellationToken = default)
    {
        if (!IsLocalVideoPath(filePath))
        {
            return null;
        }

        var fraction = ThumbnailSeekStore.GetFraction(filePath);
        var width = Math.Clamp(decodePixelWidth > 0 ? decodePixelWidth : 320, 120, 1280);
        var cacheFile = GetCacheFilePath(filePath, fraction, width);

        if (TryLoadCached(cacheFile, filePath, width, out var cached))
        {
            return cached;
        }

        var source = await CaptureFrameAsync(filePath, fraction, width, cancellationToken).ConfigureAwait(false);
        if (source is null)
        {
            return null;
        }

        var bitmap = await EncodeBitmapAsync(source, width).ConfigureAwait(false);
        if (bitmap is not null)
        {
            TryWriteCache(cacheFile, bitmap);
        }

        return bitmap;
    }

    public static Task<BitmapSource?> CapturePreviewAsync(
        string filePath,
        double fraction,
        int width = 480,
        CancellationToken cancellationToken = default)
        => CaptureFrameAsync(filePath, fraction, width, cancellationToken);

    public static void Invalidate(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var fullPath = filePath;
        try
        {
            fullPath = Path.GetFullPath(filePath);
            if (Directory.Exists(CacheDirectory))
            {
                var hash = HashPath(fullPath);
                foreach (var file in Directory.EnumerateFiles(CacheDirectory, $"{hash}_*"))
                {
                    TryDelete(file);
                }
            }
        }
        catch
        {
            // Best effort.
        }

        ImageCache.Invalidate(filePath);
        ThumbnailUpdated?.Invoke(null, fullPath);
    }

    public static void SaveThumbnailAt(string filePath, double fraction)
    {
        ThumbnailSeekStore.SetFraction(filePath, fraction);
        Invalidate(filePath);
    }

    private static async Task<BitmapSource?> CaptureFrameAsync(
        string filePath,
        double fraction,
        int targetWidth,
        CancellationToken cancellationToken)
    {
        return await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await CaptureFrameOnUiThreadAsync(filePath, fraction, targetWidth, cancellationToken);
        }).Task.Unwrap().ConfigureAwait(false);
    }

    private static async Task<BitmapSource?> CaptureFrameOnUiThreadAsync(
        string filePath,
        double fraction,
        int targetWidth,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<BitmapSource?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var player = new MediaPlayer
        {
            ScrubbingEnabled = true,
            Volume = 0,
        };

        DispatcherTimer? settleTimer = null;
        var timeoutTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(45) };

        void Finish(BitmapSource? result)
        {
            timeoutTimer.Stop();
            settleTimer?.Stop();
            player.Close();
            tcs.TrySetResult(result);
        }

        timeoutTimer.Tick += (_, _) => Finish(null);
        timeoutTimer.Start();

        player.MediaFailed += (_, _) => Finish(null);

        player.MediaOpened += (_, _) =>
        {
            if (!player.NaturalDuration.HasTimeSpan)
            {
                Finish(null);
                return;
            }

            var duration = player.NaturalDuration.TimeSpan;
            var clamped = Math.Clamp(fraction, 0.02, 0.98);
            player.Position = TimeSpan.FromTicks((long)(duration.Ticks * clamped));
            player.Pause();

            settleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
            settleTimer.Tick += (_, _) =>
            {
                try
                {
                    var videoWidth = player.NaturalVideoWidth;
                    var videoHeight = player.NaturalVideoHeight;
                    if (videoWidth <= 0 || videoHeight <= 0)
                    {
                        Finish(null);
                        return;
                    }

                    var scale = targetWidth / (double)videoWidth;
                    var renderWidth = targetWidth;
                    var renderHeight = Math.Max(1, (int)(videoHeight * scale));

                    var visual = new DrawingVisual();
                    using (var context = visual.RenderOpen())
                    {
                        context.DrawVideo(player, new Rect(0, 0, renderWidth, renderHeight));
                    }

                    var bitmap = new RenderTargetBitmap(
                        renderWidth,
                        renderHeight,
                        96,
                        96,
                        PixelFormats.Pbgra32);
                    bitmap.Render(visual);
                    if (bitmap.CanFreeze)
                    {
                        bitmap.Freeze();
                    }

                    Finish(bitmap);
                }
                catch
                {
                    Finish(null);
                }
            };
            settleTimer.Start();
        };

        try
        {
            var uri = new Uri(Path.GetFullPath(filePath), UriKind.Absolute);
            player.Open(uri);
        }
        catch
        {
            Finish(null);
        }

        using var registration = cancellationToken.Register(() => Finish(null));
        return await tcs.Task.ConfigureAwait(true);
    }

    private static async Task<BitmapImage?> EncodeBitmapAsync(BitmapSource source, int decodePixelWidth)
        => await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var frame = BitmapFrame.Create(source);
            var encoder = new JpegBitmapEncoder { QualityLevel = 85 };
            encoder.Frames.Add(frame);
            using var memory = new MemoryStream();
            encoder.Save(memory);
            memory.Position = 0;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = decodePixelWidth;
            image.StreamSource = memory;
            image.EndInit();
            if (image.CanFreeze)
            {
                image.Freeze();
            }

            return image;
        }).Task.ConfigureAwait(false);

    private static bool TryLoadCached(string cacheFile, string sourceFile, int width, out BitmapImage? image)
    {
        image = null;
        try
        {
            if (!File.Exists(cacheFile) || !File.Exists(sourceFile))
            {
                return false;
            }

            if (File.GetLastWriteTimeUtc(cacheFile) < File.GetLastWriteTimeUtc(sourceFile))
            {
                return false;
            }

            image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = width;
            image.UriSource = new Uri(cacheFile, UriKind.Absolute);
            image.EndInit();
            if (image.CanFreeze)
            {
                image.Freeze();
            }

            return true;
        }
        catch
        {
            image = null;
            return false;
        }
    }

    private static void TryWriteCache(string cacheFile, BitmapSource bitmap)
    {
        try
        {
            Directory.CreateDirectory(CacheDirectory);
            var encoder = new JpegBitmapEncoder { QualityLevel = 85 };
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(cacheFile);
            encoder.Save(stream);
        }
        catch
        {
            // Ignore cache write failures.
        }
    }

    private static string GetCacheFilePath(string filePath, double fraction, int width)
    {
        var hash = HashPath(Path.GetFullPath(filePath));
        var pct = (int)Math.Round(fraction * 1000);
        return Path.Combine(CacheDirectory, $"{hash}_{pct}_{width}.jpg");
    }

    private static string HashPath(string path)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(path.ToLowerInvariant()));
        return Convert.ToHexString(bytes)[..24];
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort.
        }
    }
}
