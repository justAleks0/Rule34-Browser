namespace Rule34Gallery.Core.Abstractions;

public interface IThumbnailProvider
{
    Task<object?> GetThumbnailAsync(string filePath, int pixelSize, CancellationToken cancellationToken = default);
}
