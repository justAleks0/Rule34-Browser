namespace Rule34Gallery.Maui.Platform;

internal sealed class MauiThumbnailProvider : IThumbnailProvider
{
    public Task<object?> GetThumbnailAsync(string filePath, int pixelSize, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return Task.FromResult<object?>(null);
        }

        return Task.FromResult<object?>(ImageSource.FromFile(filePath));
    }
}
