namespace Rule34Gallery.Core.Abstractions;

public interface IImageCache
{
    bool TryGet(string url, int? decodePixelWidth, out object? image);

    bool IsLocalPath(string url);

    Task<object?> LoadAsync(string url, int? decodePixelWidth, CancellationToken cancellationToken = default);

    Task WarmAsync(IEnumerable<string> urls, int? decodePixelWidth, CancellationToken cancellationToken = default);

    void Invalidate(string url, int? decodePixelWidth);
}
