namespace Rule34Gallery.Core.Services.Browse;

public static class BrowseSourceClientFactory
{
    private static readonly Rule34BrowseClient Rule34 = new();
    private static readonly DanbooruBrowseClient Danbooru = new();
    private static readonly E621BrowseClient E621 = new();

    public static IBrowseSourceClient Get(GallerySource source) => source switch
    {
        GallerySource.Danbooru => Danbooru,
        GallerySource.E621 => E621,
        _ => Rule34,
    };
}
