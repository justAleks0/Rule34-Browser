namespace Rule34GalleryApp.Views.Pages;

using Rule34Gallery.Core;

public sealed class ForYouGalleryEntry
{
    public required PostItem Post { get; init; }

    public string Reason { get; init; } = string.Empty;

    public int MatchedTopicCount { get; init; }
}
