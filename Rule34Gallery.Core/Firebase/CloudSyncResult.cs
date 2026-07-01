namespace Rule34Gallery.Core.Firebase;

public sealed class CloudSyncResult
{
    public bool Success { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public bool CredentialsUploaded { get; init; }

    public bool CredentialsDownloaded { get; init; }

    public int FavoritesCount { get; init; }

    public static CloudSyncResult Fail(string title, string message) => new()
    {
        Success = false,
        Title = title,
        Message = message,
    };

    public static CloudSyncResult Ok(string title, string message, bool uploaded, bool downloaded, int favoritesCount) =>
        new()
        {
            Success = true,
            Title = title,
            Message = message,
            CredentialsUploaded = uploaded,
            CredentialsDownloaded = downloaded,
            FavoritesCount = favoritesCount,
        };
}
