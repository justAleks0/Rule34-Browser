using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Rule34Gallery.Core.Abstractions;

namespace Rule34GalleryApp.Services;

/// <summary>
/// Caches remote video/GIF files to disk. Playback uses the cache only when a full file is ready;
/// otherwise streams from the original URL while downloading in the background.
/// </summary>
internal sealed class WpfMediaPlaybackCache : IMediaPlaybackCache, IDisposable
{
    private const long MaxCacheBytes = 3L * 1024 * 1024 * 1024;

    private static readonly HttpClient MediaHttp = CreateMediaHttp();

    private readonly string _cacheDirectory;
    private readonly ConcurrentDictionary<string, DownloadJob> _jobs = new(StringComparer.Ordinal);

    public WpfMediaPlaybackCache(string cacheFolder)
    {
        _cacheDirectory = Path.Combine(cacheFolder, "media-playback");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public void Prewarm(string? url)
    {
        if (!TryGetRemoteCacheTarget(url, out var key, out var cachePath))
        {
            return;
        }

        if (IsReadyCacheFile(cachePath))
        {
            return;
        }

        StartBackgroundDownload(url!, key, cachePath);
    }

    public Task<Uri> ResolvePlaybackUriAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Playback URL is required.", nameof(url));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (ImageCache.IsLocalPath(url) && File.Exists(url))
        {
            return Task.FromResult(new Uri(Path.GetFullPath(url), UriKind.Absolute));
        }

        if (TryGetRemoteCacheTarget(url, out var key, out var cachePath) && IsReadyCacheFile(cachePath))
        {
            Touch(cachePath);
            return Task.FromResult(new Uri(cachePath, UriKind.Absolute));
        }

        if (TryGetRemoteCacheTarget(url, out key, out cachePath))
        {
            StartBackgroundDownload(url, key, cachePath);
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var remote))
        {
            throw new ArgumentException("Playback URL is not a valid absolute URI.", nameof(url));
        }

        return Task.FromResult(remote);
    }

    public void Invalidate(string url)
    {
        if (!TryGetRemoteCacheTarget(url, out var key, out var cachePath))
        {
            return;
        }

        if (_jobs.TryRemove(key, out var job))
        {
            job.Dispose();
        }

        TryDelete(cachePath);
        TryDelete(cachePath + ".part");
    }

    public void ClearAll()
    {
        foreach (var job in _jobs.Values)
        {
            job.Dispose();
        }

        _jobs.Clear();

        try
        {
            if (!Directory.Exists(_cacheDirectory))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(_cacheDirectory))
            {
                TryDelete(file);
            }
        }
        catch
        {
            // Best effort.
        }
    }

    public void Dispose()
    {
        foreach (var job in _jobs.Values)
        {
            job.Dispose();
        }

        _jobs.Clear();
    }

    private bool TryGetRemoteCacheTarget(string? url, out string key, out string cachePath)
    {
        key = string.Empty;
        cachePath = string.Empty;
        if (!ShouldCache(url))
        {
            return false;
        }

        key = NormalizeUrl(url!);
        cachePath = GetCachePath(key, url!);
        return true;
    }

    private void StartBackgroundDownload(string url, string key, string cachePath)
    {
        if (IsReadyCacheFile(cachePath))
        {
            return;
        }

        var job = _jobs.GetOrAdd(key, k => new DownloadJob(
            url,
            cachePath,
            onCompleted: () =>
            {
                _jobs.TryRemove(k, out _);
                TryEnforceCacheLimit();
            }));

        job.EnsureStarted();
    }

    private static bool ShouldCache(string? url)
        => !string.IsNullOrWhiteSpace(url) && !ImageCache.IsLocalPath(url);

    private static HttpClient CreateMediaHttp()
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 8,
        };

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(15),
        };
    }

    private string GetCachePath(string key, string url)
    {
        var extension = ".bin";
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var fromPath = Path.GetExtension(uri.AbsolutePath);
            if (!string.IsNullOrEmpty(fromPath) && fromPath.Length <= 8)
            {
                extension = fromPath;
            }
        }

        return Path.Combine(_cacheDirectory, $"{HashKey(key)}{extension}");
    }

    private static string NormalizeUrl(string url) => url.Trim();

    private static string HashKey(string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes)[..32].ToLowerInvariant();
    }

    private static bool IsReadyCacheFile(string path)
        => File.Exists(path) && new FileInfo(path).Length > 0;

    private void Touch(string path)
    {
        try
        {
            File.SetLastAccessTimeUtc(path, DateTime.UtcNow);
        }
        catch
        {
            // Best effort.
        }
    }

    private void TryEnforceCacheLimit()
    {
        try
        {
            var files = Directory.EnumerateFiles(_cacheDirectory)
                .Where(path => !path.EndsWith(".part", StringComparison.OrdinalIgnoreCase))
                .Select(path => new FileInfo(path))
                .OrderBy(info => info.LastAccessTimeUtc)
                .ToList();

            long total = files.Sum(file => file.Length);
            foreach (var file in files)
            {
                if (total <= MaxCacheBytes)
                {
                    break;
                }

                total -= file.Length;
                TryDelete(file.FullName);
            }
        }
        catch
        {
            // Best effort.
        }
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

    private sealed class DownloadJob : IDisposable
    {
        private readonly string _url;
        private readonly string _cachePath;
        private readonly string _partPath;
        private readonly SemaphoreSlim _startGate = new(1, 1);
        private readonly Action? _onCompleted;

        private CancellationTokenSource? _downloadCts;
        private Task? _downloadTask;
        private bool _disposed;

        public DownloadJob(string url, string cachePath, Action? onCompleted = null)
        {
            _url = url;
            _cachePath = cachePath;
            _partPath = cachePath + ".part";
            _onCompleted = onCompleted;
        }

        public void EnsureStarted()
        {
            if (_disposed || IsReadyCacheFile(_cachePath))
            {
                return;
            }

            if (_downloadTask is { IsCompleted: false })
            {
                return;
            }

            if (!_startGate.Wait(0))
            {
                return;
            }

            try
            {
                if (_downloadTask is { IsCompleted: false })
                {
                    return;
                }

                _downloadCts = new CancellationTokenSource();
                _downloadTask = DownloadAsync(_downloadCts.Token);
            }
            finally
            {
                _startGate.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _downloadCts?.Cancel();
            _downloadCts?.Dispose();
            _startGate.Dispose();
        }

        private async Task DownloadAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (IsReadyCacheFile(_cachePath))
                {
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(_cachePath)!);
                TryDelete(_partPath);

                using var request = new HttpRequestMessage(HttpMethod.Get, _url);
                request.Headers.TryAddWithoutValidation("Accept", "*/*");
                request.Headers.TryAddWithoutValidation("Referer", "https://rule34.xxx/");

                using var response = await MediaHttp.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                await using var network = await response.Content.ReadAsStreamAsync(cancellationToken)
                    .ConfigureAwait(false);
                await using var file = new FileStream(
                    _partPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    FileOptions.Asynchronous);

                var buffer = new byte[81920];
                int read;
                while ((read = await network.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }

                await file.FlushAsync(cancellationToken).ConfigureAwait(false);
                file.Close();

                TryDelete(_cachePath);
                File.Move(_partPath, _cachePath);
            }
            catch (OperationCanceledException)
            {
                // Viewer navigated away or app shutting down.
            }
            catch
            {
                TryDelete(_partPath);
            }
            finally
            {
                _onCompleted?.Invoke();
            }
        }
    }
}
