using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media.Imaging;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp;

internal static class ImageCache
{
    private static readonly HttpClient Http = CreateClient();
    private static readonly ConcurrentDictionary<string, BitmapImage> Cache = new();
    private static readonly SemaphoreSlim Gate = new(16);

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Rule34GalleryApp/1.0");
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    public static bool TryGet(string url, int? decodePixelWidth, out BitmapImage? image)
    {
        image = null;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Cache.TryGetValue(Key(url, decodePixelWidth), out image);
    }

    public static bool IsLocalPath(string url)
        => !string.IsNullOrWhiteSpace(url) &&
           (url.StartsWith(@"\\", StringComparison.Ordinal) ||
            Path.IsPathRooted(url) ||
            File.Exists(url) ||
            Directory.Exists(url));

    public static async Task<BitmapImage?> LoadAsync(
        string url,
        int? decodePixelWidth,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var key = Key(url, decodePixelWidth);
        if (Cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Cache.TryGetValue(key, out cached))
            {
                return cached;
            }

            BitmapImage bitmap;
            if (IsLocalPath(url) && File.Exists(url))
            {
                if (LocalVideoThumbnailService.IsLocalVideoPath(url))
                {
                    var thumbWidth = decodePixelWidth is > 0 ? decodePixelWidth.Value : 320;
                    bitmap = await LocalVideoThumbnailService.GetThumbnailAsync(url, thumbWidth, cancellationToken)
                             ?? await LoadFromFileAsync(url, decodePixelWidth, cancellationToken).ConfigureAwait(false);
                }
                else if (PostMedia.ShouldUseShellThumbnail(url))
                {
                    var shellSize = decodePixelWidth is > 0 ? decodePixelWidth.Value : 320;
                    bitmap = await ShellThumbnailService.GetThumbnailAsync(url, shellSize, cancellationToken)
                             ?? await LoadFromFileAsync(url, decodePixelWidth, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    bitmap = await LoadFromFileAsync(url, decodePixelWidth, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                var memory = new MemoryStream();
                await stream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
                memory.Position = 0;

                bitmap = await CreateBitmapFromStreamAsync(memory, decodePixelWidth).ConfigureAwait(false);
            }

            Cache[key] = bitmap;
            return bitmap;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static Task WarmAsync(IEnumerable<string> urls, int? decodePixelWidth, CancellationToken cancellationToken)
    {
        var tasks = urls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct()
            .Select(url => LoadAsync(url, decodePixelWidth, cancellationToken));

        return Task.WhenAll(tasks);
    }

    private static async Task<BitmapImage> LoadFromFileAsync(
        string path,
        int? decodePixelWidth,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        memory.Position = 0;
        return await CreateBitmapFromStreamAsync(memory, decodePixelWidth).ConfigureAwait(false);
    }

    private static Task<BitmapImage> CreateBitmapFromStreamAsync(Stream memory, int? decodePixelWidth)
        => Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            if (decodePixelWidth is > 0)
            {
                image.DecodePixelWidth = decodePixelWidth.Value;
            }

            image.StreamSource = memory;
            image.EndInit();
            if (image.CanFreeze)
            {
                image.Freeze();
            }

            return image;
        }).Task;

    public static void Invalidate(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        foreach (var key in Cache.Keys.Where(k => k.StartsWith(url, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            Cache.TryRemove(key, out _);
        }
    }

    private static string Key(string url, int? decodePixelWidth)
    {
        var fraction = File.Exists(url)
            ? ThumbnailSeekStore.GetFraction(url).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
            : string.Empty;
        var baseKey = decodePixelWidth is null or <= 0 ? url : $"{url}#w={decodePixelWidth}";
        return string.IsNullOrEmpty(fraction) ? baseKey : $"{baseKey}#t={fraction}";
    }
}
