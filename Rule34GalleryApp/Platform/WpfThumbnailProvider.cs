using Rule34Gallery.Core.Abstractions;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Platform;

internal sealed class WpfThumbnailProvider : IThumbnailProvider
{
    public async Task<object?> GetThumbnailAsync(string filePath, int pixelSize, CancellationToken cancellationToken = default)
        => await ShellThumbnailService.GetThumbnailAsync(filePath, pixelSize, cancellationToken).ConfigureAwait(false);
}
