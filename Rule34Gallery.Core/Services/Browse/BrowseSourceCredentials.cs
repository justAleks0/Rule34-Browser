namespace Rule34Gallery.Core.Services.Browse;

public sealed class BrowseSourceCredentials
{
    public string Login { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public static BrowseSourceCredentials FromRule34(UserSettings settings) => new()
    {
        Login = settings.UserId?.Trim() ?? string.Empty,
        ApiKey = settings.ApiKey ?? string.Empty,
    };

    public static BrowseSourceCredentials FromDanbooru(UserSettings settings) => new()
    {
        Login = settings.DanbooruLogin?.Trim() ?? string.Empty,
        ApiKey = settings.DanbooruApiKey ?? string.Empty,
    };

    public static BrowseSourceCredentials FromE621(UserSettings settings) => new()
    {
        Login = settings.E621Username?.Trim() ?? string.Empty,
        ApiKey = settings.E621ApiKey ?? string.Empty,
    };

    public static BrowseSourceCredentials ForSource(GallerySource source, UserSettings settings) => source switch
    {
        GallerySource.Danbooru => FromDanbooru(settings),
        GallerySource.E621 => FromE621(settings),
        _ => FromRule34(settings),
    };
}
