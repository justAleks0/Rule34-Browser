using System.Windows.Media.Imaging;
using Rule34Gallery.Core.Abstractions;

namespace Rule34GalleryApp.Platform;

internal sealed class WpfImageCache : IImageCache
{
    public bool TryGet(string url, int? decodePixelWidth, out object? image)
    {
        var ok = ImageCache.TryGet(url, decodePixelWidth, out var bitmap);
        image = bitmap;
        return ok;
    }

    public bool IsLocalPath(string url) => ImageCache.IsLocalPath(url);

    public async Task<object?> LoadAsync(string url, int? decodePixelWidth, CancellationToken cancellationToken = default)
        => await ImageCache.LoadAsync(url, decodePixelWidth, cancellationToken).ConfigureAwait(false);

    public Task WarmAsync(IEnumerable<string> urls, int? decodePixelWidth, CancellationToken cancellationToken = default)
        => ImageCache.WarmAsync(urls, decodePixelWidth, cancellationToken);

    public void Invalidate(string url, int? decodePixelWidth) => ImageCache.Invalidate(url);
}
