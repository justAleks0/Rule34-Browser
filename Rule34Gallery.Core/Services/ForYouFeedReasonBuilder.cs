namespace Rule34Gallery.Core.Services;

public static class ForYouFeedReasonBuilder
{
    public static double ComputeTrainingScore(
        double weightedMatchScore,
        int matchedTopicCount,
        int queryHitCount,
        int postScore)
        => weightedMatchScore * 100.0
           + matchedTopicCount * 25.0
           + Math.Min(queryHitCount, 6) * 12.0
           + Math.Min(postScore, 500) * 0.04;

    public static IReadOnlyList<(string Tag, double Weight)> GetMatchedTopics(
        IReadOnlyList<string> postTags,
        IReadOnlyList<(string Tag, double Weight)> topicWeights)
    {
        if (topicWeights.Count == 0)
        {
            return [];
        }

        var matched = new List<(string Tag, double Weight)>();
        foreach (var (tag, weight) in topicWeights)
        {
            if (ForYouTagMatching.PostHasTag(postTags, tag))
            {
                matched.Add((tag, weight));
            }
        }

        return matched
            .OrderByDescending(m => m.Weight)
            .ThenBy(m => m.Tag, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static void Enrich(ForYouFeedItem item, IReadOnlyList<(string Tag, double Weight)> topicWeights)
    {
        if (item.Post is null || topicWeights.Count == 0)
        {
            return;
        }

        var matched = GetMatchedTopics(item.Post.GetTagList(), topicWeights);
        if (matched.Count == 0)
        {
            return;
        }

        var labels = matched
            .Take(4)
            .Select(m => m.Tag.Replace('_', ' '))
            .ToList();

        item.Reason = matched.Count switch
        {
            1 => $"From your topic: {labels[0]}",
            2 => $"Matches your topics: {labels[0]} + {labels[1]}",
            _ => $"Matches {matched.Count} of your topics ({string.Join(", ", labels)}{(matched.Count > 4 ? ", …" : string.Empty)})",
        };

        item.Topic = matched[0].Tag;
        item.MatchedTopicCount = matched.Count;
    }
}
