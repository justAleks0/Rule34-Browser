namespace Rule34Gallery.Core.Services;

public static class ForYouTopicFocus
{
    private static readonly HashSet<string> GenericTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "female", "male", "1girl", "1girls", "1boy", "1futa", "2girls", "2boys", "3girls", "solo", "duo", "group",
        "breasts", "big_breasts", "big_ass", "large_breasts", "huge_breasts", "gigantic_breasts",
        "bottomless", "3d_animation",
        "nipples", "areolae", "nude", "naked", "pussy", "penis", "ass", "anus", "sex",
        "vaginal", "anal", "oral", "fellatio", "cum", "ejaculation", "creampie",
        "blush", "smile", "open_mouth", "looking_at_viewer", "english_commentary",
        "3d", "animated", "video", "sound", "highres", "absurdres",
    };

    public static bool IsGenericTopic(string topic)
    {
        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || ForYouLearningFilter.IsExcluded(normalized))
        {
            return true;
        }

        if (GenericTags.Contains(normalized))
        {
            return true;
        }

        return normalized.EndsWith("_hair", StringComparison.Ordinal) ||
               normalized.EndsWith("_eyes", StringComparison.Ordinal);
    }

    public static bool IsCharacterTopic(string topic)
    {
        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        return normalized.Contains('(') && normalized.Contains('_');
    }

    public static IReadOnlyList<ForYouTopicProfile> RankTopicsByStrength(
        IEnumerable<ForYouTopicProfile> topics,
        int take = 24)
    {
        return topics
            .Where(t => !t.IsBlocked && !string.IsNullOrWhiteSpace(t.Topic))
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.Weight)
            .ThenByDescending(t => t.LastSeenUnix)
            .Take(take)
            .ToList();
    }

    public static IReadOnlyList<ForYouTopicProfile> FocusTopics(
        IEnumerable<ForYouTopicProfile> topics,
        int take = 6)
    {
        var filtered = topics
            .Where(t => !t.IsBlocked && !IsGenericTopic(t.Topic))
            .ToList();
        var characters = filtered
            .Where(t => IsCharacterTopic(t.Topic))
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.Weight);
        var rest = filtered
            .Where(t => !IsCharacterTopic(t.Topic))
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.Weight);
        return characters.Concat(rest).Take(take).ToList();
    }

    public static double ComputeWeightedTopicMatchScore(
        IReadOnlyList<string> postTags,
        IReadOnlyList<(string Tag, double Weight)> topicWeights)
    {
        if (topicWeights.Count == 0)
        {
            return 0;
        }

        var normalizedPost = postTags.Select(ForYouTagMatching.Normalize).ToHashSet(StringComparer.Ordinal);
        var sum = 0.0;
        foreach (var (tag, weight) in topicWeights)
        {
            if (normalizedPost.Contains(ForYouTagMatching.Normalize(tag)))
            {
                sum += weight;
            }
        }

        return sum;
    }

    public static int CountTagMatches(IReadOnlyList<string> postTags, IReadOnlyList<string> focusTags)
    {
        if (focusTags.Count == 0)
        {
            return 0;
        }

        return ForYouTagMatching.CountExactMatches(postTags, focusTags);
    }
}
