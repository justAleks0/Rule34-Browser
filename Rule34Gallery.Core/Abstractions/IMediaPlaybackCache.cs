namespace Rule34Gallery.Core.Abstractions;

public interface IMediaPlaybackCache
{
    /// <summary>Start caching remote media in the background (no-op for local paths).</summary>
    void Prewarm(string? url);

    /// <summary>Returns a local file URI when cached; otherwise buffers to disk then returns the path.</summary>
    Task<Uri> ResolvePlaybackUriAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>Deletes all buffered remote playback files.</summary>
    void ClearAll();

    /// <summary>Removes the cached file for a URL (e.g. after corrupt playback).</summary>
    void Invalidate(string url);
}
