using System.IO;
using Rule34Gallery.Core.Abstractions;

namespace Rule34Gallery.Maui.Platform;

internal sealed class PassthroughMediaPlaybackCache : IMediaPlaybackCache
{
    public void Prewarm(string? url)
    {
        // Remote streaming is handled by the platform media control on MAUI.
    }

    public Task<Uri> ResolvePlaybackUriAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Playback URL is required.", nameof(url));
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return Task.FromResult(uri);
        }

        return Task.FromResult(new Uri(Path.GetFullPath(url), UriKind.Absolute));
    }

    public void ClearAll()
    {
        // MAUI streams remote media directly; nothing to clear.
    }

    public void Invalidate(string url)
    {
        // MAUI streams remote media directly; nothing to invalidate.
    }
}
