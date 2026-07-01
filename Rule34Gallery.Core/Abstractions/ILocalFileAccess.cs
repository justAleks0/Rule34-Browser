namespace Rule34Gallery.Core.Abstractions;

public interface ILocalFileAccess
{
    bool IsAccessiblePath(string path);

    bool FileExists(string path);

    Task<Stream?> OpenReadAsync(string path, CancellationToken cancellationToken = default);
}
