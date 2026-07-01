namespace Rule34Gallery.Core;

public enum GallerySource
{
    Rule34,
    Danbooru,
    E621,
}

public static class GallerySourceExtensions
{
    public static string ToStorageKey(this GallerySource source) => source switch
    {
        GallerySource.Danbooru => "danbooru",
        GallerySource.E621 => "e621",
        _ => "rule34",
    };

    public static GallerySource FromStorageKey(string? key) => key?.Trim().ToLowerInvariant() switch
    {
        "danbooru" => GallerySource.Danbooru,
        "e621" => GallerySource.E621,
        _ => GallerySource.Rule34,
    };

    public static string DisplayName(this GallerySource source) => source switch
    {
        GallerySource.Danbooru => "Danbooru",
        GallerySource.E621 => "e621",
        _ => "Rule34",
    };
}
