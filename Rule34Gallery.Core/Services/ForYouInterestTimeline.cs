namespace Rule34Gallery.Core.Services;

public sealed class ForYouInterestTimelineSeries
{
    public string Topic { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public int ColorIndex { get; init; }

    public IReadOnlyList<double> Points { get; init; } = [];

    public bool IsRising { get; init; }

    public bool IsDeclining { get; init; }
}

public sealed class ForYouInterestTimelineResult
{
    public static ForYouInterestTimelineResult Empty { get; } = new();

    public IReadOnlyList<ForYouInterestTimelineSeries> Series { get; init; } = [];

    public IReadOnlyList<long> SampleTimestampsUnix { get; init; } = [];

    public double MaxScore { get; init; } = 100;

    public bool HasData => Series.Count > 0 && SampleTimestampsUnix.Count > 1;
}

public static class ForYouInterestTimelineBuilder
{
    public const int MaxSeries = 6;
    public const int MaxPoints = 28;
    private const int TrendWindowSearches = 25;

    public static ForYouInterestTimelineResult Build(
        IReadOnlyList<ForYouTopicProfile> currentTopics,
        IEnumerable<ForYouActivityEntry> activities,
        double learningRate = 1.0)
    {
        var activityList = activities
            .Where(a => a.TimestampUnix > 0)
            .OrderBy(a => a.TimestampUnix)
            .ToList();

        if (activityList.Count < 2)
        {
            return ForYouInterestTimelineResult.Empty;
        }

        var visibleTopics = currentTopics
            .Where(t => !string.IsNullOrWhiteSpace(t.Topic))
            .Where(t => !t.IsBlocked && !ForYouLearningFilter.IsExcluded(t.Topic))
            .ToList();
        if (visibleTopics.Count == 0)
        {
            return ForYouInterestTimelineResult.Empty;
        }

        var trackTopics = SelectTopicsToTrack(visibleTopics, activityList);
        if (trackTopics.Count == 0)
        {
            return ForYouInterestTimelineResult.Empty;
        }

        var sampleIndices = PickSampleIndices(activityList.Count, MaxPoints);
        var timestamps = new List<long>(sampleIndices.Count);
        var seriesPoints = trackTopics.ToDictionary(
            t => t,
            _ => new List<double>(sampleIndices.Count),
            StringComparer.OrdinalIgnoreCase);

        var weights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var searches = new List<ForYouTopicDecay.SearchEvent>();
        var activityIndex = 0;

        timestamps.Add(activityList[0].TimestampUnix);
        foreach (var topic in trackTopics)
        {
            seriesPoints[topic].Add(0);
        }

        foreach (var sampleIndex in sampleIndices)
        {
            while (activityIndex <= sampleIndex && activityIndex < activityList.Count)
            {
                var entry = activityList[activityIndex];
                ApplyActivity(weights, entry, learningRate);
                if (entry.SignalType is ForYouSignalType.Search or ForYouSignalType.SimilarTagSearch)
                {
                    searches = ForYouTopicDecay.ListSearchEvents(activityList.Take(activityIndex + 1)).ToList();
                    ApplySearchDecay(weights, trackTopics, searches);
                }

                activityIndex++;
            }

            timestamps.Add(activityList[Math.Min(sampleIndex, activityList.Count - 1)].TimestampUnix);
            foreach (var topic in trackTopics)
            {
                seriesPoints[topic].Add(GetScore(weights, topic));
            }
        }

        var searchesNewestFirst = ForYouTopicDecay.ListSearchEvents(activityList).Reverse().ToList();
        var rising = FindRisingTopic(visibleTopics, searchesNewestFirst);
        var declining = FindDecliningTopic(visibleTopics, searchesNewestFirst);
        var maxScore = Math.Max(
            20,
            seriesPoints.Values.SelectMany(points => points).DefaultIfEmpty(0).Max());

        var series = trackTopics
            .Select((topic, index) => new ForYouInterestTimelineSeries
            {
                Topic = topic,
                Label = ForYouInterestSummary.FormatTopicLabel(topic),
                ColorIndex = index,
                Points = seriesPoints[topic],
                IsRising = topic.Equals(rising, StringComparison.OrdinalIgnoreCase),
                IsDeclining = topic.Equals(declining, StringComparison.OrdinalIgnoreCase),
            })
            .ToList();

        return new ForYouInterestTimelineResult
        {
            Series = series,
            SampleTimestampsUnix = timestamps,
            MaxScore = maxScore,
        };
    }

    private static List<string> SelectTopicsToTrack(
        IReadOnlyList<ForYouTopicProfile> topics,
        IReadOnlyList<ForYouActivityEntry> activities)
    {
        var searchesNewestFirst = ForYouTopicDecay.ListSearchEvents(activities).Reverse().ToList();
        var rising = FindRisingTopic(topics, searchesNewestFirst);
        var declining = FindDecliningTopic(topics, searchesNewestFirst);

        var selected = new List<string>();
        void TryAdd(string? topic)
        {
            if (string.IsNullOrWhiteSpace(topic) || selected.Contains(topic, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            selected.Add(topic);
        }

        foreach (var topic in topics
                     .OrderByDescending(t => t.IsPinned)
                     .ThenByDescending(t => t.Weight)
                     .Select(t => t.Topic))
        {
            TryAdd(topic);
            if (selected.Count >= MaxSeries - 2)
            {
                break;
            }
        }

        TryAdd(rising);
        TryAdd(declining);

        return selected
            .Take(MaxSeries)
            .ToList();
    }

    private static List<int> PickSampleIndices(int activityCount, int maxPoints)
    {
        var target = Math.Clamp(maxPoints, 4, activityCount + 1);
        if (activityCount <= target)
        {
            return Enumerable.Range(0, activityCount).ToList();
        }

        var indices = new List<int> { 0 };
        for (var i = 1; i < target - 1; i++)
        {
            var ratio = i / (double)(target - 1);
            indices.Add((int)Math.Round(ratio * (activityCount - 1)));
        }

        indices.Add(activityCount - 1);
        return indices.Distinct().OrderBy(i => i).ToList();
    }

    private static void ApplyActivity(
        IDictionary<string, double> weights,
        ForYouActivityEntry entry,
        double learningRate)
    {
        if (entry.SignalType == ForYouSignalType.ManualTopic)
        {
            var topic = NormalizeTopic(entry.Topic);
            if (string.IsNullOrWhiteSpace(topic))
            {
                return;
            }

            weights[topic] = Math.Clamp(
                ParseManualTopicStrength(entry.Detail),
                ForYouSignalStrengths.MinTopicScore,
                ForYouSignalStrengths.MaxTopicScore);
            return;
        }

        var tags = ExtractBoostTags(entry);
        if (tags.Count == 0)
        {
            return;
        }

        var strength = StrengthForReplay(entry);
        foreach (var tag in tags)
        {
            if (entry.SignalType is ForYouSignalType.TopicBlocked
                or ForYouSignalType.NotInterested
                or ForYouSignalType.ReportedPost)
            {
                weights[tag] = ForYouSignalStrengths.MinBlockedTopicScore;
                continue;
            }

            var current = GetScore(weights, tag);
            weights[tag] = ForYouSignalStrengths.MergeTopicScore(current, strength, learningRate);
        }
    }

    private static void ApplySearchDecay(
        IDictionary<string, double> weights,
        IReadOnlyList<string> trackTopics,
        IReadOnlyList<ForYouTopicDecay.SearchEvent> searchesNewestFirst)
    {
        if (searchesNewestFirst.Count == 0)
        {
            return;
        }

        foreach (var topic in trackTopics)
        {
            if (!weights.TryGetValue(topic, out var score))
            {
                continue;
            }

            var penalty = ForYouTopicDecay.DecayPenalty(
                ForYouTopicDecay.SearchesSinceLastHit(topic, searchesNewestFirst));
            if (penalty <= 0)
            {
                continue;
            }

            weights[topic] = ForYouSignalStrengths.NormalizeTopicScore(score - penalty);
        }
    }

    private static double GetScore(IDictionary<string, double> weights, string topic)
    {
        if (!weights.TryGetValue(topic, out var score))
        {
            return 0;
        }

        return ForYouSignalStrengths.NormalizeTopicScore(score);
    }

    private static string NormalizeTopic(string topic)
        => UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();

    private static List<string> ExtractBoostTags(ForYouActivityEntry entry)
    {
        var tags = new List<string>();
        if (!string.IsNullOrWhiteSpace(entry.Topic))
        {
            tags.Add(NormalizeTopic(entry.Topic));
        }

        if (entry.SignalType == ForYouSignalType.Search &&
            !string.IsNullOrWhiteSpace(entry.Detail) &&
            entry.Detail.Contains(' '))
        {
            tags.AddRange(ForYouLearningFilter.Sanitize(
                entry.Detail.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));
        }

        return tags
            .Where(t => !string.IsNullOrWhiteSpace(t) && !ForYouLearningFilter.IsExcluded(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static double StrengthForReplay(ForYouActivityEntry entry)
    {
        if (Math.Abs(entry.WeightDelta) > 0.0001)
        {
            return entry.WeightDelta;
        }

        return entry.SignalType switch
        {
            ForYouSignalType.Search => ForYouSignalStrengths.ManualSearch,
            ForYouSignalType.SimilarTagSearch => ForYouSignalStrengths.SimilarTagSearchRepeated,
            ForYouSignalType.SavedTag => ForYouSignalStrengths.SavedTag,
            ForYouSignalType.Favorite => ForYouSignalStrengths.SavedPost,
            ForYouSignalType.WatchLater => ForYouSignalStrengths.SavedPost,
            ForYouSignalType.Download => ForYouSignalStrengths.FinishesPostsWithTag,
            ForYouSignalType.PostOpened => ForYouSignalStrengths.PostOpened,
            ForYouSignalType.PostReopened => ForYouSignalStrengths.PostReopened,
            ForYouSignalType.PostCompleted => ForYouSignalStrengths.FullyWatched,
            ForYouSignalType.LongView => ForYouSignalStrengths.LongView,
            ForYouSignalType.QuickSkip => ForYouSignalStrengths.QuickSkip,
            ForYouSignalType.RepeatedTagView => ForYouSignalStrengths.RepeatedTagView,
            ForYouSignalType.SimilarTag => ForYouSignalStrengths.SimilarTag,
            ForYouSignalType.TagClicked => ForYouSignalStrengths.TagClicked,
            ForYouSignalType.TopicBlocked => ForYouSignalStrengths.BlockedTag,
            ForYouSignalType.NotInterested => ForYouSignalStrengths.NotInterested,
            ForYouSignalType.ReportedPost => ForYouSignalStrengths.ReportedPost,
            ForYouSignalType.TopicWeightAdjusted => 0.2,
            _ => 0.4,
        };
    }

    private static double ParseManualTopicStrength(string detail)
    {
        if (detail.StartsWith("strength:", StringComparison.OrdinalIgnoreCase) &&
            double.TryParse(detail["strength:".Length..], out var parsed))
        {
            return parsed;
        }

        return 80;
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
}
