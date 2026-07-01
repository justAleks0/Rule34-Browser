namespace Rule34Gallery.Core.Services;

public static class ForYouInterestSummary
{
    private const int TrendWindowSearches = 25;

    public static string Build(
        IReadOnlyList<ForYouTopicProfile> topics,
        IEnumerable<ForYouActivityEntry> activities)
    {
        var visible = topics
            .Where(t => !string.IsNullOrWhiteSpace(t.Topic))
            .Where(t => !t.IsBlocked && !ForYouLearningFilter.IsExcluded(t.Topic))
            .ToList();
        if (visible.Count == 0)
        {
            return "Start searching and opening posts to train your feed.";
        }

        var searches = ForYouTopicDecay.ListSearchEvents(activities);
        var searchesNewestFirst = searches.Reverse().ToList();
        var ranked = visible
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.Weight)
            .ToList();

        var interest = ranked
            .FirstOrDefault(t => ForYouTopicFocus.IsCharacterTopic(t.Topic))
            ?.Topic
            ?? ranked.First().Topic;

        var builder = new System.Text.StringBuilder();
        builder.Append("The user is showing interest in ");
        builder.Append(FormatTopicLabel(interest));
        builder.Append(" currently.");

        var rising = FindRisingTopic(visible, searchesNewestFirst);
        var declining = FindDecliningTopic(visible, searchesNewestFirst);
        if (!string.IsNullOrWhiteSpace(rising) || !string.IsNullOrWhiteSpace(declining))
        {
            builder.AppendLine();
            builder.Append("Currently ");
            if (!string.IsNullOrWhiteSpace(rising))
            {
                builder.Append(FormatTopicLabel(rising));
                builder.Append(" ⬆️ is on a rise");
            }

            if (!string.IsNullOrWhiteSpace(rising) && !string.IsNullOrWhiteSpace(declining))
            {
                builder.Append(" and ");
            }

            if (!string.IsNullOrWhiteSpace(declining))
            {
                builder.Append(FormatTopicLabel(declining));
                builder.Append(" ⬇️ is on a decline");
            }

            builder.Append('.');
        }

        return builder.ToString();
    }

    private static string? FindRisingTopic(
        IReadOnlyList<ForYouTopicProfile> topics,
        IReadOnlyList<ForYouTopicDecay.SearchEvent> searchesNewestFirst)
    {
        var recent = searchesNewestFirst.Take(TrendWindowSearches).ToList();
        var older = searchesNewestFirst.Skip(TrendWindowSearches).Take(TrendWindowSearches).ToList();
        if (recent.Count == 0)
        {
            return null;
        }

        return topics
            .Where(t => !ForYouTopicFocus.IsGenericTopic(t.Topic))
            .Select(topic => new
            {
                Topic = topic.Topic,
                Delta = CountTopicHits(topic.Topic, recent) - CountTopicHits(topic.Topic, older),
                Recent = CountTopicHits(topic.Topic, recent),
            })
            .Where(x => x.Recent > 0 && x.Delta > 0)
            .OrderByDescending(x => x.Delta)
            .ThenByDescending(x => x.Recent)
            .Select(x => x.Topic)
            .FirstOrDefault();
    }

    private static string? FindDecliningTopic(
        IReadOnlyList<ForYouTopicProfile> topics,
        IReadOnlyList<ForYouTopicDecay.SearchEvent> searchesNewestFirst)
    {
        var recent = searchesNewestFirst.Take(TrendWindowSearches).ToList();
        var older = searchesNewestFirst.Skip(TrendWindowSearches).Take(TrendWindowSearches).ToList();

        var trendDecline = topics
            .Where(t => !ForYouTopicFocus.IsGenericTopic(t.Topic))
            .Select(topic => new
            {
                Topic = topic.Topic,
                Delta = CountTopicHits(topic.Topic, recent) - CountTopicHits(topic.Topic, older),
                Older = CountTopicHits(topic.Topic, older),
            })
            .Where(x => x.Older > 0 && x.Delta < 0)
            .OrderBy(x => x.Delta)
            .ThenByDescending(x => x.Older)
            .Select(x => x.Topic)
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(trendDecline))
        {
            return trendDecline;
        }

        return topics
            .Where(t => !t.IsPinned && !ForYouTopicFocus.IsGenericTopic(t.Topic))
            .Select(topic => new
            {
                Topic = topic.Topic,
                Penalty = ForYouTopicDecay.DecayPenalty(
                    ForYouTopicDecay.SearchesSinceLastHit(topic.Topic, searchesNewestFirst)),
            })
            .Where(x => x.Penalty > 0)
            .OrderByDescending(x => x.Penalty)
            .Select(x => x.Topic)
            .FirstOrDefault();
    }

    private static int CountTopicHits(string topic, IReadOnlyList<ForYouTopicDecay.SearchEvent> searches)
        => searches.Count(search => ForYouTopicDecay.SearchEventContainsTopic(search, topic));

    public static string FormatTopicLabel(string topic)
        => topic.Replace('_', ' ').Trim();
}
