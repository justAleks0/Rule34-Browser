namespace Rule34Gallery.Core.Services;

public static class ForYouTagMatching
{
    public static string Normalize(string tag)
        => UserSettings.NormalizeTagToken(tag).Trim().ToLowerInvariant();

    public static bool PostHasTag(IReadOnlyList<string> postTags, string topic)
    {
        var normalized = Normalize(topic);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return postTags.Any(postTag =>
            Normalize(postTag).Equals(normalized, StringComparison.Ordinal));
    }

    public static int CountExactMatches(IReadOnlyList<string> postTags, IEnumerable<string> topics)
    {
        var count = 0;
        foreach (var topic in topics)
        {
            if (PostHasTag(postTags, topic))
            {
                count++;
            }
        }

        return count;
    }

    public static double SumWeightedExactMatches(
        IReadOnlyList<string> postTags,
        IReadOnlyList<(string Tag, double Weight)> topicWeights)
    {
        var sum = 0.0;
        foreach (var (tag, weight) in topicWeights)
        {
            if (PostHasTag(postTags, tag))
            {
                sum += weight;
            }
        }

        return sum;
    }
}
