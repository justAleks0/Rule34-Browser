using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Rule34Gallery.Maui.Platform;

internal sealed class MauiImageCache : IImageCache
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly string _cacheDir;
    private readonly ConcurrentDictionary<string, object> _memory = new();
    private readonly SemaphoreSlim _gate = new(12);

    public MauiImageCache(string cacheFolder)
    {
        _cacheDir = Path.Combine(cacheFolder, "images");
        Directory.CreateDirectory(_cacheDir);
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("Rule34GalleryApp/2.0");
    }

    public bool TryGet(string url, int? decodePixelWidth, out object? image)
    {
        image = null;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return _memory.TryGetValue(Key(url, decodePixelWidth), out image);
    }

    public bool IsLocalPath(string url)
        => !string.IsNullOrWhiteSpace(url) && File.Exists(url);

    public async Task<object?> LoadAsync(string url, int? decodePixelWidth, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var key = Key(url, decodePixelWidth);
        if (_memory.TryGetValue(key, out var cached))
        {
            return cached;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_memory.TryGetValue(key, out cached))
            {
                return cached;
            }

            var cacheFile = CacheFilePath(key);
            byte[] bytes;
            if (IsLocalPath(url))
            {
                bytes = await File.ReadAllBytesAsync(url, cancellationToken).ConfigureAwait(false);
            }
            else if (File.Exists(cacheFile))
            {
                bytes = await File.ReadAllBytesAsync(cacheFile, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                bytes = await Http.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
                await File.WriteAllBytesAsync(cacheFile, bytes, cancellationToken).ConfigureAwait(false);
            }

            var image = ImageSource.FromStream(() => new MemoryStream(bytes));
            _memory[key] = image;
            return image;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task WarmAsync(IEnumerable<string> urls, int? decodePixelWidth, CancellationToken cancellationToken = default)
    {
        foreach (var url in urls)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            _ = await LoadAsync(url, decodePixelWidth, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Invalidate(string url, int? decodePixelWidth)
    {
        var key = Key(url, decodePixelWidth);
        _memory.TryRemove(key, out _);
        var file = CacheFilePath(key);
        if (File.Exists(file))
        {
            File.Delete(file);
        }
    }

    private string CacheFilePath(string key) => Path.Combine(_cacheDir, key + ".img");

    private static string Key(string url, int? width)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url + "|" + (width ?? 0))));
        return hash;
    }
}
