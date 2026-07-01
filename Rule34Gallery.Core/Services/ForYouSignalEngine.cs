namespace Rule34Gallery.Core.Services;

public static class ForYouSignalEngine
{
    public static bool AreTagsSimilar(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        if (left.Equals(right, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (left.Contains(right, StringComparison.OrdinalIgnoreCase) ||
            right.Contains(left, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var minLen = Math.Min(left.Length, right.Length);
        if (minLen < 4)
        {
            return false;
        }

        var prefix = 0;
        while (prefix < minLen &&
               char.ToLowerInvariant(left[prefix]) == char.ToLowerInvariant(right[prefix]))
        {
            prefix++;
        }

        return prefix >= 4;
    }

    public static IEnumerable<string> FindSimilarTags(
        string tag,
        IEnumerable<string> candidates,
        int take = 4)
        => candidates
            .Where(candidate => AreTagsSimilar(tag, candidate) &&
                                !candidate.Equals(tag, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(take);

    public static bool HasSimilarSearchHistory(ForYouProfile profile, string tag)
        => profile.RecentActivity.Any(entry =>
            entry.SignalType is ForYouSignalType.Search or ForYouSignalType.SimilarTagSearch &&
            AreTagsSimilar(entry.Topic, tag));

    public static bool HasOpenedPostBefore(ForYouProfile profile, string postDetail)
        => profile.RecentActivity.Any(entry =>
            entry.Detail.Equals(postDetail, StringComparison.OrdinalIgnoreCase) &&
            entry.SignalType is ForYouSignalType.PostOpened or ForYouSignalType.PostReopened);

    public static bool HasRepeatedTagViews(ForYouTopicProfile profile)
        => profile.OpenHits >= 2;
}
