namespace Rule34Gallery.Core.Services;

public static class ForYouTopicDecay
{
    public const int SearchGraceCount = 100;

    public sealed record SearchEvent(long TimestampUnix, IReadOnlyList<string> Tags);

    public static IReadOnlyList<SearchEvent> ListSearchEvents(IEnumerable<ForYouActivityEntry> activities)
    {
        var searches = new List<SearchEvent>();
        var batch = new List<ForYouActivityEntry>();

        foreach (var entry in activities
                     .Where(a => a.SignalType is ForYouSignalType.Search or ForYouSignalType.SimilarTagSearch)
                     .OrderBy(a => a.TimestampUnix))
        {
            if (batch.Count > 0 && batch[0].TimestampUnix != entry.TimestampUnix)
            {
                searches.Add(ToSearchEvent(batch));
                batch.Clear();
            }

            batch.Add(entry);
        }

        if (batch.Count > 0)
        {
            searches.Add(ToSearchEvent(batch));
        }

        return searches;
    }

    public static int SearchesSinceLastHit(string topic, IReadOnlyList<SearchEvent> searchesNewestFirst)
    {
        if (searchesNewestFirst.Count == 0)
        {
            return 0;
        }

        for (var i = 0; i < searchesNewestFirst.Count; i++)
        {
            if (SearchEventContainsTopic(searchesNewestFirst[i], topic))
            {
                return i;
            }
        }

        return searchesNewestFirst.Count;
    }

    public static double DecayPenalty(int searchesSinceLastHit)
        => Math.Max(0, searchesSinceLastHit - SearchGraceCount);

    public static double EffectiveScore(double baseScore, string topic, IReadOnlyList<SearchEvent> searchesNewestFirst)
    {
        var penalty = DecayPenalty(SearchesSinceLastHit(topic, searchesNewestFirst));
        return Math.Max(ForYouSignalStrengths.MinTopicScore, ForYouSignalStrengths.NormalizeTopicScore(baseScore) - penalty);
    }

    public static bool SearchEventContainsTopic(SearchEvent search, string topic)
        => search.Tags.Any(tag => TopicMatches(tag, topic));

    public static bool TopicMatches(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        if (left.Equals(right, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return ForYouSignalEngine.AreTagsSimilar(left, right);
    }

    private static SearchEvent ToSearchEvent(IReadOnlyList<ForYouActivityEntry> batch)
    {
        var tags = batch
            .Select(a => UserSettings.NormalizeTagToken(a.Topic))
            .Where(t => !string.IsNullOrWhiteSpace(t) && !ForYouLearningFilter.IsExcluded(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new SearchEvent(batch[0].TimestampUnix, tags);
    }
}
