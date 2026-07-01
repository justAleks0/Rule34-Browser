using System.IO;
using Rule34Gallery.Core.Abstractions;

namespace Rule34GalleryApp.Platform;

internal sealed class WpfLocalFileAccess : ILocalFileAccess
{
    public bool IsAccessiblePath(string path) => !string.IsNullOrWhiteSpace(path) && (File.Exists(path) || Directory.Exists(path));

    public bool FileExists(string path) => File.Exists(path);

    public Task<Stream?> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(File.OpenRead(path));
    }
}
