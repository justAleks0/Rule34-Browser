namespace Rule34Gallery.Core.Services;

public static class PostMetadataMerge
{
    public static PostItem Merge(PostItem stored, PostItem fresh)
    {
        stored.Tags = Prefer(fresh.Tags, stored.Tags);
        stored.TagInfo = fresh.TagInfo is { Count: > 0 } ? fresh.TagInfo : stored.TagInfo;
        stored.Rating = Prefer(fresh.Rating, stored.Rating);
        stored.Score = fresh.Score > 0 ? fresh.Score : stored.Score;
        stored.Width = fresh.Width > 0 ? fresh.Width : stored.Width;
        stored.Height = fresh.Height > 0 ? fresh.Height : stored.Height;
        stored.Owner = Prefer(fresh.Owner, stored.Owner);
        stored.PreviewUrl = Prefer(fresh.PreviewUrl, stored.PreviewUrl);
        stored.SampleUrl = Prefer(fresh.SampleUrl, stored.SampleUrl);
        stored.FileUrl = Prefer(fresh.FileUrl, stored.FileUrl);
        return stored;
    }

    public static bool LooksIncomplete(PostItem post) =>
        string.IsNullOrWhiteSpace(post.Tags) ||
        post.TagInfo is not { Count: > 0 };

    private static string Prefer(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;
}
