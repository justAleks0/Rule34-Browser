namespace Rule34Gallery.Core.Services;

/// <summary>
/// App-wide and search-level tag blocking applied to API queries and every gallery.
/// </summary>
public static class TagBlockFilter
{
    public static IEnumerable<string> GetAllBlockingTags(UserSettings settings)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in settings.GetGlobalBlockedTags())
        {
            tags.Add(tag);
        }

        foreach (var tag in settings.GetEffectiveBlacklistTags())
        {
            tags.Add(tag);
        }

        return tags;
    }

    public static bool PostHasBlockingTag(PostItem post, UserSettings settings)
    {
        var blocking = GetAllBlockingTags(settings).ToList();
        if (blocking.Count == 0)
        {
            return false;
        }

        var postTags = post.GetTagList();
        foreach (var blocked in blocking)
        {
            var normalized = NormalizeBlockedTag(blocked);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            if (postTags.Any(t => t.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    public static IEnumerable<PostItem> Apply(IEnumerable<PostItem> posts, UserSettings settings)
        => posts.Where(p => !PostHasBlockingTag(p, settings));

    public static string NormalizeBlockedTag(string tag)
        => tag.Trim().TrimStart('-').Replace(" ", "_");
}
