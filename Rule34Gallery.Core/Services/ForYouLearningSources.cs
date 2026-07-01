namespace Rule34Gallery.Core.Services;

public static class ForYouLearningSources
{
    public const string Browse = "browse";
    public const string ForYouFeed = "for-you-feed";
    public const string Library = "library";
    public const string LocalLibrary = "local-library";

    /// <summary>
    /// Passive viewing in the For You feed should not re-train the profile (avoids feedback loops).
    /// Explicit actions (favorite, download, tag click) still learn.
    /// </summary>
    public static bool ShouldLearnFromPassiveView(string? source)
        => !string.Equals(source, ForYouFeed, StringComparison.OrdinalIgnoreCase);
}
